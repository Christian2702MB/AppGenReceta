$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()

$cmd = $conn.CreateCommand()
$cmd.CommandText = "EXEC sp_helptext 'LIQ_SP_RegistrarOperacion'"

$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Output $reader[0]
}

$conn.Close()
