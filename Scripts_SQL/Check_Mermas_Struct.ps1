$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()

$sqlContent = @"
EXEC sp_help 'LIQ_MER_MermasColor'
"@
$cmd = $conn.CreateCommand()
$cmd.CommandText = $sqlContent
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host "$($reader['Column_name']) - $($reader['Type']) - $($reader['Length'])"
}
$reader.Close()
$conn.Close()
