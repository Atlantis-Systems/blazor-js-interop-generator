# Blazor JS Interop Generator

A command-line tool that automatically generates C# wrapper classes for JavaScript modules, making it easy to call JavaScript functions from Blazor applications with proper type safety.

## Features

- **Wildcard Pattern Support**: Process multiple JavaScript files using patterns like `*.js` or `**/*.js`
- **JSDoc Type Extraction**: Automatically converts JSDoc type annotations to proper C# types
- **File Monitoring**: Watch for changes and automatically regenerate C# wrappers
- **Smart Naming**: Converts JavaScript file names to proper C# class names
- **Type Safety**: Generates strongly-typed C# methods with proper parameter and return types
- **Async Support**: Handles both synchronous and asynchronous JavaScript functions

## Installation

### Install as Global .NET Tool

```bash
# Install from NuGet (when published)
dotnet tool install -g BlazorJsInteropGenerator

# Or install from local source
dotnet pack
dotnet tool install -g BlazorJsInteropGenerator --add-source ./bin/Release
```

### Install from Source

```bash
git clone <repository-url>
cd blazor-js-interop-generator/BlazorJsInteropGenerator
dotnet pack
dotnet tool install -g BlazorJsInteropGenerator --add-source ./bin/Release
```

## Usage

### Basic Usage

```bash
# Generate C# wrapper for a single JavaScript file
blazor-js-interop-generator --input example.js

# Process all JavaScript files in current directory
blazor-js-interop-generator --input "*.js"

# Process JavaScript files in subdirectories
blazor-js-interop-generator --input "src/**/*.js"
```

### Watch Mode

```bash
# Watch for changes and automatically regenerate
blazor-js-interop-generator --input "*.js" --watch
```

### Custom Namespace

```bash
# Generate wrappers with custom namespace
blazor-js-interop-generator --input "*.js" --namespace "MyApp.JsInterop"
```

### Alternative: Run from Source

If you haven't installed the tool globally, you can still run it from source:

```bash
# From the project directory
dotnet run -- --input "*.js"
```

### Command Line Options

- `--input, -i`: Input JavaScript file pattern (required)
- `--watch, -w`: Watch for file changes and regenerate automatically
- `--namespace, -n`: C# namespace for generated wrappers (default: "BlazorApp.JsInterop")

## JavaScript Requirements

For best results, use JSDoc comments to specify parameter and return types:

```javascript
/**
 * Adds two numbers together
 * @param {number} a - First number
 * @param {number} b - Second number
 * @returns {number} Sum of a and b
 */
export function add(a, b) {
    return a + b;
}

/**
 * Fetches user data asynchronously
 * @param {string} userId - User ID to fetch
 * @returns {Promise<object>} User data object
 */
export async function fetchUser(userId) {
    const response = await fetch(`/api/users/${userId}`);
    return response.json();
}
```

## Type Mapping

The generator maps JavaScript types to C# types as follows:

| JavaScript Type | C# Type |
|----------------|---------|
| `number` | `double` |
| `string` | `string` |
| `boolean` | `bool` |
| `object` | `object` |
| `array` | `object[]` |
| `void` | `void` |
| `Promise<T>` | `Task<T>` |
| `Promise` | `Task` |

## Generated Output

For a JavaScript file `math-utils.js`:

```javascript
/**
 * Multiplies two numbers
 * @param {number} x - First number
 * @param {number} y - Second number
 * @returns {number} Product of x and y
 */
export function multiply(x, y) {
    return x * y;
}
```

The generator creates `math-utils.cs`:

```csharp
using Microsoft.JSInterop;

namespace BlazorApp.JsInterop;

public class MathUtilsJsInterop
{
    private readonly IJSRuntime _jsRuntime;
    private readonly string _modulePath;

    public MathUtilsJsInterop(IJSRuntime jsRuntime, string modulePath = "./math-utils.js")
    {
        _jsRuntime = jsRuntime;
        _modulePath = modulePath;
    }

    /// <summary>
    /// Multiplies two numbers
    /// @param {number} x - First number
    /// @param {number} y - Second number
    /// @returns {number} Product of x and y
    /// </summary>
    /// <param name="x">JavaScript parameter of type double</param>
    /// <param name="y">JavaScript parameter of type double</param>
    public async Task<double> MultiplyAsync(double x, double y)
    {
        return await _jsRuntime.InvokeAsync<double>("multiply", _modulePath, x, y);
    }
}
```

## Using Generated Wrappers in Blazor

1. **Register the service** in your `Program.cs`:

```csharp
builder.Services.AddScoped<MathUtilsJsInterop>();
```

2. **Inject and use** in your Blazor component:

```csharp
@page "/calculator"
@inject MathUtilsJsInterop MathUtils

<h3>Calculator</h3>

<input @bind="numberA" type="number" />
<input @bind="numberB" type="number" />
<button @onclick="Calculate">Calculate</button>

<p>Result: @result</p>

@code {
    private double numberA;
    private double numberB;
    private double result;

    private async Task Calculate()
    {
        result = await MathUtils.MultiplyAsync(numberA, numberB);
    }
}
```

## Supported JavaScript Patterns

The generator recognizes these JavaScript function patterns:

### Function Declarations
```javascript
export function myFunction(param1, param2) { }
export async function myAsyncFunction(param) { }
```

### Arrow Functions
```javascript
export const myArrowFunction = (param1, param2) => { };
export const myAsyncArrow = async (param) => { };
```

### Optional Parameters
```javascript
export function greet(name, title = "Mr.") { }
// Generates: GreetAsync(string name, string? title = "Mr.")
```

### TypeScript Annotations
```javascript
export function calculate(x: number, y: number): number { }
// Also supported alongside JSDoc
```

## File Organization

The generator creates C# files alongside their JavaScript counterparts:

```
src/
├── utils/
│   ├── math-utils.js
│   ├── math-utils.cs        ← Generated
│   ├── string-helpers.js
│   └── string-helpers.cs    ← Generated
└── components/
    ├── chart.js
    └── chart.cs             ← Generated
```

## Watch Mode

When using `--watch`, the generator:

1. Generates initial C# wrappers for all matching files
2. Monitors the file system for changes
3. Automatically regenerates wrappers when JavaScript files are modified
4. Continues running until you press `Ctrl+C`

## Examples

### Basic Math Library

JavaScript (`math.js`):
```javascript
/**
 * @param {number} a
 * @param {number} b
 * @returns {number}
 */
export function add(a, b) { return a + b; }

/**
 * @param {number} n
 * @returns {Promise<number>}
 */
export async function factorial(n) {
    if (n <= 1) return 1;
    return n * await factorial(n - 1);
}
```

Generated C# (`math.cs`):
```csharp
public async Task<double> AddAsync(double a, double b)
public async Task<double> FactorialAsync(double n)
```

### String Utilities

JavaScript (`string-utils.js`):
```javascript
/**
 * @param {string} str
 * @returns {boolean}
 */
export function isEmpty(str) { return !str || str.length === 0; }

/**
 * @param {string} text
 * @param {string} search
 * @param {string} replace
 * @returns {string}
 */
export function replaceAll(text, search, replace) {
    return text.split(search).join(replace);
}
```

Generated C# (`string-utils.cs`):
```csharp
public async Task<bool> IsEmptyAsync(string str)
public async Task<string> ReplaceAllAsync(string text, string search, string replace)
```

## Tool Management

### Update Tool

```bash
# Update to latest version
dotnet tool update -g BlazorJsInteropGenerator
```

### Uninstall Tool

```bash
# Remove the global tool
dotnet tool uninstall -g BlazorJsInteropGenerator
```

### List Installed Tools

```bash
# See all installed .NET tools
dotnet tool list -g
```

## Troubleshooting

### Common Issues

1. **Tool not found**: Ensure the tool is installed globally with `dotnet tool list -g`
2. **No files found**: Ensure your wildcard pattern matches existing files
3. **Build errors**: Check that generated C# files don't have naming conflicts
4. **Type mismatches**: Verify JSDoc annotations are properly formatted
5. **Permission errors**: Make sure you have write permissions in the target directory

### Debug Tips

- Use specific file paths first to test individual files
- Check the console output for parsing information
- Verify that JavaScript files are properly formatted and exportable
- Use `--help` to see all available options: `blazor-js-interop-generator --help`

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.