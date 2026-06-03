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

    $('#btnExportarMatrizPdf').on('click', function() {
        var tableElement = document.getElementById('tblMatrizCruzada');
        var $tableContainer = $(tableElement).closest('.table-responsive');
        var $outerContainer = $(tableElement).closest('.table-container');
        var $matrizTab = $('#matriz');
        
        // 1. Guardar posiciones de scroll
        var scrollPos = {
            innerX: $tableContainer.scrollLeft(),
            innerY: $tableContainer.scrollTop(),
            outerX: $outerContainer.scrollLeft(),
            windowX: window.scrollX,
            windowY: window.scrollY
        };
        
        // Forzar todo el scroll a 0. Esto arregla el bug donde html2canvas recorta la parte izquierda si el usuario había scrolleado
        $tableContainer.scrollLeft(0).scrollTop(0);
        $outerContainer.scrollLeft(0).scrollTop(0);
        window.scrollTo(0, 0);

        // 2. Expandir contenedores para revelar toda el área de dibujo
        var oldCSS = {
            innerMaxH: $tableContainer.css('max-height'),
            innerOverflow: $tableContainer.css('overflow'),
            outerOverflow: $outerContainer.css('overflow'),
            tabOverflow: $matrizTab.css('overflow')
        };
        
        $tableContainer.css({ 'max-height': 'none', 'overflow': 'visible' });
        $outerContainer.css({ 'overflow': 'visible' });
        $matrizTab.css({ 'overflow': 'visible' });
        
        // 3. Remover el comportamiento sticky temporalmente en la tabla
        var originalStyles = [];
        $(tableElement).find('thead, th, td').each(function() {
            if ($(this).css('position') === 'sticky') {
                originalStyles.push({
                    el: this,
                    position: $(this).css('position'),
                    left: $(this).css('left'),
                    zIndex: $(this).css('z-index')
                });
                $(this).css({
                    'position': 'static',
                    'left': 'auto',
                    'z-index': 'auto',
                    'background-color': '#fff' // Asegurar fondo blanco
                });
            }
        });
        
        var opt = {
            margin:       0.5,
            filename:     'Matriz_Consumos.pdf',
            image:        { type: 'jpeg', quality: 0.98 },
            html2canvas:  { scale: 2, useCORS: true, scrollX: 0, scrollY: 0 },
            jsPDF:        { unit: 'in', format: 'letter', orientation: 'landscape' }
        };

        Swal.fire({ title: 'Generando PDF...', didOpen: () => { Swal.showLoading(); }});

        html2pdf().set(opt).from(tableElement).save().then(function() {
            Swal.close();
            
            // 4. Restaurar TODO (Estilos CSS y Posiciones de Scroll)
            $tableContainer.css({ 'max-height': oldCSS.innerMaxH, 'overflow': oldCSS.innerOverflow });
            $outerContainer.css({ 'overflow': oldCSS.outerOverflow });
            $matrizTab.css({ 'overflow': oldCSS.tabOverflow });
            
            $tableContainer.scrollLeft(scrollPos.innerX).scrollTop(scrollPos.innerY);
            $outerContainer.scrollLeft(scrollPos.outerX);
            window.scrollTo(scrollPos.windowX, scrollPos.windowY);

            originalStyles.forEach(function(item) {
                $(item.el).css({
                    'position': item.position,
                    'left': item.left,
                    'z-index': item.zIndex
                });
            });
        });
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

        var thead = $('#tblMatrizCruzada thead');
        thead.empty();

        // Determinar columnas dinámicas
        var fixedCols = ['Código', 'Insumo', 'Stock Real'];
        var dynamicCols = columns.filter(function(c) { return fixedCols.indexOf(c) === -1; });

        // Agrupar columnas dinámicas
        var npGroups = {};
        dynamicCols.forEach(function(col) {
            var parts = col.split('|');
            var fecha = parts.length > 0 ? parts[0].trim() : "";
            var cliente = parts.length > 1 ? parts[1].trim() : "";
            var estilo = parts.length > 2 ? parts[2].trim() : "";
            var np = parts.length > 3 ? parts[3].trim() : "";
            var color = parts.length > 4 ? parts[4].trim() : "SIN COLOR";

            if (!npGroups[np]) {
                npGroups[np] = { Fecha: fecha, Cliente: cliente, Estilo: estilo, Np: np, Colores: [], DynamicColumns: [] };
            }
            npGroups[np].Colores.push(color);
            npGroups[np].DynamicColumns.push(col);
        });

        // Generar 4 niveles de cabecera
        var trCliente = $('<tr></tr>');
        var trEstilo = $('<tr></tr>');
        var trNp = $('<tr></tr>');
        var trColor = $('<tr></tr>');

        // Columnas fijas (con rowspan=4)
        trCliente.append('<th rowspan="4" class="text-center align-middle" style="position: sticky; left: 0; top: 0; z-index: 5; background-color: #f4f8fa; width: 120px; border-bottom: 2px solid #dee2e6;">Código</th>');
        trCliente.append('<th rowspan="4" class="text-center align-middle" style="position: sticky; left: 120px; top: 0; z-index: 5; background-color: #f4f8fa; width: 250px; border-bottom: 2px solid #dee2e6;">Insumo</th>');
        trCliente.append('<th rowspan="4" class="text-center align-middle" style="position: sticky; left: 370px; top: 0; z-index: 5; background-color: #f4f8fa; width: 100px; border-bottom: 2px solid #dee2e6;">Stock Real</th>');

        Object.keys(npGroups).forEach(function(npKey) {
            var group = npGroups[npKey];
            var span = group.Colores.length + 1; // +1 por la columna CONSUMO

            trCliente.append('<th colspan="' + span + '" class="text-center" style="background-color: #e3f2fd; border-bottom: 1px solid #dee2e6; color: #002D72;">' + group.Cliente + '</th>');
            trEstilo.append('<th colspan="' + span + '" class="text-center" style="background-color: #f8f9fa; border-bottom: 1px solid #dee2e6; color: #495057;">' + group.Estilo + '</th>');
            trNp.append('<th colspan="' + span + '" class="text-center" style="background-color: #e8f5e9; border-bottom: 1px solid #dee2e6; color: #27ae60;">' + group.Np + '</th>');

            trColor.append('<th class="text-center" style="background-color: #e9ecef; color: red; font-weight: bold; border-bottom: 2px solid #dee2e6;">CONSUMO</th>');
            group.Colores.forEach(function(c) {
                trColor.append('<th class="text-center" style="background-color: #f4f8fa; border-bottom: 2px solid #dee2e6;">' + c + '</th>');
            });
        });

        thead.append(trCliente);
        thead.append(trEstilo);
        thead.append(trNp);
        thead.append(trColor);

        var tbody = $('#tbMatrizBody');
        tbody.empty();

        if (data.length === 0) {
            var totalCols = fixedCols.length;
            Object.keys(npGroups).forEach(function(npKey) { totalCols += npGroups[npKey].Colores.length + 1; });
            tbody.append('<tr><td colspan="' + totalCols + '" class="text-center">No hay registros para mostrar.</td></tr>');
            return;
        }

        data.forEach(function(row) {
            var tr = $('<tr></tr>');
            
            // Celdas Fijas
            tr.append('<td class="text-center align-middle" style="position: sticky; left: 0; z-index: 2; background-color: #fff;"><div style="font-weight: 800; font-size: 1.2rem;" class="text-primary">' + (row['Código'] || '') + '</div></td>');
            tr.append('<td class="align-middle" style="position: sticky; left: 120px; z-index: 2; background-color: #fff; font-size: 1.2rem;">' + (row['Insumo'] || '') + '</td>');
            
            var stockReal = row['Stock Real'] !== null && row['Stock Real'] !== undefined ? parseFloat(row['Stock Real']) : 0;
            tr.append('<td class="text-right align-middle" style="position: sticky; left: 370px; z-index: 2; background-color: #fff; text-align: right !important;"><div style="font-weight: 800; font-size: 1.2rem; color: #2e7d32;">' + stockReal.toFixed(2) + '</div></td>');

            // Celdas Dinámicas (agrupadas por NP)
            Object.keys(npGroups).forEach(function(npKey) {
                var group = npGroups[npKey];
                
                // 1. Columna Sumatoria
                var sumaConsumo = 0;
                group.DynamicColumns.forEach(function(col) {
                    var val = row[col] !== null && row[col] !== undefined ? parseFloat(row[col]) : 0;
                    sumaConsumo += val;
                });
                
                var displaySuma = sumaConsumo > 0 ? sumaConsumo.toFixed(2) : '-';
                tr.append('<td class="text-right align-middle" style="background-color: #f8f9fa; font-weight: 700; color: #d32f2f; text-align: right !important;">' + displaySuma + '</td>');

                // 2. Columnas de Colores Individuales
                group.DynamicColumns.forEach(function(col) {
                    var val = row[col] !== null && row[col] !== undefined ? parseFloat(row[col]) : 0;
                    var displayVal = val > 0 ? val.toFixed(2) : '-';
                    tr.append('<td class="text-right align-middle text-muted" style="text-align: right !important;">' + displayVal + '</td>');
                });
            });

            tbody.append(tr);
        });
    }
});
