using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using CliWrap;
using CliWrap.Buffered;
using Xunit.Abstractions;

namespace MinimalCleanArch.Templates.Tests;

public class TemplateIntegrationTests : IClassFixture<TemplateTestFixture>
{
    private readonly ITestOutputHelper _output;
    private readonly string _testRunId;
    private readonly string _baseOutputDir;

    public TemplateIntegrationTests(TemplateTestFixture fixture, ITestOutputHelper output)
    {
        _output = output;
        _testRunId = Guid.NewGuid().ToString("N").Substring(0, 8);
        _baseOutputDir = Path.Combine(Path.GetTempPath(), "MCA_Tests", _testRunId);
        Directory.CreateDirectory(_baseOutputDir);
    }

    private void CreateNugetConfig(string projectDir)
    {
        Directory.CreateDirectory(projectDir);
        var localPackageSource = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../../../../artifacts/local"));
        var globalPackages = Path.Combine(projectDir, ".packages");

        var nugetConfigContent = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                <packageSources>
                    <add key="local" value="{localPackageSource}" />
                </packageSources>
                <config>
                    <add key="globalPackagesFolder" value="{globalPackages}" />
                </config>
                </configuration>
                """;

        File.WriteAllText(Path.Combine(projectDir, "nuget.config"), nugetConfigContent);
        _output.WriteLine($"Created nuget.config pointing to {localPackageSource} and global packages at {globalPackages}");
    }

    [Fact]
    public async Task Create_Build_Run_SqlServer_Project()
    {
        var projectName = "TestAppSql";
        var projectDir = Path.Combine(_baseOutputDir, projectName);

        // 1. Start SQL Server Container
        _output.WriteLine("Starting SQL Server...");
        var sqlContainer = new MsSqlBuilder()
            .WithPassword("Pass@word1")
            .Build();

        await sqlContainer.StartAsync();

        try
        {
            // 2. Generate
            _output.WriteLine("Generating project...");
            CreateNugetConfig(projectDir);
            await RunDotnetCommandAsync("new", "mca", "-n", projectName, "-o", projectDir, "--db", "sqlserver", "--all");

            // 3. Update config (Source)
            var connectionString = $"Data Source={sqlContainer.Hostname},{sqlContainer.GetMappedPublicPort(1433)};Database=master;User Id=sa;Password=Pass@word1;TrustServerCertificate=True";
            _output.WriteLine($"SQL Connection String: {connectionString}");
            UpdateAppSettings(projectDir, projectName, "DefaultConnection", connectionString);

            // 4. Build
            _output.WriteLine("Building project...");
            await RunDotnetCommandAsync("build", projectDir, "/nodeReuse:false");

            // 5. Run App
            _output.WriteLine("Running app...");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            using var appProcess = StartApp(projectDir, projectName);

            try
            {
                // 6. Verify Health
                await WaitForHealthCheckAsync("http://localhost:5000/health", cts.Token);
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

        // 1. Start Postgres Container
        _output.WriteLine("Starting Postgres...");
        var pgContainer = new PostgreSqlBuilder()
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithDatabase("mca_test")
            .Build();

        await pgContainer.StartAsync();

        try
        {
            // 2. Generate
            _output.WriteLine("Generating project...");
            CreateNugetConfig(projectDir);
            await RunDotnetCommandAsync("new", "mca", "-n", projectName, "-o", projectDir, "--db", "postgres", "--all");

            // 3. Update config (Source)
            var connectionString = $"Server={pgContainer.Hostname};Port={pgContainer.GetMappedPublicPort(5432)};Database=mca_test;User Id=postgres;Password=postgres";
            _output.WriteLine($"PG Connection String: {connectionString}");
            UpdateAppSettings(projectDir, projectName, "DefaultConnection", connectionString);

            // 4. Build
            _output.WriteLine("Building project...");
            await RunDotnetCommandAsync("build", projectDir, "/nodeReuse:false");

            // 5. Run App
            _output.WriteLine("Running app...");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            using var appProcess = StartApp(projectDir, projectName);

            try
            {
                // 6. Verify Health
                await WaitForHealthCheckAsync("http://localhost:5000/health", cts.Token);
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

        // 1. Generate (Sqlite is default, but we can specify it)
        _output.WriteLine("Generating project...");
        CreateNugetConfig(projectDir);
        // Use --healthchecks to ensure we have an endpoint to test, but avoid --recommended which might add Redis
        await RunDotnetCommandAsync("new", "mca", "-n", projectName, "-o", projectDir, "--db", "sqlite", "--healthchecks");

        // 2. Build
        _output.WriteLine("Building project...");
        await RunDotnetCommandAsync("build", projectDir, "/nodeReuse:false");

        // 3. Run App
        _output.WriteLine("Running app...");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var appProcess = StartApp(projectDir, projectName);

        try
        {
            // 4. Verify Health
            await WaitForHealthCheckAsync("http://localhost:5000/health", cts.Token);
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
        var redisContainer = new RedisBuilder().Build();
        await redisContainer.StartAsync();

        try
        {
            // 2. Generate
            _output.WriteLine("Generating project...");
            CreateNugetConfig(projectDir);
            // Ensure healthchecks are enabled so we can verify startup
            await RunDotnetCommandAsync("new", "mca", "-n", projectName, "-o", projectDir, "--caching", "--healthchecks");

            // 3. Update config (Source)
            var connectionString = redisContainer.GetConnectionString();
            _output.WriteLine($"Redis Connection String: {connectionString}");
            UpdateAppSettings(projectDir, projectName, "Redis", connectionString);

            // 4. Build
            _output.WriteLine("Building project...");
            await RunDotnetCommandAsync("build", projectDir, "/nodeReuse:false");

            // 5. Run App
            _output.WriteLine("Running app...");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            using var appProcess = StartApp(projectDir, projectName);

            try
            {
                // 6. Verify Health
                await WaitForHealthCheckAsync("http://localhost:5000/health", cts.Token);
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

    // Helpers

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
        // We built it, now we run it.
        // Ideally we run the dll from bin/Debug/net9.0
        // Find the .dll
        var dllFiles = Directory.GetFiles(projectDir, $"{projectName}*.dll", SearchOption.AllDirectories);
        // Filter for the one in bin/Debug/net9.0 and is the main app
        // In Multi: projectName.Api.dll
        // In Single: projectName.dll
        
        var mainDll = dllFiles
            .Where(f => f.Contains("bin") && f.Contains("Debug") && f.Contains("net9.0"))
            .OrderByDescending(f => new FileInfo(f).LastWriteTime)
            .FirstOrDefault(f => f.EndsWith($"{projectName}.dll") || f.EndsWith($"{projectName}.Api.dll"));

        if (mainDll == null)
             throw new FileNotFoundException("Main application DLL not found");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{mainDll}\" --urls http://localhost:5000",
            WorkingDirectory = Path.GetDirectoryName(mainDll),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            EnvironmentVariables = { ["ASPNETCORE_ENVIRONMENT"] = "Development" }
        };

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
        using var client = new HttpClient();
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var response = await client.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // Ignore and retry
            }

            await Task.Delay(1000, cancellationToken);
        }

        throw new TimeoutException("App did not become healthy in time.");
    }
}
