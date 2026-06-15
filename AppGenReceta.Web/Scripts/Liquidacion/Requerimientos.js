$(document).ready(function () {
    // Inicializar Fechas por defecto (hace un mes y hoy)
    let hoy = new Date();
    let unMesAntes = new Date();
    unMesAntes.setMonth(hoy.getMonth() - 1);

    if (!$('#txtDesde').val()) {
        $('#txtDesde').val(unMesAntes.toISOString().split('T')[0]);
    }
    if (!$('#txtHasta').val()) {
        $('#txtHasta').val(hoy.toISOString().split('T')[0]);
    }

    $('input[name="optBusqueda"]').change(function () {
        manejarEstadoFiltros();
    });
    manejarEstadoFiltros();

    // Ver si la URL tiene el parámetro np
    let urlParams = new URLSearchParams(window.location.search);
    let urlNP = urlParams.get('np');
    if (urlNP) {
        $('#optNP').prop('checked', true);
        manejarEstadoFiltros();
        $('#txtNP').val(urlNP);
    }

    cargarTabla();

    $('#btnBuscar').click(function () {
        cargarTabla();
    });

    $('#btnConfirmarRecepcion').click(function () {
        confirmarRecepcionFinal();
    });
});

function cargarTabla() {
    if ($.fn.dataTable && $.fn.dataTable.isDataTable('#tblRequerimientos')) {
        $('#tblRequerimientos').DataTable().destroy();
    }

    let opcion = $('input[name="optBusqueda"]:checked').val();
    let desde = $('#txtDesde').val();
    let hasta = $('#txtHasta').val();
    let np = $('#txtNP').val();
    let numReq = $('#txtNumReq').val();

    $('#tblRequerimientos').DataTable({
        "language": {
            "url": "//cdn.datatables.net/plug-ins/1.10.24/i18n/Spanish.json"
        },
        "order": [[0, "desc"]],
        "pageLength": 25,
        "ajax": {
            "url": "/Liquidacion/ListarRequerimientosAJAX",
            "type": "POST",
            "data": { opcion: opcion, desde: desde, hasta: hasta, np: np, numReq: numReq },
            "dataSrc": function (json) {
                if (!json.success) {
                    Swal.fire('Error', json.message, 'error');
                    return [];
                }
                return json.data;
            }
        },
        "columns": [
            { "data": "NumRequerimiento", "className": "text-center font-weight-bold text-primary" },
            { "data": "FecCreacion", "className": "text-center" },
            { "data": "Partida", "className": "text-center" },
            { "data": "Observaciones" },
            {
                "data": null,
                "className": "text-center",
                "render": function (data, type, row) {
                    return `<button class="btn btn-warning btn-sm text-white" onclick="abrirVistaPrevia(${row.NumRequerimiento}, '${row.CodOrdPro}', '${row.Motivo}', '${row.Observaciones}')">
                                <i class="fas fa-clipboard-list mr-1"></i> Revisar y Recibir
                            </button>`;
                }
            }
        ]
    });
}

function abrirVistaPrevia(numReq, codOP, motivo, observaciones) {
    $('#hdnNumReq').val(numReq);
    $('#hdnCodOrdPro').val(codOP);
    $('#hdnMotivo').val(motivo);

    $('#spanNumReq').text(numReq);
    $('#spanOp').text(codOP || 'S/N');
    $('#spanObs').text(observaciones || 'Ninguna');

    $('#tblDetalleReq tbody').empty();

    Swal.fire({ title: 'Cargando detalle...', didOpen: () => { Swal.showLoading(); } });

    $.ajax({
        url: '/Liquidacion/ObtenerVistaPreviaReq',
        type: 'POST',
        data: { numReq: numReq, codOrdPro: codOP },
        success: function (res) {
            if (res.success) {
                Swal.close();
                let tbody = $('#tblDetalleReq tbody');
                res.data.forEach(item => {
                    tbody.append(`
                        <tr>
                            <td>${item.CodItem}</td>
                            <td>${item.Nombre}</td>
                            <td class="text-right font-weight-bold">${item.ConsumoRequerido.toFixed(4)}</td>
                            <td class="text-center">${item.Unidad}</td>
                            <td class="text-center">${item.Lote || ''}</td>
                        </tr>
                    `);
                });

                if (res.versiones && res.versiones.length > 1) {
                    $('#divSelectorVersion').show();
                    let cbo = $('#cboDestinoVersion');
                    cbo.empty();
                    res.versiones.forEach(v => {
                        cbo.append(`<option value="${v}">${v}</option>`);
                    });
                    // The backend sorted them putting active/in process first, so we just select the first one.
                    cbo.prop('selectedIndex', 0);
                } else {
                    $('#divSelectorVersion').hide();
                    $('#cboDestinoVersion').empty();
                }

                $('#modalVistaPrevia').modal('show');
            } else {
                Swal.fire('Error', res.message || 'Ocurrió un error al cargar el detalle.', 'error');
            }
        },
        error: function () {
            Swal.fire('Error', 'Error de conexión con el servidor.', 'error');
        }
    });
}

function confirmarRecepcionFinal() {
    let numReq = $('#hdnNumReq').val();
    let codOP = $('#hdnCodOrdPro').val();
    let motivo = $('#hdnMotivo').val();

    Swal.fire({
        title: '¿Confirmar y recibir requerimiento?',
        text: `Se procesará el requerimiento N° ${numReq} y se actualizará el stock de insumos.`,
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#28a745',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Sí, confirmar',
        cancelButtonText: 'Cancelar'
    }).then((result) => {
        if (result.isConfirmed) {
            Swal.fire({
                title: 'Procesando...',
                text: 'Por favor, espere.',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            let finalCodOP = codOP;
            if ($('#divSelectorVersion').is(':visible')) {
                finalCodOP = $('#cboDestinoVersion').val();
            }

            $.ajax({
                url: '/Liquidacion/ConfirmarRecepcion',
                type: 'POST',
                data: {
                    numRequerimiento: numReq,
                    codOrdPro: finalCodOP,
                    motivo: motivo
                },
                success: function (res) {
                    if (res.success) {
                        $('#modalVistaPrevia').modal('hide');
                        Swal.fire(
                            '¡Éxito!',
                            'Requerimiento procesado y stock actualizado.',
                            'success'
                        ).then(() => {
                            // Redirigir al historial
                            window.location.href = '/Liquidacion/MantenimientoProcesoProductivo';
                        });
                    } else {
                        Swal.fire(
                            'Error',
                            res.message || 'Ocurrió un error al procesar el requerimiento.',
                            'error'
                        );
                    }
                },
                error: function () {
                    Swal.fire('Error', 'Error de conexión con el servidor.', 'error');
                }
            });
        }
    });
}

function manejarEstadoFiltros() {
    let opt = $('input[name="optBusqueda"]:checked').val();
    
    // Ocultar todos los divs
    $('#divDesde, #divHasta, #divNumReq, #divNP').hide();
    
    // Limpiar los campos ocultos y habilitar el mostrado
    if (opt == "1") {
        $('#divDesde, #divHasta').show();
        $('#txtNP').val('');
        $('#txtNumReq').val('');
    } else if (opt == "2") {
        $('#divNP').show();
        $('#txtNumReq').val('');
    } else if (opt == "3") {
        $('#divNumReq').show();
        $('#txtNP').val('');
    }
}
