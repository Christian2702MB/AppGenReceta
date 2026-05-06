using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using AppGenReceta.BE;
using AppGenReceta.BL;
using OfficeOpenXml;
using System.Web.Configuration;
using System.IO;

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
        public JsonResult GuardarAjustesEtapa3(string jsonData, string sid)
        {
            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                serializer.MaxJsonLength = int.MaxValue;
                List<E_InsumoCalculado> lista = serializer.Deserialize<List<E_InsumoCalculado>>(jsonData);

                // Persiste existentes (UPDATE) y extras (INSERT) en un solo método
                bl.GuardarYAvanzarPaso4(lista, sid);

                string urlPaso4 = Url.Action("GenerarSolicitud", "InsumoEstampado", new { sid = sid });
                return Json(new { ok = true, mensaje = "Ajustes guardados. Derivando a Paso 4.", redirect = urlPaso4 });
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
                return Json(new { ok = true, data = resultados.Select(x => new { id = x.CodigoInsumo, text = x.Descripcion, um = x.UnidadMedida }).ToList() }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, mensaje = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // ==========================================
        // PASO 4: GENERAR SOLICITUD DE REQUERIMIENTO
        // ==========================================
        [HttpGet]
        public ActionResult GenerarSolicitud(string sid)
        {
            if (string.IsNullOrWhiteSpace(sid)) return RedirectToAction("Index");

            E_SolicitudResumen resumen = bl.ObtenerResumenSolicitud(sid);
            ViewBag.SessionID = sid;
            return View(resumen);
        }

        [HttpPost]
        public JsonResult CargarGestionPedidos(string sid, string observaciones)
        {
            try
            {
                string usuario = Session["Usuario"] != null ? Session["Usuario"].ToString() : "2081"; // Hardcoded default per request
                string numReq = bl.CargarGestionPedidos(sid, observaciones, usuario);

                // Pasamos la observación por URL para que el Voucher la muestre de inmediato
                return Json(new { ok = true, mensaje = "Requerimiento " + numReq + " generado exitosamente.", redirect = Url.Action("Voucher", new { sid = sid, num = numReq, obs = observaciones }) });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, mensaje = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult Voucher(string sid, string num, string obs, bool print = false)
        {
            // Ya no redireccionamos si sid es nulo, para permitir ver históricos desde Mantenimiento
            E_SolicitudResumen resumen = bl.ObtenerResumenSolicitud(sid);
            if (resumen == null) resumen = new E_SolicitudResumen();
            resumen.Observacion = obs; // Seteamos la observación que viene del paso anterior
            
            ViewBag.SessionID = sid;
            ViewBag.NumRequerimiento = num;

            // Datos dinámicos desde BD (Previsualización)
            var detalleERP = bl.ObtenerVoucherDesdeERP("CN", num);
            ViewBag.DetalleERP = detalleERP;

            // Firmas
            ViewBag.Firma1 = WebConfigurationManager.AppSettings["Firma_Voucher_Responsable1"] ?? "Responsable 1";
            ViewBag.Firma2 = WebConfigurationManager.AppSettings["Firma_Voucher_Responsable2"] ?? "Responsable 2";
            ViewBag.Firma3 = WebConfigurationManager.AppSettings["Firma_Voucher_Responsable3"] ?? "Responsable 3";
            ViewBag.AutoPrint = print;

            return View(resumen);
        }

        [HttpGet]
        public ActionResult ExportarVoucherExcel(string num)
        {
            if (string.IsNullOrWhiteSpace(num)) return new EmptyResult();

            var detalleERP = bl.ObtenerVoucherDesdeERP("CN", num);

            string firma1 = WebConfigurationManager.AppSettings["Firma_Voucher_Responsable1"] ?? "";
            string firma2 = WebConfigurationManager.AppSettings["Firma_Voucher_Responsable2"] ?? "";
            string firma3 = WebConfigurationManager.AppSettings["Firma_Voucher_Responsable3"] ?? "";


            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Voucher");
                ws.View.ShowGridLines = false; // Ocultar líneas de cuadrícula para un reporte más limpio
                ws.PrinterSettings.FitToPage = true;
                ws.PrinterSettings.FitToWidth = 1;
                ws.PrinterSettings.FitToHeight = 0;
                ws.PrinterSettings.Orientation = eOrientation.Landscape;

                // Configuración de columnas (A como margen, B-G datos)
                ws.Column(1).Width = 2;  // A (Margen)
                ws.Column(2).Width = 14; // B (SECUENCIA)
                ws.Column(3).Width = 18; // C (CODIGO REPUESTO)
                ws.Column(4).Width = 55; // D (DESCRIPCION ITEM)
                ws.Column(5).Width = 20; // E (CODIGO FABRICACION)
                ws.Column(6).Width = 12; // F (CANTIDAD)
                ws.Column(7).Width = 15; // G (UNIDAD MEDIDA)

                // Estilo general
                ws.Cells.Style.Font.Name = "Arial";
                ws.Cells.Style.Font.Size = 10;

                // Datos de Cabecera
                string area = detalleERP.Count > 0 ? detalleERP[0].NomArea : "Confecciones";
                string motivo = detalleERP.Count > 0 ? detalleERP[0].DesMotivo : "COMPRA";
                string solicitante = detalleERP.Count > 0 ? detalleERP[0].Trabajador + " " + detalleERP[0].NomTrabajador : "";
                string obs = detalleERP.Count > 0 ? detalleERP[0].Observacion : "";
                string fecha = detalleERP.Count > 0 ? detalleERP[0].Fecha : DateTime.Now.ToString("dd/MM/yyyy");

                // Título
                ws.Cells["B2:G2"].Merge = true;
                ws.Cells["B2"].Value = "REQUERIMIENTO DE COMPRA NRO:" + num;
                ws.Cells["B2"].Style.Font.Bold = true;
                ws.Cells["B2"].Style.Font.Size = 12;
                ws.Cells["B2"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                ws.Row(2).Height = 25;

                // Cabecera Datos
                // Cabecera Datos
                ws.Cells["B4"].Value = "AREA:";
                ws.Cells["B4"].Style.Font.Bold = true;
                ws.Cells["C4"].Value = area;

                ws.Cells["B5"].Value = "MOTIVO:";
                ws.Cells["B5"].Style.Font.Bold = true;
                ws.Cells["C5"].Value = motivo;

                ws.Cells["B6"].Value = "SOLICITANTE:";
                ws.Cells["B6"].Style.Font.Bold = true;
                ws.Cells["C6"].Value = solicitante;

                ws.Cells["B7"].Value = "OBSERVACION:";
                ws.Cells["B7"].Style.Font.Bold = true;
                ws.Cells["C7"].Value = obs;

                ws.Cells["E4"].Value = "FECHA:";
                ws.Cells["E4"].Style.Font.Bold = true;
                ws.Cells["E4"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                ws.Cells["F4"].Value = fecha;

                // Encabezados de Tabla
                int row = 9;
                ws.Cells[row, 2].Value = "SECUENCIA";
                ws.Cells[row, 3].Value = "CODIGO REPUESTO";
                ws.Cells[row, 4].Value = "DESCRIPCION ITEM";
                ws.Cells[row, 5].Value = "CODIGO FABRICACION";
                ws.Cells[row, 6].Value = "CANTIDAD";
                ws.Cells[row, 7].Value = "UNIDAD MEDIDA";

                var headerRng = ws.Cells[row, 2, row, 7];
                headerRng.Style.Font.Bold = true;
                headerRng.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                headerRng.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                headerRng.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                headerRng.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                headerRng.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                headerRng.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                headerRng.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                row++;

                // Detalle
                foreach (var item in detalleERP)
                {
                    ws.Cells[row, 2].Value = item.Secuencia;
                    ws.Cells[row, 2].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                    
                    ws.Cells[row, 3].Value = string.IsNullOrEmpty(item.CodRepuesto) ? item.CodItem : item.CodRepuesto;
                    
                    ws.Cells[row, 4].Value = item.DesItem;
                    
                    ws.Cells[row, 5].Value = item.CodFabricacion;
                    
                    ws.Cells[row, 6].Value = item.Cantidad;
                    ws.Cells[row, 6].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                    
                    ws.Cells[row, 7].Value = item.DesUniMed;

                    row++;
                    if (!string.IsNullOrEmpty(item.UltimosPrecios))
                    {
                        ws.Cells[row, 4].Value = "Ultimos Precios : " + item.UltimosPrecios;
                        ws.Cells[row, 4].Style.Font.Size = 8;
                        ws.Cells[row, 4].Style.Font.Italic = true;
                        ws.Cells[row, 4].Style.WrapText = true;
                        ws.Row(row).Height = 30; // Dar más espacio para precios largos
                        row++;
                    }
                }

                // Aplicar bordes a toda la tabla de datos
                var tableRange = ws.Cells[9, 2, row - 1, 7];
                tableRange.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                tableRange.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                tableRange.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                tableRange.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                row += 4; // Espacio para firmas

                // Filas para las cajas de firma (Gris)
                int sigBoxRowStart = row;
                int sigBoxRowEnd = row + 3;

                // Configurar cajas grises en C, E, F
                int[] sigCols = { 3, 5, 6 }; // Columnas donde van las cajas
                foreach (int col in sigCols)
                {
                    var box = ws.Cells[sigBoxRowStart, col, sigBoxRowEnd, col];
                    box.Merge = true; // Para evitar líneas horizontales internas
                    box.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    box.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(242, 242, 242));
                    box.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    box.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    box.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    box.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                }

                row = sigBoxRowEnd + 1;

                // Nombres debajo de las cajas
                string trabajador = detalleERP.Count > 0 ? (detalleERP[0].Trabajador + " " + detalleERP[0].NomTrabajador).Trim() : "";
                ws.Cells[row, 2].Value = "Elaborado por " + trabajador;
                ws.Cells[row, 2].Style.Font.Size = 9;
                ws.Cells[row, 2].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;

                // Nombres de firmantes con línea debajo
                ws.Cells[row, 3].Value = "Sr. " + firma1;
                ws.Cells[row, 3].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                ws.Cells[row, 3].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                ws.Cells[row, 5].Value = "Ing. " + firma2;
                ws.Cells[row, 5].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                ws.Cells[row, 5].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                ws.Cells[row, 6, row, 7].Merge = true;
                ws.Cells[row, 6].Value = "Ing. " + firma3;
                ws.Cells[row, 6].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                ws.Cells[row, 6].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                // Autoajustar columnas al final para que los precios se vean bien
                ws.Column(4).Style.WrapText = true;
                ws.Column(4).Width = 70; // Descripción ancha para evitar solapamiento

                var stream = new MemoryStream();
                package.SaveAs(stream);
                string fileName = $"rptVoucher_Requerimiento{num}.xlsx";
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                stream.Position = 0;
                return File(stream, contentType, fileName);
            }
        }

        // --- MANTENIMIENTO DE SOLICITUDES ---
        public ActionResult Mantenimiento()
        {
            return View();
        }

        [HttpPost]
        public JsonResult ListarSolicitudes(string filtro, string desde, string hasta)
        {
            try
            {
                var lista = bl.ListarRequerimientosCabecera(filtro, desde, hasta);
                return Json(new { success = true, data = lista });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ListarDetalles(string numReq)
        {
            try
            {
                var detalles = bl.ObtenerVoucherDesdeERP("CN", numReq);
                return Json(new { success = true, data = detalles });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarSolicitud(string numReq)
        {
            try
            {
                bool res = bl.EliminarRequerimientoCabecera(numReq);
                return Json(new { success = res });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult MantenimientoDetalle(string accion, string numReq, int secuencia, string codItem, string codFab, decimal cantidad, string um)
        {
            try
            {
                char cAccion = string.IsNullOrEmpty(accion) ? 'I' : accion[0];
                int nReq = string.IsNullOrEmpty(numReq) ? 0 : Convert.ToInt32(numReq);
                bool res = bl.MantenimientoRequerimientoDetalle(cAccion, nReq, secuencia, codItem, codFab, cantidad, um);
                return Json(new { success = res });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
