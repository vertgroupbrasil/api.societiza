namespace Identity.Domain.Enumerations;

public enum UserRole
{
    SystemAdmin = 1,
    TenantOwner = 2,
    Manager = 4,
    Accountant,
    ClientViewer = 5,

    [Obsolete("Use ClientViewer instead")]
    ReadOnly = 6
}