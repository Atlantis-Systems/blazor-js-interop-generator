namespace BlazorJsInteropGenerator;

public class JsFunction
{
    public string Name { get; set; } = string.Empty;
    public List<JsParameter> Parameters { get; set; } = new();
    public string ReturnType { get; set; } = "void";
    public bool IsAsync { get; set; }
    public List<string> Comments { get; set; } = new();
}

public class JsParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "object";
    public bool IsOptional { get; set; }
    public string? DefaultValue { get; set; }
}

public class JsModule
{
    public string ModuleName { get; set; } = string.Empty;
    public List<JsFunction> Functions { get; set; } = new();
    public string FilePath { get; set; } = string.Empty;
}