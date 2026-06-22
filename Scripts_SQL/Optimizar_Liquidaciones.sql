-- =========================================================================
-- ÍNDICES DE OPTIMIZACIÓN PARA MÓDULO DE LIQUIDACIONES
-- Se crean para evitar Table Scans en la tabla LIQ_OperacionesDetalle
-- =========================================================================

-- ÍNDICE 1: Consultas Globales (Saldos por Insumo)
IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_OperDet_CodInsumo_TipoOp_Fuente')
BEGIN
    CREATE NONCLUSTERED INDEX IX_OperDet_CodInsumo_TipoOp_Fuente
    ON [dbo].[LIQ_OperacionesDetalle] ([CodInsumo], [TipoOperacion], [FuenteConsumo])
    INCLUDE ([Cantidad], [NP], [NombreColor]);
END
GO

-- ÍNDICE 2: Consultas por NP (Grilla Principal de Liquidación)
IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_OperDet_NP_CodInsumo_TipoOp')
BEGIN
    CREATE NONCLUSTERED INDEX IX_OperDet_NP_CodInsumo_TipoOp
    ON [dbo].[LIQ_OperacionesDetalle] ([NP], [CodInsumo], [TipoOperacion])
    INCLUDE ([Cantidad], [FuenteConsumo], [NombreColor], [Motivo], [IdOperacion]);
END
GO

-- ÍNDICE 3: Consultas de Historial (Popup SweetAlert)
IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_OperDet_Historial')
BEGIN
    CREATE NONCLUSTERED INDEX IX_OperDet_Historial
    ON [dbo].[LIQ_OperacionesDetalle] ([CodInsumo], [NombreColor], [IdVisita])
    INCLUDE ([TipoOperacion], [Cantidad], [FechaRegistro], [UsuarioRegistro], [FuenteConsumo], [IdOperacion]);
END
GO

-- ÍNDICE 4: Consultas de Mermas Reutilizadas
IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_OperDet_MermaReut')
BEGIN
    CREATE NONCLUSTERED INDEX IX_OperDet_MermaReut
    ON [dbo].[LIQ_OperacionesDetalle] ([MermaReutilizada], [TipoOperacion])
    INCLUDE ([Cantidad], [FechaRegistro], [UsuarioRegistro], [FuenteConsumo], [NP], [Motivo]);
END
GO
