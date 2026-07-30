$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()

$sqlContent = @"
EXEC sp_helptext 'LIQ_SP_CrearRecepcionBlanco'
"@
$cmd = $conn.CreateCommand()
$cmd.CommandText = $sqlContent
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host "$($reader['Text'])" -NoNewline
}
$reader.Close()
$conn.Close()
