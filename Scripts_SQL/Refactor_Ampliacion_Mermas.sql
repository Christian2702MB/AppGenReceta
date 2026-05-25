-- =======================================================================
-- SP: LIQ_MER_SP_ObtenerMermasPorColor
-- Descripción: Obtiene el histórico de mermas registradas para una OP y Color
-- =======================================================================

USE [HIALPESA]
GO

IF OBJECT_ID('dbo.LIQ_MER_SP_ObtenerMermasPorColor', 'P') IS NOT NULL 
    DROP PROCEDURE dbo.LIQ_MER_SP_ObtenerMermasPorColor;
GO

CREATE PROCEDURE [dbo].[LIQ_MER_SP_ObtenerMermasPorColor]
    @NP VARCHAR(20),
    @NombreColor VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        IdMermaColor,
        CodigoMerma,
        Gramos AS Cantidad,
        FechaVencimiento,
        FechaRegistro,
        UsuarioRegistro,
        CASE 
            WHEN Gramos = 0 AND CodigoMerma = 'NO_MERMA' THEN 'No existe merma'
            ELSE 'Registrado'
        END AS EstadoIndicador
    FROM 
        LIQ_MER_MermasColor
    WHERE 
        NP = @NP 
        AND NombreColor = @NombreColor
    ORDER BY 
        FechaRegistro DESC;
END
GO
