$(document).ready(function () {
    var selectedInsumoDescripcion = "";

    // Asegurar que al abrir el modal se limpie el formulario
    $('#modalCargaInicial').on('show.bs.modal', function () {
        $('#formCargaInicial')[0].reset();
        $('#txtDescInsumo').val(null).trigger('change');
        $('#txtCodInsumo').val('');
        $('#txtUM').val('');
        $('#txtCantInsumo').val('');
        $('#lblEquivalencia').text('');
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
        $('#txtCantInsumo').trigger('input');
    });

    // Calcular equivalencia en tiempo real
    $('#txtCantInsumo').on('input', function() {
        var UM = ($('#txtUM').val() || '').toUpperCase();
        var val = parseFloat($(this).val());
        if (!isNaN(val) && val > 0) {
            if (UM === 'KG') {
                $('#lblEquivalencia').html('<i class="fas fa-exchange-alt"></i> Equivale a: ' + (val / 1000).toFixed(3) + ' KG');
            } else if (UM !== '') {
                $('#lblEquivalencia').html('<i class="fas fa-exchange-alt"></i> Equivale a: ' + val.toFixed(2) + ' ' + UM);
            }
        } else {
            $('#lblEquivalencia').text('');
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

        // Lógica de conversión: Si el usuario ingresa gramos, pero la U.M. base es KG, convertimos a KG
        var UM_Upper = UM.toUpperCase();
        var pesoFinal = pesoGramos;
        if (UM_Upper === 'KG') {
            pesoFinal = pesoGramos / 1000.0;
        }

        // Enviar la petición POST
        $.ajax({
            url: '/Liquidacion/RegistrarCargaInicial',
            type: 'POST',
            data: {
                codInsumo: codInsumo,
                descripcion: selectedInsumoDescripcion,
                unidadMedida: UM,
                pesoGramos: pesoFinal
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
        var JsPdfClass = window.jspdf ? window.jspdf.jsPDF : window.jsPDF;
        if (!JsPdfClass) {
            Swal.fire('Error', 'Librería jsPDF no disponible.', 'error');
            return;
        }

        Swal.fire({ 
            title: 'Generando PDF...', 
            text: 'Procesando vectorialmente la matriz cruzada...', 
            didOpen: () => { Swal.showLoading(); }
        });

        // Dar un respiro a la UI antes de procesar
        setTimeout(function() {
            try {
                // Crear documento A4 en horizontal
                var pdf = new JsPdfClass('l', 'pt', 'a4');
                
                // jspdf-autotable toma la tabla HTML y la dibuja vectorialmente
                // Usando horizontalPageBreak: true logra exactamente lo que el usuario pidió
                pdf.autoTable({
                    html: tableElement,
                    useCss: true, // Intenta mantener colores de fondo
                    theme: 'grid',
                    styles: {
                        fontSize: 6, // Letra pequeña para que quepan más columnas
                        cellPadding: 3,
                        valign: 'middle',
                        halign: 'center',
                        lineColor: [200, 200, 200],
                        lineWidth: 0.5
                    },
                    headStyles: {
                        fillColor: [240, 248, 255],
                        textColor: [0, 45, 114],
                        fontStyle: 'bold',
                        lineWidth: 0.5
                    },
                    horizontalPageBreak: true,
                    horizontalPageBreakRepeat: 0, // Si ponemos 0, no repite ninguna columna. Si ponemos 1, repite la primera (Código).
                    margin: { top: 30, right: 20, bottom: 30, left: 20 },
                    didParseCell: function(data) {
                        // Limpieza de textos o colores específicos si se requiere
                        var bg = data.cell.styles.fillColor;
                        if (bg && bg === 'transparent') {
                            data.cell.styles.fillColor = [255, 255, 255];
                        }
                        
                        // Eliminar bordes de las celdas vacías en la cabecera (esquinas superiores izquierdas)
                        if (data.section === 'head' && data.cell.text.join('').trim() === '') {
                            data.cell.styles.lineWidth = 0;
                        }
                    }
                });

                pdf.save('Matriz_Consumos.pdf');
                Swal.close();
            } catch (err) {
                console.error("Error en autoTable:", err);
                Swal.fire('Error', 'Fallo al exportar matriz vectorialmente. ' + err.message, 'error');
            }
        }, 100);
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

        // Generar 5 niveles de cabecera
        var trFecha = $('<tr></tr>');
        var trCliente = $('<tr></tr>');
        var trEstilo = $('<tr></tr>');
        var trNp = $('<tr></tr>');
        var trColor = $('<tr></tr>');

        // Espacio en blanco (filas 1 a 4)
        trFecha.append('<th rowspan="4" style="position: sticky; left: 0; top: 0; z-index: 6; background-color: #ffffff; min-width: 100px; max-width: 100px; border: none !important;"></th>');
        trFecha.append('<th rowspan="4" style="position: sticky; left: 100px; top: 0; z-index: 6; background-color: #ffffff; min-width: 250px; max-width: 250px; border: none !important;"></th>');
        trFecha.append('<th rowspan="4" style="position: sticky; left: 350px; top: 0; z-index: 6; background-color: #ffffff; min-width: 110px; max-width: 110px; border: none !important; border-right: 1px solid #ced4da !important;"></th>');

        Object.keys(npGroups).forEach(function(npKey) {
            var group = npGroups[npKey];
            var span = group.Colores.length + 1; // +1 por la columna CONSUMO

            trFecha.append('<th colspan="' + span + '" class="text-center" style="background-color: #fff3e0; color: #e65100;">' + group.Fecha + '</th>');
            trCliente.append('<th colspan="' + span + '" class="text-center" style="background-color: #e3f2fd; color: #002D72;">' + group.Cliente + '</th>');
            trEstilo.append('<th colspan="' + span + '" class="text-center" style="background-color: #f8f9fa; color: #495057;">' + group.Estilo + '</th>');
            trNp.append('<th colspan="' + span + '" class="text-center" style="background-color: #e8f5e9; color: #27ae60;">' + group.Np + '</th>');

            trColor.append('<th class="text-center" style="background-color: #e9ecef; color: red; font-weight: bold;">CONSUMO</th>');
            group.Colores.forEach(function(c) {
                trColor.append('<th class="text-center" style="background-color: #f4f8fa;">' + c + '</th>');
            });
        });

        // Fila 5: Cabeceras reales de las columnas fijas
        trColor.prepend('<th class="text-center align-middle" style="position: sticky; left: 350px; top: 0; z-index: 6; background-color: #f4f8fa; min-width: 110px; max-width: 110px; border-top: 1px solid #ced4da !important; color: var(--hialpesa-blue); font-weight: bold; text-transform: uppercase;">Stock Real</th>');
        trColor.prepend('<th class="text-center align-middle" style="position: sticky; left: 100px; top: 0; z-index: 6; background-color: #f4f8fa; min-width: 250px; max-width: 250px; border-top: 1px solid #ced4da !important; color: var(--hialpesa-blue); font-weight: bold; text-transform: uppercase;">Insumo</th>');
        trColor.prepend('<th class="text-center align-middle" style="position: sticky; left: 0; top: 0; z-index: 6; background-color: #f4f8fa; min-width: 100px; max-width: 100px; border-top: 1px solid #ced4da !important; color: var(--hialpesa-blue); font-weight: bold; text-transform: uppercase;">Código</th>');

        thead.append(trFecha);
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
            
            var codigoInsumo = row['Código'] || '';
            var isMerma = codigoInsumo.startsWith('MER-');
            var desc = row['Insumo'] || '';
            if (isMerma) {
                desc += ' <span class="badge" style="background-color: #8e44ad; color: white; font-size: 0.8rem; margin-left: 8px; vertical-align: middle;"><i class="fas fa-recycle" style="margin-right:3px;"></i>MERMA</span>';
            }

            // Celdas Fijas
            tr.append('<td class="text-center align-middle" style="position: sticky; left: 0; z-index: 2; background-color: #fff; min-width: 100px; max-width: 100px;"><div style="font-weight: 800; font-size: 1.1rem;" class="text-primary">' + codigoInsumo + '</div></td>');
            tr.append('<td class="align-middle" style="position: sticky; left: 100px; z-index: 2; background-color: #fff; font-size: 1.1rem; min-width: 250px; max-width: 250px; white-space: normal; word-wrap: break-word;">' + desc + '</td>');
            
            var stockReal = row['Stock Real'] !== null && row['Stock Real'] !== undefined ? parseFloat(row['Stock Real']) : 0;
            tr.append('<td class="text-right align-middle" style="position: sticky; left: 350px; z-index: 2; background-color: #fff; text-align: right !important; min-width: 110px; max-width: 110px;"><div style="font-weight: 800; font-size: 1.1rem; color: #2e7d32;">' + stockReal.toFixed(2) + '</div></td>');

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
