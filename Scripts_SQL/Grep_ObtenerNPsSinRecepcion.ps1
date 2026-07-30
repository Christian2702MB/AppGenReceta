$content = Get-Content -Path "f:\CMendezB_Innovación\ANTIGRAVITY_2026\AppGenReceta\AppGenReceta.Web\Controllers\LiquidacionController.cs"
$lineNum = 1
foreach ($line in $content) {
    if ($line -match "ObtenerNPsSinRecepcion") {
        Write-Host "$($lineNum): $($line)"
    }
    $lineNum++
}
