CREATE PROCEDURE [dbo].[SP_LISTAR_INSUMOS]  
AS    
SET NOCOUNT ON    
BEGIN    
  
SELECT      
a.Cod_Item AS Codigo,      
a.Des_Item AS Descripcion,      
a.Cod_UniMed AS Unid_Med,  
(convert(varchar(20),CONVERT(DECIMAL(18, 2), ISNULL(t.CAN_STOCK, 0))) + ' ' +  a.Cod_UniMed) 
AS Stock FROM LG_ITEM AS a 
INNER JOIN LG_FAMITE AS b ON a.Cod_FamItem = b.Cod_FamItem 
LEFT JOIN LG_STOCKSITEM t ON a.Cod_Item = t.Cod_Item AND t.COD_ALMACEN in ('51') and isnull(t.CAN_STOCK,0) <> 0 WHERE b.Flg_QyC = 'S'  
ORDER BY 1 ASC;  
  
--select * from (  
--SELECT B.COD_ITEM AS Codigo  
--  ,RTRIM(B.DES_ITEM) AS Descripcion  
--  ,B.COD_UNIMED AS 'UNID_MED'  
--  ,LOTEPROV = SPACE(100)  
--  ,STATUS_LOTE = SPACE(30)  
--  ,convert(varchar(20),CONVERT(DECIMAL(18, 2), ISNULL(A.CAN_STOCK, 0))) + ' ' +  B.COD_UNIMED AS STOCK  
--  ,Can_Reservado = convert(numeric(18,5),0)      
--  ,A.FEC_ULT_ENTRADA AS 'ULT_ENTRADA'  
--  ,A.FEC_ULT_SALIDA AS 'ULT_SALIDA'  
--  ,C.DES_PROVEEDOR AS PROVEEDOR  
--  ,PRE_ULTCOMP AS PRECIO_ULT_COMPRA  
--  ,COD_MONULTCOMP AS MONEDA  
--  ,A.CAN_STOCK * PRE_ULTCOMP AS IMPORTE  
--  ,B.COD_FAMITEM  
--  ,D.DES_FAMITEM  
--  ,B.COD_GRUITEM  
--  ,ISNULL(E.DES_FAMGRUITE, '') AS DES_FAMGRUITE  
--  ,A.Lote  
--  ,COD_BARRA = A.Cod_Item + SPACE(30)  
-- FROM LG_STOCKSITEM A  
-- INNER JOIN LG_ITEM B ON A.COD_ITEM = B.COD_ITEM  
-- LEFT OUTER JOIN LG_PROVEEDOR C ON B.COD_PROVEEDOR = C.COD_PROVEEDOR  
-- INNER JOIN LG_FAMITE D ON B.COD_FAMITEM = D.COD_FAMITEM  
-- LEFT OUTER JOIN LG_FAMGRUITE E ON B.COD_FAMITEM = E.COD_FAMITEM  
--  AND B.COD_GRUITEM = E.COD_GRUITEM  
-- WHERE A.COD_ALMACEN in ('51')  
--   AND B.COD_FAMITEM IN (  
--   'AE','AS','AU','AX','BC','CG','DC','EX','IC','PI','PL','PM','QP','RC','RX','SW','TS','WA'  
--   )  
--  AND ISNULL(A.CAN_STOCK, 0) > 0   
--    AND D.flg_trabaja_por_lotes = 'N'  
--union all  
--  SELECT B.COD_ITEM AS CODIGO  
--  ,RTRIM(B.DES_ITEM) AS NOMBRE  
--  ,B.COD_UNIMED AS 'UNID_MED'  
--  ,LOTEPROV  = F.Cod_OrdProv   
--  ,STATUS_LOTE = CASE F.Flg_Status WHEN 'P' THEN 'Por Aprobar' ELSE 'Aprobado' END   
--  ,convert(varchar(20),CONVERT(DECIMAL(18, 2), ISNULL(A.CAN_STOCK, 0))) + ' ' +  B.COD_UNIMED AS STOCK  
--  ,Can_Reservado  
--  ,A.FEC_ULT_ENTRADA AS 'ULT_ENTRADA'  
--  ,A.FEC_ULT_SALIDA AS 'ULT_SALIDA'  
--  ,C.DES_PROVEEDOR AS PROVEEDOR  
--  ,PRE_ULTCOMP AS PRECIO_ULT_COMPRA  
--  ,COD_MONULTCOMP AS MONEDA  
--  ,A.CAN_STOCK * PRE_ULTCOMP AS IMPORTE  
--  ,B.COD_FAMITEM  
--  ,D.DES_FAMITEM  
--  ,B.COD_GRUITEM  
--  ,ISNULL(E.DES_FAMGRUITE, '') AS DES_FAMGRUITE  
--  ,A.Lote  
--  ,COD_BARRA = A.COD_ITEM + '_'+   RTRIM(F.Cod_OrdProv)  + '_'  + RTRIM(dbo.uf_strzero(A.Lote,4))   
-- FROM Lg_StocksItem_Lote A  
-- INNER JOIN LG_ITEM B ON A.COD_ITEM = B.COD_ITEM  
-- INNER JOIN LG_FAMITE D ON B.COD_FAMITEM = D.COD_FAMITEM  
-- LEFT OUTER JOIN LG_FAMGRUITE E ON B.COD_FAMITEM = E.COD_FAMITEM  
--  AND B.COD_GRUITEM = E.COD_GRUITEM  
-- INNER JOIN Lg_Item_Lotes F ON A.Cod_Item = F.Cod_Item AND A.Lote = F.Lote    
-- LEFT OUTER JOIN LG_PROVEEDOR C ON F.COD_PROVEEDOR = C.COD_PROVEEDOR  
-- WHERE A.COD_ALMACEN in ('51')  
--   AND B.COD_FAMITEM IN (  
--   'AE','AS','AU','AX','BC','CG','DC','EX','IC','PI','PL','PM','QP','RC','RX','SW','TS','WA'  
--   )  
--  AND ISNULL(A.CAN_STOCK, 0) > 0   
--    AND D.flg_trabaja_por_lotes = 'S'  
--) Z  
--order by Z.Descripcion asc  
  
END  