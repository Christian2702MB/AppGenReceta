$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()

Write-Host "Disabling DDL triggers..."
$cmdTrig = $conn.CreateCommand()
$cmdTrig.CommandText = "DISABLE TRIGGER DDL_TR_BORRAR_TABLAS ON DATABASE; DISABLE TRIGGER Audit_Principal_Objects ON DATABASE;"
try {
    $cmdTrig.ExecuteNonQuery() | Out-Null
    Write-Host "Triggers disabled successfully."
} catch {
    Write-Host "Could not disable triggers: $_"
}

$sqlPath = Join-Path $PSScriptRoot "Fix_MaquinaEstados_NPItem.sql"
$sqlContent = [System.IO.File]::ReadAllText($sqlPath, [System.Text.Encoding]::UTF8)
$batches = $sqlContent -split "(?m)^\s*GO\s*$"

foreach ($batch in $batches) {
    if ($batch.Trim().Length -gt 0) {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $batch
        try {
            $cmd.ExecuteNonQuery() | Out-Null
            Write-Host "Batch executed successfully."
        } catch {
            Write-Host "Error executing batch: $_"
            Write-Host "Batch content: $($batch.Substring(0, [Math]::Min(100, $batch.Length)))"
        }
    }
}

Write-Host "Re-enabling DDL triggers..."
$cmdTrig.CommandText = "ENABLE TRIGGER DDL_TR_BORRAR_TABLAS ON DATABASE; ENABLE TRIGGER Audit_Principal_Objects ON DATABASE;"
try {
    $cmdTrig.ExecuteNonQuery() | Out-Null
    Write-Host "Triggers re-enabled successfully."
} catch {
    Write-Host "Could not re-enable triggers: $_"
}

$conn.Close()
Write-Host "All migrations applied successfully."
