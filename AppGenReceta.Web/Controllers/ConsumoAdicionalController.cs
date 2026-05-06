using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AppGenReceta.BE;
using AppGenReceta.BL;

namespace AppGenReceta.Web.Controllers
{
    public class ConsumoAdicionalController : Controller
    {
        private ConsumoAdicionalBL bl = new ConsumoAdicionalBL();

        [HttpGet]
        public ActionResult Index(string sid)
        {
            ViewBag.SessionID = sid;
            return View();
        }

        [HttpPost]
        public JsonResult Listar(string opcion, string desde, string hasta, string np)
        {
            try
            {
                // Si la vista envía YYYY-MM-DD (tipo date), lo convertimos a DD/MM/YYYY para el SP.
                string strDesde = "";
                string strHasta = "";
                
                if (opcion == "2") 
                {
                    if (!string.IsNullOrEmpty(desde))
                    {
                        DateTime dtDesde;
                        if (DateTime.TryParse(desde, out dtDesde)) strDesde = dtDesde.ToString("dd/MM/yyyy");
                        else strDesde = desde;
                    }
                    else strDesde = DateTime.Now.AddMonths(-1).ToString("dd/MM/yyyy");

                    if (!string.IsNullOrEmpty(hasta))
                    {
                        DateTime dtHasta;
                        if (DateTime.TryParse(hasta, out dtHasta)) strHasta = dtHasta.ToString("dd/MM/yyyy");
                        else strHasta = hasta;
                    }
                    else strHasta = DateTime.Now.ToString("dd/MM/yyyy");
                }

                var lista = bl.ListarRequerimientos(opcion, strDesde, strHasta, np);
                return Json(new { success = true, data = lista });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ValidarNP(string np)
        {
            try
            {
                if (string.IsNullOrEmpty(np))
                    return Json(new { success = false, message = "Debe ingresar una NP" });

                string validNP = bl.ValidarYObtenerNP(np);
                if (!string.IsNullOrEmpty(validNP))
                {
                    return Json(new { success = true, data = validNP });
                }
                else
                {
                    return Json(new { success = false, message = "NP no encontrada o cancelada en Producción." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GenerarRequerimientoCompleto(string np, string observaciones)
        {
            try
            {
                if (string.IsNullOrEmpty(np))
                    return Json(new { success = false, message = "La NP es obligatoria." });

                string numReq = bl.GenerarRequerimientoCompleto(np, observaciones);
                return Json(new { success = true, message = "Requerimiento N° " + numReq + " generado exitosamente con los insumos de su receta.", data = numReq });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult ListarDetalle(int numReq)
        {
            try
            {
                var lista = bl.ListarDetalles(numReq);
                return Json(new { success = true, data = lista });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GuardarDetalleManual(int numReq, string codItem, decimal consumo, int lote = 0)
        {
            try
            {
                if (numReq <= 0) return Json(new { success = false, message = "ID de requerimiento no válido." });
                if (string.IsNullOrEmpty(codItem)) return Json(new { success = false, message = "Debe seleccionar un insumo." });

                bl.InsertarDetalleSimple(numReq, codItem, consumo, lote);
                return Json(new { success = true, message = "Insumo agregado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
