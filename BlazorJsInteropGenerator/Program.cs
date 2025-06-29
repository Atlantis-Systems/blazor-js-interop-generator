using System.CommandLine;
using Microsoft.Extensions.FileSystemGlobbing;
using BlazorJsInteropGenerator;

var inputOption = new Option<string>(
    aliases: ["--input", "-i"],
    description: "Input JavaScript file pattern (supports wildcards like '**/*.js')")
{
    IsRequired = true
};

var watchOption = new Option<bool>(
    aliases: ["--watch", "-w"],
    description: "Watch for file changes and regenerate automatically",
    getDefaultValue: () => false);

var namespaceOption = new Option<string>(
    aliases: ["--namespace", "-n"],
    description: "C# namespace for the generated wrapper",
    getDefaultValue: () => "BlazorApp.JsInterop");

var rootCommand = new RootCommand("Blazor JS Interop Generator - Generate C# wrappers for JavaScript modules")
{
    inputOption,
    watchOption,
    namespaceOption
};

rootCommand.SetHandler(async (string inputPattern, bool watch, string namespaceName) =>
{
    try
    {
        var generator = new FileGenerator(namespaceName);
        
        if (watch)
        {
            await generator.WatchAndGenerateAsync(inputPattern);
        }
        else
        {
            await generator.GenerateAsync(inputPattern);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        Environment.Exit(1);
    }
}, inputOption, watchOption, namespaceOption);

await rootCommand.InvokeAsync(args);