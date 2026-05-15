using AppGenReceta.BE;
using AppGenReceta.BL;
using AppGenReceta.HL;
//Agregar 09/01/2026

using Microsoft.Owin.Security;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

//--- Fin ---

namespace AppGenReceta.Web.Controllers
{
    //[Authorize]
    public class HomeController : Controller
    {
        static string urlPaginaWeb = ConfigurationManager.AppSettings["URLPaginaWeb"];
        static string urlCitaVigilante = ConfigurationManager.AppSettings["URLCitaVigilante"];
        //[AllowAnonymous]
        // GET: Home
        public ActionResult Index()
        {
            Session.Clear();
            Session.Abandon();
            return View();
        }

        public ActionResult Logout()
        {
            // 1. Limpiar todas las variables de sesión
            Session.Clear();
            Session.Abandon();
            Session.RemoveAll();

            // 2. Limpiar la cookie de autenticación (Forms o OWIN)
            if (Request.IsAuthenticated)
            {
                var authManager = HttpContext.GetOwinContext().Authentication;
                authManager.SignOut();
            }
            // Elimina la cookie de autenticación si existe
            if (Request.Cookies["ASP.NET_SessionId"] != null)
            {
                Response.Cookies["ASP.NET_SessionId"].Value = string.Empty;
                Response.Cookies["ASP.NET_SessionId"].Expires = DateTime.Now.AddMonths(-20);
            }

            // 3. Redirigir al Login (Index)
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public ActionResult Ingresar(UsuarioBE usuario)
        {
            Object result;
            try
            {
                VisitaBE item = new VisitaBE();
                VisitaBL visitaBL = new VisitaBL();

                if (String.IsNullOrEmpty(usuario.NombreUsuario))
                {
                    result = new { result = "warning", title = "Campo Requerido", message = "Usuario es un campo requerido" };
                    return Json(result);
                }
                if (String.IsNullOrEmpty(usuario.Clave))
                {
                    result = new { result = "warning", title = "Campo Requerido", message = "Contraseña es un campo requerido" };
                    return Json(result);
                }
                UsuarioBL usuarioBL = new UsuarioBL();
                UsuarioBE usuarioLogueado = usuarioBL.ValidarUsuario(usuario);

                if (usuarioLogueado != null)
                {
                    if (usuarioLogueado.Correo != "")
                    {
                        Session["Usuario"] = usuarioLogueado;
                        Session["NombreUsuario"] = usuarioLogueado.Nombres + " " + usuarioLogueado.ApellidoPaterno + " " + usuarioLogueado.ApellidoMaterno;
                        Session["CorreoUsuario"] = usuarioLogueado.Correo;
                        Session["Documento"] = usuarioLogueado.Documento;

                        Session["Cod_Usuario"] = usuarioLogueado.Cod_Usuario;
                        Session["Cod_Fabrica"] = usuarioLogueado.Cod_Fabrica;
                        Session["Tip_Trabajador"] = usuarioLogueado.Tip_Trabajador;
                        Session["Cod_Trabajador"] = usuarioLogueado.Cod_Trabajador;

                        //Session["UsuariosComercial"] = usuarioBL.ConcatenarCorreos("Comercial");
                        //Session["UsuariosUDP"] = usuarioBL.ConcatenarCorreos("UDP");
                        //Session["UsuariosUDPMuestras"] = usuarioBL.ConcatenarCorreos("UDPMuestras");

                        //17/03/2026 roles
                        Session["RolUsuario"] = usuarioLogueado.NombreRol;
                        if (usuarioLogueado.NombreRol == "Estampado")
                        {
                            result = new { result = "success", title = "Satisfactorio", message = "Ingresó Correctamente.", action = Url.Action("Index", "InsumoEstampado") };
                        }
                        else
                        {
                            result = new { result = "success", title = "Satisfactorio", message = "Ingresó Correctamente.", action = Url.Action("MenuProveedor", "Home") };
                        }
                        return Json(result, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        result = new { result = "error", title = "Error", message = "Usuario y/o contraseña incorrectos." };
                        return Json(result);
                    }
                }
                else
                {
                    result = new { result = "error", title = "Error", message = "Usuario y/o contraseña invalidos." };
                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                result = new { result = "error", title = "Error", message = "Lo sentimos, hubo un problema no esperado. Vuelva a intentar por favor." + ex.Message };
                return Json(result);
            }
        }

        [HttpGet]
        [NoCache]
        public ActionResult MantenimientoVisitas(string start = null, string end = null)
        {
            VisitaBL visitaBL = new VisitaBL();
            List<VisitaBE> lstVisitas;
            try
            {
                if (Session["Usuario"] != null)
                {
                    ViewBag.Usuario = Session["NombreUsuario"];
                    ViewBag.Correo = Session["CorreoUsuario"];
                    ViewBag.CabeceraRuc = Session["CabeceraRuc"];

                    // Configurar fechas por defecto si no son provistas
                    if (string.IsNullOrEmpty(start)) start = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
                    if (string.IsNullOrEmpty(end)) end = DateTime.Now.ToString("yyyy-MM-dd");

                    ViewBag.FechaInicio = start;
                    ViewBag.FechaFin = end;

                    // Formatear a dd/MM/yyyy para la Base de Datos
                    string fechaInicioDB = DateTime.ParseExact(start, "yyyy-MM-dd", null).ToString("dd/MM/yyyy");
                    string fechaFinDB = DateTime.ParseExact(end, "yyyy-MM-dd", null).ToString("dd/MM/yyyy");

                    lstVisitas = visitaBL.ListarRecetasGeneradas(fechaInicioDB, fechaFinDB);
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
                return View(lstVisitas);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet]
        public JsonResult ObtenerRecetaCompleta(int id)
        {
            try
            {
                VisitaBL bl = new VisitaBL();
                // Este método debe obtener la cabecera y llenar la lista de Colores e Insumos
                VisitaBE entidad = bl.ObtenerRecetaPorId(id);

                return Json(new { success = true, data = entidad }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerCodigoInsumo(string dato)
        {
            VisitaBL bl = new VisitaBL();
            var insumo = bl.ListarInsumos().FirstOrDefault(x => $"{x.CodigoInsumo} {x.Descripcion}".Trim().ToUpper() == dato.Trim().ToUpper());

            return Json(new
            {
                CodigoInsumo = insumo?.CodigoInsumo ?? "",
                Descripcion = insumo?.Descripcion ?? "",
                Unid_Med = insumo?.Unid_Med ?? "",
                Stock = insumo?.Stock ?? ""
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult BuscarInsumosSelect2(string q)
        {
            VisitaBL bl = new VisitaBL();
            var insumos = bl.ListarInsumos();

            if (!string.IsNullOrEmpty(q))
            {
                q = q.Trim().ToUpper();
                insumos = insumos.Where(x => x.Descripcion.ToUpper().Contains(q) || x.CodigoInsumo.ToUpper().Contains(q)).ToList();
            }

            var result = insumos.Take(100).Select(x => new
            {
                id = x.CodigoInsumo + " " + x.Descripcion,
                text = x.CodigoInsumo + " " + x.Descripcion
            });

            return Json(new { items = result }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ListarTemporadasPorCliente(string cliente)
        {
            try
            {
                VisitaBL visitaBL = new VisitaBL();
                // Utiliza el método que ya tienes creado en tu BL
                var lista = visitaBL.ListarTemporadaPorCliente(cliente);
                return Json(new { success = true, data = lista });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Liquidación de Insumos - Estampado (Mockup)
        public ActionResult Liquidacion_Insumos()
        {
            if (Session["Usuario"] != null)
            {
                ViewBag.Usuario = Session["NombreUsuario"];
                ViewBag.Correo = Session["CorreoUsuario"];
            }
            return View();
        }

        [NoCache]
        public ActionResult Visita() //String id, String pCod_OrdPro, String pSecuencia
        {
            VisitaBL visitaBL = new VisitaBL();
            VisitaBE item = new VisitaBE();

            try
            {
                if (Session["Usuario"] != null)
                {
                    ViewBag.Usuario = Session["NombreUsuario"];
                    ViewBag.Correo = Session["CorreoUsuario"];
                    ViewBag.DNI = Session["Documento"];
                    //ViewBag.Cliente = Session["Cliente"];

                    //lstNPS = visitaBL.ListarNPS();
                    //ViewBag.data = lstNPS;

                    //lstDatosNPS = visitaBL.ListarDatosPorCadaNP();
                    //Session["DatosNPS"] = lstDatosNPS;

                    //ViewBag.Tecnicas = visitaBL.ListarTecnicas();
                    // Implementación de caché para Clientes (expira en 1 hora) para optimizar carga
                    var cacheClientes = System.Web.HttpContext.Current.Cache["ListaClientes"];
                    if (cacheClientes == null)
                    {
                        cacheClientes = visitaBL.ListarCliente();
                        if (cacheClientes != null)
                        {
                            System.Web.HttpContext.Current.Cache.Insert("ListaClientes", cacheClientes, null, 
                                DateTime.Now.AddHours(1), System.Web.Caching.Cache.NoSlidingExpiration);
                        }
                    }
                    ViewBag.Cliente = cacheClientes;

                    //ViewBag.Conceptos = visitaBL.ListarConceptos();

                    ////Datos
                    //lstEstilo = visitaBL.ListarEstilo();
                    //ViewBag.Estilo = lstEstilo;

                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
                return View(item);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        public JsonResult RegistrarVisitaCompletaEditar(VisitaBE entidad)
        {
            try
            {
                // Validar que la entidad no sea nula
                if (entidad == null)
                {
                    return Json(new { result = "error", message = "Los datos de la receta están vacíos." });
                }

                VisitaBL bl = new VisitaBL();

                // El método RegistrarRecetaAnidada debe enviar el XML al SP.
                // Si entidad.Dato (IdVisita) es > 0, el SP hará UPDATE.
                // Si entidad.Dato es "0" o null, el SP hará INSERT.
                bool rpta = bl.ActualizarRecetaCompleta(entidad);

                if (rpta)
                    return Json(new { result = "success", message = "La receta y sus insumos se actualizaron correctamente." });
                else
                    return Json(new { result = "error", message = "Ocurrió un error en la base de datos." });
            }
            catch (Exception ex)
            {
                return Json(new { result = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RegistrarVisitaCompleta(VisitaBE entidad)
        {
            try
            {
                string usuarioAuditoria = Session["NombreUsuario"] != null ? Session["NombreUsuario"].ToString() : "ADMIN_SISTEMA";

                // 1. Validaciones de servidor
                if (entidad == null || entidad.Colores == null || entidad.Colores.Count == 0)
                    return Json(new { result = "error", message = "Datos incompletos" });

                // 2. Llamada a la Capa de Negocio (BL)
                VisitaBL bl = new VisitaBL();
                bool rpta = bl.RegistrarRecetaAnidada(entidad, usuarioAuditoria);

                if (rpta)
                    return Json(new { result = "success", message = "La receta y sus insumos se generaron correctamente." });
                else
                    return Json(new { result = "error", message = "Ocurrió un error en la base de datos." });
            }
            catch (Exception ex)
            {
                return Json(new { result = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarVisita(int id, string comentario)
        {
            try
            {
                // Obtenemos el usuario que realiza la acción desde la sesión
                string usuarioAuditoria = Session["NombreUsuario"] != null ? Session["NombreUsuario"].ToString() : "ADMIN_SISTEMA";

                VisitaBL visitaBL = new VisitaBL();
                bool exito = visitaBL.EliminarVisita(id, comentario, usuarioAuditoria);

                if (exito)
                {
                    return Json(new { success = true, message = "Registro eliminado correctamente." });
                }
                else
                {
                    return Json(new { success = false, message = "No se pudo eliminar el registro." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error interno: " + ex.Message });
            }
        }

        [HttpGet]
        [NoCache]
        public ActionResult MenuProveedor() //
        {
            List<VisitaBE> lstVisitas = new List<VisitaBE>();
            Object result;

            try
            {
                if (Session["Usuario"] != null)
                {
                    ViewBag.Usuario = Session["NombreUsuario"];
                    ViewBag.Correo = Session["CorreoUsuario"];
                    ViewBag.CabeceraRuc = Session["CabeceraRuc"];
                    ViewBag.Cliente = Session["Cliente"];
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                result = new { result = "error", title = "Error", message = "Lo sentimos, hubo un problema no esperado. Vuelva a intentar por favor." + ex.Message };
                return Json(result);
            }
            return View(lstVisitas);
        }

        // ── BÚSQUEDA POR ITEM: Endpoint para el modo "Búsqueda por Item" ──────
        // Creado 08/05/2026 - CMendez
        [HttpGet]
        public JsonResult BuscarDatosPorItem(string item)
        {
            if (string.IsNullOrWhiteSpace(item) || item.Trim().Length < 3)
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);

            try
            {
                VisitaBL visitaBL = new VisitaBL();
                var lista = visitaBL.BuscarDatosPorItem(item.Trim());

                // Mapeamos a un objeto anónimo con propiedades en camelCase para el JS
                var resultado = lista.Select(x => new
                {
                    CodItem       = x.CodItem,
                    CodCliente    = x.CodCliente,
                    CodTemcli     = x.CodTemcli,
                    Ubicacion     = x.Ubicacion,
                    CodTecnica    = x.CodTecnica,
                    DescripcionTecnica = x.DescripcionTecnica
                }).ToList();

                return Json(resultado, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        // ─────────────────────────────────────────────────────────────────────

        #region Corregir SCTR

        // HomeController.cs 17/03/2026
        [HttpGet]
        [OutputCache(Duration = 60, VaryByParam = "cliente;temporada;estilo;item")]
        public JsonResult ListarConceptosPendientes(string cliente, string temporada, string estilo, string item)
        {
            try
            {
                var lista = new VisitaBL().ListarConceptosPendientes(cliente, temporada, estilo, item);
                return Json(lista, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new List<string>(), JsonRequestBehavior.AllowGet);
            }
        }

        // HomeController.cs 30/03/2026
        [HttpGet]
        [OutputCache(Duration = 60, VaryByParam = "cliente;temporada;estilo;item")]
        public JsonResult ListarTecnicas(string cliente, string temporada, string estilo, string item)
        {
            try
            {
                var lista = new VisitaBL().ListarTecnicas(cliente, temporada, estilo, item);
                return Json(lista, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new List<string>(), JsonRequestBehavior.AllowGet);
            }
        }

        // HomeController.cs 30/03/2026
        [HttpGet]
        [OutputCache(Duration = 60, VaryByParam = "cliente;temporada;estilo;item")]
        public JsonResult ListarUbicacionPendientes(string cliente, string temporada, string estilo, string item)
        {
            try
            {
                var lista = new VisitaBL().ListarUbicacionPendientes(cliente, temporada, estilo, item);
                return Json(lista, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new List<string>(), JsonRequestBehavior.AllowGet);
            }
        }

        //Inicio 27/03/26
        [HttpGet]
        [OutputCache(Duration = 300, VaryByParam = "cliente;temporada;estiloPropio")]
        public JsonResult ListarItems(string cliente, string temporada, string estiloPropio)
        {
            try
            {
                VisitaBL visitaBL = new VisitaBL();
                List<ItemBE> lstItems = visitaBL.ListarItems(cliente, temporada, estiloPropio);
                return Json(lstItems, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // 13/05/2026 - CMendez: Obtener Ubicación y Técnica filtrando por Item
        // Reutiliza el SP SP_LISTAR_ITEMS (ya mapeado con Ubicacion y NombreTecnica)
        [HttpGet]
        public JsonResult ObtenerUbicacionTecnicaPorItem(string cliente, string temporada, string estiloPropio, string codItem)
        {
            try
            {
                VisitaBL visitaBL = new VisitaBL();
                List<ItemBE> lstItems = visitaBL.ListarItems(cliente, temporada, estiloPropio);

                // Filtrar por el CodItem seleccionado en la vista
                var itemEncontrado = lstItems.FirstOrDefault(x => x.CodItem.Trim().Equals((codItem ?? "").Trim(), StringComparison.OrdinalIgnoreCase));

                if (itemEncontrado != null)
                {
                    return Json(new
                    {
                        success = true,
                        ubicacion = itemEncontrado.Ubicacion ?? "",
                        tecnica = itemEncontrado.NombreTecnica ?? ""
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { success = false, message = "Item no encontrado en la lista" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ListarEstilos(string cliente, string temporada)
        {
            try
            {
                VisitaBL visitaBL = new VisitaBL();
                List<EstiloBE> lstEstilos = visitaBL.ListarEstilos(cliente, temporada);

                // Al retornar esto, el JS recibirá exactamente las 4 propiedades de EstiloBE
                return Json(lstEstilos, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ListarTodosLosEstilos()
        {
            try
            {
                VisitaBL visitaBL = new VisitaBL();
                List<EstiloBE> lstEstilos = visitaBL.ListarTodosLosEstilos();
                return new JsonResult()
                {
                    Data = lstEstilos,
                    MaxJsonLength = int.MaxValue,
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerDatosEstiloInverso(string estiloBuscar, bool esPropio)
        {
            try
            {
                VisitaBL visitaBL = new VisitaBL();
                VisitaBE obj = visitaBL.ObtenerDatosEstiloInverso(estiloBuscar, esPropio);
                return Json(obj, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult BuscarEstilosInversoAjax(string q, bool esPropio)
        {
            try
            {
                VisitaBL visitaBL = new VisitaBL();
                // Si la búsqueda viene en null, Select2 está recién abriendo sin tipear
                string queryStr = string.IsNullOrEmpty(q) ? "" : q;
                List<EstiloBE> lstEstilos = visitaBL.BuscarEstilosInversoAjax(queryStr, esPropio);

                // Formatear la lista explícitamente para el plugin Select2
                var select2Data = lstEstilos.Select(e => new
                {
                    id = esPropio ? e.CodEstiloPropio : e.CodEstiloCliente,
                    text = esPropio ? e.CodEstiloPropio : e.CodEstiloCliente
                }).Distinct();

                return Json(new { results = select2Data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        //Fin 27/03/26


        // Nuevo método AJAX
        [HttpGet]
        public JsonResult ListarCombosPorEstilo(string cliente, string temporada, string estilo)
        {
            try
            {
                // Lógica de negocio para obtener la lista de combos en base al estilo, cliente y temporada.
                VisitaBL visitaBL = new VisitaBL();
                List<string> lstCombos = visitaBL.ListarCombosPorEstilo(cliente, temporada, estilo);

                return Json(lstCombos, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        ////[HttpGet]
        //[HttpPost]
        //public JsonResult ListarFiltrosPorClienteTemporada(string cliente, string temporada)
        //{
        //    try
        //    {
        //        VisitaBL visitaBL = new VisitaBL();
        //        var estilos = visitaBL.ListarEstilosPorClienteTemporada(cliente, temporada);
        //        //var estilosPropios = visitaBL.ListarEstilosPorClienteTemporada(cliente, temporada);
        //        var items = visitaBL.ListarItemsPorClienteTemporada(cliente, temporada);

        //        return Json(new { success = true, estilos = estilos, items = items });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = ex.Message });
        //    }
        //}

        [NoCache]
        public ActionResult VisitaCorregirSCTR(String id, bool preadonly = false)
        {
            VisitaBL visitaBL = new VisitaBL();
            VisitaBE item = new VisitaBE();
            //ViewBag.ListarInsumos = new SelectList(visitaBL.ListarInsumos(), "CodigoInsumo", "CodigoInsumo");            
            //List<NPBE> lstNPS = new List<NPBE>();
            //List<NPBE> lstDatosNPS = new List<NPBE>();
            DateTime dt;
            try
            {
                if (Session["Usuario"] != null)
                {
                    ViewBag.Usuario = Session["NombreUsuario"];
                    ViewBag.Correo = Session["CorreoUsuario"];
                    ViewBag.DNI = Session["Documento"];
                    //ViewBag.Cliente = Session["Cliente"];

                    // --- CAMBIO CLAVE AQUÍ ---
                    if (!string.IsNullOrEmpty(id))
                    {
                        // Buscamos la información real de la receta usando el ID
                        item = visitaBL.ObtenerRecetaPorId(Convert.ToInt32(id));
                        //Guardar informacion
                        //List<string> listaTecnicas = visitaBL.ListarTecnicas(item.Cliente, item.Temporada, item.EstiloPropio, item.Item);
                        //ViewBag.Tecnicas = listaTecnicas;
                        ViewBag.Conceptos = visitaBL.ListarConceptos();
                        //ViewBag.Conceptos = visitaBL.ListarConceptos(item.Cliente, item.Temporada, item.EstiloPropio, item.Item);

                        // Pasamos el ID a la vista mediante un ViewBag para el campo hidden
                        ViewBag.IdEditar = id;
                        // Si viene de "Cerrar" o si la visita ya tiene datos de cierre en BD
                        ViewBag.IsReadOnly = preadonly || !string.IsNullOrEmpty(item.FechaCierre);

                        // Si la fecha viene como 05/03/2026, la pasamos a 2026-03-05
                        if (!string.IsNullOrEmpty(item.FechaUDP))
                        {                            
                            if (DateTime.TryParseExact(item.FechaUDP, "dd/mm/yyyy", null, System.Globalization.DateTimeStyles.None, out dt))
                            {
                                item.FechaUDP = dt.ToString("yyyy-MM-dd");
                            }
                        }
                    }
                    // --------------------------
                    // lstNPS borrado por optimización

                    //lstDatosNPS = visitaBL.ListarDatosPorCadaNP();
                    //Session["DatosNPS"] = lstDatosNPS;
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
                return View(item);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        [HttpPost]
        public JsonResult CambiarEstadoAuditoria(int id, bool cerrar, string comentario)
        {
            if (Session["RolUsuario"] == null || Session["RolUsuario"].ToString() != "Administrador")
            {
                return Json(new { success = false, message = "No tiene permisos para realizar esta acción. Se requiere perfil Administrador." });
            }
            try
            {
                VisitaBL bl = new VisitaBL();
                string usuarioActual = Session["NombreUsuario"]?.ToString() ?? "SISTEMA";
                // Pasamos el comentario a la lógica de negocio
                bool exito = bl.ActualizarEstadoAuditoria(id, usuarioActual, cerrar, comentario);
                //return Json(new { success = exito });
                return Json(new { success = true, message = "Estado actualizado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        //Inicio de Mantemiento de Roles
        //17/03/26
        [NoCache]
        public ActionResult MantenimientoRoles()
        {
            if (Session["Usuario"] != null)
            {
                ViewBag.Usuario = Session["NombreUsuario"];
                ViewBag.Correo = Session["CorreoUsuario"];
                ViewBag.CabeceraRuc = Session["CabeceraRuc"];
            }
            if (Session["RolUsuario"] == null || Session["RolUsuario"].ToString() != "Administrador")
            {
                return RedirectToAction("Index", "Home"); // Expulsar si no es Admin
            }
            return View();
        }

        [HttpGet]
        public JsonResult ListarUsuariosRoles()
        {
            try
            {
                var lista = new VisitaBL().ListarUsuariosRoles();
                // Devolvemos un objeto con la propiedad "data" para que el JS lo itere correctamente
                return Json(new { data = lista }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<UsuarioBE>(), message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult ActualizarRol(int idUsuario, int idRol)
        {
            try
            {
                // Validación en Backend: Evita que alguien modifique roles inyectando código
                if (Session["RolUsuario"] == null || Session["RolUsuario"].ToString() != "Administrador")
                {
                    return Json(new { success = false, message = "Acceso denegado. Solo un Administrador puede cambiar roles." });
                }

                bool exito = new VisitaBL().ActualizarRol(idUsuario, idRol);

                if (exito)
                {
                    return Json(new { success = true, message = "Rol actualizado correctamente." });
                }
                else
                {
                    return Json(new { success = false, message = "No se encontró el usuario o no hubo cambios." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        //Fin de Mantemiento de Roles



        #endregion

        //Evitar el retorno con el botón "Atrás" (Caché)
        //El problema del botón "atrás" es que el navegador guarda una copia local(caché)
        //de la página.Para evitarlo, debemos decirles a las páginas que no se almacenen en caché.
        //La forma más efectiva y limpia es crear un Action Filter personalizado para que no tengas
        //que repetir código en cada método.
        public class NoCacheAttribute : ActionFilterAttribute
        {
            public override void OnResultExecuting(ResultExecutingContext filterContext)
            {
                var response = filterContext.HttpContext.Response;
                response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
                response.Cache.SetValidUntilExpires(false);
                response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
                response.Cache.SetCacheability(HttpCacheability.NoCache);
                response.Cache.SetNoStore();

                base.OnResultExecuting(filterContext);
            }
        }

    }
}