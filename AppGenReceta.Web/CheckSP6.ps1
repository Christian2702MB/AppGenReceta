$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "sp_helptext USP_VISITA_OBTENER_COMPLETO"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) { Write-Host $reader[0] }
