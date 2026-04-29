-- =============================================
-- Script 06: Ajustes a TBL_ESTAMPADO_INSUMO_CALCULADO
-- Módulo: Solicitud de Compra – Área Estampado
-- =============================================

-- 1. Columna para identificar insumos agregados manualmente (extra-receta)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('TBL_ESTAMPADO_INSUMO_CALCULADO')
      AND name = 'ES_EXTRA_RECETA'
)
BEGIN
    ALTER TABLE [dbo].[TBL_ESTAMPADO_INSUMO_CALCULADO]
    ADD [ES_EXTRA_RECETA] BIT NOT NULL DEFAULT 0;

    PRINT 'Columna ES_EXTRA_RECETA agregada correctamente.';
END
ELSE
    PRINT 'Columna ES_EXTRA_RECETA ya existe — omitida.';
GO

-- 2. Columna para persistir la presentación seleccionada por el usuario en Paso 3
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('TBL_ESTAMPADO_INSUMO_CALCULADO')
      AND name = 'PRESENTACION'
)
BEGIN
    ALTER TABLE [dbo].[TBL_ESTAMPADO_INSUMO_CALCULADO]
    ADD [PRESENTACION] VARCHAR(200) NULL;

    PRINT 'Columna PRESENTACION agregada correctamente.';
END
ELSE
    PRINT 'Columna PRESENTACION ya existe — omitida.';
GO
