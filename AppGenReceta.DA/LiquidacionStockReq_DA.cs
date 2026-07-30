using AppGenReceta.BE;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace AppGenReceta.DA
{
    public class LiquidacionStockReq_DA
    {
        private string ConnectionString = ConfigurationManager.ConnectionStrings["AppGenReceta_SQL"].ConnectionString;

        public List<LIQ_StockInsumoBE> ListarStockActual()
        {
            var lista = new List<LIQ_StockInsumoBE>();
            using (SqlConnection cn = new SqlConnection(ConnectionString))
            {
                string sql = @"
                    SET ARITHABORT ON;
                    WITH CTE_Insumos AS (
                        SELECT 
                            1 AS OrdenFila,
                            I.CodInsumo,
                            I.Descripcion,
                            I.UnidadMedida,
                            CONVERT(VARCHAR(10), I.FechaUltimaActualizacion, 103) + ' ' + CONVERT(VARCHAR(8), I.FechaUltimaActualizacion, 108) AS FechaModificacion,
                            ISNULL(I.StockActual, 0) AS StockInicialOriginal,
                            ISNULL(R.StockRecibido, 0) AS StockRecibidoOriginal,
                            ISNULL(O.ConsumosTotales, 0) / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END AS ConsumosTotales,
                            ISNULL(O.ConsumosProduccion, 0) / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END AS ConsumosProduccion,
                            ISNULL(O.ConsumosDesarrollo, 0) / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END AS ConsumosDesarrollo,
                            ISNULL(O.AjustesTotales, 0) / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END AS AjustesTotales,
                            ISNULL(O.DevolucionesCentral, 0) / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END AS DevolucionesCentral,
                            ISNULL(O.DevolucionesOperativo, 0) / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END AS DevolucionesOperativo,
                            ISNULL(O.AjustesManualesIngreso, 0) / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END AS AjustesManualesIngreso,
                            ISNULL(O.AjustesManualesSalida, 0) / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END AS AjustesManualesSalida,
                            
                            -- Cálculos Netos Internos
                            (ISNULL(I.StockActual, 0) + (ISNULL(O.DevolucionesOperativo, 0) / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END)) AS InicialNeto,
                            (ISNULL(R.StockRecibido, 0) - (ISNULL(O.DevolucionesOperativo, 0) / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END)) AS RecibidoNeto,

                            -- Stock Actual (Se anula DevolucionOperativo al usar los netos)
                            ((ISNULL(I.StockActual, 0) + (ISNULL(O.DevolucionesOperativo, 0) / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END)) + 
                            (ISNULL(R.StockRecibido, 0) - (ISNULL(O.DevolucionesOperativo, 0) / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END)) + 
                            (ISNULL(O.AjustesManualesIngreso, 0) / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END) - 
                            (ISNULL(O.ConsumosTotales, 0) / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END) - 
                            (ISNULL(O.AjustesTotales, 0) / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END) - 
                            (ISNULL(O.AjustesManualesSalida, 0) / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END) - 
                            (ISNULL(O.DevolucionesCentral, 0) / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END)) AS StockActual,
                            
                            -- Stock Pendiente de Liquidar
                            ISNULL(Pendientes.SaldoPendiente, 0) / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END AS StockPorLiquidar
                        FROM LIQ_STK_StockInsumos I
                        LEFT JOIN (
                            SELECT CodInsumo, SUM(CantidadRecibida) AS StockRecibido
                            FROM LIQ_REQ_RecepcionesDetalle
                            GROUP BY CodInsumo
                        ) R ON I.CodInsumo = R.CodInsumo
                        LEFT JOIN (
                            SELECT CodInsumo,
                                   SUM(CASE WHEN TipoOperacion IN ('Consumo', 'Consumo Desarrollo') AND FuenteConsumo IN ('Stock Inicial', 'Stock Solicitado', 'Solicitud Realizada') THEN Cantidad ELSE 0 END) AS ConsumosTotales,
                                   SUM(CASE WHEN TipoOperacion = 'Consumo' AND FuenteConsumo IN ('Stock Inicial', 'Stock Solicitado', 'Solicitud Realizada') THEN Cantidad ELSE 0 END) AS ConsumosProduccion,
                                   SUM(CASE WHEN TipoOperacion = 'Consumo Desarrollo' AND FuenteConsumo IN ('Stock Inicial', 'Stock Solicitado', 'Solicitud Realizada') THEN Cantidad ELSE 0 END) AS ConsumosDesarrollo,
                                   SUM(CASE WHEN TipoOperacion = 'Ajuste' AND FuenteConsumo IN ('Stock Inicial', 'Stock Solicitado', 'Solicitud Realizada') THEN Cantidad ELSE 0 END) AS AjustesTotales,
                                   SUM(CASE WHEN TipoOperacion = 'Devolucion Central' OR (TipoOperacion = 'Devolucion' AND Motivo LIKE '%Central%') THEN Cantidad ELSE 0 END) AS DevolucionesCentral,
                                   SUM(CASE WHEN TipoOperacion = 'Devolucion Operativo' OR (TipoOperacion = 'Devolucion' AND Motivo LIKE '%Operativo%') THEN Cantidad ELSE 0 END) AS DevolucionesOperativo,
                                   SUM(CASE WHEN TipoOperacion = 'Ajuste Directo' AND FuenteConsumo = 'Ingreso' THEN Cantidad ELSE 0 END) AS AjustesManualesIngreso,
                                   SUM(CASE WHEN TipoOperacion = 'Ajuste Directo' AND FuenteConsumo = 'Salida' THEN Cantidad ELSE 0 END) AS AjustesManualesSalida
                            FROM LIQ_OperacionesDetalle
                            GROUP BY CodInsumo
                        ) O ON I.CodInsumo = O.CodInsumo
                        LEFT JOIN (
                            SELECT 
                                Base.CodInsumo,
                                SUM(Base.Recibido - Base.Consumido - Base.Ajustado - Base.Devuelto) AS SaldoPendiente
                            FROM (
                                SELECT 
                                    Dist.CodInsumo,
                                    Dist.NP,
                                    ISNULL((SELECT SUM(D.CantidadRecibida * 1000.0) 
                                            FROM LIQ_REQ_Recepciones R 
                                            INNER JOIN LIQ_REQ_RecepcionesDetalle D ON R.NumRequerimiento = D.NumRequerimiento 
                                            WHERE R.CodOrdPro = Dist.NP AND D.CodInsumo = Dist.CodInsumo), 0) AS Recibido,
                                    ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = Dist.NP AND CodInsumo = Dist.CodInsumo AND TipoOperacion IN ('Consumo', 'Consumo Desarrollo') AND FuenteConsumo IN ('Stock Solicitado', 'Solicitud Realizada', 'Stock Recibido')), 0) AS Consumido,
                                    ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = Dist.NP AND CodInsumo = Dist.CodInsumo AND TipoOperacion = 'Ajuste' AND FuenteConsumo IN ('Stock Solicitado', 'Solicitud Realizada', 'Stock Recibido')), 0) AS Ajustado,
                                    ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP = Dist.NP AND CodInsumo = Dist.CodInsumo AND TipoOperacion = 'Devolucion'), 0) AS Devuelto
                                FROM (
                                    SELECT DISTINCT
                                        I.CodigoInsumo AS CodInsumo,
                                        F.NP
                                    FROM LIQ_Formulas F
                                    INNER JOIN LIQ_FormulaColores C ON F.IdFormula = C.IdFormula
                                    INNER JOIN LIQ_FormulaInsumos I ON C.IdFormulaColor = I.IdFormulaColor
                                    WHERE F.Estado != 'Cerrada'
                                ) Dist
                            ) Base
                            GROUP BY Base.CodInsumo
                        ) Pendientes ON I.CodInsumo = Pendientes.CodInsumo
                    ),
                    CTE_Mermas AS (
                        SELECT 
                            2 AS OrdenFila,
                            M.CodigoMerma AS CodInsumo,
                            M.NombreColor + ' (NP: ' + M.NP + ')' AS Descripcion,
                            'KG' AS UnidadMedida,
                            CONVERT(VARCHAR(10), M.FechaRegistro, 103) + ' ' + CONVERT(VARCHAR(8), M.FechaRegistro, 108) AS FechaModificacion,
                            (M.Gramos / 1000.0) AS StockInicialOriginal,
                            0 AS StockRecibidoOriginal,
                            (ISNULL(Op.ConsumosTotales, 0) / 1000.0) AS ConsumosTotales,
                            (ISNULL(Op.ConsumosTotales, 0) / 1000.0) AS ConsumosProduccion,
                            0 AS ConsumosDesarrollo,
                            (ISNULL(Op.AjustesTotales, 0) / 1000.0) AS AjustesTotales,
                            0 AS DevolucionesCentral,
                            0 AS DevolucionesOperativo,
                            0 AS AjustesManualesIngreso,
                            0 AS AjustesManualesSalida,
                            
                            (M.Gramos / 1000.0) AS InicialNeto,
                            0 AS RecibidoNeto,
                            
                            CAST((M.Gramos - ISNULL(Op.ConsumosTotales, 0) - ISNULL(Op.AjustesTotales, 0)) / 1000.0 AS DECIMAL(18,4)) AS StockActual,
                            0 AS StockPorLiquidar
                        FROM LIQ_MER_MermasColor M
                        LEFT JOIN (
                            SELECT MermaReutilizada, 
                                   SUM(CASE WHEN TipoOperacion = 'Consumo' THEN Cantidad ELSE 0 END) AS ConsumosTotales,
                                   SUM(CASE WHEN TipoOperacion = 'Ajuste' THEN Cantidad ELSE 0 END) AS AjustesTotales
                            FROM LIQ_OperacionesDetalle
                            WHERE (TipoOperacion = 'Consumo' OR TipoOperacion = 'Ajuste') AND FuenteConsumo = 'Stock Merma'
                            GROUP BY MermaReutilizada
                        ) Op ON M.CodigoMerma = Op.MermaReutilizada
                        WHERE M.FechaVencimiento IS NOT NULL 
                          AND M.Gramos > 0
                          AND CAST(M.FechaVencimiento AS DATE) >= CAST(GETDATE() AS DATE)
                          AND CAST(M.Gramos - ISNULL(Op.ConsumosTotales, 0) - ISNULL(Op.AjustesTotales, 0) AS DECIMAL(18,2)) > 0
                    )
                    SELECT * FROM CTE_Insumos
                    UNION ALL
                    SELECT * FROM CTE_Mermas
                    ORDER BY OrdenFila ASC, Descripcion ASC;
                ";

                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.CommandType = CommandType.Text;
                    cn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new LIQ_StockInsumoBE
                            {
                                OrdenFila = Convert.ToInt32(dr["OrdenFila"]),
                                CodInsumo = dr["CodInsumo"].ToString(),
                                Descripcion = dr["Descripcion"].ToString(),
                                UnidadMedida = dr["UnidadMedida"].ToString(),
                                StockInicialOriginal = Convert.ToDecimal(dr["StockInicialOriginal"]),
                                StockRecibidoOriginal = Convert.ToDecimal(dr["StockRecibidoOriginal"]),
                                InicialNeto = Convert.ToDecimal(dr["InicialNeto"]),
                                RecibidoNeto = Convert.ToDecimal(dr["RecibidoNeto"]),
                                StockInicial = Convert.ToDecimal(dr["StockInicialOriginal"]), // Mantenemos valor original para compatibilidad
                                StockRecibido = Convert.ToDecimal(dr["StockRecibidoOriginal"]), // Mantenemos valor original para compatibilidad
                                ConsumosTotales = Convert.ToDecimal(dr["ConsumosTotales"]),
                                ConsumosProduccion = Convert.ToDecimal(dr["ConsumosProduccion"]),
                                ConsumosDesarrollo = Convert.ToDecimal(dr["ConsumosDesarrollo"]),
                                AjustesTotales = Convert.ToDecimal(dr["AjustesTotales"]),
                                DevolucionesCentral = Convert.ToDecimal(dr["DevolucionesCentral"]),
                                DevolucionesOperativo = Convert.ToDecimal(dr["DevolucionesOperativo"]),
                                AjustesManualesIngreso = Convert.ToDecimal(dr["AjustesManualesIngreso"]),
                                AjustesManualesSalida = Convert.ToDecimal(dr["AjustesManualesSalida"]),
                                StockActual = Convert.ToDecimal(dr["StockActual"]),
                                
                                StockPorLiquidar = Convert.ToDecimal(dr["StockPorLiquidar"]),
                                StockDisponible = Convert.ToDecimal(dr["StockActual"]) - Convert.ToDecimal(dr["StockPorLiquidar"]),
                                
                                FechaModificacion = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public List<LIQ_REQ_RecepcionBE> ListarRecepcionesHistoricas(string fechaDesde, string fechaHasta)
        {
            var lista = new List<LIQ_REQ_RecepcionBE>();
            using (SqlConnection cn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("LIQ_SP_ListarRecepcionesHistoricas", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    if (!string.IsNullOrEmpty(fechaDesde)) cmd.Parameters.AddWithValue("@FechaDesde", fechaDesde);
                    if (!string.IsNullOrEmpty(fechaHasta)) cmd.Parameters.AddWithValue("@FechaHasta", fechaHasta);

                    cn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while(dr.Read())
                        {
                            string itemVal = "";
                            try
                            {
                                itemVal = dr["Item"] != DBNull.Value ? dr["Item"].ToString() : "";
                            }
                            catch { }

                            lista.Add(new LIQ_REQ_RecepcionBE
                            {
                                NumRequerimiento = Convert.ToInt32(dr["NumRequerimiento"]),
                                CodOrdPro = dr["CodOrdPro"].ToString(),
                                Item = itemVal,
                                Motivo = dr["Motivo"].ToString(),
                                Estado = dr["Estado"].ToString(),
                                FechaRecepcion = Convert.ToDateTime(dr["FechaRecepcion"]).ToString("dd/MM/yyyy HH:mm"),
                                UsuarioRecepcion = dr["UsuarioRecepcion"].ToString(),
                                Observaciones = dr["Observaciones"].ToString()
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public List<LIQ_RequerimientoBE> ListarRequerimientosAJAX(string opcion, string fechaDesde, string fechaHasta, string np = "", int? numReqBusqueda = null)
        {
            var lista = new List<LIQ_RequerimientoBE>();
            using (SqlConnection cn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("LIQ_REQ_SUSP_REQ_ADICIONALES_ESTAMPADO", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@opcion", string.IsNullOrEmpty(opcion) ? "1" : opcion);
                    cmd.Parameters.AddWithValue("@fecha", string.IsNullOrEmpty(fechaDesde) ? "" : fechaDesde);
                    cmd.Parameters.AddWithValue("@fecha2", string.IsNullOrEmpty(fechaHasta) ? "" : fechaHasta);
                    cmd.Parameters.AddWithValue("@cod_ordpro", string.IsNullOrEmpty(np) ? "" : np);
                    cmd.Parameters.AddWithValue("@Num_Requerimiento", numReqBusqueda.HasValue ? numReqBusqueda.Value : 0);

                    cn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        var idsProcesados = new HashSet<int>();
                        while (dr.Read())
                        {
                            int numReq = dr["Num_Requerimiento"] != DBNull.Value ? Convert.ToInt32(dr["Num_Requerimiento"]) : 0;
                            if (numReq > 0 && idsProcesados.Contains(numReq)) continue;
                            if (numReq > 0) idsProcesados.Add(numReq);

                            string codOrdPro = dr["Partida"] != DBNull.Value ? dr["Partida"].ToString().Trim() : "";
                            string motivo = dr["Motivo"] != DBNull.Value ? dr["Motivo"].ToString().Trim() : "";

                            if (!string.IsNullOrEmpty(codOrdPro) && motivo == "30")
                            {
                                lista.Add(new LIQ_RequerimientoBE
                                {
                                    NumRequerimiento = numReq,
                                    CodOrdPro = codOrdPro,
                                    Motivo = motivo,
                                    FecCreacion = dr["Fec_Creacion"] != DBNull.Value ? dr["Fec_Creacion"].ToString() : "",
                                    Partida = dr["Partida"] != DBNull.Value ? dr["Partida"].ToString() : "",
                                    Cliente = dr["Cod_Cliente"] != DBNull.Value ? dr["Cod_Cliente"].ToString() : "",
                                    Observaciones = dr["Observaciones"] != DBNull.Value ? dr["Observaciones"].ToString() : ""
                                });
                            }
                        }
                    }
                }
            }

            var pendientes = new List<LIQ_RequerimientoBE>();
            if (lista.Count > 0)
            {
                var nums = new List<int>();
                foreach(var req in lista) nums.Add(req.NumRequerimiento);
                var strNums = string.Join(",", nums);

                using (SqlConnection cn = new SqlConnection(ConnectionString))
                {
                    string sql = $"SELECT NumRequerimiento FROM LIQ_REQ_Recepciones WHERE NumRequerimiento IN ({strNums})";
                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        cn.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            var recibidos = new HashSet<int>();
                            while(dr.Read())
                            {
                                recibidos.Add(Convert.ToInt32(dr["NumRequerimiento"]));
                            }
                            
                            foreach(var req in lista)
                            {
                                if (!recibidos.Contains(req.NumRequerimiento))
                                {
                                    pendientes.Add(req);
                                }
                            }
                        }
                    }
                }
            }
            return pendientes;
        }

        public List<LIQ_RequerimientoBE> ListarRequerimientosPendientes()
        {
            var lista = new List<LIQ_RequerimientoBE>();
            using (SqlConnection cn = new SqlConnection(ConnectionString))
            {
                // Usamos el SP de ERP existente y filtramos aquí, ya que no podemos alterar el SP del ERP
                // Pero traemos todos los req de tintoreria (opción 1 trae cabeceras)
                using (SqlCommand cmd = new SqlCommand("ti_sm_muestra_req_adicionales_tinto", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@opcion", "1");
                    cmd.Parameters.AddWithValue("@fecha", "");
                    cmd.Parameters.AddWithValue("@fecha2", "");
                    cmd.Parameters.AddWithValue("@cod_ordtra", "");
                    cmd.Parameters.AddWithValue("@cod_maquina_tinto", "");
                    cmd.Parameters.AddWithValue("@Num_Requerimiento", 0);

                    cn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            string codOrdPro = dr["Partida"] != DBNull.Value ? dr["Partida"].ToString().Trim() : "";
                            string motivo = dr["Motivo"] != DBNull.Value ? dr["Motivo"].ToString().Trim() : "";
                            int numReq = dr["Num_Requerimiento"] != DBNull.Value ? Convert.ToInt32(dr["Num_Requerimiento"]) : 0;

                            // Regla de Negocio: Mostrar solo las que tengan "Cod.OrdPro" y "Motivo = 30"
                            if (!string.IsNullOrEmpty(codOrdPro) && motivo == "30")
                            {
                                lista.Add(new LIQ_RequerimientoBE
                                {
                                    NumRequerimiento = numReq,
                                    CodOrdPro = codOrdPro,
                                    Motivo = motivo,
                                    FecCreacion = dr["Fec_Creacion"] != DBNull.Value ? dr["Fec_Creacion"].ToString() : "",
                                    Partida = dr["Partida"] != DBNull.Value ? dr["Partida"].ToString() : "",
                                    Cliente = dr["Cod_Cliente"] != DBNull.Value ? dr["Cod_Cliente"].ToString() : "",
                                    Observaciones = dr["Observaciones"] != DBNull.Value ? dr["Observaciones"].ToString() : ""
                                });
                            }
                        }
                    }
                }
            }
            
            // Ahora debemos quitar los que ya fueron confirmados (ya existen en LIQ_REQ_Recepciones)
            var pendientes = new List<LIQ_RequerimientoBE>();
            if (lista.Count > 0)
            {
                var nums = new List<int>();
                foreach(var req in lista) nums.Add(req.NumRequerimiento);
                var strNums = string.Join(",", nums);

                using (SqlConnection cn = new SqlConnection(ConnectionString))
                {
                    // Consultamos cuales ya estan recibidos
                    string sql = $"SELECT NumRequerimiento FROM LIQ_REQ_Recepciones WHERE NumRequerimiento IN ({strNums})";
                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        cn.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            var recibidos = new HashSet<int>();
                            while(dr.Read())
                            {
                                recibidos.Add(Convert.ToInt32(dr["NumRequerimiento"]));
                            }
                            
                            foreach(var req in lista)
                            {
                                if (!recibidos.Contains(req.NumRequerimiento))
                                {
                                    pendientes.Add(req);
                                }
                            }
                        }
                    }
                }
            }
            return pendientes;
        }

        public string ConfirmarRecepcion(int numRequerimiento, string codOrdPro, string motivo, string usuarioRecepcion, string xmlDetalle, string item = null)
        {
            string mensaje = "";
            using (SqlConnection cn = new SqlConnection(ConnectionString))
            {
                string sql = "SET ARITHABORT ON; EXEC LIQ_REQ_SP_ConfirmarRecepcion @NumRequerimiento, @CodOrdPro, @Motivo, @UsuarioRecepcion, @XMLDetalle";
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@NumRequerimiento", numRequerimiento);
                    cmd.Parameters.AddWithValue("@CodOrdPro", codOrdPro);
                    cmd.Parameters.AddWithValue("@Motivo", motivo);
                    cmd.Parameters.AddWithValue("@UsuarioRecepcion", usuarioRecepcion);
                    cmd.Parameters.AddWithValue("@XMLDetalle", xmlDetalle);

                    cn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            int res = Convert.ToInt32(dr["Resultado"]);
                            mensaje = dr["Mensaje"].ToString();
                            if (res == 0) throw new Exception(mensaje);
                        }
                    }

                    // -------------------------------------------------------------
                    // ACTUALIZAR COLUMNA ITEM EN LIQ_REQ_RECEPCIONES SI FUE INGRESADO
                    // -------------------------------------------------------------
                    if (!string.IsNullOrEmpty(item))
                    {
                        try
                        {
                            string sqlUpdateItem = @"
                                IF COL_LENGTH('LIQ_REQ_Recepciones', 'Item') IS NOT NULL
                                BEGIN
                                    UPDATE LIQ_REQ_Recepciones SET Item = @Item WHERE NumRequerimiento = @NumRequerimiento;
                                END
                            ";
                            using (SqlCommand cmdItem = new SqlCommand(sqlUpdateItem, cn))
                            {
                                cmdItem.Parameters.AddWithValue("@Item", item);
                                cmdItem.Parameters.AddWithValue("@NumRequerimiento", numRequerimiento);
                                cmdItem.ExecuteNonQuery();
                            }
                        }
                        catch { }
                    }

                    // -------------------------------------------------------------
                    // REQUERIMIENTO: CAPTURAR FOTO HISTÓRICA DEL STOCK INICIAL
                    // Se ejecuta solo si la recepción fue exitosa.
                    // -------------------------------------------------------------
                    string finalNP = codOrdPro;
                    if (!string.IsNullOrEmpty(item) && !finalNP.Contains(item))
                    {
                        finalNP = finalNP + "-" + item;
                    }

                    string sqlSnapshot = "EXEC LIQ_SP_TomarSnapshotStockNP @NP;";
                    using (SqlCommand cmdSnap = new SqlCommand(sqlSnapshot, cn))
                    {
                        cmdSnap.CommandType = CommandType.Text;
                        cmdSnap.Parameters.AddWithValue("@NP", finalNP);
                        cmdSnap.ExecuteNonQuery();
                    }
                    // -------------------------------------------------------------
                }
            }
            return mensaje;
        }

        public bool ExisteFormulaParaNP(string codOrdPro)
        {
            using (SqlConnection cn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(1) FROM LIQ_Formulas WHERE NP = @NP AND Estado <> 'Eliminada'", cn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@NP", codOrdPro);
                    cn.Open();
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        public string RegistrarCargaInicial(string codInsumo, string descripcion, string unidadMedida, decimal pesoGramos, string usuario, string npDirigida = null)
        {
            string mensaje = "";
            using (SqlConnection cn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("LIQ_STK_SP_RegistrarCargaInicial", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CodInsumo", codInsumo);
                    cmd.Parameters.AddWithValue("@Descripcion", descripcion);
                    cmd.Parameters.AddWithValue("@UnidadMedida", unidadMedida);
                    cmd.Parameters.AddWithValue("@PesoGramos", pesoGramos);
                    cmd.Parameters.AddWithValue("@Usuario", usuario);
                    if (!string.IsNullOrEmpty(npDirigida))
                    {
                        cmd.Parameters.AddWithValue("@NPDirigida", npDirigida);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@NPDirigida", DBNull.Value);
                    }

                    cn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            int res = Convert.ToInt32(dr["Resultado"]);
                            mensaje = dr["Mensaje"].ToString();
                            if (res == 0) throw new Exception(mensaje);
                        }
                    }
                }
            }
            return mensaje;
        }
        public List<LIQ_REQ_RecepcionDetalleBE> ObtenerDetalleRecepcion(int numRequerimiento)
        {
            var lista = new List<LIQ_REQ_RecepcionDetalleBE>();
            using (SqlConnection cn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_ObtenerDetalleRecepcion", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@NumRequerimiento", numRequerimiento);

                    cn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            string codIns = dr["CodInsumo"].ToString();
                            string nombreIns = dr["NombreInsumo"].ToString();
                            string um = dr["UnidadMedida"].ToString();

                            // Interceptar insumo ficticio de habilitación manual
                            if (codIns == "HAB-000000")
                            {
                                nombreIns = "HABILITACIÓN MANUAL - SISTEMA";
                                um = "UND";
                            }

                            lista.Add(new LIQ_REQ_RecepcionDetalleBE
                            {
                                IdRecepcionDetalle = Convert.ToInt32(dr["IdRecepcionDetalle"]),
                                NumRequerimiento = Convert.ToInt32(dr["NumRequerimiento"]),
                                CodInsumo = codIns,
                                NombreInsumo = nombreIns,
                                CantidadRecibida = Convert.ToDecimal(dr["CantidadRecibida"]),
                                UnidadMedida = um,
                                Lote = dr["Lote"] != DBNull.Value ? dr["Lote"].ToString() : ""
                            });
                        }
                    }
                }
            }
            return lista;
        }
        public DataTable ObtenerMatrizCruzadaConsumos()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Código", typeof(string));
            dt.Columns.Add("Insumo", typeof(string));
            dt.Columns.Add("Stock Real", typeof(decimal));

            using (SqlConnection cn = new SqlConnection(ConnectionString))
            {
                string sql = @"
                    SET ARITHABORT ON;
                    WITH CTE_Insumos AS (
                        SELECT 
                            1 AS OrdenFila,
                            I.CodInsumo AS [Código],
                            I.Descripcion AS [Insumo],
                            (ISNULL(I.StockActual, 0) + ISNULL(R.StockRecibido, 0) - 
                            (ISNULL(Op.ConsumosTotales, 0) / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END) - 
                            (ISNULL(Op.AjustesTotales, 0) / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END) - 
                            (ISNULL(Op.DevolucionesCentral, 0) / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END)) AS [Stock Real],
                            CASE WHEN O.TipoOperacion = 'Consumo Desarrollo' THEN
                                ISNULL(CONVERT(varchar, (SELECT MAX(FechaRegistro) FROM LIQ_OperacionesDetalle O2 WHERE O2.TipoOperacion = 'Consumo Desarrollo'), 103), '')
                            ELSE
                                ISNULL(CONVERT(varchar, (SELECT MAX(FechaRegistro) FROM LIQ_OperacionesDetalle O2 WHERE O2.NP = O.NP AND O2.TipoOperacion IN ('Consumo', 'Ajuste')), 103), '')
                            END + ' | ' + 
                            CASE WHEN O.TipoOperacion = 'Consumo Desarrollo' THEN 'DESARROLLO' ELSE ISNULL(F.Cliente, '') END + ' | ' + 
                            CASE WHEN O.TipoOperacion = 'Consumo Desarrollo' THEN 'MUESTRA' ELSE ISNULL(F.Estilo, '') END + ' | ' + 
                            CASE WHEN O.TipoOperacion = 'Consumo Desarrollo' THEN 'VARIOS' ELSE O.NP END + ' | ' + 
                            ISNULL(O.NombreColor, 'SIN COLOR') AS PivotCol,
                            (O.Cantidad / CASE WHEN I.UnidadMedida = 'KG' THEN 1000.0 ELSE 1.0 END) AS Cantidad
                        FROM LIQ_STK_StockInsumos I
                        LEFT JOIN (
                            SELECT CodInsumo, SUM(CantidadRecibida) AS StockRecibido
                            FROM LIQ_REQ_RecepcionesDetalle GROUP BY CodInsumo
                        ) R ON I.CodInsumo = R.CodInsumo
                        LEFT JOIN (
                            SELECT CodInsumo,
                                   SUM(CASE WHEN TipoOperacion IN ('Consumo', 'Consumo Desarrollo') AND FuenteConsumo IN ('Stock Inicial', 'Stock Solicitado', 'Solicitud Realizada') THEN Cantidad ELSE 0 END) AS ConsumosTotales,
                                   SUM(CASE WHEN TipoOperacion = 'Ajuste' AND FuenteConsumo IN ('Stock Inicial', 'Stock Solicitado', 'Solicitud Realizada') THEN Cantidad ELSE 0 END) AS AjustesTotales,
                                   SUM(CASE WHEN TipoOperacion = 'Devolucion Central' OR (TipoOperacion = 'Devolucion' AND Motivo LIKE '%Central%') THEN Cantidad ELSE 0 END) AS DevolucionesCentral,
                                   SUM(CASE WHEN TipoOperacion = 'Devolucion Operativo' OR (TipoOperacion = 'Devolucion' AND Motivo LIKE '%Operativo%') THEN Cantidad ELSE 0 END) AS DevolucionesOperativo
                            FROM LIQ_OperacionesDetalle GROUP BY CodInsumo
                        ) Op ON I.CodInsumo = Op.CodInsumo
                        INNER JOIN LIQ_OperacionesDetalle O ON I.CodInsumo = O.CodInsumo
                        LEFT JOIN LIQ_Formulas F ON O.NP = F.NP
                        WHERE O.TipoOperacion IN ('Consumo', 'Consumo Desarrollo', 'Ajuste') AND O.FuenteConsumo IN ('Stock Inicial', 'Stock Solicitado', 'Solicitud Realizada')
                    ),
                    CTE_Mermas AS (
                        SELECT 
                            2 AS OrdenFila,
                            M.CodigoMerma AS [Código],
                            M.NombreColor + ' (NP: ' + M.NP + ')' AS [Insumo],
                            CAST((M.Gramos - ISNULL(Op.ConsumosTotales, 0) - ISNULL(Op.AjustesTotales, 0)) / 1000.0 AS DECIMAL(18,4)) AS [Stock Real],
                            CASE WHEN O.TipoOperacion = 'Consumo Desarrollo' THEN
                                ISNULL(CONVERT(varchar, (SELECT MAX(FechaRegistro) FROM LIQ_OperacionesDetalle O2 WHERE O2.TipoOperacion = 'Consumo Desarrollo'), 103), '')
                            ELSE
                                ISNULL(CONVERT(varchar, (SELECT MAX(FechaRegistro) FROM LIQ_OperacionesDetalle O2 WHERE O2.NP = O.NP AND O2.TipoOperacion IN ('Consumo', 'Ajuste')), 103), '')
                            END + ' | ' + 
                            CASE WHEN O.TipoOperacion = 'Consumo Desarrollo' THEN 'DESARROLLO' ELSE ISNULL(F.Cliente, '') END + ' | ' + 
                            CASE WHEN O.TipoOperacion = 'Consumo Desarrollo' THEN 'MUESTRA' ELSE ISNULL(F.Estilo, '') END + ' | ' + 
                            CASE WHEN O.TipoOperacion = 'Consumo Desarrollo' THEN 'VARIOS' ELSE O.NP END + ' | ' + 
                            ISNULL(O.NombreColor, 'SIN COLOR') AS PivotCol,
                            (O.Cantidad / 1000.0) AS Cantidad
                        FROM LIQ_MER_MermasColor M
                        LEFT JOIN (
                            SELECT MermaReutilizada, 
                                   SUM(CASE WHEN TipoOperacion = 'Consumo' THEN Cantidad ELSE 0 END) AS ConsumosTotales,
                                   SUM(CASE WHEN TipoOperacion = 'Ajuste' THEN Cantidad ELSE 0 END) AS AjustesTotales
                            FROM LIQ_OperacionesDetalle
                            WHERE (TipoOperacion = 'Consumo' OR TipoOperacion = 'Ajuste') AND FuenteConsumo = 'Stock Merma'
                            GROUP BY MermaReutilizada
                        ) Op ON M.CodigoMerma = Op.MermaReutilizada
                        INNER JOIN LIQ_OperacionesDetalle O ON M.CodigoMerma = O.MermaReutilizada
                        LEFT JOIN LIQ_Formulas F ON O.NP = F.NP
                        WHERE O.TipoOperacion = 'Consumo' AND O.FuenteConsumo = 'Stock Merma'
                          AND M.FechaVencimiento IS NOT NULL 
                          AND M.Gramos > 0
                          AND CAST(M.FechaVencimiento AS DATE) >= CAST(GETDATE() AS DATE)
                          AND CAST(M.Gramos - ISNULL(Op.ConsumosTotales, 0) - ISNULL(Op.AjustesTotales, 0) AS DECIMAL(18,2)) > 0
                    )
                    SELECT * FROM CTE_Insumos
                    UNION ALL
                    SELECT * FROM CTE_Mermas
                    ORDER BY OrdenFila ASC, [Insumo] ASC, PivotCol ASC
                ";

                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.CommandType = CommandType.Text;
                    cn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        var rowDict = new Dictionary<string, DataRow>();
                        var pivotCols = new HashSet<string>();

                        while (dr.Read())
                        {
                            string codInsumo = dr["Código"].ToString();
                            if (!rowDict.ContainsKey(codInsumo))
                            {
                                DataRow newRow = dt.NewRow();
                                newRow["Código"] = codInsumo;
                                newRow["Insumo"] = dr["Insumo"].ToString();
                                newRow["Stock Real"] = Convert.ToDecimal(dr["Stock Real"]);
                                rowDict[codInsumo] = newRow;
                                dt.Rows.Add(newRow);
                            }

                            string pivotCol = dr["PivotCol"].ToString();
                            decimal cantidad = Convert.ToDecimal(dr["Cantidad"]);

                            if (!pivotCols.Contains(pivotCol))
                            {
                                pivotCols.Add(pivotCol);
                                dt.Columns.Add(pivotCol, typeof(decimal));
                            }

                            DataRow row = rowDict[codInsumo];
                            if (row.IsNull(pivotCol))
                            {
                                row[pivotCol] = cantidad;
                            }
                            else
                            {
                                row[pivotCol] = Convert.ToDecimal(row[pivotCol]) + cantidad;
                            }
                        }
                    }
                }
            }
            return dt;
        }

        // ==========================================
        // MÉTODOS PARA FÓRMULA EN BLANCO
        // ==========================================

        public ItemDatoBE ObtenerDatosPorNP(string np)
        {
            ItemDatoBE datos = null;
            using (SqlConnection cn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SP_LISTAR_DATOS_POR_NP", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@NP", np);
                    cn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            datos = new ItemDatoBE
                            {
                                CodCliente = dr["Cliente"]?.ToString(), 
                                CodTemcli = dr["Temporada"]?.ToString(),
                                CodItem = dr["Prendas"]?.ToString(),
                                CodTecnica = dr["CodEstiloCliente"]?.ToString(),
                                DescripcionTecnica = dr["CodEstiloPropio"]?.ToString()
                            };
                        }
                    }
                }
            }
            return datos;
        }

        public bool InsertarFormulaBlanco(VisitaBE receta, string usuario)
        {
            bool result = false;
            using (SqlConnection cn = new SqlConnection(ConnectionString))
            {
                cn.Open();
                using (SqlTransaction tr = cn.BeginTransaction())
                {
                    try
                    {
                        // 1. Insertar Cabecera (IdRecetaOrigen = 0)
                        string sqlCabecera = @"
                            INSERT INTO LIQ_Formulas 
                            (IdRecetaOrigen, NP, Cliente, Temporada, Estilo, EstiloPropio, Item, Combo, Ubicacion, Tecnica, OperarioUDP, FechaUDP, Prendas, Arte, UsuarioCreacion, Estado, Observaciones)
                            OUTPUT INSERTED.IdFormula
                            VALUES 
                            (0, @NP, @Cliente, @Temporada, @Estilo, @EstiloPropio, @Item, @Combo, @Ubicacion, @Tecnica, @Operario, @FechaUDP, @Prendas, @Arte, @UsuarioCreacion, 'Activa', @Observaciones);
                        ";
                        
                        int idFormula = 0;
                        using (SqlCommand cmd = new SqlCommand(sqlCabecera, cn, tr))
                        {
                            cmd.Parameters.AddWithValue("@NP", receta.NP ?? "");
                            cmd.Parameters.AddWithValue("@Cliente", receta.Cliente ?? "");
                            cmd.Parameters.AddWithValue("@Temporada", receta.Temporada ?? "");
                            cmd.Parameters.AddWithValue("@Estilo", receta.Estilo ?? "");
                            cmd.Parameters.AddWithValue("@EstiloPropio", receta.EstiloPropio ?? "");
                            cmd.Parameters.AddWithValue("@Item", receta.Item ?? "");
                            cmd.Parameters.AddWithValue("@Combo", receta.ComboCabecera ?? "");
                            cmd.Parameters.AddWithValue("@Ubicacion", receta.Ubicacion ?? "");
                            cmd.Parameters.AddWithValue("@Tecnica", receta.Tecnica ?? "");
                            cmd.Parameters.AddWithValue("@Operario", receta.Operario ?? "");
                            cmd.Parameters.AddWithValue("@FechaUDP", receta.FechaUDP ?? "");
                            cmd.Parameters.AddWithValue("@Prendas", receta.PrendasReq ?? "");
                            cmd.Parameters.AddWithValue("@Arte", receta.Arte ?? "");
                            cmd.Parameters.AddWithValue("@UsuarioCreacion", usuario ?? "");
                            cmd.Parameters.AddWithValue("@Observaciones", receta.Observaciones ?? "");

                            idFormula = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 2. Insertar Colores e Insumos
                        if (receta.Colores != null)
                        {
                            foreach (var color in receta.Colores)
                            {
                                string sqlColor = @"
                                    INSERT INTO LIQ_FormulaColores (IdFormula, NombreColor, Combo)
                                    OUTPUT INSERTED.IdFormulaColor
                                    VALUES (@IdFormula, @NombreColor, @Combo);
                                ";
                                int idFormulaColor = 0;
                                using (SqlCommand cmd = new SqlCommand(sqlColor, cn, tr))
                                {
                                    cmd.Parameters.AddWithValue("@IdFormula", idFormula);
                                    string nombreFinalColor = !string.IsNullOrEmpty(color.NombreColor) ? color.NombreColor : (color.Nombre ?? "");
                                    cmd.Parameters.AddWithValue("@NombreColor", nombreFinalColor);
                                    cmd.Parameters.AddWithValue("@Combo", color.Combo ?? "");
                                    idFormulaColor = Convert.ToInt32(cmd.ExecuteScalar());
                                }

                                if (color.Insumos != null)
                                {
                                    foreach (var insumo in color.Insumos)
                                    {
                                        string sqlInsumo = @"
                                            INSERT INTO LIQ_FormulaInsumos (IdFormulaColor, CodigoInsumo, Descripcion, Cantidad)
                                            VALUES (@IdFormulaColor, @CodigoInsumo, @Descripcion, @Cantidad);
                                            
                                            INSERT INTO LIQ_FormulaInsumosPrueba (IdFormula, NombreColor, CodigoInsumo, NombrePrueba, GramosUDP, EsPrincipal)
                                            VALUES (@IdFormula, @NombreColor, @CodigoInsumo, 'FORMULA_INICIAL', @Cantidad, 1);
                                        ";
                                        using (SqlCommand cmd = new SqlCommand(sqlInsumo, cn, tr))
                                        {
                                            cmd.Parameters.AddWithValue("@IdFormulaColor", idFormulaColor);
                                            cmd.Parameters.AddWithValue("@IdFormula", idFormula);
                                            string nombreFinalColorInsumo = !string.IsNullOrEmpty(color.NombreColor) ? color.NombreColor : (color.Nombre ?? "");
                                            cmd.Parameters.AddWithValue("@NombreColor", nombreFinalColorInsumo);
                                            string codigoFinal = !string.IsNullOrEmpty(insumo.CodigoInsumo) ? insumo.CodigoInsumo : (insumo.Codigo ?? "");
                                            cmd.Parameters.AddWithValue("@CodigoInsumo", codigoFinal);
                                            cmd.Parameters.AddWithValue("@Descripcion", insumo.Descripcion ?? "");
                                            cmd.Parameters.AddWithValue("@Cantidad", insumo.Cantidad);
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                        }

                        tr.Commit();
                        result = true;
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw new Exception("Error al insertar Fórmula en Blanco: " + ex.Message);
                    }
                }
            }
            return result;
        }
    }
}
