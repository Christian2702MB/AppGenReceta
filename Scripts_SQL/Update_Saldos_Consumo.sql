-- =======================================================================
-- MÓDULO: LIQUIDACIÓN DE INSUMOS DE ESTAMPADO
-- ACTUALIZACIÓN: CONSUMO SEGREGADO (Stock Inicial vs Solicitud)
-- =======================================================================

USE [HIALPESA]
GO

-- 1. Alterar tabla LIQ_OperacionesDetalle
IF COL_LENGTH('LIQ_OperacionesDetalle', 'FuenteConsumo') IS NULL
BEGIN
    ALTER TABLE LIQ_OperacionesDetalle
    ADD FuenteConsumo VARCHAR(50) DEFAULT 'Stock Inicial';
END
GO

-- 2. Actualizar SP_RegistrarOperacion
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
    @FuenteConsumo VARCHAR(50) = 'Stock Inicial'
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Insertar en tabla de operaciones
        INSERT INTO LIQ_OperacionesDetalle (NP, CodInsumo, TipoOperacion, Cantidad, Motivo, MermaReutilizada, FechaRegistro, UsuarioRegistro, FuenteConsumo)
        VALUES (@NP, @CodInsumo, @TipoOperacion, @Cantidad, @Motivo, @MermaReutilizada, GETDATE(), @Usuario, @FuenteConsumo);

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

-- 3. Actualizar SP_ObtenerLiquidacionesConsolidadas
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
        'gr' AS UM,
        ISNULL(I.Cantidad, 0) AS Requerido,
        
        -- Sumatorias Generales
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ingreso'), 0) + ISNULL(I.Cantidad, 0) AS Entregado,
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo'), 0) AS Consumido,
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Devolucion'), 0) AS Devuelto,
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Merma'), 0) AS Merma,
        
        -- Consumos Segregados (Nuevos campos)
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo' AND FuenteConsumo = 'Stock Inicial'), 0) AS ConsumidoInicial,
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo' AND FuenteConsumo = 'Solicitud Realizada'), 0) AS ConsumidoSolicitud,

        -- Trazabilidad
        ISNULL((SELECT TOP 1 Motivo FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo ORDER BY IdOperacion DESC), 'Entrega Inicial') AS Trazabilidad
    FROM LIQ_Formulas F
    INNER JOIN LIQ_FormulaColores C ON F.IdFormula = C.IdFormula
    INNER JOIN LIQ_FormulaInsumos I ON C.IdFormulaColor = I.IdFormulaColor
    WHERE F.Eliminado = 0 AND F.Estado = @Estado;
END
GO
