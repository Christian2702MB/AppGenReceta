-- =============================================
-- Script 05: Columna CANTIDAD_SUGERIDA + Ajuste SP Stock
-- Ejecutar en la base de datos
-- =============================================

-- 1. Agregar columna CANTIDAD_SUGERIDA a la tabla de insumos calculados
--    (si ya existe, ignorar el error)
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'TBL_ESTAMPADO_INSUMO_CALCULADO' 
      AND COLUMN_NAME = 'CANTIDAD_SUGERIDA'
)
BEGIN
    ALTER TABLE [dbo].[TBL_ESTAMPADO_INSUMO_CALCULADO]
    ADD [CANTIDAD_SUGERIDA] [decimal](18,2) NULL;
    PRINT 'Columna CANTIDAD_SUGERIDA agregada correctamente.';
END
ELSE
    PRINT 'La columna CANTIDAD_SUGERIDA ya existe. Sin cambios.';
GO

-- =============================================
-- NOTA: El SP USP_EST_OBTENER_STOCK_ACTUAL ya está 
-- ejecutado en BD (Script en Scripts_SQL).
-- 
-- El sistema C# ahora llama a ese SP pasando @COD_ARTICULO 
-- y lee la columna "Stock_Libras" (que devuelve valor+unidad, ej: "27.50 kg").
-- La capa DA extrae solo el número para StockActual.
-- =============================================

-- 2. Verificar que el SP esté disponible (consulta de prueba)
-- EXEC USP_EST_OBTENER_STOCK_ACTUAL @COD_ARTICULO = 'AE000449'

-- 3. Verificar la estructura final de la tabla
-- SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
-- FROM INFORMATION_SCHEMA.COLUMNS 
-- WHERE TABLE_NAME = 'TBL_ESTAMPADO_INSUMO_CALCULADO'
-- ORDER BY ORDINAL_POSITION
