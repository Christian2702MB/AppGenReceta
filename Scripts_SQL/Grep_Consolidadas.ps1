$content = Get-Content -Path "f:\CMendezB_Innovación\ANTIGRAVITY_2026\AppGenReceta\AppGenReceta.DA\Liquidacion_DA.cs"
$lineNum = 1
foreach ($line in $content) {
    if ($line -match "ObtenerLiquidacionesConsolidadas") {
        Write-Host "$($lineNum): $($line)"
    }
    $lineNum++
}
