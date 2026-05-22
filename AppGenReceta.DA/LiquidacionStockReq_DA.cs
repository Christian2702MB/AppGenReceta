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
                            string codOrdPro = dr["cod_ordpro"] != DBNull.Value ? dr["cod_ordpro"].ToString().Trim() : "";
                            string motivo = dr["Cod_Motivo_Requer"] != DBNull.Value ? dr["Cod_Motivo_Requer"].ToString().Trim() : "";
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
                                    Cliente = dr["Cliente"] != DBNull.Value ? dr["Cliente"].ToString() : "",
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
                using (SqlCommand cmd = new SqlCommand("LIQ_REQ_SP_ConfirmarRecepcion", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
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
    }
}
