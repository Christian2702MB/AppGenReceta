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
});
