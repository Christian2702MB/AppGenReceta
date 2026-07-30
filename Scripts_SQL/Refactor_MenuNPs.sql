USE [HIALPESA]
GO

IF OBJECT_ID('dbo.LIQ_SP_ObtenerMenuNPs', 'P') IS NOT NULL 
    DROP PROCEDURE dbo.LIQ_SP_ObtenerMenuNPs;
GO

CREATE PROCEDURE [dbo].[LIQ_SP_ObtenerMenuNPs]
    @Estado VARCHAR(20) -- 'Activa' o 'Cerrada'
AS
BEGIN
    SET NOCOUNT ON;

    -- Solo devuelve las cabeceras básicas (ultra ligero, para el sidebar)
    SELECT 
        F.IdFormula, F.NP, ISNULL(F.Item, '0000') AS Item, F.Cliente, F.Estilo, F.Temporada, F.EstiloPropio, F.Estado, 
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
END
GO
