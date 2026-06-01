USE [HIALPESA]
GO

IF OBJECT_ID('dbo.LIQ_SP_AuditoriaDevolucionCentral', 'P') IS NOT NULL 
    DROP PROCEDURE dbo.LIQ_SP_AuditoriaDevolucionCentral;
GO

CREATE PROCEDURE [dbo].[LIQ_SP_AuditoriaDevolucionCentral]
    @NP VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Se obtienen las devoluciones ya calculadas y registradas para el Almacén Central durante el cierre
        SELECT 
            O.CodInsumo AS CodigoInsumo, 
            ISNULL((SELECT TOP 1 Descripcion FROM LIQ_FormulaInsumos WHERE CodigoInsumo = O.CodInsumo), 'Sin Descripción') AS Descripcion,
            SUM(O.Cantidad) AS Cantidad
        FROM LIQ_OperacionesDetalle O
        WHERE O.NP = @NP
          AND O.TipoOperacion = 'Devolucion'
          AND O.Motivo LIKE 'Almacén Central%'
        GROUP BY O.CodInsumo
        HAVING SUM(O.Cantidad) > 0;
    END TRY
    BEGIN CATCH
        SELECT ERROR_MESSAGE() AS Mensaje;
    END CATCH
END
GO
