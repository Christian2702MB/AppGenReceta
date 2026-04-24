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
    }
}
