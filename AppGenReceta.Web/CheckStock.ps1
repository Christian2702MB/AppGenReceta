$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT CodInsumo, StockActual FROM LIQ_STK_StockInsumos WHERE CodInsumo = 'AE000058'"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) { Write-Host "$($reader[0]) | $($reader[1])" }
