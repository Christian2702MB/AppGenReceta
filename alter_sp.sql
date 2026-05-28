ALTER PROCEDURE [dbo].[LIQ_SP_ObtenerMermasStock]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        M.CodigoMerma AS Codigo,
        M.NombreColor AS Descripcion,
        '' AS Tecnica,
        M.NP AS NPOrigen,
        CONVERT(VARCHAR(10), M.FechaRegistro, 103) AS FechaGeneracion,
        CONVERT(VARCHAR(10), M.FechaVencimiento, 103) AS FechaVencimiento,
        CAST(M.Gramos - ISNULL(
            (SELECT SUM(Cantidad) 
             FROM LIQ_OperacionesDetalle OD 
             WHERE OD.MermaReutilizada = M.CodigoMerma 
               AND (OD.TipoOperacion = 'Consumo' OR OD.TipoOperacion = 'Ajuste')
            ), 0) AS DECIMAL(18,2)) AS CantidadDisponible,
        'gr' AS UM,
        'Disponible' AS Estado
    FROM 
        LIQ_MER_MermasColor M
    WHERE 
        M.FechaVencimiento IS NOT NULL 
        AND M.Gramos > 0
    ORDER BY 
        M.FechaRegistro DESC;
END
