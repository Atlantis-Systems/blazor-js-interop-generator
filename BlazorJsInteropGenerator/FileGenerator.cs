using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace BlazorJsInteropGenerator;

public class FileGenerator
{
    private readonly string _namespaceName;
    private readonly JsParser _parser;
    private readonly CSharpGenerator _csharpGenerator;

    public FileGenerator(string namespaceName)
    {
        _namespaceName = namespaceName;
        _parser = new JsParser();
        _csharpGenerator = new CSharpGenerator();
    }

    public async Task GenerateAsync(string pattern)
    {
        var files = GetMatchingFiles(pattern);
        
        if (!files.Any())
        {
            Console.WriteLine($"No files found matching pattern: {pattern}");
            return;
        }

        Console.WriteLine($"Found {files.Count()} JavaScript files to process:");
        
        foreach (var file in files)
        {
            await GenerateForFileAsync(file);
        }
        
        Console.WriteLine("Generation completed successfully!");
    }

    public async Task WatchAndGenerateAsync(string pattern)
    {
        var files = GetMatchingFiles(pattern);
        
        if (!files.Any())
        {
            Console.WriteLine($"No files found matching pattern: {pattern}");
            return;
        }

        Console.WriteLine($"Watching {files.Count()} JavaScript files for changes...");
        Console.WriteLine("Press Ctrl+C to stop watching.");

        // Generate initial files
        foreach (var file in files)
        {
            await GenerateForFileAsync(file);
        }

        // Set up file watchers
        var watchers = new List<FileSystemWatcher>();
        var watchedDirectories = files.Select(Path.GetDirectoryName).Distinct().Where(d => !string.IsNullOrEmpty(d));

        foreach (var directory in watchedDirectories)
        {
            var watcher = new FileSystemWatcher(directory!, "*.js")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            watcher.Changed += async (sender, e) => await OnFileChanged(e.FullPath, pattern);
            watcher.Created += async (sender, e) => await OnFileChanged(e.FullPath, pattern);
            watcher.Renamed += async (sender, e) => await OnFileChanged(e.FullPath, pattern);
            
            watchers.Add(watcher);
        }

        // Keep the application running
        var tcs = new TaskCompletionSource<bool>();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            tcs.SetResult(true);
        };

        await tcs.Task;

        // Cleanup watchers
        foreach (var watcher in watchers)
        {
            watcher.Dispose();
        }
        
        Console.WriteLine("\nStopped watching files.");
    }

    private async Task OnFileChanged(string filePath, string pattern)
    {
        // Check if the changed file matches our pattern
        var files = GetMatchingFiles(pattern);
        if (files.Contains(filePath))
        {
            Console.WriteLine($"File changed: {filePath}");
            await GenerateForFileAsync(filePath);
        }
    }

    private async Task GenerateForFileAsync(string jsFilePath)
    {
        try
        {
            Console.WriteLine($"Processing: {jsFilePath}");
            
            var module = _parser.ParseFile(jsFilePath);
            Console.WriteLine($"  Found {module.Functions.Count} functions in '{module.ModuleName}'");
            
            var csharpCode = _csharpGenerator.GenerateWrapper(module, _namespaceName);
            
            // Generate C# file next to JavaScript file
            var outputPath = Path.ChangeExtension(jsFilePath, ".cs");
            await File.WriteAllTextAsync(outputPath, csharpCode);
            
            Console.WriteLine($"  Generated: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error processing {jsFilePath}: {ex.Message}");
        }
    }

    private IEnumerable<string> GetMatchingFiles(string pattern)
    {
        var matcher = new Matcher();
        
        // Handle absolute paths and relative paths
        string searchDirectory;
        string searchPattern;
        
        if (Path.IsPathRooted(pattern))
        {
            searchDirectory = Path.GetDirectoryName(pattern) ?? "/";
            searchPattern = Path.GetFileName(pattern);
        }
        else
        {
            searchDirectory = Directory.GetCurrentDirectory();
            searchPattern = pattern;
        }
        
        // If pattern contains directory separators, handle it properly
        if (searchPattern.Contains('/') || searchPattern.Contains('\\'))
        {
            matcher.AddInclude(searchPattern);
        }
        else
        {
            matcher.AddInclude(searchPattern);
        }
        
        var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(searchDirectory)));
        
        return result.Files.Select(f => Path.Combine(searchDirectory, f.Path));
    }
}