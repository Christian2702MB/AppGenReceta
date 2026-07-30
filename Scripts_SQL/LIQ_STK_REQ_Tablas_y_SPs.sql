-- =======================================================================
-- MÓDULO: LIQUIDACIÓN DE INSUMOS DE ESTAMPADO
-- SUB-MÓDULOS: STOCK ACTUAL (LIQ_STK) y REQUERIMIENTOS (LIQ_REQ)
-- =======================================================================

-- -----------------------------------------------------------------------
-- 1. TABLAS DE STOCK (LIQ_STK)
-- -----------------------------------------------------------------------

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LIQ_STK_StockInsumos')
BEGIN
    CREATE TABLE LIQ_STK_StockInsumos (
        CodInsumo VARCHAR(50) NOT NULL PRIMARY KEY,
        Descripcion VARCHAR(250) NULL,
        UnidadMedida VARCHAR(20) NULL,
        StockActual DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        FechaUltimaActualizacion DATETIME NOT NULL DEFAULT GETDATE(),
        UsuarioUltimaActualizacion VARCHAR(100) NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LIQ_STK_Kardex')
BEGIN
    CREATE TABLE LIQ_STK_Kardex (
        IdMovimiento INT IDENTITY(1,1) PRIMARY KEY,
        CodInsumo VARCHAR(50) NOT NULL,
        TipoMovimiento VARCHAR(20) NOT NULL, -- 'Entrada' o 'Salida'
        Concepto VARCHAR(100) NOT NULL, -- Ej: 'Recepción de Requerimiento', 'Consumo Fórmula'
        Cantidad DECIMAL(18,2) NOT NULL,
        StockResultante DECIMAL(18,2) NOT NULL,
        ReferenciaID VARCHAR(50) NULL, -- Ej: NumRequerimiento
        Observaciones VARCHAR(MAX) NULL,
        FechaMovimiento DATETIME NOT NULL DEFAULT GETDATE(),
        Usuario VARCHAR(100) NOT NULL,
        FOREIGN KEY (CodInsumo) REFERENCES LIQ_STK_StockInsumos(CodInsumo)
    );
END
GO

-- -----------------------------------------------------------------------
-- 2. TABLAS DE REQUERIMIENTOS (LIQ_REQ)
-- -----------------------------------------------------------------------

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LIQ_REQ_Recepciones')
BEGIN
    CREATE TABLE LIQ_REQ_Recepciones (
        NumRequerimiento INT NOT NULL PRIMARY KEY,
        CodOrdPro VARCHAR(50) NOT NULL,
        Motivo VARCHAR(20) NOT NULL,
        Estado VARCHAR(50) NOT NULL DEFAULT 'Recibido',
        FechaRecepcion DATETIME NOT NULL DEFAULT GETDATE(),
        UsuarioRecepcion VARCHAR(100) NOT NULL,
        Observaciones VARCHAR(MAX) NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LIQ_REQ_RecepcionesDetalle')
BEGIN
    CREATE TABLE LIQ_REQ_RecepcionesDetalle (
        IdRecepcionDetalle INT IDENTITY(1,1) PRIMARY KEY,
        NumRequerimiento INT NOT NULL,
        CodInsumo VARCHAR(50) NOT NULL,
        CantidadRecibida DECIMAL(18,2) NOT NULL,
        Lote VARCHAR(50) NULL,
        FOREIGN KEY (NumRequerimiento) REFERENCES LIQ_REQ_Recepciones(NumRequerimiento)
    );
END
GO

-- -----------------------------------------------------------------------
-- 3. STORED PROCEDURES (LIQ_STK)
-- -----------------------------------------------------------------------

IF OBJECT_ID('dbo.LIQ_STK_SP_ListarStockActual', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_STK_SP_ListarStockActual;
GO
CREATE PROCEDURE [dbo].[LIQ_STK_SP_ListarStockActual]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        CodInsumo,
        Descripcion,
        UnidadMedida,
        StockActual,
        CONVERT(VARCHAR(10), FechaUltimaActualizacion, 103) + ' ' + CONVERT(VARCHAR(8), FechaUltimaActualizacion, 108) AS FechaModificacion
    FROM LIQ_STK_StockInsumos
    WHERE StockActual <> 0.00
    ORDER BY Descripcion ASC;
END
GO

-- -----------------------------------------------------------------------
-- 4. STORED PROCEDURES (LIQ_REQ)
-- -----------------------------------------------------------------------

IF OBJECT_ID('dbo.LIQ_REQ_SP_ConfirmarRecepcion', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_REQ_SP_ConfirmarRecepcion;
GO
CREATE PROCEDURE [dbo].[LIQ_REQ_SP_ConfirmarRecepcion]
    @NumRequerimiento INT,
    @CodOrdPro VARCHAR(50),
    @Motivo VARCHAR(20),
    @UsuarioRecepcion VARCHAR(100),
    @XMLDetalle XML
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Insertar Cabecera
        INSERT INTO LIQ_REQ_Recepciones (NumRequerimiento, CodOrdPro, Motivo, Estado, FechaRecepcion, UsuarioRecepcion)
        VALUES (@NumRequerimiento, @CodOrdPro, @Motivo, 'Recibido', GETDATE(), @UsuarioRecepcion);

        -- 2. Insertar Detalle
        INSERT INTO LIQ_REQ_RecepcionesDetalle (NumRequerimiento, CodInsumo, CantidadRecibida, Lote)
        SELECT 
            @NumRequerimiento,
            T.c.value('(CodInsumo)[1]', 'VARCHAR(50)'),
            T.c.value('(Cantidad)[1]', 'DECIMAL(18,2)'),
            T.c.value('(Lote)[1]', 'VARCHAR(50)')
        FROM @XMLDetalle.nodes('/Detalles/Detalle') AS T(c);

        -- 3. Afectar Kardex y Stock
        DECLARE @CodInsumo VARCHAR(50);
        DECLARE @Descripcion VARCHAR(250);
        DECLARE @Cantidad DECIMAL(18,2);
        DECLARE @Lote VARCHAR(50);
        DECLARE @UM VARCHAR(20);
        
        DECLARE curDetalle CURSOR FOR 
        SELECT 
            T.c.value('(CodInsumo)[1]', 'VARCHAR(50)'),
            T.c.value('(Descripcion)[1]', 'VARCHAR(250)'),
            T.c.value('(Cantidad)[1]', 'DECIMAL(18,2)'),
            T.c.value('(UM)[1]', 'VARCHAR(20)')
        FROM @XMLDetalle.nodes('/Detalles/Detalle') AS T(c);

        OPEN curDetalle;
        FETCH NEXT FROM curDetalle INTO @CodInsumo, @Descripcion, @Cantidad, @UM;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            IF ISNULL(@UM, '') = '' SET @UM = 'KG'; -- Default
            
            -- Si el insumo no existe en el stock local, lo creamos pero con StockActual = 0 
            -- (el stock recibido se suma dinámicamente desde LIQ_REQ_RecepcionesDetalle)
            IF NOT EXISTS (SELECT 1 FROM LIQ_STK_StockInsumos WHERE CodInsumo = @CodInsumo)
            BEGIN
                INSERT INTO LIQ_STK_StockInsumos (CodInsumo, Descripcion, UnidadMedida, StockActual, FechaUltimaActualizacion, UsuarioUltimaActualizacion)
                VALUES (@CodInsumo, @Descripcion, @UM, 0.00, GETDATE(), @UsuarioRecepcion);

                -- Registrar en Kardex
                INSERT INTO LIQ_STK_Kardex (CodInsumo, TipoMovimiento, Concepto, Cantidad, StockResultante, ReferenciaID, FechaMovimiento, Usuario)
                VALUES (@CodInsumo, 'Entrada', 'Recepción Inicial Req', @Cantidad, @Cantidad, CAST(@NumRequerimiento AS VARCHAR), GETDATE(), @UsuarioRecepcion);
            END
            ELSE
            BEGIN
                -- Si ya existe, NO actualizamos el StockActual porque esto solo guarda la Carga Inicial.
                -- Solo actualizamos la fecha de última modificación.
                UPDATE LIQ_STK_StockInsumos SET 
                    FechaUltimaActualizacion = GETDATE(),
                    UsuarioUltimaActualizacion = @UsuarioRecepcion
                WHERE CodInsumo = @CodInsumo;

                -- Registrar en Kardex
                INSERT INTO LIQ_STK_Kardex (CodInsumo, TipoMovimiento, Concepto, Cantidad, StockResultante, ReferenciaID, FechaMovimiento, Usuario)
                VALUES (@CodInsumo, 'Entrada', 'Recepción Requerimiento', @Cantidad, 0, CAST(@NumRequerimiento AS VARCHAR), GETDATE(), @UsuarioRecepcion);
            END

            FETCH NEXT FROM curDetalle INTO @CodInsumo, @Descripcion, @Cantidad, @UM;
        END

        CLOSE curDetalle;
        DEALLOCATE curDetalle;

        COMMIT TRANSACTION;
        SELECT 1 AS Resultado, 'Recepción confirmada y stock actualizado correctamente.' AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0 AS Resultado, ERROR_MESSAGE() AS Mensaje;
    END CATCH
END
GO

-- -----------------------------------------------------------------------
-- 5. REGISTRAR CARGA INICIAL (LIQ_STK)
-- -----------------------------------------------------------------------

IF OBJECT_ID('dbo.LIQ_STK_SP_RegistrarCargaInicial', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_STK_SP_RegistrarCargaInicial;
GO
CREATE PROCEDURE [dbo].[LIQ_STK_SP_RegistrarCargaInicial]
    @CodInsumo VARCHAR(50),
    @Descripcion VARCHAR(250),
    @UnidadMedida VARCHAR(20),
    @PesoGramos DECIMAL(18,4),
    @Usuario VARCHAR(100),
    @NPDirigida VARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Validar Unidad de Medida por defecto
        IF ISNULL(@UnidadMedida, '') = '' SET @UnidadMedida = 'KG';

        -- El peso ya viene en la unidad correcta según la UI (KG o gr), no se convierte
        DECLARE @StockIncremento DECIMAL(18,2) = @PesoGramos;

        DECLARE @NuevoStock DECIMAL(18,2) = 0.00;

        IF NOT EXISTS (SELECT 1 FROM LIQ_STK_StockInsumos WHERE CodInsumo = @CodInsumo)
        BEGIN
            SET @NuevoStock = @StockIncremento;
            INSERT INTO LIQ_STK_StockInsumos (CodInsumo, Descripcion, UnidadMedida, StockActual, FechaUltimaActualizacion, UsuarioUltimaActualizacion)
            VALUES (@CodInsumo, @Descripcion, @UnidadMedida, @NuevoStock, GETDATE(), @Usuario);
        END
        ELSE
        BEGIN
            SELECT @NuevoStock = StockActual + @StockIncremento FROM LIQ_STK_StockInsumos WHERE CodInsumo = @CodInsumo;

            UPDATE LIQ_STK_StockInsumos SET
                StockActual = @NuevoStock,
                FechaUltimaActualizacion = GETDATE(),
                UsuarioUltimaActualizacion = @Usuario
            WHERE CodInsumo = @CodInsumo;
        END

        -- Registrar en Kardex
        INSERT INTO LIQ_STK_Kardex (CodInsumo, TipoMovimiento, Concepto, Cantidad, StockResultante, ReferenciaID, Observaciones, FechaMovimiento, Usuario)
        VALUES (@CodInsumo, 'Entrada', 'Carga Inicial', @StockIncremento, @NuevoStock, @NPDirigida, 'Carga inicial simulada', GETDATE(), @Usuario);

        -- Lógica de Recarga Dirigida a una NP
        -- Lógica de Recarga Dirigida a una NP
        IF ISNULL(@NPDirigida, '') <> ''
        BEGIN
            DECLARE @BaseNP VARCHAR(100) = @NPDirigida;
            DECLARE @BaseItem VARCHAR(100) = '0000';
            
            IF CHARINDEX('-', @NPDirigida) > 0
            BEGIN
                DECLARE @idx INT = CHARINDEX('-', REVERSE(@NPDirigida));
                SET @BaseNP = SUBSTRING(@NPDirigida, 1, LEN(@NPDirigida) - @idx);
                SET @BaseItem = SUBSTRING(@NPDirigida, LEN(@NPDirigida) - @idx + 2, LEN(@NPDirigida));
            END

            -- LIQ_NP_StockSnapshot almacena siempre en GRAMOS
            DECLARE @IncrementoGramos DECIMAL(18,4) = @StockIncremento;
            IF UPPER(LTRIM(RTRIM(@UnidadMedida))) = 'KG'
            BEGIN
                SET @IncrementoGramos = @StockIncremento * 1000.0;
            END

            IF EXISTS (SELECT 1 FROM LIQ_NP_StockSnapshot WHERE NP = @BaseNP AND CodInsumo = @CodInsumo AND Item = @BaseItem)
            BEGIN
                UPDATE LIQ_NP_StockSnapshot
                SET StockOperativoInicial = StockOperativoInicial + @IncrementoGramos,
                    FechaCaptura = GETDATE()
                WHERE NP = @BaseNP AND CodInsumo = @CodInsumo AND Item = @BaseItem;
            END
            ELSE
            BEGIN
                INSERT INTO LIQ_NP_StockSnapshot (NP, CodInsumo, StockOperativoInicial, FechaCaptura, Item)
                VALUES (@BaseNP, @CodInsumo, @IncrementoGramos, GETDATE(), @BaseItem);
            END
        END

        COMMIT TRANSACTION;
        SELECT 1 AS Resultado, 'Carga inicial registrada correctamente.' AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0 AS Resultado, ERROR_MESSAGE() AS Mensaje;
    END CATCH
END
GO
