ALTER PROCEDURE [dbo].[USP_VISITA_OBTENER_COMPLETO]
      @ID_VISITA INT
AS
BEGIN
    DECLARE @NP_RECETA VARCHAR(20) = (SELECT TOP 1 NP FROM AGR_Recetas WHERE IdRecetas = @ID_VISITA);
	
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
	ISNULL(V.Observaciones, '') as 'Observaciones',
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

    SELECT 
	C.IdColor as IdColor, 
	I.IdInsumo as IdInsumo, 
	C.NombreColor as NombreColor, 
	(C.Combo) as ComboColor, 
	I.codigoInsumo AS CodigoInsumo,
    I.Descripcion as InsumoDescripcion, 
	I.Cantidad as CantidadInsumo,
    ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = I.codigoInsumo AND TipoOperacion = 'Consumo Desarrollo' AND NombreColor = C.NombreColor AND (IdVisita = @ID_VISITA OR (IdVisita IS NULL AND NP = @NP_RECETA))), 0) AS ConsumoDesarrollo
    FROM AGR_Colores C
    INNER JOIN AGR_Insumos I ON C.IdColor = I.IdColor
    WHERE C.IdRecetas = @ID_VISITA;

	SELECT 
		NombreColor, 
		CodigoInsumo, 
		NombrePrueba, 
		ISNULL(GramosUDP, 0) AS GramosUDP,
		ISNULL(EsPrincipal, 0) AS EsPrincipal,
		IdPrueba 
	FROM dbo.AGR_Insumos_Prueba
	WHERE IdRecetas = @ID_VISITA;
END
GO
