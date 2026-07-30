$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT Id, NP, CodInsumo, StockOperativoInicial, Item FROM LIQ_NP_StockSnapshot WHERE NP = 'i8505-V7'"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) { Write-Host "$($reader[0]) | $($reader[1]) | $($reader[2]) | $($reader[3]) | $($reader[4])" }
