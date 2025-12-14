using CliWrap;

namespace MinimalCleanArch.Templates.Tests;

public class TemplateTestFixture : IAsyncLifetime
{
    private static readonly string TemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "../../../../../templates/mca");

    public async Task InitializeAsync()
    {
        // Uninstall any existing version to ensure we use the local one
        await Cli.Wrap("dotnet")
            .WithArguments(new[] { "new", "uninstall", "MinimalCleanArch.Templates" })
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        // Install the template from the local path
        await Cli.Wrap("dotnet")
            .WithArguments(new[] { "new", "install", TemplatePath, "--force" })
            .ExecuteAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
