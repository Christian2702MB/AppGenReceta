USE [HIALPESA]
GO

IF OBJECT_ID('LIQ_SP_CrearRecepcionBlanco', 'P') IS NOT NULL
    DROP PROCEDURE LIQ_SP_CrearRecepcionBlanco
GO

CREATE PROCEDURE [dbo].[LIQ_SP_CrearRecepcionBlanco]
    @NP VARCHAR(50),
    @Usuario VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @NuevoReq INT;
    DECLARE @CodInsumo VARCHAR(50) = 'INS-GENERICO';

    -- Obtener el siguiente número de requerimiento en el rango 9,000,000+
    SELECT @NuevoReq = ISNULL(MAX(NumRequerimiento), 8999999) + 1 
    FROM LIQ_REQ_Recepciones 
    WHERE NumRequerimiento >= 9000000;

    -- En lugar de usar un insumo real que cause confusión en auditoría (UX),
    -- usamos un código ficticio estandarizado para habilitaciones manuales.
    SET @CodInsumo = 'HAB-000000';

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Insertar Cabecera
        INSERT INTO LIQ_REQ_Recepciones (NumRequerimiento, CodOrdPro, Motivo, Estado, FechaRecepcion, UsuarioRecepcion, Observaciones)
        VALUES (@NuevoReq, @NP, '30', 'Recibido', GETDATE(), @Usuario, 'Habilitación Manual Control Operativo');

        -- Insertar Detalle con Cantidad 0
        INSERT INTO LIQ_REQ_RecepcionesDetalle (NumRequerimiento, CodInsumo, CantidadRecibida, Lote)
        VALUES (@NuevoReq, @CodInsumo, 0, NULL);

        COMMIT TRANSACTION;
        SELECT @NuevoReq AS NumRequerimiento, 'Habilitación completada exitosamente.' AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SELECT 0 AS NumRequerimiento, ERROR_MESSAGE() AS Mensaje;
    END CATCH
END
GO
