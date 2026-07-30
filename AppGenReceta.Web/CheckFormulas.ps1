$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT NP, Item FROM LIQ_Formulas WHERE NP LIKE 'i8505%'"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) { Write-Host "$($reader[0]) | $($reader[1])" }
