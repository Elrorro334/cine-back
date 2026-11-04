-- ============================================================================
-- Cine Teatro Xilotzin - Script de Sembrado Inicial (Database Seeding)
-- Siembra de roles (Admin, Vendedor), usuario 'admin' (password: admin123)
-- y redes locales autorizadas para el perímetro de taquilla.
-- ============================================================================

USE [cineDataBase];
GO

-- 1. Asegurar catálogo de roles
IF NOT EXISTS (SELECT 1 FROM [rol] WHERE [nombre_rol] = 'Admin')
BEGIN
    INSERT INTO [rol] ([nombre_rol], [descripcion])
    VALUES ('Admin', 'Administrador del sistema con acceso total');
END
GO

IF NOT EXISTS (SELECT 1 FROM [rol] WHERE [nombre_rol] = 'Vendedor')
BEGIN
    INSERT INTO [rol] ([nombre_rol], [descripcion])
    VALUES ('Vendedor', 'Vendedor de taquilla y dulcería');
END
GO

-- 2. Asegurar usuario 'admin' con hash BCrypt de 'admin123'
DECLARE @AdminRolId INT = (SELECT TOP 1 [id_rol] FROM [rol] WHERE [nombre_rol] = 'Admin');

IF NOT EXISTS (SELECT 1 FROM [vendedor] WHERE [nombre] = 'admin')
BEGIN
    INSERT INTO [vendedor] ([nombre], [id_rol], [password_hash], [estado_activo], [fecha_creacion])
    VALUES ('admin', @AdminRolId, '$2a$11$xjAIqnrpU0DVg7c4SLk5JuQK.cc8d2KcxRPwbcDhLDZAeBkdcEPJW', 1, SYSUTCDATETIME());
END
ELSE
BEGIN
    UPDATE [vendedor]
    SET [id_rol] = @AdminRolId,
        [password_hash] = '$2a$11$xjAIqnrpU0DVg7c4SLk5JuQK.cc8d2KcxRPwbcDhLDZAeBkdcEPJW',
        [estado_activo] = 1
    WHERE [nombre] = 'admin';
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
