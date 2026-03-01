var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    const tbl = $('#tblData');
    const urlGetAll = tbl.data('url-getall');
    const urlUpsert = tbl.data('url-upsert');
    const urlDelete = tbl.data('url-delete');

    dataTable = tbl.DataTable({
        "ajax": { url: urlGetAll },
        "columns": [
            { data: 'name', "width": "30%", "className": "ps-4" },
            { data: 'model', "width": "15%", "className": "d-none d-lg-table-cell" },
            { data: 'listPrice', "width": "10%", "className": "d-none d-sm-table-cell" },
            { data: 'brand', "width": "15%", "className": "d-none d-md-table-cell" },
            { data: 'category.name', "width": "15%" },
            {
                data: 'id',
                "render": function (data) {
                    return `<div class="btn-group" role="group">
                        <a href="${urlUpsert}?id=${data}" class="btn btn-primary btn-sm mx-1 rounded"> <i class="bi bi-pencil-square"></i></a>
                        <a onClick=Delete('${urlDelete}/${data}') class="btn btn-danger btn-sm mx-1 rounded"> <i class="bi bi-trash-fill"></i></a>
                    </div>`
                },
                "width": "20%",
                "className": "text-end pe-4"
            }
        ]
    });
}

function Delete(url) {
    Swal.fire({
        title: 'Are you sure?',
        text: "You won't be able to revert this!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, delete it!'
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
