USE [AppGenReceta]
GO

IF OBJECT_ID('dbo.LIQ_SP_TerminarNP_Fase5', 'P') IS NOT NULL 
    DROP PROCEDURE dbo.LIQ_SP_TerminarNP_Fase5;
GO

CREATE PROCEDURE [dbo].[LIQ_SP_TerminarNP_Fase5]
    @NP VARCHAR(50),
    @DestinoGlobal VARCHAR(100),
    @Usuario VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Insertar devoluciones masivas para cada insumo con Saldo > 0
        IF ISNULL(@DestinoGlobal, '') <> ''
        BEGIN
            INSERT INTO LIQ_OperacionesDetalle (NP, CodInsumo, NombreColor, TipoOperacion, Cantidad, Motivo, FechaRegistro, UsuarioRegistro)
            SELECT 
                F.NP, 
                I.CodigoInsumo, 
                (SELECT TOP 1 NombreColor FROM LIQ_FormulaColores WHERE IdFormula = F.IdFormula),
                'Devolucion', 
                (
                    -- Stock Recibido
                    ISNULL((SELECT SUM(D.CantidadRecibida * 1000.00) FROM LIQ_REQ_Recepciones R INNER JOIN LIQ_REQ_RecepcionesDetalle D ON R.NumRequerimiento = D.NumRequerimiento WHERE R.CodOrdPro = F.NP AND D.CodInsumo = I.CodigoInsumo), 0)
                    -
                    -- Consumo
                    ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo' AND FuenteConsumo = 'Solicitud Realizada'), 0)
                    -
                    -- Ajuste
                    ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste' AND FuenteConsumo = 'Solicitud Realizada'), 0)
                    -
                    -- Devoluciones Previas
                    ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Devolucion'), 0)
                ) AS SaldoPendiente,
                @DestinoGlobal,
                GETDATE(),
                @Usuario
            FROM LIQ_Formulas F
            INNER JOIN LIQ_FormulaColores C ON F.IdFormula = C.IdFormula
            INNER JOIN LIQ_FormulaInsumos I ON C.IdFormulaColor = I.IdFormulaColor
            WHERE F.NP = @NP
              AND F.Eliminado = 0
            GROUP BY F.NP, I.CodigoInsumo, F.IdFormula
            HAVING (
                ISNULL((SELECT SUM(D.CantidadRecibida * 1000.00) FROM LIQ_REQ_Recepciones R INNER JOIN LIQ_REQ_RecepcionesDetalle D ON R.NumRequerimiento = D.NumRequerimiento WHERE R.CodOrdPro = F.NP AND D.CodInsumo = I.CodigoInsumo), 0)
                - ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo' AND FuenteConsumo = 'Solicitud Realizada'), 0)
                - ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste' AND FuenteConsumo = 'Solicitud Realizada'), 0)
                - ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Devolucion'), 0)
            ) > 0;
        END

        -- 2. Cambiar Estado a Terminado (Fase 5)
        UPDATE LIQ_Formulas
        SET Estado = 'Terminado'
        WHERE NP = @NP;

        COMMIT TRANSACTION;
        SELECT 1 AS Exito, 'Liquidación terminada y saldos devueltos a ' + @DestinoGlobal AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        SELECT 0 AS Exito, ERROR_MESSAGE() AS Mensaje;
    END CATCH
END
GO
