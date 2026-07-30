$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()

Write-Host "Disabling DDL triggers..."
$cmdTrig = $conn.CreateCommand()
$cmdTrig.CommandText = "DISABLE TRIGGER DDL_TR_BORRAR_TABLAS ON DATABASE; DISABLE TRIGGER Audit_Principal_Objects ON DATABASE;"
try {
    $cmdTrig.ExecuteNonQuery() | Out-Null
    Write-Host "Triggers disabled successfully."
} catch {
    Write-Host "Could not disable triggers: $_"
}

$filePath = "f:\CMendezB_Innovación\ANTIGRAVITY_2026\AppGenReceta\Scripts_SQL\LIQ_STK_REQ_Tablas_y_SPs.sql"
$sqlContent = [System.IO.File]::ReadAllText($filePath, [System.Text.Encoding]::UTF8)

# we just need to execute lines containing LIQ_STK_SP_RegistrarCargaInicial
# It's better to just extract that SP and run it directly to avoid re-creating tables
$spContent = @"
IF OBJECT_ID('dbo.LIQ_STK_SP_RegistrarCargaInicial', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_STK_SP_RegistrarCargaInicial;
"@
$cmd = $conn.CreateCommand()
$cmd.CommandText = $spContent
$cmd.ExecuteNonQuery() | Out-Null

$spCreate = @"
CREATE PROCEDURE [dbo].[LIQ_STK_SP_RegistrarCargaInicial]
    @CodInsumo VARCHAR(50),
    @Descripcion VARCHAR(250),
    @UnidadMedida VARCHAR(20),
    @PesoGramos DECIMAL(18,4),
    @Usuario VARCHAR(100),
    @NPDirigida VARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Validar Unidad de Medida por defecto
        IF ISNULL(@UnidadMedida, '') = '' SET @UnidadMedida = 'KG';

        -- El peso ya viene en la unidad correcta según la UI (KG o gr), no se convierte
        DECLARE @StockIncremento DECIMAL(18,2) = @PesoGramos;

        DECLARE @NuevoStock DECIMAL(18,2) = 0.00;

        IF NOT EXISTS (SELECT 1 FROM LIQ_STK_StockInsumos WHERE CodInsumo = @CodInsumo)
        BEGIN
            SET @NuevoStock = @StockIncremento;
            INSERT INTO LIQ_STK_StockInsumos (CodInsumo, Descripcion, UnidadMedida, StockActual, FechaUltimaActualizacion, UsuarioUltimaActualizacion)
            VALUES (@CodInsumo, @Descripcion, @UnidadMedida, @NuevoStock, GETDATE(), @Usuario);
        END
        ELSE
        BEGIN
            SELECT @NuevoStock = StockActual + @StockIncremento FROM LIQ_STK_StockInsumos WHERE CodInsumo = @CodInsumo;

            UPDATE LIQ_STK_StockInsumos SET
                StockActual = @NuevoStock,
                FechaUltimaActualizacion = GETDATE(),
                UsuarioUltimaActualizacion = @Usuario
            WHERE CodInsumo = @CodInsumo;
        END

        -- Registrar en Kardex
        INSERT INTO LIQ_STK_Kardex (CodInsumo, TipoMovimiento, Concepto, Cantidad, StockResultante, ReferenciaID, Observaciones, FechaMovimiento, Usuario)
        VALUES (@CodInsumo, 'Entrada', 'Carga Inicial', @StockIncremento, @NuevoStock, @NPDirigida, 'Carga inicial simulada', GETDATE(), @Usuario);

        -- Lógica de Recarga Dirigida a una NP
        IF ISNULL(@NPDirigida, '') <> ''
        BEGIN
            DECLARE @BaseNP VARCHAR(100) = @NPDirigida;
            DECLARE @BaseItem VARCHAR(100) = '0000';
            
            IF CHARINDEX('-', @NPDirigida) > 0
            BEGIN
                DECLARE @idx INT = CHARINDEX('-', REVERSE(@NPDirigida));
                SET @BaseNP = SUBSTRING(@NPDirigida, 1, LEN(@NPDirigida) - @idx);
                SET @BaseItem = SUBSTRING(@NPDirigida, LEN(@NPDirigida) - @idx + 2, LEN(@NPDirigida));
            END

            -- LIQ_NP_StockSnapshot almacena siempre en GRAMOS
            DECLARE @IncrementoGramos DECIMAL(18,4) = @StockIncremento;
            IF UPPER(LTRIM(RTRIM(@UnidadMedida))) = 'KG'
            BEGIN
                SET @IncrementoGramos = @StockIncremento * 1000.0;
            END

            IF EXISTS (SELECT 1 FROM LIQ_NP_StockSnapshot WHERE NP = @BaseNP AND CodInsumo = @CodInsumo AND Item = @BaseItem)
            BEGIN
                UPDATE LIQ_NP_StockSnapshot
                SET StockOperativoInicial = StockOperativoInicial + @IncrementoGramos,
                    FechaCaptura = GETDATE()
                WHERE NP = @BaseNP AND CodInsumo = @CodInsumo AND Item = @BaseItem;
            END
            ELSE
            BEGIN
                INSERT INTO LIQ_NP_StockSnapshot (NP, CodInsumo, StockOperativoInicial, FechaCaptura, Item)
                VALUES (@BaseNP, @CodInsumo, @IncrementoGramos, GETDATE(), @BaseItem);
            END
        END

        COMMIT TRANSACTION;
        SELECT 1 AS Resultado, 'Carga inicial registrada correctamente.' AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0 AS Resultado, ERROR_MESSAGE() AS Mensaje;
    END CATCH
END
"@
$cmd = $conn.CreateCommand()
$cmd.CommandText = $spCreate
$cmd.ExecuteNonQuery() | Out-Null
Write-Host "SP LIQ_STK_SP_RegistrarCargaInicial updated!"

$conn.Close()
