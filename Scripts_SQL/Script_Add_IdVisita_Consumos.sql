-- 1. Añadir la columna IdVisita a la tabla de detalle si no existe
IF COL_LENGTH('LIQ_OperacionesDetalle', 'IdVisita') IS NULL
BEGIN
    ALTER TABLE LIQ_OperacionesDetalle ADD IdVisita INT NULL;
END
GO

-- 2. Actualizar el Procedimiento Almacenado de Registro
IF OBJECT_ID('dbo.LIQ_SP_RegistrarOperacion', 'P') IS NOT NULL 
    DROP PROCEDURE dbo.LIQ_SP_RegistrarOperacion;
GO

CREATE PROCEDURE [dbo].[LIQ_SP_RegistrarOperacion]
    @NP VARCHAR(20),
    @CodInsumo VARCHAR(50),
    @TipoOperacion VARCHAR(20),
    @Cantidad DECIMAL(18,4),
    @Motivo VARCHAR(250),
    @MermaReutilizada VARCHAR(50),
    @Usuario VARCHAR(100),
    @FuenteConsumo VARCHAR(50),
    @NombreColor VARCHAR(100) = NULL,
    @IdVisita INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Insertar en tabla de operaciones (Añadido IdVisita)
        INSERT INTO LIQ_OperacionesDetalle (NP, CodInsumo, TipoOperacion, Cantidad, Motivo, MermaReutilizada, FechaRegistro, UsuarioRegistro, FuenteConsumo, NombreColor, IdVisita)
        VALUES (@NP, @CodInsumo, @TipoOperacion, @Cantidad, @Motivo, @MermaReutilizada, GETDATE(), @Usuario, @FuenteConsumo, @NombreColor, @IdVisita);

        -- Lógica Específica por Tipo
        IF @TipoOperacion = 'Merma'
        BEGIN
            -- Generar Código de Merma (Ej. MER-2405-01)
            DECLARE @CodMerma VARCHAR(50) = 'MER-' + RIGHT(CONVERT(VARCHAR(4), YEAR(GETDATE())), 2) + RIGHT('0' + CONVERT(VARCHAR(2), MONTH(GETDATE())), 2) + '-' + RIGHT('000' + CONVERT(VARCHAR(4), SCOPE_IDENTITY()), 4);
            
            DECLARE @DescInsumo VARCHAR(250) = '';
            DECLARE @Tecnica VARCHAR(100) = '';
            
            SELECT TOP 1 @DescInsumo = Descripcion FROM LIQ_FormulaInsumos WHERE CodigoInsumo = @CodInsumo;
            SELECT TOP 1 @Tecnica = Tecnica FROM LIQ_Formulas WHERE NP = @NP;

            INSERT INTO LIQ_MermasStock (CodigoMerma, NPOrigen, CodInsumo, Descripcion, Tecnica, CantidadOriginal, CantidadDisponible, FechaVencimiento)
            VALUES (@CodMerma, @NP, @CodInsumo, 'Merma de ' + ISNULL(@DescInsumo, @CodInsumo), @Tecnica, @Cantidad, @Cantidad, DATEADD(MONTH, 3, GETDATE()));
        END
        ELSE IF @TipoOperacion = 'Ingreso' AND ISNULL(@MermaReutilizada, '') <> ''
        BEGIN
            -- Descontar de stock de mermas
            UPDATE LIQ_MermasStock
            SET CantidadDisponible = CantidadDisponible - @Cantidad
            WHERE CodigoMerma = @MermaReutilizada;

            -- Si llega a cero o menos, marcar como agotado
            UPDATE LIQ_MermasStock
            SET Estado = 'Agotado'
            WHERE CodigoMerma = @MermaReutilizada AND CantidadDisponible <= 0;
        END

        COMMIT TRANSACTION;
        SELECT 1 AS Resultado, 'Operación registrada correctamente.' AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0 AS Resultado, ERROR_MESSAGE() AS Mensaje;
    END CATCH
END
GO
