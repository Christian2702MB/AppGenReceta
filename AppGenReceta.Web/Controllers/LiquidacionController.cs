using System;
using System.Collections.Generic;
using System.Linq;
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
                
                // Validar que la receta origen tenga Combo e Item
                VisitaBL visitaBL = new VisitaBL();
                VisitaBE recetaOrigen = visitaBL.ObtenerRecetaPorId(idReceta);
                if (recetaOrigen == null)
                {
                    return Json(new { success = false, message = "No se encontró la receta origen." });
                }
                if (string.IsNullOrEmpty(recetaOrigen.ComboCabecera) || string.IsNullOrEmpty(recetaOrigen.Item))
                {
                    return Json(new { success = false, message = "La receta origen no tiene Combo o Item asignado. Por favor asigne estos datos en laboratorio antes de generar la fórmula." });
                }

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

        // Clase de ayuda temporal para agrupar colores por NP
        private class NpGroup
        {
            public string Fecha { get; set; }
            public string Cliente { get; set; }
            public string Estilo { get; set; }
            public string Np { get; set; }
            public System.Collections.Generic.List<string> Colores { get; set; } = new System.Collections.Generic.List<string>();
            public System.Collections.Generic.List<string> DynamicColumnNames { get; set; } = new System.Collections.Generic.List<string>();
        }

        [HttpGet]
        public ActionResult ExportarMatrizExcel()
        {
            if (Session["NombreUsuario"] == null)
                return RedirectToAction("Index", "Home");

            try
            {
                var dt = _stockReqBl.ObtenerMatrizCruzadaConsumos();

                using (var package = new OfficeOpenXml.ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Matriz Consumos");

                    // Construir cabeceras estáticas
                    ws.Cells["C1"].Value = "FECHA";
                    ws.Cells["C2"].Value = "CLIENTE";
                    ws.Cells["C3"].Value = "ESTILO";
                    ws.Cells["C4"].Value = "NP";

                    ws.Cells["C1:C4"].Style.Font.Bold = true;
                    ws.Cells["C1:C4"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    ws.Cells["C1:C4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 192, 0));
                    ws.Cells["C1:C4"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    ws.Cells["C1:C4"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                    ws.Cells["A5"].Value = "CÓDIGO";
                    ws.Cells["B5"].Value = "INSUMO";
                    ws.Cells["C5"].Value = "STOCK REAL";
                    
                    ws.Cells["A5:B5"].Style.Font.Bold = true;
                    ws.Cells["C5"].Style.Font.Bold = true;
                    ws.Cells["C5"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    ws.Cells["C5"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                    ws.Cells["C5"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    ws.Cells["C5"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                    // Procesar columnas dinámicas
                    var fixedCols = new System.Collections.Generic.List<string> { "Código", "Insumo", "Stock Real" };
                    var dynamicCols = new System.Collections.Generic.List<string>();

                    foreach (System.Data.DataColumn col in dt.Columns)
                    {
                        if (!fixedCols.Contains(col.ColumnName))
                            dynamicCols.Add(col.ColumnName);
                    }

                    // Agrupar columnas dinámicas por NP y Color
                    var npGroups = new System.Collections.Generic.Dictionary<string, NpGroup>();

                    foreach (var dCol in dynamicCols)
                    {
                        var parts = dCol.Split('|');
                        string fecha = parts.Length > 0 ? parts[0].Trim() : "";
                        string cliente = parts.Length > 1 ? parts[1].Trim() : "";
                        string estilo = parts.Length > 2 ? parts[2].Trim() : "";
                        string np = parts.Length > 3 ? parts[3].Trim() : "";
                        string color = parts.Length > 4 ? parts[4].Trim() : "SIN COLOR";

                        if (!npGroups.ContainsKey(np)) {
                            npGroups[np] = new NpGroup { Fecha = fecha, Cliente = cliente, Estilo = estilo, Np = np };
                        }
                        npGroups[np].Colores.Add(color);
                        npGroups[np].DynamicColumnNames.Add(dCol);
                    }

                    int colIndex = 4;
                    var pastelColors = new System.Drawing.Color[] {
                        System.Drawing.Color.FromArgb(232, 245, 233), // Green
                        System.Drawing.Color.FromArgb(227, 242, 253), // Blue
                        System.Drawing.Color.FromArgb(255, 235, 238), // Red
                        System.Drawing.Color.FromArgb(255, 243, 224), // Orange
                        System.Drawing.Color.FromArgb(243, 229, 245)  // Purple
                    };
                    int colorIndex = 0;

                    foreach (var np in npGroups.Values)
                    {
                        int startCol = colIndex;
                        var currentColor = pastelColors[colorIndex % pastelColors.Length];
                        colorIndex++;
                        
                        // Fila 5: Columna Total Consumo
                        ws.Cells[5, colIndex].Value = "CONSUMO";
                        ws.Cells[5, colIndex].Style.Font.Bold = true;
                        ws.Cells[5, colIndex].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        ws.Cells[5, colIndex].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        ws.Cells[5, colIndex].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                        ws.Cells[5, colIndex].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                        colIndex++;

                        // Fila 5: Columnas por Color
                        foreach (var color in np.Colores)
                        {
                            ws.Cells[5, colIndex].Value = color;
                            ws.Cells[5, colIndex].Style.Font.Bold = true;
                            ws.Cells[5, colIndex].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                            colIndex++;
                        }

                        int endCol = colIndex - 1;

                        // Merge para Cabeceras Nivel 1 (Fecha, Cliente, Estilo, NP)
                        if (startCol < endCol) {
                            ws.Cells[1, startCol, 1, endCol].Merge = true;
                            ws.Cells[2, startCol, 2, endCol].Merge = true;
                            ws.Cells[3, startCol, 3, endCol].Merge = true;
                            ws.Cells[4, startCol, 4, endCol].Merge = true;
                        }
                        
                        ws.Cells[1, startCol].Value = np.Fecha;
                        ws.Cells[2, startCol].Value = np.Cliente;
                        ws.Cells[3, startCol].Value = np.Estilo;
                        ws.Cells[4, startCol].Value = np.Np;

                        ws.Cells[1, startCol, 4, endCol].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        ws.Cells[1, startCol, 4, endCol].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        ws.Cells[1, startCol, 5, endCol].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        ws.Cells[1, startCol, 5, endCol].Style.Fill.BackgroundColor.SetColor(currentColor);
                        
                        // Fix gray color for Consumo so it doesn't get overridden by pastel
                        ws.Cells[5, startCol].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        ws.Cells[5, startCol].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(220, 220, 220));
                    }

                    // Agregar datos
                    int rowIndex = 6;
                    foreach (System.Data.DataRow row in dt.Rows)
                    {
                        ws.Cells[rowIndex, 1].Value = row["Código"]?.ToString() ?? "";
                        ws.Cells[rowIndex, 2].Value = row["Insumo"]?.ToString() ?? "";
                        
                        double stockReal = 0;
                        if (row["Stock Real"] != DBNull.Value)
                            double.TryParse(row["Stock Real"].ToString(), out stockReal);
                        
                        ws.Cells[rowIndex, 3].Value = stockReal;
                        ws.Cells[rowIndex, 3].Style.Numberformat.Format = "#,##0.00";
                        ws.Cells[rowIndex, 3].Style.Font.Bold = true;

                        int cIdx = 4;
                        foreach (var np in npGroups.Values)
                        {
                            double totalNp = 0;
                            int totalColIdx = cIdx;
                            cIdx++; // Avanzar índice para los colores

                            // Escribir datos de colores y sumar el total
                            foreach (var dCol in np.DynamicColumnNames)
                            {
                                double val = 0;
                                if (row[dCol] != DBNull.Value) double.TryParse(row[dCol].ToString(), out val);
                                
                                totalNp += val;

                                if (val > 0)
                                {
                                    ws.Cells[rowIndex, cIdx].Value = val;
                                    ws.Cells[rowIndex, cIdx].Style.Numberformat.Format = "#,##0.00";
                                }
                                else
                                {
                                    ws.Cells[rowIndex, cIdx].Value = "-";
                                    ws.Cells[rowIndex, cIdx].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                                }
                                cIdx++;
                            }

                            // Escribir Total NP
                            if (totalNp > 0)
                            {
                                ws.Cells[rowIndex, totalColIdx].Value = totalNp;
                                ws.Cells[rowIndex, totalColIdx].Style.Numberformat.Format = "#,##0.00";
                                ws.Cells[rowIndex, totalColIdx].Style.Font.Bold = true;
                                ws.Cells[rowIndex, totalColIdx].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                ws.Cells[rowIndex, totalColIdx].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(220, 220, 220));
                            }
                            else
                            {
                                ws.Cells[rowIndex, totalColIdx].Value = "-";
                                ws.Cells[rowIndex, totalColIdx].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                                ws.Cells[rowIndex, totalColIdx].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                ws.Cells[rowIndex, totalColIdx].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(220, 220, 220));
                            }
                        }
                        rowIndex++;
                    }

                    // AutoFit columnas
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();

                    // Aplicar bordes a toda la tabla
                    var dataRange = ws.Cells[1, 1, rowIndex - 1, colIndex - 1];
                    dataRange.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                    // Quitar bordes de A1:B4 para igualar Image 2
                    ws.Cells["A1:B4"].Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.None;
                    ws.Cells["A1:B4"].Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.None;
                    ws.Cells["A1:B4"].Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.None;
                    ws.Cells["A1:B4"].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.None;
                    ws.Cells["A1:B4"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    ws.Cells["A1:B4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.White);

                    var stream = new System.IO.MemoryStream(package.GetAsByteArray());
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Matriz_Consumos_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMsg"] = "Error al exportar a Excel: " + ex.Message;
                return RedirectToAction("StockActual");
            }
        }

        [HttpGet]
        public ActionResult ExportarBalanceExcel()
        {
            if (Session["NombreUsuario"] == null)
                return RedirectToAction("Index", "Home");

            try
            {
                var lista = _stockReqBl.ListarStockActual();

                using (var package = new OfficeOpenXml.ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Balance General");

                    // Construir cabeceras de Nivel 1 (Superiores)
                    ws.Cells["A1:C1"].Merge = true;
                    ws.Cells["A1"].Value = "DATOS BASE";
                    ws.Cells["A1:C1"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    ws.Cells["A1:C1"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(244, 248, 250)); // Azul clarito

                    ws.Cells["D1"].Value = "INGRESOS (+)";
                    ws.Cells["D1"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    ws.Cells["D1"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(232, 245, 233)); // Verde clarito

                    ws.Cells["E1"].Value = "SALIDAS (-)";
                    ws.Cells["E1"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    ws.Cells["E1"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 235, 238)); // Rojo clarito

                    ws.Cells["F1"].Value = "BALANCE FINAL";
                    ws.Cells["F1"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    ws.Cells["F1"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(227, 242, 253)); // Azul más oscuro

                    // Construir cabeceras de Nivel 2
                    ws.Cells["A2"].Value = "CÓDIGO INSUMO";
                    ws.Cells["B2"].Value = "DESCRIPCIÓN";
                    ws.Cells["C2"].Value = "U.M.";
                    ws.Cells["D2"].Value = "STOCK INICIAL / ENTRADAS";
                    ws.Cells["E2"].Value = "CONSUMIDO / AJUSTADO";
                    ws.Cells["F2"].Value = "STOCK ACTUAL";

                    // Estilo general cabeceras
                    ws.Cells["A1:F2"].Style.Font.Bold = true;
                    ws.Cells["A1:F2"].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(0, 45, 114)); // Azul Corporativo oscuro
                    ws.Cells["A1:F2"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    ws.Cells["A1:F2"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    
                    // Bordes de cabeceras
                    var borderStyle = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    ws.Cells["A1:F2"].Style.Border.Top.Style = borderStyle;
                    ws.Cells["A1:F2"].Style.Border.Left.Style = borderStyle;
                    ws.Cells["A1:F2"].Style.Border.Right.Style = borderStyle;
                    ws.Cells["A1:F2"].Style.Border.Bottom.Style = borderStyle;

                    // Llenar datos
                    int rowIndex = 3;
                    foreach (var item in lista)
                    {
                        ws.Cells[rowIndex, 1].Value = item.CodInsumo;
                        ws.Cells[rowIndex, 1].Style.Font.Bold = true;
                        ws.Cells[rowIndex, 1].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(25, 118, 210)); // Azul Link
                        ws.Cells[rowIndex, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                        ws.Cells[rowIndex, 2].Value = item.Descripcion;
                        
                        ws.Cells[rowIndex, 3].Value = item.UnidadMedida;
                        ws.Cells[rowIndex, 3].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                        // Entradas (Multilínea)
                        decimal totIngresos = item.TotalIngresosNeto + item.AjustesManualesIngreso;
                        ws.Cells[rowIndex, 4].IsRichText = true;
                        var rtInTotal = ws.Cells[rowIndex, 4].RichText.Add(totIngresos.ToString("N2"));
                        rtInTotal.Bold = true;
                        rtInTotal.Size = 12;

                        string ingresosStr = "";
                        if (item.DevolucionesOperativo > 0)
                        {
                            ingresosStr = $"\nIni: {item.InicialNeto.ToString("N2")} ({item.StockInicialOriginal.ToString("N2")} + {item.DevolucionesOperativo.ToString("N2")} Dev.Ope)\nRec: {item.RecibidoNeto.ToString("N2")} ({item.StockRecibidoOriginal.ToString("N2")} - {item.DevolucionesOperativo.ToString("N2")} Dev.Ope)";
                        }
                        else
                        {
                            ingresosStr = $"\nInicial: {item.InicialNeto.ToString("N2")}\nRecibido: {item.RecibidoNeto.ToString("N2")}";
                        }
                        
                        if (item.AjustesManualesIngreso > 0)
                        {
                            ingresosStr += $"\nAj. Manual: {item.AjustesManualesIngreso.ToString("N2")}";
                        }

                        var rtInDet = ws.Cells[rowIndex, 4].RichText.Add(ingresosStr);
                        rtInDet.Bold = false;
                        rtInDet.Size = 11;
                        rtInDet.Color = System.Drawing.Color.DimGray;
                        ws.Cells[rowIndex, 4].Style.WrapText = true;
                        ws.Cells[rowIndex, 4].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                        
                        // Salidas (Multilínea)
                        decimal totSalidas = item.ConsumosTotales + item.AjustesTotales + item.DevolucionesCentral + item.AjustesManualesSalida;
                        ws.Cells[rowIndex, 5].IsRichText = true;
                        var rtOutTotal = ws.Cells[rowIndex, 5].RichText.Add(totSalidas.ToString("N2"));
                        rtOutTotal.Bold = true;
                        rtOutTotal.Size = 12;
                        if (totSalidas > 0) rtOutTotal.Color = System.Drawing.ColorTranslator.FromHtml("#d32f2f");

                        string salidasStr = $"\nConsumos: {item.ConsumosTotales.ToString("N2")} (Pro: {item.ConsumosProduccion.ToString("N2")} | Lab: {item.ConsumosDesarrollo.ToString("N2")})";
                        if (item.AjustesTotales > 0 || item.AjustesManualesSalida > 0)
                        {
                            salidasStr += $"\nAjustes: {(item.AjustesTotales + item.AjustesManualesSalida).ToString("N2")} (NP: {item.AjustesTotales.ToString("N2")} | Man: {item.AjustesManualesSalida.ToString("N2")})";
                        }
                        if (item.DevolucionesCentral > 0)
                        {
                            salidasStr += $"\nDev. Central: {item.DevolucionesCentral.ToString("N2")}";
                        }

                        var rtOutDet = ws.Cells[rowIndex, 5].RichText.Add(salidasStr);
                        rtOutDet.Bold = false;
                        rtOutDet.Size = 11;
                        if (totSalidas > 0) rtOutDet.Color = System.Drawing.ColorTranslator.FromHtml("#d32f2f");
                        ws.Cells[rowIndex, 5].Style.WrapText = true;
                        ws.Cells[rowIndex, 5].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;

                        // Stock Actual
                        ws.Cells[rowIndex, 6].IsRichText = true;
                        var rtStockTotal = ws.Cells[rowIndex, 6].RichText.Add(item.StockActual.ToString("N2"));
                        rtStockTotal.Bold = true;
                        rtStockTotal.Size = 12;
                        if (item.StockActual > 0) rtStockTotal.Color = System.Drawing.ColorTranslator.FromHtml("#2e7d32");
                        else if (item.StockActual < 0) rtStockTotal.Color = System.Drawing.ColorTranslator.FromHtml("#d32f2f");
                        
                        string stockActualStr = $"\nDisponible: {item.StockDisponible.ToString("N2")}\nPor Liquidar: {item.StockPorLiquidar.ToString("N2")}";
                        var rtStockDet = ws.Cells[rowIndex, 6].RichText.Add(stockActualStr);
                        rtStockDet.Bold = false;
                        rtStockDet.Size = 11;
                        if (item.StockActual > 0) rtStockDet.Color = System.Drawing.ColorTranslator.FromHtml("#2e7d32");
                        else if (item.StockActual < 0) rtStockDet.Color = System.Drawing.ColorTranslator.FromHtml("#d32f2f");

                        ws.Cells[rowIndex, 6].Style.WrapText = true;
                        ws.Cells[rowIndex, 6].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;

                        // Alineación vertical de todas las celdas de la fila
                        ws.Cells[rowIndex, 1, rowIndex, 6].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        
                        // Bordes
                        ws.Cells[rowIndex, 1, rowIndex, 6].Style.Border.Top.Style = borderStyle;
                        ws.Cells[rowIndex, 1, rowIndex, 6].Style.Border.Left.Style = borderStyle;
                        ws.Cells[rowIndex, 1, rowIndex, 6].Style.Border.Right.Style = borderStyle;
                        ws.Cells[rowIndex, 1, rowIndex, 6].Style.Border.Bottom.Style = borderStyle;

                        rowIndex++;
                    }

                    // Ajustar anchos
                    ws.Column(1).Width = 18;
                    ws.Column(2).Width = 40;
                    ws.Column(3).Width = 8;
                    ws.Column(4).Width = 40;
                    ws.Column(5).Width = 40;
                    ws.Column(6).Width = 35;

                    var stream = new System.IO.MemoryStream(package.GetAsByteArray());
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Balance_General_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMsg"] = "Error al exportar a Excel: " + ex.Message;
                return RedirectToAction("StockActual");
            }
        }

        [HttpPost]
        public JsonResult RegistrarAjusteDirecto(string codInsumo, string tipoAjuste, decimal cantidad, string um, string motivo)
        {
            if (Session["NombreUsuario"] == null)
                return Json(new { error = true, message = "Sesión expirada" });

            try
            {
                if (cantidad <= 0)
                    return Json(new { error = true, message = "La cantidad debe ser mayor a 0." });
                    
                if (string.IsNullOrWhiteSpace(motivo) || motivo.Length < 5)
                    return Json(new { error = true, message = "El motivo debe ser detallado y obligatorio." });

                string fuente = tipoAjuste == "Ingreso" ? "Ingreso" : "Salida";
                
                // Conversión de KG a Gramos para la base de datos
                decimal cantidadDb = cantidad;
                if (!string.IsNullOrEmpty(um) && um.ToUpper() == "KG")
                {
                    cantidadDb = cantidad * 1000;
                }

                var ope = new LIQ_OperacionBE
                {
                    NP = "STOCK-DIR", 
                    CodInsumo = codInsumo,
                    TipoOperacion = "Ajuste Directo",
                    Cantidad = cantidadDb,
                    Motivo = motivo,
                    FuenteConsumo = fuente,
                    Usuario = Session["NombreUsuario"].ToString()
                };

                bool success = bl.RegistrarOperacion(ope);

                if (success)
                    return Json(new { success = true });
                else
                    return Json(new { error = true, message = "No se pudo registrar el ajuste en la BD." });
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerMatrizCruzada()
        {
            try
            {
                var dt = _stockReqBl.ObtenerMatrizCruzadaConsumos();
                var list = new List<Dictionary<string, object>>();
                foreach (System.Data.DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (System.Data.DataColumn col in dt.Columns)
                    {
                        dict[col.ColumnName] = row[col];
                    }
                    list.Add(dict);
                }
                return Json(new { success = true, data = list, columns = dt.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName).ToList() }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerAuditoriaAnulaciones()
        {
            try
            {
                // Validación estricta de seguridad
                if (Session["RolUsuario"] == null || (Session["RolUsuario"].ToString() != "Administrador" && Session["RolUsuario"].ToString() != "Liquidador"))
                    return Json(new { success = false, message = "Acceso denegado. Solo administradores y liquidadores pueden ver auditorías." }, JsonRequestBehavior.AllowGet);

                var lista = bl.ListarAuditoriaAnulaciones();
                return Json(new { success = true, data = lista }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
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
        public JsonResult ObtenerVistaPreviaReq(int numReq, string codOrdPro = null)
        {
            try
            {
                var detalles = _stockReqBl.ObtenerDetalleRequerimiento(numReq);
                List<string> versiones = new List<string>();
                List<string> items = new List<string>();
                if (!string.IsNullOrEmpty(codOrdPro))
                {
                    versiones = bl.ObtenerVersionesNP(codOrdPro);
                    items = bl.ObtenerItemsActivosNP(codOrdPro);
                }
                return Json(new { success = true, data = detalles, versiones = versiones, items = items });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ConfirmarRecepcion(int numRequerimiento, string codOrdPro, string motivo, string item = null)
        {
            try
            {
                if (Session["NombreUsuario"] == null)
                    return Json(new { success = false, message = "Sesión expirada" });

                string usuario = Session["NombreUsuario"].ToString();
                string msj = _stockReqBl.ConfirmarRecepcion(numRequerimiento, codOrdPro, motivo, usuario, item);
                return Json(new { success = true, message = msj });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RegistrarCargaInicial(string codInsumo, string descripcion, string unidadMedida, decimal pesoGramos, string npDirigida = null)
        {
            try
            {
                if (Session["NombreUsuario"] == null)
                    return Json(new { success = false, message = "Sesión expirada" });

                string usuario = Session["NombreUsuario"].ToString();
                string msj = _stockReqBl.RegistrarCargaInicial(codInsumo, descripcion, unidadMedida, pesoGramos, usuario, npDirigida);
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
        public JsonResult ObtenerMenuNPs(string estado)
        {
            try
            {
                var data = bl.ObtenerMenuNPs(estado);
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerLiquidacionesConsolidadas(string estado, string npFiltro = null, string itemFiltro = null)
        {
            try
            {
                var data = bl.ObtenerLiquidacionesConsolidadas(estado, npFiltro, itemFiltro);
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
                var data = bl.ObtenerSaldosPopup(np, codInsumo, null);
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

        [HttpGet]
        public JsonResult ObtenerInsumosSobrantesNP(string np)
        {
            try
            {
                // Obtenemos los consolidados en estado Terminado (que son los que pasan a devolución)
                var consolidados = bl.ObtenerLiquidacionesConsolidadas("Terminado");
                var npData = consolidados.FirstOrDefault(x => x.NP == np);
                
                if (npData == null)
                    return Json(new { success = false, message = "NP no encontrada o no está en estado Terminado." }, JsonRequestBehavior.AllowGet);

                var insumosSobrantes = npData.Colores
                    .SelectMany(c => c.Insumos)
                    .GroupBy(i => new { i.Codigo, i.Nombre, i.UM })
                    .Select(g => new {
                        CodInsumo = g.Key.Codigo,
                        Descripcion = g.Key.Nombre,
                        UM = g.Key.UM,
                        SaldoDevolver = g.Sum(x => x.SaldoSolicitud)
                    })
                    .Where(x => x.SaldoDevolver > 0)
                    .ToList();

                return Json(new { success = true, data = insumosSobrantes }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerNPsSinRecepcion()
        {
            try
            {
                var data = bl.ObtenerNPsSinRecepcion();
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerLiquidacionesActivasCombo()
        {
            try
            {
                var data = bl.ObtenerLiquidacionesActivasCombo();
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult HabilitarNP(string np)
        {
            try
            {
                if (Session["NombreUsuario"] == null)
                    return Json(new { success = false, message = "Sesión expirada" });

                string usuario = Session["NombreUsuario"].ToString();
                string msj = bl.CrearRecepcionBlanco(np, usuario);
                
                if (msj.StartsWith("OK|"))
                    return Json(new { success = true, message = msj.Substring(3) });
                else
                    return Json(new { success = false, message = msj.Substring(6) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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
        public JsonResult ObtenerHistorialUsoMerma(string codigoMerma)
        {
            try
            {
                var data = bl.ObtenerHistorialUsoMerma(codigoMerma);
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

        [HttpGet]
        public JsonResult ObtenerDevolucionesPorInsumo(string np, string codInsumo)
        {
            try
            {
                var data = bl.ObtenerDevolucionesPorInsumo(np, codInsumo);
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
        public JsonResult AvanzarEstadoNP(string np, string nuevoEstado, bool hasNoMermaGlobal = false, bool hasNoAjusteGlobal = false)
        {
            try
            {
                if (Session["NombreUsuario"] == null)
                    return Json(new { success = false, message = "Sesion expirada" });

                string usuario = Session["NombreUsuario"].ToString();
                
                if (hasNoMermaGlobal && nuevoEstado == "Pendiente")
                {
                    bl.RegistrarNoMermaGlobal(np, usuario);
                }

                if (hasNoAjusteGlobal && nuevoEstado == "Liquidado")
                {
                    bl.RegistrarNoAjusteGlobal(np, usuario);
                }

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

        [HttpGet]
        public JsonResult PredecirSiguienteVersion(string np)
        {
            try
            {
                var siguienteNP = bl.PredecirSiguienteVersionNP(np);
                return Json(new { success = true, siguienteNP = siguienteNP }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult GenerarSiguienteVersion(string np, string observacion)
        {
            try
            {
                if (Session["NombreUsuario"] == null)
                    return Json(new { success = false, message = "Sesion expirada" });

                string usuario = Session["NombreUsuario"].ToString();
                var resultado = bl.GenerarSiguienteVersionNP(np, usuario, observacion);
                
                string nuevaNP = "";
                string msg = resultado.Mensaje;
                
                if (resultado.Exito && msg.Contains("|"))
                {
                    string[] partes = msg.Split('|');
                    nuevaNP = partes[0];
                    msg = partes[1];
                }

                return Json(new { success = resultado.Exito, message = msg, nuevaNP = nuevaNP });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        [HttpGet]
        public JsonResult ObtenerAuditoriaDevolucionesCentral(string np)
        {
            try
            {
                if (Session["NombreUsuario"] == null)
                    return Json(new { success = false, message = "Sesion expirada" }, JsonRequestBehavior.AllowGet);

                var data = bl.ObtenerAuditoriaDevolucionesCentral(np);
                return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // ==========================================
        // MÉTODOS PARA FÓRMULA EN BLANCO
        // ==========================================

        [HttpGet]
        public ActionResult NuevaFormulaBlanco()
        {
            if (Session["NombreUsuario"] == null)
                return RedirectToAction("Index", "Home");

            ViewBag.Usuario = Session["NombreUsuario"];
            ViewBag.MenuActivo = "FormulaBlanco";
            return View();
        }

        [HttpGet]
        public JsonResult ConsultarDatosNP(string np)
        {
            try
            {
                var datos = _stockReqBl.ObtenerDatosPorNP(np);
                if (datos != null)
                {
                    return Json(new { success = true, data = datos }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false, message = "No se encontraron datos para la NP." }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult RegistrarFormulaBlanco(VisitaBE recetaMaster)
        {
            try
            {
                if (Session["NombreUsuario"] == null)
                    return Json(new { success = false, result = "error", message = "Sesion expirada" });

                string usuario = Session["NombreUsuario"].ToString();
                
                // Validar campos obligatorios
                if (string.IsNullOrEmpty(recetaMaster.ComboCabecera) || string.IsNullOrEmpty(recetaMaster.Item))
                {
                    return Json(new { success = false, result = "error", message = "El Combo y el Item son obligatorios." });
                }
                
                // Validar duplicidad usando NP, Combo e Item
                if (_stockReqBl.ExisteFormulaBlancoDuplicada(recetaMaster.NP, recetaMaster.ComboCabecera, recetaMaster.Item))
                {
                    return Json(new { success = false, result = "duplicate", message = "Ya existe una fórmula registrada con estos mismos datos (NP, Combo e Item)." });
                }

                bool ok = _stockReqBl.InsertarFormulaBlanco(recetaMaster, usuario);
                if (ok)
                {
                    // Devolvemos result = "ok_redirect" para que nuestra vista custom maneje la redirección
                    return Json(new { success = true, result = "ok_redirect", message = "Fórmula en Blanco generada con éxito." });
                }
                else
                {
                    return Json(new { success = false, result = "error", message = "Ocurrió un error al guardar la fórmula en base de datos." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, result = "error", message = ex.Message });
            }
        }
    }
}
