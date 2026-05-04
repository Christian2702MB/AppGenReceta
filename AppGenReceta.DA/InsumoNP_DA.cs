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
                                (ID_AGRUPACION, COD_ARTICULO, DES_ARTICULO, COLOR, UNIDAD_MEDIDA, CANTIDAD_SUGERIDA, CANTIDAD_DEFINIDA, PRESENTACION, USUARIO_PROCESA, FECHA_PROCESO)
                                VALUES
                                ((SELECT TOP 1 ID_AGRUPACION FROM TBL_ESTAMPADO_AGRUPACION_NP WHERE SESSION_ID = @SESSION_ID ORDER BY ID_AGRUPACION DESC),
                                 @COD_ARTICULO, @DES_ARTICULO, @COLOR, 'UDP', @CANTIDAD_DEFINIDA, @CANTIDAD_DEFINIDA, '', @USUARIO_PROCESA, GETDATE())";
                            
                            using (SqlCommand cmd = new SqlCommand(sql, con, tr))
                            {
                                cmd.Parameters.AddWithValue("@SESSION_ID", sessionId);
                                cmd.Parameters.AddWithValue("@COD_ARTICULO", item.CodigoInsumo ?? "");
                                cmd.Parameters.AddWithValue("@DES_ARTICULO", item.Descripcion ?? "");
                                cmd.Parameters.AddWithValue("@COLOR", item.NombreColor ?? "");
                                cmd.Parameters.AddWithValue("@CANTIDAD_DEFINIDA", item.GramosUDP);
                                //cmd.Parameters.AddWithValue("@PRESENTACION", item.NombrePrueba ?? "");
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
                con.Open();

                // PASO 1: Insumos base del session
                string sqlBase = @"
                    SELECT I.*
                    FROM TBL_ESTAMPADO_INSUMO_CALCULADO I
                    INNER JOIN TBL_ESTAMPADO_AGRUPACION_NP A ON I.ID_AGRUPACION = A.ID_AGRUPACION
                    WHERE A.SESSION_ID = @SESSION_ID";

                using (SqlCommand cmd = new SqlCommand(sqlBase, con))
                {
                    cmd.Parameters.AddWithValue("@SESSION_ID", sessionId);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new E_InsumoCalculado
                            {
                                ID_CALCULO        = GetIntSafe(dr, "ID_CALCULO"),
                                CodigoInsumo      = GetStringSafe(dr, "COD_ARTICULO"),
                                Descripcion       = GetStringSafe(dr, "DES_ARTICULO"),
                                NombreColor       = GetStringSafe(dr, "COLOR"),
                                NombrePrueba      = GetStringSafe(dr, "PRESENTACION"),
                                GramosUDP         = GetDecimalSafe(dr, "CANTIDAD_DEFINIDA"),
                                GramosUDPSugerido = GetDecimalSafe(dr, "CANTIDAD_SUGERIDA"),
                                SESSION_ID        = sessionId,
                                ProveedoresOpciones = new List<E_ProveedorOpcion>()
                            });
                        }
                    }
                }

                if (!lista.Any()) return lista;

                // PASO 2: Batch query proveedores+presentaciones (anti-N+1)
                // Formula: DES_PRESENTACION + CAPACIDAD_NUMERICA + UNIDAD_MEDIDA -> "Balde 20 kg"
                var codigos    = lista.Select(x => x.CodigoInsumo).Distinct().ToList();
                var paramNames = codigos.Select((_, i) => "@p" + i).ToList();
                string inClause = string.Join(",", paramNames);

                string sqlProv = @"
                    SELECT
                        IP.COD_ARTICULO,
                        P.ID_PROVEEDOR,
                        P.RAZON_SOCIAL,
                        IP.DES_PRESENTACION,
                        IP.CAPACIDAD_NUMERICA,
                        IP.UNIDAD_MEDIDA,
                        LTRIM(RTRIM(
                            ISNULL(IP.DES_PRESENTACION,'') + ' ' +
                            ISNULL(CONVERT(VARCHAR,CONVERT(DECIMAL(18,2),IP.CAPACIDAD_NUMERICA)),'') + ' ' +
                            ISNULL(IP.UNIDAD_MEDIDA,'')
                        )) AS TEXTO_PRESENTACION
                    FROM TBL_ESTAMPADO_INSUMO_PROVEEDOR IP
                    INNER JOIN TBL_ESTAMPADO_PROVEEDOR P ON IP.ID_PROVEEDOR = P.ID_PROVEEDOR
                    WHERE IP.COD_ARTICULO IN (" + inClause + @")
                      AND P.ES_ACTIVO = 1
                    ORDER BY IP.COD_ARTICULO, P.RAZON_SOCIAL";

                var mapProv = new Dictionary<string, List<E_ProveedorOpcion>>(StringComparer.OrdinalIgnoreCase);

                using (SqlCommand cmdProv = new SqlCommand(sqlProv, con))
                {
                    for (int i = 0; i < codigos.Count; i++)
                        cmdProv.Parameters.AddWithValue(paramNames[i], codigos[i]);

                    using (SqlDataReader dr = cmdProv.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            string cod    = GetStringSafe(dr, "COD_ARTICULO");
                            int    idProv = GetIntSafe(dr, "ID_PROVEEDOR");

                            if (!mapProv.ContainsKey(cod))
                                mapProv[cod] = new List<E_ProveedorOpcion>();

                            // Evitar duplicados de proveedor por insumo
                            if (mapProv[cod].Any(x => x.IdProveedor == idProv)) continue;

                            mapProv[cod].Add(new E_ProveedorOpcion
                            {
                                IdProveedor       = idProv,
                                RazonSocial       = GetStringSafe(dr, "RAZON_SOCIAL"),
                                CapacidadNumerica = GetDecimalSafe(dr, "CAPACIDAD_NUMERICA"),
                                Presentacion      = GetStringSafe(dr, "TEXTO_PRESENTACION")
                            });
                        }
                    }
                }

                // Asignar proveedores y presentacion a cada insumo
                foreach (var e in lista)
                {
                    if (mapProv.ContainsKey(e.CodigoInsumo) && mapProv[e.CodigoInsumo].Any())
                    {
                        e.ProveedoresOpciones = mapProv[e.CodigoInsumo];
                        var primero           = e.ProveedoresOpciones.First();
                        e.Presentacion        = string.IsNullOrWhiteSpace(primero.Presentacion)
                                               ? "Sin presentacion" : primero.Presentacion;
                        e.CapacidadNumerica   = primero.CapacidadNumerica;
                    }
                    else
                    {
                        // Sin proveedor: combo mostrara solo Stock propio
                        e.Presentacion        = "Unidad Base";
                        e.CapacidadNumerica   = 0; // Se cambia a 0 para que no calcule compra si no hay proveedor/capacidad
                    }
                }

                // PASO 3: Stock real via USP_EST_OBTENER_STOCK_ACTUAL y Cálculo inicial
                foreach (var e in lista)
                {
                    try
                    {
                        using (SqlCommand cmdStock = new SqlCommand("USP_EST_OBTENER_STOCK_ACTUAL", con))
                        {
                            cmdStock.CommandType = CommandType.StoredProcedure;
                            cmdStock.Parameters.AddWithValue("@COD_ARTICULO", e.CodigoInsumo);
                            using (SqlDataReader dr = cmdStock.ExecuteReader())
                            {
                                if (dr.Read())
                                {
                                    string raw    = GetStringSafe(dr, "Stock_Libras");
                                    decimal stock = 0;
                                    if (!string.IsNullOrWhiteSpace(raw))
                                        decimal.TryParse(raw.Split(' ')[0].Trim(),
                                            System.Globalization.NumberStyles.Any,
                                            System.Globalization.CultureInfo.InvariantCulture,
                                            out stock);
                                    e.StockActual = stock;
                                }
                            }
                        }
                    }
                    catch { e.StockActual = 0; }

                    // LÓGICA DE NEGOCIO: Cantidad a Pedir (Ceiling)
                    if (e.GramosUDP > e.StockActual && e.CapacidadNumerica > 0)
                    {
                        decimal faltante = e.GramosUDP - e.StockActual;
                        e.CantidadAPedir = (decimal)Math.Ceiling((double)(faltante / e.CapacidadNumerica));
                    }
                    else
                    {
                        e.CantidadAPedir = 0;
                    }
                }
            }
            return lista;
        }


        // =====================================================
        // PASO 3: PERSISTENCIA AL CONFIRMAR AJUSTES
        // =====================================================

        /// <summary>
        /// Actualiza insumos existentes (ID_CALCULO > 0) con todos los campos del Paso 3.
        /// Incluye PRESENTACION seleccionada por el usuario.
        /// </summary>
        public bool ActualizarAjustesInsumosCalculados(List<E_InsumoCalculado> calculados)
        {
            if (calculados == null || !calculados.Any()) return false;
            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                con.Open();
                using (SqlTransaction tr = con.BeginTransaction())
                {
                    try
                    {
                        foreach (var item in calculados.Where(x => x.ID_CALCULO > 0))
                        {
                            string sql = @"UPDATE TBL_ESTAMPADO_INSUMO_CALCULADO SET
                                CANTIDAD_DEFINIDA     = @CANTIDAD_DEFINIDA,
                                CANTIDAD_A_PEDIR      = @CANTIDAD_A_PEDIR,
                                TIPO_DESPACHO         = @TIPO_DESPACHO,
                                ID_PROVEEDOR_ASIGNADO = @ID_PROVEEDOR_ASIGNADO,
                                STOCK_CONSULTADO      = @STOCK_CONSULTADO,
                                PRESENTACION          = @PRESENTACION
                                WHERE ID_CALCULO = @ID_CALCULO";

                            using (SqlCommand cmd = new SqlCommand(sql, con, tr))
                            {
                                cmd.Parameters.AddWithValue("@ID_CALCULO",             item.ID_CALCULO);
                                cmd.Parameters.AddWithValue("@CANTIDAD_DEFINIDA",      item.GramosUDP);
                                //cmd.Parameters.AddWithValue("@CANTIDAD_SUGERIDA",      item.GramosUDPSugerido > 0 ? item.GramosUDPSugerido : item.GramosUDP);
                                cmd.Parameters.AddWithValue("@CANTIDAD_A_PEDIR",       item.CantidadAPedir);
                                cmd.Parameters.AddWithValue("@TIPO_DESPACHO",          item.TipoDespacho ?? "Total");
                                cmd.Parameters.AddWithValue("@ID_PROVEEDOR_ASIGNADO",  item.IdProveedorAsignado);
                                cmd.Parameters.AddWithValue("@STOCK_CONSULTADO",       item.StockActual);
                                cmd.Parameters.AddWithValue("@PRESENTACION",           item.Presentacion ?? (object)DBNull.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tr.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw new Exception("Error al actualizar TBL_ESTAMPADO_INSUMO_CALCULADO: " + ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Inserta los insumos extra-receta (ID_CALCULO=0) en TBL_ESTAMPADO_INSUMO_CALCULADO.
        /// Obtiene el ID_AGRUPACION desde el SESSION_ID.
        /// </summary>
        public bool InsertarInsumosExtraReceta(string sessionId, List<E_InsumoCalculado> extras)
        {
            if (extras == null || !extras.Any()) return true; // Nada que insertar es OK
            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                con.Open();

                // Obtener ID_AGRUPACION del session
                int idAgrupacion = 0;
                using (SqlCommand cmdAg = new SqlCommand(
                    "SELECT TOP 1 ID_AGRUPACION FROM TBL_ESTAMPADO_AGRUPACION_NP WHERE SESSION_ID = @SID", con))
                {
                    cmdAg.Parameters.AddWithValue("@SID", sessionId);
                    object res = cmdAg.ExecuteScalar();
                    if (res == null || res == DBNull.Value)
                        throw new Exception("No se encontrÃ³ agrupaciÃ³n para el session: " + sessionId);
                    idAgrupacion = Convert.ToInt32(res);
                }

                using (SqlTransaction tr = con.BeginTransaction())
                {
                    try
                    {
                        foreach (var item in extras)
                        {
                            string sql = @"
                                INSERT INTO TBL_ESTAMPADO_INSUMO_CALCULADO
                                    (ID_AGRUPACION, COD_ARTICULO, DES_ARTICULO,
                                     CANTIDAD_DEFINIDA, CANTIDAD_SUGERIDA, CANTIDAD_A_PEDIR,
                                     TIPO_DESPACHO, ID_PROVEEDOR_ASIGNADO,
                                     PRESENTACION, STOCK_CONSULTADO, ES_EXTRA_RECETA)
                                VALUES
                                    (@ID_AGRUPACION, @COD_ARTICULO, @DES_ARTICULO,
                                     @CANTIDAD_DEFINIDA, @CANTIDAD_SUGERIDA, @CANTIDAD_A_PEDIR,
                                     @TIPO_DESPACHO, @ID_PROVEEDOR_ASIGNADO,
                                     @PRESENTACION, 0, 1)";

                            using (SqlCommand cmd = new SqlCommand(sql, con, tr))
                            {
                                cmd.Parameters.AddWithValue("@ID_AGRUPACION",          idAgrupacion);
                                cmd.Parameters.AddWithValue("@COD_ARTICULO",           item.CodigoInsumo ?? "");
                                cmd.Parameters.AddWithValue("@DES_ARTICULO",           item.Descripcion  ?? "");
                                cmd.Parameters.AddWithValue("@CANTIDAD_DEFINIDA",      item.GramosUDP);
                                cmd.Parameters.AddWithValue("@CANTIDAD_SUGERIDA",      item.GramosUDP);
                                cmd.Parameters.AddWithValue("@CANTIDAD_A_PEDIR",       item.CantidadAPedir);
                                cmd.Parameters.AddWithValue("@TIPO_DESPACHO",          item.TipoDespacho  ?? "Total");
                                cmd.Parameters.AddWithValue("@ID_PROVEEDOR_ASIGNADO",  item.IdProveedorAsignado);
                                cmd.Parameters.AddWithValue("@PRESENTACION",           item.Presentacion  ?? (object)DBNull.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tr.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw new Exception("Error al insertar insumos extra-receta: " + ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Obtiene el resumen completo del Paso 4: insumos calculados + precio unitario del proveedor.
        /// Solo retorna filas que requieren compra (CANTIDAD_A_PEDIR > 0).
        /// </summary>
        public E_SolicitudResumen ObtenerResumenSolicitud(string sessionId)
        {
            var resumen = new E_SolicitudResumen { SessionId = sessionId };
            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                con.Open();

                // 1. NPs incluidas en la agrupacion
                using (SqlCommand cmdNP = new SqlCommand(@"
                    SELECT DISTINCT N.NP
                    FROM TBL_ESTAMPADO_AGRUPACION_NP N
                    INNER JOIN TBL_ESTAMPADO_AGRUPACION_NP A ON N.ID_AGRUPACION = A.ID_AGRUPACION
                    WHERE A.SESSION_ID = @SID", con))
                {
                    cmdNP.Parameters.AddWithValue("@SID", sessionId);
                    using (SqlDataReader dr = cmdNP.ExecuteReader())
                    {
                        while (dr.Read()) resumen.NPsIncluidas.Add(dr[0].ToString());
                    }
                }

                // 2. Insumos calculados con precio unitario del proveedor asignado
                string sqlItems = @"
                    SELECT
                        IC.ID_CALCULO,
                        IC.COD_ARTICULO,
                        IC.DES_ARTICULO,
                        IC.COLOR,
                        IC.CANTIDAD_DEFINIDA,
                        IC.CANTIDAD_A_PEDIR,
                        IC.TIPO_DESPACHO,
                        IC.ID_PROVEEDOR_ASIGNADO,
                        IC.PRESENTACION,
                        IC.STOCK_CONSULTADO,
                        IC.ES_EXTRA_RECETA,
                        ISNULL(IP.PRECIO_UNITARIO, 0) AS PRECIO_UNITARIO
                    FROM TBL_ESTAMPADO_INSUMO_CALCULADO IC
                    INNER JOIN TBL_ESTAMPADO_AGRUPACION_NP A ON IC.ID_AGRUPACION = A.ID_AGRUPACION
                    LEFT JOIN TBL_ESTAMPADO_INSUMO_PROVEEDOR IP
                        ON IP.COD_ARTICULO = IC.COD_ARTICULO
                       AND IP.ID_PROVEEDOR = IC.ID_PROVEEDOR_ASIGNADO
                    WHERE A.SESSION_ID = @SID";

                var todosLosItems = new List<E_InsumoCalculado>();
                using (SqlCommand cmdItems = new SqlCommand(sqlItems, con))
                {
                    cmdItems.Parameters.AddWithValue("@SID", sessionId);
                    using (SqlDataReader dr = cmdItems.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            todosLosItems.Add(new E_InsumoCalculado
                            {
                                ID_CALCULO          = GetIntSafe(dr, "ID_CALCULO"),
                                CodigoInsumo        = GetStringSafe(dr, "COD_ARTICULO"),
                                Descripcion         = GetStringSafe(dr, "DES_ARTICULO"),
                                NombreColor         = GetStringSafe(dr, "COLOR"),
                                GramosUDP           = GetDecimalSafe(dr, "CANTIDAD_DEFINIDA"),
                                CantidadAPedir      = GetDecimalSafe(dr, "CANTIDAD_A_PEDIR"),
                                TipoDespacho        = GetStringSafe(dr, "TIPO_DESPACHO"),
                                IdProveedorAsignado = GetIntSafe(dr, "ID_PROVEEDOR_ASIGNADO"),
                                Presentacion        = GetStringSafe(dr, "PRESENTACION"),
                                StockActual         = GetDecimalSafe(dr, "STOCK_CONSULTADO"),
                                EsExtraReceta       = dr["ES_EXTRA_RECETA"] != DBNull.Value && Convert.ToBoolean(dr["ES_EXTRA_RECETA"]),
                                PrecioUnitario      = GetDecimalSafe(dr, "PRECIO_UNITARIO")
                            });
                        }
                    }
                }

                // 3. Calcular mÃ©tricas del resumen con LINQ
                resumen.ItemsAComprar      = todosLosItems.Count(x => x.TipoDespacho != "Vacio" && x.CantidadAPedir > 0);
                resumen.ItemsConStock      = todosLosItems.Count(x => x.StockActual > 0);
                resumen.CantidadProveedores = todosLosItems.Where(x => x.IdProveedorAsignado > 0).Select(x => x.IdProveedorAsignado).Distinct().Count();
                resumen.CostoEstimado      = todosLosItems.Where(x => x.CantidadAPedir > 0).Sum(x => x.CantidadAPedir * x.PrecioUnitario);

                // Solo Ã­tems que requieren compra para la tabla inferior
                resumen.Items = todosLosItems.Where(x => x.CantidadAPedir > 0).OrderBy(x => x.CodigoInsumo).ToList();
            }
            return resumen;
        }

        /// <summary>
        /// Busca insumos en el maestro por código o descripción (mínimo 3 caracteres).
        /// Usado para agregar insumos extra-receta en el Paso 3.
        /// </summary>
        public List<E_InsumoCalculado> BuscarInsumosFiltro(string query)
        {
            List<E_InsumoCalculado> lista = new List<E_InsumoCalculado>();
            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                using (SqlCommand cmd = new SqlCommand("SP_LISTAR_INSUMOS", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            E_InsumoCalculado e = new E_InsumoCalculado();
                            e.CodigoInsumo = GetStringSafe(dr, "Codigo");
                            e.Descripcion  = GetStringSafe(dr, "Descripcion");
                            lista.Add(e);
                        }
                    }
                }
            }

            // Filtrar en memoria por código o descripción
            if (!string.IsNullOrWhiteSpace(query))
            {
                string q = query.Trim().ToUpper();
                lista = lista
                    .Where(x => (x.CodigoInsumo != null && x.CodigoInsumo.ToUpper().Contains(q))
                             || (x.Descripcion   != null && x.Descripcion.ToUpper().Contains(q)))
                    .Take(50)
                    .ToList();
            }

            return lista;
        }
        /// <summary>
        /// Paso Final: Carga la solicitud en el ERP (Tablas de Requerimientos de Compra).
        /// Ejecuta Cabecera y Detalle en una sola Transacción.
        /// </summary>
        public string CargarGestionPedidos(string sessionId, string observaciones, string usuario)
        {
            string numRequerimiento = "";
            string codArea = "";

            // 1. Obtener los ítems que requieren compra para esta sesión
            var resumen = ObtenerResumenSolicitud(sessionId);
            var items = resumen.Items;

            if (items == null || !items.Any())
                throw new Exception("No hay ítems válidos para cargar en Gestión de Pedidos.");

            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                con.Open();
                using (SqlTransaction tr = con.BeginTransaction())
                {
                    try
                    {
                        // 2. EJECUCIÓN CABECERA: LG_MAN_Requerimiento_Items
                        using (SqlCommand cmdCab = new SqlCommand("LG_MAN_Requerimiento_Items", con, tr))
                        {
                            cmdCab.CommandType = CommandType.StoredProcedure;
                            cmdCab.Parameters.AddWithValue("@ACCION", "I");
                            cmdCab.Parameters.AddWithValue("@Cod_Area", "CN");
                            cmdCab.Parameters.AddWithValue("@Num_Requerimiento", 0);
                            cmdCab.Parameters.AddWithValue("@Fec_Requerimiento", DateTime.Now.Date);
                            cmdCab.Parameters.AddWithValue("@Cod_Motivo", "003");
                            cmdCab.Parameters.AddWithValue("@Observacion", observaciones ?? "");
                            cmdCab.Parameters.AddWithValue("@Cod_Fabrica_Solicitante", "002");
                            cmdCab.Parameters.AddWithValue("@Tip_Trabajador_Solicitante", "E");
                            cmdCab.Parameters.AddWithValue("@Cod_Trabajador_Solicitante", "2081");

                            using (SqlDataReader dr = cmdCab.ExecuteReader())
                            {
                                if (dr.Read())
                                {
                                    // El SP devuelve "SELECT @Num_Requerimiento as num" en el primer Result Set
                                    numRequerimiento = dr["num"].ToString();
                                    codArea = "CN"; // Sabemos que es CN porque lo enviamos en el parámetro
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(numRequerimiento))
                            throw new Exception("El SP de cabecera no devolvió un Número de Requerimiento.");

                        // 3. EJECUCIÓN DETALLE: LG_MAN_Requerimiento_Items_DETALLE
                        foreach (var item in items)
                        {
                            using (SqlCommand cmdDet = new SqlCommand("LG_MAN_Requerimiento_Items_DETALLE", con, tr))
                            {
                                cmdDet.CommandType = CommandType.StoredProcedure;
                                cmdDet.Parameters.AddWithValue("@ACCION", "I");
                                cmdDet.Parameters.AddWithValue("@Cod_Area", codArea);
                                cmdDet.Parameters.AddWithValue("@Num_Requerimiento", numRequerimiento);
                                cmdDet.Parameters.AddWithValue("@Secuencia", 0);
                                cmdDet.Parameters.AddWithValue("@Tip_Requerimiento", "P");
                                cmdDet.Parameters.AddWithValue("@Cod_Item", item.CodigoInsumo);
                                cmdDet.Parameters.AddWithValue("@Des_Temporal_Item", "");
                                cmdDet.Parameters.AddWithValue("@Cod_Fabricacion", "");
                                cmdDet.Parameters.AddWithValue("@Cantidad", item.CantidadAPedir);
                                
                                string um = string.IsNullOrEmpty(item.UnidadMedida) ? "UN" : item.UnidadMedida;
                                if(um.Length > 2) um = um.Substring(0, 2);
                                
                                cmdDet.Parameters.AddWithValue("@Cod_UniMed", um);
                                cmdDet.Parameters.AddWithValue("@Cod_Equipo", "");
                                cmdDet.ExecuteNonQuery();
                            }
                        }

                        // 4. Actualizar estado en nuestra tabla local para saber que ya fue enviado
                        string sqlUpdate = "UPDATE TBL_ESTAMPADO_INSUMO_CALCULADO SET NUM_REQUERIMIENTO_ERP = @NUM WHERE ID_AGRUPACION IN (SELECT ID_AGRUPACION FROM TBL_ESTAMPADO_AGRUPACION_NP WHERE SESSION_ID = @SID)";
                        using (SqlCommand cmdUpd = new SqlCommand(sqlUpdate, con, tr))
                        {
                            cmdUpd.Parameters.AddWithValue("@NUM", numRequerimiento);
                            cmdUpd.Parameters.AddWithValue("@SID", sessionId);
                            cmdUpd.ExecuteNonQuery();
                        }

                        tr.Commit();
                        return numRequerimiento;
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw new Exception("Error en Cascada ERP: " + ex.Message);
                    }
                }
            }
        }
    }
}
