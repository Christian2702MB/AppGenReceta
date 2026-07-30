$content = Get-Content -Path "f:\CMendezB_Innovación\ANTIGRAVITY_2026\AppGenReceta\AppGenReceta.DA\Liquidacion_DA.cs"
$lineNum = 1
$found = $false
foreach ($line in $content) {
    if ($line -match "ObtenerLiquidacionesConsolidadas") {
        $found = $true
    }
    if ($found -and ($line -match "return lista;")) {
        Write-Host "$($lineNum): $($line)"
        break
    }
    if ($found) {
        Write-Host "$($lineNum): $($line)"
    }
    $lineNum++
}
