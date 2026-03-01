var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    const tbl = $('#tblData');
    if (!tbl.length) return;

    const urlGetAll = tbl.data('url-getall');
    const urlUpsert = tbl.data('url-upsert');
    const urlDelete = tbl.data('url-delete');

    dataTable = tbl.DataTable({
        "ajax": {
            "url": urlGetAll,
            "error": function (xhr, error, thrown) {
                console.error("DataTables AJAX error:", error, thrown);
                toastr.error("Error al cargar los productos. Por favor, recarga la página.");
            }
        },
        "columns": [
            {
                data: 'name',
                "width": "30%",
                "className": "ps-4",
                "render": function (data) {
                    return `<div class="fw-bold">${data}</div>`;
                }
            },
            { data: 'model', "width": "15%", "className": "d-none d-lg-table-cell" },
            {
                data: 'listPrice',
                "width": "15%",
                "className": "d-none d-sm-table-cell fw-bold text-info",
                "render": $.fn.dataTable.render.number(',', '.', 2, '$')
            },
            { data: 'brand', "width": "15%", "className": "d-none d-md-table-cell" },
            { data: 'category.name', "width": "15%", "className": "d-none d-sm-table-cell" },
            {
                data: 'id',
                "render": function (data) {
                    return `<div class="btn-group" role="group">
                        <a href="${urlUpsert}?id=${data}" class="btn btn-primary btn-sm mx-1 rounded" title="Editar"> 
                            <i class="bi bi-pencil-square"></i>
                        </a>
                        <a onclick="Delete('${urlDelete}/${data}')" class="btn btn-danger btn-sm mx-1 rounded" title="Eliminar"> 
                            <i class="bi bi-trash-fill"></i>
                        </a>
                    </div>`;
                },
                "width": "10%",
                "className": "text-end pe-4"
            }
        ],
        "createdRow": function (row, data, dataIndex) {
            // Add data-labels for mobile stacked view
            $('td', row).eq(0).attr('data-label', 'Producto');
            $('td', row).eq(1).attr('data-label', 'Modelo');
            $('td', row).eq(2).attr('data-label', 'Precio');
            $('td', row).eq(3).attr('data-label', 'Marca');
            $('td', row).eq(4).attr('data-label', 'Categoría');
            $('td', row).eq(5).attr('data-label', 'Acciones');
        },
        "language": {
            "emptyTable": "No se encontraron productos disponibles",
            "info": "Mostrando _START_ a _END_ de _TOTAL_ productos",
            "infoEmpty": "Mostrando 0 a 0 de 0 productos",
            "lengthMenu": "Mostrar _MENU_ registros",
            "loadingRecords": "Cargando...",
            "processing": "Procesando...",
            "search": "Buscar:",
            "zeroRecords": "No se encontraron resultados"
        }
    });
}

function Delete(url) {
    Swal.fire({
        title: '¿Estás seguro?',
        text: "¡No podrás revertir esto!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#00D4FF',
        cancelButtonColor: '#FF4757',
        confirmButtonText: 'Sí, eliminar',
        cancelButtonText: 'Cancelar'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'DELETE',
                success: function (data) {
                    if (data.success) {
                        dataTable.ajax.reload();
                        toastr.success(data.message);
                    } else {
                        toastr.error(data.message);
                    }
                }
            })
        }
    })
}
