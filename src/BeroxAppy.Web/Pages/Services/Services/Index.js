$(function () {

    var createModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'Services/Services/CreateEditModal',
        modalClass: 'ServiceCreateEditModal'
    });

    var editModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'Services/Services/CreateEditModal',
        modalClass: 'ServiceCreateEditModal'
    });

    // Kategori listesini yükle
    loadCategories();

    var dataTable = $('#ServicesTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: true,
            order: [[1, "asc"]], // Title'a göre sırala
            searching: true,
            scrollX: true,
            ajax: function (data, callback, settings) {
                var filter = data.search.value || '';

                // Sıralama bilgisini hazırla
                var sorting = '';
                if (data.order && data.order.length > 0) {
                    var sortColumn = data.columns[data.order[0].column];
                    if (sortColumn && sortColumn.data && sortColumn.orderable) {
                        sorting = sortColumn.data + ' ' + data.order[0].dir;
                    }
                }

                // Filtreler
                var categoryId = $('#CategoryFilter').val() || null;
                var targetGender = $('#GenderFilter').val() || null;
                var isActive = $('#IsActiveFilter').val() || null;
                var minPrice = parseFloat($('#MinPriceFilter').val()) || null;
                var maxPrice = parseFloat($('#MaxPriceFilter').val()) || null;

                beroxAppy.services.service.getList({
                    filter: filter,
                    categoryId: categoryId,
                    targetGender: targetGender ? parseInt(targetGender) : null,
                    isActive: isActive ? isActive === 'true' : null,
                    minPrice: minPrice,
                    maxPrice: maxPrice,
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
                                    var newStatus = !data.record.isActive;
                                    var message = newStatus ? 'aktif' : 'pasif';

                                    abp.message.confirm(
                                        'Bu hizmeti ' + message + ' yapmak istediğinizden emin misiniz?'
                                    ).then(function (confirmed) {
                                        if (confirmed) {
                                            beroxAppy.services.service
                                                .setActiveStatus(data.record.id, newStatus)
                                                .then(function () {
                                                    abp.notify.success('Hizmet durumu güncellendi');
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
                                    return 'Bu hizmeti silmek istediğinizden emin misiniz? Hizmet: ' + data.record.title;
                                },
                                action: function (data) {
                                    beroxAppy.services.service
                                        .delete(data.record.id)
                                        .then(function () {
                                            abp.notify.success('Hizmet başarıyla silindi');
                                            dataTable.ajax.reload();
                                        });
                                }
                            }
                        ]
                    }
                },
                {
                    title: 'Hizmet Adı',
                    data: "title",
                    render: function (data, type, row) {
                        var html = '<div><strong>' + data + '</strong>';
                        if (row.description) {
                            html += '<br><small class="text-muted">' +
                                (row.description.length > 50 ?
                                    row.description.substring(0, 50) + '...' :
                                    row.description) + '</small>';
                        }
                        html += '</div>';
                        return html;
                    }
                },
                {
                    title: 'Kategori',
                    data: "categoryName",
                    render: function (data) {
                        return data || '<span class="text-muted">Kategorisiz</span>';
                    }
                },
                {
                    title: 'Cinsiyet',
                    data: "targetGenderDisplay",
                    render: function (data, type, row) {
                        var badgeClass = '';
                        switch (row.targetGender) {
                            case 0: badgeClass = 'bg-secondary'; break; // Unisex
                            case 1: badgeClass = 'bg-primary'; break;   // Male
                            case 2: badgeClass = 'bg-danger'; break;    // Female
                        }
                        return '<span class="badge ' + badgeClass + '">' + data + '</span>';
                    }
                },
                {
                    title: 'Süre',
                    data: "durationDisplay",
                    render: function (data) {
                        return '<span class="badge bg-info">' + data + '</span>';
                    }
                },
                {
                    title: 'Fiyat',
                    data: "price",
                    render: function (data) {
                        return '<strong>' + data.toLocaleString('tr-TR', {
                            style: 'currency',
                            currency: 'TRY'
                        }) + '</strong>';
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

    // Event handlers
    createModal.onResult(function () {
        dataTable.ajax.reload();
        abp.notify.success('Hizmet başarıyla oluşturuldu');
    });

    editModal.onResult(function () {
        dataTable.ajax.reload();
        abp.notify.success('Hizmet başarıyla güncellendi');
    });

    $('#NewServiceButton').click(function (e) {
        e.preventDefault();
        createModal.open();
    });

    // Filtre events
    $('#CategoryFilter, #GenderFilter, #IsActiveFilter').change(function () {
        dataTable.ajax.reload();
    });

    $('#MinPriceFilter, #MaxPriceFilter').on('input', debounce(function () {
        dataTable.ajax.reload();
    }, 500));

    $('#ClearFiltersButton').click(function () {
        $('#CategoryFilter').val('');
        $('#GenderFilter').val('');
        $('#IsActiveFilter').val('');
        $('#MinPriceFilter').val('');
        $('#MaxPriceFilter').val('');
        dataTable.ajax.reload();
    });

    // Helper functions
    function loadCategories() {
        beroxAppy.services.serviceCategory.getActiveList().done(function (result) {
            var $categoryFilter = $('#CategoryFilter');
            $categoryFilter.empty().append('<option value="">Tüm Kategoriler</option>');

            result.items.forEach(function (category) {
                $categoryFilter.append('<option value="' + category.id + '">' + category.name + '</option>');
            });
        });
    }

    function debounce(func, wait) {
        var timeout;
        return function executedFunction() {
            var context = this;
            var args = arguments;
            var later = function () {
                timeout = null;
                func.apply(context, args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }
});