$connectionString = "server=HIALPESA103; database=HIALPESA; User Id=soporte; Pwd=soporte;"
$sql = "
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LIQ_NP_StockSnapshot]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[LIQ_NP_StockSnapshot](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [NP] [varchar](20) NOT NULL,
        [CodInsumo] [varchar](50) NOT NULL,
        [StockOperativoInicial] [decimal](18, 4) NULL,
        [FechaCaptura] [datetime] NULL,
        CONSTRAINT [PK_LIQ_NP_StockSnapshot] PRIMARY KEY CLUSTERED 
        (
            [NP] ASC,
            [CodInsumo] ASC
        )
    ) ON [PRIMARY]
END
"

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $cmd = New-Object System.Data.SqlClient.SqlCommand($sql, $conn)
    $conn.Open()
    $cmd.ExecuteNonQuery()
    Write-Host "Table LIQ_NP_StockSnapshot created successfully."
}
catch {
    Write-Error $_.Exception.Message
}
finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
}
