$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
ALTER PROCEDURE [dbo].[USP_ACTUALIZAR_RECETA_SCTR_XML]
    @XML_DATA XML
AS
BEGIN
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
            DECLARE @IdVisita INT = @XML_DATA.value('(VisitaBE/Dato)[1]', 'INT');

            UPDATE AGR_Recetas SET 
				NP = @XML_DATA.value('(VisitaBE/NP)[1]', 'VARCHAR(100)'),
				OperarioUDP = @XML_DATA.value('(VisitaBE/Operario)[1]', 'VARCHAR(100)'),
                Tecnica = @XML_DATA.value('(VisitaBE/Tecnica)[1]', 'VARCHAR(100)'),		
                FechaUDP = @XML_DATA.value('(VisitaBE/FechaUDP)[1]', 'VARCHAR(100)'),				
				Prendas = @XML_DATA.value('(VisitaBE/PrendasReq)[1]', 'VARCHAR(100)'),
				Observaciones = @XML_DATA.value('(VisitaBE/Observaciones)[1]', 'VARCHAR(800)'),
				FechaUpdate = getdate()
            WHERE IdRecetas = @IdVisita;

			DELETE FROM AGR_Insumos WHERE IdColor in (select C.IdColor from AGR_Colores C WHERE C.IdRecetas = @IdVisita);
			DELETE FROM AGR_Colores WHERE IdRecetas = @IdVisita;

			DECLARE @TabColores TABLE (IdGenerado INT, NombreColor VARCHAR(100));

			INSERT INTO AGR_Colores (IdRecetas, NombreColor, Combo)
			OUTPUT inserted.IdColor, inserted.NombreColor INTO @TabColores
			SELECT 
				@IdVisita,
				T.c.value('(Nombre)[1]', 'VARCHAR(100)'),
				T.c.value('(Combo)[1]', 'VARCHAR(50)')
			FROM @XML_DATA.nodes('/VisitaBE/Colores/ColorBE') AS T(c);

			-- Fix Order
			DECLARE @TabInsumos TABLE (
				IdOrden INT IDENTITY(1,1),
				NombreColor VARCHAR(100),
				Codigo VARCHAR(50),
				Descripcion VARCHAR(200),
				Cantidad DECIMAL(18,2)
			);

			INSERT INTO @TabInsumos (NombreColor, Codigo, Descripcion, Cantidad)
			SELECT 
				T.c.value('(Nombre)[1]', 'VARCHAR(100)'),
				I.c.value('(CodigoInsumo)[1]', 'VARCHAR(50)'),
				I.c.value('(Descripcion)[1]', 'VARCHAR(200)'),
				I.c.value('(Cantidad)[1]', 'DECIMAL(18,2)')
			FROM @XML_DATA.nodes('/VisitaBE/Colores/ColorBE') AS T(c)
			CROSS APPLY T.c.nodes('Insumos/InsumoBE') AS I(c);

			INSERT INTO AGR_Insumos (IdColor, CodigoInsumo, Descripcion, Cantidad)
			SELECT 
				tc.IdGenerado,
				ti.Codigo,
				ti.Descripcion,
				ti.Cantidad
			FROM @TabInsumos ti
			INNER JOIN @TabColores tc ON tc.NombreColor = ti.NombreColor
			ORDER BY ti.IdOrden ASC;
			
			DELETE FROM AGR_Insumos_Prueba WHERE IdRecetas = @IdVisita;

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

            SELECT 1 AS Resultado;
        COMMIT TRANSACTION
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT ERROR_MESSAGE();
    END CATCH
END
"@
$cmd.ExecuteNonQuery()
