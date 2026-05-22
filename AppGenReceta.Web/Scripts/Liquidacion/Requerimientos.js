$(document).ready(function () {
    if ($.fn.dataTable && $.fn.dataTable.isDataTable('#tblRequerimientos')) {
        $('#tblRequerimientos').DataTable().destroy();
    }

    $('#tblRequerimientos').DataTable({
        "language": {
            "url": "//cdn.datatables.net/plug-ins/1.10.24/i18n/Spanish.json"
        },
        "order": [[0, "desc"]],
        "pageLength": 25
    });
});

function confirmarRecepcion(numReq, codOP, motivo) {
    Swal.fire({
        title: '¿Confirmar Recepción?',
        text: `Se procesará el requerimiento N° ${numReq} y se actualizará el stock de insumos.`,
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#28a745',
        cancelButtonColor: '#d33',
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

            $.ajax({
                url: '/Liquidacion/ConfirmarRecepcion',
                type: 'POST',
                data: {
                    numRequerimiento: numReq,
                    codOrdPro: codOP,
                    motivo: motivo
                },
                success: function (res) {
                    if (res.success) {
                        Swal.fire(
                            '¡Éxito!',
                            'Requerimiento procesado y stock actualizado.',
                            'success'
                        ).then(() => {
                            // Remover la fila visualmente sin recargar si se desea, 
                            // o simplemente recargar
                            location.reload();
                        });
                    } else {
                        Swal.fire(
                            'Error',
                            res.message || 'Ocurrió un error al procesar el requerimiento.',
                            'error'
                        );
                    }
                },
                error: function (err) {
                    Swal.fire('Error', 'Error de conexión con el servidor.', 'error');
                }
            });
        }
    });
}
