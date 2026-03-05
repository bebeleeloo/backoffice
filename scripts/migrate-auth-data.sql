-- One-time migration script: copy identity data from dbo to auth schema
-- Run AFTER auth service has created its tables via EF migrations
-- Run BEFORE removing identity tables from the monolith

-- Important: This preserves all GUIDs so existing JWT tokens remain valid

SET IDENTITY_INSERT OFF;

-- 1. Permissions
INSERT INTO auth.Permissions (Id, Code, Name, Description, [Group], CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, RowVersion)
SELECT Id, Code, Name, Description, [Group], CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, RowVersion
FROM dbo.Permissions p
WHERE NOT EXISTS (SELECT 1 FROM auth.Permissions ap WHERE ap.Id = p.Id);

-- 2. Roles
INSERT INTO auth.Roles (Id, Name, Description, IsSystem, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, RowVersion)
SELECT Id, Name, Description, IsSystem, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, RowVersion
FROM dbo.Roles r
WHERE NOT EXISTS (SELECT 1 FROM auth.Roles ar WHERE ar.Id = r.Id);

-- 3. RolePermissions
INSERT INTO auth.RolePermissions (Id, RoleId, PermissionId, CreatedAt, CreatedBy)
SELECT Id, RoleId, PermissionId, CreatedAt, CreatedBy
FROM dbo.RolePermissions rp
WHERE NOT EXISTS (SELECT 1 FROM auth.RolePermissions arp WHERE arp.Id = rp.Id);

-- 4. Users
INSERT INTO auth.Users (Id, Username, Email, PasswordHash, FullName, IsActive, Photo, PhotoContentType, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, RowVersion)
SELECT Id, Username, Email, PasswordHash, FullName, IsActive, Photo, PhotoContentType, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, RowVersion
FROM dbo.Users u
WHERE NOT EXISTS (SELECT 1 FROM auth.Users au WHERE au.Id = u.Id);

-- 5. UserRoles
INSERT INTO auth.UserRoles (Id, UserId, RoleId, CreatedAt, CreatedBy)
SELECT Id, UserId, RoleId, CreatedAt, CreatedBy
FROM dbo.UserRoles ur
WHERE NOT EXISTS (SELECT 1 FROM auth.UserRoles aur WHERE aur.Id = ur.Id);

-- 6. UserPermissionOverrides
INSERT INTO auth.UserPermissionOverrides (Id, UserId, PermissionId, IsAllowed, CreatedAt, CreatedBy)
SELECT Id, UserId, PermissionId, IsAllowed, CreatedAt, CreatedBy
FROM dbo.UserPermissionOverrides upo
WHERE NOT EXISTS (SELECT 1 FROM auth.UserPermissionOverrides aupo WHERE aupo.Id = upo.Id);

-- 7. DataScopes
INSERT INTO auth.DataScopes (Id, UserId, ScopeType, ScopeValue, CreatedAt, CreatedBy)
SELECT Id, UserId, ScopeType, ScopeValue, CreatedAt, CreatedBy
FROM dbo.DataScopes ds
WHERE NOT EXISTS (SELECT 1 FROM auth.DataScopes ads WHERE ads.Id = ds.Id);

-- 8. UserRefreshTokens
INSERT INTO auth.UserRefreshTokens (Id, UserId, TokenHash, ExpiresAt, RevokedAt, ReplacedByTokenHash, CreatedAt)
SELECT Id, UserId, TokenHash, ExpiresAt, RevokedAt, ReplacedByTokenHash, CreatedAt
FROM dbo.UserRefreshTokens urt
WHERE NOT EXISTS (SELECT 1 FROM auth.UserRefreshTokens aurt WHERE aurt.Id = urt.Id);

PRINT 'Auth data migration completed successfully.';
