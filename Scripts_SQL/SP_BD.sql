USE [HIALPESA]
GO
/****** Object:  StoredProcedure [dbo].[AGO_Validar_Usuario]    Script Date: 7/04/2026 15:13:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[AGO_Validar_Usuario]
@Usuario varchar(250),
@Clave varchar(250)
AS
--SELECT 
--TOP(1)
--IDUsuario
--,Nombres
--,ApellidoPaterno
--,ApellidoMaterno
--,Documento
--,Correo
--,Cliente
--,IdRol
--,NombreRol
--  FROM dbo.AGO_Usuario
--  WHERE Correo=@Usuario AND Contrasena=@Clave

select TOP(1)
1 as 'IDUsuario',
CASE WHEN S.nombre_trabajador = '' THEN dbo.AGR_fn_SepararNombreCompleto(S.Nom_Usuario,2) ELSE S.nombre_trabajador END as 'Nombres', 
CASE WHEN S.Apellido_Paterno = '' THEN dbo.AGR_fn_SepararNombreCompleto(S.Nom_Usuario,1) ELSE S.Apellido_Paterno END as 'ApellidoPaterno',
S.Apellido_Materno as 'ApellidoMaterno',
S.dni_usuario as 'Documento',
S.Email as 'Correo',
'General' as Cliente,
isnull(R.IdRol,'1') as IdRol,
isnull(R.NombreRol,'Visualizador') as NombreRol
FROM SEGURIDAD.dbo.SEG_USUARIOS S left join AGR_RolesUsuarioReceta R
on S.COD_USUARIO = R.Usuario
where S.COD_USUARIO = @Usuario
and S.PASSWORD = @Clave
GO
/****** Object:  StoredProcedure [dbo].[ListarTemporadasPorCliente]    Script Date: 7/04/2026 15:13:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[ListarTemporadasPorCliente]
    @Cliente VARCHAR(100)
AS
BEGIN

SELECT 
cod_temcli + ' - ' + nom_temcli AS 'Temporada' 
--cod_temcli AS 'Temporada', 
--nom_temcli AS 'Descripcion' 
FROM TG_TemCli where cod_cliente in (
			SELECT c.cod_cliente
			FROM tg_cliente c
			where LTRIM(RTRIM(c.nom_cliente)) = LTRIM(RTRIM(@Cliente)))
order by cod_temcli

END
GO
/****** Object:  StoredProcedure [dbo].[SP_ActualizarRolUsuario]    Script Date: 7/04/2026 15:13:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[SP_ActualizarRolUsuario]
@IdUsuario INT,
@IdRol INT
AS
Declare @NombreRol as varchar(100) = ''

set @NombreRol = (select CASE @IdRol
								WHEN 1 THEN 'Visualizador'
								WHEN 2 THEN 'Editor'
								WHEN 3 THEN 'Administrador'
								ELSE 'Visualizador' -- Por seguridad, si hay un valor raro, le damos el menor privilegio
										END AS NombreRol)

--UPDATE dbo.AGO_Usuario
--SET 
--    IdRol = @IdRol,
--	NombreRol = @NombreRol
--WHERE 
--    IdUsuario = @IdUsuario;

UPDATE dbo.AGR_RolesUsuarioReceta
SET 
    IdRol = @IdRol,
	NombreRol = @NombreRol
WHERE 
    Usuario in (select S.COD_USUARIO FROM SEGURIDAD.dbo.SEG_USUARIOS S WHERE CONVERT(INT,S.dni_usuario) = @IdUsuario);
GO
/****** Object:  StoredProcedure [dbo].[SP_ELIMINAR_VISITA]    Script Date: 7/04/2026 15:13:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SP_ELIMINAR_VISITA]
    @IdVisita INT,
    @Comentario VARCHAR(500),
    @UsuarioElimina VARCHAR(100)
AS
BEGIN
-- ESTA ES LA LÍNEA CLAVE PARA SOLUCIONAR EL ERROR
    SET ARITHABORT ON; 
    SET ANSI_WARNINGS ON;
    SET ANSI_PADDING ON;
    SET ANSI_NULLS ON;
    SET QUOTED_IDENTIFIER ON;
    SET CONCAT_NULL_YIELDS_NULL ON;
    SET NUMERIC_ROUNDABORT OFF;

    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRAN;

        -- 1. Insertar el registro en la tabla histórica, adjuntando quién lo eliminó y por qué
		insert AGR_RecetasEliminadas (IdRecetaEliminada,NP,OperarioUDP,Tecnica,FechaUDP,Concepto,Ubicacion,Combo,
		Cliente,Estilo,Prendas,Temporada,Item,EstiloPropio,MotivoEliminacion,UsuarioEliminacion,FechaEliminacion)
		select idRecetas,NP,OperarioUDP,Tecnica,FechaUDP,Concepto,Ubicacion,Combo,Cliente,Estilo,Prendas,Temporada,
		Item,EstiloPropio,@Comentario,@UsuarioElimina,getdate() from dbo.AGR_Recetas WHERE IdRecetas = @IdVisita;
		  
        -- 2. Eliminar de la tabla original
		delete from AGR_Insumos where IdColor in 
		(select C.IdColor from AGR_Colores C 
		where C.IdRecetas = @IdVisita);

		delete from AGR_Colores 
		where IdRecetas = @IdVisita;

        DELETE FROM AGR_Recetas 
        WHERE IdRecetas = @IdVisita;

		SELECT 1 AS Resultado;
		COMMIT TRANSACTION;
		

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRAN;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END
GO
/****** Object:  StoredProcedure [dbo].[SP_GUARDAR_RECETA_COMPLETA]    Script Date: 7/04/2026 15:13:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SP_GUARDAR_RECETA_COMPLETA]
    @NP VARCHAR(20) = '',
    @XmlData XML
AS
BEGIN
-- ESTA ES LA LÍNEA CLAVE PARA SOLUCIONAR EL ERROR
    SET ARITHABORT ON; 
    SET ANSI_WARNINGS ON;
    SET ANSI_PADDING ON;
    SET ANSI_NULLS ON;
    SET QUOTED_IDENTIFIER ON;
    SET CONCAT_NULL_YIELDS_NULL ON;
    SET NUMERIC_ROUNDABORT OFF;

    SET NOCOUNT ON;
	-- Es buena práctica inicializar la transacción así
    SET XACT_ABORT ON;
	BEGIN TRY
    BEGIN TRANSACTION    
		-- 1. Insertar Cabecera usando los datos internos del XML
		--select * from Visita
        INSERT INTO AGR_Recetas (NP, OperarioUDP, Tecnica, FechaUDP, Cliente, Temporada, Estilo, EstiloPropio, Item, Combo, Prendas, Ubicacion, Arte, FechaRegistro) 
        SELECT 
            T.c.value('(NP)[1]', 'VARCHAR(20)'),
            T.c.value('(Operario)[1]', 'VARCHAR(100)'),
            T.c.value('(Tecnica)[1]', 'VARCHAR(100)'),
			T.c.value('(FechaUDP)[1]', 'VARCHAR(100)'),
			T.c.value('(Cliente)[1]', 'VARCHAR(100)'),
			T.c.value('(Temporada)[1]', 'VARCHAR(100)'),
			T.c.value('(Estilo)[1]', 'VARCHAR(100)'),
			T.c.value('(EstiloPropio)[1]', 'VARCHAR(100)'),
			T.c.value('(Item)[1]', 'VARCHAR(100)'),
			T.c.value('(ComboCabecera)[1]', 'VARCHAR(100)'),
			T.c.value('(PrendasReq)[1]', 'VARCHAR(100)'),
			T.c.value('(Ubicacion)[1]', 'VARCHAR(50)'), -- Nuevo
			T.c.value('(Arte)[1]', 'VARCHAR(100)'), -- Nuevo
            GETDATE()
        FROM @XmlData.nodes('/VisitaBE') AS T(c);
        
        DECLARE @IdVisita INT = SCOPE_IDENTITY();

        -- 2. Insertar Colores e Insumos usando OPENXML o .nodes()
        -- Creamos una tabla temporal para los colores insertados
        DECLARE @TabColores TABLE (IdGenerado INT, NombreColor VARCHAR(100));

        -- Insertamos Colores y capturamos sus IDs
        INSERT INTO AGR_Colores (IdRecetas, NombreColor, Combo)
        OUTPUT inserted.IdColor, inserted.NombreColor INTO @TabColores
        SELECT 
            @IdVisita,
            T.c.value('(Nombre)[1]', 'VARCHAR(100)'),
            T.c.value('(Combo)[1]', 'VARCHAR(50)')
        FROM @XmlData.nodes('/VisitaBE/Colores/ColorBE') AS T(c);

        -- 3. Insertar Insumos cruzando con los IDs de los colores
        INSERT INTO AGR_Insumos (IdColor, CodigoInsumo, Descripcion, Cantidad)
        SELECT 
            tc.IdGenerado,
            I.c.value('(Codigo)[1]', 'VARCHAR(50)'),
            I.c.value('(Descripcion)[1]', 'VARCHAR(200)'),
            I.c.value('(Cantidad)[1]', 'DECIMAL(18,2)')
        FROM @XmlData.nodes('/VisitaBE/Colores/ColorBE') AS T(c)
        CROSS APPLY T.c.nodes('Insumos/InsumoBE') AS I(c)
        INNER JOIN @TabColores tc ON tc.NombreColor = T.c.value('(Nombre)[1]', 'VARCHAR(100)');

        COMMIT TRANSACTION;
		SELECT 1 AS Resultado;
    END TRY
    BEGIN CATCH
        -- Punto y coma antes del IF es vital para que THROW no falle
        IF @@TRANCOUNT > 0 
            ROLLBACK TRANSACTION;

        -- Reemplazo de THROW por una forma más compatible si persiste el error
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO
/****** Object:  StoredProcedure [dbo].[SP_LISTAR_CLIENTES]    Script Date: 7/04/2026 15:13:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SP_LISTAR_CLIENTES]
AS    
SET NOCOUNT ON    
BEGIN    

SELECT nom_cliente AS 'Cliente' 
FROM tg_cliente order by nom_cliente
  
END 
GO
/****** Object:  StoredProcedure [dbo].[SP_LISTAR_ESTILOS_CLIE_PROP]    Script Date: 7/04/2026 15:13:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- 2. SP para listar Estilos filtrados
CREATE PROCEDURE [dbo].[SP_LISTAR_ESTILOS_CLIE_PROP]
    @Cliente VARCHAR(100),
    @Temporada VARCHAR(100)
AS
BEGIN
    -- Ajusta el nombre de tu tabla real de estilos
	SELECT 
	DISTINCT 
	LTRIM(RTRIM(COD_ESTCLI)) AS 'CodEstiloCliente',
	LTRIM(RTRIM(Cod_EstPro)) AS 'CodEstiloPropio'
	--Cod_EstPro
	--COD_ESTPRO
	FROM TG_ESTCLIEST
	WHERE COD_CLIENTE in (
			SELECT c.cod_cliente
			FROM tg_cliente c
			where LTRIM(RTRIM(c.nom_cliente)) = LTRIM(RTRIM(@Cliente)))
			AND COD_TEMCLI = @Temporada
	GROUP BY COD_ESTCLI,Cod_EstPro
	--COD_ESTPRO

END
GO
/****** Object:  StoredProcedure [dbo].[SP_LISTAR_INSUMOS]    Script Date: 7/04/2026 15:13:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SP_LISTAR_INSUMOS]
AS  
SET NOCOUNT ON  
BEGIN  

SELECT 
    a.Cod_Item AS Codigo, 
    a.Des_Item AS Descripcion, 
    a.Cod_UniMed AS Unid_Med,
	(convert(varchar(20),CONVERT(DECIMAL(18, 2), ISNULL(t.CAN_STOCK, 0))) + ' ' +  a.Cod_UniMed) AS Stock
FROM LG_ITEM AS a
INNER JOIN LG_FAMITE AS b ON a.Cod_FamItem = b.Cod_FamItem
LEFT JOIN LG_STOCKSITEM t ON a.Cod_Item = t.Cod_Item AND t.COD_ALMACEN in ('51') and isnull(t.CAN_STOCK,0) <> 0
WHERE b.Flg_QyC = 'S'
ORDER BY 1 ASC;

--select * from (
--SELECT B.COD_ITEM AS Codigo
--		,RTRIM(B.DES_ITEM) AS Descripcion
--		,B.COD_UNIMED AS 'UNID_MED'
--		,LOTEPROV = SPACE(100)
--		,STATUS_LOTE = SPACE(30)
--		,convert(varchar(20),CONVERT(DECIMAL(18, 2), ISNULL(A.CAN_STOCK, 0))) + ' ' +  B.COD_UNIMED AS STOCK
--		,Can_Reservado = convert(numeric(18,5),0)    
--		,A.FEC_ULT_ENTRADA AS 'ULT_ENTRADA'
--		,A.FEC_ULT_SALIDA AS 'ULT_SALIDA'
--		,C.DES_PROVEEDOR AS PROVEEDOR
--		,PRE_ULTCOMP AS PRECIO_ULT_COMPRA
--		,COD_MONULTCOMP AS MONEDA
--		,A.CAN_STOCK * PRE_ULTCOMP AS IMPORTE
--		,B.COD_FAMITEM
--		,D.DES_FAMITEM
--		,B.COD_GRUITEM
--		,ISNULL(E.DES_FAMGRUITE, '') AS DES_FAMGRUITE
--		,A.Lote
--		,COD_BARRA = A.Cod_Item + SPACE(30)
--	FROM LG_STOCKSITEM A
--	INNER JOIN LG_ITEM B ON A.COD_ITEM = B.COD_ITEM
--	LEFT OUTER JOIN LG_PROVEEDOR C ON B.COD_PROVEEDOR = C.COD_PROVEEDOR
--	INNER JOIN LG_FAMITE D ON B.COD_FAMITEM = D.COD_FAMITEM
--	LEFT OUTER JOIN LG_FAMGRUITE E ON B.COD_FAMITEM = E.COD_FAMITEM
--		AND B.COD_GRUITEM = E.COD_GRUITEM
--	WHERE A.COD_ALMACEN in ('51')
--		 AND B.COD_FAMITEM IN (
--			'AE','AS','AU','AX','BC','CG','DC','EX','IC','PI','PL','PM','QP','RC','RX','SW','TS','WA'
--			)
--		AND ISNULL(A.CAN_STOCK, 0) > 0	
--    AND D.flg_trabaja_por_lotes = 'N'
--union all
-- 	SELECT B.COD_ITEM AS CODIGO
--		,RTRIM(B.DES_ITEM) AS NOMBRE
--		,B.COD_UNIMED AS 'UNID_MED'
--		,LOTEPROV  = F.Cod_OrdProv 
--		,STATUS_LOTE = CASE F.Flg_Status WHEN 'P' THEN 'Por Aprobar' ELSE 'Aprobado' END 
--		,convert(varchar(20),CONVERT(DECIMAL(18, 2), ISNULL(A.CAN_STOCK, 0))) + ' ' +  B.COD_UNIMED AS STOCK
--		,Can_Reservado
--		,A.FEC_ULT_ENTRADA AS 'ULT_ENTRADA'
--		,A.FEC_ULT_SALIDA AS 'ULT_SALIDA'
--		,C.DES_PROVEEDOR AS PROVEEDOR
--		,PRE_ULTCOMP AS PRECIO_ULT_COMPRA
--		,COD_MONULTCOMP AS MONEDA
--		,A.CAN_STOCK * PRE_ULTCOMP AS IMPORTE
--		,B.COD_FAMITEM
--		,D.DES_FAMITEM
--		,B.COD_GRUITEM
--		,ISNULL(E.DES_FAMGRUITE, '') AS DES_FAMGRUITE
--		,A.Lote
--		,COD_BARRA = A.COD_ITEM + '_'+   RTRIM(F.Cod_OrdProv)  + '_'  + RTRIM(dbo.uf_strzero(A.Lote,4)) 
--	FROM Lg_StocksItem_Lote A
--	INNER JOIN LG_ITEM B ON A.COD_ITEM = B.COD_ITEM
--	INNER JOIN LG_FAMITE D ON B.COD_FAMITEM = D.COD_FAMITEM
--	LEFT OUTER JOIN LG_FAMGRUITE E ON B.COD_FAMITEM = E.COD_FAMITEM
--		AND B.COD_GRUITEM = E.COD_GRUITEM
--	INNER JOIN Lg_Item_Lotes F ON A.Cod_Item = F.Cod_Item AND A.Lote = F.Lote  
--	LEFT OUTER JOIN LG_PROVEEDOR C ON F.COD_PROVEEDOR = C.COD_PROVEEDOR
--	WHERE A.COD_ALMACEN in ('51')
--		 AND B.COD_FAMITEM IN (
--			'AE','AS','AU','AX','BC','CG','DC','EX','IC','PI','PL','PM','QP','RC','RX','SW','TS','WA'
--			)
--		AND ISNULL(A.CAN_STOCK, 0) > 0	
--    AND D.flg_trabaja_por_lotes = 'S'
--) Z
--order by Z.Descripcion asc

END
GO
/****** Object:  StoredProcedure [dbo].[SP_LISTAR_ITEMS]    Script Date: 7/04/2026 15:13:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- 1. SP para listar Items filtrados
CREATE PROCEDURE [dbo].[SP_LISTAR_ITEMS]
    @Cliente VARCHAR(100),
    @Temporada VARCHAR(100)
AS
BEGIN
 --   -- Ajusta el nombre de tu tabla real de Items
	--SELECT DISTINCT LTRIM(RTRIM(A.COD_ITEM)) AS 'Item'
	--FROM LG_ITEMTEMCLI A
	--	,LG_ITEM B
	--	,LG_FAMITE D
	--WHERE A.COD_CLIENTE in (
	--		SELECT c.cod_cliente
	--		FROM tg_cliente c
	--		where LTRIM(RTRIM(c.nom_cliente)) = LTRIM(RTRIM(@Cliente)))
	--	AND A.COD_TEMCLI = @Temporada
	--	AND A.COD_ITEM = B.COD_ITEM
	--	AND B.COD_FAMITEM = D.COD_FAMITEM
	--	AND D.FLG_PROCESO_CONFEC = 'S'
	--	--agregado
	--	and A.COD_ITEM like 'ES%'

	--sintaxis mejorada
	SELECT DISTINCT A.COD_ITEM AS 'CodItem'
		FROM LG_ITEMTEMCLI A
		INNER JOIN LG_ITEM B ON A.COD_ITEM = B.COD_ITEM
		INNER JOIN LG_FAMITE D ON B.COD_FAMITEM = D.COD_FAMITEM
		INNER JOIN tg_cliente C ON A.COD_CLIENTE = C.cod_cliente
		WHERE C.nom_cliente = @Cliente
		  AND A.COD_TEMCLI = @Temporada
		  AND D.FLG_PROCESO_CONFEC = 'S'
		  AND A.COD_ITEM LIKE 'ES%'
END
GO
/****** Object:  StoredProcedure [dbo].[SP_LISTAR_TECNICAS]    Script Date: 7/04/2026 15:13:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SP_LISTAR_TECNICAS]
AS  
SET NOCOUNT ON  
BEGIN  

 select 
 cod_tecnica AS Codigo , 
 LTRIM(descripcion) AS NombreTecnica , 
 Merma 
 from Es_Tecnica_Aplicaciones   
 ORDER BY LTRIM(descripcion) ASC

END
GO
/****** Object:  StoredProcedure [dbo].[SP_ListarCombosPorEstilo]    Script Date: 7/04/2026 15:13:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- 1. SP para listar Combo  
CREATE PROCEDURE [dbo].[SP_ListarCombosPorEstilo]  
(@Cliente   VARCHAR(200) = '',  
@Temporada  VARCHAR(50) = '',  
@Estilo     VARCHAR(50) = '' 
)  
AS  
BEGIN  
 SELECT distinct
    --E.COD_PRESENT, 
    E.Des_present as 'combo'
    --RIGHT('000' + LTRIM(RTRIM(E.COD_PRESENT)), 3) + '-' + E.Des_present AS 'Presentacion_Formateada'
FROM ES_ESTPROPRE E
WHERE E.COD_ESTPRO = @Estilo
and E.COD_PRESENT in (
        SELECT A.COD_PRESENT 
        FROM ES_ESTCOMPCOL A 
        WHERE A.COD_ESTPRO = @Estilo
          AND A.COD_PRESENT = E.COD_PRESENT
  )
ORDER BY E.Des_present ASC;

END  
GO
/****** Object:  StoredProcedure [dbo].[SP_ListarConceptosPendientes]    Script Date: 7/04/2026 15:13:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- 1. SP para listar Conceptos
CREATE PROCEDURE [dbo].[SP_ListarConceptosPendientes]
(@Cliente   VARCHAR(200) = '',
@Temporada  VARCHAR(50) = '',
@Estilo     VARCHAR(50) = '',
@Item       VARCHAR(50) = ''
)
AS
BEGIN
    -- Ajusta "TablaConceptos" y "NombreConcepto" al nombre real de tu tabla y columna
    SELECT NombreConcepto as Concepto FROM AGR_Conceptos 
	where NombreConcepto not in (SELECT V.Concepto FROM DBO.AGR_Recetas V
									WHERE V.Cliente = @Cliente AND V.Temporada = @Temporada 
									AND V.Estilo = @Estilo AND V.Item = @Item)
	ORDER BY NombreConcepto ASC
END
GO
/****** Object:  StoredProcedure [dbo].[SP_ListarubicacionesPendientes]    Script Date: 7/04/2026 15:13:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- 1. SP para listar Conceptos
CREATE PROCEDURE [dbo].[SP_ListarubicacionesPendientes]
(@Cliente   VARCHAR(200) = '',
@Temporada  VARCHAR(50) = '',
@Estilo     VARCHAR(50) = '',
@Item       VARCHAR(50) = ''
)
AS
BEGIN
    -- Ajusta "TablaConceptos" y "NombreConcepto" al nombre real de tu tabla y columna
    SELECT CodUbicacion as Ubicacion FROM AGR_Ubicaciones  U
	where CodUbicacion not in (SELECT V.Ubicacion FROM DBO.AGR_Recetas V
									WHERE V.Cliente = @Cliente AND V.Temporada = @Temporada 
									AND V.Estilo = @Estilo AND V.Item = @Item)
	ORDER BY CodUbicacion ASC
END

GO
/****** Object:  StoredProcedure [dbo].[SP_ListarUsuariosRoles]    Script Date: 7/04/2026 15:13:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SP_ListarUsuariosRoles]
AS
--SELECT 
--IDUsuario
--,(ISNULL(Nombres,'') + ' ' + ISNULL(ApellidoPaterno,'') + ' ' + ISNULL(ApellidoMaterno,'')) AS 'NombreUsuario'
--,IdRol
--,NombreRol
--  FROM dbo.AGO_Usuario

select 
CONVERT(INT,dni_usuario) as 'IDUsuario', 
CASE WHEN S.nombre_trabajador = '' 
THEN S.Nom_Usuario 
ELSE (ISNULL(S.nombre_trabajador,'') + ' ' + ISNULL(S.Apellido_Paterno,'') + ' ' + ISNULL(S.Apellido_Materno,''))
END as 'NombreUsuario', 
isnull(R.IdRol,'1') as IdRol,
isnull(R.NombreRol,'Visualizador') as NombreRol
FROM SEGURIDAD.dbo.SEG_USUARIOS S inner join AGR_RolesUsuarioReceta R
on S.COD_USUARIO = R.Usuario
GO
/****** Object:  StoredProcedure [dbo].[SP_VISITA_LISTAR_MANTENIMIENTO]    Script Date: 7/04/2026 15:13:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
--Para implementar el mantenimiento, el procedimiento almacenado debe ser eficiente y devolver la información necesaria para llenar la tabla de búsqueda.

--Aquí tienes la estructura de SP_VISITA_LISTAR_MANTENIMIENTO. He incluido filtros básicos (por NP o Rango de Fechas) para que el mantenimiento sea realmente funcional:

--SQL

CREATE PROCEDURE [dbo].[SP_VISITA_LISTAR_MANTENIMIENTO]
    @NP VARCHAR(20) = NULL,
    @FechaInicio VARCHAR(10) = NULL, -- Formato 'DD/MM/YYYY'
    @FechaFin VARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Variables para manejo de fechas
    DECLARE @FInicio DATETIME = NULL
    DECLARE @FFin DATETIME = NULL

    -- Convertir fechas si vienen como parámetros
    IF ISNULL(@FechaInicio, '') <> '' 
        SET @FInicio = CONVERT(DATETIME, @FechaInicio, 103)
    
    IF ISNULL(@FechaFin, '') <> '' 
        SET @FFin = DATEADD(DAY, 1, CONVERT(DATETIME, @FechaFin, 103))

	SELECT 
		NP,
        IdRecetas AS Dato, -- Usamos el alias Dato para mapear con VisitaBE.cs
		Cliente,
		Temporada,
		Estilo,
		EstiloPropio,
		Item,
		OperarioUDP,
        Tecnica,
		Combo as 'ComboCabecera',
        Ubicacion,
        -- Formateamos la fecha para que el JS la lea directamente
        CONVERT(VARCHAR, FechaUDP, 103) AS FechaUDP,
		CONVERT(VARCHAR, FechaRegistro, 103) AS FechaRegistro,
        CONVERT(VARCHAR, FechaRegistro, 108) AS HoraRegistro
    FROM AGR_Recetas (NOLOCK)
    WHERE (@NP IS NULL OR NP LIKE '%' + @NP + '%')
      AND (@FInicio IS NULL OR FechaRegistro >= @FInicio)
      AND (@FFin IS NULL OR FechaRegistro < @FFin)
    ORDER BY IdRecetas DESC -- Mostrar los más recientes primero
END
GO
/****** Object:  StoredProcedure [dbo].[USP_ACTUALIZAR_RECETA_SCTR_XML]    Script Date: 7/04/2026 15:13:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[USP_ACTUALIZAR_RECETA_SCTR_XML]
    @XML_DATA XML
AS
BEGIN
-- ESTA ES LA LÍNEA CLAVE PARA SOLUCIONAR EL ERROR
    SET ARITHABORT ON; 
    SET ANSI_WARNINGS ON;
    SET ANSI_PADDING ON;
    SET ANSI_NULLS ON;
    SET QUOTED_IDENTIFIER ON;
    SET CONCAT_NULL_YIELDS_NULL ON;
    SET NUMERIC_ROUNDABORT OFF;

    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION
            -- 1. Capturar ID de la visita desde el XML
            DECLARE @IdVisita INT = @XML_DATA.value('(VisitaBE/Dato)[1]', 'INT');

            -- 2. Actualizar Cabecera
            UPDATE AGR_Recetas SET 
				NP = @XML_DATA.value('(VisitaBE/NP)[1]', 'VARCHAR(100)'),
				OperarioUDP = @XML_DATA.value('(VisitaBE/Operario)[1]', 'VARCHAR(100)'),
                Tecnica = @XML_DATA.value('(VisitaBE/Tecnica)[1]', 'VARCHAR(100)'),		
                FechaUDP = @XML_DATA.value('(VisitaBE/FechaUDP)[1]', 'VARCHAR(100)'),				
				--Cliente = @XML_DATA.value('(VisitaBE/Cliente)[1]', 'VARCHAR(100)'),
				--Temporada = @XML_DATA.value('(VisitaBE/Temporada)[1]', 'VARCHAR(100)'),
				--Estilo = @XML_DATA.value('(VisitaBE/Estilo)[1]', 'VARCHAR(100)'),
				--Item = @XML_DATA.value('(VisitaBE/Item)[1]', 'VARCHAR(100)'),
				--Combo = @XML_DATA.value('(VisitaBE/ComboCabecera)[1]', 'VARCHAR(100)'),
				Prendas = @XML_DATA.value('(VisitaBE/PrendasReq)[1]', 'VARCHAR(100)'),
				--Concepto = @XML_DATA.value('(VisitaBE/Concepto)[1]', 'VARCHAR(100)'),
                --Ubicacion = @XML_DATA.value('(VisitaBE/Ubicacion)[1]', 'VARCHAR(50)'),
				FechaUpdate = getdate()
            WHERE IdRecetas = @IdVisita;

            -- 3. Limpiar detalle anterior para reinsertar (o usar un MERGE)           

			DELETE FROM AGR_Insumos WHERE IdColor in (select C.IdColor from AGR_Colores C WHERE C.IdRecetas = @IdVisita);

			DELETE FROM AGR_Colores WHERE IdRecetas = @IdVisita;

			-- 4. Insertar Colores e Insumos usando OPENXML o .nodes()
			-- Creamos una tabla temporal para los colores insertados
			DECLARE @TabColores TABLE (IdGenerado INT, NombreColor VARCHAR(100));

			-- Insertamos Colores y capturamos sus IDs
			INSERT INTO AGR_Colores (IdRecetas, NombreColor, Combo)
			OUTPUT inserted.IdColor, inserted.NombreColor INTO @TabColores
			SELECT 
				@IdVisita,
				T.c.value('(Nombre)[1]', 'VARCHAR(100)'),
				T.c.value('(Combo)[1]', 'VARCHAR(50)')
			FROM @XML_DATA.nodes('/VisitaBE/Colores/ColorBE') AS T(c);

			-- 3. Insertar Insumos cruzando con los IDs de los colores
			INSERT INTO AGR_Insumos (IdColor, CodigoInsumo, Descripcion, Cantidad)
			SELECT 
				tc.IdGenerado,
				I.c.value('(CodigoInsumo)[1]', 'VARCHAR(50)'),
				I.c.value('(Descripcion)[1]', 'VARCHAR(200)'),
				I.c.value('(Cantidad)[1]', 'DECIMAL(18,2)')
			FROM @XML_DATA.nodes('/VisitaBE/Colores/ColorBE') AS T(c)
			CROSS APPLY T.c.nodes('Insumos/InsumoBE') AS I(c)
			INNER JOIN @TabColores tc ON tc.NombreColor = T.c.value('(Nombre)[1]', 'VARCHAR(100)');
			
			--------------------------------------------------------------------
			--01/04/2026
			-- Variables para leer el ID principal de la receta
			-- Opcional: Eliminar las pruebas anteriores de esta receta si es una actualización
			DELETE FROM AGR_Insumos_Prueba WHERE IdRecetas = @IdVisita;

			-- Insertar todas las pruebas leyendo los nodos anidados del XML
			INSERT INTO AGR_Insumos_Prueba (IdRecetas, NombreColor, CodigoInsumo, NombrePrueba, GramosUDP, EsPrincipal)
			SELECT 
				@IdVisita AS IdRecetas,
				Color.value('(Nombre)[1]', 'VARCHAR(50)') AS IdColor,
				Insumo.value('(CodigoInsumo)[1]', 'VARCHAR(50)') AS IdInsumo,
				Prueba.value('(NombrePrueba)[1]', 'VARCHAR(100)') AS NombrePrueba,
				Prueba.value('(GramosUDP)[1]', 'DECIMAL(18,4)') AS GramosUDP,
				Prueba.value('(EsPrincipal)[1]', 'BIT') AS EsPrincipal
			FROM @XML_DATA.nodes('//VisitaBE/Colores/ColorBE') AS C(Color)
			CROSS APPLY Color.nodes('Insumos/InsumoBE') AS I(Insumo)
			CROSS APPLY Insumo.nodes('PruebasUDP/PruebaUDPBE') AS P(Prueba);
			--------------------------------------------------------------------
			--------------------------------------------------------------------

            SELECT 1 AS Resultado;
        COMMIT TRANSACTION
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT ERROR_MESSAGE();
    END CATCH
END
GO
/****** Object:  StoredProcedure [dbo].[USP_ActualizarEstadoVisita]    Script Date: 7/04/2026 15:13:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[USP_ActualizarEstadoVisita]
    @IdVisita INT,
    @Usuario VARCHAR(100),
    @Accion INT, -- 1 para CERRAR, 0 para ABRIR
	@Comentario VARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @Accion = 1
        BEGIN
            -- Lógica para CERRAR la visita
            UPDATE AGR_Recetas
            SET 
                UsuarioCierre = @Usuario,
                FechaCierre = (CONVERT(varchar(10), GETDATE(), 103) + ' ' + CONVERT(varchar(8), GETDATE(), 108)),
                -- Al cerrar, limpiamos los datos de la última apertura para auditoría limpia
                UsuarioApertura = NULL,
                FechaApertura = NULL,
				ComentarioAbrir = null,
				ComentarioCerrar = @Comentario
            WHERE IdRecetas = @IdVisita; -- Ajustar nombre de columna ID según tu tabla
        END
        ELSE
        BEGIN
            -- Lógica para ABRIR la visita
            UPDATE AGR_Recetas
            SET 
                UsuarioApertura = @Usuario,
                FechaApertura = (CONVERT(varchar(10), GETDATE(), 103) + ' ' + CONVERT(varchar(8), GETDATE(), 108)),
                -- Al abrir, eliminamos el bloqueo de cierre
                UsuarioCierre = NULL,
                FechaCierre = NULL,
				ComentarioAbrir = @Comentario,
				ComentarioCerrar = null
            WHERE IdRecetas = @IdVisita;
        END

        COMMIT TRANSACTION;
        
        -- Retornar éxito
        SELECT 1 AS Resultado, 'Estado actualizado correctamente' AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        -- Retornar error
        SELECT 0 AS Resultado, ERROR_MESSAGE() AS Mensaje;
    END CATCH
END


GO

-- NUEVA FUNCIONALIDAD: BÚSQUEDA INVERSA POR ESTILO
GO
CREATE PROCEDURE [dbo].[SP_LISTAR_TODOS_ESTILOS]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT 
        LTRIM(RTRIM(COD_ESTCLI)) AS 'CodEstiloCliente',
        LTRIM(RTRIM(Cod_EstPro)) AS 'CodEstiloPropio'
    FROM TG_ESTCLIEST
    WHERE COD_ESTCLI IS NOT NULL AND Cod_EstPro IS NOT NULL
END
GO
CREATE PROCEDURE [dbo].[SP_OBTENER_DATOS_ESTILO]
    @EstiloBuscar VARCHAR(100),
    @EsPropio BIT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP 1
        LTRIM(RTRIM(c.nom_cliente)) AS 'Cliente',
        LTRIM(RTRIM(e.COD_TEMCLI)) + ' - ' + LTRIM(RTRIM(t.nom_temcli)) AS 'Temporada',
        LTRIM(RTRIM(e.COD_ESTCLI)) AS 'CodEstiloCliente',
        LTRIM(RTRIM(e.Cod_EstPro)) AS 'CodEstiloPropio'
    FROM TG_ESTCLIEST e
    inner join TG_TemCli t on e.cod_temcli = t.cod_temcli and e.COD_CLIENTE = t.COD_CLIENTE 
    INNER JOIN tg_cliente c ON e.COD_CLIENTE = c.cod_cliente
    WHERE 
        (@EsPropio = 0 AND LTRIM(RTRIM(e.COD_ESTCLI)) = @EstiloBuscar) OR
        (@EsPropio = 1 AND LTRIM(RTRIM(e.Cod_EstPro)) = @EstiloBuscar)
END
GO
/****** Object:  StoredProcedure [dbo].[USP_VISITA_OBTENER_COMPLETO]    Script Date: 7/04/2026 15:13:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[USP_VISITA_OBTENER_COMPLETO]
    @ID_VISITA INT
AS
BEGIN
 --   -- Tabla 1: Cabecera
	--SELECT 
	--V.NP, 
	--V.OperarioUDP as 'Operario', 
	--V.Tecnica, 
	--V.Concepto, 
	--V.Ubicacion, 
	--V.Arte,
	--CONVERT(VARCHAR, V.FechaUDP, 103) as 'FechaUDP',
	--V.Cliente as 'Cliente',  
	--rtrim(V.Estilo) AS 'Estilo',  
	--rtrim(V.EstiloPropio) AS 'EstiloPropio', 
	--rtrim(V.Combo) AS 'ComboCabecera',  
	--V.Prendas as 'PrendasReq',
	--rtrim(V.Temporada) as 'Temporada',
	--rtrim(V.Item) as 'Item',
	--isnull(V.UsuarioCierre,'') as 'UsuarioCierre',  
	--isnull(V.FechaCierre,'') as 'FechaCierre',  
	--isnull(V.UsuarioApertura,'') as 'UsuarioApertura',  
	--isnull(V.FechaApertura,'') as 'FechaApertura',
	--CONVERT(VARCHAR, V.FechaRegistro, 103) AS FechaRegistro,
 --   CONVERT(VARCHAR, V.FechaRegistro, 108) AS HoraRegistro
	--FROM AGR_Recetas V 
	--WHERE IdRecetas = @ID_VISITA;

 --   -- Tabla 2: Detalle unido
 --   SELECT 
	--C.IdColor as IdColor, 
	--I.IdInsumo as IdInsumo, 
	--C.NombreColor as NombreColor, 
	--rtrim(C.Combo) as ComboColor, 
	--I.codigoInsumo,
 --   I.Descripcion as InsumoDescripcion, 
	--I.Cantidad as CantidadInsumo,
	--P.IdPrueba
 --   FROM AGR_Colores C
 --   INNER JOIN AGR_Insumos I ON C.IdColor = I.IdColor
	--LEFT JOIN AGR_Insumos_Prueba P ON P.NombreColor = C.NombreColor and P.CodigoInsumo = I.CodigoInsumo
 --   WHERE C.IdRecetas = @ID_VISITA;

	---- Tabla 3: Detalle Pruebas agregadas
	--SELECT 
	--	NombreColor, 
	--	CodigoInsumo, 
	--	NombrePrueba, 
	--	GramosUDP, 
	--	EsPrincipal,
	--	IdPrueba -- Para mantener el ID correlativo
	--FROM dbo.AGR_Insumos_Prueba
	--WHERE IdRecetas = @ID_VISITA -- O el parámetro que uses para filtrar la receta

	-- ==========================================
    -- Tabla 1: Cabecera
    -- ==========================================
	SELECT 
	V.NP, 
	V.OperarioUDP as 'Operario', 
	V.Tecnica, 
	V.Ubicacion, 
	V.Arte,
	CONVERT(VARCHAR, V.FechaUDP, 103) as 'FechaUDP',
	V.Cliente as 'Cliente',  
	rtrim(V.Estilo) AS 'Estilo',  
	rtrim(V.EstiloPropio) AS 'EstiloPropio', 
	rtrim(V.Combo) AS 'ComboCabecera',  
	V.Prendas as 'PrendasReq',
	rtrim(V.Temporada) as 'Temporada',
	rtrim(V.Item) as 'Item',
	isnull(V.UsuarioCierre,'') as 'UsuarioCierre',  
	isnull(V.FechaCierre,'') as 'FechaCierre',  
	isnull(V.UsuarioApertura,'') as 'UsuarioApertura',  
	isnull(V.FechaApertura,'') as 'FechaApertura',
	CONVERT(VARCHAR, V.FechaRegistro, 103) AS FechaRegistro,
    CONVERT(VARCHAR, V.FechaRegistro, 108) AS HoraRegistro
	FROM AGR_Recetas V 
	WHERE IdRecetas = @ID_VISITA;

    -- ==========================================
    -- Tabla 2: Detalle (Solo Colores e Insumos)
    -- ==========================================
    SELECT 
	C.IdColor as IdColor, 
	I.IdInsumo as IdInsumo, 
	C.NombreColor as NombreColor, 
	(C.Combo) as ComboColor, 
	I.codigoInsumo AS CodigoInsumo, -- Se agregó el AS para que C# lo lea exacto
    I.Descripcion as InsumoDescripcion, 
	I.Cantidad as CantidadInsumo
    FROM AGR_Colores C
    INNER JOIN AGR_Insumos I ON C.IdColor = I.IdColor
    WHERE C.IdRecetas = @ID_VISITA;
    -- ⚠️ OJO: Se quitó el LEFT JOIN a AGR_Insumos_Prueba para evitar duplicar Insumos en C#

    -- ==========================================
	-- Tabla 3: Detalle Pruebas agregadas (Columnas dinámicas)
    -- ==========================================
	SELECT 
		NombreColor, 
		CodigoInsumo, 
		NombrePrueba, 
		ISNULL(GramosUDP, 0) AS GramosUDP, -- Blindaje contra nulos
		ISNULL(EsPrincipal, 0) AS EsPrincipal, -- Blindaje contra nulos
		IdPrueba 
	FROM dbo.AGR_Insumos_Prueba
	WHERE IdRecetas = @ID_VISITA;

END


GO
