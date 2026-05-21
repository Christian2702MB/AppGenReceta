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
                    return res != null && Convert.ToInt32(res) > 0;
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
    }
}
