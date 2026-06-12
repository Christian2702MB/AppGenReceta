-- =======================================================================
-- SP: LIQ_SP_ObtenerAjustesPorInsumo
-- Descripción: Obtiene el histórico de movimientos tipo 'Ajuste'
-- =======================================================================

USE [HIALPESA]
GO

IF OBJECT_ID('dbo.LIQ_SP_ObtenerAjustesPorInsumo', 'P') IS NOT NULL 
    DROP PROCEDURE dbo.LIQ_SP_ObtenerAjustesPorInsumo;
GO

CREATE PROCEDURE [dbo].[LIQ_SP_ObtenerAjustesPorInsumo]
    @NP VARCHAR(20),
    @CodInsumo VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        IdOperacion AS IdAjuste,
        Cantidad,
        Motivo,
        CASE 
            WHEN ISNULL(FuenteConsumo, '') = 'Stock Merma' AND ISNULL(MermaReutilizada, '') != '' 
            THEN 'Stock Merma (' + MermaReutilizada + ')'
            ELSE ISNULL(FuenteConsumo, 'Sin especificar')
        END AS Fuente,
        FechaRegistro,
        UsuarioRegistro
    FROM 
        LIQ_OperacionesDetalle
    WHERE 
        TipoOperacion = 'Ajuste'
        AND NP = @NP 
        AND CodInsumo = @CodInsumo
    ORDER BY 
        FechaRegistro DESC;
END
GO
