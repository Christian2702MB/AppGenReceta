-- =======================================================================
-- MODULO: LIQUIDACION DE INSUMOS DE ESTAMPADO
-- REFACTOR: OBTENER LIQUIDACIONES CON ESTADOS AGRUPADOS
-- =======================================================================

USE [HIALPESA]
GO

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
    WHERE F.Eliminado = 0 
      AND (
          (@Estado = 'Activa' AND F.Estado IN ('Activa', 'En Proceso', 'Pendiente', 'Liquidado')) OR
          (@Estado = 'Cerrada' AND F.Estado IN ('Terminado', 'Cerrada')) OR
          (@Estado NOT IN ('Activa', 'Cerrada') AND F.Estado = @Estado)
      )
      AND EXISTS (SELECT 1 FROM LIQ_REQ_Recepciones R WHERE R.CodOrdPro = F.NP);

    -- Resultado 2: Colores e Insumos con Saldos Consolidados
    SELECT 
        F.NP,
        C.NombreColor AS Pantone,
        I.CodigoInsumo,
        I.Descripcion AS NombreInsumo,
        F.Tecnica,
        'gr' AS UM,
        -- Requerido: Extraer el valor de LIQ_FormulaInsumosPrueba (EsPrincipal = 1) por Prendas
            ISNULL((
                SELECT TOP 1 GramosUDP 
                FROM LIQ_FormulaInsumosPrueba 
                WHERE IdFormula = F.IdFormula 
                  AND NombreColor = C.NombreColor 
                  AND CodigoInsumo = I.CodigoInsumo 
                  AND EsPrincipal = 1
            ), 0) * CASE WHEN ISNUMERIC(F.Prendas) = 1 THEN CAST(F.Prendas AS DECIMAL(18,2)) ELSE 1 END AS Requerido,
			        
        -- Stock Recibido: Cruce con Liquidacion Recepciones (Convertido de KG a Gramos)
        ISNULL((
            SELECT SUM(D.CantidadRecibida * 1000.00) 
            FROM LIQ_REQ_Recepciones R
            INNER JOIN LIQ_REQ_RecepcionesDetalle D ON R.NumRequerimiento = D.NumRequerimiento
            WHERE R.CodOrdPro = F.NP AND D.CodInsumo = I.CodigoInsumo
        ), 0) AS StockRecibido,
			        
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo' AND NombreColor = C.NombreColor), 0) AS Consumido,
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Devolucion'), 0) AS Devuelto,
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Merma'), 0) AS Merma,
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste' AND NombreColor = C.NombreColor), 0) AS Ajuste,
        
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo' AND FuenteConsumo = 'Stock Inicial'), 0) AS ConsumidoInicial,
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo' AND FuenteConsumo = 'Solicitud Realizada'), 0) AS ConsumidoSolicitud,

        ISNULL((SELECT TOP 1 Motivo FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo ORDER BY IdOperacion DESC), 'Entrega Inicial') AS Trazabilidad,
        
        -- Bandera de Bloqueo Merma
        CAST(CASE WHEN EXISTS (
            SELECT 1 FROM LIQ_MER_MermasColor 
            WHERE NP = F.NP AND NombreColor = C.NombreColor AND Gramos = 0 AND CodigoMerma = 'NO_MERMA'
        ) THEN 1 ELSE 0 END AS BIT) AS BloqueoMerma,
        
        -- Bandera de Bloqueo Ajuste
        CAST(CASE WHEN EXISTS (
            SELECT 1 FROM LIQ_OperacionesDetalle 
            WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste' AND Cantidad = 0 AND Motivo = 'No existe ajuste'
        ) THEN 1 ELSE 0 END AS BIT) AS BloqueoAjuste
        
    FROM LIQ_Formulas F
    INNER JOIN LIQ_FormulaColores C ON F.IdFormula = C.IdFormula
    INNER JOIN LIQ_FormulaInsumos I ON C.IdFormulaColor = I.IdFormulaColor
    WHERE F.Eliminado = 0 
      AND (
          (@Estado = 'Activa' AND F.Estado IN ('Activa', 'En Proceso', 'Pendiente', 'Liquidado')) OR
          (@Estado = 'Cerrada' AND F.Estado IN ('Terminado', 'Cerrada')) OR
          (@Estado NOT IN ('Activa', 'Cerrada') AND F.Estado = @Estado)
      )
      AND EXISTS (SELECT 1 FROM LIQ_REQ_Recepciones R WHERE R.CodOrdPro = F.NP);
END
