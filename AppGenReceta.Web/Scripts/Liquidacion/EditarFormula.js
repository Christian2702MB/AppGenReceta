var hayCambios = false;
var nuevaPruebaCreada = false;
var recetaMaster = {
    IdFormula: 0,
    Colores: [],
    PruebasGlobales: []
};

var contadorPruebas = 0;
window.edicionHabilitada = false;

$(document).ready(function () {
    if (window.idFormulaActual && window.idFormulaActual !== 0) {
        Swal.fire({
            title: 'Cargando Fórmula',
            text: 'Por favor espere...',
            allowOutsideClick: false,
            didOpen: () => { Swal.showLoading(); }
        });
        cargarDatosParaEdicion(window.idFormulaActual);
    }

    $('#txtNombrePrueba').select2({
        dropdownParent: $('#modalAgregarPrueba'),
        width: '100%',
        placeholder: 'Escriba o seleccione...',
        language: 'es',
        tags: true,
        createTag: function (params) {
            var term = $.trim(params.term);
            if (term === '' || term.toUpperCase() === 'STRIKE OFF') {
                return null;
            }
            return {
                id: term,
                text: term,
                newTag: true
            }
        }
    });

    if (!window.g_isReadOnly) {
        $('#txtDescInsumo').select2({
            placeholder: 'Seleccione o busque un insumo...',
            ajax: {
                url: $('#txtDescInsumo').data('url-busqueda'),
                dataType: 'json',
                delay: 250,
                data: function (params) {
                    return { q: params.term || '' };
                },
                processResults: function (data) {
                    return { results: data.items };
                },
                cache: true
            },
            minimumInputLength: 3,
            language: {
                inputTooShort: function () { return "Ingrese 3 o más caracteres..."; },
                noResults: function () { return "No se encontraron insumos"; },
                searching: function () { return "Buscando..."; }
            }
        });

        $('#formFormula').on('change input', 'input', function () {
            hayCambios = true;
        });
    }

    if (window.g_isReadOnly) {
        $("#formFormula input, #formFormula select").prop("disabled", true);
        if ($('.select2').length > 0) {
            $('.select2').prop('disabled', true);
        }
    } 

    $('#btnHabilitarEdicion').click(function () {
        if (!window.g_isReadOnly) {
            window.edicionHabilitada = true;
            inyectarPruebasDOM();
            $(this).prop('disabled', true).html('<i class="fa fa-check"></i> Edición Habilitada');
        }
    });
});

function cargarDatosParaEdicion(id) {
    $.get("/Liquidacion/ObtenerFormulaCompleta", { id: id }, function (res) {
        if (res.success && res.data) {
            procesarDatosReceta(res.data);
            Swal.close();
        } else {
            Swal.fire('Error', res.message || 'No se pudo cargar la fórmula.', 'error');
        }
    }).fail(function () {
        Swal.fire('Error', 'Error de comunicación con el servidor.', 'error');
    });
}

function procesarDatosReceta(data) {
    recetaMaster.IdFormula = data.IdFormula;
    recetaMaster.Colores = data.Colores || [];

    if (recetaMaster.Colores) {
        recetaMaster.Colores.forEach(c => {
            if (c.Insumos) {
                c.Insumos.forEach(i => {
                    // Mantener la cantidad original como "Base Receta"
                    i.Cantidad = i.Cantidad || 0;
                    // Ya no limpiamos i.Pruebas para que las columnas históricas se sigan renderizando
                });
            }
        });
    }

    sincronizarPruebasExistentes();
    actualizarVistaColores();
    inyectarPruebasDOM();
}

function sincronizarPruebasExistentes() {
    let pruebasEncontradas = [];
    let nombresUnicos = new Set();
    let maxId = 0;

    let arrayColores = recetaMaster.Colores;
    if (arrayColores && Array.isArray(arrayColores)) {
        arrayColores.forEach(color => {
            let arrayInsumos = color.Insumos;
            if (Array.isArray(arrayInsumos)) {
                arrayInsumos.forEach(insumo => {
                    let arrayPruebas = insumo.Pruebas;
                    if (Array.isArray(arrayPruebas)) {
                        arrayPruebas.forEach(prueba => {
                            let id = prueba.IdPrueba !== undefined ? prueba.IdPrueba : 0;
                            let nombre = prueba.NombrePrueba || "";
                            let nombreNormalizado = nombre.trim().toUpperCase();
                            let esPrinc = (prueba.EsPrincipal === true);

                            if (id > maxId) maxId = id;

                            if (nombreNormalizado !== "" && !nombresUnicos.has(nombreNormalizado)) {
                                nombresUnicos.add(nombreNormalizado);
                                pruebasEncontradas.push({
                                    IdPrueba: id,
                                    NombrePrueba: nombre,
                                    NombreNormalizado: nombreNormalizado,
                                    EsHistorico: true,
                                    EsPrincipal: esPrinc
                                });
                            } else if (esPrinc) {
                                let pruebaGlobal = pruebasEncontradas.find(p => p.NombreNormalizado === nombreNormalizado);
                                if (pruebaGlobal) pruebaGlobal.EsPrincipal = true;
                            }
                        });
                    }
                });
            }
        });
    }

    recetaMaster.PruebasGlobales = pruebasEncontradas;
    if (maxId > 0) contadorPruebas = maxId;
}

function abrirModalPrueba() {
    $("#txtNombrePrueba").val(null).trigger('change');
    $("#modalAgregarPrueba").modal('show');
}

function agregarColumnaPrueba() {
    if (typeof contadorPruebas === 'undefined' || isNaN(contadorPruebas)) {
        window.contadorPruebas = (recetaMaster.PruebasGlobales && recetaMaster.PruebasGlobales.length > 0)
            ? Math.max(...recetaMaster.PruebasGlobales.map(p => p.IdPrueba)) : 0;
    }

    let nombreNuevaPrueba = $("#txtNombrePrueba").val();
    if (!nombreNuevaPrueba || nombreNuevaPrueba.trim().length === 0) {
        Swal.fire("Aviso", "Ingrese el nombre de la prueba", "warning");
        return;
    }

    contadorPruebas++;
    let titulo = nombreNuevaPrueba;
    let nombreNorm = nombreNuevaPrueba.trim().toUpperCase();

    if (!recetaMaster.PruebasGlobales) recetaMaster.PruebasGlobales = [];

    let existeGlobal = recetaMaster.PruebasGlobales.find(p => p.NombreNormalizado === nombreNorm);
    if (!existeGlobal) {
        recetaMaster.PruebasGlobales.push({
            IdPrueba: contadorPruebas,
            NombrePrueba: titulo,
            NombreNormalizado: nombreNorm,
            EsHistorico: false,
            EsPrincipal: false
        });
    }

    let arrayColores = recetaMaster.Colores;
    if (arrayColores && Array.isArray(arrayColores)) {
        arrayColores.forEach(color => {
            let arrayInsumos = color.Insumos;
            if (Array.isArray(arrayInsumos)) {
                arrayInsumos.forEach(insumo => {
                    if (!insumo.Pruebas) insumo.Pruebas = [];
                    let existe = insumo.Pruebas.find(p => (p.NombrePrueba || "").trim().toUpperCase() === nombreNorm);

                    if (!existe) {
                        insumo.Pruebas.push({
                            IdPrueba: contadorPruebas,
                            NombrePrueba: titulo,
                            GramosUDP: undefined,
                            EsPrincipal: true
                        });
                    } else {
                        existe.EsPrincipal = true;
                    }
                    
                    // Asegurar que las demás pruebas de este insumo queden como falsas
                    insumo.Pruebas.forEach(p => {
                        if ((p.NombrePrueba || "").trim().toUpperCase() !== nombreNorm) {
                            p.EsPrincipal = false;
                        }
                    });
                });
            }
            color.PruebaPrincipal = nombreNorm;
        });
    }

    hayCambios = true;
    nuevaPruebaCreada = true;
    window.edicionHabilitada = true;
    $("#txtNombrePrueba").val(null).trigger('change');
    $("#modalAgregarPrueba").modal('hide');

    inyectarPruebasDOM();
}

function actualizarVistaColores() {
    const contenedor = $("#contenedorColores");
    contenedor.empty();

    if (!recetaMaster.Colores || recetaMaster.Colores.length === 0) {
        contenedor.append('<div class="alert alert-info text-center">No hay colores en la fórmula.</div>');
        return;
    }

    recetaMaster.Colores.forEach((color, indexColor) => {
        let htmlColor = `
            <div class="panel panel-info shadow-sm" style="margin-bottom: 15px;">
                <div class="panel-heading" style="background-color: #f5f5f5; border-color: #bce8f1;">
                    <h4 class="panel-title" style="color: #31708f;">
                        <strong>Color Pantone:</strong> ${color.Nombre}
                    </h4>
                </div>
                <div class="panel-body">
                    <table class="table table-condensed table-hover" data-colorindex="${indexColor}">
                        <thead>
                            <tr class="active">
                                <th style="width: 15%">Código</th>
                                <th style="width: 35%">Insumo</th>
                                <th style="width: 25%" class="text-right">Base Receta</th>
                            </tr>
                        </thead>
                        <tbody>`;

        color.Insumos.forEach((insumo, indexInsumo) => {
            let valCant = typeof insumo.Cantidad === 'number' ? insumo.Cantidad.toFixed(2) : "0.00";
            
            htmlColor += `
                <tr data-index="${indexInsumo}">
                    <td><strong>${insumo.CodigoInsumo}</strong></td>
                    <td><strong>${insumo.Descripcion} (gr)</strong></td>
                    <td class="text-right align-middle" style="padding-right: 15px; font-size: 1.1em; color: #555;">
                        <strong>${valCant}</strong>
                    </td>
                </tr>`;
        });

        htmlColor += `
                        </tbody>
                    </table>
                </div>
            </div>`;

        contenedor.append(htmlColor);
    });

    actualizarComboColores();
    inyectarPruebasDOM();
}

function actualizarComboColores() {
    const ddl = $("#ddlColorDestino");
    if (!ddl.length) return;
    ddl.empty();
    ddl.append('<option value="">Seleccione</option>');

    recetaMaster.Colores.forEach((color, index) => {
        ddl.append('<option value="' + index + '">' + color.Nombre + '</option>');
    });
}

function agregarInsumoAColor() {
    const valorCompleto = $("#txtDescInsumo").val() || "";
    let codigoReal = "";
    let descripcionReal = "";

    if (valorCompleto.length >= 8) {
        codigoReal = valorCompleto.substring(0, 8).trim();
        descripcionReal = valorCompleto.substring(8).trim();
    } else {
        Swal.fire("Atención", "Por favor seleccione un insumo válido.", "warning");
        return;
    }

    const indexColor = $("#ddlColorDestino").val();
    let cantidad = 0;

    if (indexColor === "" || !codigoReal || !descripcionReal) {
        Swal.fire("Atención", "Seleccione el color a asignar.", "warning");
        return;
    }

    recetaMaster.Colores[indexColor].Insumos.push({
        IdInsumo: 0,
        CodigoInsumo: codigoReal,
        Descripcion: descripcionReal,
        Cantidad: cantidad,
        Pruebas: [] // Inicia sin pruebas
    });

    // Como añadimos un insumo nuevo, sincronizamos sus columnas de pruebas con las globales
    if (recetaMaster.PruebasGlobales && recetaMaster.PruebasGlobales.length > 0) {
        let nvoInsumo = recetaMaster.Colores[indexColor].Insumos[recetaMaster.Colores[indexColor].Insumos.length - 1];
        recetaMaster.PruebasGlobales.forEach(p => {
            nvoInsumo.Pruebas.push({
                IdPrueba: p.IdPrueba,
                NombrePrueba: p.NombrePrueba,
                GramosUDP: undefined,
                EsPrincipal: false
            });
        });
    }

    $("#txtDescInsumo").val("").trigger('change');
    hayCambios = true;
    actualizarVistaColores();
}

function actualizarCantidadInsumo(idxColor, idxInsumo, elem) {
    let valor = parseFloat($(elem).val());
    if (isNaN(valor) || valor < 0) valor = 0;
    
    recetaMaster.Colores[idxColor].Insumos[idxInsumo].Cantidad = valor;
    $(elem).val(valor.toFixed(2));
    hayCambios = true;
}

function inyectarPruebasDOM() {
    $('#contenedorColores .th-prueba, #contenedorColores .th-prueba-reset').remove();
    $('#contenedorColores .td-prueba, #contenedorColores .td-prueba-reset').remove();

    if (!recetaMaster.PruebasGlobales || recetaMaster.PruebasGlobales.length === 0) return;

    let pruebasUnicas = [];
    let nombresVistos = new Set();

    recetaMaster.PruebasGlobales.forEach(p => {
        let norm = (p.NombreNormalizado || p.NombrePrueba || "").trim().toUpperCase();
        if (norm !== "" && !nombresVistos.has(norm)) {
            nombresVistos.add(norm);
            p.NombreNormalizado = norm;
            pruebasUnicas.push(p);
        }
    });
    recetaMaster.PruebasGlobales = pruebasUnicas;

    // Forzar que SIEMPRE la última prueba global sea la principal por defecto
    if (recetaMaster.PruebasGlobales.length > 0) {
        let ultimaPruebaNorm = recetaMaster.PruebasGlobales[recetaMaster.PruebasGlobales.length - 1].NombreNormalizado;
        recetaMaster.PruebasGlobales.forEach(p => p.EsPrincipal = (p.NombreNormalizado === ultimaPruebaNorm));
        
        if (recetaMaster.Colores && Array.isArray(recetaMaster.Colores)) {
            recetaMaster.Colores.forEach(color => {
                color.PruebaPrincipal = ultimaPruebaNorm;
                if (color.Insumos && Array.isArray(color.Insumos)) {
                    color.Insumos.forEach(insumo => {
                        if (insumo.Pruebas && Array.isArray(insumo.Pruebas)) {
                            insumo.Pruebas.forEach(p => {
                                p.EsPrincipal = ((p.NombrePrueba || "").trim().toUpperCase() === ultimaPruebaNorm);
                            });
                        }
                    });
                }
            });
        }
    }

    $('#contenedorColores table').each(function () {
        let tabla = $(this);
        let idxColor = tabla.data('colorindex');
        
        if (idxColor === undefined || !recetaMaster.Colores[idxColor]) return;
        
        let colorActual = recetaMaster.Colores[idxColor];
        let pruebaPrincipalDelColor = colorActual.PruebaPrincipal || "";

        let theadTr = tabla.find('thead tr');
        if (theadTr.length > 0) {
            recetaMaster.PruebasGlobales.forEach(p => {
                let isPrincipal = (pruebaPrincipalDelColor === p.NombreNormalizado);
                let isChecked = isPrincipal ? 'checked="checked"' : '';
                let bgColHeader = isPrincipal ? 'background-color: #fff3cd !important; border: 2px solid #ffc107; border-bottom: none;' : 'background-color: #f8f9fa;';
                // El radio button siempre debe estar deshabilitado para que el usuario no pueda cambiarlo
                let radioDisabled = 'disabled="disabled"';
                let nombreParam = p.NombrePrueba.replace(/'/g, "\\'");

                let th = `<th class="th-prueba text-center align-middle" style="min-width: 85px; width: 85px; padding: 6px; ${bgColHeader}">
                            <div class="mb-1" title="Prueba principal por defecto">
                                <input type="radio" name="rdoPruebaPrincipal_${idxColor}" class="form-check-input" style="position:relative; margin:0; cursor:not-allowed;" ${isChecked} ${radioDisabled}>
                            </div>
                            ${p.NombrePrueba}
                          </th>`;
                theadTr.append(th);
            });
            theadTr.append(`<th class="th-prueba-reset text-center bg-light align-middle" style="width: 40px; padding: 6px; border-left: 2px solid #dee2e6;"></th>`);
        }

        let filasInsumos = tabla.find('tbody tr');
        let insumos = colorActual.Insumos;

        if (!insumos) return;

        filasInsumos.each(function (indexRow) {
            let tr = $(this);
            let idxInsumo = tr.data('index');
            let insumo = insumos[idxInsumo];

            if (insumo) {
                let pruebasDelInsumo = insumo.Pruebas || [];
                
                recetaMaster.PruebasGlobales.forEach((p, idxPruebaGlobal) => {
                    let nombreGlobalNorm = p.NombreNormalizado;
                    let pruebaData = pruebasDelInsumo.find(x => (x.NombrePrueba || "").trim().toUpperCase() === nombreGlobalNorm);

                    let valorStr = '';
                    if (pruebaData && pruebaData.GramosUDP !== undefined && pruebaData.GramosUDP !== null && pruebaData.GramosUDP !== "") {
                        valorStr = parseFloat(pruebaData.GramosUDP).toFixed(2);
                    }

                    let isPrincipal = (pruebaPrincipalDelColor === nombreGlobalNorm);
                    let isLastRow = (indexRow === filasInsumos.length - 1);
                    
                    let habilitado = true;
                    if (window.g_isReadOnly) {
                        habilitado = false;
                    } else if (!window.edicionHabilitada) {
                        habilitado = false;
                    } else if (nuevaPruebaCreada) {
                        let isLastColumn = (idxPruebaGlobal === recetaMaster.PruebasGlobales.length - 1);
                        habilitado = isLastColumn;
                    }

                    let disabledAttr = habilitado ? '' : 'disabled="disabled"';
                    
                    let bgColorInput = '';
                    if (!habilitado) {
                        bgColorInput = 'background-color: #e9ecef; color: #495057; cursor: not-allowed;';
                    } else if (isPrincipal) {
                        bgColorInput = 'background-color: #fff; font-weight: bold; border-color: #ffc107;';
                    }
                    
                    let borderBtm = isLastRow ? 'border-bottom: 2px solid #ffc107;' : '';
                    let bgColTd = isPrincipal ? `background-color: #fff3cd !important; border-left: 2px solid #ffc107; border-right: 2px solid #ffc107; ${borderBtm}` : '';
                    let nombreParam = p.NombrePrueba.replace(/'/g, "\\'");

                    let td = `<td class="td-prueba text-center align-middle" style="padding: 4px 6px; ${bgColTd}">
                                <input type="number" step="0.01" placeholder="0.00" min="0" class="form-control form-control-sm text-center font-weight-bold"
                                       style="width: 100%; min-width: 78px; max-width: 100px; margin: 0 auto; height: 28px; ${bgColorInput}" 
                                       value="${valorStr}" ${disabledAttr}
                                       onchange="actualizarValorPrueba('${idxColor}', '${idxInsumo}', '${nombreParam}', this)">
                              </td>`;
                    tr.append(td);
                });
                tr.append(`<td class="td-prueba-reset text-center align-middle" style="border-left: 2px solid #dee2e6;"></td>`);
            }
        });
    });
}

function actualizarValorPrueba(idxColor, idxInsumo, nombrePrueba, element) {
    if (!recetaMaster || !recetaMaster.Colores || !recetaMaster.Colores[idxColor]) return;

    let colorActual = recetaMaster.Colores[idxColor];
    let insumos = colorActual.Insumos;
    if (!insumos) return;

    let insumo = insumos[idxInsumo];
    if (insumo) {
        if (!insumo.Pruebas) insumo.Pruebas = [];

        let nombreNorm = nombrePrueba.trim().toUpperCase();
        let isPrincipalLocal = (colorActual.PruebaPrincipal === nombreNorm);
        let prueba = insumo.Pruebas.find(p => (p.NombrePrueba || "").trim().toUpperCase() === nombreNorm);
        
        let valor = $(element).val();
        let valorParseado = parseFloat(valor);

        if (prueba) {
            prueba.GramosUDP = isNaN(valorParseado) ? undefined : valorParseado;
            prueba.EsPrincipal = isPrincipalLocal;
            hayCambios = true;
        } else {
            if (!isNaN(valorParseado) && valorParseado >= 0) {
                if (typeof contadorPruebas !== 'undefined') contadorPruebas++;
                insumo.Pruebas.push({
                    IdPrueba: contadorPruebas,
                    NombrePrueba: nombrePrueba,
                    GramosUDP: valorParseado,
                    EsPrincipal: isPrincipalLocal
                });
                hayCambios = true;
            }
        }

        if (!isNaN(valorParseado)) {
            $(element).val(valorParseado.toFixed(2));
        } else {
            $(element).val('');
        }
    }
}

function marcarPruebaPrincipalColor(nombrePrueba, idxColor) {
    let nombreNorm = nombrePrueba.trim().toUpperCase();
    let color = recetaMaster.Colores[idxColor];
    if (!color) return;

    color.PruebaPrincipal = nombreNorm;

    let arrayInsumos = color.Insumos;
    if (Array.isArray(arrayInsumos)) {
        arrayInsumos.forEach(insumo => {
            let arrayPruebas = insumo.Pruebas;
            if (Array.isArray(arrayPruebas)) {
                arrayPruebas.forEach(prueba => {
                    let pName = (prueba.NombrePrueba || "").trim().toUpperCase();
                    prueba.EsPrincipal = (pName === nombreNorm);
                });
            }
        });
    }

    hayCambios = true;
    inyectarPruebasDOM();
}

function guardarFormulaCompleta() {
    // Validaciones
    if (recetaMaster.Colores.length === 0) {
        Swal.fire("Aviso", "No hay colores en la fórmula.", "warning");
        return;
    }

    Swal.fire({
        title: '¿Guardar Cambios?',
        text: "Se actualizarán las cantidades y pruebas de la fórmula",
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Sí, guardar',
        cancelButtonText: 'Cancelar'
    }).then((result) => {
        if (result.value) {
            // Preparar el objeto final con IdFormula
            let datosEnviar = Object.assign({}, recetaMaster);
            
            Swal.fire({ title: 'Guardando...', didOpen: function () { Swal.showLoading(); } });

            $.ajax({
                url: window.urlGuardarFormula,
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify(datosEnviar),
                success: function (res) {
                    if (res.success) {
                        hayCambios = false;
                        Swal.fire("¡Actualizado!", res.message, "success")
                            .then(() => { window.location.href = window.urlMantenimiento; });
                    } else {
                        Swal.fire("Error", res.message, "error");
                    }
                },
                error: function () {
                    Swal.fire("Error", "No se pudo conectar con el servidor", "error");
                }
            });
        }
    });
}
