USE [HIALPESA]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('dbo.LIQ_SP_GenerarSiguienteVersionNP', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_GenerarSiguienteVersionNP;
GO

-- =========================================================================================
-- Author:      Antigravity
-- Create date: 2026-06-15
-- Description: Clona una Fórmula Terminada (NP) a una nueva versión consecutiva (Activa),
--              manteniendo intacta la original para auditoría.
-- =========================================================================================
CREATE PROCEDURE [dbo].[LIQ_SP_GenerarSiguienteVersionNP]
    @NPOriginal VARCHAR(100),
    @Usuario VARCHAR(50),
    @Observacion VARCHAR(500),
    @NuevaNPGerada VARCHAR(100) OUTPUT,
    @Exito INT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- 1. Validar que la NP original exista y esté terminada
        DECLARE @IdFormulaOriginal INT;
        DECLARE @BaseNP VARCHAR(100);
        DECLARE @BaseItem VARCHAR(100);

        SELECT TOP 1 
            @IdFormulaOriginal = IdFormula,
            @BaseNP = NP,
            @BaseItem = ISNULL(Item, '')
        FROM LIQ_Formulas 
        WHERE (NP = @NPOriginal OR (NP + '-' + ISNULL(Item, '')) = @NPOriginal) 
          AND Estado IN ('Terminado', 'Terminada')
          AND ISNULL(Eliminado, 0) = 0;

        IF @IdFormulaOriginal IS NULL
        BEGIN
            SET @Exito = 0;
            SET @Mensaje = 'La OP proporcionada no existe o no se encuentra en estado Terminado.';
            RETURN;
        END

        -- 2. Determinar el nuevo sufijo de versión
        DECLARE @CurrentMaxVersion INT = 1;

        -- Extraer la base pura (ej. 'i8460' de 'i8460-V2' o de 'i8460')
        IF CHARINDEX('-V', @BaseNP) > 0
        BEGIN
            SET @BaseNP = SUBSTRING(@BaseNP, 1, CHARINDEX('-V', @BaseNP) - 1);
        END

        -- Buscar la versión más alta actual para esta base e ítem
        SELECT @CurrentMaxVersion = ISNULL(MAX(
            CAST(SUBSTRING(NP, CHARINDEX('-V', NP) + 2, LEN(NP)) AS INT)
        ), 1)
        FROM LIQ_Formulas
        WHERE NP LIKE @BaseNP + '-V%' 
          AND ISNULL(Item, '') = @BaseItem
          AND ISNUMERIC(SUBSTRING(NP, CHARINDEX('-V', NP) + 2, LEN(NP))) = 1;

        -- Si la original no tenía -V, y no encontramos derivadas, la nueva es V2
        IF CHARINDEX('-V', @BaseNP) = 0 AND @CurrentMaxVersion = 1
        BEGIN
            SET @CurrentMaxVersion = 1; 
        END

        DECLARE @NextVersion INT = @CurrentMaxVersion + 1;
        DECLARE @NuevaNP VARCHAR(100) = @BaseNP + '-V' + CAST(@NextVersion AS VARCHAR(10));

        -- Validar colisión (sanity check)
        IF EXISTS (SELECT 1 FROM LIQ_Formulas WHERE NP = @NuevaNP AND ISNULL(Item, '') = @BaseItem AND ISNULL(Eliminado, 0) = 0)
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
        WHERE IdFormula = @IdFormulaOriginal;

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
        WHERE IdFormula = @IdFormulaOriginal;

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
        WHERE P.IdFormula = @IdFormulaOriginal;

        -- 6. Tomar Snapshot del Stock Global Actual para la Nueva Versión
        -- Esto garantiza que la nueva versión inicie con el saldo real de la planta en gramos.
        INSERT INTO LIQ_NP_StockSnapshot (NP, CodInsumo, StockOperativoInicial, FechaCaptura)
        SELECT DISTINCT
            @NuevaNP,
            I.CodigoInsumo,
            -- Cálculo para obtener exactamente el STOCK DISPONIBLE, convertido a GRAMOS
            (
                -- 1. Tomamos el STOCK ACTUAL GLOBAL
                (ISNULL((SELECT TOP 1 CASE WHEN LOWER(LTRIM(RTRIM(ISNULL(UnidadMedida, 'gr')))) = 'kg' THEN ISNULL(StockActual, 0) * 1000.0 ELSE ISNULL(StockActual, 0) END FROM LIQ_STK_StockInsumos WHERE CodInsumo = I.CodigoInsumo), 0)) + 
                ISNULL((SELECT SUM(D.CantidadRecibida * 1000.0) FROM LIQ_REQ_RecepcionesDetalle D INNER JOIN LIQ_REQ_Recepciones R ON D.NumRequerimiento = R.NumRequerimiento WHERE D.CodInsumo = I.CodigoInsumo), 0) +
                ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste Directo' AND FuenteConsumo = 'Ingreso'), 0) -
                ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = I.CodigoInsumo AND TipoOperacion IN ('Consumo', 'Consumo Desarrollo') AND FuenteConsumo IN ('Stock Inicial', 'Stock Solicitado', 'Solicitud Realizada')), 0) -
                ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste' AND FuenteConsumo IN ('Stock Inicial', 'Stock Solicitado', 'Solicitud Realizada')), 0) -
                ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste Directo' AND FuenteConsumo = 'Salida'), 0) -
                ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = I.CodigoInsumo AND (TipoOperacion = 'Devolucion Central' OR (TipoOperacion = 'Devolucion' AND Motivo LIKE '%Central%'))), 0)
                
                -- 2. MENOS (-) EL STOCK POR LIQUIDAR (Pendiente en otras NPs activas)
                - ISNULL((
                    SELECT SUM(ISNULL(Base.Recibido, 0) - ISNULL(Base.Consumido, 0) - ISNULL(Base.Ajustado, 0) - ISNULL(Base.Devuelto, 0))
                    FROM (
                        SELECT 
                            ISNULL((SELECT SUM(D.CantidadRecibida * 1000.0) FROM LIQ_REQ_RecepcionesDetalle D INNER JOIN LIQ_REQ_Recepciones R ON D.NumRequerimiento = R.NumRequerimiento WHERE R.CodOrdPro = F.NP AND D.CodInsumo = I.CodigoInsumo), 0) AS Recibido,
                            ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion IN ('Consumo', 'Consumo Desarrollo') AND FuenteConsumo IN ('Stock Solicitado', 'Solicitud Realizada', 'Stock Recibido')), 0) AS Consumido,
                            ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste' AND FuenteConsumo IN ('Stock Solicitado', 'Solicitud Realizada', 'Stock Recibido')), 0) AS Ajustado,
                            ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Devolucion'), 0) AS Devuelto
                        FROM LIQ_Formulas F
                        INNER JOIN LIQ_FormulaColores C ON F.IdFormula = C.IdFormula
                        INNER JOIN LIQ_FormulaInsumos FI ON C.IdFormulaColor = FI.IdFormulaColor
                        WHERE F.Estado != 'Cerrada' AND FI.CodigoInsumo = I.CodigoInsumo
                    ) Base
                ), 0)
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
