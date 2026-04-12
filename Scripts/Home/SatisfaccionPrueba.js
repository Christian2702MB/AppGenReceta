$('#btnRegistrarSatisfaccion').on('click', function (e) {
    e.preventDefault();

    console.log("aqui");
    var form = document.getElementById("formLibroSatisfaccion");
    console.log(form);
    var data = new FormData(form);
    console.log(data);
    

    $.ajax({
        url: $('#formLibroSatisfaccion').attr('action'),
        data: data,
        dataType: 'json',
        type: 'post',
        contentType: false,
        processData: false,
        beforeSend: function () {
            console.log("ingrese1");
            $('#btnRegistrarSatisfaccion').val('Enviando....');

            $('#btnRegistrarSatisfaccion').attr('disabled', 'disabled');
        },
        complete: function () {
            console.log("ingrese2");
            $('#btnRegistrarSatisfaccion').val('<i class="fa fa-check"></i>&nbsp;Enviar');

            $('#btnRegistrarSatisfaccion').removeAttr('disabled');

        },
        success: function (json) {
            console.log("ingrese3");
            $('#btnRegistrarSatisfaccion').val('<i class="fa fa-check"></i>&nbsp;Enviar');

            $('#btnRegistrarSatisfaccion').removeAttr('disabled');

            if (json.result == 'warning') {

                Swal.fire({
                    icon: 'error',
                    title: json.title,
                    text: json.message
                });

            } else if (json.result == 'success') {

                Swal.fire({
                    icon: 'success',
                    title: json.title,
                    text: json.message,
                    footer: 'En breve recibirá un correo confirmando su respuesta enviada, de no recibirlo, favor verificar en su bandeja de correos no deseados.'
                }).then(function (result) {
                        if (result.value) {
                            window.location.href = "https://www.senati.edu.pe/";
                        }
                    });

                //Swal.fire(
                //        json.title,
                //        json.message,
                //    'success').then(function (result) {
                //        if (result.value) {
                //            window.location.href = "https://www.senati.edu.pe/";
                //        }
                //    });

                $('#RespuestaCliente').val('');
            }
        },
        error: function () {
            console.log("ingrese8");
            $('#btnRegistrarSatisfaccion').val('<i class="fa fa-check"></i>&nbsp;Enviar');

            $('#btnRegistrarSatisfaccion').removeAttr('disabled');

        }
    });

});

$('#btnCancelar').on('click', function (e) {

    e.preventDefault();

    window.location.href = "https://www.senati.edu.pe/";
});

$('input[type="radio"]').on('change', this, function () {
    console.log("radio va: " + $(this).val())
    if ($(this).val() == 'false') {
        $('.NoDeacuerdo').show(); 
    }
    if ($(this).val() == 'true') {
        $('.NoDeacuerdo').hide(); 
    }
});
