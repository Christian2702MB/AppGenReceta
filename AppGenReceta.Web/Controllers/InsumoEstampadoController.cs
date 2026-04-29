using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using AppGenReceta.BE;
using AppGenReceta.BL;

namespace AppGenReceta.Web.Controllers
{
    public class InsumoEstampadoController : Controller
    {
        private InsumoNP_BL bl = new InsumoNP_BL();

        [HttpGet]
        public ActionResult Index()
        {
            // Set default dates for ViewBag (used in View on load)
            E_InsumoNPFiltro defecto = bl.ConstruirFiltroDefecto();
            ViewBag.FechaInicio = defecto.FechaInicio.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = defecto.FechaFin.ToString("yyyy-MM-dd");
            return View();
        }

        [HttpPost]
        public JsonResult BuscarNPs(string fechaInicio, string fechaFin, string np, string tipoBusqueda)
        {
            try
            {
                E_InsumoNPFiltro filtro = new E_InsumoNPFiltro();
                filtro.TipoBusqueda = string.IsNullOrWhiteSpace(tipoBusqueda) ? "RANGO" : tipoBusqueda.ToUpper();
                filtro.NP = np;
                
                DateTime dtInicio;
                DateTime dtFin;

                // Validate and parse dates (dd/MM/yyyy format assuming standard from UI or default)
                // The frontend might send yyyy-MM-dd based on the input type="date".
                // We handle both format possibilities standard in C#.
                if (DateTime.TryParse(fechaInicio, out dtInicio))
                    filtro.FechaInicio = dtInicio;
                else
                    filtro.FechaInicio = bl.ConstruirFiltroDefecto().FechaInicio;

                if (DateTime.TryParse(fechaFin, out dtFin))
                    filtro.FechaFin = dtFin;
                else
                    filtro.FechaFin = bl.ConstruirFiltroDefecto().FechaFin;

                List<E_InsumoNP> lista = bl.BuscarNPs(filtro);

                return Json(new { ok = true, data = lista, mensaje = "Búsqueda completada." }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, data = new List<E_InsumoNP>(), mensaje = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult AutocompleteNP(string term)
        {
            try
            {
                List<E_NpAutocomplete> lista = bl.AutocompleteNP(term);
                // jQuery UI Autocomplete expects an array of strings or objects with 'label' and 'value'.
                // Returning a simple array of strings is sufficient if we use source mapping in JS.
                var result = lista.Select(x => x.NP).ToList();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                // Return empty quietly on error for autocomplete
                return Json(new List<string>(), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult AgregarNP(string codigoNP, string npsActualesJson)
        {
            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                serializer.MaxJsonLength = int.MaxValue;
                
                List<E_InsumoNP> listaActual = string.IsNullOrWhiteSpace(npsActualesJson) 
                    ? new List<E_InsumoNP>() 
                    : serializer.Deserialize<List<E_InsumoNP>>(npsActualesJson);

                List<E_InsumoNP> listaNueva = bl.AgregarNPExterna(listaActual, codigoNP);

                return Json(new { ok = true, data = listaNueva, mensaje = "NP agregada existosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, data = new List<E_InsumoNP>(), mensaje = ex.Message });
            }
        }

        // ==========================================
        // PASO 2: CÁLCULO DE INSUMOS
        // ==========================================

        [HttpPost]
        public JsonResult GuardarAgrupacion(string jsonData)
        {
            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                serializer.MaxJsonLength = int.MaxValue;
                List<E_InsumoNP> lista = serializer.Deserialize<List<E_InsumoNP>>(jsonData);

                // Asumimos un usuario de sesión (mock por ahora)
                string usuarioCrea = Session["Usuario"] != null ? Session["Usuario"].ToString() : "GUEST";
                
                string sid = bl.GuardarAgrupacionNP(lista, usuarioCrea);
                Session["CurrentAgrupacion"] = sid; // Guardamos en sesión web por seguridad si se prefiere no enviar por url

                return Json(new { ok = true, sessionId = sid, redirect = Url.Action("CalcularInsumos", "InsumoEstampado", new { sid = sid }) });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, mensaje = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult CalcularInsumos(string sid)
        {
            if (string.IsNullOrWhiteSpace(sid)) return RedirectToAction("Index");

            List<E_InsumoNP> listaIzquierda = bl.ObtenerAgrupacionNP(sid);
            ViewBag.SessionID = sid;
            ViewBag.NPsGuardadas = listaIzquierda;

            return View();
        }

        [HttpPost]
        public JsonResult EjecutarCalculo(string sid, string estilosCsv)
        {
            try
            {
                List<E_InsumoCalculado> resultadoSP = bl.CalcularInsumos(sid, estilosCsv);
                return Json(new { ok = true, data = resultadoSP, mensaje = "Cálculo ejecutado exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, data = new List<E_InsumoCalculado>(), mensaje = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ConfirmarCalculo(string jsonData, string sid)
        {
            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                serializer.MaxJsonLength = int.MaxValue;
                List<E_InsumoCalculado> lista = serializer.Deserialize<List<E_InsumoCalculado>>(jsonData);

                string usuarioProcesa = Session["Usuario"] != null ? Session["Usuario"].ToString() : "GUEST";

                bl.GuardarInsumosCalculados(lista, sid, usuarioProcesa);

                return Json(new { ok = true, mensaje = "Insumos guardados exitosamente.", redirect = Url.Action("AjustarCantidades", "InsumoEstampado", new { sid = sid }) });
            }
            catch(Exception ex)
            {
                return Json(new { ok = false, mensaje = ex.Message });
            }
        }

        // ==========================================
        // PASO 3: AJUSTAR CANTIDADES
        // ==========================================
        [HttpGet]
        public ActionResult AjustarCantidades(string sid)
        {
            if (string.IsNullOrWhiteSpace(sid)) return RedirectToAction("Index");

            List<E_InsumoCalculado> calculados = bl.ObtenerInsumosCalculados(sid);
            ViewBag.SessionID = sid;
            ViewBag.InsumosCalculados = calculados;

            return View();
        }

        [HttpPost]
        public JsonResult GuardarAjustesEtapa3(string jsonData)
        {
            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                serializer.MaxJsonLength = int.MaxValue;
                List<E_InsumoCalculado> lista = serializer.Deserialize<List<E_InsumoCalculado>>(jsonData);

                // Llamar a través de la capa de Negocio (BL)
                bool exito = bl.ActualizarAjustesInsumosCalculados(lista);
                
                return Json(new { ok = exito, mensaje = "Cantidades y proveedores guardados. Derivando a Paso 4."});
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, mensaje = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult BuscarInsumoExtraReceta(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 3) return Json(new { ok = false, data = new List<dynamic>() }, JsonRequestBehavior.AllowGet);
            
            try
            {
                // Llamar a través de la capa de Negocio (BL)
                var resultados = bl.BuscarInsumosFiltro(q);
                return Json(new { ok = true, data = resultados.Select(x => new { id = x.CodigoInsumo, text = x.Descripcion }).ToList() }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, mensaje = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
