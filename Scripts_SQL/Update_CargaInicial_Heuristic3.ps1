$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()

$sqlContent = @"
ALTER PROCEDURE [dbo].[LIQ_STK_SP_RegistrarCargaInicial]
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

        IF ISNULL(@UnidadMedida, '') = '' SET @UnidadMedida = 'KG';
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

        INSERT INTO LIQ_STK_Kardex (CodInsumo, TipoMovimiento, Concepto, Cantidad, StockResultante, ReferenciaID, Observaciones, FechaMovimiento, Usuario)
        VALUES (@CodInsumo, 'Entrada', 'Carga Inicial', @StockIncremento, @NuevoStock, @NPDirigida, 'Carga inicial simulada', GETDATE(), @Usuario);

        IF ISNULL(@NPDirigida, '') <> ''
        BEGIN
            DECLARE @BaseNP VARCHAR(100) = @NPDirigida;
            DECLARE @BaseItem VARCHAR(100) = '0000';
            
            IF EXISTS (SELECT 1 FROM LIQ_Formulas WHERE NP = @NPDirigida AND ISNULL(Item, '0000') = '0000')
            BEGIN
                SET @BaseNP = @NPDirigida;
                SET @BaseItem = '0000';
            END
            ELSE IF CHARINDEX('-', @NPDirigida) > 0
            BEGIN
                DECLARE @idx INT = CHARINDEX('-', REVERSE(@NPDirigida));
                DECLARE @posNP VARCHAR(100) = SUBSTRING(@NPDirigida, 1, LEN(@NPDirigida) - @idx);
                DECLARE @posItem VARCHAR(100) = SUBSTRING(@NPDirigida, LEN(@NPDirigida) - @idx + 2, LEN(@NPDirigida));
                
                IF EXISTS (SELECT 1 FROM LIQ_Formulas WHERE NP = @posNP AND Item = @posItem)
                BEGIN
                    SET @BaseNP = @posNP;
                    SET @BaseItem = @posItem;
                END
                ELSE
                BEGIN
                    SET @BaseNP = @posNP;
                    SET @BaseItem = @posItem;
                END
            END

            DECLARE @IncrementoGramos DECIMAL(18,4) = @StockIncremento;
            IF UPPER(LTRIM(RTRIM(@UnidadMedida))) = 'KG'
            BEGIN
                SET @IncrementoGramos = @StockIncremento * 1000.0;
            END

            IF EXISTS (SELECT 1 FROM LIQ_NP_StockSnapshot WHERE NP = @BaseNP AND CodInsumo = @CodInsumo AND Item = @BaseItem)
            BEGIN
                UPDATE LIQ_NP_StockSnapshot
                SET StockOperativoInicial = StockOperativoInicial + @IncrementoGramos
                WHERE NP = @BaseNP AND CodInsumo = @CodInsumo AND Item = @BaseItem;
            END
            ELSE
            BEGIN
                INSERT INTO LIQ_NP_StockSnapshot (NP, CodInsumo, StockOperativoInicial, FechaCaptura, Item)
                VALUES (@BaseNP, @CodInsumo, @IncrementoGramos, GETDATE(), @BaseItem);
            END
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 
        BEGIN
            ROLLBACK TRANSACTION;
        END
        DECLARE @err VARCHAR(MAX) = ERROR_MESSAGE();
        RAISERROR(@err, 16, 1);
    END CATCH
END
"@
$cmd = $conn.CreateCommand()
$cmd.CommandText = $sqlContent
$cmd.ExecuteNonQuery()
Write-Host "SP LIQ_STK_SP_RegistrarCargaInicial updated correctly!"
$conn.Close()
