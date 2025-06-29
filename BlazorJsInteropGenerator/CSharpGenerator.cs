using System.Text;

namespace BlazorJsInteropGenerator;

public class CSharpGenerator
{
    public string GenerateWrapper(JsModule module, string namespaceName = "BlazorApp.JsInterop")
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("using Microsoft.JSInterop;");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();
        sb.AppendLine($"public class {module.ModuleName}JsInterop");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly IJSRuntime _jsRuntime;");
        sb.AppendLine($"    private readonly string _modulePath;");
        sb.AppendLine();
        sb.AppendLine($"    public {module.ModuleName}JsInterop(IJSRuntime jsRuntime, string modulePath = \"./{Path.GetFileName(module.FilePath)}\")");
        sb.AppendLine("    {");
        sb.AppendLine("        _jsRuntime = jsRuntime;");
        sb.AppendLine("        _modulePath = modulePath;");
        sb.AppendLine("    }");
        sb.AppendLine();

        foreach (var function in module.Functions)
        {
            GenerateMethod(sb, function);
            sb.AppendLine();
        }

        sb.AppendLine("}");
        
        return sb.ToString();
    }

    private void GenerateMethod(StringBuilder sb, JsFunction function)
    {
        // Add XML documentation comments
        if (function.Comments.Any())
        {
            sb.AppendLine("    /// <summary>");
            foreach (var comment in function.Comments)
            {
                sb.AppendLine($"    /// {comment}");
            }
            sb.AppendLine("    /// </summary>");
        }

        // Add parameter documentation
        foreach (var param in function.Parameters)
        {
            sb.AppendLine($"    /// <param name=\"{param.Name}\">JavaScript parameter of type {param.Type}</param>");
        }

        // Determine return type and method signature
        var returnType = function.ReturnType;
        var isVoid = returnType == "void";
        var methodPrefix = isVoid ? "async Task" : $"async Task<{returnType}>";

        // Generate method signature
        var parameters = string.Join(", ", function.Parameters.Select(p => 
        {
            var paramType = p.IsOptional && !p.Type.EndsWith("?") ? $"{p.Type}?" : p.Type;
            var defaultValue = p.IsOptional && p.DefaultValue != null ? $" = {ConvertDefaultValue(p.DefaultValue, p.Type)}" : 
                              p.IsOptional ? " = null" : "";
            return $"{paramType} {p.Name}{defaultValue}";
        }));

        sb.AppendLine($"    public {methodPrefix} {function.Name.ToPascalCase()}Async({parameters})");
        sb.AppendLine("    {");

        // Generate method body
        var jsParameters = function.Parameters.Select(p => p.Name).ToList();
        var parametersList = jsParameters.Any() ? $", {string.Join(", ", jsParameters)}" : "";

        if (isVoid)
        {
            sb.AppendLine($"        await _jsRuntime.InvokeVoidAsync(\"{function.Name}\", _modulePath{parametersList});");
        }
        else
        {
            sb.AppendLine($"        return await _jsRuntime.InvokeAsync<{returnType}>(\"{function.Name}\", _modulePath{parametersList});");
        }

        sb.AppendLine("    }");
    }

    private string ConvertDefaultValue(string jsDefault, string csharpType)
    {
        return jsDefault switch
        {
            "true" => "true",
            "false" => "false",
            "null" => "null",
            "undefined" => "null",
            _ when jsDefault.StartsWith("\"") || jsDefault.StartsWith("'") => jsDefault.Replace("'", "\""),
            _ when double.TryParse(jsDefault, out _) => jsDefault,
            _ => "null"
        };
    }
}