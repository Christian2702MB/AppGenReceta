USE [HIALPESA]
GO

IF OBJECT_ID('dbo.LIQ_SP_CrearFormulaDesdeReceta', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_CrearFormulaDesdeReceta;
GO

CREATE PROCEDURE [dbo].[LIQ_SP_CrearFormulaDesdeReceta]
    @IdRecetaOrigen INT,
    @UsuarioCreacion VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION

        -- 1. Copiar cabecera desde AGR_Recetas
        INSERT INTO LIQ_Formulas (
            IdRecetaOrigen, NP, Cliente, Temporada, Estilo, EstiloPropio,
            Item, Combo, Ubicacion, Tecnica, OperarioUDP, FechaUDP,
            Prendas, Arte, UsuarioCreacion, FechaCreacion, Estado
        )
        SELECT
            IdRecetas, NP, Cliente, Temporada, Estilo, EstiloPropio,
            Item, Combo, Ubicacion, Tecnica, OperarioUDP,
            CONVERT(VARCHAR, FechaUDP, 103),
            Prendas, Arte, @UsuarioCreacion, GETDATE(), 'Activa'
        FROM AGR_Recetas
        WHERE IdRecetas = @IdRecetaOrigen;

        DECLARE @IdFormula INT = SCOPE_IDENTITY();

        -- 2. Copiar colores
        DECLARE @MapColores TABLE (IdColorOriginal INT, IdFormulaColorNuevo INT, NombreColor VARCHAR(100));

        INSERT INTO LIQ_FormulaColores (IdFormula, NombreColor, Combo)
        OUTPUT inserted.IdFormulaColor, inserted.NombreColor INTO @MapColores(IdFormulaColorNuevo, NombreColor)
        SELECT
            @IdFormula, C.NombreColor, C.Combo
        FROM AGR_Colores C
        WHERE C.IdRecetas = @IdRecetaOrigen;

        -- Mapear IDs originales a nuevos
        UPDATE M SET M.IdColorOriginal = C.IdColor
        FROM @MapColores M
        INNER JOIN AGR_Colores C ON C.NombreColor = M.NombreColor AND C.IdRecetas = @IdRecetaOrigen;

        -- 3. Copiar insumos cruzando con el mapeo de colores
        -- IMPORTANTE: Solo tomamos la prueba principal (EsPrincipal = 1) para obtener la Cantidad correcta
        INSERT INTO LIQ_FormulaInsumos (IdFormulaColor, CodigoInsumo, Descripcion, Cantidad)
        SELECT
            MC.IdFormulaColorNuevo,
            I.CodigoInsumo,
            I.Descripcion,
            ISNULL(P.GramosUDP, 0) -- Usamos los gramos de la prueba principal, si no tiene, es 0
        FROM AGR_Insumos I
        INNER JOIN @MapColores MC ON MC.IdColorOriginal = I.IdColor
        LEFT JOIN AGR_Insumos_Prueba P 
               ON P.IdRecetas = @IdRecetaOrigen 
              AND P.NombreColor = MC.NombreColor 
              AND P.CodigoInsumo = I.CodigoInsumo 
              AND P.EsPrincipal = 1;

        /* 
        -- 4. Copiar pruebas UDP (A petición del usuario, las pruebas no se arrastran, solo la cantidad a la base)
        INSERT INTO LIQ_FormulaInsumosPrueba (IdFormula, NombreColor, CodigoInsumo, NombrePrueba, GramosUDP, EsPrincipal)
        SELECT
            @IdFormula, P.NombreColor, P.CodigoInsumo, P.NombrePrueba, P.GramosUDP, P.EsPrincipal
        FROM AGR_Insumos_Prueba P
        WHERE P.IdRecetas = @IdRecetaOrigen
          AND P.EsPrincipal = 1;
        */

        COMMIT TRANSACTION;

        -- Retornar el ID de la nueva fórmula
        SELECT @IdFormula AS IdFormula, 1 AS Resultado;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMsg, 16, 1);
    END CATCH
END
GO
