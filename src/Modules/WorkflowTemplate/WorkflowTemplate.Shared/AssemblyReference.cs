using System.Reflection;

namespace WorkflowTemplate.Shared;
public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}