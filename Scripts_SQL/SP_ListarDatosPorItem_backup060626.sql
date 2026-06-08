CREATE PROCEDURE [dbo].[SP_ListarDatosPorItem_backup060626]    
(@Item   VARCHAR(200) = ''   
)    
AS    
BEGIN 
	SELECT   
			(SELECT top 1 I.nom_cliente FROM tg_cliente I where I.COD_CLIENTE = A.COD_CLIENTE) as 'Cliente'
			,(SELECT T.cod_temcli + ' - ' + T.nom_temcli FROM TG_TemCli T where T.cod_temcli = A.COD_TEMCLI) as 'Temporada'
			,A.COD_ITEM   as 'Item'
			,B.Ubicacion  as 'Ubicacion'
			,b.Cod_Tecnica as 'Cod_Tecnica'
			,ISNULL(N.DESCRIPCION, '')  as 'Tecnica'
		FROM LG_ITEMTEMCLI A
			,LG_ITEM B
			,LG_FAMITE D
			,ES_TECNICA_APLICACIONES AS N 
		WHERE A.COD_ITEM = B.COD_ITEM
			AND B.COD_FAMITEM = D.COD_FAMITEM		
			AND D.FLG_PROCESO_CONFEC = 'S'
			AND B.Cod_Tecnica = N.COD_TECNICA
			and B.Cod_FamItem = 'ES'
			AND A.COD_ITEM LIKE '%' + @Item + '%'  
END    
