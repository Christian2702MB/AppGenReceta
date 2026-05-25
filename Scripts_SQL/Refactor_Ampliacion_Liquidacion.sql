-- =======================================================================
-- MÓDULO: LIQUIDACIÓN DE INSUMOS DE ESTAMPADO
-- ACTUALIZACIÓN: REFACTORIZACIÓN Y AMPLIACIÓN (CABECERA, MERMA COLOR, AJUSTE, TRAZABILIDAD)
-- =======================================================================

USE [HIALPESA]
GO

-- 1. MODIFICAR TABLA DE OPERACIONES PARA TRAZABILIDAD POR COLOR
IF COL_LENGTH('LIQ_OperacionesDetalle', 'NombreColor') IS NULL
BEGIN
    ALTER TABLE LIQ_OperacionesDetalle
    ADD NombreColor VARCHAR(100) NULL;
END
GO

-- 2. CREAR TABLA PARA MERMA POR COLOR
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LIQ_MER_MermasColor')
BEGIN
    CREATE TABLE LIQ_MER_MermasColor (
        IdMermaColor INT IDENTITY(1,1) PRIMARY KEY,
        IdFormulaColor INT NULL,
        NP VARCHAR(20) NOT NULL,
        NombreColor VARCHAR(100) NOT NULL,
        Gramos DECIMAL(18,4) NOT NULL,
        FechaVencimiento DATETIME NULL,
        CodigoMerma VARCHAR(50) NULL,
        FechaRegistro DATETIME DEFAULT GETDATE(),
        UsuarioRegistro VARCHAR(100) NULL
    );
END
GO

-- 3. ACTUALIZAR SP DE REGISTRO DE OPERACIÓN (Incluyendo parámetro Color)
IF OBJECT_ID('dbo.LIQ_SP_RegistrarOperacion', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_RegistrarOperacion;
GO
CREATE PROCEDURE [dbo].[LIQ_SP_RegistrarOperacion]
    @NP VARCHAR(20),
    @CodInsumo VARCHAR(50),
    @TipoOperacion VARCHAR(20),
    @Cantidad DECIMAL(18,4),
    @Motivo VARCHAR(250),
    @MermaReutilizada VARCHAR(50),
    @Usuario VARCHAR(100),
    @FuenteConsumo VARCHAR(50),
    @NombreColor VARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Insertar en tabla de operaciones
        INSERT INTO LIQ_OperacionesDetalle (NP, CodInsumo, TipoOperacion, Cantidad, Motivo, MermaReutilizada, FechaRegistro, UsuarioRegistro, FuenteConsumo, NombreColor)
        VALUES (@NP, @CodInsumo, @TipoOperacion, @Cantidad, @Motivo, @MermaReutilizada, GETDATE(), @Usuario, @FuenteConsumo, @NombreColor);

        -- Lógica Específica por Tipo
        IF @TipoOperacion = 'Merma'
        BEGIN
            -- Generar Código de Merma (Ej. MER-2405-01)
            DECLARE @CodMerma VARCHAR(50) = 'MER-' + RIGHT(CONVERT(VARCHAR(4), YEAR(GETDATE())), 2) + RIGHT('0' + CONVERT(VARCHAR(2), MONTH(GETDATE())), 2) + '-' + RIGHT('000' + CONVERT(VARCHAR(4), SCOPE_IDENTITY()), 4);
            
            DECLARE @DescInsumo VARCHAR(250) = '';
            DECLARE @Tecnica VARCHAR(100) = '';
            
            SELECT TOP 1 @DescInsumo = Descripcion FROM LIQ_FormulaInsumos WHERE CodigoInsumo = @CodInsumo;
            SELECT TOP 1 @Tecnica = Tecnica FROM LIQ_Formulas WHERE NP = @NP;

            INSERT INTO LIQ_MermasStock (CodigoMerma, NPOrigen, CodInsumo, Descripcion, Tecnica, CantidadOriginal, CantidadDisponible, FechaVencimiento)
            VALUES (@CodMerma, @NP, @CodInsumo, 'Merma de ' + ISNULL(@DescInsumo, @CodInsumo), @Tecnica, @Cantidad, @Cantidad, DATEADD(MONTH, 3, GETDATE()));
        END
        ELSE IF @TipoOperacion = 'Ingreso' AND ISNULL(@MermaReutilizada, '') <> ''
        BEGIN
            -- Descontar de stock de mermas
            UPDATE LIQ_MermasStock
            SET CantidadDisponible = CantidadDisponible - @Cantidad
            WHERE CodigoMerma = @MermaReutilizada;

            -- Si llega a cero o menos, marcar como agotado
            UPDATE LIQ_MermasStock
            SET Estado = 'Agotado'
            WHERE CodigoMerma = @MermaReutilizada AND CantidadDisponible <= 0;
        END

        COMMIT TRANSACTION;
        SELECT 1 AS Resultado, 'Operación registrada correctamente.' AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0 AS Resultado, ERROR_MESSAGE() AS Mensaje;
    END CATCH
END
GO

-- 4. ACTUALIZAR SP DE CONSOLIDADOS (Sin Combo, Con Temporada y Estilo Propio)
IF OBJECT_ID('dbo.LIQ_SP_ObtenerLiquidacionesConsolidadas', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_ObtenerLiquidacionesConsolidadas;
GO
CREATE PROCEDURE [dbo].[LIQ_SP_ObtenerLiquidacionesConsolidadas]
    @Estado VARCHAR(20) -- 'Activa' o 'Cerrada'
AS
BEGIN
    SET NOCOUNT ON;

    -- Resultado 1: Cabeceras
    SELECT 
        F.IdFormula, F.NP, F.Cliente, F.Estilo, F.Temporada, F.EstiloPropio, F.Estado, 
        CONVERT(VARCHAR(10), F.FechaCreacion, 103) AS FechaCreacion,
        ISNULL(F.FechaCierre, '--') AS FechaCierre
    FROM LIQ_Formulas F
    WHERE F.Eliminado = 0 AND F.Estado = @Estado
      AND EXISTS (SELECT 1 FROM LIQ_REQ_Recepciones R WHERE R.CodOrdPro = F.NP);

    -- Resultado 2: Colores e Insumos con Saldos Consolidados
    SELECT 
        F.NP,
        C.NombreColor AS Pantone,
        I.CodigoInsumo,
        I.Descripcion AS NombreInsumo,
        F.Tecnica,
        'gr' AS UM,
        ISNULL(I.Cantidad, 0) AS Requerido,
        
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo'), 0) AS Consumido,
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Devolucion'), 0) AS Devuelto,
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Merma'), 0) AS Merma,
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste'), 0) AS Ajuste,
        
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo' AND FuenteConsumo = 'Stock Inicial'), 0) AS ConsumidoInicial,
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo' AND FuenteConsumo = 'Solicitud Realizada'), 0) AS ConsumidoSolicitud,

        ISNULL((SELECT TOP 1 Motivo FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo ORDER BY IdOperacion DESC), 'Entrega Inicial') AS Trazabilidad
    FROM LIQ_Formulas F
    INNER JOIN LIQ_FormulaColores C ON F.IdFormula = C.IdFormula
    INNER JOIN LIQ_FormulaInsumos I ON C.IdFormulaColor = I.IdFormulaColor
    WHERE F.Eliminado = 0 AND F.Estado = @Estado
      AND EXISTS (SELECT 1 FROM LIQ_REQ_Recepciones R WHERE R.CodOrdPro = F.NP);
END
GO

-- 5. CREAR SP PARA OBTENER NPs PENDIENTES DE LIQUIDAR
IF OBJECT_ID('dbo.LIQ_SP_ObtenerNPsPendientes', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_ObtenerNPsPendientes;
GO
CREATE PROCEDURE [dbo].[LIQ_SP_ObtenerNPsPendientes]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        F.NP, 
        F.Cliente, 
        F.Temporada, 
        F.Estilo, 
        F.EstiloPropio
    FROM LIQ_Formulas F
    WHERE F.Eliminado = 0 AND F.Estado <> 'Terminado'
      AND EXISTS (SELECT 1 FROM LIQ_REQ_Recepciones R WHERE R.CodOrdPro = F.NP);
END
GO

-- 6. CREAR SP PARA REGISTRAR MERMA POR COLOR
IF OBJECT_ID('dbo.LIQ_SP_RegistrarMermaColor', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_RegistrarMermaColor;
GO
CREATE PROCEDURE [dbo].[LIQ_SP_RegistrarMermaColor]
    @NP VARCHAR(20),
    @NombreColor VARCHAR(100),
    @Gramos DECIMAL(18,4),
    @FechaVencimiento DATETIME = NULL,
    @Usuario VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Determinar Color ID si aplica
        DECLARE @IdFormulaColor INT = NULL;
        SELECT TOP 1 @IdFormulaColor = C.IdFormulaColor 
        FROM LIQ_FormulaColores C
        INNER JOIN LIQ_Formulas F ON C.IdFormula = F.IdFormula
        WHERE F.NP = @NP AND C.NombreColor = @NombreColor;

        INSERT INTO LIQ_MER_MermasColor (IdFormulaColor, NP, NombreColor, Gramos, FechaVencimiento, UsuarioRegistro)
        VALUES (@IdFormulaColor, @NP, @NombreColor, @Gramos, @FechaVencimiento, @Usuario);
        
        DECLARE @IdGenerado INT = SCOPE_IDENTITY();
        DECLARE @CodMerma VARCHAR(50) = 'MER-' + CONVERT(VARCHAR(4), YEAR(GETDATE())) + '-' + RIGHT('000' + CONVERT(VARCHAR(4), @IdGenerado), 4);
        
        UPDATE LIQ_MER_MermasColor
        SET CodigoMerma = @CodMerma
        WHERE IdMermaColor = @IdGenerado;

        COMMIT TRANSACTION;
        SELECT 1 AS Resultado, 'Merma por color registrada con código ' + @CodMerma AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0 AS Resultado, ERROR_MESSAGE() AS Mensaje;
    END CATCH
END
GO
