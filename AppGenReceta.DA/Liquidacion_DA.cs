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

        // =======================================================================
        // METODOS OPERATIVOS (CONSUMOS, MERMAS, DEVOLUCIONES)
        // =======================================================================

        public bool RegistrarOperacion(LIQ_OperacionBE ope)
        {
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_RegistrarOperacion", cnx);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@NP", ope.NP);
                cmd.Parameters.AddWithValue("@CodInsumo", ope.CodInsumo);
                cmd.Parameters.AddWithValue("@TipoOperacion", ope.TipoOperacion);
                cmd.Parameters.AddWithValue("@Cantidad", ope.Cantidad);
                cmd.Parameters.AddWithValue("@Motivo", ope.Motivo ?? "");
                cmd.Parameters.AddWithValue("@MermaReutilizada", ope.MermaReutilizada ?? "");
                cmd.Parameters.AddWithValue("@Usuario", ope.Usuario ?? "");
                cmd.Parameters.AddWithValue("@FuenteConsumo", string.IsNullOrEmpty(ope.FuenteConsumo) ? "Stock Inicial" : ope.FuenteConsumo);
                cmd.Parameters.AddWithValue("@NombreColor", string.IsNullOrEmpty(ope.NombreColor) ? (object)DBNull.Value : ope.NombreColor);

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

        public List<LIQ_LiquidacionConsolidadaBE> ObtenerLiquidacionesConsolidadas(string estado)
        {
            List<LIQ_LiquidacionConsolidadaBE> lista = new List<LIQ_LiquidacionConsolidadaBE>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_ObtenerLiquidacionesConsolidadas", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Estado", estado);
                    cnx.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        // 1. LEER CABECERAS
                        while (dr.Read())
                        {
                            lista.Add(new LIQ_LiquidacionConsolidadaBE
                            {
                                NP = dr["NP"].ToString(),
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
                                string pantone = dr["Pantone"].ToString();

                                var cabecera = lista.Find(x => x.NP == np);
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
                                    decimal devuelto = Convert.ToDecimal(dr["Devuelto"]);
                                    decimal merma = Convert.ToDecimal(dr["Merma"]);
                                    decimal ajuste = Convert.ToDecimal(dr["Ajuste"]);
                                    decimal requerido = Convert.ToDecimal(dr["Requerido"]);
                                    decimal saldo = requerido - consumido - devuelto - merma - ajuste;
                                    if (saldo < 0) saldo = 0;

                                    color.Insumos.Add(new LIQ_LiquidacionInsumoBE
                                    {
                                        Codigo = dr["CodigoInsumo"].ToString(),
                                        Nombre = dr["NombreInsumo"].ToString(),
                                        Tecnica = dr["Tecnica"].ToString(),
                                        UM = dr["UM"].ToString(),
                                        Requerido = requerido,
                                        Consumido = consumido,
                                        ConsumidoInicial = consumidoInicial,
                                        ConsumidoSolicitud = consumidoSolicitud,
                                        Devuelto = devuelto,
                                        Merma = merma,
                                        Ajuste = ajuste,
                                        Saldo = saldo,
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
                SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_ObtenerMermasStock", cnx);
                cmd.CommandType = CommandType.StoredProcedure;
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

        public LIQ_SaldosPopupBE ObtenerSaldosPopup(string np, string codInsumo)
        {
            LIQ_SaldosPopupBE saldos = new LIQ_SaldosPopupBE();
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_ObtenerSaldosPopup", cnx);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@NP", np);
                cmd.Parameters.AddWithValue("@CodInsumo", codInsumo);
                cnx.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        saldos.StockInicial = Convert.ToDecimal(dr["StockInicial"]);
                        saldos.StockSolicitud = Convert.ToDecimal(dr["StockSolicitud"]);
                        saldos.StockTotal = Convert.ToDecimal(dr["StockTotal"]);
                    }
                    
                    if (dr.NextResult())
                    {
                        while (dr.Read())
                        {
                            saldos.HistorialConsumo.Add(new LIQ_OperacionDetalleBE
                            {
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
            return false;
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
                    SqlCommand cmd = new SqlCommand("dbo.LIQ_SP_ObtenerAjustesPorInsumo", cnx);
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
    }
}
