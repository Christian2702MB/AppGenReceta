$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "sp_help LIQ_NP_StockSnapshot"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) { Write-Host "$($reader[0])" }
