var hayCambios = false; // Flag global

// Al inicio de tus archivos .js
var recetaMaster = {
    Dato: "0",
    NP: "",
    FechaUDP: "",    // <-- Agregado
    Operario: "",    // <-- Agregado
    Tecnica: "",    // <-- Agregado
    Cliente: "",     // <-- Asegúrate de que existan
    Temporada: "",   // <-- Agregado
    Estilo: "",      // <-- Agregado
    EstiloPropio: "",      // <-- Agregado
    Item: "",        // <-- Agregado
    ComboCabecera: "",       // <-- Agregado
    PrendasReq: "",       // <-- Agregado   
    // Concepto ya no va aquí
    Ubicacion: "",    // <-- Agregado
    Arte: "",    // <-- Agregado
    Colores: [],
    PruebasGlobales: [] // <-- NUEVO: Guardará la información de las columnas de prueba
};

var indexColorSeleccionado = -1;

function sincronizarPruebasExistentes() {
    let pruebasEncontradas = [];
    let nombresUnicos = new Set();
    let maxId = 0;

    let arrayColores = recetaMaster.Colores || recetaMaster.colores;
    if (arrayColores && Array.isArray(arrayColores)) {
        arrayColores.forEach(color => {
            let arrayInsumos = color.Insumos || color.insumos;
            if (Array.isArray(arrayInsumos)) {
                arrayInsumos.forEach(insumo => {
                    let arrayPruebas = insumo.Pruebas || insumo.pruebas;
                    if (Array.isArray(arrayPruebas)) {
                        arrayPruebas.forEach(prueba => {
                            let id = prueba.IdPrueba !== undefined ? prueba.IdPrueba : prueba.idPrueba;
                            let nombre = prueba.NombrePrueba || prueba.nombrePrueba || "";
                            let nombreNormalizado = nombre.trim().toUpperCase();

                            // 🟢 CAPTURAMOS EL DATO DESDE LA BD (Por si llega en minúscula o mayúscula)
                            let esPrinc = (prueba.EsPrincipal === true || prueba.esPrincipal === true);

                            if (id > maxId) maxId = id;

                            if (nombreNormalizado !== "" && !nombresUnicos.has(nombreNormalizado)) {
                                nombresUnicos.add(nombreNormalizado);
                                pruebasEncontradas.push({
                                    IdPrueba: id,
                                    NombrePrueba: nombre,
                                    NombreNormalizado: nombreNormalizado,
                                    EsHistorico: true,
                                    EsPrincipal: esPrinc // 🟢 Lo guardamos en la columna global
                                });
                            } else if (esPrinc) {
                                // Si ya existe la columna pero este insumo nos dice que es el principal, forzamos el true globalmente
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
    inyectarPruebasDOM();
}

var contadorPruebas = 0;

function abrirModalPrueba() {
    $("#txtNombrePrueba").val(null).trigger('change');
    $("#modalAgregarPrueba").modal('show');
}

// REEMPLAZA TU FUNCIÓN DE AGREGAR PRUEBA POR ESTA VERSIÓN
function agregarColumnaPrueba() { // <-- Usa el nombre exacto que tenga tu función actual

    // 1. Asegurarnos de que el contador exista
    if (typeof contadorPruebas === 'undefined' || isNaN(contadorPruebas)) {
        window.contadorPruebas = (recetaMaster.PruebasGlobales && recetaMaster.PruebasGlobales.length > 0)
            ? Math.max(...recetaMaster.PruebasGlobales.map(p => p.IdPrueba))
            : 0;
    }

    // 2. Aumentamos el contador y creamos el nombre
    //contadorPruebas++;
    //let nombreNuevaPrueba = "Prueba " + contadorPruebas;
    //let nombreNorm = nombreNuevaPrueba.trim().toUpperCase();

    let nombreNuevaPrueba = $("#txtNombrePrueba").val();
    contadorPruebas++;
    let titulo = nombreNuevaPrueba ? nombreNuevaPrueba : `Prueba ${contadorPruebas}`;
    let nombreNorm = nombreNuevaPrueba.trim().toUpperCase();

    if (nombreNuevaPrueba.length === 0) {
        Swal.fire("Aviso", "Ingrese el nombre de la prueba", "warning");
        return;
    }

    if (!recetaMaster.PruebasGlobales) recetaMaster.PruebasGlobales = [];

    // 3. Agregamos a la lista global de la tabla (Para que se dibuje la cabecera)
    recetaMaster.PruebasGlobales.push({
        IdPrueba: contadorPruebas,
        NombrePrueba: titulo,
        NombreNormalizado: nombreNorm,
        EsHistorico: false,
        EsPrincipal: false
    });

    // 4. 🟢 LA CLAVE DEL ARREGLO:
    // Insertamos la prueba vacía en TODOS los insumos de TODOS los colores al mismo tiempo
    let arrayColores = recetaMaster.Colores || recetaMaster.colores;
    if (arrayColores && Array.isArray(arrayColores)) {
        arrayColores.forEach(color => {
            let arrayInsumos = color.Insumos || color.insumos;
            if (Array.isArray(arrayInsumos)) {
                arrayInsumos.forEach(insumo => {
                    if (!insumo.Pruebas) insumo.Pruebas = [];

                    // Buscamos si ya existe para evitar duplicados por seguridad
                    let existe = insumo.Pruebas.find(p => (p.NombrePrueba || p.nombrePrueba || "").trim().toUpperCase() === nombreNorm);

                    if (!existe) {
                        insumo.Pruebas.push({
                            IdPrueba: contadorPruebas,
                            NombrePrueba: titulo,
                            GramosUDP: undefined, // En blanco para que el cuadro de texto salga limpio
                            EsPrincipal: false
                        });
                    }
                });
            }
        });
    }

    hayCambios = true;
    $("#txtNombrePrueba").val(null).trigger('change');
    $("#modalAgregarPrueba").modal('hide');

    // 2. Ejecutar la inyección visual de las columnas
    // 5. Dejamos que inyectarPruebasDOM haga su magia.
    // Limpiará la tabla y redibujará las cabeceras y los inputs perfectamente alineados.
    inyectarPruebasDOM();
}

function actualizarValorPrueba(idxColor, idxInsumo, nombrePrueba, valor) {
    if (!recetaMaster || !recetaMaster.Colores || !recetaMaster.Colores[idxColor]) return;

    let colorActual = recetaMaster.Colores[idxColor];
    let insumos = colorActual.Insumos || colorActual.insumos;
    if (!insumos) return;

    let insumo = insumos[idxInsumo];
    if (insumo) {
        if (!insumo.Pruebas) insumo.Pruebas = [];

        let nombreNorm = nombrePrueba.trim().toUpperCase();

        // 🟢 Heredar el estado "EsPrincipal" del color actual
        let isPrincipalLocal = (colorActual.PruebaPrincipal === nombreNorm);

        let prueba = insumo.Pruebas.find(p => (p.NombrePrueba || p.nombrePrueba || "").trim().toUpperCase() === nombreNorm);
        let valorParseado = parseFloat(valor);

        if (prueba) {
            prueba.GramosUDP = isNaN(valorParseado) ? 0 : valorParseado;
            prueba.EsPrincipal = isPrincipalLocal;
            hayCambios = true;
        } else {
            if (!isNaN(valorParseado) && valorParseado > 0) {
                // (Ojo: Asegúrate de que tu variable global contadorPruebas esté definida)
                if (typeof contadorPruebas !== 'undefined') contadorPruebas++;
                insumo.Pruebas.push({
                    IdPrueba: (typeof contadorPruebas !== 'undefined') ? contadorPruebas : 0,
                    NombrePrueba: nombrePrueba,
                    GramosUDP: valorParseado,
                    EsPrincipal: isPrincipalLocal
                });
                hayCambios = true;
            }
        }
    }
}

function marcarPruebaPrincipalColor(nombrePrueba, idxColor) {
    let nombreNorm = nombrePrueba.trim().toUpperCase();

    let color = recetaMaster.Colores[idxColor];
    if (!color) return;

    // 1. Guardar a nivel de color para que lo recuerde al redibujar
    color.PruebaPrincipal = nombreNorm;

    // 2. Actualizar todos los insumos existentes ÚNICAMENTE de ESTE color
    let arrayInsumos = color.Insumos || color.insumos;
    if (Array.isArray(arrayInsumos)) {
        arrayInsumos.forEach(insumo => {
            let arrayPruebas = insumo.Pruebas || insumo.pruebas;
            if (Array.isArray(arrayPruebas)) {
                arrayPruebas.forEach(prueba => {
                    let pName = (prueba.NombrePrueba || prueba.nombrePrueba || "").trim().toUpperCase();
                    let esElSeleccionado = (pName === nombreNorm);

                    prueba.EsPrincipal = esElSeleccionado;
                    if (prueba.esPrincipal !== undefined) {
                        prueba.esPrincipal = esElSeleccionado;
                    }
                });
            }
        });
    }

    hayCambios = true;
    inyectarPruebasDOM(); // Redibujar DOM para aplicar estilos de selección
}

function inyectarPruebasDOM() {
    // 1. Limpieza total en el contenedor
    $('#contenedorColores .th-prueba, #contenedorColores .th-prueba-reset').remove();
    $('#contenedorColores .td-prueba, #contenedorColores .td-prueba-reset').remove();

    if (!recetaMaster.PruebasGlobales || recetaMaster.PruebasGlobales.length === 0) return;

    // 2. Filtro Salvavidas para evitar columnas duplicadas
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

    // 🟢 3. Recorremos TABLA POR TABLA
    $('#contenedorColores table').each(function (indexTabla) {
        let tabla = $(this);
        let colorIdxAtributo = tabla.attr('data-colorindex') || tabla.data('colorindex');
        let idxColor = colorIdxAtributo !== undefined ? parseInt(colorIdxAtributo) : indexTabla;

        if (idxColor === -1 || !recetaMaster.Colores[idxColor]) return;

        let colorActual = recetaMaster.Colores[idxColor];

        // --- Averiguar qué prueba es la principal para ESTE COLOR ---
        let pruebaPrincipalDelColor = colorActual.PruebaPrincipal || "";
        if (pruebaPrincipalDelColor === "") {
            let insumosColor = colorActual.Insumos || colorActual.insumos;
            if (insumosColor && insumosColor.length > 0) {
                let arrayPruebas = insumosColor[0].Pruebas || insumosColor[0].pruebas;
                if (arrayPruebas) {
                    let pPrincipal = arrayPruebas.find(x => x.EsPrincipal === true || x.esPrincipal === true);
                    if (pPrincipal) {
                        pruebaPrincipalDelColor = (pPrincipal.NombrePrueba || pPrincipal.nombrePrueba || "").trim().toUpperCase();
                        colorActual.PruebaPrincipal = pruebaPrincipalDelColor;
                    }
                }
            }
        }

        // --- INYECTAR CABECERAS ---
        let theadTr = tabla.find('thead tr').last();
        if (theadTr.length > 0) {
            // CORRECCIÓN: En modo edición (g_isReadOnly=false) el último <th> es la columna de
            // acción (vacía, donde va la "x"), por lo que insertamos ANTES de ella para que las
            // pruebas queden entre STRIKE OFF y la acción.
            // En modo lectura (g_isReadOnly=true) NO existe columna de acción, así que el último
            // <th> sería STRIKE OFF y las pruebas se insertarían antes de ella (orden incorrecto).
            // Por eso en modo lectura hacemos append() al final de la fila.
            let thAccion = theadTr.find('th').last();

            if (thAccion.length > 0) {
                recetaMaster.PruebasGlobales.forEach(p => {
                    let isPrincipal = (pruebaPrincipalDelColor === p.NombreNormalizado);
                    let isChecked = isPrincipal ? 'checked="checked"' : '';
                    let nombreParam = p.NombrePrueba.replace(/'/g, "\\'");

                    let bgColHeader = isPrincipal ? 'background-color: #fff3cd !important; border: 2px solid #ffc107; border-bottom: none;' : 'background-color: #f8f9fa;';

                    // CAMBIO 2: Radio button deshabilitado en modo solo lectura.
                    let radioDisabled = window.g_isReadOnly ? 'disabled="disabled"' : '';

                    // 🟢 EL TRUCO: name="rdoPruebaPrincipal_${idxColor}" hace que sean grupos separados
                    let th = `<th class="th-prueba text-center align-middle" style="min-width: 85px; width: 85px; padding: 6px; ${bgColHeader}">
                                <div class="mb-1" title="Marcar como envio para producción">
                                    <input type="radio" name="rdoPruebaPrincipal_${idxColor}" class="form-check-input" style="position:relative; margin:0; cursor:pointer;" ${isChecked} ${radioDisabled} onchange="marcarPruebaPrincipalColor('${nombreParam}', ${idxColor})">
                                </div>
                                ${p.NombrePrueba}
                              </th>`;

                    // CORRECCIÓN ORDEN: En modo lectura no hay columna de acción al final,
                    // por lo que usamos append para colocar las pruebas al final de la fila.
                    // En modo edición sí hay columna de acción → insertBefore la mantiene antes de la "x".
                    if (window.g_isReadOnly) {
                        theadTr.append(th);
                    } else {
                        $(th).insertBefore(thAccion);
                    }
                });

                let thReset = `<th class="th-prueba-reset text-center bg-light align-middle" style="width: 40px; padding: 6px; border-left: 2px solid #dee2e6;"></th>`;
                // El separador también sigue la misma lógica
                if (window.g_isReadOnly) {
                    theadTr.append(thReset);
                } else {
                    $(thReset).insertBefore(thAccion);
                }
            }
        }

        // --- INYECTAR DATOS (INPUTS) ---
        let filasInsumos = tabla.find('tbody tr');
        let insumos = recetaMaster.Colores[idxColor].Insumos || recetaMaster.Colores[idxColor].insumos;

        if (!insumos) return;

        filasInsumos.each(function (indexRow) {
            let tr = $(this);
            let idxInsumo = tr.data('index') !== undefined ? tr.data('index') : indexRow;
            let insumo = insumos[idxInsumo];

            if (insumo) {
                let pruebasDelInsumo = (insumo && insumo.Pruebas) ? insumo.Pruebas : (insumo && insumo.pruebas) ? insumo.pruebas : [];
                // En modo edición el último <td> es la celda de acción (botón "x").
                // En modo lectura el último <td> es STRIKE OFF — mismo problema que en thead.
                let tdAccion = tr.find('td').last();

                if (tdAccion.length > 0) {
                    recetaMaster.PruebasGlobales.forEach(p => {
                        let nombreGlobalNorm = p.NombreNormalizado;

                        let pruebaData = pruebasDelInsumo.find(x => {
                            let nombreLocal = (x.NombrePrueba || x.nombrePrueba || "").trim().toUpperCase();
                            return nombreLocal === nombreGlobalNorm;
                        });

                        let valor = '';
                        if (pruebaData) {
                            if (pruebaData.GramosUDP !== undefined) valor = pruebaData.GramosUDP;
                            else if (pruebaData.gramosUDP !== undefined) valor = pruebaData.gramosUDP;
                        }

                        let isPrincipal = (pruebaPrincipalDelColor === nombreGlobalNorm);
                        let isLastRow = (indexRow === filasInsumos.length - 1);

                        let disabledAttr = p.EsHistorico ? 'disabled="disabled" title="Dato guardado"' : '';
                        let bgColorInput = p.EsHistorico ? 'background-color: #e9ecef;' : (isPrincipal ? 'background-color: #fff; font-weight: bold; border-color: #ffc107;' : '');

                        let borderBtm = isLastRow ? 'border-bottom: 2px solid #ffc107;' : '';
                        let bgColTd = isPrincipal ? `background-color: #fff3cd !important; border-left: 2px solid #ffc107; border-right: 2px solid #ffc107; ${borderBtm}` : '';

                        let nombreParam = p.NombrePrueba.replace(/'/g, "\\'");

                        let td = `
                            <td class="td-prueba text-center align-middle" style="padding: 4px 6px; ${bgColTd}">
                                <input type="number" step="0.01" placeholder="0.00" min="0" class="form-control form-control-sm text-center font-weight-bold"
                                       style="width: 100%; min-width: 78px; max-width: 100px; margin: 0 auto; height: 28px; ${bgColorInput}" 
                                       value="${valor}" ${disabledAttr}
                                       onchange="actualizarValorPrueba('${idxColor}', '${idxInsumo}', '${nombreParam}', this.value)">
                            </td>
                        `;
                        // CORRECCIÓN ORDEN: mismo criterio que en thead.
                        // Lectura → append al final. Edición → insertBefore la celda de acción.
                        if (window.g_isReadOnly) {
                            tr.append(td);
                        } else {
                            $(td).insertBefore(tdAccion);
                        }
                    });

                    let tdReset = `<td class="td-prueba-reset text-center align-middle" style="border-left: 2px solid #dee2e6;"></td>`;
                    if (window.g_isReadOnly) {
                        tr.append(tdReset);
                    } else {
                        $(tdReset).insertBefore(tdAccion);
                    }
                }
            }
        });
    });
}

function cancelarEdicion() {
    const urlRetorno = '/Home/MantenimientoVisitas'; // Tu ruta de retorno
    if (hayCambios) {
        // Solo si hubo cambios, mostramos el SweetAlert
        Swal.fire({
            title: '¿Desea cancelar?',
            text: "Los cambios no guardados se perderán",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Sí, salir',
            cancelButtonText: 'No, continuar'
        }).then((result) => {
            if (result.value) {
                window.location.href = urlRetorno; // O la ruta de tu listado
            }
        });
    } else {
        // Si NO hubo cambios, redirigimos directamente sin preguntar
        window.location.href = urlRetorno;
    }
}

$(document).ready(function () {
    // Si el campo hidden tiene un ID, significa que el servidor ya cargó el modelo
    var idExistente = $("#hdnIdVisita").val();
    if (idExistente && idExistente !== "0") {
        // Mostrar SweetAlert mientras carga los datos por AJAX
        Swal.fire({
            title: 'Procesando Datos',
            text: 'Por favor espere...',
            allowOutsideClick: false,
            didOpen: () => { Swal.showLoading(); }
        });
        // Simulación de tu llamada de carga de datos
        cargarDatosParaEdicion(idExistente);
    }

    // Inicializar Select2 para habilitar la caja de búsqueda en el desplegable
    $('#txtDescInsumo').select2({
        placeholder: "Escriba para buscar o filtrar un insumo...",
        allowClear: true,
        width: '100%' // Es importante para que se adapte correctamente al div/columna de Bootstrap
    });

    $("#txtPrendasReq").on("keypress", function (e) {
        // Si la tecla presionada es un punto (.) o una coma (,) o la letra 'e', bloqueamos la acción
        if (e.key === "." || e.key === "," || e.key === "e" || e.key === "E") {
            e.preventDefault();
        }
    });

    //31/03/26
    // Detectar cambios en cualquier elemento del formulario
    $('#formLibroVisita').on('change input', 'input, select, textarea', function () {
        hayCambios = true;
    });

    //----------------------------------
    //01/04/2026
    //inicializarPruebasDesdeBD();
    //----------------------------------
    //sincronizarPruebasExistentes(); // Identifica qué pruebas vinieron de la BD y actualiza el contador global

    // Si tienes botones que agregan filas (como agregar colores o insumos), 
    // asegúrate de marcar el cambio ahí también
    $('#btnAgregarColor, #btnAgregarInsumo').click(function () {
        hayCambios = true;
    });

    // 2. Luego mandamos a dibujar el primer color (o la tabla por defecto)
    if (recetaMaster.Colores && recetaMaster.Colores.length > 0) {
        actualizarVistaColores(); // <-- ESTA función es la que adentro debe tener el inyectarPruebasDOM() al final
    }
    //sincronizarPruebasExistentes(); // Identifica qué pruebas vinieron de la BD y actualiza el contador global
    //inyectarPruebasDOM(); // Redibuja las columnas con las pruebas encontradas
});

function cargarDatosParaEdicion(id) {
    $.get("/Home/ObtenerRecetaCompleta", { id: id }, function (res) {
        const d = res.data;
        // Llenar cabecera

        // 1. Re-formatear la fecha de DD/MM/YYYY a YYYY-MM-DD
        if (d.FechaUDP) {
            // Suponiendo que d.FechaUDP viene como "05/03/2026"
            let partes = d.FechaUDP.split('/');
            if (partes.length === 3) {
                let fechaISO = `${partes[2]}-${partes[1]}-${partes[0]}`;
                $("#dtFechaUDP").val(fechaISO);
            }
        }
        $("#txtNP").val(d.NP);
        $("#txtTemporada").val(d.Temporada);
        $("#txtItem").val(d.Item);
        $("#txtOperario").val(d.Operario);
        $("#txtTecnica").val(d.Tecnica);
        $("#txtCombo").val(d.ComboCabecera);
        $("#txtCliente").val(d.Cliente);
        $("#txtEstilo").val(d.Estilo);
        $("#txtEstiloPropio").val(d.EstiloPropio);
        $("#txtPrendasReq").val(d.PrendasReq);
        // $("#txtConcepto").val(d.Concepto); // Removido
        // Llenar Radios Horizontales
        $("#txtUbicacion").val(d.Ubicacion);
        //Inicio
        $("#txtArte").val(d.Arte);

        /*$(`input[name='ubicacion'][value='${d.Ubicacion}']`).prop('checked', true);*/
        // Dentro de la carga de datos:
        res.data.Colores.forEach(c => {
            c.Insumos.forEach(i => {
                // Aseguramos que 'Cantidad' exista para que actualizarVistaColores no de error
                i.Cantidad = i.Cantidad || i.GramosUDP;
            });
        });
        // Reconstruir objeto global recetaMaster
        recetaMaster.Colores = d.Colores;

        // Justo después de cargar los datos del Model, sincronizamos las pruebas
        sincronizarPruebasExistentes();

        // Dibujamos la vista por primera vez
        actualizarVistaColores();  // Tu función para pintar la tabla de colores

        // AGREGA ESTA LÍNEA AL FINAL DE LA FUNCIÓN:
        inyectarPruebasDOM();

        Swal.close(); // Cerrar el "Break Time" cuando termine
    });
}


function actualizarVistaColores() {
    const contenedor = $("#contenedorColores"); // Asegúrate que este ID exista en tu HTML
    contenedor.empty(); // Limpiar antes de redibujar

    if (recetaMaster.Colores.length === 0) {
        contenedor.append('<div class="alert alert-info text-center">No hay colores agregados.</div>');
        return;
    }

    recetaMaster.Colores.forEach((color, indexColor) => {
        let htmlColor = window.g_isReadOnly ?
            `
            <div class="panel panel-info shadow-sm" style="margin-bottom: 15px;">
                <div class="panel-heading" style="background-color: #f5f5f5; border-color: #bce8f1;">
                    <div class="row">
                        <div class="col-md-6">
                            <h4 class="panel-title" style="color: #31708f;">
                                <strong>Color Pantone:</strong> ${color.Nombre}
                            </h4>
                        </div>
                    </div>
                </div>
                <div class="panel-body">
                    <table class="table table-condensed table-hover">
                        <thead>
                            <tr class="active">
                                <th style="width: 15%">Código</th>
                                <th style="width: 35%">Insumo</th>
                                <th style="width: 28%" class="text-right">STRIKE OFF</th>
                            </tr>
                        </thead>
                        <tbody>`
            : `
            <div class="panel panel-info shadow-sm" style="margin-bottom: 15px;">
                <div class="panel-heading" style="background-color: #f5f5f5; border-color: #bce8f1;">
                    <div class="row">
                        <div class="col-md-6">
                            <h4 class="panel-title" style="color: #31708f;">
                                <strong>Color Pantone:</strong> ${color.Nombre}
                            </h4>
                        </div>
                        <div class="col-md-6 text-right">
                            <button type="button" class="btn btn-danger btn-xs" onclick="eliminarColor(${indexColor})">
                                <i class="fa fa-trash"></i> Eliminar Color
                            </button>
                        </div>
                    </div>
                </div>
                <div class="panel-body">
                    <table class="table table-condensed table-hover">
                        <thead>
                            <tr class="active">
                                <th style="width: 15%">Código</th>
                                <th style="width: 35%">Insumo</th>
                                <th style="width: 28%" class="text-right">STRIKE OFF</th>
                                <th style="width: 10%"></th>
                            </tr>
                        </thead>
                        <tbody>`;

        // Iterar sobre los insumos de este color
        color.Insumos.forEach((insumo, indexInsumo) => {
            htmlColor += window.g_isReadOnly ? `
                <tr data-idinsumo="${insumo.IDInsumo || insumo.IdInsumo || ''}">
                    <td><strong>${insumo.CodigoInsumo}</strong></td>
                    <td><strong>${insumo.Descripcion} (gr)</strong></td>
                    <td class="text-right"><strong>${insumo.Cantidad.toFixed(2)}</strong></td>
                </tr>`
                : `
                <tr data-idinsumo="${insumo.IDInsumo || insumo.IdInsumo || ''}">
                    <td>
                        <input type="hidden" class="hidden-id-insumo" value="${insumo.IDInsumo || insumo.IdInsumo || ''}" />
                        <strong>${insumo.CodigoInsumo}</strong>
                    </td>
                    <td><strong>${insumo.Descripcion} (gr)</strong></td>
                    <td class="text-right"><strong>${insumo.Cantidad.toFixed(2)}</strong></td>
                    <td class="text-center">
                        <button type="button" class="btn btn-link btn-xs text-danger" 
                                onclick="eliminarInsumo(${indexColor}, ${indexInsumo})">
                            <i class="fa fa-times"></i>
                        </button>
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

    // Opcional: Actualizar el select de "Color Destino" para agregar nuevos insumos
    actualizarComboColores();

    //01/04/26
    // AGREGA ESTA LÍNEA AL FINAL DE LA FUNCIÓN:
    inyectarPruebasDOM();
}
function actualizarComboColores() {
    const ddl = $("#ddlColorDestino");
    ddl.empty(); // Limpiar opciones anteriores
    ddl.append('<option value="">Seleccione</option>');

    // Recorremos el objeto global para llenar el combo con los nombres de colores actuales
    recetaMaster.Colores.forEach((color, index) => {
        ddl.append(`<option value="${index}">${color.Nombre}</option>`);
    });
}

function agregarInsumoAColor() {

    // 1. Capturamos el valor completo (Ej: "12345678 TELA ALGODON ROJO")
    const valorCompleto = $("#txtDescInsumo").val() || "";

    let codigoReal = "";
    let descripcionReal = "";

    // 2. Separamos el texto asegurándonos de que tenga la longitud mínima
    if (valorCompleto.length >= 8) {
        codigoReal = valorCompleto.substring(0, 8).trim();
        descripcionReal = valorCompleto.substring(8).trim();
    } else {
        Swal.fire("Atención", "Por favor seleccione un insumo válido del listado.", "warning");
        return; // Detenemos la función si no hay un insumo válido
    }

    // 3. Capturamos el resto de los datos del formulario
    const indexColor = $("#ddlColorDestino").val();
    const cantidad = parseFloat($("#txtCantInsumo").val());

    // 4. Validamos que no falte nada
    //Aceptamos el valor cero || isNaN(cantidad)
    //Por si desea agregar un nuevo insumo, que no se agrego en el primer registro.
    if (indexColor === "" || !codigoReal || !descripcionReal ) {
        Swal.fire("Atención", "Seleccione el color a asignar y la cantidad", "warning");
        return;
    }

    // 5. Agregamos al objeto global con los datos limpios y separados
    recetaMaster.Colores[indexColor].Insumos.push({
        CodigoInsumo: codigoReal,
        Descripcion: descripcionReal,
        Cantidad: cantidad
    });

    // 6. Limpiamos los campos y refrescamos la pantalla
    $("#txtDescInsumo").val("").trigger('change');
    $("#txtStock").val("");
    $("#txtCantInsumo").val("");
    actualizarVistaColores();
}

function guardarRecetaCompleta() {
    // 1. Validaciones básicas de cabecera

    // 1. IMPORTANTE: Capturar el ID para que el SP sepa que es EDITAR y no NUEVO
    recetaMaster.Dato = $("#hdnIdVisita").val();
    recetaMaster.NP = $("#txtNP").val();
    var fechaSeleccionada = $("#dtFechaUDP").val();
    recetaMaster.Operario = $("#txtOperario").val();
    recetaMaster.Tecnica = $("#txtTecnica").val();
    recetaMaster.Cliente = $("#txtCliente").val();
    recetaMaster.Temporada = $("#txtTemporada").val();
    recetaMaster.Estilo = $("#txtEstilo").val();
    recetaMaster.EstiloPropio = $("#EstiloPropio").val();
    recetaMaster.Item = $("#txtItem").val();
    recetaMaster.ComboCabecera = $("#txtCombo").val();
    recetaMaster.PrendasReq = $("#txtPrendasReq").val();
    // recetaMaster.Concepto = $("#txtConcepto").val(); // Removido
    recetaMaster.Ubicacion = $("#txtUbicacion").val();
    recetaMaster.Arte = $("#txtArte").val();

    // VALIDACIÓN: Si el valor es una cadena vacía, nulo o indefinido
    if (!fechaSeleccionada || fechaSeleccionada.trim() === "") {
        Swal.fire("Aviso", "La fecha es obligatoria", "warning");
        return; // Detiene la ejecución de la función
    }

    if (fechaSeleccionada) {
        // Dividimos y reordenamos
        var partes = fechaSeleccionada.split('-');
        // Formato DD/MM/YYYY
        recetaMaster.FechaUDP = partes[2] + '/' + partes[1] + '/' + partes[0];
    }

    // Validación básica    
    if (recetaMaster.Operario.length === 0) {
        Swal.fire("Aviso", "Agregue el muestrista", "warning");
        return;
    }
    if (recetaMaster.Tecnica.length === 0) {
        Swal.fire("Aviso", "Agregue la técnica", "warning");
        return;
    }
    if (recetaMaster.Cliente.length === 0) {
        Swal.fire("Aviso", "Agregue el cliente", "warning");
        return;
    }
    if (recetaMaster.Temporada.length === 0) {
        Swal.fire("Aviso", "Agregue la temporada", "warning");
        return;
    }

    if (recetaMaster.Item.length === 0) {
        Swal.fire("Aviso", "Agregue el item", "warning");
        return;
    }
    // Validador de Concepto removido de cabecera
    if (recetaMaster.Colores.length === 0) {
        Swal.fire("Aviso", "Agregue al menos un color pantone", "warning");
        return;
    }
    if (recetaMaster.Colores[0].Insumos.length === 0) {
        Swal.fire("Aviso", "Agregue al menos un Insumo para el color", "warning");
        return;
    }

    if (!recetaMaster.PruebasGlobales) {
        recetaMaster.PruebasGlobales = [];
    }

    // 3. Enviar al Servidor mediante AJAX
    Swal.fire({
        title: '¿Guardar Cambios?',
        text: "Se actualizará la receta y sus componentes",
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Sí, guardar'
    }).then((result) => {
        if (result.value) {
            $.ajax({
                url: '/Home/RegistrarVisitaCompletaEditar', // Asegúrate que coincida con tu Controller
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify(recetaMaster),
                success: function (res) {
                    if (res.result === "success") {
                        Swal.fire("¡Actualizado!", "Receta actualizada correctamente", "success")
                            .then(() => { window.location.href = '/Home/MantenimientoVisitas'; });
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

//*********************************************


// --- FUNCIONES AJAX ---

// Evento para autocompletar el codigo
$(document).on("change", "#txtDescInsumo", function () { //txtDescInsumo
    var dato = $(this).val();
    if (dato) {
        $.get("/Home/ObtenerCodigoInsumo", { dato: dato }, function (res) { // codigo
            //$("#ddlInsumo").val(res.CodigoInsumo); //cboInsumo  -- descripcion
            //$("#txtDescInsumo").val(res.Descripcion).trigger('change');
            $("#txtStock").val(res.Stock); //cboInsumo  -- descripcion
        });
    } else {
        //$("#ddlInsumo").val(""); //cboInsumo
        //$("#txtDescInsumo").val("").trigger('change');
        $("#txtStock").val(""); //cboInsumo
    }
});


// AGREGAR NIVEL 2 (COLOR)
// ESTA FUNCIÓN DEBE ESTAR AL FINAL DEL ARCHIVO, FUERA DEL $(document).ready
function agregarNuevoColor() {
    //Si tienes funciones que agregan filas dinámicamente(como agregarColor), asegúrate de que no se ejecuten si el modo es solo lectura:
    if ('@ViewBag.IsReadOnly'.toLowerCase() === 'true') return;

    // 1. Obtener el valor del input
    var nombreColor = $("#txtNuevoColorNombre").val();
    var comboColor = $("#txtCombo").val();

    // 2. Validar que no esté vacío
    if (nombreColor.trim() === "") {
        Swal.fire("Aviso", "Por favor, ingrese un nombre para el color pantone.", "warning");
        return;
    }

    // 3. Crear el nuevo objeto de color (siguiendo la estructura de ColorBE)
    var nuevoColor = {
        Nombre: nombreColor,
        Combo: comboColor, // Genera un combo automático (C1, C2...)
        Insumos: [] // Lista de insumos vacía al inicio
    };

    // 4. Agregar al array global
    recetaMaster.Colores.push(nuevoColor);

    // 5. Limpiar el input y refrescar la vista
    $("#txtNuevoColorNombre").val("");
    actualizarVistaColores();

    Swal.fire("Éxito", "Color Pantone agregado. Ahora puede añadirle insumos.", "success");
}

function eliminarColor(index) {
    Swal.fire({
        title: '¿Eliminar Color Pantone?',
        text: "Se borrarán todos los insumos asociados a este color pantone.",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        confirmButtonText: 'Sí, eliminar'
    }).then((result) => {
        if (result.value) {
            recetaMaster.Colores.splice(index, 1); // Elimina del array
            actualizarVistaColores(); // Refresca el HTML
        }
    });
}

// Función para eliminar un Insumo específico (la X)
function eliminarInsumo(indexColor, indexInsumo) {
    // Eliminamos solo el insumo del array interno del color seleccionado
    recetaMaster.Colores[indexColor].Insumos.splice(indexInsumo, 1);
    actualizarVistaColores(); // Refresca el HTML
}

$('#btnCancelar').on('click', function (e) {
    e.preventDefault();
    window.location.href = "/Home/MenuProveedor";
});


$('#btnGrabarVisitaCorregirSCTR').on('click', function (e) {
    console.log("ingrese");
    e.preventDefault();
    var form = document.getElementById("formLibroVisita");
    var data = new FormData(form);
    $.ajax({
        url: $('#formLibroVisita').attr('action'),
        data: data,
        dataType: 'json',
        type: 'post',
        cache: false,
        contentType: false,
        processData: false,
        beforeSend: function () {
            console.log("ingrese1");
            $('#btnGrabarVisitaCorregirSCTR').val('Enviando....');
            $('#btnGrabarVisitaCorregirSCTR').attr('disabled', 'disabled');
        },
        complete: function () {
            console.log("ingrese2");
            $('#btnGrabarVisitaCorregirSCTR').val('<i class="fa fa-check"></i>&nbsp;Grabar');

            $('#btnGrabarVisitaCorregirSCTR').removeAttr('disabled');

        },
        success: function (json) {
            console.log("ingrese3");
            $('#btnGrabarVisitaCorregirSCTR').val('<i class="fa fa-check"></i>&nbsp;Grabar');

            $('#btnGrabarVisitaCorregirSCTR').removeAttr('disabled');

            if (json.result == 'warning') {
                console.log("ingrese4");
                Swal.fire({
                    icon: 'warning',
                    title: json.title,
                    text: json.message
                });
            } else if (json.result == 'success') {
                setTimeout(() => {
                    console.log("ingrese5");
                    $('#btnGrabarVisitaCorregirSCTR').val('Enviando....');
                    $('#btnGrabarVisitaCorregirSCTR').attr('disabled', 'disabled');
                }, 200)
                Swal.fire({
                    icon: 'success',
                    title: json.title,
                    text: json.message
                    //footer: 'Se genero la solicitud de reprogramación correctamente.'
                }).then(function (result) {
                    if (result.value) {
                        window.location.href = '/Home/MantenimientoVisitas';
                    }
                }
                );
            } else if (json.result == 'error') {
                Swal.fire({
                    icon: 'error',
                    title: json.title,
                    text: json.message
                });
            }
        },
        error: function () {
            $('#btnGrabarVisitaCorregirSCTR').val('<i class="fa fa-check"></i>&nbsp;Grabar');
            $('#btnGrabarVisitaCorregirSCTR').removeAttr('disabled');
        }
    });

});


function valideKey(evt) {
    // code is the decimal ASCII representation of the pressed key.
    var code = (evt.which) ? evt.which : evt.keyCode;
    if (code == 8) { // backspace.
        return true;
    } else if (code >= 48 && code <= 57) { // is a number.
        return true;
    } else { // other keys.
        return false;
    }
}

//Archivo por descargar

function descargarArchivo(id) {
    $.ajax({
        type: "POST",
        url: "/Home/Archivo",
        data: { id },
        success: function (response) {
            var respuesta = response;
            const nombreArchivo = document.getElementById('CodigoVisita').value;
            const formato = document.getElementById('Formato').value;
            const blob = base64ToBlob(respuesta.Mensaje_Respuesta, formato);
            guardarArchivo(blob, nombreArchivo);
        },
        error: function (jqXHR, textStatus, errorThrown) {
            console.log(jqXHR);
            console.log(textStatus);
            console.log(errorThrown);
            alert("Ocurrió un error al verificar los CFDI(s): " + jqXHR);
        }
    });
}

function base64ToBlob(base64, type = "application/octet-stream") {
    const binStr = atob(base64);
    const len = binStr.length;
    const arr = new Uint8Array(len);
    for (let i = 0; i < len; i++) {
        arr[i] = binStr.charCodeAt(i);
    }
    return new Blob([arr], { type: type });
}

function guardarArchivo(blob, filename) {
    if (window.navigator.msSaveOrOpenBlob) {
        window.navigator.msSaveOrOpenBlob(blob, filename);
    } else {
        const a = document.createElement('a');
        document.body.appendChild(a);
        const url = window.URL.createObjectURL(blob);
        a.href = url;
        a.download = filename;
        a.click();
        setTimeout(() => {
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);
        }, 0)
    }
}



