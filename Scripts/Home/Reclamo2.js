$('#CodigoDepartamento').on('change', function () {

    var CodigoDepartamento = $(this).val();


    $.ajax({
        url: $('#URL_ListarProvinciasPorDepartamento').val(),
        type: 'POST',
        data: 'CodigoDepartamento=' + CodigoDepartamento,
        dataType: 'JSON',
        success: function (data) {
            $("#CodigoProvincia").empty();
            $("#CodigoProvincia").html('<option value="0">Seleccione</option>');

            $("#IDUbigeo").empty();
            $("#IDUbigeo").html('<option value="0">Seleccione</option>');

            for (var i = 0; i < data.length; i++) {
                $("#CodigoProvincia").append('<option value=' + data[i].CodigoProvincia + '>' + data[i].Provincia + '</option>');
            }

        }
    });

});

$('#CodigoProvincia').on('change', function () {

    var CodigoProvincia = $(this).val();
    var CodigoDepartamento = $('#CodigoDepartamento').val();
    $.ajax({
        url: $('#URL_ListarDistritosPorProvincia').val(),
        type: 'POST',
        data: 'CodigoProvincia=' + CodigoProvincia + '&CodigoDepartamento=' + CodigoDepartamento,
        dataType: 'JSON',
        success: function (data) {

            $("#IDUbigeo").empty();
            $("#IDUbigeo").html('<option value="0">Seleccione</option>');
            for (var i = 0; i < data.length; i++) {
                $("#IDUbigeo").append('<option value=' + data[i].IDUbigeo + '>' + data[i].Distrito + '</option>');
            }
        }
    });

});


$('#IDContacto').on('change', function () {
    $("#IDAlumno").prop('disabled', true);
    var pIDContacto = $('#IDContacto option:selected').text();
    if (pIDContacto == "Alumno") {
        $("#IDAlumno").prop('disabled', false);
    }
    else {
        if (pIDContacto == "Egresado") {
            $("#IDAlumno").prop('disabled', false);
        }
        else {
            $("#IDAlumno").val('');
        }
    }
    
    console.log(pIDAlumno);
});


$('#IDZonal').on('change', function () {

    var IDZonal = $(this).val();
    $.ajax({
        url: $('#URL_ListarSedesPorIDZonal').val(),
        type: 'POST',
        data: 'IDZonal=' + IDZonal,
        dataType: 'JSON',
        success: function (data) {

            $("#IDSede").empty();
            $("#IDSede").html('<option value="0">Seleccione</option>');
            let encontrado = false;
            for (var i = 0; i < data.length; i++) {
                if (data[i].IDSede == SedeIDBuscarSede) {
                    encontrado = true;
                }
                $("#IDSede").append('<option value=' + data[i].IDSede + ' data-direccion="' + data[i].Direccion + '">' + data[i].Nombre + '</option>');
            }

            if (SedeIDBuscarSede != 0 && encontrado) {

                $("#IDSede").val(SedeIDBuscarSede).trigger('change');

            } else {
                $("#IDSede").val('0').trigger('change');
                $('#Sede_Direccion').html('');
            }
            $('#IDSede').selectpicker('refresh');



        }
    });

});

$('#IDSede').on('change', function () {

    var Direccion = $('#IDSede option:selected').attr('data-direccion');
    $('#Sede_Direccion').html(Direccion);

});

var SedeIDBuscarSede = 0;


$('#btnBuscarSede').on('click', function (e) {
    e.preventDefault();
    let NombreBuscar = $('#SedeBuscar').val();
    $.ajax({
        url: $('#URL_BuscarSedePorNombre').val(),
        data: 'NombreBuscar=' + NombreBuscar,
        dataType: 'json',
        type: 'post',

        beforeSend: function () {
            $('#btnBuscarSede').val('Buscando....');

            $('#btnBuscarSede').attr('disabled', 'disabled');
        },
        complete: function () {
            $('#btnBuscarSede').val('Buscar');

            $('#btnBuscarSede').removeAttr('disabled');

        },
        success: function (json) {
            $('#btnBuscarSede').val('Buscar');

            $('#btnBuscarSede').removeAttr('disabled');

            if (json == null) {

                $('#IDZonal').val('0').trigger('change');

                SedeIDBuscarSede = 0;

                $("#IDSede").val('0').trigger('change');
                $('#Sede_Direccion').html('');

                Swal.fire({
                    icon: 'error',
                    title: 'No encontrado',
                    text: 'Sede no encontrada'
                });

                $('#SedeBuscarResultadoLabel').html('');

            } else {

                SedeIDBuscarSede = json.IDSede;

                $('#IDZonal').val(json.Zonal.IDZonal).trigger('change');

                $('#SedeBuscarResultadoLabel').html('');

            }



        },
        error: function () {

            Swal.fire({
                icon: 'error',
                title: 'No encontrado',
                text: 'Sede no encontrada'
            });

            $('#IDZonal').val('0').trigger('change');

            SedeIDBuscarSede = 0;

            $("#IDSede").val('0').trigger('change');
            $('#Sede_Direccion').html('');

            $('#SedeBuscarResultadoLabel').html('');

            $('#btnBuscarSede').val('Buscar');

            $('#btnBuscarSede').removeAttr('disabled');

        }
    });

});




$('#btnRegistrar').on('click', function (e) {
    e.preventDefault();


    var form = document.getElementById("formLibroReclamo");

    var data = new FormData(form);

    var Evidencia = $("#Evidencia").get(0).files;
    if (Evidencia.length > 0) {
        data.append("DocEvidencia", Evidencia[0]);
    }

    $.ajax({
        url: $('#formLibroReclamo').attr('action'),
        data: data,
        dataType: 'json',
        type: 'post',
        contentType: false,
        processData: false,
        beforeSend: function () {
            $('#btnRegistrar').val('Enviando....');

            $('#btnRegistrar').attr('disabled', 'disabled');
        },
        complete: function () {
            $('#btnRegistrar').val('<i class="fa fa-check"></i>&nbsp;Registrar');

            $('#btnRegistrar').removeAttr('disabled');

        },
        success: function (json) {
            $('#btnRegistrar').val('<i class="fa fa-check"></i>&nbsp;Registrar');

            $('#btnRegistrar').removeAttr('disabled');

            if (json.result == 'warning') {

                Swal.fire({
                    icon: 'error',
                    title: json.title,
                    text: json.message
                });

            } else if (json.result == 'success') {

                Swal.fire(
                        json.title,
                        json.message,
                        'success');

                $('#Nombres').val('');
                $('#ApellidoPaterno').val('');
                $('#ApellidoMaterno').val('');
                $('#Documento').val('');
                $('#TelefonoCelular').val('');
                $('#Correo').val('');
                $('#Direccion').val('');
                $('#Apoderado').val('');
                $('#MontoReclamado').val('');
                $('#Descripcion').val('');
                $('#Detalle').val('');
                $('#Pedido').val('');
                $('#Apoderado').val('');

            }



        },
        error: function () {
            $('#btnRegistrar').val('<i class="fa fa-check"></i>&nbsp;Registrar');

            $('#btnRegistrar').removeAttr('disabled');

        }
    });

});