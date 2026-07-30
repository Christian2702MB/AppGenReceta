Add-Type -Path "f:\CMendezB_Innovación\ANTIGRAVITY_2026\AppGenReceta\AppGenReceta.DA\bin\Debug\AppGenReceta.DA.dll"
$da = New-Object AppGenReceta.DA.Liquidacion_DA
$items = $da.ObtenerItemsActivosNP("i8505-V4-ES050437")
Write-Host "Items:"
foreach($i in $items) { Write-Host $i }
$versiones = $da.ObtenerVersionesNP("i8505-V4-ES050437")
Write-Host "Versiones:"
foreach($v in $versiones) { Write-Host $v }
