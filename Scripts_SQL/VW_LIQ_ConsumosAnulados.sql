-- =========================================================================
-- VISTA DE AUDITORÍA: CONSUMOS ANULADOS
-- Muestra el historial completo de los consumos de desarrollo (u otros) 
-- que fueron anulados a través de la aplicación web.
-- =========================================================================

IF OBJECT_ID('dbo.VW_LIQ_ConsumosAnulados', 'V') IS NOT NULL
    DROP VIEW dbo.VW_LIQ_ConsumosAnulados;
GO

CREATE VIEW [dbo].[VW_LIQ_ConsumosAnulados]
AS
SELECT 
    OD.IdOperacion,
    OD.NP,
    OD.CodInsumo,
    ISNULL(I.Descripcion, 'N/A') AS DescripcionInsumo,
    OD.NombreColor,
    OD.IdVisita,
    OD.Cantidad AS CantidadAnulada,
    OD.FuenteConsumo,
    OD.UsuarioRegistro AS UsuarioAnulacion,
    OD.FechaRegistro AS FechaAnulacion,
    -- Limpieza del motivo: Si contiene el prefijo 'ANULADO: ', lo extraemos
    CASE 
        WHEN OD.Motivo LIKE 'ANULADO: % | Por:%' 
        THEN SUBSTRING(OD.Motivo, 10, CHARINDEX(' | Por:', OD.Motivo) - 10)
        ELSE OD.Motivo 
    END AS MotivoAnulacion,
    -- Traza original completa por si se requiere
    OD.Motivo AS TrazaOriginalSistema
FROM 
    [dbo].[LIQ_OperacionesDetalle] OD
LEFT JOIN 
    LIQ_STK_StockInsumos I 
    ON OD.CodInsumo = I.CodInsumo
WHERE 
    OD.TipoOperacion = 'Consumo Anulado';
GO
