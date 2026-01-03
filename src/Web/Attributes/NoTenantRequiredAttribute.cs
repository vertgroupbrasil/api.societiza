namespace Web.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class NoTenantRequiredAttribute : Attribute
{
}