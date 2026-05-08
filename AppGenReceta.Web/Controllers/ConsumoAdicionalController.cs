using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AppGenReceta.BE;
using AppGenReceta.BL;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.IO;

namespace AppGenReceta.Web.Controllers
{
    public class ConsumoAdicionalController : Controller
    {
        private ConsumoAdicionalBL bl = new ConsumoAdicionalBL();

        [HttpGet]
        public ActionResult Index(string sid)
        {
            ViewBag.Usuario = Session["NombreUsuario"];
            ViewBag.Correo = Session["CorreoUsuario"];
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

        [HttpPost]
        public JsonResult Eliminar(int numReq)
        {
            try
            {
                bl.EliminarCabecera(numReq);
                return Json(new { success = true, message = "Requerimiento eliminado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarDetalle(int numReq, int secu)
        {
            try
            {
                bl.EliminarDetalle(numReq, secu);
                return Json(new { success = true, message = "Insumo eliminado del requerimiento." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ExportarExcel(int numReq)
        {
            try
            {
                var filas = bl.ObtenerVoucherQyc(numReq);

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Qyc");
                    ws.View.ShowGridLines = false;
                    ws.PrinterSettings.FitToPage   = true;
                    ws.PrinterSettings.FitToWidth  = 1;
                    ws.PrinterSettings.FitToHeight = 0;
                    ws.PrinterSettings.Orientation = eOrientation.Landscape;

                    // ── Columnas (A=margen, B-H=datos) ──
                    ws.Column(1).Width = 2;   // A margen
                    ws.Column(2).Width = 10;  // B Partida
                    ws.Column(3).Width = 13;  // C CODIGO
                    ws.Column(4).Width = 40;  // D DESCRIPCION
                    ws.Column(5).Width = 8;   // E UN
                    ws.Column(6).Width = 13;  // F CANTIDAD
                    ws.Column(7).Width = 8;   // G LOTE
                    ws.Column(8).Width = 5;   // H aux para merge

                    ws.Cells.Style.Font.Name = "Arial";
                    ws.Cells.Style.Font.Size = 10;

                    // ── Cabecera de datos ──
                    string partida   = "Kg Crudo";                              // Campo fijo solicitado (amarillo)
                    string kgsCrudo  = filas.Count > 0 ? filas[0].KgsCrudo      : "";
                    string maquina   = filas.Count > 0 ? filas[0].Maquina       : "";
                    string motivo    = filas.Count > 0 ? filas[0].Motivo        : "";
                    string fecha     = filas.Count > 0 ? filas[0].Fecha         : "";
                    string obs       = filas.Count > 0 ? filas[0].Observaciones : "";
                    string op        = filas.Count > 0 ? filas[0].OP            : "";
                    string cliente   = filas.Count > 0 ? filas[0].Cliente       : "";

                    // Partida visual = "Kg Crudo  [valor] kgs"
                    string partidaDisplay = string.IsNullOrEmpty(kgsCrudo)
                        ? partida
                        : partida + "  " + kgsCrudo + " kgs";

                    // ── Fila 3: Título centrado en azul ──
                    ws.Cells["B3:H3"].Merge = true;
                    ws.Cells["B3"].Value = "REQUERIMIENTO DE QYC : " + numReq;
                    ws.Cells["B3"].Style.Font.Bold  = true;
                    ws.Cells["B3"].Style.Font.Size  = 14;
                    ws.Cells["B3"].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                    ws.Cells["B3"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    ws.Row(3).Height = 28;

                    // ── Color celeste para etiquetas de cabecera ──
                    var lblColor = System.Drawing.Color.LightCyan;
                    Action<ExcelRange> setLabel = r =>
                    {
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(lblColor);
                        r.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                    };

                    // Fila 5: PARTIDA | valor | MAQUINA | valor
                    setLabel(ws.Cells["B5"]); ws.Cells["B5"].Value = "PARTIDA";
                    ws.Cells["C5:D5"].Merge = true; ws.Cells["C5"].Value = partidaDisplay;
                    setLabel(ws.Cells["E5"]); ws.Cells["E5"].Value = "MAQUINA";
                    ws.Cells["F5:H5"].Merge = true; ws.Cells["F5"].Value = maquina;

                    // Fila 6: MOTIVO | valor | FECHA | valor
                    setLabel(ws.Cells["B6"]); ws.Cells["B6"].Value = "MOTIVO";
                    ws.Cells["C6:D6"].Merge = true; ws.Cells["C6"].Value = motivo;
                    setLabel(ws.Cells["E6"]); ws.Cells["E6"].Value = "FECHA";
                    ws.Cells["F6:H6"].Merge = true; ws.Cells["F6"].Value = fecha;

                    // Fila 7: OBSERVACIONES | valor
                    setLabel(ws.Cells["B7"]); ws.Cells["B7"].Value = "OBSERVACIONES";
                    ws.Cells["C7:D7"].Merge = true; ws.Cells["C7"].Value = obs;

                    // Fila 8: OP | valor
                    setLabel(ws.Cells["E8"]); ws.Cells["E8"].Value = "OP";
                    ws.Cells["F8:H8"].Merge = true; ws.Cells["F8"].Value = op;

                    // Fila 9: CLIENTE / OP | valor
                    setLabel(ws.Cells["E9"]); ws.Cells["E9"].Value = "CLIENTE / OP";
                    ws.Cells["F9:H9"].Merge = true; ws.Cells["F9"].Value = cliente;

                    // ── Encabezados de tabla (fila 11) ──
                    int row = 11;
                    string[] headers = { "Partida", "CODIGO", "DESCRIPCION", "UN", "CANTIDAD", "LOTE" };
                    int[] hCols     = {     2,        3,           4,          5,       6,         7   };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        ws.Cells[row, hCols[i]].Value = headers[i];
                    }
                    var hdrRng = ws.Cells[row, 2, row, 7];
                    hdrRng.Style.Font.Bold = true;
                    hdrRng.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    hdrRng.Style.Fill.BackgroundColor.SetColor(lblColor);
                    hdrRng.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    hdrRng.Style.Border.Top.Style    = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    hdrRng.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    hdrRng.Style.Border.Left.Style   = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    hdrRng.Style.Border.Right.Style  = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                    row++;

                    // ── Detalle ──
                    foreach (var d in filas)
                    {
                        ws.Cells[row, 2].Value = d.Partida;
                        ws.Cells[row, 3].Value = d.CodItem;
                        ws.Cells[row, 4].Value = d.DesItem;
                        ws.Cells[row, 5].Value = d.UnidadMedida;
                        ws.Cells[row, 6].Value = d.Cantidad;
                        ws.Cells[row, 6].Style.Numberformat.Format = "0.0000";
                        ws.Cells[row, 6].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                        ws.Cells[row, 7].Value = d.Lote > 0 ? (object)d.Lote : "";
                        row++;
                    }

                    // Bordes de toda la tabla
                    var tblRng = ws.Cells[11, 2, row - 1, 7];
                    tblRng.Style.Border.Top.Style    = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    tblRng.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    tblRng.Style.Border.Left.Style   = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    tblRng.Style.Border.Right.Style  = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                    // ── Pie de página / Firmas ──
                    row += 4;
                    // Líneas de firma
                    ws.Cells[row, 2, row, 3].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    ws.Cells[row, 5, row, 7].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    row++;
                    ws.Cells[row, 2, row, 3].Merge = true;
                    ws.Cells[row, 2].Value = "VBO";
                    ws.Cells[row, 2].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    ws.Cells[row, 5, row, 7].Merge = true;
                    ws.Cells[row, 5].Value = "SUPERVISOR";
                    ws.Cells[row, 5].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;
                    return File(stream,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "VoucherQYC_" + numReq + ".xlsx");
                }
            }
            catch (Exception ex)
            {
                return Content("Error al generar Excel: " + ex.Message);
            }
        }

        [HttpGet]
        public ActionResult Imprimir(int numReq)
        {
            if (numReq <= 0) return RedirectToAction("Index");

            try
            {
                var filas = bl.ObtenerVoucherQyc(numReq);
                if (filas == null || filas.Count == 0) return RedirectToAction("Index");

                ViewBag.Filas    = filas;
                ViewBag.NumReq   = numReq;
                return View("VoucherPDF");
            }
            catch (Exception ex)
            {
                return Content("Error al generar el voucher: " + ex.Message);
            }
        }
    }
}
