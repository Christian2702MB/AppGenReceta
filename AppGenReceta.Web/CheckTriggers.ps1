$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT name FROM sys.triggers WHERE parent_id = OBJECT_ID('LIQ_REQ_Recepciones')"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) { Write-Host $reader[0] }
