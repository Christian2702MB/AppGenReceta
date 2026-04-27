using System;
using System.Collections.Generic;
using System.Linq;
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

        // ==========================================
        // PASO 2: AGRUPACIÓN Y CÁLCULO
        // ==========================================
        public bool GuardarAgrupacionNP(List<E_InsumoNP> lista, string sessionId, string usuarioCrea)
        {
            if (lista == null || !lista.Any()) return false;

            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                con.Open();
                using (SqlTransaction tr = con.BeginTransaction())
                {
                    try
                    {
                        foreach (var item in lista)
                        {
                            string sql = @"INSERT INTO TBL_ESTAMPADO_AGRUPACION_NP 
                                (SESSION_ID, USUARIO_CREADOR, FECHA_REGISTRO, NP, COMBO, ESTILO_PROPIO, ESTILO_CLIENTE, FEC_DESPACHO, NOM_CLIENTE, CANT_PRODUCIR)
                                VALUES
                                (@SESSION_ID, @USUARIO_CREADOR, GETDATE(), @NP, @COMBO, @ESTILO_PROPIO, @ESTILO_CLIENTE, @FEC_DESPACHO, @NOM_CLIENTE, @CANT_PRODUCIR)";
                            
                            using (SqlCommand cmd = new SqlCommand(sql, con, tr))
                            {
                                cmd.Parameters.AddWithValue("@SESSION_ID", sessionId);
                                cmd.Parameters.AddWithValue("@USUARIO_CREADOR", usuarioCrea ?? "");
                                cmd.Parameters.AddWithValue("@NP", item.NP ?? "");
                                cmd.Parameters.AddWithValue("@COMBO", item.COMBO ?? "");
                                cmd.Parameters.AddWithValue("@ESTILO_PROPIO", item.ESTILO_PROPIO ?? "");
                                cmd.Parameters.AddWithValue("@ESTILO_CLIENTE", item.ESTILO_CLIENTE ?? "");
                                cmd.Parameters.AddWithValue("@FEC_DESPACHO", item.FEC_DESPACHO ?? "");
                                cmd.Parameters.AddWithValue("@NOM_CLIENTE", item.NOM_CLIENTE ?? "");
                                cmd.Parameters.AddWithValue("@CANT_PRODUCIR", item.NUM_ENPROCESO);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tr.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw new Exception("Error al guardar agrupación: " + ex.Message);
                    }
                }
            }
        }

        public List<E_InsumoNP> ObtenerAgrupacionNP(string sessionId)
        {
            List<E_InsumoNP> lista = new List<E_InsumoNP>();
            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                string sql = "SELECT * FROM TBL_ESTAMPADO_AGRUPACION_NP WHERE SESSION_ID = @SESSION_ID ORDER BY FECHA_REGISTRO ASC";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@SESSION_ID", sessionId);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            E_InsumoNP ent = new E_InsumoNP();
                            ent.NP = GetStringSafe(dr, "NP");
                            ent.COMBO = GetStringSafe(dr, "COMBO");
                            ent.ESTILO_PROPIO = GetStringSafe(dr, "ESTILO_PROPIO");
                            ent.ESTILO_CLIENTE = GetStringSafe(dr, "ESTILO_CLIENTE");
                            ent.FEC_DESPACHO = GetStringSafe(dr, "FEC_DESPACHO");
                            ent.NOM_CLIENTE = GetStringSafe(dr, "NOM_CLIENTE");
                            ent.NUM_ENPROCESO = GetIntSafe(dr, "CANT_PRODUCIR");
                            lista.Add(ent);
                        }
                    }
                }
            }
            return lista;
        }

        public List<E_InsumoCalculado> CalcularInsumosAgrupacion(string sessionId, string estilosCsv)
        {
            List<E_InsumoCalculado> lista = new List<E_InsumoCalculado>();
            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                using (SqlCommand cmd = new SqlCommand("GRI_CALCULAR_INSUMOS", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    
                    // Pasamos la agrupación de estilos propios
                    cmd.Parameters.AddWithValue("@ESTILOS_PROPIOS", estilosCsv);
                    
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            E_InsumoCalculado e = new E_InsumoCalculado();
                            e.CodigoInsumo = GetStringSafe(dr, "CodigoInsumo");
                            e.Descripcion = GetStringSafe(dr, "Descripcion");
                            e.NombreColor = GetStringSafe(dr, "NombreColor");
                            e.NombrePrueba = GetStringSafe(dr, "NombrePrueba");
                            e.GramosUDP = GetDecimalSafe(dr, "GramosUDP");
                            e.SESSION_ID = sessionId;
                            lista.Add(e);
                        }
                    }
                }
            }
            return lista;
        }

        public bool GuardarInsumosCalculados(List<E_InsumoCalculado> calculados, string sessionId, string usuario)
        {
            if (calculados == null || !calculados.Any()) return false;

            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                con.Open();
                using (SqlTransaction tr = con.BeginTransaction())
                {
                    try
                    {
                        foreach (var item in calculados)
                        {
                            string sql = @"INSERT INTO TBL_ESTAMPADO_INSUMO_CALCULADO 
                                (ID_AGRUPACION, COD_ARTICULO, DES_ARTICULO, COLOR, UNIDAD_MEDIDA, CANTIDAD_DEFINIDA, PRESENTACION, USUARIO_PROCESA, FECHA_PROCESO)
                                VALUES
                                ((SELECT TOP 1 ID_AGRUPACION FROM TBL_ESTAMPADO_AGRUPACION_NP WHERE SESSION_ID = @SESSION_ID ORDER BY ID_AGRUPACION DESC),
                                 @COD_ARTICULO, @DES_ARTICULO, @COLOR, 'UDP', @CANTIDAD_DEFINIDA, @PRESENTACION, @USUARIO_PROCESA, GETDATE())";
                            
                            using (SqlCommand cmd = new SqlCommand(sql, con, tr))
                            {
                                cmd.Parameters.AddWithValue("@SESSION_ID", sessionId);
                                cmd.Parameters.AddWithValue("@COD_ARTICULO", item.CodigoInsumo ?? "");
                                cmd.Parameters.AddWithValue("@DES_ARTICULO", item.Descripcion ?? "");
                                cmd.Parameters.AddWithValue("@COLOR", item.NombreColor ?? "");
                                cmd.Parameters.AddWithValue("@CANTIDAD_DEFINIDA", item.GramosUDP);
                                cmd.Parameters.AddWithValue("@PRESENTACION", item.NombrePrueba ?? "");
                                cmd.Parameters.AddWithValue("@USUARIO_PROCESA", usuario ?? "");
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tr.Commit();
                        return true;
                    }
                    catch(Exception ex)
                    {
                        tr.Rollback();
                        throw new Exception("Error guardando el cálculo final: " + ex.Message);
                    }
                }
            }
        }

        public List<E_InsumoCalculado> ObtenerInsumosCalculados(string sessionId)
        {
            List<E_InsumoCalculado> lista = new List<E_InsumoCalculado>();
            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                // Obtenemos los insumos usando el ID_AGRUPACION asociado al SESSION_ID
                string sql = @"
                    SELECT I.* 
                    FROM TBL_ESTAMPADO_INSUMO_CALCULADO I
                    INNER JOIN TBL_ESTAMPADO_AGRUPACION_NP A ON I.ID_AGRUPACION = A.ID_AGRUPACION
                    WHERE A.SESSION_ID = @SESSION_ID";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@SESSION_ID", sessionId);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            E_InsumoCalculado e = new E_InsumoCalculado();
                            e.ID_CALCULO = GetIntSafe(dr, "ID_CALCULO");
                            e.CodigoInsumo = GetStringSafe(dr, "COD_ARTICULO");
                            e.Descripcion = GetStringSafe(dr, "DES_ARTICULO");
                            e.NombreColor = GetStringSafe(dr, "COLOR");
                            e.NombrePrueba = GetStringSafe(dr, "PRESENTACION"); // mapeado inverso
                            e.GramosUDP = GetDecimalSafe(dr, "CANTIDAD_DEFINIDA"); // map inverso
                            e.SESSION_ID = sessionId;
                            lista.Add(e);
                        }
                    }
                }
            }
            return lista;
        }
    }
}
