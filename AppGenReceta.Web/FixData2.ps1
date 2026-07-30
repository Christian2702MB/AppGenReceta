$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
UPDATE LIQ_REQ_Recepciones 
SET CodOrdPro = 'i8505-V5-ES050437', Item = 'ES050437' 
WHERE CodOrdPro = 'i8505-V5' AND Item = 'V5'

UPDATE LIQ_REQ_Recepciones 
SET CodOrdPro = 'i8505-V3-ES050437', Item = 'ES050437' 
WHERE CodOrdPro = 'i8505-V3' AND Item = 'V3'
"@
$cmd.ExecuteNonQuery()
