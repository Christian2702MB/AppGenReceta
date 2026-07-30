$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT TOP 10 NumRequerimiento, CodOrdPro, Item FROM LIQ_REQ_Recepciones ORDER BY FechaRecepcion DESC"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) { Write-Host "$($reader[0]) | $($reader[1]) | $($reader[2])" }
