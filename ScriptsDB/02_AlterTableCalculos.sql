-- =============================================
-- Alteración de la Tabla de Persistencia
-- =============================================

-- Modificamos la tabla principal donde se guardan históricamente los pedidos:
ALTER TABLE [dbo].[TBL_ESTAMPADO_INSUMO_CALCULADO]
ADD 
    [CANTIDAD_A_PEDIR] [decimal](18,2) NULL,
    [STOCK_CONSULTADO] [decimal](18,2) NULL,
    [TIPO_DESPACHO] [varchar](20) NULL,
    [ID_PROVEEDOR_ASIGNADO] [int] NULL;

GO

-- Opcional: Agregar Constraint
-- ALTER TABLE [dbo].[TBL_ESTAMPADO_INSUMO_CALCULADO] 
-- ADD FOREIGN KEY (ID_PROVEEDOR_ASIGNADO) REFERENCES [dbo].[TBL_ESTAMPADO_PROVEEDOR](ID_PROVEEDOR);
