using System.Text.RegularExpressions;

namespace BlazorJsInteropGenerator;

public class JsParser
{
    private static readonly Regex FunctionRegex = new(@"(?:export\s+)?(?:async\s+)?function\s+(\w+)\s*\(([^)]*)\)\s*(?::\s*([^{]+))?\s*{", RegexOptions.Multiline);
    private static readonly Regex ArrowFunctionRegex = new(@"(?:export\s+)?(?:const|let|var)\s+(\w+)\s*=\s*(?:async\s+)?\(([^)]*)\)\s*(?::\s*([^=]+))?\s*=>\s*{?", RegexOptions.Multiline);
    private static readonly Regex CommentRegex = new(@"\/\*\*(.*?)\*\/|\/\/(.*)$", RegexOptions.Multiline | RegexOptions.Singleline);
    private static readonly Regex JSDocParamRegex = new(@"@param\s+\{([^}]+)\}\s+(\w+)", RegexOptions.Multiline);
    private static readonly Regex JSDocReturnRegex = new(@"@returns?\s+\{([^}]+)\}", RegexOptions.Multiline);

    public JsModule ParseFile(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var moduleName = Path.GetFileNameWithoutExtension(filePath).ToPascalCase();
        
        var module = new JsModule
        {
            ModuleName = moduleName,
            FilePath = filePath
        };

        var functions = new List<JsFunction>();
        
        // Parse regular functions
        var functionMatches = FunctionRegex.Matches(content);
        foreach (Match match in functionMatches)
        {
            var function = ParseFunction(match, content);
            if (function != null)
                functions.Add(function);
        }

        // Parse arrow functions
        var arrowMatches = ArrowFunctionRegex.Matches(content);
        foreach (Match match in arrowMatches)
        {
            var function = ParseArrowFunction(match, content);
            if (function != null)
                functions.Add(function);
        }

        module.Functions = functions;
        return module;
    }

    private JsFunction? ParseFunction(Match match, string content)
    {
        var name = match.Groups[1].Value;
        var parameters = match.Groups[2].Value;
        var returnType = match.Groups[3].Value.Trim();

        var function = new JsFunction
        {
            Name = name,
            ReturnType = MapJsTypeToCSharp(returnType),
            IsAsync = content.Substring(Math.Max(0, match.Index - 50), Math.Min(50, content.Length - match.Index)).Contains("async")
        };

        var jsdocInfo = ExtractJSDocInfo(content, match.Index);
        function.Parameters = ParseParameters(parameters, jsdocInfo);
        function.Comments = ExtractComments(content, match.Index);
        
        // Validate JSDoc is present
        if (!HasValidJSDoc(jsdocInfo, function.Comments))
        {
            Console.Error.WriteLine($"ERROR: Function '{name}' is missing required JSDoc comments. JSDoc documentation is required for all functions.");
            return null;
        }
        
        if (string.IsNullOrEmpty(returnType) && !string.IsNullOrEmpty(jsdocInfo.ReturnType))
        {
            function.ReturnType = MapJsTypeToCSharp(jsdocInfo.ReturnType);
        }

        return function;
    }

    private JsFunction? ParseArrowFunction(Match match, string content)
    {
        var name = match.Groups[1].Value;
        var parameters = match.Groups[2].Value;
        var returnType = match.Groups[3].Value.Trim();

        var function = new JsFunction
        {
            Name = name,
            ReturnType = MapJsTypeToCSharp(returnType),
            IsAsync = content.Substring(Math.Max(0, match.Index - 50), Math.Min(50, content.Length - match.Index)).Contains("async")
        };

        var jsdocInfo = ExtractJSDocInfo(content, match.Index);
        function.Parameters = ParseParameters(parameters, jsdocInfo);
        function.Comments = ExtractComments(content, match.Index);
        
        // Validate JSDoc is present
        if (!HasValidJSDoc(jsdocInfo, function.Comments))
        {
            Console.Error.WriteLine($"ERROR: Function '{name}' is missing required JSDoc comments. JSDoc documentation is required for all functions.");
            return null;
        }
        
        if (string.IsNullOrEmpty(returnType) && !string.IsNullOrEmpty(jsdocInfo.ReturnType))
        {
            function.ReturnType = MapJsTypeToCSharp(jsdocInfo.ReturnType);
        }

        return function;
    }

    private List<JsParameter> ParseParameters(string parametersString, JSDocInfo? jsdocInfo = null)
    {
        var parameters = new List<JsParameter>();
        
        if (string.IsNullOrWhiteSpace(parametersString))
            return parameters;

        var paramParts = parametersString.Split(',');
        
        foreach (var part in paramParts)
        {
            var trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            var parameter = new JsParameter();
            
            // Handle optional parameters
            if (trimmed.Contains('?'))
            {
                parameter.IsOptional = true;
                trimmed = trimmed.Replace("?", "");
            }

            // Handle default values
            if (trimmed.Contains('='))
            {
                var defaultSplit = trimmed.Split('=');
                trimmed = defaultSplit[0].Trim();
                parameter.DefaultValue = defaultSplit[1].Trim();
                parameter.IsOptional = true;
            }

            // Handle type annotations
            if (trimmed.Contains(':'))
            {
                var typeSplit = trimmed.Split(':');
                parameter.Name = typeSplit[0].Trim();
                parameter.Type = MapJsTypeToCSharp(typeSplit[1].Trim());
            }
            else
            {
                parameter.Name = trimmed;
                // Try to get type from JSDoc
                var jsdocType = jsdocInfo?.ParameterTypes.GetValueOrDefault(parameter.Name);
                parameter.Type = !string.IsNullOrEmpty(jsdocType) ? MapJsTypeToCSharp(jsdocType) : "object";
            }

            parameters.Add(parameter);
        }

        return parameters;
    }

    private string MapJsTypeToCSharp(string jsType)
    {
        // Handle Promise types
        if (jsType.StartsWith("Promise<") && jsType.EndsWith(">"))
        {
            var innerType = jsType.Substring(8, jsType.Length - 9);
            var mappedInnerType = MapJsTypeToCSharp(innerType);
            return mappedInnerType == "void" ? "Task" : $"Task<{mappedInnerType}>";
        }
        
        return jsType.ToLower() switch
        {
            "string" => "string",
            "number" => "double",
            "boolean" => "bool",
            "object" => "object",
            "array" => "object[]",
            "void" => "void",
            "promise" => "Task",
            "" => "void",
            _ => "object"
        };
    }

    private List<string> ExtractComments(string content, int functionIndex)
    {
        var comments = new List<string>();
        
        // Look for comments above the function
        var lines = content.Substring(0, functionIndex).Split('\n');
        var relevantLines = lines.TakeLast(10).Reverse();
        
        foreach (var line in relevantLines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("//"))
            {
                comments.Insert(0, trimmed.Substring(2).Trim());
            }
            else if (trimmed.StartsWith("/*") || trimmed.StartsWith("*"))
            {
                comments.Insert(0, trimmed.TrimStart('/', '*').Trim());
            }
            else if (!string.IsNullOrWhiteSpace(trimmed))
            {
                break;
            }
        }

        return comments;
    }

    private JSDocInfo ExtractJSDocInfo(string content, int functionIndex)
    {
        var jsdocInfo = new JSDocInfo();
        
        // Look for JSDoc comment block above the function
        var lines = content.Substring(0, functionIndex).Split('\n');
        var jsdocLines = new List<string>();
        
        // Find the JSDoc block (starts with /** and ends with */)
        bool inJSDoc = false;
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            var line = lines[i].Trim();
            
            if (line.EndsWith("*/"))
            {
                inJSDoc = true;
                jsdocLines.Add(line);
            }
            else if (inJSDoc)
            {
                jsdocLines.Add(line);
                if (line.StartsWith("/**"))
                {
                    break;
                }
            }
            else if (!string.IsNullOrWhiteSpace(line))
            {
                break;
            }
        }
        
        if (jsdocLines.Count == 0) return jsdocInfo;
        
        // Combine all JSDoc lines
        var jsdocContent = string.Join("\n", jsdocLines.AsEnumerable().Reverse());
        
        // Parse @param tags
        var paramMatches = JSDocParamRegex.Matches(jsdocContent);
        foreach (Match match in paramMatches)
        {
            var type = match.Groups[1].Value;
            var name = match.Groups[2].Value;
            jsdocInfo.ParameterTypes[name] = type;
        }
        
        // Parse @returns tag
        var returnMatch = JSDocReturnRegex.Match(jsdocContent);
        if (returnMatch.Success)
        {
            jsdocInfo.ReturnType = returnMatch.Groups[1].Value;
        }
        
        return jsdocInfo;
    }

    private bool HasValidJSDoc(JSDocInfo jsdocInfo, List<string> comments)
    {
        // Only accept proper JSDoc with @param, @returns, or @description tags
        return jsdocInfo.ParameterTypes.Any() || !string.IsNullOrEmpty(jsdocInfo.ReturnType);
    }
}

public class JSDocInfo
{
    public Dictionary<string, string> ParameterTypes { get; set; } = new();
    public string ReturnType { get; set; } = string.Empty;
}

public static class StringExtensions
{
    public static string ToPascalCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
            
        // Replace hyphens and underscores with spaces, then convert to PascalCase
        var cleaned = input.Replace("-", " ").Replace("_", " ");
        var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var result = string.Join("", words.Select(word => 
            char.ToUpper(word[0]) + word.Substring(1).ToLower()));
            
        return result;
    }
}