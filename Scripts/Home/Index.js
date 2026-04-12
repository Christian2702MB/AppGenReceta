

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
                    icon: 'error',
                    title: json.title,
                    text: json.message
                });

            } else if (json.result == 'success') {

                Swal.fire(
                        json.title,
                        json.message,
                        'success');

                document.location = json.action;

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