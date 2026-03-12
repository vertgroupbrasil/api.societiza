namespace WorkflowTemplate.Domain.ValueObjects;

public record TemplateVersion(int Major, int Minor)
{
    public override string ToString() => $"v{Major}.{Minor}";

    public static TemplateVersion Parse(string version)
    {
        var parts = version.TrimStart('v').Split('.');
        return new(int.Parse(parts[0]), int.Parse(parts[1]));
    }
}