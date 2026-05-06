using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using AppGenReceta.BE;

namespace AppGenReceta.DA
{
    public class ConsumoAdicional_DA
    {
        private string GetConnectionString()
        {
            var config = ConfigurationManager.ConnectionStrings["AppGenReceta_SQL"];
            if (config == null) throw new Exception("No se encontró la cadena de conexión 'AppGenReceta_SQL' en el Web.config.");
            return config.ConnectionString;
        }

        private string GetStringSafe(SqlDataReader dr, string colName)
        {
            for (int i = 0; i < dr.FieldCount; i++)
                if (dr.GetName(i).Equals(colName, StringComparison.OrdinalIgnoreCase))
                    return dr[i] != DBNull.Value ? dr[i].ToString().Trim() : string.Empty;
            return string.Empty;
        }

        private decimal GetDecimalSafe(SqlDataReader dr, string colName)
        {
            for (int i = 0; i < dr.FieldCount; i++)
                if (dr.GetName(i).Equals(colName, StringComparison.OrdinalIgnoreCase))
                    return dr[i] != DBNull.Value ? Convert.ToDecimal(dr[i]) : 0;
            return 0;
        }
        
        private int GetIntSafe(SqlDataReader dr, string colName)
        {
            for (int i = 0; i < dr.FieldCount; i++)
                if (dr.GetName(i).Equals(colName, StringComparison.OrdinalIgnoreCase))
                    return dr[i] != DBNull.Value ? Convert.ToInt32(dr[i]) : 0;
            return 0;
        }

        public List<E_ConsumoAdicionalCabecera> ListarRequerimientos(string opcion, string fechaDesde, string fechaHasta, string np)
        {
            var lista = new List<E_ConsumoAdicionalCabecera>();
            using (SqlConnection cn = new SqlConnection(GetConnectionString()))
            {
                using (SqlCommand cmd = new SqlCommand("ti_sm_muestra_req_adicionales_tinto", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@opcion", opcion);
                    cmd.Parameters.AddWithValue("@fecha", string.IsNullOrEmpty(fechaDesde) ? "" : fechaDesde);
                    cmd.Parameters.AddWithValue("@fecha2", string.IsNullOrEmpty(fechaHasta) ? "" : fechaHasta);
                    cmd.Parameters.AddWithValue("@cod_ordtra", string.IsNullOrEmpty(np) ? "" : np);
                    cmd.Parameters.AddWithValue("@cod_maquina_tinto", "");
                    cmd.Parameters.AddWithValue("@Num_Requerimiento", 0);

                    cn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        var idsProcesados = new HashSet<int>();
                        while (dr.Read())
                        {
                            int numReq = GetIntSafe(dr, "Num_Requerimiento");
                            if (numReq > 0 && idsProcesados.Contains(numReq)) continue;
                            if (numReq > 0) idsProcesados.Add(numReq);

                            var req = new E_ConsumoAdicionalCabecera();
                            req.NumRequerimiento = numReq;
                            // Fechas devueltas como string o formateadas
                            req.FecCreacion = dr["Fec_Creacion"] != DBNull.Value ? dr["Fec_Creacion"].ToString() : "";
                            req.UltimaImpresion = dr["Ultima_Impresion"] != DBNull.Value ? dr["Ultima_Impresion"].ToString() : "";
                            req.Partida = GetStringSafe(dr, "Partida");
                            req.Motivo = GetStringSafe(dr, "Motivo");
                            req.Nombre = GetStringSafe(dr, "Nombre");
                            req.Unidad = GetStringSafe(dr, "UN");
                            lista.Add(req);
                        }
                    }
                }
            }
            return lista;
        }

        public string ValidarYObtenerNP(string npBuscado)
        {
            string npResult = string.Empty;
            using (SqlConnection cn = new SqlConnection(GetConnectionString()))
            {
                string sql = "SELECT COD_ORDPRO FROM ES_ORDPRO WHERE COD_FABRICA = '001' AND COD_ORDPRO = @BusquedaNP AND FEC_CANCELACION IS NULL";
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@BusquedaNP", npBuscado.Trim());
                    cn.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        npResult = result.ToString().Trim();
                    }
                }
            }
            return npResult;
        }

        public int InsertarCabecera(string np, string observaciones)
        {
            using (SqlConnection cn = new SqlConnection(GetConnectionString()))
            {
                using (SqlCommand cmd = new SqlCommand("ti_up_man_Ti_Ordtra_Tintoreria_Req_Adic", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ACCION", "I");
                    cmd.Parameters.AddWithValue("@NUM_REQUERIMIENTO", "0");
                    cmd.Parameters.AddWithValue("@COD_ORDTRA", "");
                    cmd.Parameters.AddWithValue("@COD_MOTIVO_REQUER", "30 ");
                    cmd.Parameters.AddWithValue("@COD_MAQUINA_TINTO", "");
                    cmd.Parameters.AddWithValue("@OBSERVACIONES", observaciones ?? "");
                    cmd.Parameters.AddWithValue("@COD_USUARIO", "MILLANES");
                    cmd.Parameters.AddWithValue("@PC", "PC011520");
                    cmd.Parameters.AddWithValue("@COD_ORDPRO", np);
                    cmd.Parameters.AddWithValue("@COD_CLIENTE", "");
                    cmd.Parameters.AddWithValue("@COD_ORDPRO_TEX", "");

                    cn.Open();
                    object result = cmd.ExecuteScalar();
                    return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
                }
            }
        }

        public void InsertarDetalleSimple(int numReq, string codItem, decimal consumo, int lote = 0)
        {
            using (SqlConnection cn = new SqlConnection(GetConnectionString()))
            {
                using (SqlCommand cmd = new SqlCommand("ti_up_man_Ti_Ordtra_Tintoreria_Req_Adic_Items", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@accion", "I");
                    cmd.Parameters.AddWithValue("@num_requerimiento", numReq);
                    cmd.Parameters.AddWithValue("@secu", "0");
                    cmd.Parameters.AddWithValue("@cod_item", codItem);
                    cmd.Parameters.AddWithValue("@cons_requerido", consumo);
                    cmd.Parameters.AddWithValue("@cod_ordtra", "");
                    cmd.Parameters.AddWithValue("@Lote", lote);

                    cn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public string ObtenerEstiloPorNP(string np)
        {
            using (SqlConnection cn = new SqlConnection(GetConnectionString()))
            {
                string sql = "SELECT TOP 1 EstiloPropio AS ESTILO_PROPIO FROM AGR_Recetas WHERE NP = @np";
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@np", np);
                    cn.Open();
                    object res = cmd.ExecuteScalar();
                    return res != null ? res.ToString().Trim() : "";
                }
            }
        }

        public List<E_ConsumoAdicionalDetalle> ListarDetalles(int numReq)
        {
            var lista = new List<E_ConsumoAdicionalDetalle>();
            using (SqlConnection cn = new SqlConnection(GetConnectionString()))
            {
                using (SqlCommand cmd = new SqlCommand("ti_sm_muestra_req_adicionales_tinto", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@opcion", "4");
                    cmd.Parameters.AddWithValue("@fecha", "");
                    cmd.Parameters.AddWithValue("@fecha2", "");
                    cmd.Parameters.AddWithValue("@cod_ordtra", "");
                    cmd.Parameters.AddWithValue("@cod_maquina_tinto", "");
                    cmd.Parameters.AddWithValue("@Num_Requerimiento", numReq);

                    cn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var item = new E_ConsumoAdicionalDetalle();
                            item.NumRequerimiento = GetIntSafe(dr, "Num_Requerimiento");
                            item.Secuencia = GetIntSafe(dr, "Secu");
                            item.CodItem = GetStringSafe(dr, "Cod_Item");
                            item.Nombre = GetStringSafe(dr, "Nombre");
                            item.ConsumoRequerido = GetDecimalSafe(dr, "Consumo_Requerido");
                            item.Lote = GetIntSafe(dr, "Lote");
                            item.Unidad = GetStringSafe(dr, "UN");
                            if (!string.IsNullOrEmpty(item.CodItem)) lista.Add(item);
                        }
                    }
                }
            }
            return lista;
        }
    }
}
