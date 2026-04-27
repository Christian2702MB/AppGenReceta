CREATE PROCEDURE dbo.GRI_CALCULAR_INSUMOS
@NP AS VARCHAR(50) = '',
@ESTILO AS VARCHAR(50) = '',
@UBICACION AS VARCHAR(50) = ''
AS  
SET NOCOUNT ON  
BEGIN  
select 
I.CodigoInsumo,
I.Descripcion,
P.NombrePrueba,
P.GramosUDP
--R.*,C.*,I.*
from 
[dbo].[AGR_Recetas] R 
INNER JOIN [dbo].[AGR_Colores] C 
ON R.IdRecetas = C.IdRecetas
INNER JOIN [dbo].[AGR_Insumos] I
ON C.IdColor = I.IdColor
INNER JOIN dbo.AGR_Insumos_Prueba P
ON C.NombreColor = P.NombreColor
and I.CodigoInsumo = P.CodigoInsumo
and P.EsPrincipal = 1
where R.EstiloPropio = @ESTILO
--and R.Ubicacion = 'ETQ'
--and C.NombreColor = 'Rosado'
END  

