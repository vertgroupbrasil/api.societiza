using Ardalis.SmartEnum;
using Newtonsoft.Json;

namespace Identity.Domain.Enumerations;

[method:JsonConstructor]
public class UserRole(string name, int value) : SmartEnum<UserRole>(name, value)
{
    public static readonly UserRole SystemAdmin = new SystemAdminRole();
    public static readonly UserRole AccountingAdmin = new AccountingAdminRole();
    public static readonly UserRole AccountingEmployee = new AccountingEmployeeRole();

    public static implicit operator UserRole(string name) 
        => FromName(name);

    public static implicit operator UserRole(int value) 
        => FromValue(value);

    public static implicit operator string(UserRole role) 
        => role.Name;

    public static implicit operator int(UserRole role)
        => role.Value;

    public class SystemAdminRole() : UserRole(nameof(SystemAdmin), 0) { }
    public class AccountingAdminRole() : UserRole(nameof(AccountingAdmin), 1) { }
    public class AccountingEmployeeRole() : UserRole(nameof(AccountingEmployee), 2) { }
}