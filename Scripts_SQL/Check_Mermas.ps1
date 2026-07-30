$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()

$sqlContent = @"
SELECT TOP 1 * FROM LIQ_MER_MermasColor WHERE NP = 'i8505'
"@
$cmd = $conn.CreateCommand()
$cmd.CommandText = $sqlContent
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host "NP: $($reader['NP']) Color: $($reader['NombreColor']) Gramos: $($reader['Gramos'])"
}
$reader.Close()
$conn.Close()
