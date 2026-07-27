$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()

$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT t.name, m.definition FROM sys.triggers t JOIN sys.sql_modules m ON t.object_id = m.object_id WHERE m.definition LIKE '%You may not modify%'"
$r = $cmd.ExecuteReader()
while($r.Read()) {
    Write-Host "Trigger Name: $($r['name'])"
    Write-Host "Definition: $($r['definition'])"
}
$r.Close()
$conn.Close()
