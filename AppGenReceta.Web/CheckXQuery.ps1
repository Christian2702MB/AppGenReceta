$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
DECLARE @xml XML = '<VisitaBE><Colores><ColorBE><Nombre>Azul</Nombre><Insumos><InsumoBE><Codigo>PL000008</Codigo></InsumoBE><InsumoBE><Codigo>PL000005</Codigo></InsumoBE><InsumoBE><Codigo>PL000010</Codigo></InsumoBE></Insumos></ColorBE></Colores></VisitaBE>'

SELECT 
    I.c.value('(Codigo)[1]', 'VARCHAR(50)') AS Codigo,
    I.c.value('let $n := . return count(../*[. << $n])', 'int') AS Posicion
FROM @xml.nodes('/VisitaBE/Colores/ColorBE') AS T(c)
CROSS APPLY T.c.nodes('Insumos/InsumoBE') AS I(c)
ORDER BY Posicion
"@
$reader = $cmd.ExecuteReader()
while ($reader.Read()) { Write-Host "$($reader[0]) | $($reader[1])" }
