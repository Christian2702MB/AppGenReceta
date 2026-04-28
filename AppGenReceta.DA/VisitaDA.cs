using AppGenReceta.BE;
//Agregado Sharepoint
//using Microsoft.SharePoint.Client;
//using Microsoft.SqlServer.Server;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
//Agregado diciembre 2025
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
//using System.Net;
//using System.Runtime.InteropServices;
using System.Security;
using System.Text;
//using static System.Net.Mime.MediaTypeNames;
//Paralistar la impresora
using System.Xml.Serialization;
using System.Xml;

namespace AppGenReceta.DA
{
    public class VisitaDA
    {
        private String ConnectionString = ConfigurationManager.ConnectionStrings["AppGenReceta_SQL"].ConnectionString;
        private String ConnectionStringPrueba = ConfigurationManager.ConnectionStrings["AppGenReceta_SQL_Prueba"].ConnectionString;

        static string urlPlatLibroRec = ConfigurationManager.AppSettings["URLSitioWeb"];
        static string usr = ConfigurationManager.AppSettings["UsuarioSitioWeb"];
        static string pwd = ConfigurationManager.AppSettings["ClaveSitioWeb"];
        static string urlRutaAdjuntos = ConfigurationManager.AppSettings["URLRutaAdjuntos"];
        //static string nombreImpresoraSeleccionada = "ZDesigner ZM400 200 dpi (ZPL) en pc011521"; //"Microsoft Print To PDF";//"Generic / Text Only";// //"Microsoft Print To PDF";//"Microsoft XPS Document Writer";//"ZDesigner ZM400 200 dpi (ZPL) en pc011521";
        //Imprime con ambas impresoras
               
        #region Formatear Fecha

        public string FormatoHora(String pHora)
        {
            string getHora = "";
            switch (pHora)
            {
                case "8:00AM":
                    getHora = "08:00";
                    break;
                case "8:30AM":
                    getHora = "08:30";
                    break;
                case "9:00AM":
                    getHora = "09:00";
                    break;
                case "9:30AM":
                    getHora = "09:30";
                    break;
                case "08:00AM":
                    getHora = "08:00";
                    break;
                case "08:30AM":
                    getHora = "08:30";
                    break;
                case "09:00AM":
                    getHora = "09:00";
                    break;
                case "09:30AM":
                    getHora = "09:30";
                    break;
                case "10:00AM":
                    getHora = "10:00";
                    break;
                case "10:30AM":
                    getHora = "10:30";
                    break;
                case "11:00AM":
                    getHora = "11:00";
                    break;
                case "11:30AM":
                    getHora = "11:30";
                    break;
                case "12:00PM":
                    getHora = "12:00";
                    break;
                case "12:30PM":
                    getHora = "12:30";
                    break;
                case "1:00PM":
                    getHora = "13:00";
                    break;
                case "1:30PM":
                    getHora = "13:30";
                    break;
                case "2:00PM":
                    getHora = "14:00";
                    break;
                case "2:30PM":
                    getHora = "14:30";
                    break;
                case "3:00PM":
                    getHora = "15:00";
                    break;
                case "3:30PM":
                    getHora = "15:30";
                    break;
                case "4:00PM":
                    getHora = "16:00";
                    break;
                case "4:30PM":
                    getHora = "16:30";
                    break;
                case "5:00PM":
                    getHora = "17:00";
                    break;
                case "5:30PM":
                    getHora = "17:30";
                    break;
                default:
                    getHora = pHora;
                    break;
            }
            return getHora;
        }

        public int IndicaFormato(String pFecha)
        {
            int intFormato = 0;
            int cantdigitos = 0;
            int i = 0;
            string letra;
            cantdigitos = pFecha.Length;
            while (i <= cantdigitos)
            {
                letra = pFecha.Substring(i, 1);
                if (letra == "-" || letra == "/")
                {
                    intFormato = i;
                    i = cantdigitos;
                }
                i++;
            }
            return intFormato;
        }

        public DateTime DevolverFormatoFecha(String pFecha)
        {
            DateTime dtfecha = Convert.ToDateTime("1990-01-01");
            string sdia, smes, sanio, getFecha;
            int cantdigitos = 0, pindicador = 0;
            cantdigitos = pFecha.Length;
            if (cantdigitos == 10)
            {
                pindicador = IndicaFormato(pFecha);
                if (pindicador == 2)
                {
                    sdia = pFecha.Substring(0, 2);   //"28/12/2024"
                    smes = pFecha.Substring(3, 2);   //"28/12/2024"
                    sanio = pFecha.Substring(6, 4);  //"28/12/2024"
                    getFecha = string.Concat(sanio + "-" + smes + "-" + sdia); //"2024-12-28"
                    dtfecha = Convert.ToDateTime(getFecha);
                }
                else if (pindicador == 4)
                {
                    dtfecha = Convert.ToDateTime(pFecha);                      //"2024-12-28"
                }
            }
            return dtfecha;
        }

        #endregion

        #region SQlServer_RFID
       
        // Métodos auxiliares para no repetir código (Refactorización)
        private void ConfigurarLH(StringBuilder sb, string coordenadas)
        {
            sb.AppendLine($"   ^LH{coordenadas}^FS");
        }

        #endregion


        #region SQlServer_Receta

        public List<NPBE> ListarNPS()
        {
            NPBE item;
            List<NPBE> lista = new List<NPBE>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("dbo.SP_LISTAR_NPS", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 600;

                    cnx.Open();
                    IDataReader dr = cmd.ExecuteReader();
                    item = null;
                    while (dr.Read())
                    {
                        item = new NPBE();
                        if (!dr.IsDBNull(dr.GetOrdinal("NP")))
                        {
                            item.NP = dr.GetString(dr.GetOrdinal("NP"));
                        }
                        lista.Add(item);
                    }
                    cnx.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lista;
        }

        //ListarEstilo
        public List<NPBE> ListarCliente()
        {
            NPBE item;
            List<NPBE> lista = new List<NPBE>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("dbo.SP_LISTAR_CLIENTES", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 600;

                    cnx.Open();
                    IDataReader dr = cmd.ExecuteReader();
                    item = null;
                    while (dr.Read())
                    {
                        item = new NPBE();
                        if (!dr.IsDBNull(dr.GetOrdinal("Cliente")))
                        {
                            item.Cliente = dr.GetString(dr.GetOrdinal("Cliente"));
                        }
                        lista.Add(item);
                    }
                    cnx.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lista;
        }

        //SP_LISTAR_DATOS_POR_NP
        public List<NPBE> ListarDatosPorCadaNP()
        {
            NPBE item;
            List<NPBE> lista = new List<NPBE>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("dbo.SP_LISTAR_DATOS_POR_NP", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 600;

                    cnx.Open();
                    IDataReader dr = cmd.ExecuteReader();
                    item = null;
                    while (dr.Read())
                    {
                        item = new NPBE();
                        if (!dr.IsDBNull(dr.GetOrdinal("Cliente")))
                        {
                            item.Cliente = dr.GetString(dr.GetOrdinal("Cliente"));
                        }
                        if (!dr.IsDBNull(dr.GetOrdinal("Estilo")))
                        {
                            item.Estilo = dr.GetString(dr.GetOrdinal("Estilo"));
                        }
                        if (!dr.IsDBNull(dr.GetOrdinal("NP")))
                        {
                            item.NP = dr.GetString(dr.GetOrdinal("NP"));
                        }
                        if (!dr.IsDBNull(dr.GetOrdinal("COMBO")))
                        {
                            item.Combo = dr.GetString(dr.GetOrdinal("COMBO"));
                        }
                        if (!dr.IsDBNull(dr.GetOrdinal("PrendasReq")))
                        {
                            item.PrendasReq = dr.GetInt32(dr.GetOrdinal("PrendasReq"));
                        }
                        if (!dr.IsDBNull(dr.GetOrdinal("ListaItems")))
                        {
                            item.ListaItems = dr.GetString(dr.GetOrdinal("ListaItems"));
                        }
                        lista.Add(item);
                    }
                    cnx.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lista;
        }

        public List<RecetaInsumoBE>  ListarInsumos()
        {
            List<RecetaInsumoBE> lista = new List<RecetaInsumoBE>();
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("SP_LISTAR_INSUMOS", cnx);
                cmd.CommandType = CommandType.StoredProcedure;
                cnx.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new RecetaInsumoBE
                        {
                            CodigoInsumo = dr["Codigo"].ToString(),
                            Descripcion = dr["Descripcion"].ToString(),
                            Stock = dr["Stock"].ToString()
                        });
                    }
                }
            }
            return lista;
        }

        public List<string> ListarTecnicas(string cliente, string temporada, string estilo, string item)
        {
            List<string> lista = new List<string>();
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("SP_LISTAR_TECNICAS", cnx);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Cliente", cliente);
                cmd.Parameters.AddWithValue("@Temporada", temporada);
                cmd.Parameters.AddWithValue("@Estilo", estilo);
                cmd.Parameters.AddWithValue("@Item", item);
                cmd.CommandType = CommandType.StoredProcedure;
                cnx.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(dr["NombreTecnica"].ToString());
                    }
                }
            }
            return lista;
        }

        public bool RegistrarRecetaAnidada(VisitaBE entidad)
        {
            // Convertimos la lista de colores/insumos a un String XML para el SP
            string xmlData = SerializarRecetaAXml(entidad);

            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("SP_GUARDAR_RECETA_COMPLETA", cnx);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@NP", entidad.NP);
                cmd.Parameters.AddWithValue("@XmlData", xmlData);
                cnx.Open();
                using (SqlCommand cmdSet = new SqlCommand("SET ARITHABORT ON;", cnx)) { cmdSet.ExecuteNonQuery(); }

                object res = cmd.ExecuteScalar();
                return res != null && Convert.ToInt32(res) > 0;
            }
        }

        public bool ActualizarRecetaCompleta(VisitaBE entidad)
        {
            bool rpta;
            try
            {
                // 1. Serializar el objeto VisitaBE a XML
                XmlSerializer serializer = new XmlSerializer(typeof(VisitaBE));
                string xmlString = "";
                using (var sww = new StringWriter())
                {
                    using (XmlWriter writer = XmlWriter.Create(sww))
                    {
                        serializer.Serialize(writer, entidad);
                        xmlString = sww.ToString();
                    }
                }

                // 2. Enviar a la Base de Datos
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    // El SP debe estar preparado para recibir el XML y procesar cabecera/detalle
                    SqlCommand cmd = new SqlCommand("dbo.USP_ACTUALIZAR_RECETA_SCTR_XML", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 600;

                    // Enviamos el XML como un string largo o tipo XML en SQL
                    cmd.Parameters.AddWithValue("@XML_DATA", xmlString);

                    cnx.Open();
                    // Suponiendo que el SP devuelve "OK" si todo salió bien
                    object res = cmd.ExecuteScalar();
                    cnx.Close();
                    return res != null && Convert.ToInt32(res) > 0;

                }
            }
            catch (Exception)
            {
                rpta = false;
            }
            return rpta;
        }

        public bool EliminarVisita(int id, string comentario, string usuario)
        {
            bool respuesta = false;
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    // Apuntamos al Procedimiento Almacenado que crearemos en SQL
                    SqlCommand cmd = new SqlCommand("SP_ELIMINAR_VISITA", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@IdVisita", id);
                    cmd.Parameters.AddWithValue("@Comentario", comentario ?? "");
                    cmd.Parameters.AddWithValue("@UsuarioElimina", usuario ?? "");

                    cnx.Open();
                    int filasAfectadas = cmd.ExecuteNonQuery();
                    // Validamos que haya afectado al menos 1 fila
                    respuesta = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return respuesta;
        }

        public string SerializarRecetaAXml(VisitaBE entidad)
        {
            if (entidad == null) return string.Empty;

            // Configuramos para que el XML no tenga declaraciones raras al inicio
            XmlSerializer serializer = new XmlSerializer(typeof(VisitaBE));
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", ""); // Quita prefijos innecesarios

            using (StringWriter sw = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sw, new XmlWriterSettings { OmitXmlDeclaration = true }))
                {
                    serializer.Serialize(writer, entidad, namespaces);
                    return sw.ToString(); // Esto devuelve el string XML
                }
            }
        }

        public VisitaBE ObtenerRecetaPorId(int idVisita)
        {
            VisitaBE entidad = null;
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("dbo.USP_VISITA_OBTENER_COMPLETO", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID_VISITA", idVisita);
                    cnx.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        // 1. LEER CABECERA
                        if (dr.Read())
                        {
                            entidad = new VisitaBE();
                            entidad.NP = dr["NP"].ToString();
                            entidad.Operario = dr["Operario"].ToString();
                            entidad.Tecnica = dr["Tecnica"].ToString();
                            // entidad.Concepto = dr["Concepto"].ToString(); // <- Concepto movido a nivel Prueba
                            entidad.Ubicacion = dr["Ubicacion"].ToString();
                            entidad.Arte = dr["Arte"].ToString();
                            entidad.FechaUDP = dr["FechaUDP"].ToString();

                            entidad.Temporada = dr["Temporada"].ToString();
                            entidad.Item = dr["Item"].ToString();

                            entidad.Cliente = dr["Cliente"].ToString();
                            entidad.Estilo = dr["Estilo"].ToString();
                            entidad.EstiloPropio = dr["EstiloPropio"].ToString();

                            entidad.ComboCabecera = dr["ComboCabecera"].ToString();
                            entidad.PrendasReq = dr["PrendasReq"].ToString();

                            entidad.FechaRegistro = dr["FechaRegistro"].ToString();
                            entidad.HoraRegistro = dr["HoraRegistro"].ToString();

                            entidad.UsuarioCierre = dr["UsuarioCierre"].ToString();
                            entidad.FechaCierre = dr["FechaCierre"].ToString();
                            entidad.UsuarioApertura = dr["UsuarioApertura"].ToString();
                            entidad.FechaApertura = dr["FechaApertura"].ToString();

                            entidad.Colores = new List<ColorBE>();
                            //Fin

                        }

                        // 2. LEER DETALLE (COLORES E INSUMOS)
                        if (dr.NextResult())
                        {
                            while (dr.Read())
                            {
                                string nombreColor = dr["NombreColor"].ToString();

                                // Buscar si el color ya existe en la lista para no duplicarlo
                                var color = entidad.Colores.FirstOrDefault(c => c.Nombre == nombreColor);
                                if (color == null)
                                {
                                    color = new ColorBE
                                    {
                                        IdColor = Convert.ToInt32(dr["IdColor"]),
                                        Nombre = nombreColor,
                                        Combo = dr["ComboColor"].ToString(),
                                        Insumos = new List<InsumoBE>()
                                    };
                                    entidad.Colores.Add(color);
                                }

                                // Agregar el insumo al color correspondiente
                                color.Insumos.Add(new InsumoBE
                                {
                                    IdInsumo = Convert.ToInt32(dr["IdInsumo"]),
                                    CodigoInsumo = dr["CodigoInsumo"].ToString(),
                                    Descripcion = dr["InsumoDescripcion"].ToString(),
                                    Cantidad = Convert.ToDecimal(dr["CantidadInsumo"].ToString()),
                                    //GramosUDP = dr["GramosUDP"] != DBNull.Value ? Convert.ToDouble(dr["GramosUDP"]) : 0,
                                    Pruebas = new List<PruebaUDPBE>() // Inicializar la lista
                                });

                            }
                        }

                        //// 3. NUEVO: LEER PRUEBAS (LAS COLUMNAS DINÁMICAS)
                        //if (dr.NextResult())
                        //{
                        //    while (dr.Read())
                        //    {
                        //        string NomColorPrueba = dr["NombreColor"].ToString();
                        //        string codInsumoPrueba = dr["CodigoInsumo"].ToString();

                        //        // Buscar el insumo específico dentro de la estructura ya cargada
                        //        var insumoTarget = entidad.Colores
                        //            .Where(c => c.Nombre == NomColorPrueba)
                        //            .SelectMany(c => c.Insumos)
                        //            .FirstOrDefault(i => i.CodigoInsumo == codInsumoPrueba);

                        //        if (insumoTarget != null)
                        //        {
                        //            insumoTarget.Pruebas.Add(new PruebaUDPBE
                        //            {
                        //                IdPrueba = dr["IdPrueba"] != DBNull.Value ? Convert.ToInt32(dr["IdPrueba"]) : 0,
                        //                NombrePrueba = dr["NombrePrueba"] != DBNull.Value ? dr["NombrePrueba"].ToString() : "",

                        //                // Blindaje contra nulos en números y booleanos
                        //                GramosUDP = dr["GramosUDP"] != DBNull.Value ? Convert.ToDouble(dr["GramosUDP"]) : 0.0,
                        //                EsPrincipal = dr["EsPrincipal"] != DBNull.Value ? Convert.ToBoolean(dr["EsPrincipal"]) : false
                        //            });
                        //        }

                        //    }
                        //}

                        // 3. NUEVO: LEER PRUEBAS (LAS COLUMNAS DINÁMICAS)
                        if (dr.NextResult())
                        {
                            while (dr.Read())
                            {
                                // 1. Aplicamos Trim() y ToUpper() desde el inicio
                                string NomColorPrueba = dr["NombreColor"] != DBNull.Value ? dr["NombreColor"].ToString().Trim().ToUpper() : "";
                                string codInsumoPrueba = dr["CodigoInsumo"] != DBNull.Value ? dr["CodigoInsumo"].ToString().Trim().ToUpper() : "";

                                // 2. Buscamos ignorando espacios y mayúsculas/minúsculas
                                var insumoTarget = entidad.Colores
                                    .Where(c => c.Nombre != null && c.Nombre.Trim().ToUpper() == NomColorPrueba)
                                    .SelectMany(c => c.Insumos)
                                    .FirstOrDefault(i => i.CodigoInsumo != null && i.CodigoInsumo.Trim().ToUpper() == codInsumoPrueba);

                                if (insumoTarget != null)
                                {
                                    // Opcional: Para estar 100% seguros de que la lista existe antes de agregar
                                    if (insumoTarget.Pruebas == null)
                                    {
                                        insumoTarget.Pruebas = new List<PruebaUDPBE>();
                                    }

                                    insumoTarget.Pruebas.Add(new PruebaUDPBE
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
            catch (Exception ex)
            {
                throw ex;
            }
            return entidad;
        }

        public List<VisitaBE> ListarRecetasGeneradas(string fechaInicio, string fechaFin)
        {
            List<VisitaBE> lista = new List<VisitaBE>();
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                // Procedimiento que hace un SELECT a tu tabla Visita
                SqlCommand cmd = new SqlCommand("dbo.SP_VISITA_LISTAR_MANTENIMIENTO", cnx);
                cmd.CommandType = CommandType.StoredProcedure;

                // Las fechas ya vienen calculadas desde el Controlador (por defecto 3 meses o las enviadas por la UI)
                cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@FechaFin", fechaFin ?? (object)DBNull.Value);

                cnx.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new VisitaBE
                        {
                            //Dato = dr["ID_VISITA"].ToString(), // Usamos Dato para el ID temporalmente
                            //NP = dr["NP"].ToString(),
                            //Tecnica = dr["TECNICA"].ToString(),
                            //Concepto = dr["CONCEPTO"].ToString(),
                            //Ubicacion = dr["UBICACION"].ToString(),
                            //FechaUDP = dr["FECHA_REGISTRO"].ToString()
                            hdnIdVisita = Convert.ToInt32(dr["Dato"].ToString()),
                            Dato = dr["Dato"].ToString(), // ID de la Visita

                            Cliente = dr["Cliente"].ToString(),
                            Temporada = dr["Temporada"].ToString(),
                            Estilo = dr["Estilo"].ToString(),
                            EstiloPropio = dr["EstiloPropio"].ToString(),
                            ComboCabecera = dr["ComboCabecera"].ToString(),
                            Item = dr["Item"].ToString(),
                            // Concepto = dr["Concepto"].ToString(), // <- Desvinculado de cabecera
                            Operario = dr["OperarioUDP"].ToString(),
                            Tecnica = dr["Tecnica"].ToString(),
                            Ubicacion = dr["Ubicacion"].ToString(),
                            FechaUDP = dr["FechaUDP"].ToString(), // "DD/MM/YYYY"
                            FechaRegistro = dr["FechaRegistro"].ToString(),
                            HoraRegistro = dr["HoraRegistro"].ToString(),
                            NP = dr["NP"].ToString()
                        });
                    }
                }
            }
            return lista;
        }

        // En VisitaDA.cs
        public bool ActualizarEstadoAuditoria(int idVisita, string usuario, bool cerrar, string comentario)
        {
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    // USP_ActualizarEstadoVisita debe manejar la lógica de:
                    // IF @cerrar = 1 UPDATE SET UsuarioCierre = @usuario, FechaCierre = GETDATE()...
                    SqlCommand cmd = new SqlCommand("dbo.USP_ActualizarEstadoVisita", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdVisita", idVisita);
                    cmd.Parameters.AddWithValue("@Usuario", usuario);
                    if (cerrar)
                        cmd.Parameters.AddWithValue("@Accion", 1);
                    else
                        cmd.Parameters.AddWithValue("@Accion", 0);
                    cmd.Parameters.AddWithValue("@Comentario", comentario);
                    cnx.Open();
                    int filas = cmd.ExecuteNonQuery();
                    //return filas > 0;
                    return true;
                }
            }
            catch (Exception ex) { throw ex; }
        }


        // 1. Listar Conceptos desde la base de datos
        public List<string> ListarConceptos()
        {
            List<string> lista = new List<string>();
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("dbo.SP_LISTAR_CONCEPTOS", cnx); // Crear este SP en SQL
                cmd.CommandType = CommandType.StoredProcedure;
                cnx.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(dr["Concepto"].ToString());
                    }
                }
            }
            return lista;
        }

        // 1.5 Listar Conceptos desde la base de datos
        public List<string> ListarConceptosPendientes(string cliente, string temporada, string estilo, string item)
        {
            List<string> lista = new List<string>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    // El procedimiento debe devolver conceptos que NO estén en la tabla Visita para esos filtros
                    SqlCommand cmd = new SqlCommand("dbo.SP_ListarConceptosPendientes", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Cliente", cliente);
                    cmd.Parameters.AddWithValue("@Temporada", temporada);
                    cmd.Parameters.AddWithValue("@Estilo", estilo);
                    cmd.Parameters.AddWithValue("@Item", item);

                    cnx.Open();
                    using (IDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(dr["Concepto"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex) { throw ex; }
            return lista;
        }

        public List<string> ListarUbicacionPendientes(string cliente, string temporada, string estilo, string item)
        {
            List<string> lista = new List<string>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    // El procedimiento debe devolver conceptos que NO estén en la tabla Visita para esos filtros
                    SqlCommand cmd = new SqlCommand("dbo.SP_ListarubicacionesPendientes", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Cliente", cliente);
                    cmd.Parameters.AddWithValue("@Temporada", temporada);
                    cmd.Parameters.AddWithValue("@Estilo", estilo);
                    cmd.Parameters.AddWithValue("@Item", item);

                    cnx.Open();
                    using (IDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(dr["Ubicacion"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex) { throw ex; }
            return lista;
        }

        // 1.5 Listar Conceptos desde la base de datos
        public List<string> ListarCombosPorEstilo(string cliente, string temporada, string estilo)
        {
            List<string> lista = new List<string>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    // El procedimiento debe devolver conceptos que NO estén en la tabla Visita para esos filtros
                    SqlCommand cmd = new SqlCommand("dbo.SP_ListarCombosPorEstilo", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Cliente", cliente);
                    cmd.Parameters.AddWithValue("@Temporada", temporada);
                    cmd.Parameters.AddWithValue("@Estilo", estilo);

                    cnx.Open();
                    using (IDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(dr["combo"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex) { throw ex; }
            return lista;
        }

        // Método para listar Items
        //Listar Items
        public List<ItemBE> ListarItems(string cliente, string temporada, string estiloPropio)
        {
            List<ItemBE> lstItems = new List<ItemBE>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    // Reemplaza "SP_LISTAR_ITEMS" por el nombre de tu SP en SQL Server
                    SqlCommand cmd = new SqlCommand("SP_LISTAR_ITEMS", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Cliente", cliente ?? "");
                    cmd.Parameters.AddWithValue("@Temporada", temporada ?? "");
                    cmd.Parameters.AddWithValue("@EstiloPropio", estiloPropio ?? "");

                    cnx.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            ItemBE obj = new ItemBE();
                            obj.CodItem = dr.IsDBNull(dr.GetOrdinal("CodItem")) ? "" : dr.GetString(dr.GetOrdinal("CodItem"));
                            //obj.NombreItem = dr.IsDBNull(dr.GetOrdinal("NombreItem")) ? "" : dr.GetString(dr.GetOrdinal("NombreItem"));
                            lstItems.Add(obj);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lstItems;
        }

        //Listar Estilos propios y Estilos cliente
        // Método para listar Estilos (Relación 1 a 1)
        public List<EstiloBE> ListarEstilos(string cliente, string temporada)
        {
            List<EstiloBE> lstEstilos = new List<EstiloBE>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    // Reemplaza "SP_LISTAR_ESTILOS" por el nombre de tu SP en SQL Server
                    SqlCommand cmd = new SqlCommand("SP_LISTAR_ESTILOS_CLIE_PROP", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Cliente", cliente ?? "");
                    cmd.Parameters.AddWithValue("@Temporada", temporada ?? "");

                    cnx.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            EstiloBE obj = new EstiloBE();
                            // Estilo Cliente
                            obj.CodEstiloCliente = dr.IsDBNull(dr.GetOrdinal("CodEstiloCliente")) ? "" : dr.GetString(dr.GetOrdinal("CodEstiloCliente"));
                            //obj.NombreEstiloCliente = dr.IsDBNull(dr.GetOrdinal("NombreEstiloCliente")) ? "" : dr.GetString(dr.GetOrdinal("NombreEstiloCliente"));
                            // Estilo Propio equivalente
                            obj.CodEstiloPropio = dr.IsDBNull(dr.GetOrdinal("CodEstiloPropio")) ? "" : dr.GetString(dr.GetOrdinal("CodEstiloPropio"));
                            //obj.NombreEstiloPropio = dr.IsDBNull(dr.GetOrdinal("NombreEstiloPropio")) ? "" : dr.GetString(dr.GetOrdinal("NombreEstiloPropio"));

                            lstEstilos.Add(obj);

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lstEstilos;
        }

        // 2. Listar Estilos filtrados por Cliente y Temporada
        //public List<string> ListarEstilosPorClienteTemporada(string cliente, string temporada)
        //{
        //    List<string> lstEstilos = new List<string>();
        //    using (SqlConnection cnx = new SqlConnection(ConnectionString))
        //    {
        //        SqlCommand cmd = new SqlCommand("dbo.SP_ListarEstilosPorClienteTemporada", cnx);
        //        cmd.CommandType = CommandType.StoredProcedure;
        //        cmd.Parameters.AddWithValue("@Cliente", string.IsNullOrEmpty(cliente) ? "" : cliente);
        //        cmd.Parameters.AddWithValue("@Temporada", string.IsNullOrEmpty(temporada) ? "" : temporada);
        //        cnx.Open();
        //        IDataReader dr = cmd.ExecuteReader();
        //        while (dr.Read())
        //        {
        //            if (!dr.IsDBNull(0)) lstEstilos.Add(dr.GetString(0));
        //        }
        //        cnx.Close();
        //    }
        //    return lstEstilos;
        //}

        // 3. Listar Items filtrados por Cliente y Temporada
        public List<string> ListarItemsPorClienteTemporada(string cliente, string temporada)
        {
            List<string> lstItems = new List<string>();
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("dbo.SP_ListarItemsPorClienteTemporada", cnx);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Cliente", string.IsNullOrEmpty(cliente) ? "" : cliente);
                cmd.Parameters.AddWithValue("@Temporada", string.IsNullOrEmpty(temporada) ? "" : temporada);
                cnx.Open();
                IDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    if (!dr.IsDBNull(0)) lstItems.Add(dr.GetString(0));
                }
                cnx.Close();
            }
            return lstItems;
        }

        public List<string> ListarTemporadaPorCliente(string cliente)
        {
            List<string> lstItems = new List<string>();
            using (SqlConnection cnx = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("dbo.ListarTemporadasPorCliente", cnx);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Cliente", string.IsNullOrEmpty(cliente) ? "" : cliente);
                cnx.Open();
                IDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    // Concatenamos el Código y la Descripción para mostrarlo al usuario
                    if (!dr.IsDBNull(0)) lstItems.Add(dr.GetString(0));
                }
                cnx.Close();
            }
            return lstItems;
        }

        public List<UsuarioBE> ListarUsuariosRoles()
        {
            List<UsuarioBE> lista = new List<UsuarioBE>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    // Asumimos que crearás este SP en la base de datos
                    SqlCommand cmd = new SqlCommand("dbo.SP_ListarUsuariosRoles", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cnx.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            UsuarioBE obj = new UsuarioBE();
                            obj.IDUsuario = Convert.ToInt32(dr["IdUsuario"]);
                            obj.NombreUsuario = dr["NombreUsuario"].ToString();
                            obj.IdRol = Convert.ToInt32(dr["IdRol"]);
                            obj.NombreRol = dr["NombreRol"].ToString();
                            lista.Add(obj);
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

        public bool ActualizarRol(int idUsuario, int idRol)
        {
            bool exito = false;
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    // Asumimos que crearás este SP en la base de datos
                    SqlCommand cmd = new SqlCommand("dbo.SP_ActualizarRolUsuario", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    cmd.Parameters.AddWithValue("@IdRol", idRol);

                    cnx.Open();
                    int filasAfectadas = cmd.ExecuteNonQuery();
                    exito = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return exito;
        }

        #endregion

        // NUEVA FUNCIONALIDAD: BÚSQUEDA INVERSA
        public List<EstiloBE> ListarTodosLosEstilos()
        {
            List<EstiloBE> lstEstilos = new List<EstiloBE>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_LISTAR_TODOS_ESTILOS", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cnx.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            EstiloBE obj = new EstiloBE();
                            obj.CodEstiloCliente = dr.IsDBNull(dr.GetOrdinal("CodEstiloCliente")) ? "" : dr.GetString(dr.GetOrdinal("CodEstiloCliente"));
                            obj.CodEstiloPropio = dr.IsDBNull(dr.GetOrdinal("CodEstiloPropio")) ? "" : dr.GetString(dr.GetOrdinal("CodEstiloPropio"));
                            lstEstilos.Add(obj);
                        }
                    }
                }
            }
            catch (Exception ex) { throw ex; }
            return lstEstilos;
        }

        public VisitaBE ObtenerDatosEstiloInverso(string estiloBuscar, bool esPropio)
        {
            VisitaBE obj = new VisitaBE();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("SP_OBTENER_DATOS_ESTILO", cnx);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@EstiloBuscar", estiloBuscar);
                    cmd.Parameters.AddWithValue("@EsPropio", esPropio);
                    cnx.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            obj.Cliente = dr.IsDBNull(dr.GetOrdinal("Cliente")) ? "" : dr.GetString(dr.GetOrdinal("Cliente"));
                            obj.Temporada = dr.IsDBNull(dr.GetOrdinal("Temporada")) ? "" : dr.GetString(dr.GetOrdinal("Temporada"));
                            obj.Estilo = dr.IsDBNull(dr.GetOrdinal("CodEstiloCliente")) ? "" : dr.GetString(dr.GetOrdinal("CodEstiloCliente"));
                            obj.EstiloPropio = dr.IsDBNull(dr.GetOrdinal("CodEstiloPropio")) ? "" : dr.GetString(dr.GetOrdinal("CodEstiloPropio"));
                        }
                    }
                }
            }
            catch (Exception ex) { throw ex; }
            return obj;
        }

        public List<EstiloBE> BuscarEstilosInversoAjax(string q, bool esPropio)
        {
            List<EstiloBE> lstEstilos = new List<EstiloBE>();
            try
            {
                using (SqlConnection cnx = new SqlConnection(ConnectionString))
                {
                    string filtro = "%" + q + "%";
                    string condicion = esPropio ? "Cod_EstPro LIKE @Q" : "COD_ESTCLI LIKE @Q";
                    string query = $@"
                        SELECT 
                            LTRIM(RTRIM(COD_ESTCLI)) AS 'CodEstiloCliente', 
                            LTRIM(RTRIM(Cod_EstPro)) AS 'CodEstiloPropio' 
                        FROM TG_ESTCLIEST 
                        WHERE COD_ESTCLI IS NOT NULL AND Cod_EstPro IS NOT NULL AND {condicion}";

                    SqlCommand cmd = new SqlCommand(query, cnx);
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@Q", filtro);
                    cnx.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            EstiloBE obj = new EstiloBE();
                            obj.CodEstiloCliente = dr.IsDBNull(dr.GetOrdinal("CodEstiloCliente")) ? "" : dr.GetString(dr.GetOrdinal("CodEstiloCliente"));
                            obj.CodEstiloPropio = dr.IsDBNull(dr.GetOrdinal("CodEstiloPropio")) ? "" : dr.GetString(dr.GetOrdinal("CodEstiloPropio"));
                            lstEstilos.Add(obj);
                        }
                    }
                }
            }
            catch (Exception ex) { throw ex; }
            return lstEstilos;
        }

    }
}
