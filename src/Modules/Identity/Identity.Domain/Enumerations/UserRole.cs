using Ardalis.SmartEnum;
using Newtonsoft.Json;

namespace Identity.Domain.Enumerations;

[method:JsonConstructor]
public class UserRole(string name, int value) : SmartEnum<UserRole>(name, value)
{
    public static readonly UserRole SystemAdmin = new SystemAdminRole();
    public static readonly UserRole AccountancyAdmin = new AccountancyAdminRole();
    public static readonly UserRole AccountancyEmployee = new AccountancyEmployeeRole();

    public static implicit operator UserRole(string name)
        => FromName(name);

    public static implicit operator UserRole(int value)
        => FromValue(value);

    public static implicit operator string(UserRole role)
        => role.Name;

    public static implicit operator int(UserRole role)
        => role.Value;

    public class SystemAdminRole() : UserRole(nameof(SystemAdmin), 0) { }
    public class AccountancyAdminRole() : UserRole(nameof(AccountancyAdmin), 1) { }
    public class AccountancyEmployeeRole() : UserRole(nameof(AccountancyEmployee), 2) { }
}