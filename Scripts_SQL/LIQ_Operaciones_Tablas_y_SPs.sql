-- =======================================================================
-- MÓDULO: LIQUIDACIÓN DE INSUMOS DE ESTAMPADO
-- SUB-MÓDULO: CONTROL OPERATIVO (LIQ_OPE)
-- =======================================================================

USE [HIALPESA]
GO

-- -----------------------------------------------------------------------
-- 1. TABLAS DE OPERACIONES Y MERMAS
-- -----------------------------------------------------------------------

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LIQ_OperacionesDetalle')
BEGIN
    CREATE TABLE LIQ_OperacionesDetalle (
        IdOperacion INT IDENTITY(1,1) PRIMARY KEY,
        NP VARCHAR(20) NOT NULL,
        CodInsumo VARCHAR(50) NOT NULL,
        TipoOperacion VARCHAR(20) NOT NULL, -- 'Consumo', 'Merma', 'Ingreso', 'Devolucion'
        Cantidad DECIMAL(18,4) NOT NULL DEFAULT 0.00,
        Motivo VARCHAR(250) NULL,
        MermaReutilizada VARCHAR(50) NULL, -- Si reutilizó código de merma
        LoteVencimiento VARCHAR(100) NULL,
        FechaRegistro DATETIME NOT NULL DEFAULT GETDATE(),
        UsuarioRegistro VARCHAR(100) NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LIQ_MermasStock')
BEGIN
    CREATE TABLE LIQ_MermasStock (
        CodigoMerma VARCHAR(50) NOT NULL PRIMARY KEY,
        NPOrigen VARCHAR(20) NOT NULL,
        CodInsumo VARCHAR(50) NOT NULL,
        Descripcion VARCHAR(250) NULL,
        Tecnica VARCHAR(100) NULL,
        CantidadOriginal DECIMAL(18,4) NOT NULL,
        CantidadDisponible DECIMAL(18,4) NOT NULL,
        UM VARCHAR(20) DEFAULT 'gr',
        FechaGeneracion DATETIME NOT NULL DEFAULT GETDATE(),
        FechaVencimiento DATETIME NULL,
        Estado VARCHAR(20) DEFAULT 'Disponible'
    );
END
GO

-- -----------------------------------------------------------------------
-- 2. STORED PROCEDURES (OPERACIONES)
-- -----------------------------------------------------------------------

-- 2.1 Registrar Operación Individual
IF OBJECT_ID('dbo.LIQ_SP_RegistrarOperacion', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_RegistrarOperacion;
GO
CREATE PROCEDURE [dbo].[LIQ_SP_RegistrarOperacion]
    @NP VARCHAR(20),
    @CodInsumo VARCHAR(50),
    @TipoOperacion VARCHAR(20),
    @Cantidad DECIMAL(18,4),
    @Motivo VARCHAR(250),
    @MermaReutilizada VARCHAR(50),
    @Usuario VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Insertar en tabla de operaciones
        INSERT INTO LIQ_OperacionesDetalle (NP, CodInsumo, TipoOperacion, Cantidad, Motivo, MermaReutilizada, FechaRegistro, UsuarioRegistro)
        VALUES (@NP, @CodInsumo, @TipoOperacion, @Cantidad, @Motivo, @MermaReutilizada, GETDATE(), @Usuario);

        -- 2. Lógica Específica por Tipo
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

-- 2.2 Obtener Datos Agrupados para la Vista Maestro-Detalle
IF OBJECT_ID('dbo.LIQ_SP_ObtenerLiquidacionesConsolidadas', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_ObtenerLiquidacionesConsolidadas;
GO
CREATE PROCEDURE [dbo].[LIQ_SP_ObtenerLiquidacionesConsolidadas]
    @Estado VARCHAR(20) -- 'Activa' o 'Cerrada'
AS
BEGIN
    SET NOCOUNT ON;

    -- Resultado 1: Cabeceras
    SELECT 
        IdFormula, NP, Cliente, Estilo, Combo, Estado, 
        CONVERT(VARCHAR(10), FechaCreacion, 103) AS FechaCreacion,
        ISNULL(FechaCierre, '--') AS FechaCierre
    FROM LIQ_Formulas
    WHERE Eliminado = 0 AND Estado = @Estado;

    -- Resultado 2: Colores e Insumos con Saldos Consolidados
    SELECT 
        F.NP,
        C.NombreColor AS Pantone,
        I.CodigoInsumo,
        I.Descripcion AS NombreInsumo,
        F.Tecnica,
        'gr' AS UM, -- Asumido estándar
        ISNULL(I.Cantidad, 0) AS Requerido,
        
        -- Sumatorias desde Operaciones
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ingreso'), 0) + ISNULL(I.Cantidad, 0) AS Entregado,
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo'), 0) AS Consumido,
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Devolucion'), 0) AS Devuelto,
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Merma'), 0) AS Merma,
        
        -- Trazabilidad: Última acción registrada
        ISNULL((SELECT TOP 1 Motivo FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo ORDER BY IdOperacion DESC), 'Entrega Inicial') AS Trazabilidad
    FROM LIQ_Formulas F
    INNER JOIN LIQ_FormulaColores C ON F.IdFormula = C.IdFormula
    INNER JOIN LIQ_FormulaInsumos I ON C.IdFormulaColor = I.IdFormulaColor
    WHERE F.Eliminado = 0 AND F.Estado = @Estado;
END
GO

-- 2.3 Obtener Stock de Mermas Reutilizables
IF OBJECT_ID('dbo.LIQ_SP_ObtenerMermasStock', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_ObtenerMermasStock;
GO
CREATE PROCEDURE [dbo].[LIQ_SP_ObtenerMermasStock]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        CodigoMerma AS Codigo,
        Descripcion,
        Tecnica,
        NPOrigen,
        CONVERT(VARCHAR(10), FechaGeneracion, 103) AS FechaGeneracion,
        ISNULL(CONVERT(VARCHAR(10), FechaVencimiento, 103), '--') AS FechaVencimiento,
        CantidadDisponible,
        UM,
        Estado
    FROM LIQ_MermasStock
    WHERE Estado = 'Disponible' AND CantidadDisponible > 0
    ORDER BY FechaGeneracion DESC;
END
GO
