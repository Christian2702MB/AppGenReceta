$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()

$sqlContent = @"
sp_help 'LIQ_NP_StockSnapshot'
"@
$cmd = $conn.CreateCommand()
$cmd.CommandText = $sqlContent
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host "$($reader['Column_name'])"
}
$reader.Close()
$conn.Close()
