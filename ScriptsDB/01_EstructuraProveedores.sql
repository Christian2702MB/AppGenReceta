-- =============================================
-- Etapa 3: Módulo de Proveedores y Stock
-- =============================================

-- 1. Tabla Maestra de Proveedores
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TBL_ESTAMPADO_PROVEEDOR]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[TBL_ESTAMPADO_PROVEEDOR](
        [ID_PROVEEDOR] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [RUC] [varchar](11) NOT NULL,
        [RAZON_SOCIAL] [varchar](150) NOT NULL,
        [CONTACTO] [varchar](100) NULL,
        [ES_ACTIVO] [bit] NOT NULL DEFAULT 1,
        [FECHA_REGISTRO] [datetime] DEFAULT GETDATE()
    );
END
GO

-- 2. Tabla Intermedia/Relacional: Insumo <-> Proveedor
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TBL_ESTAMPADO_INSUMO_PROVEEDOR]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[TBL_ESTAMPADO_INSUMO_PROVEEDOR](
        [ID_RELACION] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ID_PROVEEDOR] [int] NOT NULL,
        [COD_ARTICULO] [varchar](50) NOT NULL,
        [CAPACIDAD_NUMERICA] [decimal](18,4) NOT NULL DEFAULT 1.0, -- El número vital para la división (ej. 20 para Balde de 20kg)
        [PRECIO_UNITARIO] [decimal](18,2) NULL,
        FOREIGN KEY (ID_PROVEEDOR) REFERENCES [dbo].[TBL_ESTAMPADO_PROVEEDOR](ID_PROVEEDOR)
    );
END
GO

-- 3. Stored Procedure Emulado de "Stock Actual" (Esto dependerá directamente de las tablas físicas del ERP/Almacén real del usuario).
IF OBJECT_ID('USP_EST_OBTENER_STOCK_ACTUAL', 'P') IS NOT NULL DROP PROCEDURE USP_EST_OBTENER_STOCK_ACTUAL;
GO
CREATE PROCEDURE [dbo].[USP_EST_OBTENER_STOCK_ACTUAL]
    @COD_ARTICULO VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    -- [AQUÍ VA TU LÓGICA DE ALMACÉN REAL]
    -- Mockup funcional: Si el insumo existe devolver un número aleatorio simulando stock para testear interfaz.
    SELECT 
        @COD_ARTICULO AS Cod_Articulo,
        ISNULL((CONVERT(DECIMAL(10,2), RAND() * 100)), 0) AS Stock_Libras
END
GO
