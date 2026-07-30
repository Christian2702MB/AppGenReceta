$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()

$sqlContent = @"
SELECT TOP 5 NP, NombreColor, Gramos, FechaRegistro FROM LIQ_MER_MermasColor ORDER BY FechaRegistro DESC
"@
$cmd = $conn.CreateCommand()
$cmd.CommandText = $sqlContent
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host "NP: $($reader['NP']) Color: $($reader['NombreColor']) Gramos: $($reader['Gramos'])"
}
$reader.Close()
$conn.Close()
