
// Función compatible para obtener parámetros de la URL (Global)
function getQueryParam(param) {
    var search = window.location.search.substring(1);
    var params = search.split('&');
    for (var i = 0; i < params.length; i++) {
        var pair = params[i].split('=');
        if (decodeURIComponent(pair[0]) === param) {
            return decodeURIComponent(pair[1] || '');
        }
    }
    return null;
}

$(document).ready(function () {

    // Verificar si hay un ID en la URL para editar
    var idEditar = getQueryParam('id');
    var ignorarEventosEstilo = false;

    $("#txtPrendasReq").on("keypress", function (e) {
        // Si la tecla presionada es un punto (.) o una coma (,) o la letra 'e', bloqueamos la acción
        if (e.key === "." || e.key === "," || e.key === "e" || e.key === "E") {
            e.preventDefault();
        }
    });

    $("#txtCantInsumo").on("change blur", function () {
        var val = parseFloat($(this).val());
        if (!isNaN(val)) {
            $(this).val(val.toFixed(2));
        }
    });

    // --- PERSISTENCIA DEL MODO DE REGISTRO ---
    var modoQuery = getQueryParam('modo');

    // Si no hay parámetro en la URL, forzamos que se comporte como INVERSO
    if (modoQuery === 'NORMAL') {
        // Lógica para modo normal
        $("#rdoModoNormal").prop("checked", true);
        $("#lblInfoModoRegistro").html('(Seleccione Cliente &rarr; Temporada &rarr; Estilo)');
        $("#txtCliente, #txtTemporada").prop("disabled", false);
    } else if (modoQuery === 'ITEM') {
        // Lógica para modo Búsqueda por Item
        $("#rdoModoItem").prop("checked", true);
        $("#lblInfoModoRegistro").html('(Busque directamente un <strong>Item</strong> [mín. 3 car.], los datos de cabecera se auto-rellenarán)');
        // Bloquear campos superiores; el punto de entrada es txtItem con Select2 AJAX
        $("#txtCliente, #txtTemporada, #txtEstilo, #txtEstiloPropio, #txtCombo").prop("disabled", true);
        // Deshabilitar Ubicación y Técnica en modo ITEM (se auto-rellenan al seleccionar Item)
        $("#txtUbicacion, #txtTecnica").prop("disabled", true);
    } else {
        // Por defecto (incluyendo modoQuery === 'INVERSO' o vacío), modo INVERSO
        $("#rdoModoInverso").prop("checked", true);
        $("#lblInfoModoRegistro").html('(Busque directamente un Estilo [mín. 4 letras], todo se auto-rellenará)');
        $("#txtCliente, #txtTemporada").prop("disabled", true);
        // 13/05/2026: Deshabilitar Ubicación y Técnica en modo INVERSO (se auto-rellenan al seleccionar Item)
        $("#txtUbicacion, #txtTecnica").prop("disabled", true);
    }

    // 1. CUANDO CAMBIA EL CLIENTE
    $("#txtCliente").on("change", function () {
        var clienteSeleccionado = $(this).val();
        // Limpiamos los tres combos dependientes
        $("#txtTemporada").empty();
        $("#txtEstilo").empty();
        $("#txtEstiloPropio").empty();
        $("#txtItem").empty();
        $("#txtUbicacion").empty();
        $("#txtCombo").empty(); // NUEVO
        if (clienteSeleccionado) {
            cargarTemporadas(clienteSeleccionado);
        }
    });

    // 2. CUANDO CAMBIA LA TEMPORADA
    $("#txtTemporada").on("change", function () {
        var clienteSeleccionado = $("#txtCliente").val();
        var temporadaSeleccionada = $(this).val().substring(0, 3);

        // Limpiamos Estilo e Item
        $("#txtEstilo").empty().append('<option value="">Seleccione un Estilo Cliente</option>');
        $("#txtEstiloPropio").empty().append('<option value="">Seleccione un Estilo Propio</option>');
        $("#txtItem").empty().append('<option value="">Seleccione un Item</option>');

        //$("#txtConcepto").empty().append('<option value="">Seleccione un Concepto</option>');
        $("#txtUbicacion").empty().append('<option value="">Seleccione un Ubicacion</option>');
        $("#txtCombo").empty().append('<option value="">Seleccione un Combo</option>'); // NUEVO
        $("#txtTecnica").empty().append('<option value="">Seleccione una Técnica</option>');

        if (clienteSeleccionado && temporadaSeleccionada) {
            //se cambiara 27/03/2026
            //cargarFiltrosEstiloItem(clienteSeleccionado, temporadaSeleccionada);
            //cargarItems(clienteSeleccionado, temporadaSeleccionada);
            cargarEstilos(clienteSeleccionado, temporadaSeleccionada);
        }
    });

    // SINCRONIZACIÓN: Cuando cambia txtEstilo (Estilo Cliente) -> Cambia txtEstiloPropio
    $("#txtEstilo").on("change", function () {
        if (ignorarEventosEstilo) return;
        // Obtenemos la opción seleccionada
        var opcionSeleccionada = $(this).find('option:selected');

        // Leemos el valor equivalente del atributo data-propio
        var idEstiloPropioAsociado = opcionSeleccionada.data('propio');

        if (idEstiloPropioAsociado) {
            // Evitamos un bucle infinito validando si ya tiene ese valor
            if ($("#txtEstiloPropio").val() !== idEstiloPropioAsociado) {
                // Asignamos el valor y actualizamos la vista de Select2
                $("#txtEstiloPropio").val(idEstiloPropioAsociado).trigger('change.select2');
            }
        } else {
            // Si elige "Seleccione...", limpiamos el otro
            if ($("#txtEstiloPropio").val() !== "") {
                $("#txtEstiloPropio").val("").trigger('change.select2');
                //limpiar 27/03/26
                $("#txtUbicacion").empty().append('<option value="">Seleccione una Ubicación</option>');
                $("#txtCombo").empty().append('<option value="">Seleccione un Combo</option>');
                $("#txtTecnica").empty().append('<option value="">Seleccione una Técnica</option>');
            }
        }
        // ¡IMPORTANTE! Aquí debe ir el código que ya tenías para cargar
        // Combos, Conceptos, Items, etc. basados en txtEstilo.
        var isNormal = $("#rdoModoNormal").is(":checked");
        if (isNormal) {
            rellenarCombos();
        } else {
            //autocompletarCabeceraDesdeEstilo($(this).val(), false);
        }
    });

    // SINCRONIZACIÓN: Cuando cambia txtEstiloPropio -> Cambia txtEstilo (Estilo Cliente)
    $("#txtEstiloPropio").on("change", function () {
        if (ignorarEventosEstilo) return;
        // Obtenemos la opción seleccionada
        var opcionSeleccionada = $(this).find('option:selected');

        // Leemos el valor equivalente del atributo data-cliente
        var idEstiloClienteAsociado = opcionSeleccionada.data('cliente');

        if (idEstiloClienteAsociado) {
            // Evitamos un bucle infinito validando si ya tiene ese valor
            if ($("#txtEstilo").val() !== idEstiloClienteAsociado) {

                // NOTA: Al ejecutar esto y disparar 'change', 
                // se activará automáticamente el evento de arriba $("#txtEstilo").on("change")
                // por lo que cargará la data subordinada (Combos, Conceptos) sin que hagas nada extra.
                $("#txtEstilo").val(idEstiloClienteAsociado).trigger('change');
                // Re-aplicamos diseño Select2
                $("#txtEstilo").trigger('change.select2');
            }
        } else {
            if ($("#txtEstilo").val() !== "") {
                $("#txtEstilo").val("").trigger('change.select2');
                //limpiar 27/03/26
                $("#txtUbicacion").empty().append('<option value="">Seleccione una Ubicación</option>');
                $("#txtCombo").empty().append('<option value="">Seleccione un Combo</option>');
                $("#txtTecnica").empty().append('<option value="">Seleccione una Técnica</option>');
            }
        }
        // ¡IMPORTANTE! Aquí debe ir el código que ya tenías para cargar
        // Combos, Conceptos, Items, etc. basados en txtEstilo.
        var isNormal = $("#rdoModoNormal").is(":checked");
        if (isNormal) {
            // rellenarCombos no suele llamarse aquí, ya que txtEstilo recibe un trigger change y lo hace
        } else {
            //autocompletarCabeceraDesdeEstilo($(this).val(), true);
        }
    });


    var isNormal = $("#rdoModoNormal").is(":checked");
    if (isNormal) {
        // 1. Hacer los campos desplegables editables (permiten texto libre)
        // Optimización: Diferir ligeramente la inicialización de Select2 para no bloquear el renderizado inicial
        setTimeout(function() {
            $('#txtCliente, #txtTemporada, #txtEstilo, #txtEstiloPropio, #txtCombo, #txtItem, #txtUbicacion, #txtTecnica').select2({
                placeholder: "Seleccione o escriba...",
                allowClear: true,
                width: '100%'
            });
        }, 100);


    } else if (modoQuery !== 'ITEM') {
        // 1. Hacer los campos desplegables editables (permiten texto libre)
        // Optimización: Diferir inicialización de Select2 básicos
        setTimeout(function() {
            $('#txtCliente, #txtTemporada, #txtCombo, #txtItem, #txtUbicacion, #txtTecnica').select2({
                placeholder: "Seleccione o escriba...",
                allowClear: true,
                width: '100%'
            });
        }, 100);


        // Optimización: Búsqueda dinámica para Estilo Cliente
        $('#txtEstilo').select2({
            placeholder: "Seleccione o escriba...",
            allowClear: true,
            width: '100%',
            ajax: {
                url: "/Home/BuscarEstilosInversoAjax",
                dataType: 'json',
                delay: 300,
                data: function (params) {
                    return {
                        q: params.term,
                        esPropio: false
                    };
                },
                processResults: function (data) {
                    return {
                        results: data.results
                    };
                },
                cache: true
            },
            minimumInputLength: 4,
            language: {
                inputTooShort: function () {
                    return "Por favor, ingrese 4 o más caracteres";
                },
                noResults: function () {
                    return "No se encontraron resultados";
                },
                searching: function () {
                    return "Buscando...";
                }
            }
        }).on('select2:select', function (e) {
            if (ignorarEventosEstilo) return;
            var data = e.params.data;
            autocompletarCabeceraDesdeEstilo(data.id, false);
        });

        // Optimización: Búsqueda dinámica para Estilo Propio
        $('#txtEstiloPropio').select2({
            placeholder: "Seleccione o escriba...",
            allowClear: true,
            width: '100%',
            ajax: {
                url: "/Home/BuscarEstilosInversoAjax",
                dataType: 'json',
                delay: 300,
                data: function (params) {
                    return {
                        q: params.term,
                        esPropio: true
                    };
                },
                processResults: function (data) {
                    return {
                        results: data.results
                    };
                },
                cache: true
            },
            minimumInputLength: 4,
            language: {
                inputTooShort: function () {
                    return "Por favor, ingrese 4 o más caracteres";
                },
                noResults: function () {
                    return "No se encontraron resultados";
                },
                searching: function () {
                    return "Buscando...";
                }
            }
        }).on('select2:select', function (e) {
            if (ignorarEventosEstilo) return;
            var data = e.params.data;
            autocompletarCabeceraDesdeEstilo(data.id, true);
        });

    } else {  // modoQuery === 'ITEM'
        // ── MODO BÚSQUEDA POR ITEM ────────────────────────────────────────────
        // Inicializar campos no-pivot como Select2 básico (sin AJAX, bloqueados)
        // Optimización: Diferir inicialización de Select2 básicos en modo ITEM
        setTimeout(function() {
            $('#txtCliente, #txtTemporada, #txtCombo, #txtUbicacion, #txtTecnica').select2({
                placeholder: "Seleccione o escriba...",
                allowClear: true,
                width: '100%'
            });
        }, 100);


        // Estilo Cliente y Propio también como Select2 básico
        // (se habilitarán y rellenarán tras la selección del Item)
        $('#txtEstilo, #txtEstiloPropio').select2({
            placeholder: "Seleccione o escriba...",
            allowClear: true,
            width: '100%'
        });

        // ── CAMPO PIVOT: Item con búsqueda AJAX dinámica ──────────────────────
        $('#txtItem').select2({
            placeholder: "Escriba un código de Item [mín. 3 caracteres]...",
            allowClear: true,
            width: '100%',
            ajax: {
                url: "/Home/BuscarDatosPorItem",
                dataType: 'json',
                delay: 350,
                data: function (params) {
                    return { item: params.term || '' };
                },
                processResults: function (data) {
                    // El SP devuelve filas con COD_ITEM duplicado por combinaciones
                    // Agrupamos para mostrar sólo Items únicos en el dropdown
                    var seen = {};
                    var resultados = [];
                    $.each(data, function (i, row) {
                        if (!seen[row.CodItem]) {
                            seen[row.CodItem] = true;
                            resultados.push({ id: row.CodItem, text: row.CodItem, _data: row });
                        }
                    });
                    return { results: resultados };
                },
                cache: false
            },
            minimumInputLength: 3,
            language: {
                inputTooShort: function () { return "Por favor ingrese 3 o más caracteres..."; },
                noResults: function () { return "No se encontraron ítems"; },
                searching: function () { return "Buscando..."; }
            }
        }).on('select2:select', function (e) {
            // Al seleccionar el Item, lanzamos el autocompletado
            autocompletarCabeceraDesdeItem(e.params.data.id);
        });

    } // fin else (modoQuery === 'ITEM')

    // Inicializar Select2 para habilitar la caja de búsqueda en el desplegable (todos los modos)
    // La inicialización de Select2 para #txtDescInsumo se maneja en la vista (Visita.cshtml) 
    // debido a su configuración AJAX específica.


});

// --- FUNCIONES AJAX ---
// Inicio  27/03/2026


// --- NUEVA FUNCIONALIDAD: MODO DE REGISTRO INVERSO ---
function cambiarModoRegistro() {
    var modo;
    if ($("#rdoModoNormal").is(":checked")) modo = "NORMAL";
    else if ($("#rdoModoItem").is(":checked")) modo = "ITEM";
    else modo = "INVERSO";

    // Obtener parámetros actuales
    var search = window.location.search.substring(1);
    var params = search.split('&');
    var newParams = [];
    var foundModo = false;

    for (var i = 0; i < params.length; i++) {
        if (!params[i]) continue;
        var pair = params[i].split('=');
        if (pair[0] === 'modo') {
            newParams.push('modo=' + modo);
            foundModo = true;
        } else {
            newParams.push(params[i]);
        }
    }

    if (!foundModo) {
        newParams.push('modo=' + modo);
    }

    // Redirigir con los nuevos parámetros
    window.location.href = window.location.pathname + '?' + newParams.join('&');
}

function cargarTodosLosEstilosInverso() {
    $.ajax({
        url: "/Home/ListarTodosLosEstilos",
        type: "GET",
        success: function (data) {
            $("#txtEstilo").empty().append('<option value="">Seleccione o escriba un Estilo Cliente</option>');
            $("#txtEstiloPropio").empty().append('<option value="">Seleccione o escriba un Estilo Propio</option>');

            if (data && data.length > 0) {
                $.each(data, function (i, e) {
                    // Llenar combo Estilo Cliente
                    if (e.CodEstiloCliente && e.CodEstiloCliente.trim() !== '') {
                        var optC = $('<option>', { value: e.CodEstiloCliente }).text(e.CodEstiloCliente);
                        optC.attr('data-propio', e.CodEstiloPropio || '');
                        $("#txtEstilo").append(optC);
                    }
                    // Llenar combo Estilo Propio
                    if (e.CodEstiloPropio && e.CodEstiloPropio.trim() !== '') {
                        var optP = $('<option>', { value: e.CodEstiloPropio }).text(e.CodEstiloPropio);
                        optP.attr('data-cliente', e.CodEstiloCliente || '');
                        $("#txtEstiloPropio").append(optP);
                    }
                });
            }
        }
    });
}

function autocompletarCabeceraDesdeEstilo(estiloBuscado, esPropio) {
    if (!estiloBuscado) return;
    var isNormal = $("#rdoModoNormal").is(":checked");
    if (isNormal) return; // Solo funciona en modo inverso


    $.ajax({
        url: "/Home/ObtenerDatosEstiloInverso",
        type: "GET",
        data: { estiloBuscar: estiloBuscado, esPropio: esPropio },
        success: function (data) {
            if (data && data.Cliente) {
                ignorarEventosEstilo = true; // ACTIVAR BLOQUEO

                // Sincronizar los estilos (Cliente <-> Propio)
                if (esPropio) {
                    // Si seleccionamos Propio, actualizamos Cliente
                    if ($('#txtEstilo').find("option[value='" + data.Estilo + "']").length == 0) {
                        $('#txtEstilo').append(new Option(data.Estilo, data.Estilo, true, true)).trigger('change.select2');
                    } else {
                        $('#txtEstilo').val(data.Estilo).trigger('change.select2');
                    }
                } else {
                    // Si seleccionamos Cliente, actualizamos Propio
                    if ($('#txtEstiloPropio').find("option[value='" + data.EstiloPropio + "']").length == 0) {
                        $('#txtEstiloPropio').append(new Option(data.EstiloPropio, data.EstiloPropio, true, true)).trigger('change.select2');
                    } else {
                        $('#txtEstiloPropio').val(data.EstiloPropio).trigger('change.select2');
                    }
                }

                // Auto-agregar y seleccionar el cliente si no existe
                if ($('#txtCliente').find("option[value='" + data.Cliente + "']").length == 0) {
                    $('#txtCliente').append(new Option(data.Cliente, data.Cliente, true, true));
                } else {
                    $('#txtCliente').val(data.Cliente).trigger('change.select2');
                }

                // Temporada
                if ($('#txtTemporada').find("option[value='" + data.Temporada + "']").length == 0) {
                    $('#txtTemporada').append(new Option(data.Temporada, data.Temporada, true, true));
                } else {
                    $('#txtTemporada').val(data.Temporada).trigger('change.select2');
                }

                //Disparar carga de sub-combos ahora que Cliente y Temporada existen   
                var temporadaSeleccionada = $("#txtTemporada").val().substring(0, 3);
                if (temporadaSeleccionada != "") {
                    //cargarItems(data.Cliente, temporadaSeleccionada, data.EstiloPropio);
                    rellenarCombos();
                }

                // Pequeño retardo para asegurar que los triggers de Select2 terminen de procesarse
                setTimeout(function () {
                    ignorarEventosEstilo = false;
                }, 500);
            }
        }
    });
}
// -----------------------------------------------------

// ── MODO BÚSQUEDA POR ITEM: AutoRelleno de Cabecera ──────────────────────────
function autocompletarCabeceraDesdeItem(itemSeleccionado) {
    if (!itemSeleccionado) return;

    $.ajax({
        url: '/Home/BuscarDatosPorItem',
        type: 'GET',
        data: { item: itemSeleccionado },
        dataType: 'json',
        success: function (data) {
            if (!data || data.length === 0) {
                Swal.fire('Sin resultados', 'No se encontraron datos para el ítem seleccionado.', 'warning');
                return;
            }

            // Tomamos la primera fila (el SP puede retornar varias por combinaciones)
            var row = data[0];

            // ── 1. Rellenar campos auto-rellenados (bloqueados) ───────────
            // Cliente
            $('#txtCliente').empty().append(new Option(row.CodCliente, row.CodCliente, true, true)).trigger('change.select2');

            // Temporada
            $('#txtTemporada').empty().append(new Option(row.CodTemcli, row.CodTemcli, true, true)).trigger('change.select2');

            // Ubicación (rellenar y auto-seleccionar si viene del SP)
            if (row.Ubicacion && row.Ubicacion.trim() !== '') {
                $('#txtUbicacion').empty()
                    .append(new Option(row.Ubicacion, row.Ubicacion, true, true))
                    .trigger('change.select2');
            } else {
                $('#txtUbicacion').empty().append('<option value="">Seleccione una Ubicación</option>').trigger('change.select2');
            }

            // Técnica (DESCRIPCION_TECNICA del SP)
            if (row.DescripcionTecnica && row.DescripcionTecnica.trim() !== '') {
                $('#txtTecnica').empty()
                    .append(new Option(row.DescripcionTecnica, row.DescripcionTecnica, true, true))
                    .trigger('change.select2');
            } else {
                $('#txtTecnica').empty().append('<option value="">Seleccione una Técnica</option>').trigger('change.select2');
            }

            // Los campos Estilo Cliente, Estilo Propio y Combo permanecen bloqueados

            Swal.fire({
                icon: 'success',
                title: 'Item encontrado',
                text: 'Se han auto-rellenado los datos de la cabecera.',
                timer: 2000,
                showConfirmButton: false
            });
        },
        error: function () {
            Swal.fire('Error', 'No se pudo comunicar con el servidor. Intente nuevamente.', 'error');
        }
    });
}
// ──────────────────────────────────────────────────────────────────────────────

function rellenarCombos() {
    //Se llena el campo de combo que depende del Estilo Propio
    var cliente = $("#txtCliente").val();
    var temporada = $("#txtTemporada").val().substring(0, 3);
    var estilo = $("#txtEstilo").val();
    var estiloProp = $("#txtEstiloPropio").val();
    var ubicacion = $("#txtUbicacion").val();
    var item = $("#txtItem").val();

    $("#txtUbicacion").empty().append('<option value="">Seleccione una Ubicación</option>');
    $("#txtCombo").empty().append('<option value="">Seleccione un Combo</option>'); // NUEVO

    cargarItems(cliente, temporada, estiloProp);

    if (cliente && temporada && estilo) {
        if (item) {
            //cargarConceptos(cliente, temporada, estilo, item);
            cargarUbicacion(cliente, temporada, estiloProp, item);
            cargarTecnica(cliente, temporada, estiloProp, item);
        }
        // NUEVO: Cargar combos basados en el estilo seleccionado
        cargarCombos(cliente, temporada, estiloProp);
    }
}


// Carga de Items
function cargarItems(cliente, temporada, estiloPropio) {
    $("#txtItem").empty().append('<option value="">Seleccione un Item</option>');
    $.ajax({
        url: "/Home/ListarItems", // Asegúrate que esta ruta exista en tu HomeController
        type: "GET",
        data: { cliente: cliente, temporada: temporada, estiloPropio: estiloPropio },
        success: function (data) {
            if (data && data.length > 0) {
                $.each(data, function (i, item) {
                    $("#txtItem").append(new Option(item.CodItem, item.CodItem));
                });
            }
        }
    });
}

// Carga de Estilos (Cliente y Propio) relacionados 1 a 1
function cargarEstilos(cliente, temporada) {
    $.ajax({
        url: "/Home/ListarEstilos", // Esta ruta asume que devuelve la lista de la BD
        type: "GET",
        data: { cliente: cliente, temporada: temporada },
        success: function (data) {
            if (data && data.length > 0) {
                $.each(data, function (i, item) {

                    // Llenamos el combo Estilo Cliente y le incrustamos el ID del Propio
                    var optionEstilo = $('<option></option>')
                        .val(item.CodEstiloCliente)
                        .text(item.CodEstiloCliente)
                        .attr('data-propio', item.CodEstiloPropio);
                    $("#txtEstilo").append(optionEstilo);

                    // Llenamos el combo Estilo Propio y le incrustamos el ID del Cliente
                    var optionPropio = $('<option></option>')
                        .val(item.CodEstiloPropio)
                        .text(item.CodEstiloPropio)
                        .attr('data-cliente', item.CodEstiloCliente);
                    $("#txtEstiloPropio").append(optionPropio);
                });
            }
        }
    });
}
// Fin  -  27/03/2026

// --- FUNCIONES AJAX ---
// Evento para autocompletar el codigo
$(document).on("change", "#txtDescInsumo", function () { //txtDescInsumo
    var dato = $(this).val();
    if (dato) {
        $.get("/Home/ObtenerCodigoInsumo", { dato: dato }, function (res) { // codigo
            //$("#cboInsumo").val(res.CodigoInsumo); //cboInsumo  -- descripcion
            $("#txtStock").val(res.Stock); //cboInsumo  -- descripcion
        });
    } else {
        //$("#cboInsumo").val(""); //cboInsumo
        $("#txtStock").val(""); //cboInsumo
    }
});

// CUANDO CAMBIA EL ITEM (Gatilla la carga de conceptos)
$("#txtItem").on("change", function () {
    var cliente = $("#txtCliente").val();
    var temporadaRaw = $("#txtTemporada").val() || "";
    var temporada = temporadaRaw.substring(0, 3);
    var estilo = $("#txtEstilo").val();
    var estiloPropio = $("#txtEstiloPropio").val();
    var item = $(this).val();

    // Limpiar campos dependientes
    $("#txtUbicacion").empty().append('<option value="">Seleccione una Ubicacion</option>');
    $("#txtTecnica").empty().append('<option value="">Seleccione una Técnica</option>');

    var modoActual = getQueryParam('modo');

    if (modoActual !== 'NORMAL' && modoActual !== 'ITEM') {
        // ── MODO INVERSO: Autocompletar Ubicación y Técnica vía AJAX ──
        if (cliente && temporada && estiloPropio && item) {
            $.ajax({
                url: "/Home/ObtenerUbicacionTecnicaPorItem",
                type: "GET",
                data: {
                    cliente: cliente,
                    temporada: temporada,
                    estiloPropio: estiloPropio,
                    codItem: item
                },
                success: function (resp) {
                    if (resp.success) {
                        // Autocompletar Ubicación (deshabilitado)
                        if (resp.ubicacion && resp.ubicacion.trim() !== '') {
                            $("#txtUbicacion").empty()
                                .append(new Option(resp.ubicacion, resp.ubicacion, true, true))
                                .trigger('change.select2');
                        }
                        // Autocompletar Técnica (deshabilitado)
                        if (resp.tecnica && resp.tecnica.trim() !== '') {
                            $("#txtTecnica").empty()
                                .append(new Option(resp.tecnica, resp.tecnica, true, true))
                                .trigger('change.select2');
                        }
                    }
                },
                error: function () {
                    console.error("Error al obtener Ubicación/Técnica para el Item seleccionado.");
                }
            });
        }
    } else {
        // ── MODO CLÁSICO (NORMAL): Carga estándar con desplegables editables ──
        if (cliente && temporada && estiloPropio && item) {
            cargarTecnica(cliente, temporada, estiloPropio, item);
            cargarUbicacion(cliente, temporada, estiloPropio, item);
        }
    }
});

//19/03/26
//Cmendez
function cargarCombos(cliente, temporada, estilo) {
    $("#txtCombo").empty().append('<option value="">Seleccione un Combo</option>');
    $.ajax({
        url: "/Home/ListarCombosPorEstilo",
        type: "GET",
        data: { cliente: cliente, temporada: temporada, estilo: estilo },
        success: function (data) {
            if (data && data.length > 0) {
                $.each(data, function (i, combo) {
                    $("#txtCombo").append($('<option>', {
                        value: combo,
                        text: combo
                    }));
                });
            }
        }
    });
}

function cargarTecnica(cliente, temporada, estilo, item) {
    $("#txtTecnica").empty().append('<option value="">Seleccione una Técnica</option>');
    $.ajax({
        url: "/Home/ListarTecnicas", // Asegúrate que la ruta coincida con tu RouteConfig
        type: "GET",
        data: { cliente: cliente, temporada: temporada, estilo: estilo, item: item },
        success: function (data) {
            if (data && data.length > 0) {
                $.each(data, function (i, NombreTecnica) {
                    $("#txtTecnica").append($('<option>', {
                        value: NombreTecnica,
                        text: NombreTecnica
                    }));
                });
            }
        }
    });
}
//Fin 30/03/2026

//Inicio 30/03/2026
function cargarUbicacion(cliente, temporada, estilo, item) {
    $("#txtUbicacion").empty().append('<option value="">Seleccione una Ubicación</option>');
    $.ajax({
        url: "/Home/ListarUbicacionPendientes", // Asegúrate que la ruta coincida con tu RouteConfig
        type: "GET",
        data: { cliente: cliente, temporada: temporada, estilo: estilo, item: item },
        success: function (data) {
            if (data && data.length > 0) {
                $.each(data, function (i, ubicacion) {
                    $("#txtUbicacion").append($('<option>', {
                        value: ubicacion,
                        text: ubicacion
                    }));
                });
            }
        }
    });
}
//Fin 30/03/2026

function cargarTemporadas(cliente) {
    $.ajax({
        url: '/Home/ListarTemporadasPorCliente',
        type: 'POST',
        data: { cliente: cliente },
        success: function (response) {
            if (response.success) {
                var ddlTemporada = $("#txtTemporada");
                $.each(response.data, function (i, item) {
                    // OJO: Como tu entidad se llama RequerimientoBE, la propiedad mapeada se llama "Requerimiento"
                    ddlTemporada.append($('<option></option>').val(item).html(item));
                });
            } else {
                console.error("Error al cargar temporadas: " + response.message);
            }
        },
        error: function (error) {
            console.log("Error en AJAX (Temporadas)", error);
        }
    });
}

function guardarRecetaCompleta() {
    // Llenamos la cabecera del objeto recetaMaster

    // 2. Aseguramos que la NP esté capturada
    //Agrega NP
    recetaMaster.NP = $("#txtNP").val();
    var fechaSeleccionada = $("#dtFechaUDP").val();
    recetaMaster.Operario = $("#txtOperario").val();
    recetaMaster.Tecnica = $("#txtTecnica").val();
    recetaMaster.Cliente = $("#txtCliente").val();
    recetaMaster.Temporada = $("#txtTemporada").val();
    recetaMaster.Estilo = $("#txtEstilo").val();
    recetaMaster.EstiloPropio = $("#txtEstiloPropio").val();
    recetaMaster.Item = $("#txtItem").val();
    recetaMaster.ComboCabecera = $("#txtCombo").val();
    recetaMaster.PrendasReq = $("#txtPrendasReq").val();
    // Concepto ya no va en cabecera
    recetaMaster.Ubicacion = $("#txtUbicacion").val();
    recetaMaster.Arte = $("#txtArte").val();

    // VALIDACIÓN: Si el valor es una cadena vacía, nulo o indefinido
    if (!fechaSeleccionada || fechaSeleccionada.trim() === "") {
        Swal.fire("Aviso", "La fecha es obligatoria", "warning");
        return; // Detiene la ejecución de la función
    }
    var partes = fechaSeleccionada.split('-');
    // Formato DD/MM/YYYY
    recetaMaster.FechaUDP = partes[2] + '/' + partes[1] + '/' + partes[0];

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

    // Aqui que lee dependiendo de que esta seleccionado
    var modoQuery = getQueryParam('modo');

    // Si no hay parámetro en la URL, forzamos que se comporte como INVERSO
    if (modoQuery != 'ITEM') {
        if (recetaMaster.Estilo.length === 0) {
            Swal.fire("Aviso", "Agregue estilo", "warning");
            return;
        }
        if (recetaMaster.EstiloPropio.length === 0) {
            Swal.fire("Aviso", "Agregue estilo propio", "warning");
            return;
        }
        if (recetaMaster.ComboCabecera.length === 0) {
            Swal.fire("Aviso", "Agregue estilo propio", "warning");
            return;
        }
    }

    if (recetaMaster.Item.length === 0) {
        Swal.fire("Aviso", "Agregue el item", "warning");
        return;
    }
    // Validacion concepto removida
    if (recetaMaster.Colores.length === 0) {
        Swal.fire("Aviso", "Agregue al menos un color pantone", "warning");
        return;
    }
    if (recetaMaster.Colores[0].Insumos.length === 0) {
        Swal.fire("Aviso", "Agregue al menos un Insumo para el color", "warning");
        return;
    }

    // 3. Envío AJAX
    $.ajax({
        url: '/Home/RegistrarVisitaCompleta', // Ajusta a tu ruta
        type: 'POST',
        dataType: 'json',
        contentType: 'application/json; charset=utf-8',
        data: JSON.stringify(recetaMaster), // Convertimos el objeto de 3 niveles a texto
        beforeSend: function () {
            // Opcional: Bloquear botón para evitar doble clic
            $("#btnGuardarTodo").prop("disabled", true).text("Guardando...");
        },
        success: function (res) {
            if (res.result === "success") {
                Swal.fire("¡Logrado!", res.message, "success").then(function () {
                    location.reload(); // Recargar para nueva entrada
                });
            } else {
                Swal.fire("Error", res.message, "error");
                $("#btnGuardarTodo").prop("disabled", false).text("Guardar Todo");
            }
        },
        error: function () {
            Swal.fire("Error", "No se pudo conectar con el servidor", "error");
            $("#btnGuardarTodo").prop("disabled", false).text("Guardar Todo");
        }
    });
}

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
    // Concepto no va aquí
    Ubicacion: "",    // <-- Agregado
    Arte: "",    // <-- Agregado
    Colores: []
};

var indexColorSeleccionado = -1;

// AGREGAR NIVEL 2 (COLOR)
function agregarColor() {
    var colorNom = $("#txtNuevoColor").val();
    var comboNom = $("#txtCombo").val();

    //if (comboNom === "") {
    //    Swal.fire("Aviso", "Debe seleccionar una NP", "info");
    //    return;
    //}
    if (colorNom === "") {
        Swal.fire("Aviso", "Ingrese el nombre del color pantone", "info");
        return;
    }
    // Creamos el objeto con su lista de insumos vacía
    var nuevoColor = {
        nombre: colorNom,
        combo: comboNom,
        Insumos: []
    };

    recetaMaster.Colores.push(nuevoColor);
    renderizarTabla();

    // Limpiar campos
    $("#txtNuevoColor").val("");
}

function agregarInsumo() {
    // 1. Validar que haya un color seleccionado antes de hacer cualquier cosa
    if (indexColorSeleccionado === -1) {
        Swal.fire("Aviso", "Seleccione un color destino primero.", "info");
        return;
    }

    // 2. Capturamos el valor completo (Ej: "12345678 TELA ALGODON ROJO")
    var valorCompleto = $("#txtDescInsumo").val() || "";

    var codigoReal = "";
    var descripcionReal = "";

    // 3. Separamos el texto asegurándonos de que tenga la longitud mínima
    if (valorCompleto.length >= 8) {
        codigoReal = valorCompleto.substring(0, 8).trim();
        descripcionReal = valorCompleto.substring(8).trim();
    } else {
        Swal.fire("Atención", "Por favor seleccione un insumo válido del listado.", "warning");
        return;
    }

    // 4. Capturamos la cantidad
    var cant = parseFloat($("#txtCantInsumo").val());

    // 5. Validamos que no falte la cantidad
    if (!codigoReal || isNaN(cant) || cant <= 0) {
        Swal.fire("Aviso", "Ingrese un peso (cantidad)", "info");
        return;
    }

    // 5.5 Capturamos Unidades
    var valorStock = $("#txtStock").val() || "";
    var unid = "";

    if (valorStock.length >= 4) {
        unid = valorStock.substring(valorStock.length - 2, valorStock.length).trim();
    }
    if (unid == "KG") {
        unid = "gr";
    }

    // 6. Insertar en la estructura global
    // 🟢 NOTA: Se usan mayúsculas iniciales (CodigoInsumo) para mantener 
    // la compatibilidad con el modelo de C# y la función sincronizarPruebasExistentes
    recetaMaster.Colores[indexColorSeleccionado].Insumos.push({
        codigo: codigoReal,
        descripcion: descripcionReal,
        unidades: unid,
        cantidad: cant
    });

    // 7. Redibujar la tabla
    renderizarTabla();

    // 8. Limpiar campos
    $("#txtDescInsumo").val("").trigger('change');
    $("#txtStock").val("");
    $("#txtCantInsumo").val("");
}

// RENDERIZADO TIPO ACORDEÓN
function renderizarTabla() {
    var $tbody = $("#cuerpoTablaMaestra");
    $tbody.empty();

    recetaMaster.Colores.forEach(function (color, idx) {
        // Dibujamos los insumos como una lista interna
        var htmlInsumos = "";
        color.Insumos.forEach(function (ins, idxIns) {
            htmlInsumos += `
                            <div class="alert alert-warning" style="padding:5px; margin-bottom:2px;">
                            <small><b>${ins.codigo}</b> - ${ins.descripcion} | <b>Cant:</b> ${parseFloat(ins.cantidad).toFixed(2)} </b> ${ins.unidades}</small>
                            <button type="button" class="close" onclick="eliminarInsumo(${idx}, ${idxIns})">&times;</button>
                        </div>`;
        });
        var claseActiva = (indexColorSeleccionado === idx) ? "success" : "";
        var fila = `
            <tr class="${claseActiva}">
                <td onclick="activarColor(${idx})" style="cursor:pointer">
                    <i class="fa fa-chevron-right"></i> <strong>${color.nombre}</strong><br>
                </td>
                <td>${htmlInsumos || '<span class="text-muted">Sin insumos</span>'}</td>
                <td>
                    <button type="button" class="btn btn-danger btn-sm" onclick="eliminarColor(${idx})">
                        <i class="fa fa-trash-alt"></i>
                    </button>
                </td>
            </tr>`;
        $tbody.append(fila);
    });
}

function activarColor(idx) {
    indexColorSeleccionado = idx;
    $("#lblColorActivo").text(recetaMaster.Colores[idx].nombre);
    $("#panelInsumos").fadeIn(); // Mostramos el panel de insumos
    renderizarTabla(); // Refrescamos para resaltar la fila
}

function eliminarColor(idx) {
    recetaMaster.Colores.splice(idx, 1);
    indexColorSeleccionado = -1;
    $("#panelInsumos").hide();
    renderizarTabla();
}

function eliminarInsumo(idxColor, idxIns) {
    recetaMaster.Colores[idxColor].Insumos.splice(idxIns, 1);
    renderizarTabla();
}

//Agregar llenar combo items
function llenarComboItems(lista) {
    var $select = $("#txtListaItems");

    // 1. Limpiar opciones anteriores
    $select.empty();

    // 2. Agregar opción por defecto
    $select.append('<option value="">Seleccione una opción...</option>');

    // 3. Iterar sobre la data y agregar opciones
    // Asumiendo que 'lista' es un array de objetos [{Id: 1, Nombre: 'Item A'}, ...]
    $.each(lista, function (index, item) {
        $select.append('<option value="' + item.Id + '">' + item.Nombre + '</option>');
    });

    // 4. IMPORTANTE: Refrescar el componente bootstrap-select
    $select.selectpicker('refresh');
}
//fin

$('#btnCancelarVisita').on('click', function (e) {
    e.preventDefault();
    setTimeout(function () {
        $('#btnRegistrarTodo').attr('disabled', 'disabled');
        $('#btnLimpiar').attr('disabled', 'disabled');
        $('#btnCancelarVisita').attr('disabled', 'disabled');
    }, 200)
    /*var tempIDCliHijo = document.getElementById('Cliente').value;*/
    window.location.href = '/Home/MenuProveedor/';
});


$('#btnImprimirRFID').on('click', function (e) {
    e.preventDefault();
    setTimeout(function () {
        $('#btnGrabarVisita').attr('disabled', 'disabled');
        $('#btnCancelarVisita').attr('disabled', 'disabled');
    }, 200)
    //alert('Antes de imprimir');
    var codpid = 'Datos'; // document.getElementById('IDVisita').value;
    $.ajax({
        url: $('#URL_Imprimir').val(),
        type: 'POST',
        data: 'codpid=' + codpid,

        dataType: 'JSON',
        success: function (json) {
            //alert('Despues de imprimir');
            if (json.result == 'success') {
                setTimeout(function () {
                    console.log("ingrese5");
                    $('#btnImprimir').val('Enviando....');
                    $('#btnImprimir').attr('disabled', 'disabled');
                }, 200)
                Swal.fire({
                    icon: 'success',
                    title: json.title,
                    text: json.message
                    //footer: 'Se genero la solicitud de reprogramación correctamente.'
                }).then(function (result) {
                    if (result.value) {
                        window.location.href = '/Home/Visita';
                    }
                }
                );
            }
        }
    });


});

$('#btnCancelarVisitaSolicitudes').on('click', function (e) {
    $('#btnAprobar').attr('disabled', 'disabled');
    $('#btnAnularAprobaciones').attr('disabled', 'disabled');
    $('#btnCancelarVisitaSolicitudes').attr('disabled', 'disabled');
    e.preventDefault();
    window.location.href = '/Home/MantenimientoVisitasSolicitudes';
});

$('#btnCancelarVisitaLogistica').on('click', function (e) {
    e.preventDefault();
    $('#btnGrabarVisitaLogiReprogFecha').attr('disabled', 'disabled');
    $('#btnCancelarVisitaLogistica').attr('disabled', 'disabled');
    $('#btnElegirCitaReprogFecha').attr('disabled', 'disabled');
    window.location.href = '/Home/MantenimientoVisitasLogistica';
});

$('#btnCancelarVisitaVigilante').on('click', function (e) {
    e.preventDefault();
    window.location.href = '/Home/MantenimientoVisitasVigilante';
});

// Formatear fecha para el NP
var spanishDateFormatter = new Intl.DateTimeFormat('es-PE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
});
var now = new Date();
var formattedDate = spanishDateFormatter.format(now);




$('#btnAprobar').on('click', function (e) {
    var codpid = document.getElementById('IDVisita').value;
    $.ajax({
        url: $('#URL_RegistrarAprobar').val(),
        type: 'POST',
        data: 'codpid=' + codpid,
        dataType: 'JSON',
        success: function (json) {
            if (json.result == 'success') {
                setTimeout(function () {
                    console.log("ingrese5");
                    $('#btnAprobar').val('Enviando....');
                    $('#btnAprobar').attr('disabled', 'disabled');
                }, 200)
                Swal.fire({
                    icon: 'success',
                    title: json.title,
                    text: json.message
                    //footer: 'Se genero la solicitud de reprogramación correctamente.'
                }).then(function (result) {
                    if (result.value) {
                        window.location.href = '/Home/MantenimientoVisitasSolicitudes';
                    }
                }
                );
            }
        }
    });
});

$('#btnAnularAprobaciones').on('click', function (e) {
    var codpid = document.getElementById('IDVisita').value;
    $.ajax({
        url: $('#URL_AnularAprobaciones').val(),
        type: 'POST',
        data: 'codpid=' + codpid,
        dataType: 'JSON',
        success: function (json) {
            if (json.result == 'success') {
                setTimeout(function () {
                    console.log("ingrese5");
                    $('#btnAnularAprobaciones').val('Enviando....');
                    $('#btnAnularAprobaciones').attr('disabled', 'disabled');
                }, 200)
                Swal.fire({
                    icon: 'success',
                    title: json.title,
                    text: json.message
                    //footer: 'Se genero la solicitud de reprogramación correctamente.'
                }).then(function (result) {
                    if (result.value) {
                        window.location.href = '/Home/MantenimientoVisitasSolicitudes';
                    }
                }
                );
            }
        }
    });
});

$('#btnCancelar').on('click', function (e) {
    e.preventDefault();
    window.location.href = "/Home/MenuProveedor";
});

$('#btnBuscarVisualizar').on('click', function (e) {
    e.preventDefault();
    var tempNP = document.getElementById('txtBuscarNP').value;
    var tempSecuencia = document.getElementById('txtBuscarSec').value;
    window.location.href = '/Home/VisitaVisualizar/' + '?id=' + '0' + '&pCod_OrdPro=' + tempNP + '&pSecuencia=' + tempSecuencia;
});

$('#btnBuscarObservaciones').on('click', function (e) {
    e.preventDefault();
    var tempNP = document.getElementById('txtBuscarNP').value;
    var tempSecuencia = document.getElementById('txtBuscarSec').value;
    window.location.href = '/Home/VisualizarObservaciones/' + '?id=' + '0' + '&pCod_OrdPro=' + tempNP + '&pSecuencia=' + tempSecuencia;
});

$('#btnBuscar').on('click', function (e) {
    e.preventDefault();
    var tempNP = document.getElementById('txtBuscarNP').value;
    var tempSecuencia = document.getElementById('txtBuscarSec').value;
    /*window.location.href = '/Home/Visita/' + '?id=1&pCod_OrdPro=' + tempNP + '&pSecuencia=' + tempSecuencia;*/
    window.location.href = '/Home/Visita/' + '?id=' + '0' + '&pCod_OrdPro=' + tempNP + '&pSecuencia=' + tempSecuencia;
});

$('#btnGrabarVisita').on('click', function (e) {
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
            $('#btnGrabarVisita').attr('disabled', 'disabled');
            $('#btnCancelarVisita').attr('disabled', 'disabled');
        },
        complete: function () {
            console.log("ingrese2");
            $('#btnGrabarVisita').val('<i class="fa fa-check"></i>&nbsp;Grabar');
        },
        success: function (json) {
            console.log("ingrese3");
            $('#btnGrabarVisita').val('<i class="fa fa-check"></i>&nbsp;Grabar');
            $('#btnGrabarVisita').removeAttr('disabled');
            $('#btnCancelarVisita').removeAttr('disabled');
            if (json.result == 'warning') {
                console.log("ingrese4");
                Swal.fire({
                    icon: 'warning',
                    title: json.title,
                    text: json.message
                });
            } else if (json.result == 'success') {
                e.preventDefault();
                setTimeout(function () {
                    console.log("ingrese5");
                }, 200)
                Swal.fire({
                    icon: 'success',
                    title: json.title,
                    text: json.message,
                    footer: 'Se generó correctamente.'
                }).then(function (result) {
                    if (result.value) {
                        var tempNP = document.getElementById('txtBuscarNP').value;
                        var tempSecuencia = document.getElementById('txtBuscarSec').value;
                        window.location.href = '/Home/Visita/' + '?id=' + '0' + '&pCod_OrdPro=' + tempNP + '&pSecuencia=' + tempSecuencia;
                    } else {
                        var tempNP = document.getElementById('txtBuscarNP').value;
                        var tempSecuencia = document.getElementById('txtBuscarSec').value;
                        window.location.href = '/Home/Visita/' + '?id=' + '0' + '&pCod_OrdPro=' + tempNP + '&pSecuencia=' + tempSecuencia;
                    }
                }
                );
            } else if (json.result == 'error') {
                   $('#btnGrabarVisita').val('<i class="fa fa-check"></i>&nbsp;Grabar');
                   $('#btnGrabarVisita').removeAttr('disabled');
                   $('#btnCancelarVisita').removeAttr('disabled');
            }
        },
        error: function () {
            $('#btnGrabarVisita').val('<i class="fa fa-check"></i>&nbsp;Grabar');
            $('#btnGrabarVisita').removeAttr('disabled');
            $('#btnCancelarVisita').removeAttr('disabled');
        }
    });
});

$('#btnElegirCita').on('click', function (e) {
    e.preventDefault();
    var pExisteHub = "1"//document.getElementById('Hub').value;
    var pExistenOrdenes = "1"//document.getElementById('SumaTotalCodigos').value;
    var pExistenVisitantes = "1"//document.getElementById('SumaTotalVisitantes').value;
    //Envia los periodos, pero antes verifica que no sea mas del maximo de periodos por dia
    var pCantPeriodos = "1"//document.getElementById('CantidadPeriodos').value;
    var numPer = Number.parseInt(pCantPeriodos, 10)

    if (pExisteHub == 0) {
        Swal.fire({
            icon: 'warning',
            title: 'Dato Requerido',
            text: 'Debe seleccionar un hub.'
        });
    }
    else if (pExistenOrdenes == 0) {
        Swal.fire({
            icon: 'warning',
            title: 'Dato Requerido',
            text: 'Debe completar la lista de datos de compra.'
        });
    }
    else if (pExistenVisitantes == 0) {
        Swal.fire({
            icon: 'warning',
            title: 'Dato Requerido',
            text: 'Debe completar la lista de visitantes.'
        });
    }
    else {
        if (numPer > 17) {
            Swal.fire({
                icon: 'warning',
                title: 'Exceso de códigos por día',
                text: 'Favor de revisar la suma total de códigos por atender, ya que excede la cantidad maxima de códigos por día (85).'
            });
        }
        else {
            setTimeout(function () {
                console.log("ingrese5");
                $('#btnElegirCita').val('Enviando....');
                $('#btnElegirCita').attr('disabled', 'disabled');
            }, 200)
            window.location.href = '/Home/Citas/' + 'pCantPeriodos=' + pCantPeriodos;
        }
    }
}
);

$('#btnAnular').on('click', function (e) {
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
            $('#btnAnular').val('Enviando....');
            $('#btnAnular').attr('disabled', 'disabled');
        },
        complete: function () {
            console.log("ingrese2");
            $('#btnAnular').val('<i class="fa fa-check"></i>&nbsp;Grabar');

            $('#btnAnular').removeAttr('disabled');

        },
        success: function (json) {
            console.log("ingrese3");
            $('#btnAnular').val('<i class="fa fa-check"></i>&nbsp;Grabar');

            $('#btnAnular').removeAttr('disabled');

            if (json.result == 'warning') {
                console.log("ingrese4");
                Swal.fire({
                    icon: 'warning',
                    title: json.title,
                    text: json.message
                });
            } else if (json.result == 'success') {
                setTimeout(function () {
                    console.log("ingrese5");
                    $('#btnAnular').val('Enviando....');
                    $('#btnAnular').attr('disabled', 'disabled');
                }, 200)
                Swal.fire({
                    icon: 'success',
                    title: json.title,
                    text: json.message
                    //footer: 'Se genero la solicitud de reprogramación correctamente.'
                }).then(function (result) {
                    if (result.value) {
                        window.location.href = '/Home/MantenimientoVisitasLogistica';
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
            $('#btnAnular').val('<i class="fa fa-check"></i>&nbsp;Grabar');
            $('#btnAnular').removeAttr('disabled');
        }
    });

});

$('#btnAnularAprobaciones').on('click', function (e) {
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
            $('#btnAnularAprobaciones').val('Enviando....');
            $('#btnAnularAprobaciones').attr('disabled', 'disabled');
        },
        complete: function () {
            console.log("ingrese2");
            $('#btnAnularAprobaciones').val('<i class="fa fa-check"></i>&nbsp;Grabar');

            $('#btnAnularAprobaciones').removeAttr('disabled');

        },
        success: function (json) {
            console.log("ingrese3");
            $('#btnAnularAprobaciones').val('<i class="fa fa-check"></i>&nbsp;Grabar');

            $('#btnAnularAprobaciones').removeAttr('disabled');

            if (json.result == 'warning') {
                console.log("ingrese4");
                Swal.fire({
                    icon: 'warning',
                    title: json.title,
                    text: json.message
                });
            } else if (json.result == 'success') {
                setTimeout(function () {
                    console.log("ingrese5");
                    $('#btnAnularAprobaciones').val('Enviando....');
                    $('#btnAnularAprobaciones').attr('disabled', 'disabled');
                }, 200)
                Swal.fire({
                    icon: 'success',
                    title: json.title,
                    text: json.message
                    //footer: 'Se genero la solicitud de reprogramación correctamente.'
                }).then(function (result) {
                    if (result.value) {
                        window.location.href = '/Home/MantenimientoVisitasSolicitudes';
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
            $('#btnAnularAprobaciones').val('<i class="fa fa-check"></i>&nbsp;Grabar');
            $('#btnAnularAprobaciones').removeAttr('disabled');
        }
    });

});

$('#btnAprobar').on('click', function (e) {
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
            $('#btnAprobar').val('Enviando....');
            $('#btnAprobar').attr('disabled', 'disabled');
        },
        complete: function () {
            console.log("ingrese2");
            $('#btnAprobar').val('<i class="fa fa-check"></i>&nbsp;Grabar');

            $('#btnAprobar').removeAttr('disabled');

        },
        success: function (json) {
            console.log("ingrese3");
            $('#btnAprobar').val('<i class="fa fa-check"></i>&nbsp;Grabar');

            $('#btnAprobar').removeAttr('disabled');

            if (json.result == 'warning') {
                console.log("ingrese4");
                Swal.fire({
                    icon: 'warning',
                    title: json.title,
                    text: json.message
                });
            } else if (json.result == 'success') {
                setTimeout(function () {
                    console.log("ingrese5");
                    $('#btnAprobar').val('Enviando....');
                    $('#btnAprobar').attr('disabled', 'disabled');
                }, 200)
                Swal.fire({
                    icon: 'success',
                    title: json.title,
                    text: json.message
                    //footer: 'Se genero la solicitud de reprogramación correctamente.'
                }).then(function (result) {
                    if (result.value) {
                        window.location.href = '/Home/MantenimientoVisitasSolicitudes';
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
            $('#btnAprobar').val('<i class="fa fa-check"></i>&nbsp;Grabar');
            $('#btnAprobar').removeAttr('disabled');
        }
    });

});

$('#btnGrabarVisitaLogiReprogFecha').on('click', function (e) {
    console.log("ingrese");
    $('#btnGrabarVisitaLogiReprogFecha').attr('disabled', 'disabled');
    $('#btnCancelarVisitaLogistica').attr('disabled', 'disabled');
    $('#btnElegirCitaReprogFecha').attr('disabled', 'disabled');
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
            $('#btnGrabarVisitaLogiReprogFecha').val('Enviando....');
            $('#btnGrabarVisitaLogiReprogFecha').attr('disabled', 'disabled');
        },
        complete: function () {
            console.log("ingrese2");
            $('#btnGrabarVisitaLogiReprogFecha').val('<i class="fa fa-check"></i>&nbsp;Grabar');

            $('#btnGrabarVisitaLogiReprogFecha').removeAttr('disabled');

        },
        success: function (json) {
            console.log("ingrese3");
            $('#btnGrabarVisitaLogiReprogFecha').val('<i class="fa fa-check"></i>&nbsp;Grabar');

            $('#btnGrabarVisitaLogiReprogFecha').removeAttr('disabled');

            if (json.result == 'warning') {
                console.log("ingrese4");
                $('#btnGrabarVisitaLogiReprogFecha').removeAttr('disabled');
                $('#btnCancelarVisitaLogistica').removeAttr('disabled');
                $('#btnElegirCitaReprogFecha').removeAttr('disabled');
                Swal.fire({
                    icon: 'warning',
                    title: json.title,
                    text: json.message
                });
            } else if (json.result == 'success') {
                setTimeout(function () {
                    console.log("ingrese5");
                    $('#btnGrabarVisitaLogiReprogFecha').attr('disabled', 'disabled');
                    $('#btnCancelarVisitaLogistica').attr('disabled', 'disabled');
                    $('#btnElegirCitaReprogFecha').attr('disabled', 'disabled');
                }, 200)
                Swal.fire({
                    icon: 'success',
                    title: json.title,
                    text: json.message
                    //footer: 'Se genero la solicitud de reprogramación correctamente.'
                }).then(function (result) {
                    if (result.value) {
                        window.location.href = '/Home/MantenimientoVisitasLogistica';
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
            $('#btnGrabarVisitaLogiReprogFecha').val('<i class="fa fa-check"></i>&nbsp;Grabar');
            $('#btnGrabarVisitaLogiReprogFecha').removeAttr('disabled');
        }
    });

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
                setTimeout(function () {
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
function base64ToBlob(base64, type) {
    if (!type) type = "application/octet-stream";
    var binStr = atob(base64);
    var len = binStr.length;
    var arr = new Uint8Array(len);
    for (var i = 0; i < len; i++) {
        arr[i] = binStr.charCodeAt(i);
    }
    return new Blob([arr], { type: type });
}

function guardarArchivo(blob, filename) {
    if (window.navigator.msSaveOrOpenBlob) {
        window.navigator.msSaveOrOpenBlob(blob, filename);
    } else {
        var a = document.createElement('a');
        document.body.appendChild(a);
        var url = window.URL.createObjectURL(blob);
        a.href = url;
        a.download = filename;
        a.click();
        setTimeout(function () {
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);
        }, 0)
    }
}



