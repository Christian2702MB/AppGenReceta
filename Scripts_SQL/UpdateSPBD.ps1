$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;Min Pool Size=5;Max Pool Size=50;Connection Timeout=30;"
$sqlFile = "SP_BD.sql"

# Read the file and split by GO since SqlConnection cannot execute GO batches natively
$scriptContent = Get-Content -Path $sqlFile -Raw
$batches = $scriptContent -split "(?m)^\s*GO\s*$"

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    
    foreach ($batch in $batches) {
        if (![string]::IsNullOrWhiteSpace($batch)) {
            $cmd = $conn.CreateCommand()
            $cmd.CommandText = $batch
            $cmd.ExecuteNonQuery() | Out-Null
        }
    }
    Write-Host "Success executing SP_BD.sql"
}
catch {
    Write-Error $_.Exception.Message
}
finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
}
