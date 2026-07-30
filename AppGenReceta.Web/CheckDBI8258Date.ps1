$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT FechaCreacion FROM LIQ_Formulas WHERE NP LIKE '%I8258%'"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) { Write-Host "$($reader[0])" }
