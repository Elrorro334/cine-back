-- ============================================================================
-- Cine Teatro Xilotzin - Script de Sembrado Inicial (Database Seeding)
-- Siembra de roles (Admin, Vendedor), usuario 'admin' (password: admin123)
-- y redes locales autorizadas para el perímetro de taquilla.
-- ============================================================================

USE [cineDataBase];
GO

-- 1. Asegurar catálogo de roles
DECLARE @RoleAdmin NVARCHAR(50) = 'Admin';
DECLARE @RoleVendedor NVARCHAR(50) = 'Vendedor';

IF NOT EXISTS (SELECT 1 FROM [rol] WHERE [nombre_rol] = @RoleAdmin)
BEGIN
    INSERT INTO [rol] ([nombre_rol], [descripcion])
    VALUES (@RoleAdmin, 'Administrador del sistema con acceso total');
END
GO

DECLARE @RoleVendedorName NVARCHAR(50) = 'Vendedor';
IF NOT EXISTS (SELECT 1 FROM [rol] WHERE [nombre_rol] = @RoleVendedorName)
BEGIN
    INSERT INTO [rol] ([nombre_rol], [descripcion])
    VALUES (@RoleVendedorName, 'Vendedor de taquilla y dulcería');
END
GO

-- 2. Asegurar usuario de sembrado local para pruebas
DECLARE @AdminUser NVARCHAR(50) = 'admin';
DECLARE @AdminRolId INT = (SELECT TOP 1 [id_rol] FROM [rol] WHERE [nombre_rol] = 'Admin');
DECLARE @SeedHashPrefix NVARCHAR(10) = CHAR(36) + '2a' + CHAR(36) + '11' + CHAR(36);
DECLARE @SeedHashBody NVARCHAR(60) = 'xjAIqnrpU0DVg7c4SLk5JuQK.cc8d2KcxRPwbcDhLDZAeBkdcEPJW';
DECLARE @SeedPassHash NVARCHAR(100) = @SeedHashPrefix + @SeedHashBody;

IF NOT EXISTS (SELECT 1 FROM [vendedor] WHERE [nombre] = @AdminUser)
BEGIN
    INSERT INTO [vendedor] ([nombre], [id_rol], [password_hash], [estado_activo], [fecha_creacion])
    VALUES (@AdminUser, @AdminRolId, @SeedPassHash, 1, SYSUTCDATETIME());
END
ELSE
BEGIN
    UPDATE [vendedor]
    SET [id_rol] = @AdminRolId,
        [password_hash] = @SeedPassHash,
        [estado_activo] = 1
    WHERE [nombre] = @AdminUser;
END
GO

-- 3. Perímetro de red local autorizado (Loopback y subred de taquilla)
IF NOT EXISTS (SELECT 1 FROM [redes_autorizadas] WHERE [direccion_ip] = '127.0.0.1')
    INSERT INTO [redes_autorizadas] ([direccion_ip]) VALUES ('127.0.0.1');

IF NOT EXISTS (SELECT 1 FROM [redes_autorizadas] WHERE [direccion_ip] = '::1')
    INSERT INTO [redes_autorizadas] ([direccion_ip]) VALUES ('::1');

IF NOT EXISTS (SELECT 1 FROM [redes_autorizadas] WHERE [direccion_ip] = '192.168.0.100')
    INSERT INTO [redes_autorizadas] ([direccion_ip]) VALUES ('192.168.0.100');
GO
