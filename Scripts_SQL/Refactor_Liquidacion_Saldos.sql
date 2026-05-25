-- =======================================================================
-- MÓDULO: LIQUIDACIÓN DE INSUMOS DE ESTAMPADO
-- ACTUALIZACIÓN: REFACTORIZACIÓN INTEGRAL (Eliminación 'Entregado' y Filtro Recepciones)
-- =======================================================================

USE [HIALPESA]
GO

-- 1. Actualizar SP Maestro de Liquidaciones Consolidadas
IF OBJECT_ID('dbo.LIQ_SP_ObtenerLiquidacionesConsolidadas', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_ObtenerLiquidacionesConsolidadas;
GO
CREATE PROCEDURE [dbo].[LIQ_SP_ObtenerLiquidacionesConsolidadas]
    @Estado VARCHAR(20) -- 'Activa' o 'Cerrada'
AS
BEGIN
    SET NOCOUNT ON;

    -- Resultado 1: Cabeceras (Solo fórmulas con Recepción en LIQ_REQ_Recepciones)
    SELECT 
        F.IdFormula, F.NP, F.Cliente, F.Estilo, F.Combo, F.Estado, 
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
        
        -- SE ELIMINÓ LA COLUMNA Y CÁLCULO DE 'Entregado'
        
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo'), 0) AS Consumido,
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Devolucion'), 0) AS Devuelto,
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Merma'), 0) AS Merma,
        
        -- Consumos Segregados
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo' AND FuenteConsumo = 'Stock Inicial'), 0) AS ConsumidoInicial,
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo' AND FuenteConsumo = 'Solicitud Realizada'), 0) AS ConsumidoSolicitud,

        -- Trazabilidad
        ISNULL((SELECT TOP 1 Motivo FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo ORDER BY IdOperacion DESC), 'Entrega Inicial') AS Trazabilidad
    FROM LIQ_Formulas F
    INNER JOIN LIQ_FormulaColores C ON F.IdFormula = C.IdFormula
    INNER JOIN LIQ_FormulaInsumos I ON C.IdFormulaColor = I.IdFormulaColor
    WHERE F.Eliminado = 0 AND F.Estado = @Estado
      AND EXISTS (SELECT 1 FROM LIQ_REQ_Recepciones R WHERE R.CodOrdPro = F.NP);
END
GO

-- 2. Crear SP para Popup Dinámico de Consumo Diario
IF OBJECT_ID('dbo.LIQ_SP_ObtenerSaldosPopup', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_ObtenerSaldosPopup;
GO
CREATE PROCEDURE [dbo].[LIQ_SP_ObtenerSaldosPopup]
    @NP VARCHAR(20),
    @CodInsumo VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StockInicial DECIMAL(18,4) = 0;
    DECLARE @StockSolicitud DECIMAL(18,4) = 0;
    
    -- Obtener Stock Inicial desde la tabla de inventario maestro
    -- Convirtiendo a gramos si la unidad (UM) está en kilogramos (kg)
    SELECT TOP 1 @StockInicial = 
        CASE 
            WHEN LOWER(LTRIM(RTRIM(ISNULL(UM, 'gr')))) = 'kg' THEN ISNULL(StockActual, 0) * 1000.0
            ELSE ISNULL(StockActual, 0)
        END
    FROM LIQ_STK_StockInsumos
    WHERE CodInsumo = @CodInsumo;
    
    -- Obtener Stock Solicitud sumando desde los detalles de recepción
    -- Asumiendo estructura estándar de cabecera-detalle o detalle directo con CodOrdPro
    SELECT @StockSolicitud = ISNULL(SUM(D.CantidadRecibida), 0)
    FROM LIQ_REQ_RecepcionesDetalle D
    INNER JOIN LIQ_REQ_Recepciones R ON D.IdRecepcion = R.IdRecepcion
    WHERE R.CodOrdPro = @NP AND D.CodInsumo = @CodInsumo;

    SELECT 
        @StockInicial AS StockInicial,
        @StockSolicitud AS StockSolicitud,
        (@StockInicial + @StockSolicitud) AS StockTotal;
        
    -- Result Set 2: Historial de Consumos
    SELECT 
        FechaRegistro,
        UsuarioRegistro,
        FuenteConsumo,
        Cantidad
    FROM LIQ_OperacionesDetalle
    WHERE NP = @NP AND CodInsumo = @CodInsumo AND TipoOperacion = 'Consumo'
    ORDER BY FechaRegistro DESC;
END
GO
