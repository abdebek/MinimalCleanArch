using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using CliWrap;
using CliWrap.Buffered;
using Xunit.Abstractions;

namespace MinimalCleanArch.Templates.Tests;

public class TemplateIntegrationTests : IClassFixture<TemplateTestFixture>, IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly string _testRunId;
    private readonly string _baseOutputDir;
    private readonly string _packageSource;
    private readonly string _templateVersion;
    private readonly int _appPort;
    private const string TemplateFramework = "net10.0";

    /// <summary>
    /// When true (env MCA_KEEP_TEMPLATE_OUTPUT=1), scaffold dirs under temp/MCA_Tests are kept for debugging.
    /// </summary>
    private static readonly bool KeepTemplateOutput =
        string.Equals(Environment.GetEnvironmentVariable("MCA_KEEP_TEMPLATE_OUTPUT"), "1", StringComparison.Ordinal)
        || string.Equals(Environment.GetEnvironmentVariable("MCA_KEEP_TEMPLATE_OUTPUT"), "true", StringComparison.OrdinalIgnoreCase);

    public TemplateIntegrationTests(TemplateTestFixture fixture, ITestOutputHelper output)
    {
        _output = output;
        _testRunId = Guid.NewGuid().ToString("N").Substring(0, 8);
        _baseOutputDir = Path.Combine(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../../../../temp")), "MCA_Tests", _testRunId);
        Directory.CreateDirectory(_baseOutputDir);
        _packageSource = fixture.PackageSource;
        _templateVersion = fixture.TemplateVersion;
        // Unique port per test instance so parallel template runs do not collide on :5000
        _appPort = Random.Shared.Next(5200, 5900);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        if (KeepTemplateOutput)
        {
            _output.WriteLine($"Keeping template output (MCA_KEEP_TEMPLATE_OUTPUT): {_baseOutputDir}");
            return Task.CompletedTask;
        }

        TryDeleteDirectory(_baseOutputDir, _output);
        return Task.CompletedTask;
    }

    private static void TryDeleteDirectory(string path, ITestOutputHelper? output = null)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return;
        }

        try
        {
            // Builds can leave read-only files; clear attributes before delete.
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                catch
                {
                    // Best-effort
                }
            }

            Directory.Delete(path, recursive: true);
            output?.WriteLine($"Cleaned template output: {path}");
        }
        catch (Exception ex)
        {
            output?.WriteLine($"Warning: failed to clean template output '{path}': {ex.Message}");
        }
    }

    private void CreateNugetConfig(string projectDir)
    {
        Directory.CreateDirectory(projectDir);
        var repoRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../../../../"));
        var candidateSources = new[]
        {
            _packageSource,
            Path.Combine(repoRoot, "artifacts/nuget"),
            Path.Combine(repoRoot, "artifacts/packages")
        };

        var localPackageSource = candidateSources
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path) && Directory.EnumerateFiles(path, "*.nupkg").Any())
            ?? candidateSources.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            ?? candidateSources.First(path => !string.IsNullOrWhiteSpace(path));
        var globalPackages = Path.Combine(projectDir, ".packages");

        var nugetConfigContent = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                <packageSources>
                    <clear />
                    <add key="local" value="{localPackageSource}" />
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                </packageSources>
                <packageSourceMapping>
                    <packageSource key="local">
                        <package pattern="MinimalCleanArch*" />
                    </packageSource>
                    <packageSource key="nuget.org">
                        <package pattern="*" />
                    </packageSource>
                </packageSourceMapping>
                <config>
                    <add key="globalPackagesFolder" value="{globalPackages}" />
                </config>
                </configuration>
                """;

        File.WriteAllText(Path.Combine(projectDir, "nuget.config"), nugetConfigContent);
        _output.WriteLine($"Created nuget.config pointing to {localPackageSource} and global packages at {globalPackages}");

        // Generated apps under repo temp/ inherit the repository Directory.Build.props
        // (TreatWarningsAsErrors, MinVer, etc.). Isolate so NuGet audit advisories and
        // repo packaging policy do not fail template smoke builds.
        WriteIsolatedDirectoryBuildProps(projectDir);
    }

    private static void WriteIsolatedDirectoryBuildProps(string projectDir)
    {
        var content = """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
                <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
                <NuGetAudit>false</NuGetAudit>
                <GenerateDocumentationFile>false</GenerateDocumentationFile>
                <EnforceCodeStyleInBuild>false</EnforceCodeStyleInBuild>
              </PropertyGroup>
            </Project>
            """;

        File.WriteAllText(Path.Combine(projectDir, "Directory.Build.props"), content);

        // Nearest Directory.Packages.props wins; disable CPM so explicit Version= pins work.
        var packagesProps = """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
              </PropertyGroup>
            </Project>
            """;
        File.WriteAllText(Path.Combine(projectDir, "Directory.Packages.props"), packagesProps);
    }

    [Fact]
    public async Task Create_Build_Run_SqlServer_Project()
    {
        var projectName = "TestAppSql";
        var projectDir = Path.Combine(_baseOutputDir, projectName);
        var dbName = $"mca_sql_{_testRunId}";

        // 1. Start SQL Server Container
        _output.WriteLine("Starting SQL Server...");
        var sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Pass@word1")
            .Build();

        await sqlContainer.StartAsync();

        try
        {
            // 2. Generate
            _output.WriteLine("Generating project...");
            CreateNugetConfig(projectDir);
            await RunDotnetCommandAsync(BuildTemplateArgs("new", "mca", "-n", projectName, "-o", projectDir, "--db", "sqlserver", "--dbName", dbName, "--all"));
            AssertTargetFramework(projectDir, projectName);

            // 3. Update config (Source)
            var connectionString = $"Server={sqlContainer.Hostname},{sqlContainer.GetMappedPublicPort(1433)};Database={dbName};User Id=sa;Password=Pass@word1;TrustServerCertificate=True";
            _output.WriteLine($"SQL Connection String: {connectionString}");
            UpdateAppSettings(projectDir, projectName, "DefaultConnection", connectionString);

            // 4. Build
            _output.WriteLine("Building project...");
            await BuildGeneratedProjectAsync(projectDir);

            // 5. Run App
            _output.WriteLine("Running app...");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            using var appProcess = StartApp(projectDir, projectName);

            try
            {
                // 6. Verify Health
                await WaitForHealthCheckAsync(GetHealthUrl(), cts.Token);
                _output.WriteLine("App is healthy!");
            }
            finally
            {
                if (!appProcess.HasExited)
                {
                    appProcess.Kill(true);
                }
            }
        }
        finally
        {
            await sqlContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task Create_Build_Run_Postgres_Project()
    {
        var projectName = "TestAppPg";
        var projectDir = Path.Combine(_baseOutputDir, projectName);
        var dbName = $"mca_pg_{_testRunId}";

        // 1. Start Postgres Container
        _output.WriteLine("Starting Postgres...");
        var pgContainer = new PostgreSqlBuilder("postgres:latest")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithDatabase(dbName)
            .Build();

        await pgContainer.StartAsync();

        try
        {
            // 2. Generate
            _output.WriteLine("Generating project...");
            CreateNugetConfig(projectDir);
            await RunDotnetCommandAsync(BuildTemplateArgs("new", "mca", "-n", projectName, "-o", projectDir, "--db", "postgres", "--dbName", dbName, "--all"));
            AssertTargetFramework(projectDir, projectName);

            // 3. Update config (Source)
            var connectionString = $"Server={pgContainer.Hostname};Port={pgContainer.GetMappedPublicPort(5432)};Database={dbName};User Id=postgres;Password=postgres";
            _output.WriteLine($"PG Connection String: {connectionString}");
            UpdateAppSettings(projectDir, projectName, "DefaultConnection", connectionString);

            // 4. Build
            _output.WriteLine("Building project...");
            await BuildGeneratedProjectAsync(projectDir);

            // 5. Run App
            _output.WriteLine("Running app...");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            using var appProcess = StartApp(projectDir, projectName);

            try
            {
                // 6. Verify Health
                await WaitForHealthCheckAsync(GetHealthUrl(), cts.Token);
                _output.WriteLine("App is healthy!");
            }
            finally
            {
                if (!appProcess.HasExited)
                {
                    appProcess.Kill(true);
                }
            }
        }
        finally
        {
            await pgContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task Create_Build_Run_Sqlite_Project()
    {
        var projectName = "TestAppSqlite";
        var projectDir = Path.Combine(_baseOutputDir, projectName);
        var dbName = $"mca_sqlite_{_testRunId}";

        // 1. Generate (Sqlite is default, but we can specify it)
        _output.WriteLine("Generating project...");
        CreateNugetConfig(projectDir);
        // Use --healthchecks to ensure we have an endpoint to test, but avoid --recommended which might add Redis
        await RunDotnetCommandAsync(BuildTemplateArgs("new", "mca", "-n", projectName, "-o", projectDir, "--db", "sqlite", "--dbName", dbName, "--healthchecks"));
        AssertTargetFramework(projectDir, projectName);

        // 2. Build
        _output.WriteLine("Building project...");
        await BuildGeneratedProjectAsync(projectDir);

        // 3. Run App
        _output.WriteLine("Running app...");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var appProcess = StartApp(projectDir, projectName);

        try
        {
            // 4. Verify Health
            await WaitForHealthCheckAsync(GetHealthUrl(), cts.Token);
            _output.WriteLine("App is healthy!");
        }
        finally
        {
            if (!appProcess.HasExited)
            {
                appProcess.Kill(true);
            }
        }
    }

    [Fact]
    public async Task Create_Build_Run_Redis_Project()
    {
        var projectName = "TestAppRedis";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        // 1. Start Redis Container
        _output.WriteLine("Starting Redis...");
        var redisContainer = new RedisBuilder("redis:latest").Build();
        await redisContainer.StartAsync();

        try
        {
            // 2. Generate
            _output.WriteLine("Generating project...");
            CreateNugetConfig(projectDir);
            // Ensure healthchecks are enabled so we can verify startup
            await RunDotnetCommandAsync(BuildTemplateArgs("new", "mca", "-n", projectName, "-o", projectDir, "--caching", "--healthchecks"));
            AssertTargetFramework(projectDir, projectName);

            // 3. Update config (Source)
            var connectionString = redisContainer.GetConnectionString();
            _output.WriteLine($"Redis Connection String: {connectionString}");
            UpdateAppSettings(projectDir, projectName, "Redis", connectionString);

            // 4. Build
            _output.WriteLine("Building project...");
            await BuildGeneratedProjectAsync(projectDir);

            // 5. Run App
            _output.WriteLine("Running app...");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            using var appProcess = StartApp(projectDir, projectName);

            try
            {
                // 6. Verify Health
                await WaitForHealthCheckAsync(GetHealthUrl(), cts.Token);
                _output.WriteLine("App is healthy!");
            }
            finally
            {
                if (!appProcess.HasExited)
                {
                    appProcess.Kill(true);
                }
            }
        }
        finally
        {
            await redisContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task Create_Build_Storage_MultiProject()
    {
        var projectName = "TestAppStorage";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        _output.WriteLine("Generating multi-project with --storage...");
        CreateNugetConfig(projectDir);
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", projectName, "-o", projectDir,
            "--storage", "--healthchecks"));

        AssertMultiProjectSrcLayout(projectDir, projectName);
        var apiCsproj = MultiSrc(projectDir, projectName, "Api", $"{projectName}.Api.csproj");
        File.Exists(apiCsproj).Should().BeTrue();
        File.ReadAllText(apiCsproj).Should().Contain("MinimalCleanArch.Storage");
        // Scaffolded TFM is fixed — no dead dual-package groups for the other TFM
        File.ReadAllText(apiCsproj).Should().NotContain("net9.0");
        File.ReadAllText(apiCsproj).Should().Contain("net10.0");

        var storageEndpoints = MultiSrc(projectDir, projectName, "Api", "Endpoints", "StorageEndpoints.cs");
        File.Exists(storageEndpoints).Should().BeTrue();

        var apiProgram = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "Program.cs"));
        apiProgram.Should().Contain("AddBlobStorage");
        apiProgram.Should().Contain("MapStorageEndpoints");
        apiProgram.Should().NotContain("using MCA.Application.Commands");

        var appsettings = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "appsettings.json"));
        appsettings.Should().Contain("BlobStorage");
        appsettings.Should().Contain("\"Provider\"");
        appsettings.Should().Contain("R2ServiceUrl");
        appsettings.Should().NotContain("OpenIddict");
        appsettings.Should().NotContain("RateLimiting");

        AssertTargetFramework(projectDir, projectName);

        _output.WriteLine("Building storage-enabled solution...");
        await BuildGeneratedProjectAsync(projectDir);
    }

    [Fact]
    public async Task Create_Auth_UsesEmailPackage_NotCopiedSenders()
    {
        var projectName = "TestAppAuthEmail";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        CreateNugetConfig(projectDir);
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", projectName, "-o", projectDir, "--auth", "--tests"));

        AssertMultiProjectSrcLayout(projectDir, projectName);

        var infraCsproj = File.ReadAllText(MultiSrc(projectDir, projectName, "Infrastructure", $"{projectName}.Infrastructure.csproj"));
        infraCsproj.Should().Contain("MinimalCleanArch.Email");

        var apiCsproj = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", $"{projectName}.Api.csproj"));
        apiCsproj.Should().Contain("MinimalCleanArch.Email");

        var identityExt = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "Configuration", "IdentityServiceExtensions.cs"));
        identityExt.Should().Contain("AddEmail(");
        identityExt.Should().NotContain("new SmtpEmailSender");
        identityExt.Should().NotContain("ApiEmailSender");
        identityExt.Should().Contain("AddGoogle");
        identityExt.Should().Contain("AddMicrosoftAccount");
        identityExt.Should().Contain("AddGitHub");
        identityExt.Should().NotContain("// .AddGoogle");
        identityExt.Should().NotContain("// .AddMicrosoftAccount");
        identityExt.Should().NotContain("// .AddGitHub");

        var authEndpoints = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "Endpoints", "AuthEndpoints.cs"));
        authEndpoints.Should().Contain("Continue with Google");
        authEndpoints.Should().NotContain("<!--");

        var appsettingsAuth = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "appsettings.json"));
        appsettingsAuth.Should().Contain("\"Authentication\"");
        appsettingsAuth.Should().Contain("\"ClientId\": \"\"");
        appsettingsAuth.Should().Contain("\"ClientSecret\": \"\"");

        Directory.GetFiles(
                MultiSrc(projectDir, projectName, "Infrastructure", "Services"),
                "*EmailSender.cs",
                SearchOption.TopDirectoryOnly)
            .Should().BeEmpty("transport senders live in MinimalCleanArch.Email, not copied stubs");

        var emailService = File.ReadAllText(MultiSrc(projectDir, projectName, "Infrastructure", "Services", "EmailService.cs"));
        emailService.Should().Contain("using MinimalCleanArch.Email");

        var appInterface = File.ReadAllText(MultiSrc(projectDir, projectName, "Application", "Interfaces", "IEmailService.cs"));
        appInterface.Should().Contain("IEmailService");
        appInterface.Should().NotContain("interface IEmailSender");

        await BuildGeneratedProjectAsync(projectDir);
        await RunDotnetCommandAsync(
            "test",
            Path.Combine(projectDir, "tests", $"{projectName}.IntegrationTests", $"{projectName}.IntegrationTests.csproj"),
            "--filter", "FullyQualifiedName~ExternalProvider",
            "--nologo",
            "/p:NuGetAudit=false",
            "/p:TreatWarningsAsErrors=false");
    }

    [Fact]
    public async Task Create_All_Tests_KeepsAuthFactory_WhenAuthEndpointTestsExcluded()
    {
        var projectName = "TestAppAllAuthFactory";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        CreateNugetConfig(projectDir);
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", projectName, "-o", projectDir, "--all"));

        var testsDir = Path.Combine(projectDir, "tests", $"{projectName}.IntegrationTests");
        File.Exists(Path.Combine(testsDir, "AuthEndpointTests.cs")).Should().BeFalse();
        File.Exists(Path.Combine(testsDir, "Auth", "AuthTestApiFactory.cs")).Should().BeTrue();
        File.Exists(Path.Combine(testsDir, "Auth", "ExternalProviderTests.cs")).Should().BeTrue();
        File.ReadAllText(Path.Combine(testsDir, "Auth", "AuthTestApiFactory.cs"))
            .Should().Contain("class AuthTestApiFactory")
            .And.NotContain("#if");

        await BuildGeneratedProjectAsync(projectDir);
    }

    [Fact]
    public async Task Create_Auth_Multitenant_IsolatesTodosByTenant()
    {
        var projectName = "TestAppTenancy";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        CreateNugetConfig(projectDir);
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", projectName, "-o", projectDir, "--auth", "--multitenant", "--tests"));

        AssertMultiProjectSrcLayout(projectDir, projectName);

        var todoEntity = File.ReadAllText(MultiSrc(projectDir, projectName, "Domain", "Entities", "Todo.cs"));
        todoEntity.Should().Contain("ITenantEntity");
        todoEntity.Should().Contain("TenantId");
        todoEntity.Should().NotContain("#if");

        var userEntity = File.ReadAllText(MultiSrc(projectDir, projectName, "Application", "Identity", "ApplicationUser.cs"));
        userEntity.Should().Contain("TenantId");

        var claimsFactory = File.ReadAllText(MultiSrc(projectDir, projectName, "Infrastructure", "Services", "CustomClaimsPrincipalFactory.cs"));
        claimsFactory.Should().Contain("tenant_id");

        var todoEndpoints = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "Endpoints", "TodoEndpoints.cs"));
        todoEndpoints.Should().Contain("RequireAuthorization");

        var programCs = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "Program.cs"));
        programCs.Should().Contain("AddMinimalCleanArchExecutionContext");
        programCs.Should().Contain("MapOrganizationEndpoints");

        File.Exists(MultiSrc(projectDir, projectName, "Domain", "Entities", "Organization.cs")).Should().BeTrue();

        File.Exists(Path.Combine(projectDir, "tests", $"{projectName}.IntegrationTests", "Auth", "TenantIsolationTests.cs"))
            .Should().BeTrue();
        File.Exists(Path.Combine(projectDir, "tests", $"{projectName}.IntegrationTests", "Auth", "OrganizationMembershipTests.cs"))
            .Should().BeTrue();
        File.Exists(Path.Combine(projectDir, "tests", $"{projectName}.IntegrationTests", "TodoEndpointTests.cs"))
            .Should().BeFalse("unauthenticated Todo tests are replaced by tenant isolation tests");

        await BuildGeneratedProjectAsync(projectDir);
        await RunDotnetCommandAsync(
            "test",
            Path.Combine(projectDir, "tests", $"{projectName}.IntegrationTests", $"{projectName}.IntegrationTests.csproj"),
            "--filter", "FullyQualifiedName~TenantIsolation|FullyQualifiedName~OrganizationMembership",
            "--nologo",
            "/p:NuGetAudit=false",
            "/p:TreatWarningsAsErrors=false");
    }

    [Fact]
    public async Task Create_Minimal_HasNoDeadFeatureCode()
    {
        var projectName = "TestAppMinimal";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        CreateNugetConfig(projectDir);
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", projectName, "-o", projectDir));

        AssertMultiProjectSrcLayout(projectDir, projectName);
        var endpointsDir = MultiSrc(projectDir, projectName, "Api", "Endpoints");
        Directory.GetFiles(endpointsDir, "*.cs").Select(Path.GetFileName)
            .Should().BeEquivalentTo(new[] { "TodoEndpoints.cs" });

        var apiCsproj = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", $"{projectName}.Api.csproj"));
        apiCsproj.Should().NotContain("MinimalCleanArch.Storage");
        apiCsproj.Should().NotContain("MinimalCleanArch.Email");
        apiCsproj.Should().NotContain("OpenIddict");
        apiCsproj.Should().NotContain("WolverineFx.FluentValidation");
        // Only the selected TFM package line (net10 default)
        apiCsproj.Should().NotContain("Version=\"9.0.");
        apiCsproj.Should().NotContain("Condition=\"'$(TargetFramework)'");

        var appsettings = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "appsettings.json"));
        appsettings.Should().NotContain("BlobStorage");
        appsettings.Should().NotContain("OpenIddict");
        appsettings.Should().NotContain("RateLimiting");
        appsettings.Should().NotContain("Encryption");
        appsettings.Should().Contain("Database");

        var program = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "Program.cs"));
        program.Should().Contain("MapScalarApiReference()");
        program.Should().NotContain("AddPasswordFlow");
        program.Should().NotContain("MapStorageEndpoints");
        program.Should().NotContain("AddMinimalCleanArchApiVersioning");

        var todoEntity = File.ReadAllText(MultiSrc(projectDir, projectName, "Domain", "Entities", "Todo.cs"));
        todoEntity.Should().NotContain("ITenantEntity");
        todoEntity.Should().NotContain("TenantId");

        Directory.Exists(Path.Combine(projectDir, "apps")).Should().BeFalse("API-only generation must not emit apps/web or apps/mobile");

        await BuildGeneratedProjectAsync(projectDir);
    }

    [Fact]
    public async Task Create_Frontend_EmitsAppsWeb_ApiStillBuilds()
    {
        var projectName = "TestAppFrontend";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        CreateNugetConfig(projectDir);
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", projectName, "-o", projectDir, "--frontend"));

        AssertMultiProjectSrcLayout(projectDir, projectName);
        File.Exists(Path.Combine(projectDir, "apps", "web", "README.md")).Should().BeTrue();
        File.Exists(Path.Combine(projectDir, "apps", "web", "src", "lib", "auth", "client.ts")).Should().BeTrue();
        File.Exists(Path.Combine(projectDir, "apps", "web", "astro.config.mjs")).Should().BeTrue();
        File.Exists(Path.Combine(projectDir, "apps", "web", "src", "pages", "login.astro")).Should().BeTrue();
        File.Exists(Path.Combine(projectDir, "apps", "web", "src", "pages", "todos.astro")).Should().BeTrue();
        File.Exists(Path.Combine(projectDir, "apps", "web", "src", "lib", "i18n.ts")).Should().BeTrue();
        File.Exists(Path.Combine(projectDir, "apps", "web", "e2e", "smoke.spec.ts")).Should().BeTrue();
        File.ReadAllText(Path.Combine(projectDir, "apps", "web", "src", "lib", "i18n.ts")).Should().Contain("rtl");
        Directory.Exists(Path.Combine(projectDir, "apps", "mobile")).Should().BeFalse();
        File.ReadAllText(Path.Combine(projectDir, "apps", "web", "README.md"))
            .Should().Contain($"src/{projectName}.Api");
        var slnx = File.ReadAllText(Path.Combine(projectDir, $"{projectName}.slnx"));
        slnx.Should().NotContain("apps/web");

        await BuildGeneratedProjectAsync(projectDir);
    }

    [Fact]
    public async Task Create_Frontend_Auth_EmitsOidcPkceClient()
    {
        var projectName = "TestAppOidc";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        CreateNugetConfig(projectDir);
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", projectName, "-o", projectDir, "--frontend", "--auth"));

        AssertMultiProjectSrcLayout(projectDir, projectName);
        var authClient = File.ReadAllText(Path.Combine(projectDir, "apps", "web", "src", "lib", "auth", "client.ts"));
        authClient.Should().Contain("oidc-client-ts");
        authClient.Should().Contain("signinRedirect");
        authClient.Should().Contain("signinSilent");
        authClient.Should().Contain("Authorization");
        authClient.Should().NotContain("#if");

        var pkg = File.ReadAllText(Path.Combine(projectDir, "apps", "web", "package.json"));
        pkg.Should().Contain("oidc-client-ts");

        var identity = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "Configuration", "IdentityServiceExtensions.cs"));
        identity.Should().Contain("mca-spa-client");
        identity.Should().Contain("http://localhost:4321/callback");
        identity.Should().Contain("http://127.0.0.1:4321/callback");
        identity.Should().Contain("AddEphemeralSigningKey");
        identity.Should().NotContain("AddDevelopmentSigningCertificate");
        identity.Should().Contain("ClientTypes.Public");
        identity.Should().NotContain("#if");

        var appsettings = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "appsettings.json"));
        appsettings.Should().Contain("mca-spa-client");
        appsettings.Should().Contain("http://localhost:4321");
        appsettings.Should().Contain("http://127.0.0.1:4321");
        appsettings.Should().NotContain("#if");

        var program = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "Program.cs"));
        program.Should().Contain("AddCors");
        program.Should().Contain("UseCors");
        program.Should().NotContain("#if");

        await BuildGeneratedProjectAsync(projectDir);
    }

    [Fact]
    public async Task Create_Mobile_EmitsAppsMobile_ApiStillBuilds()
    {
        var projectName = "TestAppMobile";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        CreateNugetConfig(projectDir);
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", projectName, "-o", projectDir, "--mobile"));

        AssertMultiProjectSrcLayout(projectDir, projectName);
        File.Exists(Path.Combine(projectDir, "apps", "mobile", "README.md")).Should().BeTrue();
        File.Exists(Path.Combine(projectDir, "apps", "mobile", "App.js")).Should().BeTrue();
        File.Exists(Path.Combine(projectDir, "apps", "mobile", "app.json")).Should().BeTrue();
        File.ReadAllText(Path.Combine(projectDir, "apps", "mobile", "App.js")).Should().Contain("expo-secure-store");
        Directory.Exists(Path.Combine(projectDir, "apps", "web")).Should().BeFalse();

        await BuildGeneratedProjectAsync(projectDir);
    }

    [Fact]
    public async Task Create_Frontend_Tanstack_EmitsViteReact()
    {
        var projectName = "TestAppTanstack";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        CreateNugetConfig(projectDir);
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", projectName, "-o", projectDir, "--frontend", "--webFramework", "tanstack"));

        File.Exists(Path.Combine(projectDir, "apps", "web", "vite.config.ts")).Should().BeTrue();
        File.Exists(Path.Combine(projectDir, "apps", "web", "src", "router.tsx")).Should().BeTrue();
        File.Exists(Path.Combine(projectDir, "apps", "web", "src", "routes", "__root.tsx")).Should().BeTrue();
        File.Exists(Path.Combine(projectDir, "apps", "web", "src", "lib", "auth", "client.ts")).Should().BeTrue();
        File.Exists(Path.Combine(projectDir, "apps", "web", "astro.config.mjs")).Should().BeFalse();
        File.ReadAllText(Path.Combine(projectDir, "apps", "web", "package.json"))
            .Should().Contain("@tanstack/react-start")
            .And.Contain("test:e2e");
        File.Exists(Path.Combine(projectDir, "apps", "web", "e2e", "smoke.spec.ts")).Should().BeTrue();
        File.Exists(Path.Combine(projectDir, "apps", "web", "playwright.config.ts")).Should().BeTrue();
        File.ReadAllText(Path.Combine(projectDir, "apps", "web", "e2e", "smoke.spec.ts"))
            .Should().Contain("todo-title")
            .And.Contain("oidc-login");
        File.ReadAllText(Path.Combine(projectDir, "apps", "web", "src", "routes", "login.tsx"))
            .Should().Contain("oidc-login");
        File.ReadAllText(Path.Combine(projectDir, "apps", "web", "src", "lib", "auth", "client.ts"))
            .Should().Contain("oidc-client-ts")
            .And.Contain("callbackPromise");

        await BuildGeneratedProjectAsync(projectDir);
    }

    [Fact]
    public async Task Create_Controllers_MapsTodoController_NotMinimalTodoEndpoints()
    {
        var projectName = "TestAppControllers";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        CreateNugetConfig(projectDir);
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", projectName, "-o", projectDir, "--controllers"));

        AssertMultiProjectSrcLayout(projectDir, projectName);
        var program = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "Program.cs"));
        program.Should().Contain("AddControllers");
        program.Should().Contain("MapControllers");
        program.Should().NotContain("MapTodoEndpoints");
        File.Exists(MultiSrc(projectDir, projectName, "Api", "Controllers", "TodoController.cs")).Should().BeTrue();
        File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "Controllers", "TodoController.cs"))
            .Should().Contain("TodoCommandHandler")
            .And.NotContain("MapGet");

        await BuildGeneratedProjectAsync(projectDir);
    }

    [Fact]
    public async Task Create_FastEndpoints_MapsTodos_NotMinimalTodoEndpoints()
    {
        var projectName = "TestAppFastEp";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        CreateNugetConfig(projectDir);
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", projectName, "-o", projectDir, "--fastendpoints"));

        AssertMultiProjectSrcLayout(projectDir, projectName);
        var program = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "Program.cs"));
        program.Should().Contain("AddFastEndpoints");
        program.Should().Contain("UseFastEndpoints");
        program.Should().NotContain("MapTodoEndpoints");
        program.Should().NotContain("MapControllers");
        File.Exists(MultiSrc(projectDir, projectName, "Api", "FastEndpoints", "TodoFastEndpoints.cs")).Should().BeTrue();
        File.ReadAllText(MultiSrc(projectDir, projectName, "Api", $"{projectName}.Api.csproj"))
            .Should().Contain("FastEndpoints");

        await BuildGeneratedProjectAsync(projectDir);
    }

    [Fact]
    public async Task Create_Recommended_RegistersApiVersioning()
    {
        var projectName = "TestAppVersioning";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        CreateNugetConfig(projectDir);
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", projectName, "-o", projectDir, "--recommended"));

        AssertMultiProjectSrcLayout(projectDir, projectName);
        var program = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "Program.cs"));
        program.Should().Contain("AddMinimalCleanArchApiVersioning");
        program.Should().Contain("using MinimalCleanArch.Extensions.Versioning");
        program.Should().NotContain("#if");

        var todoHandler = File.ReadAllText(MultiSrc(projectDir, projectName, "Application", "Handlers", "TodoCommandHandler.cs"));
        todoHandler.Should().Contain("ICacheService");
        todoHandler.Should().NotContain("IMemoryCache");

        await BuildGeneratedProjectAsync(projectDir);
    }

    [Fact]
    public async Task Create_Caching_TodoUsesICacheService_HitPath()
    {
        var projectName = "TestAppCache";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        CreateNugetConfig(projectDir);
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", projectName, "-o", projectDir, "--caching", "--tests"));

        AssertMultiProjectSrcLayout(projectDir, projectName);
        var todoHandler = File.ReadAllText(MultiSrc(projectDir, projectName, "Application", "Handlers", "TodoCommandHandler.cs"));
        todoHandler.Should().Contain("ICacheService");
        todoHandler.Should().Contain("GetOrCreateAsync");
        todoHandler.Should().Contain("todos.Select(MapToResponse)");
        todoHandler.Should().NotContain("IMemoryCache");

        var appCsproj = File.ReadAllText(MultiSrc(projectDir, projectName, "Application", $"{projectName}.Application.csproj"));
        appCsproj.Should().Contain("MinimalCleanArch.Extensions");
        File.Exists(Path.Combine(projectDir, "tests", $"{projectName}.UnitTests", "Handlers", "TodoCacheTests.cs"))
            .Should().BeTrue();

        await BuildGeneratedProjectAsync(projectDir);
    }

    [Fact]
    public async Task Create_Jobs_RegistersPurgeSoftDeletedJob()
    {
        var projectName = "TestAppJobs";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        CreateNugetConfig(projectDir);
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", projectName, "-o", projectDir, "--jobs"));

        AssertMultiProjectSrcLayout(projectDir, projectName);
        var program = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "Program.cs"));
        program.Should().Contain("AddJobs");
        program.Should().Contain("PurgeSoftDeletedTodos");
        program.Should().NotContain("AddWolverineJobs");
        program.Should().NotContain("#if");

        var appCsproj = File.ReadAllText(MultiSrc(projectDir, projectName, "Application", $"{projectName}.Application.csproj"));
        appCsproj.Should().Contain("MinimalCleanArch.Jobs");

        File.Exists(MultiSrc(projectDir, projectName, "Application", "Jobs", "PurgeSoftDeletedTodos.cs"))
            .Should().BeTrue();
        var repo = File.ReadAllText(MultiSrc(projectDir, projectName, "Infrastructure", "Repositories", "TodoRepository.cs"));
        repo.Should().Contain("HardDeleteSoftDeletedOlderThanAsync");
        repo.Should().Contain("IgnoreQueryFilters");

        await BuildGeneratedProjectAsync(projectDir);
    }

    [Fact]
    public async Task Create_Realtime_MapsHub_AndPublishesTodoChanges()
    {
        var projectName = "TestAppRealtime";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        CreateNugetConfig(projectDir);
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", projectName, "-o", projectDir, "--realtime", "--tests"));

        AssertMultiProjectSrcLayout(projectDir, projectName);
        var program = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "Program.cs"));
        program.Should().Contain("AddMinimalCleanArchRealtime");
        program.Should().Contain("MapMinimalCleanArchRealtime");
        program.Should().NotContain("#if");

        var handler = File.ReadAllText(MultiSrc(projectDir, projectName, "Application", "Handlers", "TodoCommandHandler.cs"));
        handler.Should().Contain("IRealtimePublisher");
        handler.Should().Contain("PublishTodoChangedAsync");
        handler.Should().NotContain("Hub");

        var appCsproj = File.ReadAllText(MultiSrc(projectDir, projectName, "Application", $"{projectName}.Application.csproj"));
        appCsproj.Should().Contain("MinimalCleanArch.Realtime");
        appCsproj.Should().NotContain("SignalR");

        File.Exists(Path.Combine(projectDir, "tests", $"{projectName}.IntegrationTests", "RealtimeHubTests.cs"))
            .Should().BeTrue();

        await BuildGeneratedProjectAsync(projectDir);
        await RunDotnetCommandAsync(
            "test",
            Path.Combine(projectDir, "tests", $"{projectName}.IntegrationTests", $"{projectName}.IntegrationTests.csproj"),
            "--filter", "FullyQualifiedName~RealtimeHub",
            "--nologo",
            "/p:NuGetAudit=false",
            "/p:TreatWarningsAsErrors=false");
    }

    [Fact]
    public async Task Create_Features_GatesExportEndpointFromConfig()
    {
        var projectName = "TestAppFeatures";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        CreateNugetConfig(projectDir);
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", projectName, "-o", projectDir, "--features", "--tests"));

        AssertMultiProjectSrcLayout(projectDir, projectName);
        var program = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "Program.cs"));
        program.Should().Contain("AddFeatures");
        program.Should().NotContain("#if");

        var endpoints = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "Endpoints", "TodoEndpoints.cs"));
        endpoints.Should().Contain("RequireFeature(\"todo-export\")");
        endpoints.Should().Contain("/export");
        File.ReadAllText(MultiSrc(projectDir, projectName, "Application", "Handlers", "TodoCommandHandler.cs"))
            .Should().NotContain("IFeatureGate");

        var appsettings = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "appsettings.json"));
        appsettings.Should().Contain("todo-export");

        var apiCsproj = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", $"{projectName}.Api.csproj"));
        apiCsproj.Should().Contain("MinimalCleanArch.Features");

        File.Exists(Path.Combine(projectDir, "tests", $"{projectName}.IntegrationTests", "FeatureGateTests.cs"))
            .Should().BeTrue();

        await BuildGeneratedProjectAsync(projectDir);
        await RunDotnetCommandAsync(
            "test",
            Path.Combine(projectDir, "tests", $"{projectName}.IntegrationTests", $"{projectName}.IntegrationTests.csproj"),
            "--filter", "FullyQualifiedName~FeatureGate",
            "--nologo",
            "/p:NuGetAudit=false",
            "/p:TreatWarningsAsErrors=false");
    }

    [Fact]
    public async Task Create_Restore_SoftDeletedTodo_IsVisibleAgain()
    {
        var projectName = "TestAppRestore";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        CreateNugetConfig(projectDir);
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", projectName, "-o", projectDir, "--tests"));

        AssertMultiProjectSrcLayout(projectDir, projectName);
        var endpoints = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "Endpoints", "TodoEndpoints.cs"));
        endpoints.Should().Contain("RestoreTodo");
        endpoints.Should().Contain("/restore");
        endpoints.Should().NotContain("#if");

        var entity = File.ReadAllText(MultiSrc(projectDir, projectName, "Domain", "Entities", "Todo.cs"));
        entity.Should().Contain("public void Restore()");
        File.ReadAllText(MultiSrc(projectDir, projectName, "Application", "Handlers", "TodoCommandHandler.cs"))
            .Should().Contain("RestoreTodoCommand");
        File.ReadAllText(MultiSrc(projectDir, projectName, "Infrastructure", "Repositories", "TodoRepository.cs"))
            .Should().Contain("SoftDelete");

        await BuildGeneratedProjectAsync(projectDir);
        await RunDotnetCommandAsync(
            "test",
            Path.Combine(projectDir, "tests", $"{projectName}.IntegrationTests", $"{projectName}.IntegrationTests.csproj"),
            "--filter", "FullyQualifiedName~RestoreTodo_AfterSoftDelete",
            "--nologo",
            "/p:NuGetAudit=false",
            "/p:TreatWarningsAsErrors=false");
    }

    [Fact]
    public async Task Create_Postgres_Docker_Uses_DbName_In_Compose_And_ConnectionStrings()
    {
        var namedDir = Path.Combine(_baseOutputDir, "ShopNamed");
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", "Shop", "-o", namedDir,
            "--db", "postgres", "--dbName", "shop", "--docker"));

        AssertGeneratedDbName(
            namedDir,
            projectName: "Shop",
            expectedDbName: "shop",
            singleProject: false);

        var defaultDir = Path.Combine(_baseOutputDir, "ShopDefault");
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", "Shop", "-o", defaultDir,
            "--db", "postgres", "--docker"));

        AssertGeneratedDbName(
            defaultDir,
            projectName: "Shop",
            expectedDbName: "Shop",
            singleProject: false);

        var singleDir = Path.Combine(_baseOutputDir, "ShopSingle");
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", "Shop", "-o", singleDir,
            "--single-project", "--db", "postgres", "--dbName", "shop", "--docker"));

        AssertGeneratedDbName(
            singleDir,
            projectName: "Shop",
            expectedDbName: "shop",
            singleProject: true);
    }

    private static void AssertGeneratedDbName(
        string projectDir,
        string projectName,
        string expectedDbName,
        bool singleProject)
    {
        var composePath = Path.Combine(projectDir, "docker-compose.yml");
        File.Exists(composePath).Should().BeTrue();
        var compose = File.ReadAllText(composePath);
        compose.Should().NotContain("__DB_NAME__");
        compose.Should().Contain($"Host=postgres;Database={expectedDbName};");
        compose.Should().Contain($"POSTGRES_DB={expectedDbName}");
        compose.Should().NotContain("Database=;");
        compose.Should().NotContain("POSTGRES_DB=\n");

        var appsettingsPath = singleProject
            ? Path.Combine(projectDir, "appsettings.json")
            : MultiSrc(projectDir, projectName, "Api", "appsettings.json");
        var appsettings = File.ReadAllText(appsettingsPath);
        appsettings.Should().NotContain("__DB_CONNECTION_STRING__");
        appsettings.Should().NotContain("{dbName}");
        appsettings.Should().NotContain("{EffectiveDbName}");
        appsettings.Should().Contain($"Database={expectedDbName}");
        appsettings.Should().NotContain("Database=;");

        var programPath = singleProject
            ? Path.Combine(projectDir, "Program.cs")
            : MultiSrc(projectDir, projectName, "Api", "Program.cs");
        var program = File.ReadAllText(programPath);
        program.Should().NotContain("Replace(\"{dbName}\"");
        program.Should().Contain("GetConnectionString(\"DefaultConnection\")");

        var factoryPath = singleProject
            ? Path.Combine(projectDir, "Infrastructure", "Data", "AppDbContextFactory.cs")
            : MultiSrc(projectDir, projectName, "Infrastructure", "Data", "AppDbContextFactory.cs");
        var factory = File.ReadAllText(factoryPath);
        factory.Should().NotContain("__DB_NAME__");
        factory.Should().Contain($"Database={expectedDbName}");
    }

    [Fact]
    public async Task Create_Build_Aspire_MultiProject()
    {
        var projectName = "TestAppAspire";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        _output.WriteLine("Generating Aspire multi-project...");
        CreateNugetConfig(projectDir);
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", projectName, "-o", projectDir,
            "--recommended", "--aspire", "--db", "postgres"));

        AssertMultiProjectSrcLayout(projectDir, projectName);
        var appHostCsproj = Path.Combine(projectDir, $"{projectName}.AppHost", $"{projectName}.AppHost.csproj");
        var serviceDefaultsCsproj = MultiSrc(projectDir, projectName, "ServiceDefaults", $"{projectName}.ServiceDefaults.csproj");
        File.Exists(appHostCsproj).Should().BeTrue("AppHost project should be generated");
        File.Exists(serviceDefaultsCsproj).Should().BeTrue("ServiceDefaults project should be generated under src/");

        // Stable connection name must not be rewritten by sourceName (MCA → project name)
        var appHostProgram = File.ReadAllText(Path.Combine(projectDir, $"{projectName}.AppHost", "Program.cs"));
        appHostProgram.Should().Contain("AddDatabase(\"appdb\")");
        appHostProgram.Should().NotContain("AddDatabase(\"mca\")");

        var apiProgram = File.ReadAllText(MultiSrc(projectDir, projectName, "Api", "Program.cs"));
        apiProgram.Should().Contain("GetConnectionString(\"appdb\")");
        apiProgram.Should().Contain("AddServiceDefaults()");
        apiProgram.Should().Contain("MapDefaultEndpoints()");
        apiProgram.Should().Contain("AddMinimalCleanArchApiVersioning");

        // docker-compose must be omitted when --aspire is set
        Directory.GetFiles(projectDir, "docker-compose.yml", SearchOption.AllDirectories)
            .Should().BeEmpty();

        AssertTargetFramework(projectDir, projectName);

        _output.WriteLine("Building Aspire solution (AppHost)...");
        await RunDotnetCommandAsync(
            "build",
            appHostCsproj,
            "/nodeReuse:false",
            "/p:NuGetAudit=false",
            "/p:TreatWarningsAsErrors=false");
    }

    [Fact]
    public async Task Create_Build_Aspire_SingleProject()
    {
        var projectName = "TestAppAspireSingle";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        _output.WriteLine("Generating Aspire single-project...");
        CreateNugetConfig(projectDir);
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", projectName, "-o", projectDir,
            "--single-project", "--recommended", "--aspire", "--db", "sqlserver"));

        var appHostCsproj = Path.Combine(projectDir, $"{projectName}.AppHost", $"{projectName}.AppHost.csproj");
        File.Exists(appHostCsproj).Should().BeTrue("AppHost project should be generated for single-project");
        File.Exists(Path.Combine(projectDir, $"{projectName}.csproj")).Should().BeTrue();
        File.Exists(Path.Combine(projectDir, $"{projectName}.slnx")).Should().BeTrue("single + aspire should include solution file");

        var appHostProgram = File.ReadAllText(Path.Combine(projectDir, $"{projectName}.AppHost", "Program.cs"));
        appHostProgram.Should().Contain("AddDatabase(\"appdb\")");
        appHostProgram.Should().Contain("AddSqlServer");
        // Single-project host references Projects.{Name} not Projects.{Name}_Api
        appHostProgram.Should().Contain($"Projects.{projectName}");
        appHostProgram.Should().NotContain($"Projects.{projectName}_Api");

        var program = File.ReadAllText(Path.Combine(projectDir, "Program.cs"));
        program.Should().Contain("GetConnectionString(\"appdb\")");
        program.Should().Contain("AddServiceDefaults()");

        AssertTargetFramework(projectDir, projectName);

        _output.WriteLine("Building Aspire single-project AppHost...");
        await RunDotnetCommandAsync(
            "build",
            appHostCsproj,
            "/nodeReuse:false",
            "/p:NuGetAudit=false",
            "/p:TreatWarningsAsErrors=false");
    }

    [Fact]
    public async Task Create_Build_Run_Storage_Endpoints()
    {
        var projectName = "TestAppStorageRun";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        _output.WriteLine("Generating --storage --healthchecks single-project...");
        CreateNugetConfig(projectDir);
        await RunDotnetCommandAsync(BuildTemplateArgs(
            "new", "mca", "-n", projectName, "-o", projectDir,
            "--storage", "--healthchecks"));
        AssertTargetFramework(projectDir, projectName);

        // Switch to R2 with fake credentials so presign (SigV4) succeeds without a live backend.
        // AzureBlobStorage SAS generation requires a reachable Azurite; R2 presign is a pure signing call.
        UpdateAppSettingsBlobStorage(projectDir, projectName);

        // Build
        _output.WriteLine("Building storage-enabled project...");
        await BuildGeneratedProjectAsync(projectDir);

        // Run App
        _output.WriteLine("Running app...");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var appProcess = StartApp(projectDir, projectName);

        try
        {
            await WaitForHealthCheckAsync(GetHealthUrl(), cts.Token);
            _output.WriteLine("App is healthy; exercising storage endpoints...");

            using var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{_appPort}") };

            // 1. Invalid upload request (missing blobKey) -> validation problem with only the missing fields
            var invalidUpload = await client.PostAsJsonAsync(
                "/api/storage/upload-url",
                new { BlobKey = "", ContentType = "application/pdf", ByteLength = 1024 });
            invalidUpload.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var invalidBody = await invalidUpload.Content.ReadAsStringAsync();
            _output.WriteLine($"Invalid upload response: {invalidBody}");
            invalidBody.Should().Contain("blobKey");
            // Content type and byteLength were valid; only blobKey error should be present
            invalidBody.Should().NotContain("Content type is required");
            invalidBody.Should().NotContain("Byte length must be greater than zero");

            // 2. Valid upload request -> presigned URL returned (R2 presign is pure SigV4, no network)
            var validUpload = await client.PostAsJsonAsync(
                "/api/storage/upload-url",
                new { BlobKey = "uploads/test.pdf", ContentType = "application/pdf", ByteLength = 1024 });
            validUpload.StatusCode.Should().Be(HttpStatusCode.OK);
            var uploadBody = await validUpload.Content.ReadAsStringAsync();
            _output.WriteLine($"Valid upload response: {uploadBody}");
            uploadBody.Should().Contain("uploadUrl");
            uploadBody.Should().Contain("uploads/test.pdf");

            // 3. Valid download request -> presigned URL returned
            var validDownload = await client.GetAsync("/api/storage/download-url?blobKey=uploads/test.pdf");
            validDownload.StatusCode.Should().Be(HttpStatusCode.OK);
            var downloadBody = await validDownload.Content.ReadAsStringAsync();
            _output.WriteLine($"Valid download response: {downloadBody}");
            downloadBody.Should().Contain("downloadUrl");
            downloadBody.Should().Contain("uploads/test.pdf");

            // 4. Path-traversal blob key -> rejected (BlobKeyValidator throws; not 200 OK with a presigned URL)
            var traversalDownload = await client.GetAsync("/api/storage/download-url?blobKey=../../etc/passwd");
            traversalDownload.StatusCode.Should().NotBe(HttpStatusCode.OK);
            var traversalBody = await traversalDownload.Content.ReadAsStringAsync();
            traversalBody.Should().NotContain("downloadUrl");
        }
        finally
        {
            if (!appProcess.HasExited)
            {
                appProcess.Kill(true);
            }
        }
    }

    private void UpdateAppSettingsBlobStorage(string projectDir, string projectName)
    {
        var appSettingsFiles = Directory.GetFiles(projectDir, "appsettings*.json", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToList();
        foreach (var file in appSettingsFiles)
        {
            var json = File.ReadAllText(file);
            var jNode = JsonNode.Parse(json);
            if (jNode?["BlobStorage"] is JsonObject blobStorage)
            {
                blobStorage["Provider"] = "R2";
                blobStorage["R2ServiceUrl"] = "https://example.r2.cloudflarestorage.com";
                blobStorage["R2AccessKeyId"] = "test-key-id";
                blobStorage["R2SecretAccessKey"] = "test-secret-key";
                blobStorage["R2BucketName"] = "app-data";
                File.WriteAllText(file, jNode!.ToString());
                _output.WriteLine($"Updated BlobStorage in {file}");
            }
        }
    }

    // Helpers

    private string GetHealthUrl() => $"http://localhost:{_appPort}/health";

    private Task BuildGeneratedProjectAsync(string projectDir) =>
        RunDotnetCommandAsync(
            "build",
            projectDir,
            "/nodeReuse:false",
            "/p:NuGetAudit=false",
            "/p:TreatWarningsAsErrors=false");

    private void AssertTargetFramework(string projectDir, string projectName)
    {
        var targetFramework = ResolveTargetFramework(projectDir, projectName);
        targetFramework.Should().Be(TemplateFramework);
    }

    private string ResolveTargetFramework(string projectDir, string projectName)
    {
        var projectPath = ResolveAppProjectPath(projectDir, projectName);
        var doc = XDocument.Load(projectPath);
        var tfm = doc.Descendants("TargetFramework").FirstOrDefault()?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(tfm))
        {
            return tfm;
        }

        var tfms = doc.Descendants("TargetFrameworks").FirstOrDefault()?.Value;
        if (!string.IsNullOrWhiteSpace(tfms))
        {
            var frameworks = tfms.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var match = frameworks.FirstOrDefault(framework => framework.Equals(TemplateFramework, StringComparison.OrdinalIgnoreCase));
            return match ?? frameworks.First();
        }

        throw new InvalidOperationException($"Target framework not found in {projectPath}");
    }

    private static string MultiSrc(string projectDir, string projectName, string suffix, params string[] rest)
    {
        return Path.Combine(new[] { projectDir, "src", $"{projectName}.{suffix}" }.Concat(rest).ToArray());
    }

    private static void AssertMultiProjectSrcLayout(string projectDir, string projectName)
    {
        Directory.Exists(MultiSrc(projectDir, projectName, "Api")).Should().BeTrue();
        Directory.Exists(MultiSrc(projectDir, projectName, "Domain")).Should().BeTrue();
        Directory.Exists(MultiSrc(projectDir, projectName, "Application")).Should().BeTrue();
        Directory.Exists(MultiSrc(projectDir, projectName, "Infrastructure")).Should().BeTrue();
        Directory.Exists(Path.Combine(projectDir, $"{projectName}.Api")).Should().BeFalse(
            "multi-project output must use filesystem src/, not a root {Name}.Api folder");

        var slnxPath = Path.Combine(projectDir, $"{projectName}.slnx");
        File.Exists(slnxPath).Should().BeTrue();
        var slnx = File.ReadAllText(slnxPath);
        slnx.Should().Contain($"src/{projectName}.Api/{projectName}.Api.csproj");
        slnx.Should().Contain($"src/{projectName}.Domain/{projectName}.Domain.csproj");
        slnx.Should().Contain($"src/{projectName}.Application/{projectName}.Application.csproj");
        slnx.Should().Contain($"src/{projectName}.Infrastructure/{projectName}.Infrastructure.csproj");
        slnx.Should().NotContain($"Path=\"{projectName}.Api/{projectName}.Api.csproj\"");
    }

    private string ResolveAppProjectPath(string projectDir, string projectName)
    {
        var csprojFiles = Directory.GetFiles(projectDir, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !IsInDirectory(path, "bin") && !IsInDirectory(path, "obj") && !IsInDirectory(path, "tests"))
            .ToList();

        if (!csprojFiles.Any())
        {
            throw new FileNotFoundException("No project files found.");
        }

        var namedProject = csprojFiles.FirstOrDefault(path =>
            Path.GetFileName(path).Equals($"{projectName}.Api.csproj", StringComparison.OrdinalIgnoreCase) ||
            Path.GetFileName(path).Equals($"{projectName}.csproj", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(namedProject))
        {
            return namedProject;
        }

        var webProject = csprojFiles.FirstOrDefault(path =>
            File.ReadAllText(path).Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase));

        return webProject ?? csprojFiles.First();
    }

    private static bool IsInDirectory(string path, string directoryName)
    {
        var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        var segments = path.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => segment.Equals(directoryName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task RunDotnetCommandAsync(params string[] args)
    {
        var result = await Cli.Wrap("dotnet")
            .WithArguments(args)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        if (result.ExitCode != 0)
        {
            _output.WriteLine($"Command failed: dotnet {string.Join(" ", args)}");
            _output.WriteLine(result.StandardOutput);
            _output.WriteLine(result.StandardError);
            throw new Exception($"Command failed with exit code {result.ExitCode}");
        }
    }

    private string[] BuildTemplateArgs(params string[] args)
    {
        return args.Concat(new[] { "--mcaVersion", _templateVersion, "--framework", TemplateFramework }).ToArray();
    }

    private void UpdateAppSettings(string projectDir, string projectName, string key, string value)
    {
        var searchPattern = "appsettings*.json";
        var appSettingsFiles = Directory.GetFiles(projectDir, searchPattern, SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToList();

        if (!appSettingsFiles.Any())
            throw new FileNotFoundException("No appsettings.json files found");

        foreach (var file in appSettingsFiles)
        {
            try
            {
                var json = File.ReadAllText(file);
                var jNode = JsonNode.Parse(json);
                if (jNode == null) continue;

                if (jNode["ConnectionStrings"] is JsonObject connStrings)
                {
                    connStrings[key] = value;
                    var newContent = jNode.ToString();
                    File.WriteAllText(file, newContent);
                    _output.WriteLine($"Updated {file}. Content: {newContent}");
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Failed to update {file}: {ex.Message}");
            }
        }
    }

    private Process StartApp(string projectDir, string projectName)
    {
        // We built it, now we run it from the target framework output.
        var targetFramework = ResolveTargetFramework(projectDir, projectName);
        var outputSegment = Path.Combine("bin", "Debug", targetFramework);

        // Find the .dll
        var dllFiles = Directory.GetFiles(projectDir, $"{projectName}*.dll", SearchOption.AllDirectories);
        // Filter for the one in bin/Debug/<tfm> and is the main app
        // In Multi: projectName.Api.dll
        // In Single: projectName.dll

        var mainDll = dllFiles
            .Where(f => f.Contains(outputSegment, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => new FileInfo(f).LastWriteTime)
            .FirstOrDefault(f => f.EndsWith($"{projectName}.dll") || f.EndsWith($"{projectName}.Api.dll"));

        if (mainDll == null)
             throw new FileNotFoundException("Main application DLL not found");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{mainDll}\" --urls http://localhost:{_appPort}",
            WorkingDirectory = Path.GetDirectoryName(mainDll),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            EnvironmentVariables =
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["ASPNETCORE_URLS"] = $"http://localhost:{_appPort}"
            }
        };
        _output.WriteLine($"Starting app on http://localhost:{_appPort}");

        var process = new Process { StartInfo = psi };

        process.OutputDataReceived += (s, e) => { if (e.Data != null) _output.WriteLine($"[APP]: {e.Data}"); };
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) _output.WriteLine($"[APP ERR]: {e.Data}"); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return process;
    }

    private async Task WaitForHealthCheckAsync(string url, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        HttpStatusCode? lastStatus = null;
        string? lastBody = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var response = await client.GetAsync(url, cancellationToken);
                lastStatus = response.StatusCode;
                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                lastBody = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastBody = ex.Message;
            }

            await Task.Delay(1000, cancellationToken);
        }

        throw new TimeoutException(
            $"App did not become healthy in time. url={url}, lastStatus={lastStatus}, lastBody={lastBody}");
    }
}
