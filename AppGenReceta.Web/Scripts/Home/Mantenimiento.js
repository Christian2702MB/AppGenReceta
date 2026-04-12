function eliminarVisita(id) {
    Swal.fire({
        title: '¿Eliminar registro?',
        text: "Esta acción moverá el registro al historial. Debe ingresar un motivo.",
        icon: 'warning',
        input: 'textarea',
        inputPlaceholder: 'Ingrese el motivo de la eliminación...',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Sí, eliminar',
        cancelButtonText: 'Cancelar',
        inputValidator: (value) => {
            if (!value) {
                return '¡El comentario es obligatorio para eliminar!';
            }
        }
    }).then((result) => {
        if (result.value) {
            const comentarioUsuario = result.value;

            Swal.fire({
                title: 'Eliminando...',
                didOpen: () => { Swal.showLoading(); }
            });

            // Llamada al servidor
            $.post("/Home/EliminarVisita", {
                id: id,
                comentario: comentarioUsuario
            }, function (res) {
                if (res.success) {
                    Swal.fire('¡Eliminado!', 'El registro ha sido movido al historial.', 'success').then(() => {
                        location.reload();
                    });
                } else {
                    Swal.fire('Error', res.message, 'error');
                }
            }).fail(function () {
                Swal.fire('Error', 'Error de comunicación con el servidor.', 'error');
            });
        }
    });
}

//$(document).ready(function () {
//    cargarTablaRecetas();
//});

//function cargarTablaRecetas() {
//    $.get("/Home/ListarRecetas", function (res) {
//        $("#tblRecetas tbody").empty();
//        res.data.forEach(item => {
//            $("#tblRecetas tbody").append(`
//                <tr>
//                    <td>${item.Dato}</td>
//                    <td><b>${item.NP}</b></td>
//                    <td>${item.Tecnica}</td>
//                    <td>${item.Concepto}</td>
//                    <td>${item.Ubicacion}</td>
//                    <td>${item.FechaUDP}</td>
//                    <td>
//                        <button class="btn btn-sm btn-info" onclick="irAEditar('${item.Dato}')">
//                            <i class="fas fa-edit"></i>
//                        </button>
//                        <button class="btn btn-sm btn-danger" onclick="eliminarReceta('${item.Dato}')">
//                            <i class="fas fa-trash"></i>
//                        </button>
//                    </td>
//                </tr>
//            `);
//        });
//    });
//}

//function irAEditar(id) {
//    // Redirige al formulario principal pasando el ID por URL
//    window.location.href = "/Home/Visita?id=" + id;
//}

//function irAEditar(id) {
//    let timerInterval;
//    Swal.fire({
//        title: 'Cargando información...',
//        html: 'Iniciando sesión de edición en <b></b> milisegundos.',
//        timer: 2000, // Tiempo estimado o hasta que cambie la página
//        timerProgressBar: true,
//        didOpen: () => {
//            Swal.showLoading();
//            const b = Swal.getHtmlContainer().querySelector('b');
//            timerInterval = setInterval(() => {
//                b.textContent = Swal.getTimerLeft();
//            }, 100);
//        },
//        willClose: () => {
//            clearInterval(timerInterval);
//        }
//    });

//    // Redirige después de un pequeño delay para que se vea el efecto o directo
//    setTimeout(() => {
//        window.location.href = "/Home/VisitaCorregirSCTR/" + id;
//    }, 500);
//}

//function editarReceta(id) {
//    $.get("/Home/CargarReceta", { id: id }, function (res) {
//        if (res.success) {
//            const d = res.data;

//            // 1. Cabecera
//            $("#txtNP").val(d.NP);
//            $("#ddlTecnica").val(d.Tecnica);
//            $("#txtOperario").val(d.Operario);

//            // 2. Fecha (Convertir DD/MM/YYYY a YYYY-MM-DD para el input date)
//            if (d.FechaUDP) {
//                let p = d.FechaUDP.split('/');
//                $("#dtFechaUDP").val(`${p[2]}-${p[1]}-${p[0]}`);
//            }

//            // 3. Radios Horizontales
//            $(`input[name='concepto'][value='${d.Concepto}']`).prop('checked', true);
//            $(`input[name='ubicacion'][value='${d.Ubicacion}']`).prop('checked', true);

//            // 4. Detalle de Colores e Insumos
//            recetaMaster.Colores = d.Colores;
//            actualizarTablaColores(); // Esta función debe limpiar el div y hacer append de los colores cargados

//            Swal.fire("Cargado", "Puedes editar los datos ahora", "info");
//        }
//    });
//}