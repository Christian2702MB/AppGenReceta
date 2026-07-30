$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "EXEC LIQ_SP_TomarSnapshotStockNP 'i8505-V7-ES050437'"
$cmd.ExecuteNonQuery()
