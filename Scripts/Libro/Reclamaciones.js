

$('#tLibros').DataTable(
        {
            "processing": true,
            "serverSide": true,
            "searching": false,
            "sort": false,
            "lengthChange": false,
            "ajax": {
                "url": $('#URL_ListarLibroReclamoPaginado').val(),
                "type": "POST"
            },
            "oLanguage": {
                "oPaginate": {
                    "sFirst": "Primero",
                    "sNext": "Siguiente",
                    "sPrevious": "Anterior",
                    "sLast": "Ultimo"
                },
                "sInfo": "_START_ a _END_ de _TOTAL_ registros",
                "sLengthMenu": "Mostrar _MENU_ registros",
                "sSearch": "Buscar:",
                "sProcessing": "",
                "sInfoFiltered": "",
                "sInfoFiltered": '',
                "sZeroRecords": "No se encontro registros",
                "sInfoEmpty": "No hay registros para mostrar"
            },
            "serverParams": function (setting) {
                setting.Estado = $('#Estado_formBusqueda').val();
                setting.Codigo = $('#Codigo_formBusqueda').val();
                setting.IDTipo = $('#IDTipo_formBusqueda').val();
            },
            "columns": [
                { "data": "Codigo" },
                { "data": "Nombres" },
                { "data": "ApellidoPaterno" },
                { "data": "ApellidoMaterno" },
                { "data": "Tipo.Nombre" },
                { "data": "FechaRegistroString" },
                { "data": "Estado" },
                { "data": "Accion" }
            ]
        });


$('#btnBuscar').on('click', function (e) {
    e.preventDefault();
    $("#tLibros").dataTable().fnDraw();
});

$('#tLibros').on('click', '.btnVer', function (e) {
    e.preventDefault();
    let Codigo = $(this).attr('data-id');

    $.ajax({
        url: $('#URL_ObtenerLibroReclamacionPorCodigo').val(),
        data: 'Codigo=' + Codigo,
        dataType: 'json',
        type:'post',
        success: function (json) {
            $('#Codigo_ModalReclamo').html(json.Codigo);
            $('#Nombres_ModalReclamo').val(json.Nombres);
            $('#ApellidoPaterno_ModalReclamo').val(json.ApellidoPaterno);
            $('#ApellidoMaterno_ModalReclamo').val(json.ApellidoMaterno);
            $('#Correo_ModalReclamo').val(json.Correo);
            $('#Documento_ModalReclamo').val(json.Documento);
            $('#Departamento_ModalReclamo').val(json.Ubigeo.Departamento);
            $('#Provincia_ModalReclamo').val(json.Ubigeo.Provincia);
            $('#Distrito_ModalReclamo').val(json.Ubigeo.Distrito);
            $('#Direccion_ModalReclamo').val(json.Direccion);
            $('#TelefonoCelular_ModalReclamo').val(json.TelefonoCelular);
            $('#BienContratado_ModalReclamo').val(json.BienContratado.Nombre);
            $('#MontoReclamado_ModalReclamo').val(json.MontoReclamado);
            $('#Descripcion_ModalReclamo').val(json.Descripcion);
            $('#Tipo_ModalReclamo').val(json.Tipo.Nombre);
            $('#Zonal_ModalReclamo').val(json.Sede.Zonal.Nombre);
            $('#Sede_ModalReclamo').val(json.Sede.Nombre);
            $('#Detalle_ModalReclamo').val(json.Detalle);
            $('#Pedido_ModalReclamo').val(json.Pedido);

            $('#Estado_ModalReclamo').val(json.Estado);
            $('#Estado_ModalReclamo').selectpicker('render');

            if (json.Procede) {
                $('#Procede_ModalReclamo').val('true');
            } else {
                $('#Procede_ModalReclamo').val('false');
            }
           
            $('#Procede_ModalReclamo').selectpicker('render');

            $('#FechaRegistro_ModalReclamo').val(json.FechaRegistroString);

            $('#IDLibroReclamacion_ModalReclamo').val(json.IDLibroReclamacion);
            $('#Respuesta_ModalReclamo').val(json.Respuesta);

            $('#ModalReclamo').modal('show');
        }
    });

});

$('#btnActualizarRespuesta').on('click', function (e) {
    e.preventDefault();

    $.ajax({
        url: $('#frmActualizarRespuesta').attr('action'),
        data: $('#frmActualizarRespuesta').serialize(),
        dataType: 'json',
        type: 'post',
        beforeSend: function () {
            $('#btnActualizarRespuesta').html('Actualizando....');

            $('#btnActualizarRespuesta').attr('disabled', 'disabled');
        },
        complete: function () {
            $('#btnActualizarRespuesta').html('Actualizar');

            $('#btnActualizarRespuesta').removeAttr('disabled');

        },
        success: function (json) {
            $('#btnActualizarRespuesta').html('Actualizar');

            $('#btnActualizarRespuesta').removeAttr('disabled');

            if (json.result == 'warning') {

                notificaciones(json.message, json.result);

            } else if (json.result == 'success') {

                notificaciones(json.message, json.result);

                $("#tLibros").dataTable().fnDraw();
                //$('#ModalReclamo').modal('hide');

            } else if (json.result == 'error') {
                notificaciones(json.message, json.result);
            }



        },
        error: function () {
            $('#btnActualizarRespuesta').html('Actualizar');
            $('#btnActualizarRespuesta').removeAttr('disabled');

        }
    });
});


$('#btnGenerarCorreo').on('click', function (e) {
    e.preventDefault();

    $.ajax({
        url: $('#URL_EnviarCorreo').val(),
        data: 'Codigo='+ $('#Codigo_ModalReclamo').html(),
        dataType: 'json',
        type: 'post',
        beforeSend: function () {
            $('#btnGenerarCorreo').html('Enviando....');

            $('#btnGenerarCorreo').attr('disabled', 'disabled');
        },
        complete: function () {
            $('#btnGenerarCorreo').html('Enviar Correo');

            $('#btnGenerarCorreo').removeAttr('disabled');

        },
        success: function (json) {
            $('#btnGenerarCorreo').html('Enviar Correo');

            $('#btnGenerarCorreo').removeAttr('disabled');

            if (json.result == 'warning') {

                notificaciones(json.message, json.result);

            } else if (json.result == 'success') {

                notificaciones(json.message, json.result);

                

            } else if (json.result == 'error') {
                notificaciones(json.message, json.result);
            }



        },
        error: function () {
            $('#btnGenerarCorreo').html('Enviar Correo');

            $('#btnGenerarCorreo').removeAttr('disabled');

        }
    });
});

function notificaciones(Mensaje, Tipo) {

    if (Tipo == 'warning') {
        Tipo = 'alert-warning';
    } else if (Tipo == 'success') {
        Tipo = 'alert-success';
    } else if (Tipo == 'error') {
        Tipo = 'alert-danger';
    }

    allowDismiss = true;

    $.notify({
        message: Mensaje
    }, {
        type: Tipo,
        allow_dismiss: allowDismiss,
        newest_on_top: true,
        timer: 1000,
        placement: {
            from: 'bottom',
            align: 'left'
        },
        animate: {
            enter: 'animated fadeInDown',
            exit: 'animated fadeOutUp'
        },
        template: '<div data-notify="container" class="bootstrap-notify-container alert alert-dismissible {0} ' + (allowDismiss ? "p-r-35" : "") + '" role="alert">' +
        '<button type="button" aria-hidden="true" class="close" data-notify="dismiss">×</button>' +
        '<span data-notify="icon"></span> ' +
        '<span data-notify="message">{2}</span>' +
        '<div class="progress" data-notify="progressbar">' +
        '<div class="progress-bar progress-bar-{0}" role="progressbar" aria-valuenow="0" aria-valuemin="0" aria-valuemax="100" style="width: 0%;"></div>' +
        '</div>' +
        '</div>'
    });
}