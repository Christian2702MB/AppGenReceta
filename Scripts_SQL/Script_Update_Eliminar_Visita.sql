IF OBJECT_ID('dbo.SP_ELIMINAR_VISITA', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_ELIMINAR_VISITA;
GO

CREATE PROCEDURE [dbo].[SP_ELIMINAR_VISITA]
    @IdVisita INT,
    @Comentario VARCHAR(500),
    @UsuarioElimina VARCHAR(100)
AS
BEGIN
    -- ESTA ES LA LÍNEA CLAVE PARA SOLUCIONAR EL ERROR
    SET ARITHABORT ON; 
    SET ANSI_WARNINGS ON;
    SET ANSI_PADDING ON;
    SET ANSI_NULLS ON;
    SET QUOTED_IDENTIFIER ON;
    SET CONCAT_NULL_YIELDS_NULL ON;
    SET NUMERIC_ROUNDABORT OFF;

    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRAN;

        -- 1. Insertar el registro en la tabla histórica, adjuntando quién lo eliminó y por qué
		INSERT INTO AGR_RecetasEliminadas (
            IdRecetaEliminada, NP, OperarioUDP, Tecnica, FechaUDP, Concepto, Ubicacion, Combo,
		    Cliente, Estilo, Prendas, Temporada, Item, EstiloPropio, MotivoEliminacion, 
            UsuarioEliminacion, FechaEliminacion
        )
		SELECT 
            IdRecetas, NP, OperarioUDP, Tecnica, FechaUDP, Concepto, Ubicacion, Combo, 
            Cliente, Estilo, Prendas, Temporada, Item, EstiloPropio, @Comentario, 
            @UsuarioElimina, GETDATE() 
        FROM dbo.AGR_Recetas 
        WHERE IdRecetas = @IdVisita;
		  
        -- 2. Eliminar de las tablas hijas y asociadas (en cascada manual)

        -- 2.1 NUEVO: Eliminar todos los rastros de consumos ("Consumo Desarrollo") atados a esta visita
        DELETE FROM LIQ_OperacionesDetalle 
        WHERE IdVisita = @IdVisita;

        -- 2.2 NUEVO: Eliminar el registro de las pruebas de los insumos asociadas a esta receta
        DELETE FROM AGR_Insumos_Prueba 
        WHERE IdRecetas = @IdVisita;

        -- 2.3 Eliminar los Insumos principales asociados a los colores de la receta
		DELETE FROM AGR_Insumos 
        WHERE IdColor IN (SELECT C.IdColor FROM AGR_Colores C WHERE C.IdRecetas = @IdVisita);

        -- 2.4 Eliminar los Colores de la receta
		DELETE FROM AGR_Colores 
		WHERE IdRecetas = @IdVisita;

        -- 2.5 Eliminar la Cabecera (Receta/Visita)
        DELETE FROM AGR_Recetas 
        WHERE IdRecetas = @IdVisita;

		SELECT 1 AS Resultado;
		COMMIT TRANSACTION;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRAN;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END
GO
