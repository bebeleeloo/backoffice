namespace Broker.Backoffice.Domain.Identity;

public static class Permissions
{
    public const string UsersRead = "users.read";
    public const string UsersCreate = "users.create";
    public const string UsersUpdate = "users.update";
    public const string UsersDelete = "users.delete";

    public const string RolesRead = "roles.read";
    public const string RolesCreate = "roles.create";
    public const string RolesUpdate = "roles.update";
    public const string RolesDelete = "roles.delete";

    public const string PermissionsRead = "permissions.read";

    public const string AuditRead = "audit.read";

    public const string ClientsRead = "clients.read";
    public const string ClientsCreate = "clients.create";
    public const string ClientsUpdate = "clients.update";
    public const string ClientsDelete = "clients.delete";

    public const string AccountsRead = "accounts.read";
    public const string AccountsCreate = "accounts.create";
    public const string AccountsUpdate = "accounts.update";
    public const string AccountsDelete = "accounts.delete";

    public const string InstrumentsRead = "instruments.read";
    public const string InstrumentsCreate = "instruments.create";
    public const string InstrumentsUpdate = "instruments.update";
    public const string InstrumentsDelete = "instruments.delete";

    public const string SettingsManage = "settings.manage";

    public static readonly (string Code, string Name, string Group)[] All =
    [
        (UsersRead, "View users", "Users"),
        (UsersCreate, "Create users", "Users"),
        (UsersUpdate, "Update users", "Users"),
        (UsersDelete, "Delete users", "Users"),
        (RolesRead, "View roles", "Roles"),
        (RolesCreate, "Create roles", "Roles"),
        (RolesUpdate, "Update roles", "Roles"),
        (RolesDelete, "Delete roles", "Roles"),
        (PermissionsRead, "View permissions", "Permissions"),
        (AuditRead, "View audit log", "Audit"),
        (ClientsRead, "View clients", "Clients"),
        (ClientsCreate, "Create clients", "Clients"),
        (ClientsUpdate, "Update clients", "Clients"),
        (ClientsDelete, "Delete clients", "Clients"),
        (AccountsRead, "View accounts", "Accounts"),
        (AccountsCreate, "Create accounts", "Accounts"),
        (AccountsUpdate, "Update accounts", "Accounts"),
        (AccountsDelete, "Delete accounts", "Accounts"),
        (InstrumentsRead, "View instruments", "Instruments"),
        (InstrumentsCreate, "Create instruments", "Instruments"),
        (InstrumentsUpdate, "Update instruments", "Instruments"),
        (InstrumentsDelete, "Delete instruments", "Instruments"),
        (SettingsManage, "Manage reference data settings", "Settings"),
    ];
}
