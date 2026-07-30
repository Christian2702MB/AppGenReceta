$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
CREATE PROCEDURE LIQ_SP_TomarSnapshotStockNP
    @NP VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    -- Eliminar la foto anterior si existiera para tomar una nueva
    DELETE FROM LIQ_NP_StockSnapshot 
    WHERE (CASE WHEN ISNULL(Item, '0000') = '0000' THEN NP ELSE NP + '-' + Item END) = @NP;

    -- Extraer la NP base (sin el item concatenado si es que lo mandaron así)
    -- En LIQ_Formulas, NP es 'i8505-V7' y Item es 'ES050437'.
    -- Si @NP viene como 'i8505-V7-ES050437', debemos buscar en la BD usando la expresión.

    INSERT INTO LIQ_NP_StockSnapshot (NP, Item, CodInsumo, StockOperativoInicial, FechaCaptura)
    SELECT DISTINCT
        F.NP, 
        ISNULL(F.Item, '0000'),
        I.CodigoInsumo,
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
                        ISNULL((SELECT SUM(D.CantidadRecibida * 1000.0) FROM LIQ_REQ_RecepcionesDetalle D INNER JOIN LIQ_REQ_Recepciones R ON D.NumRequerimiento = R.NumRequerimiento WHERE R.CodOrdPro = F2.NP AND D.CodInsumo = I.CodigoInsumo), 0) AS Recibido,
                        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F2.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion IN ('Consumo', 'Consumo Desarrollo') AND FuenteConsumo IN ('Stock Solicitado', 'Solicitud Realizada', 'Stock Recibido')), 0) AS Consumido,
                        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F2.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste' AND FuenteConsumo IN ('Stock Solicitado', 'Solicitud Realizada', 'Stock Recibido')), 0) AS Ajustado,
                        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = F2.NP AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Devolucion'), 0) AS Devuelto
                    FROM LIQ_Formulas F2
                    INNER JOIN LIQ_FormulaColores C2 ON F2.IdFormula = C2.IdFormula
                    INNER JOIN LIQ_FormulaInsumos FI2 ON C2.IdFormulaColor = FI2.IdFormulaColor
                    WHERE F2.Estado != 'Cerrada' AND FI2.CodigoInsumo = I.CodigoInsumo
                ) Base
            ), 0)
        ) AS StockOperativoInicial,
        GETDATE()
    FROM LIQ_Formulas F
    INNER JOIN LIQ_FormulaColores C ON F.IdFormula = C.IdFormula
    INNER JOIN LIQ_FormulaInsumos I ON C.IdFormulaColor = I.IdFormulaColor
    WHERE (CASE WHEN ISNULL(F.Item, '0000') = '0000' THEN F.NP ELSE F.NP + '-' + F.Item END) = @NP;
END
"@
$cmd.ExecuteNonQuery()
