[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class ShaderAttribute : Attribute
{
    public string? VertexShaderName { get; private set; }
    public string? FragmentShaderName { get; private set; }
    public string? ComputeShaderName { get; private set; }

    public ShaderAttribute(string computeShaderName)
    {
        ComputeShaderName = computeShaderName;
    }

    public ShaderAttribute(string vertexShaderName, string fragmentShaderName)
    {
        VertexShaderName = vertexShaderName;
        FragmentShaderName = fragmentShaderName;
    }
}