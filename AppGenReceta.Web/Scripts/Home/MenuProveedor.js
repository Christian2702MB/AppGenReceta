//////Cuando la ventana cargue por completo
////window.onload = function () {
////    //alert('pagina completa');
////    $('#onload').fadeOut();
////    $('form').removeClass('hidden');
////};


document.querySelectorAll('#btnUpdateEstado').forEach(e => {
    e.addEventListener('click', function (element) {
        var codpid = element.currentTarget.dataset.codigovisitante;
        $.ajax({
            url: $('#URL_SimularEstado').val(),
            type: 'GET',
            data: 'codpid=' + codpid,
            dataType: 'JSON',
            success: function (json) {
                if (json.result == 'success') {
                    setTimeout(() => {
                        console.log("ingrese1");
                        $('#btnUpdateEstado').val('Enviando....');
                        $('#btnUpdateEstado').attr('disabled', 'disabled');
                    }, 20)

                    const pIDCliHijo = document.getElementById('ClienteHijo').value;
                    if (pIDCliHijo == '') {
                        window.location.href = '/Home/MenuProveedor/' + '?id=' + '0'
                    } else {
                        window.location.href = '/Home/MenuProveedor/' + '?id=' + '0' + '&pIDCliHijo=' + pIDCliHijo;
                    }
                }
            }
        });
    })
})

$('#ClienteHijo').on('change', function () {
    var IDCliHijo = $(this).val();
    window.location.href = '/Home/MenuProveedor/' + '?id=' + '0' + '&pIDCliHijo=' + IDCliHijo;
});


//$('#txtBuscar').on('clic', function () {
//    const IDCliHijo = document.getElementById('ClienteHijo').value;
//    const tempBusqueda = document.getElementById('txtBuscar').value;
//    window.location.href = '/Home/MenuProveedor/' + '?id=' + '0' + '&pIDCliHijo=' + IDCliHijo + '&pBusqueda=' + tempBusqueda;
//}

//$('#txtBuscar').on('change', function () {
//    const IDCliHijo = document.getElementById('ClienteHijo').value;
//    const tempBusqueda = document.getElementById('txtBuscar').value;
//    window.location.href = '/Home/MenuProveedor/' + '?id=' + '0' + '&pIDCliHijo=' + IDCliHijo + '&pBusqueda=' + tempBusqueda;
//}

//$('#txtBuscar').on('change', function () {
//    const tempBusqueda = document.getElementById('txtBuscar').value;
//    const IDCliHijo = document.getElementById('ClienteHijo').value;
//    window.location.href = '/Home/MenuProveedor/' + '?id=' + '0' + '&pIDCliHijo=' + IDCliHijo + '&pBusqueda=' + tempBusqueda;
//});

$('#txtBuscar').keypress(function (event) {
    var keycode = (event.keyCode ? event.keyCode : event.which);
    if (keycode == '13') {
        codpid = "0";
        $.ajax({
            url: $('#URL_BuscaRegistro').val(),
            type: 'GET',
            data: 'codpid=' + codpid,
            dataType: 'JSON',
            success: function (json) {
                if (json.result == 'success') {
                    setTimeout(() => {
                        console.log("ingrese1");
                        $('#btnUpdateEstado').val('Enviando....');
                        $('#btnUpdateEstado').attr('disabled', 'disabled');
                    }, 20)
                    const tempBusqueda = document.getElementById('txtBuscar').value;
                    const pIDCliHijo = document.getElementById('ClienteHijo').value;
                    if (pIDCliHijo == '') {
                        window.location.href = '/Home/MenuProveedor/' + '?id=' + '0' + '&pBusqueda=' + tempBusqueda;
                    } else {
                        window.location.href = '/Home/MenuProveedor/' + '?id=' + '0' + '&pIDCliHijo=' + pIDCliHijo + '&pBusqueda=' + tempBusqueda;
                    }
                }
            }
        });
    }
});

//Cuando el html este listo

//$('#btnHomeLogin').on('click', function (e) {
//    e.preventDefault();

//    $.ajax({
//        url: $('#formHomeLogin').attr('action'),
//        data: $('#formHomeLogin').serialize(),
//        dataType: 'json',
//        type: 'post',
//        beforeSend: function () {
//            $('#btnHomeLogin').val('Validando....');

//            $('#btnHomeLogin').attr('disabled', 'disabled');
//        },
//        complete: function () {
//            $('#btnHomeLogin').val('Entrar <i class="icon-circle-right2 position-right"></i>');

//            $('#btnHomeLogin').removeAttr('disabled');

//        },
//        success: function (json) {
//            $('#btnHomeLogin').val('Entrar <i class="icon-circle-right2 position-right"></i');

//            $('#btnHomeLogin').removeAttr('disabled');

//            if (json.result == 'warning') {

//                Swal.fire({
//                    icon: 'warning',
//                    title: json.title,
//                    text: json.message
//                });

//            } else if (json.result == 'success') {

//                Swal.fire(
//                    json.title,
//                    json.message,
//                    'success');

//                document.location = json.action;

//            } else {
//                Swal.fire({
//                    icon: 'error',
//                    title: json.title,
//                    text: json.message
//                });
//            }



//        },
//        error: function () {
//            $('#btnHomeLogin').val('Entrar <i class="icon-circle-right2 position-right"></i');

//            $('#btnHomeLogin').removeAttr('disabled');

//        }
//    });

//});