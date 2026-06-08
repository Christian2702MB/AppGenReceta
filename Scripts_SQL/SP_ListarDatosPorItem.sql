ALTER PROCEDURE [dbo].[SP_ListarDatosPorItem]    
(@Item   VARCHAR(200) = ''   
)    
AS    
BEGIN 
	-- 0. Optimización: Buscar primero los items que coinciden para evitar Table Scans pesados en otras tablas.
	SELECT DISTINCT A.COD_ITEM 
	INTO #TempItems
	FROM LG_ITEMTEMCLI A
	INNER JOIN LG_ITEM B ON A.COD_ITEM = B.COD_ITEM
	INNER JOIN LG_FAMITE D ON B.COD_FAMITEM = D.COD_FAMITEM
	WHERE D.FLG_PROCESO_CONFEC = 'S' 
	  AND B.Cod_FamItem = 'ES'
	  AND A.COD_ITEM LIKE '%' + @Item + '%'

	-- 1. Result Set Cabecera
	SELECT   
			(SELECT top 1 I.nom_cliente FROM tg_cliente I where I.COD_CLIENTE = A.COD_CLIENTE) as 'Cliente'
			,(SELECT T.cod_temcli + ' - ' + T.nom_temcli FROM TG_TemCli T where T.cod_temcli = A.COD_TEMCLI) as 'Temporada'
			,A.COD_ITEM   as 'Item'
			,B.Ubicacion  as 'Ubicacion'
			,b.Cod_Tecnica as 'Cod_Tecnica'
			,ISNULL(N.DESCRIPCION, '')  as 'Tecnica'
		FROM LG_ITEMTEMCLI A
			INNER JOIN LG_ITEM B ON A.COD_ITEM = B.COD_ITEM
			INNER JOIN LG_FAMITE D ON B.COD_FAMITEM = D.COD_FAMITEM
			LEFT JOIN ES_TECNICA_APLICACIONES N ON B.Cod_Tecnica = N.COD_TECNICA
		WHERE D.FLG_PROCESO_CONFEC = 'S' 
		  AND B.Cod_FamItem = 'ES'
		  AND A.COD_ITEM IN (SELECT COD_ITEM FROM #TempItems)

	-- 2. Result Set Estilos (Cliente y Propio)
	SELECT DISTINCT 
	LTRIM(RTRIM(T.COD_ESTCLI)) AS 'CodEstiloCliente',
	LTRIM(RTRIM(T.Cod_EstPro)) AS 'CodEstiloPropio'
	FROM ES_ESTPROCOMP A  
	inner JOIN TG_ESTCLIEST T ON A.COD_ESTPRO = T.Cod_EstPro
	WHERE A.COD_ITEM IN (SELECT COD_ITEM FROM #TempItems)

	-- 3. Result Set Combos
	SELECT DISTINCT
    E.Des_present as 'Combo'
	FROM ES_ESTPROPRE E
	INNER JOIN ES_ESTPROCOMP A ON E.COD_ESTPRO = A.COD_ESTPRO
	WHERE A.COD_ITEM IN (SELECT COD_ITEM FROM #TempItems)

	DROP TABLE #TempItems
END    