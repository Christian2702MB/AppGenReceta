using AppGenReceta.BE;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace AppGenReceta.DA
{
    /// <summary>
    /// Capa de Acceso a Datos para el módulo de Liquidación de Insumos.
    /// Tablas: LIQ_Formulas, LIQ_FormulaColores, LIQ_FormulaInsumos, LIQ_FormulaInsumosPrueba
    /// </summary>
    public class Liquidacion_DA
    {
        private string ConnectionString = ConfigurationManager.ConnectionStrings["AppGenReceta_SQL"].ConnectionString;

        public List<string> ObtenerVersionesNP(string baseNP)
        {
            var lista = new List<string>();
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                // Extraer base en caso de que pasen algo como I8253-V2
                string realBase = baseNP;
                if (realBase.Contains("-V"))
                {
                    realBase = realBase.Substring(0, realBase.IndexOf("-V"));
                }

                // Buscar la base exacta y las versiones (Activas, En Proceso, Pendientes, Liquidadas) -> No terminadas
                // O mejor, devolver todas las que existen y el JS o el SP decide? El usuario pidio "todas las versiones encontradas" 
                // pero auto-seleccionar la que este Activa/En Proceso.
                // Traeremos NP y Estado para preseleccionar? El metodo puede retornar objetos anonimos, pero la firma List<string> es simple.
                // Hagamos que retorne un JSON object o solo las NPs, y en frontend seleccionamos la última.
                // "auto-seleccionar la version que se encuentre en estado Activo/En Proceso"
                string sql = @"
                    SELECT NP 
                    FROM LIQ_Formulas 
                    WHERE (NP = @BaseNP OR NP LIKE @BaseNP + '-V%') 
                      AND Estado NOT IN ('Eliminada', 'Terminado', 'Cerrada')
                    ORDER BY 
                      CASE 
                        WHEN Estado IN ('Activa', 'En Proceso') THEN 1 
                        WHEN Estado IN ('Pendiente', 'Liquidado') THEN 2
                        ELSE 3 
                      END ASC, 
                      FechaCreacion DESC;
                ";
                SqlCommand cmd = new SqlCommand(sql, cnx);
                cmd.Parameters.AddWithValue("@BaseNP", realBase);
                cnx.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(dr["NP"].ToString());
                    }
                }
            }
            return lista;
        }

        public List<string> ObtenerItemsActivosNP(string baseNP)
        {
            var lista = new List<string>();
            if (string.IsNullOrEmpty(baseNP)) return lista;

            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                string realBase = baseNP;
                if (realBase.Contains("-"))
                {
                    realBase = realBase.Split('-')[0];
                }

                string sql = @"
                    SELECT DISTINCT ISNULL(Item, '0000') AS Item 
                    FROM LIQ_Formulas 
                    WHERE (NP = @BaseNP OR NP LIKE @BaseNP + '-%' OR REPLACE(LOWER(NP), 'i', '') = REPLACE(LOWER(@BaseNP), 'i', ''))
                      AND Estado = 'Activa'
                      AND ISNULL(Item, '0000') <> '0000';
                ";
                SqlCommand cmd = new SqlCommand(sql, cnx);
                cmd.Parameters.AddWithValue("@BaseNP", realBase);
                cnx.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string itm = dr["Item"].ToString();
                        if (!string.IsNullOrEmpty(itm) && !lista.Contains(itm))
                        {
                            lista.Add(itm);
                        }
                    }
                }
            }
            return lista;
        }

        public List<LIQ_NPSinRecepcionBE> ObtenerLiquidacionesActivasCombo()
        {
            List<LIQ_NPSinRecepcionBE> lista = new List<LIQ_NPSinRecepcionBE>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("LIQ_SP_ObtenerLiquidacionesActivasCombo", cnx))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cnx.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                lista.Add(new LIQ_NPSinRecepcionBE
                                {
                                    NP = dr["NP"].ToString(),
                                    Item = dr["Item"] != DBNull.Value ? dr["Item"].ToString() : "",
                                    Cliente = dr["Cliente"] != DBNull.Value ? dr["Cliente"].ToString() : "",
                                    Estilo = dr["Estilo"] != DBNull.Value ? dr["Estilo"].ToString() : ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lista;
        }
        
        /// <summary>
        /// Lista fórmulas para el grid de Mantenimiento, filtradas por rango de fechas.
        /// </summary>
        public List<LIQ_FormulaBE> ListarFormulas(string fechaInicio, string fechaFin)
        {
            List<LIQ_FormulaBE> lista = new List<LIQ_FormulaBE>();
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_ListarFormulas", cnx);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@FechaFin", fechaFin ?? (object)DBNull.Value);

                cnx.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new LIQ_FormulaBE
                        {
                            IdFormula = Convert.ToInt32(dr["IdFormula"]),
                            IdRecetaOrigen = Convert.ToInt32(dr["IdRecetaOrigen"]),
                            NP = dr["NP"].ToString(),
                            Cliente = dr["Cliente"].ToString(),
                            Temporada = dr["Temporada"].ToString(),
                            Estilo = dr["Estilo"].ToString(),
                            EstiloPropio = dr["EstiloPropio"].ToString(),
                            Item = dr["Item"].ToString(),
                            ComboCabecera = dr["Combo"].ToString(),
                            Ubicacion = dr["Ubicacion"].ToString(),
                            Tecnica = dr["Tecnica"].ToString(),
                            Operario = dr["OperarioUDP"].ToString(),
                            PrendasReq = dr["Prendas"].ToString(),
                            Estado = dr["Estado"].ToString(),
                            FechaRegistro = dr["FechaRegistro"].ToString(),
                            HoraRegistro = dr["HoraRegistro"].ToString(),
                            UsuarioCierre = dr["UsuarioCierre"].ToString(),
                            FechaCierre = dr["FechaCierre"].ToString(),
                            UsuarioApertura = dr["UsuarioApertura"].ToString(),
                            FechaApertura = dr["FechaApertura"].ToString()
                        });
                    }
                }
            }
            return lista;
        }

        /// <summary>
        /// Obtiene una fórmula completa (cabecera + colores/insumos + pruebas UDP) por su ID.
        /// </summary>
        public LIQ_FormulaBE ObtenerFormulaCompleta(int idFormula)
        {
            LIQ_FormulaBE entidad = null;
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_ObtenerFormulaCompleta", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdFormula", idFormula);
                    cnx.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        // 1. LEER CABECERA
                        if (dr.Read())
                        {
                            entidad = new LIQ_FormulaBE
                            {
                                IdFormula = Convert.ToInt32(dr["IdFormula"]),
                                IdRecetaOrigen = Convert.ToInt32(dr["IdRecetaOrigen"]),
                                NP = dr["NP"].ToString(),
                                Operario = dr["Operario"].ToString(),
                                Tecnica = dr["Tecnica"].ToString(),
                                Ubicacion = dr["Ubicacion"].ToString(),
                                Arte = dr["Arte"].ToString(),
                                FechaUDP = dr["FechaUDP"].ToString(),
                                Cliente = dr["Cliente"].ToString(),
                                Estilo = dr["Estilo"].ToString(),
                                EstiloPropio = dr["EstiloPropio"].ToString(),
                                ComboCabecera = dr["ComboCabecera"].ToString(),
                                PrendasReq = dr["PrendasReq"].ToString(),
                                Temporada = dr["Temporada"].ToString(),
                                Item = dr["Item"].ToString(),
                                UsuarioCierre = dr["UsuarioCierre"].ToString(),
                                FechaCierre = dr["FechaCierre"].ToString(),
                                UsuarioApertura = dr["UsuarioApertura"].ToString(),
                                FechaApertura = dr["FechaApertura"].ToString(),
                                FechaRegistro = dr["FechaRegistro"].ToString(),
                                HoraRegistro = dr["HoraRegistro"].ToString(),
                                Estado = dr["Estado"].ToString(),
                                Observaciones = "", // Se llenará en la siguiente consulta
                                Colores = new List<LIQ_FormulaColorBE>()
                            };
                        }

                        // 2. LEER DETALLE (COLORES + INSUMOS)
                        if (entidad != null && dr.NextResult())
                        {
                            while (dr.Read())
                            {
                                string nombreColor = dr["NombreColor"].ToString();
                                var color = entidad.Colores.Find(c => c.Nombre == nombreColor);
                                if (color == null)
                                {
                                    color = new LIQ_FormulaColorBE
                                    {
                                        IdColor = Convert.ToInt32(dr["IdColor"]),
                                        Nombre = nombreColor,
                                        Combo = dr["ComboColor"].ToString(),
                                        Insumos = new List<LIQ_FormulaInsumoBE>()
                                    };
                                    entidad.Colores.Add(color);
                                }

                                color.Insumos.Add(new LIQ_FormulaInsumoBE
                                {
                                    IdInsumo = Convert.ToInt32(dr["IdInsumo"]),
                                    CodigoInsumo = dr["CodigoInsumo"].ToString(),
                                    Descripcion = dr["InsumoDescripcion"].ToString(),
                                    Cantidad = Convert.ToDecimal(dr["CantidadInsumo"]),
                                    Pruebas = new List<LIQ_FormulaPruebaBE>()
                                });
                            }
                        }

                        // 3. LEER PRUEBAS UDP
                        if (entidad != null && dr.NextResult())
                        {
                            while (dr.Read())
                            {
                                string nomColor = dr["NombreColor"] != DBNull.Value ? dr["NombreColor"].ToString().Trim().ToUpper() : "";
                                string codInsumo = dr["CodigoInsumo"] != DBNull.Value ? dr["CodigoInsumo"].ToString().Trim().ToUpper() : "";

                                LIQ_FormulaInsumoBE insumoTarget = null;
                                foreach (var c in entidad.Colores)
                                {
                                    if (c.Nombre != null && c.Nombre.Trim().ToUpper() == nomColor)
                                    {
                                        foreach (var i in c.Insumos)
                                        {
                                            if (i.CodigoInsumo != null && i.CodigoInsumo.Trim().ToUpper() == codInsumo)
                                            {
                                                insumoTarget = i;
                                                break;
                                            }
                                        }
                                    }
                                    if (insumoTarget != null) break;
                                }

                                if (insumoTarget != null)
                                {
                                    if (insumoTarget.Pruebas == null) insumoTarget.Pruebas = new List<LIQ_FormulaPruebaBE>();
                                    insumoTarget.Pruebas.Add(new LIQ_FormulaPruebaBE
                                    {
                                        IdPrueba = dr["IdPrueba"] != DBNull.Value ? Convert.ToInt32(dr["IdPrueba"]) : 0,
                                        NombrePrueba = dr["NombrePrueba"] != DBNull.Value ? dr["NombrePrueba"].ToString().Trim() : "",
                                        GramosUDP = dr["GramosUDP"] != DBNull.Value ? Convert.ToDouble(dr["GramosUDP"]) : 0.0,
                                        EsPrincipal = dr["EsPrincipal"] != DBNull.Value ? Convert.ToBoolean(dr["EsPrincipal"]) : false
                                    });
                                }
                            }
                        }
                    }

                    // 4. OBTENER OBSERVACIONES (Workaround por trigger DDL que impide modificar el SP)
                    if (entidad != null)
                    {
                        using (SqlCommand cmdObs = new SqlCommand("SELECT ISNULL(Observaciones, '') FROM LIQ_Formulas WHERE IdFormula = @IdFormula", cnx))
                        {
                            cmdObs.CommandType = CommandType.Text;
                            cmdObs.Parameters.AddWithValue("@IdFormula", idFormula);
                            object obs = cmdObs.ExecuteScalar();
                            if (obs != null) entidad.Observaciones = obs.ToString();
                        }
                    }
                }
            }
            catch (Exception ex) { throw ex; }
            return entidad;
        }

        /// <summary>
        /// Crea una fórmula copiando una receta existente. Retorna el ID de la nueva fórmula.
        /// </summary>
        public int CrearFormulaDesdeReceta(int idRecetaOrigen, string usuario)
        {
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_CrearFormulaDesdeReceta", cnx);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 600;
                cmd.Parameters.AddWithValue("@IdRecetaOrigen", idRecetaOrigen);
                cmd.Parameters.AddWithValue("@UsuarioCreacion", usuario);

                cnx.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        return Convert.ToInt32(dr["IdFormula"]);
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// Obtiene una lista de NPs que tienen fórmula pero NO tienen recepción.
        /// </summary>
        public List<LIQ_NPSinRecepcionBE> ObtenerNPsSinRecepcion()
        {
            var lista = new List<LIQ_NPSinRecepcionBE>();
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                string sql = @"
                    SELECT F.NP, F.Item, C.NombreColor AS Cliente, F.Estilo 
                    FROM LIQ_Formulas F 
                    LEFT JOIN LIQ_FormulaColores C ON F.IdFormula = C.IdFormula AND C.NombreColor <> ''
                    WHERE F.Estado <> 'Eliminada' AND ISNULL(F.Eliminado, 0) = 0
                      AND NOT EXISTS (
                          SELECT 1 FROM LIQ_REQ_Recepciones R 
                          WHERE R.CodOrdPro = F.NP 
                             OR R.CodOrdPro = (F.NP + '-' + ISNULL(F.Item, ''))
                             OR (R.CodOrdPro = F.NP AND ISNULL(R.Item, '') = ISNULL(F.Item, ''))
                      )
                ";
                using (SqlCommand cmd = new SqlCommand(sql, cnx))
                {
                    cnx.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        var npSet = new HashSet<string>();
                        while (dr.Read())
                        {
                            string np = dr["NP"].ToString();
                            string itemStr = dr["Item"] != DBNull.Value ? dr["Item"].ToString() : "";
                            string npClave = string.IsNullOrEmpty(itemStr) || itemStr == "0000" ? np : np + "-" + itemStr;
                            
                            if (!npSet.Contains(npClave))
                            {
                                npSet.Add(npClave);
                                lista.Add(new LIQ_NPSinRecepcionBE
                                {
                                    NP = np,
                                    Item = itemStr,
                                    Cliente = dr["Cliente"]?.ToString(),
                                    Estilo = dr["Estilo"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            return lista;
        }

        /// <summary>
        /// Crea una recepción en blanco para habilitar la NP en Control Operativo.
        /// </summary>
        public string CrearRecepcionBlanco(string np, string usuario)
        {
            string resultado = "ERROR|No se obtuvo respuesta del servidor.";
            bool exito = false;

            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_CrearRecepcionBlanco", cnx))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@NP", np);
                    cmd.Parameters.AddWithValue("@Usuario", usuario);

                    cnx.Open();
                    int numReq = 0;
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            numReq = Convert.ToInt32(dr["NumRequerimiento"]);
                            if (numReq > 0)
                            {
                                resultado = "OK|" + dr["Mensaje"].ToString();
                                exito = true;
                            }
                            else
                            {
                                resultado = "ERROR|" + dr["Mensaje"].ToString();
                            }
                        }
                    }

                    if (exito && numReq > 0)
                    {
                        string itemStr = "0000";
                        try
                        {
                            string sqlGetItem = "SELECT TOP 1 ISNULL(Item, '0000') FROM LIQ_Formulas WHERE (CASE WHEN ISNULL(Item, '0000') = '0000' THEN NP ELSE NP + '-' + Item END) = @NP AND ISNULL(Eliminado, 0) = 0 ORDER BY IdFormula DESC";
                            using (SqlCommand cmdGetItem = new SqlCommand(sqlGetItem, cnx))
                            {
                                cmdGetItem.Parameters.AddWithValue("@NP", np);
                                object itemObj = cmdGetItem.ExecuteScalar();
                                if (itemObj != null) itemStr = itemObj.ToString();
                            }
                        }
                        catch { }

                        if (itemStr != "0000")
                        {
                            try
                            {
                                string sqlUpdateItem = @"
                                    IF COL_LENGTH('LIQ_REQ_Recepciones', 'Item') IS NOT NULL
                                    BEGIN
                                        UPDATE LIQ_REQ_Recepciones SET Item = @Item WHERE NumRequerimiento = @NumReq;
                                    END
                                ";
                                using (SqlCommand cmdItem = new SqlCommand(sqlUpdateItem, cnx))
                                {
                                    cmdItem.Parameters.AddWithValue("@Item", itemStr);
                                    cmdItem.Parameters.AddWithValue("@NumReq", numReq);
                                    cmdItem.ExecuteNonQuery();
                                }
                            }
                            catch { }
                        }
                    }
                }

                if (exito)
                {
                    string sqlSnapshot = "EXEC LIQ_SP_TomarSnapshotStockNP @NP;";
                    using (SqlCommand cmdSnap = new SqlCommand(sqlSnapshot, cnx))
                    {
                        cmdSnap.CommandType = CommandType.Text;
                        cmdSnap.Parameters.AddWithValue("@NP", np);
                        cmdSnap.ExecuteNonQuery();
                    }
                }
            }
            return resultado;
        }

        /// <summary>
        /// Actualiza una fórmula existente enviando la data como XML.
        /// </summary>
        public bool ActualizarFormula(LIQ_FormulaBE entidad)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(LIQ_FormulaBE));
                XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                namespaces.Add("", "");

                string xmlString;
                using (StringWriter sw = new StringWriter())
                {
                    using (XmlWriter writer = XmlWriter.Create(sw, new XmlWriterSettings { OmitXmlDeclaration = true }))
                    {
                        serializer.Serialize(writer, entidad, namespaces);
                        xmlString = sw.ToString();
                    }
                }

                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_ActualizarFormula", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@XML_DATA", xmlString);

                    cnx.Open();
                    object res = cmd.ExecuteScalar();
                    bool success = res != null && Convert.ToInt32(res) > 0;
                    
                    if (success)
                    {
                        using (SqlCommand cmdObs = new SqlCommand("UPDATE LIQ_Formulas SET Observaciones = @Obs WHERE IdFormula = @IdFormula", cnx))
                        {
                            cmdObs.CommandType = CommandType.Text;
                            cmdObs.Parameters.AddWithValue("@Obs", entidad.Observaciones ?? "");
                            cmdObs.Parameters.AddWithValue("@IdFormula", entidad.IdFormula);
                            cmdObs.ExecuteNonQuery();
                        }
                    }
                    
                    return success;
                }
            }
            catch (Exception ex) { throw ex; }
        }

        /// <summary>
        /// Eliminación lógica de una fórmula.
        /// </summary>
        public bool EliminarFormula(int idFormula, string usuario, string comentario)
        {
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_EliminarFormula", cnx);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdFormula", idFormula);
                cmd.Parameters.AddWithValue("@UsuarioElimina", usuario ?? "");
                cmd.Parameters.AddWithValue("@Comentario", comentario ?? "");

                cnx.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        return Convert.ToInt32(dr["Resultado"]) > 0;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Lista recetas disponibles (de AGR_Recetas) para seleccionar como base de nueva fórmula.
        /// Solo muestra recetas con NP de 5 dígitos que aún no tengan fórmula asignada.
        /// </summary>
        public List<LIQ_RecetaDisponibleBE> ListarRecetasParaFormula()
        {
            List<LIQ_RecetaDisponibleBE> lista = new List<LIQ_RecetaDisponibleBE>();
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_ListarRecetasParaFormula", cnx);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 600;

                cnx.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new LIQ_RecetaDisponibleBE
                        {
                            IdRecetas = Convert.ToInt32(dr["IdRecetas"]),
                            NP = dr["NP"].ToString(),
                            Cliente = dr["Cliente"].ToString(),
                            Temporada = dr["Temporada"].ToString(),
                            Estilo = dr["Estilo"].ToString(),
                            EstiloPropio = dr["EstiloPropio"].ToString(),
                            Item = dr["Item"].ToString(),
                            Combo = dr["Combo"].ToString(),
                            Ubicacion = dr["Ubicacion"].ToString(),
                            Tecnica = dr["Tecnica"].ToString(),
                            OperarioUDP = dr["OperarioUDP"].ToString(),
                            FechaRegistro = dr["FechaRegistro"].ToString()
                        });
                    }
                }
            }
            return lista;
        }

        /// <summary>
        /// Cambia el estado de una fórmula (Cerrar/Abrir).
        /// </summary>
        public bool CambiarEstadoFormula(int idFormula, string usuario, bool cerrar, string comentario)
        {
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_CambiarEstadoFormula", cnx);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdFormula", idFormula);
                cmd.Parameters.AddWithValue("@Usuario", usuario);
                cmd.Parameters.AddWithValue("@Accion", cerrar ? 1 : 0);
                cmd.Parameters.AddWithValue("@Comentario", comentario ?? "");

                cnx.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
        }

        // =======================================================================
        // METODOS OPERATIVOS (CONSUMOS, MERMAS, DEVOLUCIONES)
        // =======================================================================

        public bool RegistrarOperacion(LIQ_OperacionBE ope)
        {
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_RegistrarOperacion", cnx);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@NP", ope.NP ?? "");
                cmd.Parameters.AddWithValue("@CodInsumo", ope.CodInsumo);
                cmd.Parameters.AddWithValue("@TipoOperacion", ope.TipoOperacion);
                cmd.Parameters.AddWithValue("@Cantidad", ope.Cantidad);
                cmd.Parameters.AddWithValue("@Motivo", ope.Motivo ?? "");
                cmd.Parameters.AddWithValue("@MermaReutilizada", ope.MermaReutilizada ?? "");
                cmd.Parameters.AddWithValue("@Usuario", ope.Usuario ?? "");
                cmd.Parameters.AddWithValue("@FuenteConsumo", string.IsNullOrEmpty(ope.FuenteConsumo) ? "Stock Inicial" : ope.FuenteConsumo);
                cmd.Parameters.AddWithValue("@NombreColor", string.IsNullOrEmpty(ope.NombreColor) ? (object)DBNull.Value : ope.NombreColor);
                cmd.Parameters.AddWithValue("@IdVisita", ope.IdVisita.HasValue ? (object)ope.IdVisita.Value : DBNull.Value);

                cnx.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        return Convert.ToInt32(dr["Resultado"]) > 0;
                    }
                }
            }
            return false;
        }

        public List<LIQ_MenuNP_BE> ObtenerMenuNPs(string estado)
        {
            List<LIQ_MenuNP_BE> lista = new List<LIQ_MenuNP_BE>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("LIQ_SP_ObtenerMenuNPs", cnx))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Estado", estado);
                        
                        cnx.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                lista.Add(new LIQ_MenuNP_BE
                                {
                                    NP = dr["NP"].ToString(),
                                    Item = dr["Item"] != DBNull.Value ? dr["Item"].ToString() : "",
                                    Cliente = dr["Cliente"].ToString(),
                                    Estilo = dr["Estilo"].ToString(),
                                    Temporada = dr["Temporada"].ToString(),
                                    EstiloPropio = dr["EstiloPropio"].ToString(),
                                    Estado = dr["Estado"].ToString(),
                                    Creacion = dr["FechaCreacion"].ToString(),
                                    Cierre = dr["FechaCierre"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lista;
        }

        public List<LIQ_LiquidacionConsolidadaBE> ObtenerLiquidacionesConsolidadas(string estado, string npFiltro = null, string itemFiltro = null)
        {
            List<LIQ_LiquidacionConsolidadaBE> lista = new List<LIQ_LiquidacionConsolidadaBE>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    string sql = @"
    SET NOCOUNT ON;

    -- Resultado 1: Cabeceras
    SELECT 
        F.IdFormula, F.NP, F.Item, F.Cliente, F.Estilo, F.Temporada, F.EstiloPropio, F.Estado, 
        CONVERT(VARCHAR(10), F.FechaCreacion, 103) AS FechaCreacion,
        ISNULL(F.FechaCierre, '--') AS FechaCierre
    FROM LIQ_Formulas F
    WHERE F.Eliminado = 0 
      AND (
          (@Estado = 'Activa' AND F.Estado IN ('Activa', 'En Proceso', 'Pendiente', 'Liquidado')) OR
          (@Estado = 'Cerrada' AND F.Estado IN ('Terminado', 'Cerrada')) OR
          (@Estado NOT IN ('Activa', 'Cerrada') AND F.Estado = @Estado)
      )
      AND (@npFiltro IS NULL OR F.NP = @npFiltro)
      AND (@itemFiltro IS NULL OR ISNULL(F.Item, '0000') = @itemFiltro)
      AND EXISTS (
          SELECT 1 FROM LIQ_REQ_Recepciones R 
          WHERE R.CodOrdPro = F.NP OR R.CodOrdPro = (CASE WHEN ISNULL(F.Item, '0000') = '0000' THEN F.NP ELSE F.NP + '-' + F.Item END)
      );

    -- Resultado 2: Colores e Insumos con Saldos Consolidados
    ;WITH CTE_Base AS (
        SELECT 
            F.NP,
            ISNULL(F.Item, '0000') AS Item,
            C.NombreColor AS Pantone,
            I.CodigoInsumo,
            I.Descripcion AS NombreInsumo,
            F.Tecnica,
            'gr' AS UM,
            -- Requerido: Extraer el valor de LIQ_FormulaInsumosPrueba (EsPrincipal = 1) por Prendas
            ISNULL((
                SELECT TOP 1 GramosUDP 
                FROM LIQ_FormulaInsumosPrueba 
                WHERE IdFormula = F.IdFormula 
                  AND NombreColor = C.NombreColor 
                  AND CodigoInsumo = I.CodigoInsumo 
                  AND EsPrincipal = 1
            ), 0) * CASE WHEN ISNUMERIC(F.Prendas) = 1 THEN CAST(F.Prendas AS DECIMAL(18,2)) ELSE 1 END AS Requerido,
			        
            -- Stock Recibido Global
            ISNULL((
                SELECT SUM(D.CantidadRecibida * 1000.00) 
                FROM LIQ_REQ_Recepciones R
                INNER JOIN LIQ_REQ_RecepcionesDetalle D ON R.NumRequerimiento = D.NumRequerimiento
                WHERE R.CodOrdPro = F.NP AND D.CodInsumo = I.CodigoInsumo
            ), 0) AS StockRecibidoGlobal,

            -- Stock Recibido (Distribuido proporcionalmente por colores que usan el insumo)
            ISNULL((
                SELECT SUM(D.CantidadRecibida * 1000.00) 
                FROM LIQ_REQ_Recepciones R
                INNER JOIN LIQ_REQ_RecepcionesDetalle D ON R.NumRequerimiento = D.NumRequerimiento
                WHERE R.CodOrdPro = F.NP AND D.CodInsumo = I.CodigoInsumo
            ), 0) / ISNULL(NULLIF((SELECT COUNT(*) FROM LIQ_FormulaInsumos I3 INNER JOIN LIQ_FormulaColores C3 ON I3.IdFormulaColor = C3.IdFormulaColor WHERE C3.IdFormula = F.IdFormula AND I3.CodigoInsumo = I.CodigoInsumo), 0), 1) AS StockRecibido,
            
            -- Stock Operativo Global (Calculado como BaseInicial - Consumos, Ajustes y Devoluciones GLOBALES, priorizando la foto histórica si existe)
            COALESCE(
                (SELECT TOP 1 SS.StockOperativoInicial FROM LIQ_NP_StockSnapshot SS WHERE SS.NP = F.NP AND ISNULL(SS.Item, '0000') = ISNULL(F.Item, '0000') AND SS.CodInsumo = I.CodigoInsumo ORDER BY SS.FechaCaptura DESC),
                ISNULL((
                    SELECT TOP 1 CASE 
                        WHEN LOWER(LTRIM(RTRIM(ISNULL(UnidadMedida, 'gr')))) = 'kg' THEN ISNULL(StockActual, 0) * 1000.0
                        ELSE ISNULL(StockActual, 0)
                    END
                    FROM LIQ_STK_StockInsumos
                    WHERE CodInsumo = I.CodigoInsumo
                ), 0) -
                ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo' AND FuenteConsumo = 'Stock Inicial'), 0) -
                ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste' AND FuenteConsumo = 'Stock Inicial'), 0) -
                ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = I.CodigoInsumo AND (TipoOperacion = 'Devolucion Central' OR (TipoOperacion = 'Devolucion' AND Motivo LIKE '%Central%'))), 0) +
                ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = I.CodigoInsumo AND (TipoOperacion = 'Devolucion Operativo' OR (TipoOperacion = 'Devolucion' AND Motivo LIKE '%Operativo%'))), 0)
            ) AS StockOperativoGlobal,

            -- Stock Operativo (Distribuido proporcionalmente)
            COALESCE(
                (SELECT TOP 1 SS.StockOperativoInicial FROM LIQ_NP_StockSnapshot SS WHERE SS.NP = F.NP AND ISNULL(SS.Item, '0000') = ISNULL(F.Item, '0000') AND SS.CodInsumo = I.CodigoInsumo ORDER BY SS.FechaCaptura DESC),
                ISNULL((
                    SELECT TOP 1 CASE 
                        WHEN LOWER(LTRIM(RTRIM(ISNULL(UnidadMedida, 'gr')))) = 'kg' THEN ISNULL(StockActual, 0) * 1000.0
                        ELSE ISNULL(StockActual, 0)
                    END
                    FROM LIQ_STK_StockInsumos
                    WHERE CodInsumo = I.CodigoInsumo
                ), 0) -
                ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo' AND FuenteConsumo = 'Stock Inicial'), 0) -
                ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste' AND FuenteConsumo = 'Stock Inicial'), 0) -
                ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = I.CodigoInsumo AND (TipoOperacion = 'Devolucion Central' OR (TipoOperacion = 'Devolucion' AND Motivo LIKE '%Central%'))), 0) +
                ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = I.CodigoInsumo AND (TipoOperacion = 'Devolucion Operativo' OR (TipoOperacion = 'Devolucion' AND Motivo LIKE '%Operativo%'))), 0)
            ) / ISNULL(NULLIF((SELECT COUNT(*) FROM LIQ_FormulaInsumos I3 INNER JOIN LIQ_FormulaColores C3 ON I3.IdFormulaColor = C3.IdFormulaColor WHERE C3.IdFormula = F.IdFormula AND I3.CodigoInsumo = I.CodigoInsumo), 0), 1) AS StockOperativo,
			        
            ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP IN (F.NP, F.NP + '-' + F.Item) AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo' AND NombreColor = C.NombreColor), 0) AS Consumido,
            ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP IN (F.NP, F.NP + '-' + F.Item) AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Devolucion'), 0) AS Devuelto,
            ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP IN (F.NP, F.NP + '-' + F.Item) AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Devolucion' AND Motivo LIKE 'Almacén Central%'), 0) AS DevueltoCentral,
            ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP IN (F.NP, F.NP + '-' + F.Item) AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Devolucion' AND Motivo LIKE 'Almacén Operativo%'), 0) AS DevueltoOperativo,
            ISNULL((SELECT SUM(Gramos) FROM LIQ_MER_MermasColor WHERE NP IN (F.NP, F.NP + '-' + F.Item) AND NombreColor = C.NombreColor), 0) AS Merma,
            ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP IN (F.NP, F.NP + '-' + F.Item) AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste' AND NombreColor = C.NombreColor AND FuenteConsumo != 'Stock Merma'), 0) AS Ajuste,
            
            ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP IN (F.NP, F.NP + '-' + F.Item) AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo' AND FuenteConsumo = 'Stock Inicial' AND NombreColor = C.NombreColor), 0) AS ConsumidoInicial,
            ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP IN (F.NP, F.NP + '-' + F.Item) AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo' AND FuenteConsumo = 'Solicitud Realizada' AND NombreColor = C.NombreColor), 0) AS ConsumidoSolicitud,
            ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP IN (F.NP, F.NP + '-' + F.Item) AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste' AND FuenteConsumo = 'Solicitud Realizada' AND NombreColor = C.NombreColor), 0) AS AjusteSolicitud,
            
            ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP IN (F.NP, F.NP + '-' + F.Item) AND CodInsumo = I.CodigoInsumo AND TipoOperacion IN ('Consumo', 'Consumo Desarrollo') AND FuenteConsumo IN ('Stock Inicial', 'Stock Solicitado', 'Solicitud Realizada', 'Stock Recibido')), 0) AS ConsumidoTotalGlobal,
            ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP IN (F.NP, F.NP + '-' + F.Item) AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste' AND FuenteConsumo IN ('Stock Inicial', 'Stock Solicitado', 'Solicitud Realizada', 'Stock Recibido') AND FuenteConsumo != 'Stock Merma'), 0) AS AjusteTotalGlobal,
            ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP IN (F.NP, F.NP + '-' + F.Item) AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo' AND FuenteConsumo = 'Solicitud Realizada'), 0) AS ConsumidoSolicitudGlobal,
            ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP IN (F.NP, F.NP + '-' + F.Item) AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste' AND FuenteConsumo = 'Solicitud Realizada'), 0) AS AjusteSolicitudGlobal,

            ISNULL((SELECT TOP 1 Motivo FROM LIQ_OperacionesDetalle WHERE NP IN (F.NP, F.NP + '-' + F.Item) AND CodInsumo = I.CodigoInsumo ORDER BY IdOperacion DESC), 'Entrega Inicial') AS Trazabilidad,
            
            -- Bandera de Bloqueo Merma
            CAST(CASE WHEN EXISTS (
                SELECT 1 FROM LIQ_MER_MermasColor 
                WHERE NP IN (F.NP, F.NP + '-' + F.Item) AND NombreColor = C.NombreColor AND Gramos = 0 AND CodigoMerma = 'NO_MERMA'
            ) THEN 1 ELSE 0 END AS BIT) AS BloqueoMerma,
            
            -- Bandera de Bloqueo Ajuste
            CAST(CASE WHEN EXISTS (
                SELECT 1 FROM LIQ_OperacionesDetalle 
                WHERE NP IN (F.NP, F.NP + '-' + F.Item) AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste' AND Cantidad = 0 AND Motivo = 'No existe ajuste'
            ) THEN 1 ELSE 0 END AS BIT) AS BloqueoAjuste
            
        FROM LIQ_Formulas F
        INNER JOIN LIQ_FormulaColores C ON F.IdFormula = C.IdFormula
        INNER JOIN LIQ_FormulaInsumos I ON C.IdFormulaColor = I.IdFormulaColor
        WHERE F.Eliminado = 0 
          AND (
              (@Estado = 'Activa' AND F.Estado IN ('Activa', 'En Proceso', 'Pendiente', 'Liquidado')) OR
              (@Estado = 'Cerrada' AND F.Estado IN ('Terminado', 'Cerrada')) OR
              (@Estado NOT IN ('Activa', 'Cerrada') AND F.Estado = @Estado)
          )
          AND (@npFiltro IS NULL OR F.NP = @npFiltro)
          AND (@itemFiltro IS NULL OR ISNULL(F.Item, '0000') = @itemFiltro)
          AND EXISTS (
              SELECT 1 FROM LIQ_REQ_Recepciones R 
              WHERE R.CodOrdPro = F.NP OR R.CodOrdPro = (CASE WHEN ISNULL(F.Item, '0000') = '0000' THEN F.NP ELSE F.NP + '-' + F.Item END)
          )
    )
    SELECT 
        NP, Item, Pantone, CodigoInsumo, NombreInsumo, Tecnica, UM, Requerido,
        
        StockOperativo, 
        StockRecibido, 
        StockOperativoGlobal,
        StockRecibidoGlobal,
        (StockOperativo + StockRecibido) AS StockTotal,
        
        ConsumidoInicial,
        ConsumidoSolicitud,
        ConsumidoTotalGlobal AS ConsumidoTotal,
        ConsumidoSolicitudGlobal,
        AjusteSolicitudGlobal,
        
        (Ajuste - AjusteSolicitud) AS AjusteInicial,
        AjusteSolicitud,
        AjusteTotalGlobal AS AjusteTotal,
        
        (StockOperativo - ConsumidoInicial - (Ajuste - AjusteSolicitud)) AS SaldoInicial,
        (StockRecibido - ConsumidoSolicitud - AjusteSolicitud) AS SaldoSolicitud,
        ((StockOperativo - ConsumidoInicial - (Ajuste - AjusteSolicitud)) + (StockRecibido - ConsumidoSolicitud - AjusteSolicitud)) AS SaldoTotal,
        
        Consumido,
        Devuelto, DevueltoCentral, DevueltoOperativo,
        Merma,
        Ajuste,
        Trazabilidad, BloqueoMerma, BloqueoAjuste
    FROM CTE_Base;
                    ";

                    SqlCommand cmd = new SqlCommand(sql, cnx);
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@Estado", estado);
                    cmd.Parameters.AddWithValue("@npFiltro", string.IsNullOrEmpty(npFiltro) ? (object)DBNull.Value : npFiltro);
                    cmd.Parameters.AddWithValue("@itemFiltro", string.IsNullOrEmpty(itemFiltro) ? (object)DBNull.Value : itemFiltro);
                    cnx.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new LIQ_LiquidacionConsolidadaBE
                            {
                                NP = dr["NP"].ToString(),
                                Item = dr["Item"] != DBNull.Value ? dr["Item"].ToString() : "",
                                Cliente = dr["Cliente"].ToString(),
                                Estilo = dr["Estilo"].ToString(),
                                Temporada = dr["Temporada"].ToString(),
                                EstiloPropio = dr["EstiloPropio"].ToString(),
                                Estado = dr["Estado"].ToString(),
                                Creacion = dr["FechaCreacion"].ToString(),
                                Cierre = dr["FechaCierre"].ToString(),
                                Colores = new List<LIQ_LiquidacionColorBE>()
                            });
                        }

                        // 2. LEER DETALLE Y ASOCIAR
                        if (dr.NextResult())
                        {
                            while (dr.Read())
                            {
                                string np = dr["NP"].ToString();
                                string itemStr = dr["Item"] != DBNull.Value ? dr["Item"].ToString() : "";
                                string pantone = dr["Pantone"].ToString();

                                var cabecera = lista.Find(x => x.NP == np && (x.Item == itemStr || (string.IsNullOrEmpty(x.Item) && string.IsNullOrEmpty(itemStr))));
                                if (cabecera != null)
                                {
                                    var color = cabecera.Colores.Find(c => c.Pantone == pantone);
                                    if (color == null)
                                    {
                                        color = new LIQ_LiquidacionColorBE { 
                                            Pantone = pantone, 
                                            Insumos = new List<LIQ_LiquidacionInsumoBE>(),
                                            BloqueoMerma = Convert.ToBoolean(dr["BloqueoMerma"])
                                        };
                                        cabecera.Colores.Add(color);
                                    }

                                    decimal consumido = Convert.ToDecimal(dr["Consumido"]);
                                    decimal consumidoInicial = Convert.ToDecimal(dr["ConsumidoInicial"]);
                                    decimal consumidoSolicitud = Convert.ToDecimal(dr["ConsumidoSolicitud"]);
                                    decimal ajusteSolicitud = Convert.ToDecimal(dr["AjusteSolicitud"]);
                                    decimal devuelto = Convert.ToDecimal(dr["Devuelto"]);
                                    decimal devueltoCentral = Convert.ToDecimal(dr["DevueltoCentral"]);
                                    decimal devueltoOperativo = Convert.ToDecimal(dr["DevueltoOperativo"]);
                                    decimal merma = Convert.ToDecimal(dr["Merma"]);
                                    decimal ajuste = Convert.ToDecimal(dr["Ajuste"]);
                                    color.Insumos.Add(new LIQ_LiquidacionInsumoBE
                                    {
                                        Codigo = dr["CodigoInsumo"].ToString(),
                                        Nombre = dr["NombreInsumo"].ToString(),
                                        Tecnica = dr["Tecnica"].ToString(),
                                        UM = dr["UM"].ToString(),
                                        Requerido = Convert.ToDecimal(dr["Requerido"]),
                                        
                                        StockRecibido = Convert.ToDecimal(dr["StockRecibido"]),
                                        StockOperativo = Convert.ToDecimal(dr["StockOperativo"]),
                                        StockRecibidoGlobal = Convert.ToDecimal(dr["StockRecibidoGlobal"]),
                                        StockOperativoGlobal = Convert.ToDecimal(dr["StockOperativoGlobal"]),
                                        StockTotal = Convert.ToDecimal(dr["StockTotal"]),
                                        
                                        ConsumidoInicial = Convert.ToDecimal(dr["ConsumidoInicial"]),
                                        ConsumidoSolicitud = Convert.ToDecimal(dr["ConsumidoSolicitud"]),
                                        ConsumidoTotal = Convert.ToDecimal(dr["ConsumidoTotal"]),
                                        ConsumidoSolicitudGlobal = Convert.ToDecimal(dr["ConsumidoSolicitudGlobal"]),
                                        Consumido = consumido,
                                        
                                        AjusteInicial = Convert.ToDecimal(dr["AjusteInicial"]),
                                        AjusteSolicitud = Convert.ToDecimal(dr["AjusteSolicitud"]),
                                        AjusteTotal = Convert.ToDecimal(dr["AjusteTotal"]),
                                        AjusteSolicitudGlobal = Convert.ToDecimal(dr["AjusteSolicitudGlobal"]),
                                        Ajuste = ajuste,
                                        
                                        SaldoInicial = Convert.ToDecimal(dr["SaldoInicial"]),
                                        SaldoSolicitud = Convert.ToDecimal(dr["SaldoSolicitud"]),
                                        SaldoTotal = Convert.ToDecimal(dr["SaldoTotal"]),
                                        Saldo = Convert.ToDecimal(dr["SaldoTotal"]),
                                        
                                        Devuelto = Convert.ToDecimal(dr["Devuelto"]),
                                        DevueltoCentral = Convert.ToDecimal(dr["DevueltoCentral"]),
                                        DevueltoOperativo = Convert.ToDecimal(dr["DevueltoOperativo"]),
                                        Merma = Convert.ToDecimal(dr["Merma"]),
                                        LoteVenc = "", // Para implementar si se requiere de lote real
                                        Trazabilidad = dr["Trazabilidad"].ToString(),
                                        BloqueoAjuste = Convert.ToBoolean(dr["BloqueoAjuste"])
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { throw ex; }
            return lista;
        }

        public List<LIQ_MermaStockBE> ObtenerMermasStock()
        {
            List<LIQ_MermaStockBE> lista = new List<LIQ_MermaStockBE>();
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                string query = @"
                    SELECT 
                        M.CodigoMerma AS Codigo,
                        M.NombreColor AS Descripcion,
                        '' AS Tecnica,
                        M.NP AS NPOrigen,
                        CONVERT(VARCHAR(10), M.FechaRegistro, 103) AS FechaGeneracion,
                        CONVERT(VARCHAR(10), M.FechaVencimiento, 103) AS FechaVencimiento,
                        CAST(M.Gramos - ISNULL(
                            (SELECT SUM(Cantidad) 
                             FROM LIQ_OperacionesDetalle OD 
                             WHERE OD.MermaReutilizada = M.CodigoMerma 
                               AND (OD.TipoOperacion = 'Consumo' OR OD.TipoOperacion = 'Ajuste')
                            ), 0) AS DECIMAL(18,2)) AS CantidadDisponible,
                        'gr' AS UM,
                        'Disponible' AS Estado
                    FROM 
                        LIQ_MER_MermasColor M
                    WHERE 
                        M.FechaVencimiento IS NOT NULL 
                        AND M.Gramos > 0
                    ORDER BY 
                        M.FechaRegistro DESC;
                ";
                SqlCommand cmd = new SqlCommand(query, cnx);
                cmd.CommandType = CommandType.Text;
                cnx.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new LIQ_MermaStockBE
                        {
                            Codigo = dr["Codigo"].ToString(),
                            Descripcion = dr["Descripcion"].ToString(),
                            Tecnica = dr["Tecnica"].ToString(),
                            NPOrigen = dr["NPOrigen"].ToString(),
                            FechaGeneracion = dr["FechaGeneracion"].ToString(),
                            FechaVencimiento = dr["FechaVencimiento"].ToString(),
                            CantidadDisponible = Convert.ToDecimal(dr["CantidadDisponible"]),
                            UM = dr["UM"].ToString(),
                            Estado = dr["Estado"].ToString()
                        });
                    }
                }
            }
            return lista;
        }

        public LIQ_SaldosPopupBE ObtenerSaldosPopup(string np, string codInsumo, string nombreColor = null, int? idVisita = null)
        {
            LIQ_SaldosPopupBE saldos = new LIQ_SaldosPopupBE();
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                string sql = @"
    SET NOCOUNT ON;
    
    DECLARE @RealNP VARCHAR(100) = @NP;
    SELECT TOP 1 @RealNP = NP 
    FROM LIQ_Formulas 
    WHERE (NP + '-' + ISNULL(Item, '0000')) = @NP 
       OR NP = @NP;
    
    DECLARE @StockInicial DECIMAL(18,4) = 0;
    DECLARE @StockSolicitud DECIMAL(18,4) = 0;
    
    SELECT TOP 1 @StockInicial = 
        CASE 
            WHEN LOWER(LTRIM(RTRIM(ISNULL(UnidadMedida, 'gr')))) = 'kg' THEN ISNULL(StockActual, 0) * 1000.0
            ELSE ISNULL(StockActual, 0)
        END
    FROM LIQ_STK_StockInsumos
    WHERE CodInsumo = @CodInsumo;
    
    SELECT @StockSolicitud = ISNULL(SUM(D.CantidadRecibida * 1000.00), 0)
    FROM LIQ_REQ_RecepcionesDetalle D
    INNER JOIN LIQ_REQ_Recepciones R ON D.NumRequerimiento = R.NumRequerimiento
    WHERE R.CodOrdPro IN (@NP, @RealNP) AND D.CodInsumo = @CodInsumo;

    DECLARE @ConsumidoOperativoGlobal DECIMAL(18,4) = ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = @CodInsumo AND TipoOperacion IN ('Consumo', 'Consumo Desarrollo') AND FuenteConsumo = 'Stock Inicial'), 0);
    DECLARE @AjusteOperativoGlobal DECIMAL(18,4) = ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = @CodInsumo AND TipoOperacion = 'Ajuste' AND FuenteConsumo = 'Stock Inicial'), 0);
    DECLARE @DevueltoCentralGlobal DECIMAL(18,4) = ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = @CodInsumo AND (TipoOperacion = 'Devolucion Central' OR (TipoOperacion = 'Devolucion' AND Motivo LIKE '%Central%'))), 0);
    DECLARE @DevueltoOperativoGlobal DECIMAL(18,4) = ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = @CodInsumo AND (TipoOperacion = 'Devolucion Operativo' OR (TipoOperacion = 'Devolucion' AND Motivo LIKE '%Operativo%'))), 0);

    DECLARE @ConsumidoSolicitudNP DECIMAL(18,4) = ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP IN (@NP, @RealNP) AND CodInsumo = @CodInsumo AND TipoOperacion IN ('Consumo', 'Consumo Desarrollo') AND FuenteConsumo IN ('Stock Solicitado', 'Solicitud Realizada', 'Stock Recibido')), 0);
    DECLARE @AjusteSolicitudNP DECIMAL(18,4) = ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP IN (@NP, @RealNP) AND CodInsumo = @CodInsumo AND TipoOperacion = 'Ajuste' AND FuenteConsumo IN ('Stock Solicitado', 'Solicitud Realizada', 'Stock Recibido')), 0);

    DECLARE @SnapshotStock DECIMAL(18,4) = NULL;
    SELECT TOP 1 @SnapshotStock = StockOperativoInicial 
    FROM LIQ_NP_StockSnapshot 
    WHERE (CASE WHEN ISNULL(Item, '0000') = '0000' THEN NP ELSE NP + '-' + Item END) IN (@NP, @RealNP) AND CodInsumo = @CodInsumo 
    ORDER BY FechaCaptura DESC;
    
    DECLARE @StockOperativoBase DECIMAL(18,4);
    IF @SnapshotStock IS NOT NULL
    BEGIN
        SET @StockOperativoBase = @SnapshotStock;
    END
    ELSE
    BEGIN
        SET @StockOperativoBase = @StockInicial - @ConsumidoOperativoGlobal - @AjusteOperativoGlobal - @DevueltoCentralGlobal + @DevueltoOperativoGlobal;
    END

    DECLARE @ConsumidoInicialNP DECIMAL(18,4) = ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP IN (@NP, @RealNP) AND CodInsumo = @CodInsumo AND TipoOperacion IN ('Consumo', 'Consumo Desarrollo') AND FuenteConsumo = 'Stock Inicial'), 0);
    DECLARE @AjusteInicialNP DECIMAL(18,4) = ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE NP IN (@NP, @RealNP) AND CodInsumo = @CodInsumo AND TipoOperacion = 'Ajuste' AND FuenteConsumo = 'Stock Inicial'), 0);

    DECLARE @StockOperativoReal DECIMAL(18,4) = @StockOperativoBase - @ConsumidoInicialNP - @AjusteInicialNP;

    DECLARE @StockSolicitudReal DECIMAL(18,4) = @StockSolicitud - @ConsumidoSolicitudNP - @AjusteSolicitudNP;

    DECLARE @StockGlobalReal DECIMAL(18,4) = @StockInicial + @StockSolicitud - 
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = @CodInsumo AND TipoOperacion IN ('Consumo', 'Consumo Desarrollo') AND FuenteConsumo IN ('Stock Inicial', 'Stock Solicitado', 'Solicitud Realizada', 'Stock Recibido')), 0) - 
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = @CodInsumo AND TipoOperacion = 'Ajuste' AND FuenteConsumo IN ('Stock Inicial', 'Stock Solicitado', 'Solicitud Realizada', 'Stock Recibido')), 0) - 
        ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE CodInsumo = @CodInsumo AND (TipoOperacion = 'Devolucion Central' OR (TipoOperacion = 'Devolucion' AND Motivo LIKE '%Central%'))), 0);

    SELECT 
        @StockOperativoReal AS StockInicial,
        @StockSolicitudReal AS StockSolicitud,
        (@StockOperativoReal + @StockSolicitudReal) AS StockTotal,
        @StockGlobalReal AS StockGlobal;
        
    SELECT 
        IdOperacion,
        FechaRegistro,
        UsuarioRegistro,
        CASE 
            WHEN ISNULL(FuenteConsumo, '') = 'Stock Merma' AND ISNULL(MermaReutilizada, '') != '' 
            THEN 'Stock Merma (' + MermaReutilizada + ')'
            ELSE ISNULL(FuenteConsumo, 'Sin especificar')
        END AS FuenteConsumo,
        Cantidad
    FROM LIQ_OperacionesDetalle
    WHERE CodInsumo = @CodInsumo AND TipoOperacion IN ('Consumo', 'Consumo Desarrollo')
      AND (@NombreColor IS NULL OR NombreColor = @NombreColor)
      AND (
            (@IdVisita IS NOT NULL AND IdVisita = @IdVisita)
            OR 
            (@IdVisita IS NULL AND NP IN (@NP, @RealNP))
          )
    ORDER BY FechaRegistro DESC;
                ";
                
                SqlCommand cmd = new SqlCommand(sql, cnx);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@NP", np ?? "");
                cmd.Parameters.AddWithValue("@CodInsumo", codInsumo);
                cmd.Parameters.AddWithValue("@NombreColor", string.IsNullOrEmpty(nombreColor) ? (object)DBNull.Value : nombreColor);
                cmd.Parameters.AddWithValue("@IdVisita", idVisita.HasValue ? (object)idVisita.Value : DBNull.Value);
                cnx.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        saldos.StockInicial = Convert.ToDecimal(dr["StockInicial"]);
                        saldos.StockSolicitud = Convert.ToDecimal(dr["StockSolicitud"]);
                        saldos.StockTotal = Convert.ToDecimal(dr["StockTotal"]);
                        saldos.StockGlobal = Convert.ToDecimal(dr["StockGlobal"]);
                    }
                    
                    if (dr.NextResult())
                    {
                        while (dr.Read())
                        {
                            saldos.HistorialConsumo.Add(new LIQ_OperacionDetalleBE
                            {
                                IdOperacion = Convert.ToInt32(dr["IdOperacion"]),
                                Fecha = Convert.ToDateTime(dr["FechaRegistro"]).ToString("dd/MM/yyyy HH:mm"),
                                Usuario = dr["UsuarioRegistro"].ToString(),
                                Fuente = dr["FuenteConsumo"] != DBNull.Value ? dr["FuenteConsumo"].ToString() : "N/A",
                                Cantidad = Convert.ToDecimal(dr["Cantidad"])
                            });
                        }
                    }
                }
            }
            return saldos;
        }

        /// <summary>
        /// Soft-Delete: Cambia el TipoOperacion a 'Consumo Anulado' para restaurar stock sin borrar el registro.
        /// </summary>
        public bool AnularOperacion(int idOperacion, string motivo, string usuario)
        {
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                string sql = @"
                    UPDATE LIQ_OperacionesDetalle 
                    SET TipoOperacion = 'Consumo Anulado',
                        Motivo = 'ANULADO: ' + @Motivo + ' | Por: ' + @Usuario + ' | Fecha: ' + CONVERT(VARCHAR(20), GETDATE(), 120)
                    WHERE IdOperacion = @IdOperacion
                      AND TipoOperacion IN ('Consumo', 'Consumo Desarrollo');
                ";
                SqlCommand cmd = new SqlCommand(sql, cnx);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@IdOperacion", idOperacion);
                cmd.Parameters.AddWithValue("@Motivo", motivo ?? "");
                cmd.Parameters.AddWithValue("@Usuario", usuario ?? "");
                cnx.Open();
                int rows = cmd.ExecuteNonQuery();
                return rows > 0;
            }
        }

        public List<LIQ_NPPendienteBE> ObtenerNPsPendientes()
        {
            List<LIQ_NPPendienteBE> lista = new List<LIQ_NPPendienteBE>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_ObtenerNPsPendientes", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cnx.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new LIQ_NPPendienteBE
                            {
                                NP = dr["NP"].ToString(),
                                Cliente = dr["Cliente"].ToString(),
                                Temporada = dr["Temporada"].ToString(),
                                Estilo = dr["Estilo"].ToString(),
                                EstiloPropio = dr["EstiloPropio"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { throw ex; }
            return lista;
        }

        public bool RegistrarMermaColor(LIQ_MermaColorRegistroBE merma, string usuario)
        {
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                if (merma.Gramos <= 0)
                {
                    string query = @"
                        -- Auto-sanear registros anteriores erróneos
                        UPDATE LIQ_MER_MermasColor 
                        SET CodigoMerma = 'NO_MERMA' 
                        WHERE Gramos <= 0 AND CodigoMerma <> 'NO_MERMA';

                        DECLARE @IdFormulaColor INT = NULL;
                        SELECT TOP 1 @IdFormulaColor = C.IdFormulaColor 
                        FROM LIQ_FormulaColores C
                        INNER JOIN LIQ_Formulas F ON C.IdFormula = F.IdFormula
                        WHERE F.NP = @NP AND C.NombreColor = @NombreColor;

                        IF NOT EXISTS (SELECT 1 FROM LIQ_MER_MermasColor WHERE NP = @NP AND NombreColor = @NombreColor)
                        BEGIN
                            INSERT INTO LIQ_MER_MermasColor (IdFormulaColor, NP, NombreColor, Gramos, CodigoMerma, FechaRegistro, UsuarioRegistro)
                            VALUES (@IdFormulaColor, @NP, @NombreColor, 0, 'NO_MERMA', GETDATE(), @Usuario);
                        END
                    ";
                    SqlCommand cmd = new SqlCommand(query, cnx);
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@NP", merma.NP);
                    cmd.Parameters.AddWithValue("@NombreColor", merma.NombreColor);
                    cmd.Parameters.AddWithValue("@Usuario", usuario);
                    cnx.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
                else
                {
                    SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_RegistrarMermaColor", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@NP", merma.NP);
                    cmd.Parameters.AddWithValue("@NombreColor", merma.NombreColor);
                    cmd.Parameters.AddWithValue("@Gramos", merma.Gramos);
                    
                    if (string.IsNullOrEmpty(merma.FechaVencimiento))
                        cmd.Parameters.AddWithValue("@FechaVencimiento", DBNull.Value);
                    else
                    {
                        DateTime fechaParsed;
                        if (DateTime.TryParse(merma.FechaVencimiento, out fechaParsed))
                            cmd.Parameters.AddWithValue("@FechaVencimiento", fechaParsed);
                        else
                            cmd.Parameters.AddWithValue("@FechaVencimiento", DBNull.Value);
                    }

                    cmd.Parameters.AddWithValue("@Usuario", usuario);

                    cnx.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            return Convert.ToInt32(dr["Resultado"]) > 0;
                        }
                    }
                }
            }
            return false;
        }

        public List<LIQ_HistorialUsoMermaBE> ObtenerHistorialUsoMerma(string codigoMerma)
        {
            List<LIQ_HistorialUsoMermaBE> lista = new List<LIQ_HistorialUsoMermaBE>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    string query = @"
                        SELECT 
                            FechaRegistro, 
                            UsuarioRegistro, 
                            FuenteConsumo, 
                            NP,
                            Cantidad, 
                            Motivo,
                            TipoOperacion
                        FROM LIQ_OperacionesDetalle 
                        WHERE MermaReutilizada = @CodigoMerma 
                          AND (TipoOperacion = 'Consumo' OR TipoOperacion = 'Ajuste')
                        ORDER BY FechaRegistro DESC";
                    
                    SqlCommand cmd = new SqlCommand(query, cnx);
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@CodigoMerma", codigoMerma);
                    cnx.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new LIQ_HistorialUsoMermaBE
                            {
                                Fecha = dr["FechaRegistro"] != DBNull.Value ? Convert.ToDateTime(dr["FechaRegistro"]).ToString("dd/MM/yyyy HH:mm") : "",
                                Usuario = dr["UsuarioRegistro"].ToString(),
                                Fuente = dr["FuenteConsumo"].ToString(),
                                NP = dr["NP"].ToString(),
                                Cantidad = Convert.ToDecimal(dr["Cantidad"]),
                                Motivo = dr["Motivo"].ToString(),
                                TipoOperacion = dr["TipoOperacion"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { throw ex; }
            return lista;
        }

        public List<LIQ_MermaHistoricoBE> ObtenerMermasPorColor(string np, string nombreColor)
        {
            List<LIQ_MermaHistoricoBE> lista = new List<LIQ_MermaHistoricoBE>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("dbo.LIQ_MER_SP_ObtenerMermasPorColor", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@NP", np);
                    cmd.Parameters.AddWithValue("@NombreColor", nombreColor);
                    cnx.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new LIQ_MermaHistoricoBE
                            {
                                IdMermaColor = Convert.ToInt32(dr["IdMermaColor"]),
                                CodigoMerma = dr["CodigoMerma"].ToString(),
                                Cantidad = Convert.ToDecimal(dr["Cantidad"]),
                                FechaVencimiento = dr["FechaVencimiento"] != DBNull.Value ? Convert.ToDateTime(dr["FechaVencimiento"]).ToString("dd/MM/yyyy") : "",
                                FechaRegistro = dr["FechaRegistro"] != DBNull.Value ? Convert.ToDateTime(dr["FechaRegistro"]).ToString("dd/MM/yyyy HH:mm") : "",
                                UsuarioRegistro = dr["UsuarioRegistro"].ToString(),
                                EstadoIndicador = dr["EstadoIndicador"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { throw ex; }
            return lista;
        }

        public List<LIQ_AjusteHistoricoBE> ObtenerAjustesPorInsumo(string np, string codInsumo)
        {
            List<LIQ_AjusteHistoricoBE> lista = new List<LIQ_AjusteHistoricoBE>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    string sql = @"
                        DECLARE @RealNP VARCHAR(100) = @NP;
                        IF CHARINDEX('-', @NP) > 0 AND @NP <> 'STOCK-DIR'
                            SET @RealNP = LEFT(@NP, CHARINDEX('-', @NP) - 1);

                        SELECT 
                            IdOperacion AS IdAjuste,
                            Cantidad,
                            Motivo,
                            CASE 
                                WHEN ISNULL(FuenteConsumo, '') = 'Stock Merma' AND ISNULL(MermaReutilizada, '') != '' 
                                THEN 'Stock Merma (' + MermaReutilizada + ')'
                                ELSE ISNULL(FuenteConsumo, 'Sin especificar')
                            END AS Fuente,
                            FechaRegistro,
                            UsuarioRegistro
                        FROM 
                            LIQ_OperacionesDetalle
                        WHERE 
                            TipoOperacion = 'Ajuste'
                            AND NP IN (@NP, @RealNP) 
                            AND CodInsumo = @CodInsumo
                        ORDER BY 
                            FechaRegistro DESC;";
                    SqlCommand cmd = new SqlCommand(sql, cnx);
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@NP", np);
                    cmd.Parameters.AddWithValue("@CodInsumo", codInsumo);
                    cnx.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new LIQ_AjusteHistoricoBE
                            {
                                IdAjuste = Convert.ToInt32(dr["IdAjuste"]),
                                Cantidad = Convert.ToDecimal(dr["Cantidad"]),
                                Motivo = dr["Motivo"].ToString(),
                                Fuente = dr["Fuente"].ToString(),
                                FechaRegistro = dr["FechaRegistro"] != DBNull.Value ? Convert.ToDateTime(dr["FechaRegistro"]).ToString("dd/MM/yyyy HH:mm") : "",
                                UsuarioRegistro = dr["UsuarioRegistro"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { throw ex; }
            return lista;
        }

        public List<LIQ_AjusteHistoricoBE> ObtenerDevolucionesPorInsumo(string np, string codInsumo)
        {
            List<LIQ_AjusteHistoricoBE> lista = new List<LIQ_AjusteHistoricoBE>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_ObtenerDevolucionesPorInsumo", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@NP", np);
                    cmd.Parameters.AddWithValue("@CodInsumo", codInsumo);
                    cnx.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new LIQ_AjusteHistoricoBE
                            {
                                IdAjuste = Convert.ToInt32(dr["IdAjuste"]),
                                Cantidad = Convert.ToDecimal(dr["Cantidad"]),
                                Motivo = dr["Motivo"].ToString(),
                                FechaRegistro = dr["FechaRegistro"] != DBNull.Value ? Convert.ToDateTime(dr["FechaRegistro"]).ToString("dd/MM/yyyy HH:mm") : "",
                                UsuarioRegistro = dr["UsuarioRegistro"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { throw ex; }
            return lista;
        }

        // =======================================================================
        // MAQUINA DE ESTADOS
        // =======================================================================

        public LIQ_TransicionResultadoBE AvanzarEstadoNP(string np, string nuevoEstado, string usuario)
        {
            var resultado = new LIQ_TransicionResultadoBE();
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_AvanzarEstadoNP", cnx);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@NP", np);
                cmd.Parameters.AddWithValue("@NuevoEstado", nuevoEstado);
                cmd.Parameters.AddWithValue("@Usuario", usuario);

                SqlParameter pResultado = new SqlParameter("@Resultado", SqlDbType.Int) { Direction = ParameterDirection.Output };
                SqlParameter pMensaje = new SqlParameter("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(pResultado);
                cmd.Parameters.Add(pMensaje);

                cnx.Open();
                cmd.ExecuteNonQuery();

                resultado.Exito = Convert.ToInt32(pResultado.Value) == 1;
                resultado.Mensaje = pMensaje.Value.ToString();
            }
            return resultado;
        }

        public void RegistrarNoMermaGlobal(string np, string usuario)
        {
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_RegistrarNoMermaGlobal", cnx);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@NP", np);
                cmd.Parameters.AddWithValue("@Usuario", usuario);
                cnx.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void RegistrarNoAjusteGlobal(string np, string usuario)
        {
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_RegistrarNoAjusteGlobal", cnx);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@NP", np);
                cmd.Parameters.AddWithValue("@Usuario", usuario);
                cnx.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public LIQ_TransicionResultadoBE TerminarNP(string np, string destinoGlobal, string usuario)
        {
            var resultado = new LIQ_TransicionResultadoBE();
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_TerminarNP_Fase5", cnx);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@NP", np);
                cmd.Parameters.AddWithValue("@DestinoGlobal", destinoGlobal);
                cmd.Parameters.AddWithValue("@Usuario", usuario);

                cnx.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        resultado.Exito = Convert.ToInt32(dr["Exito"]) == 1;
                        resultado.Mensaje = dr["Mensaje"].ToString();
                    }
                }
            }
            return resultado;
        }

        public LIQ_TransicionResultadoBE GenerarSiguienteVersionNP(string npOriginal, string usuario, string observacion)
        {
            var resultado = new LIQ_TransicionResultadoBE();
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_GenerarSiguienteVersionNP", cnx);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@NPOriginal", npOriginal);
                cmd.Parameters.AddWithValue("@Usuario", usuario);
                cmd.Parameters.AddWithValue("@Observacion", observacion ?? "");

                SqlParameter pNuevaNP = new SqlParameter("@NuevaNPGerada", SqlDbType.VarChar, 100) { Direction = ParameterDirection.Output };
                SqlParameter pExito = new SqlParameter("@Exito", SqlDbType.Int) { Direction = ParameterDirection.Output };
                SqlParameter pMensaje = new SqlParameter("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
                
                cmd.Parameters.Add(pNuevaNP);
                cmd.Parameters.Add(pExito);
                cmd.Parameters.Add(pMensaje);

                cnx.Open();
                cmd.ExecuteNonQuery();

                resultado.Exito = Convert.ToInt32(pExito.Value) == 1;
                // Devolvemos el mensaje, si fue exito podemos concatenar la nueva NP, o el controlador puede sacarla del mensaje
                resultado.Mensaje = pMensaje.Value.ToString();
                
                if (resultado.Exito)
                {
                    resultado.Mensaje = pNuevaNP.Value.ToString() + "|" + resultado.Mensaje;
                }
            }
            return resultado;
        }

        public List<LIQ_AuditoriaDevolucionBE> ObtenerAuditoriaDevolucionesCentral(string np)
        {
            List<LIQ_AuditoriaDevolucionBE> lista = new List<LIQ_AuditoriaDevolucionBE>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_AuditoriaDevolucionCentral", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@NP", np);
                    cnx.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new LIQ_AuditoriaDevolucionBE
                            {
                                CodigoInsumo = dr["CodigoInsumo"].ToString(),
                                Descripcion = dr["Descripcion"].ToString(),
                                Cantidad = Convert.ToDecimal(dr["Cantidad"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { throw ex; }
            return lista;
        }

        public string PredecirSiguienteVersionNP(string npOriginal)
        {
            string predictedDisplay = "";
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    string sql = @"
                        DECLARE @IdFormulaOriginal INT;
                        DECLARE @BaseNP VARCHAR(100);
                        DECLARE @BaseItem VARCHAR(100);
                        DECLARE @CurrentMaxVersion INT = 1;

                        SELECT TOP 1 
                            @IdFormulaOriginal = IdFormula,
                            @BaseNP = NP,
                            @BaseItem = ISNULL(Item, '')
                        FROM LIQ_Formulas 
                        WHERE (NP = @NPOriginal OR (CASE WHEN ISNULL(Item, '0000') = '0000' THEN NP ELSE NP + '-' + Item END) = @NPOriginal) 
                          AND ISNULL(Eliminado, 0) = 0
                        ORDER BY IdFormula DESC;

                        IF @IdFormulaOriginal IS NOT NULL
                        BEGIN
                            IF CHARINDEX('-V', @BaseNP) > 0
                            BEGIN
                                SET @BaseNP = SUBSTRING(@BaseNP, 1, CHARINDEX('-V', @BaseNP) - 1);
                            END

                            SELECT @CurrentMaxVersion = ISNULL(MAX(
                                CAST(SUBSTRING(NP, CHARINDEX('-V', NP) + 2, LEN(NP)) AS INT)
                            ), 1)
                            FROM LIQ_Formulas
                            WHERE NP LIKE @BaseNP + '-V%' 
                              AND ISNULL(Item, '') = @BaseItem
                              AND ISNUMERIC(SUBSTRING(NP, CHARINDEX('-V', NP) + 2, LEN(NP))) = 1;

                            IF CHARINDEX('-V', @BaseNP) = 0 AND @CurrentMaxVersion = 1
                            BEGIN
                                SET @CurrentMaxVersion = 1; 
                            END

                            DECLARE @NextVersion INT = @CurrentMaxVersion + 1;
                            DECLARE @NuevaNP VARCHAR(100) = @BaseNP + '-V' + CAST(@NextVersion AS VARCHAR(10));
                            
                            IF @BaseItem = '' OR @BaseItem = '0000'
                                SELECT @NuevaNP AS Prediction;
                            ELSE
                                SELECT @NuevaNP + '-' + @BaseItem AS Prediction;
                        END
                        ELSE
                        BEGIN
                            SELECT @NPOriginal + '-VX' AS Prediction;
                        END
                    ";
                    
                    SqlCommand cmd = new SqlCommand(sql, cnx);
                    cmd.Parameters.AddWithValue("@NPOriginal", npOriginal);
                    cnx.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        predictedDisplay = result.ToString();
                    }
                    else
                    {
                        predictedDisplay = npOriginal + "-VX";
                    }
                }
            }
            catch
            {
                predictedDisplay = npOriginal + "-VX";
            }
            return predictedDisplay;
        }

        public List<LIQ_AuditoriaAnulacionBE> ListarAuditoriaAnulaciones()
        {
            List<LIQ_AuditoriaAnulacionBE> lista = new List<LIQ_AuditoriaAnulacionBE>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    string sql = @"
                        SELECT 
                            OD.IdOperacion,
                            OD.NP,
                            OD.CodInsumo,
                            ISNULL(I.Descripcion, 'N/A') AS DescripcionInsumo,
                            OD.NombreColor,
                            OD.IdVisita,
                            OD.Cantidad AS CantidadAnulada,
                            OD.FuenteConsumo,
                            OD.UsuarioRegistro AS UsuarioAnulacion,
                            OD.FechaRegistro AS FechaAnulacion,
                            CASE 
                                WHEN OD.Motivo LIKE 'ANULADO: % | Por:%' 
                                THEN SUBSTRING(OD.Motivo, 10, CHARINDEX(' | Por:', OD.Motivo) - 10)
                                ELSE OD.Motivo 
                            END AS MotivoAnulacion,
                            OD.Motivo AS TrazaOriginalSistema
                        FROM 
                            LIQ_OperacionesDetalle OD
                        LEFT JOIN 
                            LIQ_STK_StockInsumos I ON OD.CodInsumo = I.CodInsumo
                        WHERE 
                            OD.TipoOperacion = 'Consumo Anulado'
                        ORDER BY OD.FechaRegistro DESC";
                        
                    SqlCommand cmd = new SqlCommand(sql, cnx);
                    cmd.CommandType = CommandType.Text;
                    cnx.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new LIQ_AuditoriaAnulacionBE
                            {
                                IdOperacion = Convert.ToInt32(dr["IdOperacion"]),
                                NP = dr["NP"].ToString(),
                                CodInsumo = dr["CodInsumo"].ToString(),
                                DescripcionInsumo = dr["DescripcionInsumo"].ToString(),
                                NombreColor = dr["NombreColor"] != DBNull.Value ? dr["NombreColor"].ToString() : "",
                                IdVisita = dr["IdVisita"] != DBNull.Value ? Convert.ToInt32(dr["IdVisita"]) : (int?)null,
                                CantidadAnulada = Convert.ToDecimal(dr["CantidadAnulada"]),
                                FuenteConsumo = dr["FuenteConsumo"].ToString(),
                                UsuarioAnulacion = dr["UsuarioAnulacion"].ToString(),
                                FechaAnulacion = Convert.ToDateTime(dr["FechaAnulacion"]),
                                MotivoAnulacion = dr["MotivoAnulacion"].ToString(),
                                TrazaOriginalSistema = dr["TrazaOriginalSistema"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { throw ex; }
            return lista;
        }
    }
}
