-- ============================================================================
-- Cine Teatro Xilotzin - Complemento de Datos Maestros y Transaccionales
-- Tablas: butaca (200), categoria (6), producto (11), estado_butaca_funcion,
-- detalle_preorden_dulceria, venta (10) y detalle_venta.
-- ============================================================================

USE [cineDataBase];
GO

-- 1. BUTACAS (SALA 1: 120 BUTACAS [A-J, 1-12], SALA 2: 80 BUTACAS [A-H, 1-10])
IF NOT EXISTS (SELECT 1 FROM [butaca] WHERE [id_sala] = 1)
BEGIN
    DECLARE @Fila CHAR(1);
    DECLARE @Num INT;
    DECLARE @FilasS1 VARCHAR(10) = 'ABCDEFGHIJ';
    DECLARE @i INT = 1;
    
    WHILE @i <= LEN(@FilasS1)
    BEGIN
        SET @Fila = SUBSTRING(@FilasS1, @i, 1);
        SET @Num = 1;
        WHILE @Num <= 12
        BEGIN
            INSERT INTO [butaca] ([id_sala], [fila], [numero]) VALUES (1, @Fila, @Num);
            SET @Num = @Num + 1;
        END
        SET @i = @i + 1;
    END
END
GO

IF NOT EXISTS (SELECT 1 FROM [butaca] WHERE [id_sala] = 2)
BEGIN
    DECLARE @Fila2 CHAR(1);
    DECLARE @Num2 INT;
    DECLARE @FilasS2 VARCHAR(8) = 'ABCDEFGH';
    DECLARE @j INT = 1;
    
    WHILE @j <= LEN(@FilasS2)
    BEGIN
        SET @Fila2 = SUBSTRING(@FilasS2, @j, 1);
        SET @Num2 = 1;
        WHILE @Num2 <= 10
        BEGIN
            INSERT INTO [butaca] ([id_sala], [fila], [numero]) VALUES (2, @Fila2, @Num2);
            SET @Num2 = @Num2 + 1;
        END
        SET @j = @j + 1;
    END
END
GO

-- 2. CATEGORIAS DE PRODUCTOS
IF NOT EXISTS (SELECT 1 FROM [categoria] WHERE [nombre] = 'Taquilla')
    INSERT INTO [categoria] ([nombre], [descripcion]) VALUES ('Taquilla', 'Boletos y accesos oficiales a salas');

IF NOT EXISTS (SELECT 1 FROM [categoria] WHERE [nombre] = 'Palomitas')
    INSERT INTO [categoria] ([nombre], [descripcion]) VALUES ('Palomitas', 'Palomitas de maíz tradicionales, acarameladas y gourmet');

IF NOT EXISTS (SELECT 1 FROM [categoria] WHERE [nombre] = 'Bebidas')
    INSERT INTO [categoria] ([nombre], [descripcion]) VALUES ('Bebidas', 'Refrescos de máquina, agua embotellada e infusiones frías');

IF NOT EXISTS (SELECT 1 FROM [categoria] WHERE [nombre] = 'Alimentos')
    INSERT INTO [categoria] ([nombre], [descripcion]) VALUES ('Alimentos', 'Nachos, hot dogs jumbo, pizzas y comida caliente');

IF NOT EXISTS (SELECT 1 FROM [categoria] WHERE [nombre] = 'Dulces')
    INSERT INTO [categoria] ([nombre], [descripcion]) VALUES ('Dulces', 'Chocolates, confitería y dulces importados');

IF NOT EXISTS (SELECT 1 FROM [categoria] WHERE [nombre] = 'Combos')
    INSERT INTO [categoria] ([nombre], [descripcion]) VALUES ('Combos', 'Paquetes de dulcería con palomitas, bebidas y complementos');
GO

-- 3. PRODUCTOS DEL CATÁLOGO DE DULCERÍA Y BOLETAJE
DECLARE @CatTaquilla INT = (SELECT TOP 1 [id_categoria] FROM [categoria] WHERE [nombre] = 'Taquilla');
DECLARE @CatPalomitas INT = (SELECT TOP 1 [id_categoria] FROM [categoria] WHERE [nombre] = 'Palomitas');
DECLARE @CatBebidas INT = (SELECT TOP 1 [id_categoria] FROM [categoria] WHERE [nombre] = 'Bebidas');
DECLARE @CatAlimentos INT = (SELECT TOP 1 [id_categoria] FROM [categoria] WHERE [nombre] = 'Alimentos');
DECLARE @CatDulces INT = (SELECT TOP 1 [id_categoria] FROM [categoria] WHERE [nombre] = 'Dulces');
DECLARE @CatCombos INT = (SELECT TOP 1 [id_categoria] FROM [categoria] WHERE [nombre] = 'Combos');

IF NOT EXISTS (SELECT 1 FROM [producto] WHERE [codigo_barras] = 'BOL-GEN')
    INSERT INTO [producto] ([codigo_barras], [nombre], [id_categoria], [precio_venta], [costo], [stock_actual], [stock_minimo])
    VALUES ('BOL-GEN', 'Boleto General Taquilla', @CatTaquilla, 50.00, 0.00, 99999, 10);

IF NOT EXISTS (SELECT 1 FROM [producto] WHERE [codigo_barras] = '7501001')
    INSERT INTO [producto] ([codigo_barras], [nombre], [id_categoria], [precio_venta], [costo], [stock_actual], [stock_minimo])
    VALUES ('7501001', 'Palomitas Clásicas Grandes', @CatPalomitas, 65.00, 15.00, 150, 20);

IF NOT EXISTS (SELECT 1 FROM [producto] WHERE [codigo_barras] = '7501002')
    INSERT INTO [producto] ([codigo_barras], [nombre], [id_categoria], [precio_venta], [costo], [stock_actual], [stock_minimo])
    VALUES ('7501002', 'Refresco Grande 1L', @CatBebidas, 45.00, 10.00, 200, 30);

IF NOT EXISTS (SELECT 1 FROM [producto] WHERE [codigo_barras] = '7501003')
    INSERT INTO [producto] ([codigo_barras], [nombre], [id_categoria], [precio_venta], [costo], [stock_actual], [stock_minimo])
    VALUES ('7501003', 'Nachos con Queso y Jalapeños', @CatAlimentos, 55.00, 18.00, 100, 15);

IF NOT EXISTS (SELECT 1 FROM [producto] WHERE [codigo_barras] = '7501004')
    INSERT INTO [producto] ([codigo_barras], [nombre], [id_categoria], [precio_venta], [costo], [stock_actual], [stock_minimo])
    VALUES ('7501004', 'Hot Dog Jumbo Clásico', @CatAlimentos, 50.00, 16.00, 80, 15);

IF NOT EXISTS (SELECT 1 FROM [producto] WHERE [codigo_barras] = '7501005')
    INSERT INTO [producto] ([codigo_barras], [nombre], [id_categoria], [precio_venta], [costo], [stock_actual], [stock_minimo])
    VALUES ('7501005', 'M&M''s Chocolate 120g', @CatDulces, 35.00, 12.00, 90, 10);

DECLARE @SkuPizza NVARCHAR(20) = '7501006';
IF NOT EXISTS (SELECT 1 FROM [producto] WHERE [codigo_barras] = @SkuPizza)
    INSERT INTO [producto] ([codigo_barras], [nombre], [id_categoria], [precio_venta], [costo], [stock_actual], [stock_minimo])
    VALUES (@SkuPizza, 'Pizza Personal Pepperoni', @CatAlimentos, 75.00, 25.00, 60, 10);

IF NOT EXISTS (SELECT 1 FROM [producto] WHERE [codigo_barras] = '7501007')
    INSERT INTO [producto] ([codigo_barras], [nombre], [id_categoria], [precio_venta], [costo], [stock_actual], [stock_minimo])
    VALUES ('7501007', 'Crepa Dulce Nutella', @CatAlimentos, 60.00, 18.00, 50, 10);

IF NOT EXISTS (SELECT 1 FROM [producto] WHERE [codigo_barras] = 'COMBO01')
    INSERT INTO [producto] ([codigo_barras], [nombre], [id_categoria], [precio_venta], [costo], [stock_actual], [stock_minimo])
    VALUES ('COMBO01', 'Combo Pareja (Palomitas Grandes + 2 Refrescos)', @CatCombos, 130.00, 32.00, 80, 10);

IF NOT EXISTS (SELECT 1 FROM [producto] WHERE [codigo_barras] = 'COMBO02')
    INSERT INTO [producto] ([codigo_barras], [nombre], [id_categoria], [precio_venta], [costo], [stock_actual], [stock_minimo])
    VALUES ('COMBO02', 'Combo Amigos (Palomitas + Nachos + 2 Refrescos)', @CatCombos, 175.00, 48.00, 60, 10);

IF NOT EXISTS (SELECT 1 FROM [producto] WHERE [codigo_barras] = 'COMBO03')
    INSERT INTO [producto] ([codigo_barras], [nombre], [id_categoria], [precio_venta], [costo], [stock_actual], [stock_minimo])
    VALUES ('COMBO03', 'Combo Familiar (2 Palomitas + 4 Refrescos + Hot Dog)', @CatCombos, 260.00, 70.00, 40, 10);
GO

-- 4. ESTADO DE BUTACAS POR FUNCIÓN (Ocupación en funciones activas)
DECLARE @FnId1 INT = 241;
DECLARE @FnId2 INT = 242;
DECLARE @FnId3 INT = 315;

IF NOT EXISTS (SELECT 1 FROM [estado_butaca_funcion] WHERE [id_funcion] = @FnId1)
BEGIN
    INSERT INTO [estado_butaca_funcion] ([id_butaca], [id_funcion], [estato])
    SELECT [id_butaca], @FnId1, 1 FROM [butaca] WHERE [id_sala] = 1 AND [fila] IN ('C', 'D') AND [numero] IN (5, 6, 7);
END

IF NOT EXISTS (SELECT 1 FROM [estado_butaca_funcion] WHERE [id_funcion] = @FnId2)
BEGIN
    INSERT INTO [estado_butaca_funcion] ([id_butaca], [id_funcion], [estato])
    SELECT [id_butaca], @FnId2, 1 FROM [butaca] WHERE [id_sala] = 1 AND [fila] IN ('E', 'F') AND [numero] IN (4, 5, 6, 7);
END

IF NOT EXISTS (SELECT 1 FROM [estado_butaca_funcion] WHERE [id_funcion] = @FnId3)
BEGIN
    INSERT INTO [estado_butaca_funcion] ([id_butaca], [id_funcion], [estato])
    SELECT [id_butaca], @FnId3, 1 FROM [butaca] WHERE [id_sala] = 1 AND [fila] = 'B' AND [numero] IN (3, 4);
END
GO

-- 5. DETALLE PREORDEN DULCERIA
DECLARE @ProdCombo01 INT = (SELECT TOP 1 [id_producto] FROM [producto] WHERE [codigo_barras] = 'COMBO01');
DECLARE @ProdPalomitas INT = (SELECT TOP 1 [id_producto] FROM [producto] WHERE [codigo_barras] = '7501001');
DECLARE @ProdRefresco INT = (SELECT TOP 1 [id_producto] FROM [producto] WHERE [codigo_barras] = '7501002');
DECLARE @ProdNachos INT = (SELECT TOP 1 [id_producto] FROM [producto] WHERE [codigo_barras] = '7501003');

IF NOT EXISTS (SELECT 1 FROM [detalle_preorden_dulceria] WHERE [id_preorden] = 1)
    INSERT INTO [detalle_preorden_dulceria] ([id_preorden], [id_producto], [cantidad]) VALUES (1, @ProdCombo01, 1);

IF NOT EXISTS (SELECT 1 FROM [detalle_preorden_dulceria] WHERE [id_preorden] = 2)
BEGIN
    INSERT INTO [detalle_preorden_dulceria] ([id_preorden], [id_producto], [cantidad]) VALUES (2, @ProdPalomitas, 1);
    INSERT INTO [detalle_preorden_dulceria] ([id_preorden], [id_producto], [cantidad]) VALUES (2, @ProdRefresco, 2);
END

IF NOT EXISTS (SELECT 1 FROM [detalle_preorden_dulceria] WHERE [id_preorden] = 3)
    INSERT INTO [detalle_preorden_dulceria] ([id_preorden], [id_producto], [cantidad]) VALUES (3, @ProdNachos, 1);

IF NOT EXISTS (SELECT 1 FROM [detalle_preorden_dulceria] WHERE [id_preorden] = 9)
    INSERT INTO [detalle_preorden_dulceria] ([id_preorden], [id_producto], [cantidad]) VALUES (9, @ProdCombo01, 1);
GO

-- 6. VENTAS Y DETALLE DE VENTAS (Semana Cinematográfica: 29/10/2026 al 04/11/2026)
DECLARE @IdAdmin INT = (SELECT TOP 1 [id_vendedor] FROM [vendedor] WHERE [nombre] = 'admin');
DECLARE @ProdBoleto INT = (SELECT TOP 1 [id_producto] FROM [producto] WHERE [codigo_barras] = 'BOL-GEN');
DECLARE @ProdCombo01 INT = (SELECT TOP 1 [id_producto] FROM [producto] WHERE [codigo_barras] = 'COMBO01');
DECLARE @ProdCombo02 INT = (SELECT TOP 1 [id_producto] FROM [producto] WHERE [codigo_barras] = 'COMBO02');
DECLARE @ProdCombo03 INT = (SELECT TOP 1 [id_producto] FROM [producto] WHERE [codigo_barras] = 'COMBO03');
DECLARE @ProdPalomitas INT = (SELECT TOP 1 [id_producto] FROM [producto] WHERE [codigo_barras] = '7501001');
DECLARE @ProdRefresco INT = (SELECT TOP 1 [id_producto] FROM [producto] WHERE [codigo_barras] = '7501002');
DECLARE @ProdNachos INT = (SELECT TOP 1 [id_producto] FROM [producto] WHERE [codigo_barras] = '7501003');
DECLARE @ProdHotDog INT = (SELECT TOP 1 [id_producto] FROM [producto] WHERE [codigo_barras] = '7501004');

DECLARE @MetodoEfectivo NVARCHAR(20) = 'Efectivo';
DECLARE @MetodoTarjeta NVARCHAR(20) = 'Tarjeta';

IF NOT EXISTS (SELECT 1 FROM [venta])
BEGIN
    -- Venta 1: 29 Octubre 14:30 - Efectivo (2 Boletos)
    INSERT INTO [venta] ([id_vendedor], [fecha_venta], [total], [metodo_pago])
    VALUES (@IdAdmin, '2026-10-29 14:30:00', 100.00, @MetodoEfectivo);
    DECLARE @V1 INT = SCOPE_IDENTITY();
    INSERT INTO [detalle_venta] ([id_venta], [id_producto], [cantidad]) VALUES (@V1, @ProdBoleto, 2);

    -- Venta 2: 29 Octubre 18:45 - Tarjeta (1 Combo Pareja + 2 Boletos)
    INSERT INTO [venta] ([id_vendedor], [fecha_venta], [total], [metodo_pago])
    VALUES (@IdAdmin, '2026-10-29 18:45:00', 230.00, @MetodoTarjeta);
    DECLARE @V2 INT = SCOPE_IDENTITY();
    INSERT INTO [detalle_venta] ([id_venta], [id_producto], [cantidad]) VALUES (@V2, @ProdBoleto, 2);
    INSERT INTO [detalle_venta] ([id_venta], [id_producto], [cantidad]) VALUES (@V2, @ProdCombo01, 1);

    -- Venta 3: 30 Octubre 16:15 - Efectivo (3 Boletos + 1 Nachos + 2 Refrescos)
    INSERT INTO [venta] ([id_vendedor], [fecha_venta], [total], [metodo_pago])
    VALUES (@IdAdmin, '2026-10-30 16:15:00', 295.00, @MetodoEfectivo);
    DECLARE @V3 INT = SCOPE_IDENTITY();
    INSERT INTO [detalle_venta] ([id_venta], [id_producto], [cantidad]) VALUES (@V3, @ProdBoleto, 3);
    INSERT INTO [detalle_venta] ([id_venta], [id_producto], [cantidad]) VALUES (@V3, @ProdNachos, 1);
    INSERT INTO [detalle_venta] ([id_venta], [id_producto], [cantidad]) VALUES (@V3, @ProdRefresco, 2);

    -- Venta 4: 31 Octubre 19:00 - Tarjeta (4 Boletos + 1 Combo Familiar)
    INSERT INTO [venta] ([id_vendedor], [fecha_venta], [total], [metodo_pago])
    VALUES (@IdAdmin, '2026-10-31 19:00:00', 460.00, @MetodoTarjeta);
    DECLARE @V4 INT = SCOPE_IDENTITY();
    INSERT INTO [detalle_venta] ([id_venta], [id_producto], [cantidad]) VALUES (@V4, @ProdBoleto, 4);
    INSERT INTO [detalle_venta] ([id_venta], [id_producto], [cantidad]) VALUES (@V4, @ProdCombo03, 1);

    -- Venta 5: 01 Noviembre 15:20 - Efectivo (2 Boletos + 1 Palomitas + 1 Refresco)
    INSERT INTO [venta] ([id_vendedor], [fecha_venta], [total], [metodo_pago])
    VALUES (@IdAdmin, '2026-11-01 15:20:00', 210.00, @MetodoEfectivo);
    DECLARE @V5 INT = SCOPE_IDENTITY();
    INSERT INTO [detalle_venta] ([id_venta], [id_producto], [cantidad]) VALUES (@V5, @ProdBoleto, 2);
    INSERT INTO [detalle_venta] ([id_venta], [id_producto], [cantidad]) VALUES (@V5, @ProdPalomitas, 1);
    INSERT INTO [detalle_venta] ([id_venta], [id_producto], [cantidad]) VALUES (@V5, @ProdRefresco, 1);

    -- Venta 6: 02 Noviembre 17:50 - Tarjeta (2 Boletos + 1 Hot Dog + 1 Refresco)
    INSERT INTO [venta] ([id_vendedor], [fecha_venta], [total], [metodo_pago])
    VALUES (@IdAdmin, '2026-11-02 17:50:00', 195.00, @MetodoTarjeta);
    DECLARE @V6 INT = SCOPE_IDENTITY();
    INSERT INTO [detalle_venta] ([id_venta], [id_producto], [cantidad]) VALUES (@V6, @ProdBoleto, 2);
    INSERT INTO [detalle_venta] ([id_venta], [id_producto], [cantidad]) VALUES (@V6, @ProdHotDog, 1);
    INSERT INTO [detalle_venta] ([id_venta], [id_producto], [cantidad]) VALUES (@V6, @ProdRefresco, 1);

    -- Venta 7: 03 Noviembre 13:10 - Efectivo (1 Combo Pareja)
    INSERT INTO [venta] ([id_vendedor], [fecha_venta], [total], [metodo_pago])
    VALUES (@IdAdmin, '2026-11-03 13:10:00', 130.00, @MetodoEfectivo);
    DECLARE @V7 INT = SCOPE_IDENTITY();
    INSERT INTO [detalle_venta] ([id_venta], [id_producto], [cantidad]) VALUES (@V7, @ProdCombo01, 1);

    -- Venta 8: 03 Noviembre 20:30 - Tarjeta (2 Boletos + 1 Combo Amigos)
    INSERT INTO [venta] ([id_vendedor], [fecha_venta], [total], [metodo_pago])
    VALUES (@IdAdmin, '2026-11-03 20:30:00', 275.00, @MetodoTarjeta);
    DECLARE @V8 INT = SCOPE_IDENTITY();
    INSERT INTO [detalle_venta] ([id_venta], [id_producto], [cantidad]) VALUES (@V8, @ProdBoleto, 2);
    INSERT INTO [detalle_venta] ([id_venta], [id_producto], [cantidad]) VALUES (@V8, @ProdCombo02, 1);

    -- Venta 9: 04 Noviembre 12:00 - Efectivo (2 Boletos + 1 Palomitas)
    INSERT INTO [venta] ([id_vendedor], [fecha_venta], [total], [metodo_pago])
    VALUES (@IdAdmin, '2026-11-04 12:00:00', 165.00, @MetodoEfectivo);
    DECLARE @V9 INT = SCOPE_IDENTITY();
    INSERT INTO [detalle_venta] ([id_venta], [id_producto], [cantidad]) VALUES (@V9, @ProdBoleto, 2);
    INSERT INTO [detalle_venta] ([id_venta], [id_producto], [cantidad]) VALUES (@V9, @ProdPalomitas, 1);

    -- Venta 10: 04 Noviembre 15:45 - Tarjeta (3 Boletos)
    INSERT INTO [venta] ([id_vendedor], [fecha_venta], [total], [metodo_pago])
    VALUES (@IdAdmin, '2026-11-04 15:45:00', 150.00, @MetodoTarjeta);
    DECLARE @V10 INT = SCOPE_IDENTITY();
    INSERT INTO [detalle_venta] ([id_venta], [id_producto], [cantidad]) VALUES (@V10, @ProdBoleto, 3);
END
GO

-- 7. PROVEEDORES, FACTURAS Y ENTRADAS DE MERCANCIA (HU-11)
DECLARE @ProvNombre1 NVARCHAR(100) = 'Distribuidora Central de Dulces';
DECLARE @ProvNombre2 NVARCHAR(100) = 'Bebidas y Refrescos de Oriente';
DECLARE @ProvNombre3 NVARCHAR(100) = 'Insumos y Alimentos Xilotzin';

IF NOT EXISTS (SELECT 1 FROM [proveedor] WHERE [nombre] = @ProvNombre1)
    INSERT INTO [proveedor] ([nombre], [rfc], [telefono], [contacto], [activo])
    VALUES (@ProvNombre1, 'DCD850315AB1', '222-345-6789', 'Lic. Roberto Morales', 1);

IF NOT EXISTS (SELECT 1 FROM [proveedor] WHERE [nombre] = @ProvNombre2)
    INSERT INTO [proveedor] ([nombre], [rfc], [telefono], [contacto], [activo])
    VALUES (@ProvNombre2, 'BRO920810XY2', '246-123-4567', 'Ing. Patricia Ramos', 1);

IF NOT EXISTS (SELECT 1 FROM [proveedor] WHERE [nombre] = @ProvNombre3)
    INSERT INTO [proveedor] ([nombre], [rfc], [telefono], [contacto], [activo])
    VALUES (@ProvNombre3, 'IAX100422MN3', '246-987-6543', 'C.P. Fernando Tellez', 1);

-- Ajustar factor de equivalencia para Pizza (1 pizza = 8 rebanadas individuales)
UPDATE [producto] SET [factor_equivalencia] = 8 WHERE [codigo_barras] = '7501006';

-- Facturas y entradas iniciales
DECLARE @ProvDulces INT = (SELECT TOP 1 [id_proveedor] FROM [proveedor] WHERE [nombre] = @ProvNombre1);
DECLARE @ProvBebidas INT = (SELECT TOP 1 [id_proveedor] FROM [proveedor] WHERE [nombre] = @ProvNombre2);
DECLARE @ProdMms INT = (SELECT TOP 1 [id_producto] FROM [producto] WHERE [codigo_barras] = '7501005');
DECLARE @ProdRefresco2 INT = (SELECT TOP 1 [id_producto] FROM [producto] WHERE [codigo_barras] = '7501002');
DECLARE @AdminId INT = (SELECT TOP 1 [id_vendedor] FROM [vendedor] WHERE [nombre] = 'admin');
DECLARE @FolioFact1 NVARCHAR(50) = 'F-8810';
DECLARE @FolioFact2 NVARCHAR(50) = 'FAC-9012';

IF NOT EXISTS (SELECT 1 FROM [facturas_proveedor] WHERE [folio] = @FolioFact1)
BEGIN
    INSERT INTO [facturas_proveedor] ([id_proveedor], [folio], [total_pagado], [fecha_registro], [id_vendedor])
    VALUES (@ProvDulces, @FolioFact1, 600.00, '2026-10-25 10:15:00', @AdminId);
    DECLARE @Fact1 INT = SCOPE_IDENTITY();

    INSERT INTO [entrada_mercancia] ([id_factura], [id_producto], [folio_factura], [cantidad], [costo_unitario], [costo_total], [cantidad_stock_agregada], [fecha_entrada])
    VALUES (@Fact1, @ProdMms, @FolioFact1, 50, 12.00, 600.00, 50, '2026-10-25 10:15:00');
END

IF NOT EXISTS (SELECT 1 FROM [facturas_proveedor] WHERE [folio] = @FolioFact2)
BEGIN
    INSERT INTO [facturas_proveedor] ([id_proveedor], [folio], [total_pagado], [fecha_registro], [id_vendedor])
    VALUES (@ProvBebidas, @FolioFact2, 1000.00, '2026-10-28 16:40:00', @AdminId);
    DECLARE @Fact2 INT = SCOPE_IDENTITY();

    INSERT INTO [entrada_mercancia] ([id_factura], [id_producto], [folio_factura], [cantidad], [costo_unitario], [costo_total], [cantidad_stock_agregada], [fecha_entrada])
    VALUES (@Fact2, @ProdRefresco2, @FolioFact2, 100, 10.00, 1000.00, 100, '2026-10-28 16:40:00');
END
GO
