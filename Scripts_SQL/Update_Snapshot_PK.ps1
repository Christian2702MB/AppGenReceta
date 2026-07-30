$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()

$sqlContent = @"
ALTER TABLE LIQ_NP_StockSnapshot DROP CONSTRAINT PK_LIQ_NP_StockSnapshot;
ALTER TABLE LIQ_NP_StockSnapshot ADD CONSTRAINT PK_LIQ_NP_StockSnapshot PRIMARY KEY (NP, CodInsumo, Item);
"@
$cmd = $conn.CreateCommand()
$cmd.CommandText = $sqlContent
try {
    $cmd.ExecuteNonQuery()
    Write-Host "PK updated successfully!"
} catch {
    Write-Host "Error: $($_.Exception.Message)"
}
$conn.Close()
