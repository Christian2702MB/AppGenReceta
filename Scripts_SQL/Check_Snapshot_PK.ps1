$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()

$sqlContent = @"
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + QUOTENAME(CONSTRAINT_NAME)), 'IsPrimaryKey') = 1
AND TABLE_NAME = 'LIQ_NP_StockSnapshot';
"@
$cmd = $conn.CreateCommand()
$cmd.CommandText = $sqlContent
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host "PK Column: $($reader['COLUMN_NAME'])"
}
$reader.Close()
$conn.Close()
