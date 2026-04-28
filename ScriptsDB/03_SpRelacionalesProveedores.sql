-- ==============================================================
-- Etapa 3 B: Módulo de Proveedores (Mejoras UI y SPs Transaccionales)
-- ==============================================================

-- 1. Agregamos las columnas adicionales exigidas por el Mockup de Interfaz
ALTER TABLE [dbo].[TBL_ESTAMPADO_INSUMO_PROVEEDOR]
ADD 
    [DES_PRESENTACION] [varchar](100) NULL, -- Ej: 'Balde 20 kg'
    [UNIDAD_MEDIDA] [varchar](50) NULL;     -- Ej: 'kg'

GO

-- 2. Stored Procedure Principal: Listar Grilla Multipropósito
IF OBJECT_ID('USP_EST_LISTAR_PROVEEDORES_INSUMOS', 'P') IS NOT NULL DROP PROCEDURE USP_EST_LISTAR_PROVEEDORES_INSUMOS;
GO
CREATE PROCEDURE [dbo].[USP_EST_LISTAR_PROVEEDORES_INSUMOS]
    @FiltROTexto VARCHAR(100) = ''
AS
BEGIN
    SET NOCOUNT ON;
    -- Relaciona el Maestro de Proveedores con la tabla local cruzada
    SELECT 
        R.ID_RELACION,
        P.ID_PROVEEDOR,
        P.RAZON_SOCIAL,
        R.COD_ARTICULO,
        -- Mockup Description: Idealmente sacas DESCRIPCION mediante JOIN al maestro de articulos del ERP
        CAST(R.COD_ARTICULO AS VARCHAR(100)) AS DES_INSUMO, 
        R.CAPACIDAD_NUMERICA,
        R.DES_PRESENTACION,
        R.UNIDAD_MEDIDA,
        R.PRECIO_UNITARIO
    FROM [dbo].[TBL_ESTAMPADO_INSUMO_PROVEEDOR] R
    INNER JOIN [dbo].[TBL_ESTAMPADO_PROVEEDOR] P ON R.ID_PROVEEDOR = P.ID_PROVEEDOR
    WHERE P.ES_ACTIVO = 1
    AND (
        P.RAZON_SOCIAL LIKE '%' + @FiltROTexto + '%' OR
        R.COD_ARTICULO LIKE '%' + @FiltROTexto + '%' OR 
        R.DES_PRESENTACION LIKE '%' + @FiltROTexto + '%'
    )
    ORDER BY P.RAZON_SOCIAL ASC
END
GO
