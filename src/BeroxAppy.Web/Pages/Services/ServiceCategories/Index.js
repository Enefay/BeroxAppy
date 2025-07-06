// Pages/Services/ServiceCategories/Index.js
$(function () {

    var createModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'Services/ServiceCategories/CreateEditModal',
        modalClass: 'ServiceCategoryCreateEditModal'
    });

    var editModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'Services/ServiceCategories/CreateEditModal',
        modalClass: 'ServiceCategoryCreateEditModal'
    });

    var dataTable = $('#ServiceCategoriesTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: true,
            order: [[1, "asc"]], // DisplayOrder'a göre sırala
            searching: true,
            scrollX: true,
            ajax: function (data, callback, settings) {
                var filter = data.search.value || '';

                // Sıralama bilgisini düzgün hazırlayalım
                var sorting = '';
                if (data.order && data.order.length > 0) {
                    var sortColumn = data.columns[data.order[0].column];
                    if (sortColumn && sortColumn.data && sortColumn.orderable) {
                        sorting = sortColumn.data + ' ' + data.order[0].dir;
                    }
                }

                beroxAppy.services.serviceCategory.getList({
                    filter: filter,
                    sorting: sorting,
                    skipCount: data.start,
                    maxResultCount: data.length
                }).done(function (result) {
                    callback({
                        draw: data.draw,
                        recordsTotal: result.totalCount,
                        recordsFiltered: result.totalCount,
                        data: result.items
                    });
                }).fail(function (error) {
                    abp.notify.error(error.message || 'Bir hata oluştu');
                });
            },
            columnDefs: [
                {
                    title: 'İşlemler',
                    orderable: false,
                    rowAction: {
                        items: [
                            {
                                text: 'Düzenle',
                                iconStyle: 'fas fa-edit',
                                action: function (data) {
                                    editModal.open({ id: data.record.id });
                                }
                            },
                            {
                                text: function (data) {
                                    return data.isActive ? 'Pasif Yap' : 'Aktif Yap';
                                },
                                iconStyle: function (data) {
                                    return data.isActive ? 'fas fa-eye-slash' : 'fas fa-eye';
                                },
                                action: function (data) {
                                    debugger
                                    var newStatus = !data.record.isActive;
                                    var message = newStatus ? 'aktif' : 'pasif';

                                    abp.message.confirm(
                                        'Bu kategoriyi ' + message + ' yapmak istediğinizden emin misiniz?'
                                    ).then(function (confirmed) {
                                        if (confirmed) {
                                            beroxAppy.services.serviceCategory
                                                .setActiveStatus(data.record.id, newStatus)
                                                .then(function () {
                                                    abp.notify.success('Kategori durumu güncellendi');
                                                    dataTable.ajax.reload();
                                                });
                                        }
                                    });
                                }
                            },
                            {
                                text: 'Sil',
                                iconStyle: 'fas fa-trash',
                                confirmMessage: function (data) {
                                    return 'Bu kategoriyi silmek istediğinizden emin misiniz? Kategori: ' + data.record.name;
                                },
                                action: function (data) {
                                    beroxAppy.services.serviceCategory
                                        .delete(data.record.id)
                                        .then(function () {
                                            abp.notify.success('Kategori başarıyla silindi');
                                            dataTable.ajax.reload();
                                        });
                                }
                            }
                        ]
                    }
                },
                {
                    title: 'Sıra',
                    data: "displayOrder",
                    render: function (data) {
                        return '<span class="badge bg-secondary">' + data + '</span>';
                    }
                },
                {
                    title: 'Renk',
                    data: "color",
                    orderable: false,
                    render: function (data) {
                        return '<div style="background-color: ' + data + '; width: 30px; height: 30px; border-radius: 50%; border: 2px solid #ddd; display: inline-block;"></div>';
                    }
                },
                {
                    title: 'Kategori Adı',
                    data: "name",
                    render: function (data, type, row) {
                        return '<div class="d-flex align-items-center">' +
                            '<div style="background-color: ' + row.color + '; width: 15px; height: 15px; border-radius: 3px; margin-right: 8px;"></div>' +
                            '<strong>' + data + '</strong>' +
                            '</div>';
                    }
                },
                {
                    title: 'Durum',
                    data: "isActive",
                    render: function (data) {
                        if (data) {
                            return '<span class="badge bg-success">Aktif</span>';
                        } else {
                            return '<span class="badge bg-danger">Pasif</span>';
                        }
                    }
                },
                {
                    title: 'Oluşturma Tarihi',
                    data: "creationTime",
                    render: function (data) {
                        return luxon
                            .DateTime
                            .fromISO(data, {
                                locale: abp.localization.currentCulture.name
                            }).toLocaleString(luxon.DateTime.DATETIME_SHORT);
                    }
                }
            ]
        })
    );

    createModal.onResult(function () {
        dataTable.ajax.reload();
        abp.notify.success('Kategori başarıyla oluşturuldu');
    });

    editModal.onResult(function () {
        dataTable.ajax.reload();
        abp.notify.success('Kategori başarıyla güncellendi');
    });

    $('#NewServiceCategoryButton').click(function (e) {
        e.preventDefault();
        createModal.open();
    });
});