using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using AppGenReceta.BE;

namespace AppGenReceta.DA
{
    public class InsumoNP_DA
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
                    return dr[i] != DBNull.Value ? dr[i].ToString() : string.Empty;
            return string.Empty;
        }

        private int GetIntSafe(SqlDataReader dr, string colName)
        {
            for (int i = 0; i < dr.FieldCount; i++)
                if (dr.GetName(i).Equals(colName, StringComparison.OrdinalIgnoreCase))
                    return dr[i] != DBNull.Value ? Convert.ToInt32(dr[i]) : 0;
            return 0;
        }

        private decimal GetDecimalSafe(SqlDataReader dr, string colName)
        {
            for (int i = 0; i < dr.FieldCount; i++)
                if (dr.GetName(i).Equals(colName, StringComparison.OrdinalIgnoreCase))
                    return dr[i] != DBNull.Value ? Convert.ToDecimal(dr[i]) : 0;
            return 0;
        }

        private List<E_InsumoNP> MapearNPs(SqlDataReader dr)
        {
            List<E_InsumoNP> lista = new List<E_InsumoNP>();
            while (dr.Read())
            {
                E_InsumoNP ent = new E_InsumoNP();
                ent.NP = GetStringSafe(dr, "COD_ORDPRO");
                if (string.IsNullOrEmpty(ent.NP)) ent.NP = GetStringSafe(dr, "NP"); // fallback

                ent.COD_FABRICA = GetStringSafe(dr, "COD_FABRICA");
                ent.COD_PRESENT = GetStringSafe(dr, "COD_PRESENT");
                ent.COD_PROVEEDOR = GetStringSafe(dr, "COD_PROVEEDOR");
                ent.COMBO = GetStringSafe(dr, "DES_PRESENT");
                if (string.IsNullOrEmpty(ent.COMBO)) ent.COMBO = GetStringSafe(dr, "COMBO"); // fallback
                ent.ESTILO_PROPIO = GetStringSafe(dr, "ESTILO_PROPIO");
                ent.ESTILO_CLIENTE = GetStringSafe(dr, "ESTILO_CLIENTE");
                ent.NOM_CLIENTE = GetStringSafe(dr, "NOM_CLIENTE");
                ent.COD_FAMITEM = GetStringSafe(dr, "COD_FAMITEM");
                ent.DES_FAMITEM = GetStringSafe(dr, "DES_FAMITEM");
                ent.PROVEEDOR = GetStringSafe(dr, "PROVEEDOR");
                ent.NUM_ENPROCESO = GetIntSafe(dr, "NUM_ENPROCESO");
                ent.NUM_ENREPROCESOS = GetIntSafe(dr, "NUM_ENREPROCESOS");
                ent.PROCESADAS = GetIntSafe(dr, "PROCESADAS");
                ent.REQUERIDAS = GetIntSafe(dr, "REQUERIDAS");
                ent.POR_INGRESAR = GetIntSafe(dr, "POR_INGRESAR");
                ent.NRO_ARTES = GetIntSafe(dr, "NRO_ARTES");
                ent.STOCK_VALORIZADO = GetDecimalSafe(dr, "STOCK_VALORIZADO");

                // Fechas (Manejadas como string desde la base para la Vista)
                ent.FEC_DESPACHO = GetStringSafe(dr, "FEC_DESPACHO");
                ent.FEC_PRIENTRADA = GetStringSafe(dr, "FEC_PRIENTRADA");
                ent.FECHA_FIN_PRODUCCION = GetStringSafe(dr, "FECHA_FIN_PRODUCCION");
                ent.FEC_SUGERIDA_DESPACHO = GetStringSafe(dr, "FEC_SUGERIDA_DESPACHO");

                ent.Seleccionado = true;
                lista.Add(ent);
            }
            return lista;
        }

        public List<E_InsumoNP> ObtenerNPsPorRango(DateTime fechaInicio, DateTime fechaFin)
        {
            List<E_InsumoNP> lista = new List<E_InsumoNP>();
            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                using (SqlCommand cmd = new SqlCommand("GRI_LISTAR_NP_AGRUPACION", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@FEC_INICIO", fechaInicio);
                    cmd.Parameters.AddWithValue("@FEC_FIN", fechaFin);
                    cmd.Parameters.AddWithValue("@NP", "");
                    try
                    {
                        con.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            lista = MapearNPs(dr);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    finally
                    {
                        if (con.State == ConnectionState.Open) con.Close();
                    }
                }
            }
            return lista;
        }

        public List<E_InsumoNP> ObtenerNPPorCodigo(string np, DateTime fechaInicio, DateTime fechaFin)
        {
            List<E_InsumoNP> lista = new List<E_InsumoNP>();
            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                using (SqlCommand cmd = new SqlCommand("GRI_LISTAR_NP_AGRUPACION", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@FEC_INICIO", fechaInicio);
                    cmd.Parameters.AddWithValue("@FEC_FIN", fechaFin);
                    cmd.Parameters.AddWithValue("@NP", np);
                    try
                    {
                        con.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            lista = MapearNPs(dr);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    finally
                    {
                        if (con.State == ConnectionState.Open) con.Close();
                    }
                }
            }
            return lista;
        }

        public List<E_NpAutocomplete> BuscarNPsAutocomplete(string texto)
        {
            List<E_NpAutocomplete> lista = new List<E_NpAutocomplete>();
            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                using (SqlCommand cmd = new SqlCommand("GRI_LISTAR_NPS", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@NP", texto);
                    try
                    {
                        con.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                E_NpAutocomplete ent = new E_NpAutocomplete();
                                string val = GetStringSafe(dr, "NP");
                                if (string.IsNullOrEmpty(val)) val = GetStringSafe(dr, "COD_ORDPRO");
                                ent.NP = val;
                                lista.Add(ent);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    finally
                    {
                        if (con.State == ConnectionState.Open) con.Close();
                    }
                }
            }
            return lista;
        }
    }
}
