using System;
using System.Collections.Generic;
using System.Web.Mvc;
using AppGenReceta.BE;
using AppGenReceta.BL;

namespace AppGenReceta.Web.Controllers
{
    /// <summary>
    /// Controlador del módulo de Liquidación de Insumos de Estampado.
    /// Accesible por usuarios con rol Liquidador o Administrador.
    /// </summary>
    public class LiquidacionController : Controller
    {
        private Liquidacion_BL bl = new Liquidacion_BL();
        private LiquidacionStockReq_BL _stockReqBl = new LiquidacionStockReq_BL();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            string rol = Session["RolUsuario"] != null ? Session["RolUsuario"].ToString() : "";
            if (rol != "Liquidador" && filterContext.ActionDescriptor.ActionName != "ObtenerNPsPendientes")
            {
                filterContext.Result = new RedirectToRouteResult(new System.Web.Routing.RouteValueDictionary(new { controller = "Home", action = "Index" }));
                return;
            }
            base.OnActionExecuting(filterContext);
        }

        // ==========================================
        // MENÚ PRINCIPAL DEL MÓDULO
        // ==========================================
        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.Usuario = Session["NombreUsuario"];
            ViewBag.Correo = Session["CorreoUsuario"];
            return View();
        }

        // ==========================================
        // GESTIÓN DE LIQUIDACIONES DE INSUMOS
        // ==========================================
        [HttpGet]
        public ActionResult LiquidacionInsumos()
        {
            if (Session["NombreUsuario"] == null)
                return RedirectToAction("Index", "Home");

            ViewBag.Usuario = Session["NombreUsuario"];
            ViewBag.Correo = Session["CorreoUsuario"];

            return View();
        }

        // ==========================================
        // MANTENIMIENTO DE FÓRMULAS
        // ==========================================
        [HttpGet]
        public ActionResult MantenimientoFormulas(string start = null, string end = null)
        {
            try
            {
                if (Session["NombreUsuario"] == null)
                    return RedirectToAction("Index", "Home");

                ViewBag.Usuario = Session["NombreUsuario"];
                ViewBag.Correo = Session["CorreoUsuario"];

                // Fechas por defecto: último mes
                if (string.IsNullOrEmpty(start)) start = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
                if (string.IsNullOrEmpty(end)) end = DateTime.Now.ToString("yyyy-MM-dd");

                ViewBag.FechaInicio = start;
                ViewBag.FechaFin = end;

                // Formatear a dd/MM/yyyy para la BD
                string fechaInicioDB = DateTime.ParseExact(start, "yyyy-MM-dd", null).ToString("dd/MM/yyyy");
                string fechaFinDB = DateTime.ParseExact(end, "yyyy-MM-dd", null).ToString("dd/MM/yyyy");

                List<LIQ_FormulaBE> lista = bl.ListarFormulas(fechaInicioDB, fechaFinDB);
                return View(lista);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // ==========================================
        // NUEVA FÓRMULA — Listar recetas disponibles
        // ==========================================
        [HttpGet]
        public ActionResult NuevaFormula()
        {
            if (Session["NombreUsuario"] == null)
                return RedirectToAction("Index", "Home");

            ViewBag.Usuario = Session["NombreUsuario"];
            ViewBag.Correo = Session["CorreoUsuario"];

            // Cargar directamente la lista de recetas que cumplen las reglas de negocio
            List<LIQ_RecetaDisponibleBE> recetas = bl.ListarRecetasParaFormula();
            return View(recetas);
        }

        /// <summary>
        /// Obtiene los datos completos de una receta (cabecera + colores + insumos + prueba principal)
        /// para mostrar en el modal de preview antes de formular.
        /// Reutiliza la capa de VisitaBL ya existente.
        /// </summary>
        [HttpGet]
        public JsonResult ObtenerRecetaPreview(int id)
        {
            try
            {
                VisitaBL visitaBL = new VisitaBL();
                VisitaBE entidad = visitaBL.ObtenerRecetaPorId(id);
                return Json(new { success = true, data = entidad }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Crea una nueva fórmula copiando la receta seleccionada.
        /// Retorna el ID de la fórmula para redirigir a edición.
        /// </summary>
        [HttpPost]
        public JsonResult CrearFormulaDesdeReceta(int idReceta)
        {
            try
            {
                string usuario = Session["Cod_Usuario"] != null ? Session["Cod_Usuario"].ToString() : "GUEST";
                int idFormula = bl.CrearFormulaDesdeReceta(idReceta, usuario);

                if (idFormula > 0)
                {
                    return Json(new
                    {
                        success = true,
                        idFormula = idFormula,
                        message = "Fórmula creada exitosamente.",
                        redirect = Url.Action("EditarFormula", "Liquidacion", new { id = idFormula })
                    });
                }
                else
                {
                    return Json(new { success = false, message = "No se pudo crear la fórmula." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // EDITAR FÓRMULA
        // ==========================================
        [HttpGet]
        public ActionResult EditarFormula(int id)
        {
            if (Session["NombreUsuario"] == null)
                return RedirectToAction("Index", "Home");

            ViewBag.Usuario = Session["NombreUsuario"];
            ViewBag.Correo = Session["CorreoUsuario"];
            ViewBag.DNI = Session["Documento"];

            string rolActual = Session["RolUsuario"] != null ? Session["RolUsuario"].ToString() : "Visualizador";

            LIQ_FormulaBE formula = bl.ObtenerFormulaCompleta(id);
            if (formula == null)
                return RedirectToAction("MantenimientoFormulas");

            // Controlar estado de solo lectura
            formula.EstaCerrado = (formula.Estado == "Cerrada");
            formula.IsReadOnly = formula.EstaCerrado && rolActual != "Administrador";

            ViewBag.RolActual = rolActual;
            return View(formula);
        }

        /// <summary>
        /// Obtiene la fórmula completa como JSON (para carga AJAX).
        /// </summary>
        [HttpGet]
        public JsonResult ObtenerFormulaCompleta(int id)
        {
            try
            {
                LIQ_FormulaBE entidad = bl.ObtenerFormulaCompleta(id);
                return Json(new { success = true, data = entidad }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Guarda los cambios de una fórmula editada.
        /// </summary>
        [HttpPost]
        public JsonResult GuardarFormula(LIQ_FormulaBE entidad)
        {
            try
            {
                string usuario = Session["Cod_Usuario"] != null ? Session["Cod_Usuario"].ToString() : "GUEST";
                entidad.UsuarioModificacion = usuario;

                // Recuperar los datos de cabecera que son de solo lectura y no se envían desde la vista
                LIQ_FormulaBE formulaExistente = bl.ObtenerFormulaCompleta(entidad.IdFormula);
                if (formulaExistente != null)
                {
                    entidad.Operario = formulaExistente.Operario;
                    entidad.Tecnica = formulaExistente.Tecnica;
                    entidad.FechaUDP = formulaExistente.FechaUDP;
                    entidad.PrendasReq = formulaExistente.PrendasReq;
                }

                bool resultado = bl.ActualizarFormula(entidad);
                if (resultado)
                    return Json(new { success = true, message = "Fórmula actualizada correctamente." });
                else
                    return Json(new { success = false, message = "No se pudo actualizar la fórmula." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Elimina lógicamente una fórmula.
        /// </summary>
        [HttpPost]
        public JsonResult EliminarFormula(int id, string comentario)
        {
            try
            {
                string usuario = Session["NombreUsuario"] != null ? Session["NombreUsuario"].ToString() : "GUEST";
                bool resultado = bl.EliminarFormula(id, usuario, comentario);
                if (resultado)
                    return Json(new { success = true, message = "Fórmula eliminada correctamente." });
                else
                    return Json(new { success = false, message = "No se pudo eliminar la fórmula." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Cambia el estado de una fórmula (Cerrar/Abrir) — auditoría.
        /// </summary>
        [HttpPost]
        public JsonResult CambiarEstadoFormula(int id, bool cerrar, string comentario)
        {
            try
            {
                string usuario = Session["NombreUsuario"] != null ? Session["NombreUsuario"].ToString() : "GUEST";
                bool resultado = bl.CambiarEstadoFormula(id, usuario, cerrar, comentario);
                if (resultado)
                    return Json(new { success = true, message = "Estado actualizado correctamente." });
                else
                    return Json(new { success = false, message = "Error al actualizar el estado." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // SUB-MÓDULO: STOCK ACTUAL
        // ==========================================
        [HttpGet]
        public ActionResult StockActual()
        {
            if (Session["NombreUsuario"] == null)
                return RedirectToAction("Index", "Home");

            ViewBag.Usuario = Session["NombreUsuario"];
            ViewBag.Correo = Session["CorreoUsuario"];

            try
            {
                var lista = _stockReqBl.ListarStockActual();
                return View(lista);
            }
            catch (Exception)
            {
                return View(new List<LIQ_StockInsumoBE>());
            }
        }

        // ==========================================
        // SUB-MÓDULO: REQUERIMIENTOS Y RECEPCIÓN
        // ==========================================
        [HttpGet]
        public ActionResult MantenimientoProcesoProductivo(string start = null, string end = null)
        {
            try
            {
                if (Session["NombreUsuario"] == null)
                    return RedirectToAction("Index", "Home");

                ViewBag.Usuario = Session["NombreUsuario"];
                ViewBag.Correo = Session["CorreoUsuario"];

                // Fechas por defecto: un mes atrás hasta hoy
                if (string.IsNullOrEmpty(start)) start = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
                if (string.IsNullOrEmpty(end)) end = DateTime.Now.ToString("yyyy-MM-dd");

                ViewBag.FechaInicio = start;
                ViewBag.FechaFin = end;

                // Formatear a dd/MM/yyyy para la BD
                string fechaInicioDB = DateTime.ParseExact(start, "yyyy-MM-dd", null).ToString("dd/MM/yyyy");
                string fechaFinDB = DateTime.ParseExact(end, "yyyy-MM-dd", null).ToString("dd/MM/yyyy");

                var lista = _stockReqBl.ListarRecepcionesHistoricas(start, end);
                return View(lista);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet]
        public JsonResult ObtenerDetalleRecepcion(int id)
        {
            try
            {
                var detalle = _stockReqBl.ObtenerDetalleRecepcion(id);
                return Json(new { success = true, data = detalle }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public ActionResult Requerimientos()
        {
            if (Session["NombreUsuario"] == null)
                return RedirectToAction("Index", "Home");

            ViewBag.Usuario = Session["NombreUsuario"];
            ViewBag.Correo = Session["CorreoUsuario"];

            return View(new List<LIQ_RequerimientoBE>());
        }

        [HttpPost]
        public JsonResult ListarRequerimientosAJAX(string opcion, string desde, string hasta, string np = "", int? numReq = null)
        {
            try
            {
                string strDesde = "";
                string strHasta = "";

                if (!string.IsNullOrEmpty(desde))
                {
                    DateTime dtDesde;
                    if (DateTime.TryParseExact(desde, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dtDesde)) 
                        strDesde = dtDesde.ToString("dd/MM/yyyy");
                    else 
                        strDesde = desde;
                }
                else strDesde = DateTime.Now.AddMonths(-1).ToString("dd/MM/yyyy");

                if (!string.IsNullOrEmpty(hasta))
                {
                    DateTime dtHasta;
                    if (DateTime.TryParseExact(hasta, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dtHasta)) 
                        strHasta = dtHasta.ToString("dd/MM/yyyy");
                    else 
                        strHasta = hasta;
                }
                else strHasta = DateTime.Now.ToString("dd/MM/yyyy");

                var lista = _stockReqBl.ListarRequerimientosAJAX(opcion, strDesde, strHasta, np, numReq);
                return Json(new { success = true, data = lista });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ObtenerVistaPreviaReq(int numReq)
        {
            try
            {
                var detalles = _stockReqBl.ObtenerDetalleRequerimiento(numReq);
                return Json(new { success = true, data = detalles });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ConfirmarRecepcion(int numRequerimiento, string codOrdPro, string motivo)
        {
            try
            {
                if (Session["NombreUsuario"] == null)
                    return Json(new { success = false, message = "Sesión expirada" });

                string usuario = Session["NombreUsuario"].ToString();
                string msj = _stockReqBl.ConfirmarRecepcion(numRequerimiento, codOrdPro, motivo, usuario);
                return Json(new { success = true, message = msj });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RegistrarCargaInicial(string codInsumo, string descripcion, string unidadMedida, decimal pesoGramos)
        {
            try
            {
                if (Session["NombreUsuario"] == null)
                    return Json(new { success = false, message = "Sesión expirada" });

                string usuario = Session["NombreUsuario"].ToString();
                string msj = _stockReqBl.RegistrarCargaInicial(codInsumo, descripcion, unidadMedida, pesoGramos, usuario);
                return Json(new { success = true, message = msj });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =======================================================================
        // METODOS OPERATIVOS (CONSUMOS, MERMAS, DEVOLUCIONES)
        // =======================================================================

        [HttpGet]
        public JsonResult ObtenerLiquidacionesConsolidadas(string estado)
        {
            try
            {
                var data = bl.ObtenerLiquidacionesConsolidadas(estado);
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerMermasStock()
        {
            try
            {
                var data = bl.ObtenerMermasStock();
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult RegistrarOperacion(LIQ_OperacionBE ope)
        {
            try
            {
                if (Session["NombreUsuario"] == null)
                    return Json(new { success = false, message = "Sesión expirada" });

                ope.Usuario = Session["NombreUsuario"].ToString();
                bool res = bl.RegistrarOperacion(ope);
                return Json(new { success = res });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerSaldosInsumo(string np, string codInsumo)
        {
            try
            {
                var data = bl.ObtenerSaldosPopup(np, codInsumo);
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerNPsPendientes()
        {
            try
            {
                var data = bl.ObtenerNPsPendientes();
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult RegistrarMermaColor(LIQ_MermaColorRegistroBE merma)
        {
            try
            {
                if (Session["NombreUsuario"] == null)
                    return Json(new { success = false, message = "Sesión expirada" });

                string usuario = Session["NombreUsuario"].ToString();
                bool res = bl.RegistrarMermaColor(merma, usuario);
                return Json(new { success = res });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerMermasPorColor(string np, string color)
        {
            try
            {
                var data = bl.ObtenerMermasPorColor(np, color);
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerAjustesPorInsumo(string np, string codInsumo)
        {
            try
            {
                var data = bl.ObtenerAjustesPorInsumo(np, codInsumo);
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // =======================================================================
        // MAQUINA DE ESTADOS
        // =======================================================================

        [HttpPost]
        public JsonResult AvanzarEstadoNP(string np, string nuevoEstado)
        {
            try
            {
                if (Session["NombreUsuario"] == null)
                    return Json(new { success = false, message = "Sesion expirada" });

                string usuario = Session["NombreUsuario"].ToString();
                var resultado = bl.AvanzarEstadoNP(np, nuevoEstado, usuario);
                return Json(new { success = resultado.Exito, message = resultado.Mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RegistrarNoMermaGlobal(string np)
        {
            try
            {
                if (Session["NombreUsuario"] == null)
                    return Json(new { success = false, message = "Sesion expirada" });

                string usuario = Session["NombreUsuario"].ToString();
                bl.RegistrarNoMermaGlobal(np, usuario);

                // Auto-avanzar a Pendiente
                var resultado = bl.AvanzarEstadoNP(np, "Pendiente", usuario);
                return Json(new { success = resultado.Exito, message = resultado.Exito ? "Merma global registrada y NP avanzada a Pendiente de Liquidacion." : resultado.Mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RegistrarNoAjusteGlobal(string np)
        {
            try
            {
                if (Session["NombreUsuario"] == null)
                    return Json(new { success = false, message = "Sesion expirada" });

                string usuario = Session["NombreUsuario"].ToString();
                bl.RegistrarNoAjusteGlobal(np, usuario);

                // Auto-avanzar a Liquidado
                var resultado = bl.AvanzarEstadoNP(np, "Liquidado", usuario);
                return Json(new { success = resultado.Exito, message = resultado.Exito ? "Ajustes globales registrados y NP liquidada exitosamente." : resultado.Mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult TerminarNP(string np, string destinoGlobal)
        {
            try
            {
                if (Session["NombreUsuario"] == null)
                    return Json(new { success = false, message = "Sesion expirada" });

                string usuario = Session["NombreUsuario"].ToString();
                var resultado = bl.TerminarNP(np, destinoGlobal, usuario);
                return Json(new { success = resultado.Exito, message = resultado.Mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
