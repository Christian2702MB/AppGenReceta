using AppGenReceta.BE;
using AppGenReceta.BL;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace AppGenReceta.Web.Controllers
{
    public class MovimientosController : Controller
    {
        private LIQ_MovimientoBL _movimientoBL = new LIQ_MovimientoBL();

        // GET: Movimientos
        public ActionResult Index(string start = null, string end = null)
        {
            if (Session["RolUsuario"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            ViewBag.Usuario = Session["NombreUsuario"];
            ViewBag.Correo = Session["CorreoUsuario"];

            string rolActual = Session["RolUsuario"].ToString();
            string usuarioLogueado = Session["UsuarioLogueado"] != null ? Session["UsuarioLogueado"].ToString() : "Anonimo";

            try
            {
                if (string.IsNullOrEmpty(start)) start = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
                if (string.IsNullOrEmpty(end)) end = DateTime.Now.ToString("yyyy-MM-dd");

                ViewBag.FechaInicio = start;
                ViewBag.FechaFin = end;

                DateTime dtStart = DateTime.ParseExact(start, "yyyy-MM-dd", null).Date;
                DateTime dtEnd = DateTime.ParseExact(end, "yyyy-MM-dd", null).Date.AddDays(1).AddTicks(-1);

                var solicitudes = _movimientoBL.ListarSolicitudes(usuarioLogueado, rolActual);
                
                // Filtrar en memoria por rango de fechas
                var filtradas = new List<LIQ_MOV_SolicitudBE>();
                foreach (var s in solicitudes)
                {
                    if (s.FechaSolicitud >= dtStart && s.FechaSolicitud <= dtEnd)
                    {
                        filtradas.Add(s);
                    }
                }

                ViewBag.RolActual = rolActual;
                return View(filtradas);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al cargar movimientos: " + ex.Message;
                return View(new List<LIQ_MOV_SolicitudBE>());
            }
        }

        // POST: Movimientos/Registrar
        [HttpPost]
        public JsonResult Registrar(LIQ_MOV_SolicitudBE solicitud)
        {
            if (Session["RolUsuario"] == null || Session["RolUsuario"].ToString() != "Liquidador")
            {
                return Json(new { success = false, message = "No tiene permisos para registrar movimientos." });
            }

            try
            {
                solicitud.UsuarioLiquidador = Session["UsuarioLogueado"] != null ? Session["UsuarioLogueado"].ToString() : "Liquidador";
                
                int id = _movimientoBL.RegistrarSolicitud(solicitud);
                return Json(new { success = true, idSolicitud = id, message = "Solicitud registrada con éxito." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // POST: Movimientos/Aprobar
        [HttpPost]
        public JsonResult Aprobar(int idSolicitud)
        {
            string rolActual = Session["RolUsuario"] != null ? Session["RolUsuario"].ToString() : "";
            if (rolActual != "Aprobador Insumos" && rolActual != "Aprobador Mermas")
            {
                return Json(new { success = false, message = "Solo un aprobador puede realizar esta acción." });
            }

            try
            {
                string usuario = Session["UsuarioLogueado"] != null ? Session["UsuarioLogueado"].ToString() : "Aprobador";
                string numMovERP;
                _movimientoBL.AprobarSolicitud(idSolicitud, usuario, out numMovERP);

                return Json(new { success = true, numMovimiento = numMovERP, message = "Movimiento aprobado e insertado en el ERP con éxito." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al aprobar: " + ex.Message });
            }
        }

        // POST: Movimientos/Observar
        [HttpPost]
        public JsonResult Observar(int idSolicitud, string observacion)
        {
            string rolActual = Session["RolUsuario"] != null ? Session["RolUsuario"].ToString() : "";
            if (rolActual != "Aprobador Insumos" && rolActual != "Aprobador Mermas")
            {
                return Json(new { success = false, message = "Solo un aprobador puede realizar esta acción." });
            }

            try
            {
                string usuario = Session["UsuarioLogueado"] != null ? Session["UsuarioLogueado"].ToString() : "Aprobador";
                _movimientoBL.ObservarSolicitud(idSolicitud, usuario, observacion);

                return Json(new { success = true, message = "Solicitud observada." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // GET: Movimientos/Detalle
        public JsonResult ObtenerDetalle(int idSolicitud)
        {
            try
            {
                var detalles = _movimientoBL.ObtenerDetalles(idSolicitud);
                return Json(new { success = true, data = detalles }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
