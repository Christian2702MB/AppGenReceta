-- =======================================================================
-- MÓDULO: MANTENIMIENTO DE PROCESO PRODUCTIVO (RECEPCIONES)
-- ACTUALIZACIÓN: REFACTORIZACIÓN EN CAPAS Y OPTIMIZACIÓN DE FECHAS
-- =======================================================================

USE [HIALPESA]
GO

-- 1. SP para listar las cabeceras con filtro de un mes por defecto
IF OBJECT_ID('dbo.LIQ_SP_ListarRecepcionesHistoricas', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_ListarRecepcionesHistoricas;
GO
CREATE PROCEDURE [dbo].[LIQ_SP_ListarRecepcionesHistoricas]
    @FechaDesde DATE = NULL,
    @FechaHasta DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Lógica de negocio en base de datos: Si no se manda fecha, se toma exactamente un mes hacia atrás
    IF @FechaDesde IS NULL
    BEGIN
        SET @FechaDesde = DATEADD(MONTH, -1, CAST(GETDATE() AS DATE));
    END

    IF @FechaHasta IS NULL
    BEGIN
        SET @FechaHasta = CAST(GETDATE() AS DATE);
    END

    SELECT 
        NumRequerimiento, 
        CodOrdPro, 
        Motivo, 
        Estado, 
        FechaRecepcion, 
        UsuarioRecepcion, 
        Observaciones 
    FROM LIQ_REQ_Recepciones 
    WHERE CONVERT(DATE, FechaRecepcion) BETWEEN @FechaDesde AND @FechaHasta 
    ORDER BY FechaRecepcion DESC;
END
GO

-- 2. SP para obtener el detalle de insumos en el popup
IF OBJECT_ID('dbo.LIQ_SP_ObtenerDetalleRecepcion', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_ObtenerDetalleRecepcion;
GO
CREATE PROCEDURE [dbo].[LIQ_SP_ObtenerDetalleRecepcion]
    @NumRequerimiento INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        D.IdRecepcionDetalle,
        D.NumRequerimiento,
        D.CodInsumo,
        ISNULL(S.Descripcion, 'Insumo sin descripción') AS NombreInsumo,
        D.CantidadRecibida,
        ISNULL(S.UnidadMedida, 'gr') AS UnidadMedida,
        D.Lote
    FROM LIQ_REQ_RecepcionesDetalle D
    LEFT JOIN LIQ_STK_StockInsumos S ON D.CodInsumo = S.CodInsumo
    WHERE D.NumRequerimiento = @NumRequerimiento
    ORDER BY S.Descripcion ASC;
END
GO
