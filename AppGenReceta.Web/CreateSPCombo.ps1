$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
CREATE PROCEDURE LIQ_SP_ObtenerLiquidacionesActivasCombo
AS
BEGIN
    SET NOCOUNT ON;

    SELECT NP, Item, Cliente, Estilo 
    FROM LIQ_Formulas
    WHERE Estado != 'Cerrada' AND ISNULL(Eliminado, 0) = 0
    ORDER BY NP DESC;
END
"@
$cmd.ExecuteNonQuery()
