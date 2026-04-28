using AppGenReceta.BL;
using AppGenReceta.BE;
using System.Web.Mvc;
using System.Collections.Generic;

namespace AppGenReceta.Web.Controllers
{
    public class ProveedorController : Controller
    {
        private Proveedor_BL bl = new Proveedor_BL();

        public ActionResult Index()
        {
            // Carga inicial (todo)
            List<E_InsumoProveedor> modelo = bl.ListarRelacionesProveedorInsumo("");
            return View(modelo);
        }

        [HttpPost]
        public ActionResult FiltrarGrilla(string filtroBusqueda)
        {
            List<E_InsumoProveedor> modelo = bl.ListarRelacionesProveedorInsumo(filtroBusqueda);
            return PartialView("_ListaProveedoresGrid", modelo);
        }

        [HttpPost]
        public JsonResult GuardarRelacionMulti(E_Proveedor objData)
        {
            try
            {
                if (objData == null || string.IsNullOrWhiteSpace(objData.RazonSocial) || 
                    objData.ListaInsumos == null || objData.ListaInsumos.Count == 0)
                {
                    return Json(new { success = false, message = "Datos incompletos. Se requiere Razón Social y al menos un insumo." });
                }

                int id_generado = bl.GuardarProveedorConInsumos(objData);
                if (id_generado > 0)
                {
                    return Json(new { success = true, message = "Proveedor y relaciones guardadas con éxito." });
                }
                else
                {
                    return Json(new { success = false, message = "No se pudo guardar la información." });
                }
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = "Error interno: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarRelacion(int idRelacion)
        {
            try
            {
                bool r = bl.EliminarRelacion(idRelacion);
                if (r) return Json(new { success = true, message = "Insumo eliminado con éxito." });
                else return Json(new { success = false, message = "No se pudo eliminar el registro o ya no existe." });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerProveedor(int idProveedor)
        {
            try
            {
                var prov = bl.ObtenerProveedorConDetalles(idProveedor);
                if(prov != null)
                {
                    return Json(new { success = true, data = prov }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false, message="No encontrado" }, JsonRequestBehavior.AllowGet);
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public async System.Threading.Tasks.Task<JsonResult> ConsultarRucBase(string ruc)
        {
            if (string.IsNullOrWhiteSpace(ruc) || ruc.Length != 11)
            {
                return Json(new { success = false, message = "RUC inválido." }, JsonRequestBehavior.AllowGet);
            }

            var service = new RucService();
            var resultado = await service.ConsultarRucAsync(ruc);

            return Json(resultado, JsonRequestBehavior.AllowGet);
        }
    }
}
