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

        // ==========================================
        // MENÚ PRINCIPAL DEL MÓDULO
        // ==========================================
        [HttpGet]
        public ActionResult Index()
        {
            if (Session["NombreUsuario"] == null)
                return RedirectToAction("Index", "Home");

            string rol = Session["RolUsuario"] != null ? Session["RolUsuario"].ToString() : "";
            if (rol != "Liquidador" && rol != "Administrador")
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
        // SUB-MÓDULO: REQUERIMIENTOS
        // ==========================================
        [HttpGet]
        public ActionResult Requerimientos()
        {
            if (Session["NombreUsuario"] == null)
                return RedirectToAction("Index", "Home");

            ViewBag.Usuario = Session["NombreUsuario"];
            ViewBag.Correo = Session["CorreoUsuario"];

            try
            {
                var lista = _stockReqBl.ListarRequerimientosPendientes();
                return View(lista);
            }
            catch (Exception)
            {
                return View(new List<LIQ_RequerimientoBE>());
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
    }
}
