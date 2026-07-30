$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
UPDATE LIQ_REQ_Recepciones 
SET Item = 'ES050437' 
WHERE CodOrdPro = 'i8505-V4-ES050437' AND Item = 'V4'

UPDATE LIQ_REQ_Recepciones 
SET Item = 'ES050437' 
WHERE CodOrdPro = 'i8505-V6-ES050437' AND Item = 'V6'

UPDATE LIQ_REQ_Recepciones 
SET Item = 'ES050437' 
WHERE CodOrdPro = 'i8505-V2-ES050437' AND Item = 'V2'
"@
$cmd.ExecuteNonQuery()
