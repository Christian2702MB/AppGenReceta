USE [HIALPESA]
GO

/****** 1. TABLA: LIQ_MOV_Solicitudes ******/
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LIQ_MOV_Solicitudes]') AND type in (N'U'))
BEGIN
	CREATE TABLE [dbo].[LIQ_MOV_Solicitudes](
		[IdSolicitud] [int] IDENTITY(1,1) NOT NULL,
		[TipoMovimiento] [varchar](50) NOT NULL,
		[TipoAprobador] [varchar](50) NOT NULL,
		[Estado] [varchar](20) NOT NULL,
		[FechaSolicitud] [datetime] NOT NULL,
		[UsuarioLiquidador] [varchar](100) NOT NULL,
		[UsuarioAprobador] [varchar](100) NULL,
		[NumMovimientoERP] [varchar](20) NULL,
		[Almacen] [varchar](20) NOT NULL,
		[Observaciones] [varchar](500) NULL,
		[NroGuia] [varchar](50) NULL,
	 CONSTRAINT [PK_LIQ_MOV_Solicitudes] PRIMARY KEY CLUSTERED 
	(
		[IdSolicitud] ASC
	)
	) ON [PRIMARY]
END
GO

/****** 2. TABLA: LIQ_MOV_SolicitudesDetalle ******/
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LIQ_MOV_SolicitudesDetalle]') AND type in (N'U'))
BEGIN
	CREATE TABLE [dbo].[LIQ_MOV_SolicitudesDetalle](
		[IdDetalle] [int] IDENTITY(1,1) NOT NULL,
		[IdSolicitud] [int] NOT NULL,
		[NP] [varchar](50) NULL,
		[CodItem] [varchar](50) NOT NULL,
		[NombreItem] [varchar](200) NULL,
		[UM] [varchar](10) NULL,
		[Cantidad] [decimal](18, 5) NOT NULL,
		[CodMermaOrigen] [varchar](50) NULL,
		[Lote] [varchar](50) NULL,
		[LoteProv] [varchar](50) NULL,
	 CONSTRAINT [PK_LIQ_MOV_SolicitudesDetalle] PRIMARY KEY CLUSTERED 
	(
		[IdDetalle] ASC
	)
	) ON [PRIMARY]

	ALTER TABLE [dbo].[LIQ_MOV_SolicitudesDetalle]  WITH CHECK ADD  CONSTRAINT [FK_LIQ_MOV_Detalle_Cabecera] FOREIGN KEY([IdSolicitud])
	REFERENCES [dbo].[LIQ_MOV_Solicitudes] ([IdSolicitud])
END
GO

/****** 3. SP: LIQ_MOV_SP_ListarSolicitudes ******/
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'LIQ_MOV_SP_ListarSolicitudes')
    DROP PROCEDURE [dbo].[LIQ_MOV_SP_ListarSolicitudes]
GO

CREATE PROCEDURE [dbo].[LIQ_MOV_SP_ListarSolicitudes]
	@UsuarioActual VARCHAR(100),
	@RolUsuario VARCHAR(50)
AS
BEGIN
	SET NOCOUNT ON;

	-- Si es Liquidador, ve su propio historial (o todo el historial de liquidadores).
	-- Si es Aprobador, ve las pendientes que le corresponden a su tipo de aprobador.

	SELECT 
		S.IdSolicitud,
		S.TipoMovimiento,
		S.TipoAprobador,
		S.Estado,
		S.FechaSolicitud,
		S.UsuarioLiquidador,
		S.UsuarioAprobador,
		S.NumMovimientoERP,
		S.Almacen,
		S.Observaciones
	FROM LIQ_MOV_Solicitudes S
	WHERE 
		(@RolUsuario = 'Liquidador' OR @RolUsuario = 'Administrador')
		OR
		(@RolUsuario IN ('Aprobador Insumos', 'Aprobador Mermas') AND S.TipoAprobador = @RolUsuario)
	ORDER BY S.FechaSolicitud DESC
END
GO

/****** 4. SP: LIQ_MOV_SP_RegistrarSolicitud ******/
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'LIQ_MOV_SP_RegistrarSolicitud')
    DROP PROCEDURE [dbo].[LIQ_MOV_SP_RegistrarSolicitud]
GO

CREATE PROCEDURE [dbo].[LIQ_MOV_SP_RegistrarSolicitud]
	@TipoMovimiento VARCHAR(50),
	@TipoAprobador VARCHAR(50),
	@UsuarioLiquidador VARCHAR(100),
	@Almacen VARCHAR(20),
	@Observaciones VARCHAR(500),
	@NroGuia VARCHAR(50) = NULL,
	@IdSolicitud INT OUTPUT
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO LIQ_MOV_Solicitudes (
		TipoMovimiento,
		TipoAprobador,
		Estado,
		FechaSolicitud,
		UsuarioLiquidador,
		Almacen,
		Observaciones,
		NroGuia
	) VALUES (
		@TipoMovimiento,
		@TipoAprobador,
		'Pendiente',
		GETDATE(),
		@UsuarioLiquidador,
		@Almacen,
		@Observaciones,
		@NroGuia
	);

	SET @IdSolicitud = SCOPE_IDENTITY();
END
GO

/****** 5. SP: LIQ_MOV_SP_RegistrarSolicitudDetalle ******/
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'LIQ_MOV_SP_RegistrarSolicitudDetalle')
    DROP PROCEDURE [dbo].[LIQ_MOV_SP_RegistrarSolicitudDetalle]
GO

CREATE PROCEDURE [dbo].[LIQ_MOV_SP_RegistrarSolicitudDetalle]
	@IdSolicitud INT,
	@NP VARCHAR(50),
	@CodItem VARCHAR(50),
	@NombreItem VARCHAR(200),
	@UM VARCHAR(10),
	@Cantidad DECIMAL(18,5),
	@CodMermaOrigen VARCHAR(50),
	@Lote VARCHAR(50) = NULL,
	@LoteProv VARCHAR(50) = NULL
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO LIQ_MOV_SolicitudesDetalle (
		IdSolicitud,
		NP,
		CodItem,
		NombreItem,
		UM,
		Cantidad,
		CodMermaOrigen,
		Lote,
		LoteProv
	) VALUES (
		@IdSolicitud,
		@NP,
		@CodItem,
		@NombreItem,
		@UM,
		@Cantidad,
		@CodMermaOrigen,
		@Lote,
		@LoteProv
	);
END
GO

/****** 6. SP: LIQ_MOV_SP_ObservarSolicitud ******/
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'LIQ_MOV_SP_ObservarSolicitud')
    DROP PROCEDURE [dbo].[LIQ_MOV_SP_ObservarSolicitud]
GO

CREATE PROCEDURE [dbo].[LIQ_MOV_SP_ObservarSolicitud]
	@IdSolicitud INT,
	@UsuarioAprobador VARCHAR(100),
	@Observacion VARCHAR(500)
AS
BEGIN
	SET NOCOUNT ON;

	UPDATE LIQ_MOV_Solicitudes
	SET Estado = 'Observado',
		UsuarioAprobador = @UsuarioAprobador,
		Observaciones = @Observacion
	WHERE IdSolicitud = @IdSolicitud;
END
GO

/****** 7. SP: LIQ_MOV_SP_AprobarSolicitud ******/
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'LIQ_MOV_SP_AprobarSolicitud')
    DROP PROCEDURE [dbo].[LIQ_MOV_SP_AprobarSolicitud]
GO

CREATE PROCEDURE [dbo].[LIQ_MOV_SP_AprobarSolicitud]
	@IdSolicitud INT,
	@UsuarioAprobador VARCHAR(100),
	@NumMovimientoERP VARCHAR(20) = NULL OUTPUT
AS
BEGIN
	SET NOCOUNT ON;
	
	-- Aquí en un caso real se llamaría al wrapper de inserción del ERP:
	-- EXEC LIQ_MOV_SP_InsertarERP @IdSolicitud, @NumMovimientoERP OUTPUT
	-- Como aún no podemos probar la inserción real en LG_MOVISTK,
	-- dejamos la simulación/firma de aprobación de nuestra tabla.
	
	-- Para el ejemplo simularemos que el ERP nos devolvió '016677'
	IF @NumMovimientoERP IS NULL
	BEGIN
		SET @NumMovimientoERP = '016677' 
	END

	UPDATE LIQ_MOV_Solicitudes
	SET Estado = 'Aprobado',
		UsuarioAprobador = @UsuarioAprobador,
		NumMovimientoERP = @NumMovimientoERP
	WHERE IdSolicitud = @IdSolicitud;
END
GO

/****** 8. SP: LIQ_MOV_SP_ObtenerDetalleSolicitud ******/
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'LIQ_MOV_SP_ObtenerDetalleSolicitud')
    DROP PROCEDURE [dbo].[LIQ_MOV_SP_ObtenerDetalleSolicitud]
GO

CREATE PROCEDURE [dbo].[LIQ_MOV_SP_ObtenerDetalleSolicitud]
	@IdSolicitud INT
AS
BEGIN
	SET NOCOUNT ON;

	SELECT 
		IdDetalle,
		IdSolicitud,
		NP,
		CodItem,
		NombreItem,
		UM,
		Cantidad,
		CodMermaOrigen,
		Lote,
		LoteProv
	FROM LIQ_MOV_SolicitudesDetalle
	WHERE IdSolicitud = @IdSolicitud
END
GO
