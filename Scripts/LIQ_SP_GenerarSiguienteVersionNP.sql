USE [HIALPESA103]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =========================================================================================
-- Author:      Antigravity
-- Create date: 2026-06-15
-- Description: Clona una Fórmula Terminada (NP) a una nueva versión consecutiva (Activa),
--              manteniendo intacta la original para auditoría.
-- =========================================================================================
CREATE PROCEDURE [dbo].[LIQ_SP_GenerarSiguienteVersionNP]
    @NPOriginal VARCHAR(50),
    @Usuario VARCHAR(50),
    @Observacion VARCHAR(500),
    @NuevaNPGerada VARCHAR(50) OUTPUT,
    @Exito INT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- 1. Validar que la NP original exista y esté terminada
        IF NOT EXISTS (SELECT 1 FROM LIQ_Formulas WHERE NP = @NPOriginal AND Estado IN ('Terminado', 'Terminada'))
        BEGIN
            SET @Exito = 0;
            SET @Mensaje = 'La OP proporcionada no existe o no se encuentra en estado Terminado.';
            RETURN;
        END

        -- 2. Determinar el nuevo sufijo de versión
        DECLARE @BaseNP VARCHAR(50);
        DECLARE @CurrentMaxVersion INT = 1;

        -- Extraer la base (ej. 'I8253' de 'I8253-V2')
        IF CHARINDEX('-V', @NPOriginal) > 0
        BEGIN
            SET @BaseNP = SUBSTRING(@NPOriginal, 1, CHARINDEX('-V', @NPOriginal) - 1);
        END
        ELSE
        BEGIN
            SET @BaseNP = @NPOriginal;
        END

        -- Buscar la versión más alta actual para esta base
        SELECT @CurrentMaxVersion = ISNULL(MAX(
            CAST(SUBSTRING(NP, CHARINDEX('-V', NP) + 2, LEN(NP)) AS INT)
        ), 1)
        FROM LIQ_Formulas
        WHERE NP LIKE @BaseNP + '-V%' 
          AND ISNUMERIC(SUBSTRING(NP, CHARINDEX('-V', NP) + 2, LEN(NP))) = 1;

        -- Si la original no tenía -V, y no encontramos derivadas, la nueva es V2
        IF CHARINDEX('-V', @NPOriginal) = 0 AND @CurrentMaxVersion = 1
        BEGIN
            SET @CurrentMaxVersion = 1; 
        END

        DECLARE @NextVersion INT = @CurrentMaxVersion + 1;
        DECLARE @NuevaNP VARCHAR(50) = @BaseNP + '-V' + CAST(@NextVersion AS VARCHAR(10));

        -- Validar colisión (sanity check)
        IF EXISTS (SELECT 1 FROM LIQ_Formulas WHERE NP = @NuevaNP)
        BEGIN
            SET @Exito = 0;
            SET @Mensaje = 'Error de concurrencia: La versión ' + @NuevaNP + ' ya existe.';
            RETURN;
        END

        BEGIN TRANSACTION;

        -- 3. Clonar la cabecera (LIQ_Formulas)
        DECLARE @NuevoIdFormula INT;

        INSERT INTO LIQ_Formulas (
            IdRecetaOrigen, NP, Cliente, Temporada, Estilo, EstiloPropio, Item, Combo, 
            Ubicacion, Tecnica, OperarioUDP, FechaUDP, Prendas, Arte, 
            UsuarioCreacion, Estado, Observaciones
        )
        SELECT 
            IdRecetaOrigen, @NuevaNP, Cliente, Temporada, Estilo, EstiloPropio, Item, Combo, 
            Ubicacion, Tecnica, OperarioUDP, FechaUDP, Prendas, Arte, 
            @Usuario, 'Activa', ISNULL(NULLIF(LTRIM(RTRIM(@Observacion)), '') + ' | ', '') + 'Versión derivada de ' + @NPOriginal + '. ' + ISNULL(Observaciones, '')
        FROM LIQ_Formulas
        WHERE NP = @NPOriginal;

        SET @NuevoIdFormula = SCOPE_IDENTITY();

        -- 4. Clonar los colores (LIQ_FormulaColores)
        -- Usaremos una tabla temporal para mapear IdFormulaColor Viejo -> Nuevo
        CREATE TABLE #MapColores (
            OldId INT,
            NewId INT
        );

        DECLARE @OldIdColor INT, @NombreColor VARCHAR(100), @ComboColor VARCHAR(100);

        DECLARE curColores CURSOR FOR
        SELECT IdFormulaColor, NombreColor, Combo
        FROM LIQ_FormulaColores
        WHERE IdFormula = (SELECT IdFormula FROM LIQ_Formulas WHERE NP = @NPOriginal);

        OPEN curColores;
        FETCH NEXT FROM curColores INTO @OldIdColor, @NombreColor, @ComboColor;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            INSERT INTO LIQ_FormulaColores (IdFormula, NombreColor, Combo)
            VALUES (@NuevoIdFormula, @NombreColor, @ComboColor);

            DECLARE @NewIdColor INT = SCOPE_IDENTITY();

            INSERT INTO #MapColores (OldId, NewId) VALUES (@OldIdColor, @NewIdColor);

            FETCH NEXT FROM curColores INTO @OldIdColor, @NombreColor, @ComboColor;
        END

        CLOSE curColores;
        DEALLOCATE curColores;

        -- 5. Clonar los insumos (LIQ_FormulaInsumos) e InsumosPrueba
        INSERT INTO LIQ_FormulaInsumos (IdFormulaColor, CodigoInsumo, Descripcion, Cantidad)
        SELECT M.NewId, I.CodigoInsumo, I.Descripcion, I.Cantidad
        FROM LIQ_FormulaInsumos I
        INNER JOIN #MapColores M ON I.IdFormulaColor = M.OldId;

        INSERT INTO LIQ_FormulaInsumosPrueba (IdFormula, NombreColor, CodigoInsumo, NombrePrueba, GramosUDP, EsPrincipal)
        SELECT @NuevoIdFormula, P.NombreColor, P.CodigoInsumo, P.NombrePrueba, P.GramosUDP, P.EsPrincipal
        FROM LIQ_FormulaInsumosPrueba P
        WHERE P.IdFormula = (SELECT IdFormula FROM LIQ_Formulas WHERE NP = @NPOriginal);

        -- 6. Tomar Snapshot del Stock Global Actual para la Nueva Versión
        -- Esto garantiza que la nueva versión inicie con el saldo real de la planta en gramos.
        INSERT INTO LIQ_NP_StockSnapshot (NP, CodInsumo, StockOperativoInicial, FechaCaptura)
        SELECT DISTINCT
            @NuevaNP,
            I.CodigoInsumo,
            -- Cálculo idéntico a Gestión de Stock (Stock Global Real), convertido a GRAMOS
            (
                (ISNULL((SELECT TOP 1 CASE WHEN LOWER(LTRIM(RTRIM(ISNULL(UnidadMedida, 'gr')))) = 'kg' THEN ISNULL(StockActual, 0) * 1000.0 ELSE ISNULL(StockActual, 0) END FROM LIQ_STK_StockInsumos WHERE CodInsumo = I.CodigoInsumo), 0)) + 
                ISNULL((SELECT SUM(D.CantidadRecibida * 1000.0) FROM LIQ_REQ_RecepcionesDetalle D INNER JOIN LIQ_REQ_Recepciones R ON D.NumRequerimiento = R.NumRequerimiento WHERE D.CodInsumo = I.CodigoInsumo), 0) +
                ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste Directo' AND FuenteConsumo = 'Ingreso'), 0) -
                ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = I.CodigoInsumo AND TipoOperacion IN ('Consumo', 'Consumo Desarrollo') AND FuenteConsumo IN ('Stock Inicial', 'Stock Solicitado', 'Solicitud Realizada')), 0) -
                ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste' AND FuenteConsumo IN ('Stock Inicial', 'Stock Solicitado', 'Solicitud Realizada')), 0) -
                ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste Directo' AND FuenteConsumo = 'Salida'), 0) -
                ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = I.CodigoInsumo AND (TipoOperacion = 'Devolucion Central' OR (TipoOperacion = 'Devolucion' AND Motivo LIKE '%Central%'))), 0)
            ) AS StockOperativoInicial,
            GETDATE()
        FROM LIQ_FormulaInsumos I
        WHERE I.IdFormulaColor IN (SELECT NewId FROM #MapColores);

        DROP TABLE #MapColores;

        COMMIT TRANSACTION;

        SET @NuevaNPGerada = @NuevaNP;
        SET @Exito = 1;
        SET @Mensaje = 'Nueva versión (' + @NuevaNP + ') generada exitosamente.';

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @Exito = 0;
        SET @Mensaje = 'Error SQL: ' + ERROR_MESSAGE();
    END CATCH
END
GO
