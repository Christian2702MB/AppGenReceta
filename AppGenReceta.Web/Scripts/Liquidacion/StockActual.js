$(document).ready(function () {
    var selectedInsumoDescripcion = "";

    // Asegurar que al abrir el modal se limpie el formulario
    $('#modalCargaInicial').on('show.bs.modal', function () {
        $('#formCargaInicial')[0].reset();
        $('#txtDescInsumo').val(null).trigger('change');
        $('#txtCodInsumo').val('');
        $('#txtUM').val('');
        selectedInsumoDescripcion = "";
    });

    // Inicializar Select2 en el buscador de insumos
    $('#txtDescInsumo').select2({
        placeholder: 'Seleccione o busque un insumo...',
        dropdownParent: $('#modalCargaInicial'), // Asegura que el dropdown renderice sobre el modal
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
            inputTooShort: function () {
                return "Por favor ingrese 3 o más caracteres...";
            },
            noResults: function () {
                return "No se encontraron insumos";
            },
            searching: function () {
                return "Buscando...";
            }
        }
    });

    // Evento al seleccionar un insumo
    $('#txtDescInsumo').on('change', function () {
        var dato = $(this).val();
        if (dato) {
            $.get("/Home/ObtenerCodigoInsumo", { dato: dato }, function (res) {
                if (res) {
                    $('#txtCodInsumo').val(res.CodigoInsumo);
                    $('#txtUM').val(res.Unid_Med || 'KG');
                    selectedInsumoDescripcion = res.Descripcion;
                }
            });
        } else {
            $('#txtCodInsumo').val('');
            $('#txtUM').val('');
            selectedInsumoDescripcion = "";
        }
    });

    // Presionar Enter en el input de peso
    $('#txtCantInsumo').on('keydown', function (e) {
        if (e.key === 'Enter' || e.keyCode === 13) {
            e.preventDefault();
            $('#btnGuardarCarga').click();
        }
    });

    // Guardar Carga Inicial
    $('#btnGuardarCarga').on('click', function () {
        var codInsumo = $('#txtCodInsumo').val();
        var UM = $('#txtUM').val();
        var pesoGramosStr = $('#txtCantInsumo').val();

        if (!codInsumo) {
            Swal.fire({
                icon: 'warning',
                title: 'Atención',
                text: 'Debe buscar y seleccionar un insumo válido.',
                confirmButtonColor: '#002D72'
            });
            return;
        }

        var pesoGramos = parseFloat(pesoGramosStr);
        if (isNaN(pesoGramos) || pesoGramos <= 0) {
            Swal.fire({
                icon: 'warning',
                title: 'Atención',
                text: 'Debe ingresar un peso en gramos válido y mayor a cero.',
                confirmButtonColor: '#002D72'
            });
            return;
        }

        // Mostrar indicador de carga
        Swal.fire({
            title: 'Procesando...',
            text: 'Registrando la carga inicial en el inventario.',
            allowOutsideClick: false,
            didOpen: () => {
                Swal.showLoading();
            }
        });

        // Enviar la petición POST
        $.ajax({
            url: '/Liquidacion/RegistrarCargaInicial',
            type: 'POST',
            data: {
                codInsumo: codInsumo,
                descripcion: selectedInsumoDescripcion,
                unidadMedida: UM,
                pesoGramos: pesoGramos
            },
            success: function (response) {
                Swal.close();
                if (response.success) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Éxito',
                        text: response.message || 'Carga inicial registrada con éxito.',
                        timer: 2000,
                        showConfirmButton: false
                    }).then(function () {
                        $('#modalCargaInicial').modal('hide');
                        // Recargar la página o la tabla para mostrar los datos nuevos
                        location.reload();
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error',
                        text: response.message || 'Ocurrió un error al procesar la solicitud.',
                        confirmButtonColor: '#002D72'
                    });
                }
            },
            error: function (xhr, status, error) {
                Swal.close();
                Swal.fire({
                    icon: 'error',
                    title: 'Error de Red o Servidor',
                    text: 'No se pudo procesar la carga inicial. Verifique su conexión o intente nuevamente.',
                    confirmButtonColor: '#002D72'
                });
            }
        });
    });

    // ==========================================
    // LÓGICA DE LA MATRIZ CRUZADA
    // ==========================================
    var matrizCargada = false;

    $('a[href="#matriz"]').on('shown.bs.tab', function (e) {
        if (!matrizCargada) {
            cargarMatrizCruzada();
        }
    });

    $('#btnRecargarMatriz').on('click', function() {
        cargarMatrizCruzada();
    });

    function cargarMatrizCruzada() {
        $('#tbMatrizBody').html('<tr><td colspan="100%" class="text-center py-4"><i class="fas fa-spinner fa-spin fa-2x text-primary"></i><br>Cargando datos...</td></tr>');
        
        $.ajax({
            url: '/Liquidacion/ObtenerMatrizCruzada',
            type: 'GET',
            success: function (response) {
                if (response.success && response.data) {
                    renderizarMatriz(response.data, response.columns);
                    matrizCargada = true;
                } else {
                    $('#tbMatrizBody').html('<tr><td colspan="100%" class="text-center text-danger">Error al cargar la matriz: ' + (response.message || 'Desconocido') + '</td></tr>');
                }
            },
            error: function () {
                $('#tbMatrizBody').html('<tr><td colspan="100%" class="text-center text-danger">Error de conexión al cargar la matriz.</td></tr>');
            }
        });
    }

    function renderizarMatriz(data, columns) {
        if (!columns || columns.length === 0) {
            $('#tbMatrizBody').html('<tr><td colspan="100%" class="text-center">No hay datos de consumo disponibles para la matriz.</td></tr>');
            return;
        }

        var thead = $('#trMatrizHeaders');
        thead.empty();

        // Renderizar Cabeceras Fijas
        thead.append('<th style="position: sticky; left: 0; z-index: 4; background-color: #f4f8fa; width: 120px;">Código</th>');
        thead.append('<th style="position: sticky; left: 120px; z-index: 4; background-color: #f4f8fa; width: 250px;">Insumo</th>');
        thead.append('<th style="position: sticky; left: 370px; z-index: 4; background-color: #f4f8fa; width: 100px;" class="text-end">Stock Real</th>');

        // Determinar columnas dinámicas
        var fixedCols = ['Código', 'Insumo', 'Stock Real'];
        var dynamicCols = columns.filter(function(c) { return fixedCols.indexOf(c) === -1; });

        // Renderizar Cabeceras Dinámicas (NPs)
        dynamicCols.forEach(function(col) {
            thead.append('<th class="text-end" style="background-color: #f4f8fa;">' + col + '</th>');
        });

        var tbody = $('#tbMatrizBody');
        tbody.empty();

        if (data.length === 0) {
            tbody.append('<tr><td colspan="' + (fixedCols.length + dynamicCols.length) + '" class="text-center">No hay registros para mostrar.</td></tr>');
            return;
        }

        data.forEach(function(row) {
            var tr = $('<tr></tr>');
            
            // Celdas Fijas
            tr.append('<td class="text-center align-middle" style="position: sticky; left: 0; z-index: 2; background-color: #fff;"><div style="font-weight: 800; font-size: 1.2rem;" class="text-primary">' + (row['Código'] || '') + '</div></td>');
            tr.append('<td class="align-middle" style="position: sticky; left: 120px; z-index: 2; background-color: #fff; font-size: 1.2rem;">' + (row['Insumo'] || '') + '</td>');
            
            var stockReal = row['Stock Real'] !== null && row['Stock Real'] !== undefined ? parseFloat(row['Stock Real']) : 0;
            tr.append('<td class="text-end align-middle" style="position: sticky; left: 370px; z-index: 2; background-color: #fff;"><div style="font-weight: 800; font-size: 1.2rem; color: #2e7d32;">' + stockReal.toFixed(2) + '</div></td>');

            // Celdas Dinámicas
            dynamicCols.forEach(function(col) {
                var val = row[col] !== null && row[col] !== undefined ? parseFloat(row[col]) : 0;
                var displayVal = val > 0 ? val.toFixed(2) : '-';
                tr.append('<td class="text-end text-muted">' + displayVal + '</td>');
            });

            tbody.append(tr);
        });
    }
});
