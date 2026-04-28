-- ==============================================================
-- Etapa 3 C: CRUD Maestro - Detalle de Proveedores a Insumos
-- ==============================================================

-- 1. SP para insertar o recuperar el ID_PROVEEDOR según el RUC
IF OBJECT_ID('USP_EST_REGISTRAR_PROVEEDOR', 'P') IS NOT NULL DROP PROCEDURE USP_EST_REGISTRAR_PROVEEDOR;
GO
CREATE PROCEDURE [dbo].[USP_EST_REGISTRAR_PROVEEDOR]
    @RUC VARCHAR(20),
    @RAZON_SOCIAL VARCHAR(200),
    @CONTACTO VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ID_EXISTENTE INT = 0;

    -- Buscar si el RUC ya existe
    SELECT @ID_EXISTENTE = ID_PROVEEDOR 
    FROM [dbo].[TBL_ESTAMPADO_PROVEEDOR] 
    WHERE RUC = @RUC;

    IF @ID_EXISTENTE > 0
    BEGIN
        -- Actualizar datos si es que cambiaron la razon social o contacto (opcional)
        UPDATE [dbo].[TBL_ESTAMPADO_PROVEEDOR]
        SET 
            RAZON_SOCIAL = @RAZON_SOCIAL,
            CONTACTO = @CONTACTO
        WHERE ID_PROVEEDOR = @ID_EXISTENTE;
        
        -- Retornar el ID existente
        SELECT @ID_EXISTENTE AS RESULTADO_ID;
    END
    ELSE
    BEGIN
        -- Insertar nuevo Proveedor
        INSERT INTO [dbo].[TBL_ESTAMPADO_PROVEEDOR] (
            RUC, 
            RAZON_SOCIAL, 
            CONTACTO, 
            ES_ACTIVO,
            FECHA_REGISTRO
        ) VALUES (
            @RUC,
            @RAZON_SOCIAL,
            @CONTACTO,
            1,
            GETDATE()
        );

        -- Obtener el Identity y retornarlo
        SELECT CAST(SCOPE_IDENTITY() AS INT) AS RESULTADO_ID;
    END
END
GO

-- 2. SP para registrar en la tabla puente (Detalle de Insumos / Atributo de Compra)
IF OBJECT_ID('USP_EST_REGISTRAR_INSUMO_PROVEEDOR_DETALLE', 'P') IS NOT NULL DROP PROCEDURE USP_EST_REGISTRAR_INSUMO_PROVEEDOR_DETALLE;
GO
CREATE PROCEDURE [dbo].[USP_EST_REGISTRAR_INSUMO_PROVEEDOR_DETALLE]
    @ID_PROVEEDOR INT,
    @COD_ARTICULO VARCHAR(50),
    @PRESENTACION VARCHAR(100),
    @CAPACIDAD_NUMERICA DECIMAL(18,4),
    @UNIDAD_MEDIDA VARCHAR(50),
    @PRECIO_UNITARIO DECIMAL(18,4)
AS
BEGIN
    SET NOCOUNT ON;

    -- Validar si la relación exacta ya existe para NO duplicar, si existe, solo se actualiza precio y presentación
    DECLARE @ID_RELACION INT = 0;

    SELECT @ID_RELACION = ID_RELACION 
    FROM [dbo].[TBL_ESTAMPADO_INSUMO_PROVEEDOR]
    WHERE ID_PROVEEDOR = @ID_PROVEEDOR
      AND COD_ARTICULO = @COD_ARTICULO
      -- Si deseas que un proveedor venda el mismo articulo en multiples presentaciones (Ej. Galon y Cilindro), 
      -- descomenta la siguiente restriccion. Asumimos 1 art = multiples presentaciones.
      AND DES_PRESENTACION = @PRESENTACION;

    IF @ID_RELACION > 0
    BEGIN
        UPDATE [dbo].[TBL_ESTAMPADO_INSUMO_PROVEEDOR]
        SET 
            CAPACIDAD_NUMERICA = @CAPACIDAD_NUMERICA,
            UNIDAD_MEDIDA = @UNIDAD_MEDIDA,
            PRECIO_UNITARIO = @PRECIO_UNITARIO
        WHERE ID_RELACION = @ID_RELACION;
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[TBL_ESTAMPADO_INSUMO_PROVEEDOR] (
            ID_PROVEEDOR,
            COD_ARTICULO,
            CAPACIDAD_NUMERICA,
            PRECIO_UNITARIO,
            DES_PRESENTACION,
            UNIDAD_MEDIDA
        ) VALUES (
            @ID_PROVEEDOR,
            @COD_ARTICULO,
            @CAPACIDAD_NUMERICA,
            @PRECIO_UNITARIO,
            @PRESENTACION,
            @UNIDAD_MEDIDA
        );
    END
END
GO
