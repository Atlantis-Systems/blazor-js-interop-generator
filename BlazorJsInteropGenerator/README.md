# Blazor JS Interop Generator

A command-line tool that automatically generates C# wrapper classes for JavaScript modules, making it easy to call JavaScript functions from Blazor applications using IJSRuntime.

## Features

- Parses JavaScript files to extract function signatures
- Generates strongly-typed C# wrapper classes
- Supports both regular functions and arrow functions
- Handles async/await patterns
- Maps JavaScript types to C# types
- Preserves JSDoc comments as XML documentation
- Supports optional parameters and default values

## Usage

```bash
dotnet run -- --input script.js --output ScriptJsInterop.cs --namespace MyApp.JsInterop
```

### Options

- `--input, -i`: Input JavaScript file (required)
- `--output, -o`: Output C# file path (optional, defaults to input filename with .cs extension)
- `--namespace, -n`: C# namespace (optional, defaults to "BlazorApp.JsInterop")

## Example

Given a JavaScript file `mathUtils.js`:

```javascript
// Adds two numbers
export function add(a, b) {
    return a + b;
}

// Calculates factorial asynchronously
export async function factorial(n = 1) {
    if (n <= 1) return 1;
    return n * await factorial(n - 1);
}

// Formats a number as currency
export const formatCurrency = (amount, currency = 'USD') => {
    return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: currency
    }).format(amount);
};
```

The generator creates:

```csharp
using Microsoft.JSInterop;

namespace BlazorApp.JsInterop;

public class MathUtilsJsInterop
{
    private readonly IJSRuntime _jsRuntime;
    private readonly string _modulePath;

    public MathUtilsJsInterop(IJSRuntime jsRuntime, string modulePath = "./mathUtils.js")
    {
        _jsRuntime = jsRuntime;
        _modulePath = modulePath;
    }

    /// <summary>
    /// Adds two numbers
    /// </summary>
    public async Task<object> AddAsync(object a, object b)
    {
        return await _jsRuntime.InvokeAsync<object>("add", _modulePath, a, b);
    }

    /// <summary>
    /// Calculates factorial asynchronously
    /// </summary>
    public async Task<object> FactorialAsync(object? n = null)
    {
        return await _jsRuntime.InvokeAsync<object>("factorial", _modulePath, n);
    }

    /// <summary>
    /// Formats a number as currency
    /// </summary>
    public async Task<object> FormatCurrencyAsync(object amount, object? currency = null)
    {
        return await _jsRuntime.InvokeAsync<object>("formatCurrency", _modulePath, amount, currency);
    }
}
```

## Integration in Blazor

1. Register the wrapper in your DI container:

```csharp
builder.Services.AddScoped<MathUtilsJsInterop>();
```

2. Use in your Blazor components:

```csharp
@inject MathUtilsJsInterop MathUtils

@code {
    private async Task CalculateSum()
    {
        var result = await MathUtils.AddAsync(5, 3);
        // Use result...
    }
}
```

## Building

```bash
dotnet build
```

## Type Mapping

| JavaScript Type | C# Type |
|----------------|---------|
| string         | string  |
| number         | double  |
| boolean        | bool    |
| object         | object  |
| array          | object[]|
| void           | void    |
| Promise        | Task    |