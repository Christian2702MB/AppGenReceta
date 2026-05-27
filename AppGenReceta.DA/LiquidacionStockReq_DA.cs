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
                using (SqlCommand cmd = new SqlCommand("LIQ_STK_SP_ListarStockActual", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new LIQ_StockInsumoBE
                            {
                                CodInsumo = dr["CodInsumo"].ToString(),
                                Descripcion = dr["Descripcion"].ToString(),
                                UnidadMedida = dr["UnidadMedida"].ToString(),
                                StockActual = Convert.ToDecimal(dr["StockActual"]),
                                FechaModificacion = dr["FechaModificacion"].ToString()
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
                            lista.Add(new LIQ_REQ_RecepcionBE
                            {
                                NumRequerimiento = Convert.ToInt32(dr["NumRequerimiento"]),
                                CodOrdPro = dr["CodOrdPro"].ToString(),
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

        public string ConfirmarRecepcion(int numRequerimiento, string codOrdPro, string motivo, string usuarioRecepcion, string xmlDetalle)
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

        public string RegistrarCargaInicial(string codInsumo, string descripcion, string unidadMedida, decimal pesoGramos, string usuario)
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
                            lista.Add(new LIQ_REQ_RecepcionDetalleBE
                            {
                                IdRecepcionDetalle = Convert.ToInt32(dr["IdRecepcionDetalle"]),
                                NumRequerimiento = Convert.ToInt32(dr["NumRequerimiento"]),
                                CodInsumo = dr["CodInsumo"].ToString(),
                                NombreInsumo = dr["NombreInsumo"].ToString(),
                                CantidadRecibida = Convert.ToDecimal(dr["CantidadRecibida"]),
                                UnidadMedida = dr["UnidadMedida"].ToString(),
                                Lote = dr["Lote"] != DBNull.Value ? dr["Lote"].ToString() : ""
                            });
                        }
                    }
                }
            }
            return lista;
        }
    }
}
