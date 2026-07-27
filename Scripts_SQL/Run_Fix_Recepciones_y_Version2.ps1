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

$files = @(
    (Join-Path $PSScriptRoot "Refactor_Mantenimiento_Recepciones.sql"),
    (Join-Path (Split-Path $PSScriptRoot) "Scripts\LIQ_SP_GenerarSiguienteVersionNP.sql")
)

foreach ($filePath in $files) {
    Write-Host "Executing file: $filePath"
    $sqlContent = [System.IO.File]::ReadAllText($filePath, [System.Text.Encoding]::UTF8)
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
            }
        }
    }
}

$conn.Close()
Write-Host "All scripts executed completed."
