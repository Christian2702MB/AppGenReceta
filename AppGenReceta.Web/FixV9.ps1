$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
UPDATE LIQ_REQ_Recepciones 
SET CodOrdPro = 'i8505-V9-ES050437', Item = 'ES050437' 
WHERE NumRequerimiento = 9000028;

UPDATE LIQ_NP_StockSnapshot
SET NP = 'i8505-V9'
WHERE NP = 'i8505-V9-ES050437' -- If it was mistakenly saved as such. Wait, the snapshot SP took 'i8505-V9-ES050437' previously, so the old snapshot SP might have inserted 'i8505-V9-ES050437' as NP if there was no match? No, it inserts F.NP, so it's fine.
"@
$cmd.ExecuteNonQuery()
