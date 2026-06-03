$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$sql = "sp_helptext 'dbo.LIQ_SP_ObtenerLiquidacionesConsolidadas'"

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $cmd = New-Object System.Data.SqlClient.SqlCommand($sql, $conn)
    $conn.Open()
    $reader = $cmd.ExecuteReader()
    while ($reader.Read()) {
        Write-Host $reader[0] -NoNewline
    }
}
catch {
    Write-Error $_.Exception.Message
}
finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
}
