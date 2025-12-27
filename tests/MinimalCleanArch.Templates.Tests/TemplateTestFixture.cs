using System.Xml.Linq;
using CliWrap;

namespace MinimalCleanArch.Templates.Tests;

public class TemplateTestFixture : IAsyncLifetime
{
    private static readonly string TemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "../../../../../templates/mca");
    private static readonly string TemplatesCsprojPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../../../templates/MinimalCleanArch.Templates.csproj");

    public string TemplatePackagePath { get; }
    public string TemplateVersion { get; }
    public string PackageSource { get; }

    public TemplateTestFixture()
    {
        var (packagePath, version) = ResolveTemplatePackage();
        TemplatePackagePath = packagePath;
        TemplateVersion = version;
        PackageSource = Directory.Exists(packagePath) ? packagePath : Path.GetDirectoryName(packagePath)!;
    }

    public async Task InitializeAsync()
    {
        // Uninstall any existing version to ensure we use the local one
        await Cli.Wrap("dotnet")
            .WithArguments(new[] { "new", "uninstall", "MinimalCleanArch.Templates" })
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        // Clear the template cache
        await Cli.Wrap("dotnet")
            .WithArguments(new[] { "new", "--debug:reinit" })
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        // Install the template from the local path
        await Cli.Wrap("dotnet")
            .WithArguments(new[] { "new", "install", TemplatePackagePath, "--force" })
            .ExecuteAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private static (string packagePath, string version) ResolveTemplatePackage()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../../../../"));
        var candidateDirs = new[]
        {
            Path.Combine(repoRoot, "artifacts", "packages"),
            Path.Combine(repoRoot, "artifacts", "nuget")
        };

        var packagePath = candidateDirs
            .Where(Directory.Exists)
            .SelectMany(dir => Directory.GetFiles(dir, "MinimalCleanArch.Templates*.nupkg", SearchOption.TopDirectoryOnly)
                .Where(file => !file.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        var version = ExtractVersion(packagePath) ?? ReadVersionFromCsproj() ?? "0.1.11-preview";

        return (packagePath ?? TemplatePath, version);
    }

    private static string? ExtractVersion(string? packagePath)
    {
        if (string.IsNullOrWhiteSpace(packagePath))
        {
            return null;
        }

        var fileName = Path.GetFileNameWithoutExtension(packagePath);
        const string prefix = "MinimalCleanArch.Templates.";
        if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return fileName[prefix.Length..];
    }

    private static string? ReadVersionFromCsproj()
    {
        if (!File.Exists(TemplatesCsprojPath))
        {
            return null;
        }

        var doc = XDocument.Load(TemplatesCsprojPath);
        var versionElement = doc.Descendants("Version").FirstOrDefault();
        return versionElement?.Value?.Trim();
    }
}
