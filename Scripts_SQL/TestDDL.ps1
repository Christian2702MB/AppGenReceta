$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$sql = "CREATE TABLE LIQ_NP_StockSnapshot (NP VARCHAR(20), CodInsumo VARCHAR(50), StockOperativoInicial DECIMAL(18,4), FechaCaptura DATETIME)"

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $cmd = New-Object System.Data.SqlClient.SqlCommand($sql, $conn)
    $conn.Open()
    $cmd.ExecuteNonQuery()
    Write-Host "Table created successfully."
    $sqlDrop = "DROP TABLE LIQ_NP_StockSnapshot"
    $cmdDrop = New-Object System.Data.SqlClient.SqlCommand($sqlDrop, $conn)
    $cmdDrop.ExecuteNonQuery()
}
catch {
    Write-Error $_.Exception.Message
}
finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
}
