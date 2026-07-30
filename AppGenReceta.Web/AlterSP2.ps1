$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
ALTER PROCEDURE [dbo].[LIQ_SP_GenerarSiguienteVersionNP]
    @NPOriginal VARCHAR(100),
    @Usuario VARCHAR(100),
    @Observacion VARCHAR(250),
    @NuevaNPGerada VARCHAR(100) OUTPUT,
    @Exito INT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Exito = 0;
    SET @Mensaje = '';

    BEGIN TRY
        -- 1. Buscar la fórmula base (la versión actual desde donde copiamos)
        DECLARE @IdFormulaOriginal INT;
        DECLARE @BaseNP VARCHAR(100);
        DECLARE @BaseItem VARCHAR(100);
        DECLARE @CurrentMaxVersion INT = 1;

        SELECT TOP 1 
            @IdFormulaOriginal = IdFormula,
            @BaseNP = NP,
            @BaseItem = ISNULL(Item, '')
        FROM LIQ_Formulas 
        WHERE (NP = @NPOriginal OR (CASE WHEN ISNULL(Item, '0000') = '0000' THEN NP ELSE NP + '-' + Item END) = @NPOriginal) 
          AND ISNULL(Eliminado, 0) = 0
        ORDER BY IdFormula DESC;

        IF @IdFormulaOriginal IS NULL
        BEGIN
            SET @Exito = 0;
            SET @Mensaje = 'No se encontró una fórmula activa para la NP: ' + @NPOriginal;
            RETURN;
        END

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
        DECLARE @SnapshotNP VARCHAR(100) = (CASE WHEN @BaseItem = '' OR @BaseItem = '0000' THEN @NuevaNP ELSE @NuevaNP + '-' + @BaseItem END);
        EXEC LIQ_SP_TomarSnapshotStockNP @SnapshotNP;

        DROP TABLE #MapColores;

        COMMIT TRANSACTION;

        SET @NuevaNPGerada = @SnapshotNP;
        SET @Exito = 1;
        SET @Mensaje = 'Nueva versión (' + @SnapshotNP + ') generada exitosamente.';

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @Exito = 0;
        SET @Mensaje = 'Error SQL: ' + ERROR_MESSAGE();
    END CATCH
END
"@
$cmd.ExecuteNonQuery()
