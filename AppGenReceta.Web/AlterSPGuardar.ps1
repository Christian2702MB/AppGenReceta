$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
ALTER PROCEDURE [dbo].[SP_GUARDAR_RECETA_COMPLETA]
	@Usuario VARCHAR(100) = '',
    @XmlData XML
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
    SET XACT_ABORT ON;
	BEGIN TRY
    BEGIN TRANSACTION    
        INSERT INTO AGR_Recetas (NP, OperarioUDP, Tecnica, FechaUDP, Cliente, Temporada, Estilo, EstiloPropio, Item, Combo, Prendas, Ubicacion, Arte, Observaciones, FechaRegistro, Usuario) 
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
			T.c.value('(Ubicacion)[1]', 'VARCHAR(50)'), 
			T.c.value('(Arte)[1]', 'VARCHAR(100)'), 
			T.c.value('(Observaciones)[1]', 'VARCHAR(800)'), 
            GETDATE(),
			@Usuario
        FROM @XmlData.nodes('/VisitaBE') AS T(c);
        
        DECLARE @IdVisita INT = SCOPE_IDENTITY();

        DECLARE @TabColores TABLE (IdGenerado INT, NombreColor VARCHAR(100));

        INSERT INTO AGR_Colores (IdRecetas, NombreColor, Combo)
        OUTPUT inserted.IdColor, inserted.NombreColor INTO @TabColores
        SELECT 
            @IdVisita,
            T.c.value('(Nombre)[1]', 'VARCHAR(100)'),
            T.c.value('(Combo)[1]', 'VARCHAR(50)')
        FROM @XmlData.nodes('/VisitaBE/Colores/ColorBE') AS T(c);

        -- Fix Order: Extract into temp table sequentially before joining
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
            I.c.value('(Codigo)[1]', 'VARCHAR(50)'),
            I.c.value('(Descripcion)[1]', 'VARCHAR(200)'),
            I.c.value('(Cantidad)[1]', 'DECIMAL(18,2)')
        FROM @XmlData.nodes('/VisitaBE/Colores/ColorBE') AS T(c)
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

        COMMIT TRANSACTION;
		SELECT 1 AS Resultado;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 
            ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
"@
$cmd.ExecuteNonQuery()
