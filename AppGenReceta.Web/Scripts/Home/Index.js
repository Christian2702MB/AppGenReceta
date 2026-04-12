//////Cuando la ventana cargue por completo
////window.onload = function () {
////    //alert('pagina completa');
////    $('#onload').fadeOut();
////    $('form').removeClass('hidden');
////    $('table').removeClass('hidden');
////};

$('#btnHomeLogin').on('click', function (e) {
    e.preventDefault();

    $.ajax({
        url: $('#formHomeLogin').attr('action'),
        data: $('#formHomeLogin').serialize(),
        dataType: 'json',
        type: 'post',
        beforeSend: function () {
            $('#btnHomeLogin').val('Validando....');

            $('#btnHomeLogin').attr('disabled', 'disabled');
        },
        complete: function () {
            $('#btnHomeLogin').val('Entrar <i class="icon-circle-right2 position-right"></i>');

            $('#btnHomeLogin').removeAttr('disabled');

        },
        success: function (json) {
            $('#btnHomeLogin').val('Entrar <i class="icon-circle-right2 position-right"></i');

            $('#btnHomeLogin').removeAttr('disabled');

            if (json.result == 'warning') {

                Swal.fire({
                    icon: 'warning',
                    title: json.title,
                    text: json.message
                });

            } else if (json.result == 'success') {

                setTimeout(() => {
                    console.log("ingrese1");
                    $('#btnHomeLogin').val('Enviando....');
                    $('#btnHomeLogin').attr('disabled', 'disabled');
                }, 20)

                document.location = json.action;
                //window.location.href = '/Home/MenuProveedor';

            } else {
                Swal.fire({
                    icon: 'error',
                    title: json.title,
                    text: json.message
                });
            }



        },
        error: function () {
            $('#btnHomeLogin').val('Entrar <i class="icon-circle-right2 position-right"></i');

            $('#btnHomeLogin').removeAttr('disabled');

        }
    });

});



$('#btnCambioClave').on('click', function (e) {
    e.preventDefault();

    $.ajax({
        url: $('#formHomeLogin').attr('action'),
        data: $('#formHomeLogin').serialize(),
        dataType: 'json',
        type: 'post',
        beforeSend: function () {
            $('#btnCambioClave').val('Validando....');

            $('#btnCambioClave').attr('disabled', 'disabled');
        },
        complete: function () {
            $('#btnCambioClave').val('Entrar <i class="icon-circle-right2 position-right"></i>');

            $('#btnCambioClave').removeAttr('disabled');

        },
        success: function (json) {
            $('#btnCambioClave').val('Entrar <i class="icon-circle-right2 position-right"></i');

            $('#btnCambioClave').removeAttr('disabled');

            if (json.result == 'warning') {

                Swal.fire({
                    icon: 'warning',
                    title: json.title,
                    text: json.message
                });

            } else if (json.result == 'success') {
                //setTimeout(() => {
                //    $('#btnCambioClave').val('Enviando....');
                //    $('#btnCambioClave').attr('disabled', 'disabled');
                //}, 20)
                Swal.fire(
                    json.title,
                    json.message,
                    'success').then(function (result) {
                    if (result.value) {
                        document.location = json.action;
                        //window.location.href = "/Home/Index";
                        
                    }
                }

                );
            } else {
                Swal.fire({
                    icon: 'error',
                    title: json.title,
                    text: json.message
                });
            }



        },
        error: function () {
            $('#btnCambioClave').val('Entrar <i class="icon-circle-right2 position-right"></i');

            $('#btnCambioClave').removeAttr('disabled');

        }
    });

});


//************** Pruebas **************

//function debounce(func, wait) {
//    let timeout;
//    return function () {
//        const context = this;
//        const args = arguments;
//        clearTimeout(timeout);
//        timeout = setTimeout(() => {
//            func.apply(context, args);
//        }, wait);
//    };
//}

//const miElemento = document.getElementById('btnGrabarVisita');
//const miFuncion = () => {
//    console.log('La función se ha ejecutado después del debounce.');
//};

//miElemento.addEventListener('click', debounce(miFuncion, 300)); // Ejecutar después de 300ms sin nuevos clics


//$(".downloadupaddinvoice").click(function () {
//    var filename = $('#mySCTR').val();

//    if (filename == "" || filename == null) {
//        alert('Error');
//    } else {
//        var file = document.getElementById('mySCTR').files[0];
//        var filename = document.getElementById('mySCTR').files[0].name;
//        var blob = new Blob([file]);
//        var url = URL.createObjectURL(blob);

//        $(this).attr({ 'download': filename, 'href': url });
//        filename = "";
//    }

//})

//function descargarArchivo(id) {
//    const arr = "";
//    $.ajax({
//        type: "POST",
//        url: "/Home/Archivos",
//        data: { id },
//        success: function (response) {
//            var respuesta = response;

//            const nombreArchivo = document.getElementById('txtCuenta').value;
//            const formato = document.getElementById('txtClave').value;

//            alert("nombreArchivo: " + nombreArchivo + "formato: " + formato);

//            const blob = base64ToBlob(respuesta.Mensaje_Respuesta, formato);
//            guardarArchivo(blob, nombreArchivo);

//        },
//        error: function (jqXHR, textStatus, errorThrown) {
//            console.log(jqXHR);
//            console.log(textStatus);
//            console.log(errorThrown);
//            alert("Ocurrió un error al verificar los CFDI(s): " + jqXHR);
//        }
//    });
//}

//function base64ToBlob(base64, type = "application/octet-stream") {
//    const binStr = atob(base64);
//    const len = binStr.length;
//    const arr = new Uint8Array(len);
//    for (let i = 0; i < len; i++) {
//        arr[i] = binStr.charCodeAt(i);
//    }
//    return new Blob([arr], { type: type });
//}

//function guardarArchivo(blob, filename) {
//    if (window.navigator.msSaveOrOpenBlob) {
//        window.navigator.msSaveOrOpenBlob(blob, filename);
//    } else {
//        const a = document.createElement('a');
//        document.body.appendChild(a);
//        const url = window.URL.createObjectURL(blob);
//        a.href = url;
//        a.download = filename;
//        a.click();
//        setTimeout(() => {
//            window.URL.revokeObjectURL(url);
//            document.body.removeChild(a);
//        }, 0)
//    }
//}


//$('#txtCuenta').on('change', function () {
//    var pTotalMonto = $(this).val();
//    var pTotalMonto2 = Number.parseFloat(pTotalMonto).toFixed(2);
//    //$("#txtCuenta").val(pTotalMonto2).trigger('change');
//    if (pTotalMonto2 == "NaN") {
//        document.getElementById('txtCuenta').value = "";
//    } else { 
//        document.getElementById('txtCuenta').value = pTotalMonto2;
//    }    
//});

//function valideKeyEsp(evt) {
//    // code is the decimal ASCII representation of the pressed key.

//    var pTotalMonto = $(this).val();
//    var key = window.Event ? evt.which : evt.keyCode;
//    var chark = String.fromCharCode(key);
//    var tempValue = pTotalMonto + chark;


//    var code = (evt.which) ? evt.which : evt.keyCode;
//    if (code == 8) { // backspace.
//        return true;
//    } else if (code >= 48 && code <= 57) { // is a number.
//        if (filter(tempValue) === false) {
//            return false;
//        } else {
//            return true;
//        }
//        //return true;
//    } else { // other keys.
//        //if (key == 8 || key == 13 || key == 0) {
//        //    return true;
//        //} else if (key == 46) {
//        //    if (filter(tempValue) === false) {
//        //        return false;
//        //    } else {
//        //        return true;
//        //    }
//        //} else {
//        //    return false;
//        //}
//        return false;
//    }
//}
////
//function valideKey(evt) {
//    // code is the decimal ASCII representation of the pressed key.
//    var code = (evt.which) ? evt.which : evt.keyCode;
//    if (code == 8) { // backspace.
//        return true;
//    } else if (code >= 48 && code <= 57) { // is a number.
//        return true;
//    } else { // other keys.
//        return false;
//    }
//}
////NaN
//function filterFloat(evt) {
//    // Backspace = 8, Enter = 13, ‘0′ = 48, ‘9′ = 57, ‘.’ = 46, ‘-’ = 43
//    var pTotalMonto = $(this).val();
//    var key = window.Event ? evt.which : evt.keyCode;
//    var chark = String.fromCharCode(key);
//    var tempValue = pTotalMonto + chark;
//    if (key >= 48 && key <= 57) {
//        if (filter(tempValue) === false) {
//            return false;
//        } else {
//            return true;
//        }
//    } else {
//        if (key == 8 || key == 13 || key == 0) {
//            return true;
//        } else if (key == 46) {
//            if (filter(tempValue) === false) {
//                return false;
//            } else {
//                return true;
//            }
//        } else {
//            return false;
//        }
//    }
//}

//function filter(__val__) {
//    var preg = /^([0-9]+\.?[0-9]{0,2})$/;
//    if (preg.test(__val__) === true) {
//        return true;
//    } else {
//        return false;
//    }

//}

////function valideDecimal() {
////    const input = document.getElementById('TotalMonto');
////    const tot = Number.parseInt(evt, 10);
////    const total = evt.toFixed(2); //(10).toFixed(1)
////    const totalt = (evt).toString(10);
////    alert('Dato: ori ' + input);
////    alert('Dato: evt ' + evt);
////    alert('Dato: Math ' + Math.round(evt));
////    alert('Dato: total ' + total);
////    alert('Dato: tot ' + tot);
////    alert('Dato: totalt ' + totalt);
////    return parseFloat(evt);
////}