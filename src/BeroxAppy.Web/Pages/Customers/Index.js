$(function () {

    var createModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'Customers/CreateEditModal',
        modalClass: 'CustomerCreateEditModal'
    });

    var editModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'Customers/CreateEditModal',
        modalClass: 'CustomerCreateEditModal'
    });

    var dataTable = $('#CustomersTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: true,
            order: [[1, "asc"]], // FullName'e göre sırala
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
                var gender = $('#GenderFilter').val() || null;
                var isActive = $('#IsActiveFilter').val() || null;
                var hasDebt = $('#HasDebtFilter').val() || null;
                var minDiscountRate = parseFloat($('#MinDiscountFilter').val()) || null;
                var maxDiscountRate = parseFloat($('#MaxDiscountFilter').val()) || null;
                var birthDateFrom = $('#BirthDateFromFilter').val() || null;
                var birthDateTo = $('#BirthDateToFilter').val() || null;

                beroxAppy.customers.customer.getList({
                    filter: filter,
                    gender: gender ? parseInt(gender) : null,
                    isActive: isActive ? isActive === 'true' : null,
                    hasDebt: hasDebt ? hasDebt === 'true' : null,
                    minDiscountRate: minDiscountRate,
                    maxDiscountRate: maxDiscountRate,
                    birthDateFrom: birthDateFrom,
                    birthDateTo: birthDateTo,
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
                                text: 'Telefon Et',
                                iconStyle: 'fas fa-phone',
                                action: function (data) {
                                    window.open('tel:' + data.record.phone);
                                }
                            },
                            {
                                text: 'WhatsApp',
                                iconStyle: 'fab fa-whatsapp',
                                action: function (data) {
                                    var phone = data.record.phone.replace(/\D/g, '');
                                    window.open('https://wa.me/90' + phone, '_blank');
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
                                        'Bu müşteriyi ' + message + ' yapmak istediğinizden emin misiniz?'
                                    ).then(function (confirmed) {
                                        if (confirmed) {
                                            beroxAppy.customers.customer
                                                .setActiveStatus(data.record.id, newStatus)
                                                .then(function () {
                                                    abp.notify.success('Müşteri durumu güncellendi');
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
                                    return 'Bu müşteriyi silmek istediğinizden emin misiniz? Müşteri: ' + data.record.fullName;
                                },
                                action: function (data) {
                                    beroxAppy.customers.customer
                                        .delete(data.record.id)
                                        .then(function () {
                                            abp.notify.success('Müşteri başarıyla silindi');
                                            dataTable.ajax.reload();
                                        });
                                }
                            }
                        ]
                    }
                },
                {
                    title: 'Ad Soyad',
                    data: "fullName",
                    render: function (data, type, row) {
                        var html = '<div><strong>' + data + '</strong>';
                        if (row.instagram) {
                            html += '<br><small class="text-muted"><i class="fab fa-instagram"></i> @' + row.instagram + '</small>';
                        }
                        html += '</div>';
                        return html;
                    }
                },
                {
                    title: 'Telefon',
                    data: "phone",
                    render: function (data) {
                        return '<a href="tel:' + data + '" class="text-decoration-none">' +
                            '<i class="fas fa-phone text-success me-1"></i>' + data + '</a>';
                    }
                },
                {
                    title: 'Email',
                    data: "email",
                    render: function (data) {
                        if (data) {
                            return '<a href="mailto:' + data + '" class="text-decoration-none">' +
                                '<i class="fas fa-envelope text-info me-1"></i>' + data + '</a>';
                        }
                        return '<span class="text-muted">-</span>';
                    }
                },
                {
                    title: 'Cinsiyet',
                    data: "genderDisplay",
                    render: function (data, type, row) {
                        var badgeClass = '';
                        switch (row.gender) {
                            case 0: badgeClass = 'bg-secondary'; break; // Unisex
                            case 1: badgeClass = 'bg-primary'; break;   // Male
                            case 2: badgeClass = 'bg-danger'; break;    // Female
                        }
                        return '<span class="badge ' + badgeClass + '">' + data + '</span>';
                    }
                },
                {
                    title: 'Yaş',
                    data: "age",
                    render: function (data) {
                        if (data) {
                            return '<span class="badge bg-info">' + data + ' yaş</span>';
                        }
                        return '<span class="text-muted">-</span>';
                    }
                },
                {
                    title: 'İndirim',
                    data: "discountRate",
                    render: function (data) {
                        if (data > 0) {
                            return '<span class="badge bg-warning">%' + data.toFixed(1) + '</span>';
                        }
                        return '<span class="text-muted">-</span>';
                    }
                },
                {
                    title: 'Borç',
                    data: "debtStatusDisplay",
                    render: function (data, type, row) {
                        if (row.totalDebt > 0) {
                            return '<span class="badge bg-danger">' + data + '</span>';
                        }
                        return '<span class="badge bg-success">' + data + '</span>';
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
                    title: 'Kayıt Tarihi',
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
        abp.notify.success('Müşteri başarıyla oluşturuldu');
    });

    editModal.onResult(function () {
        dataTable.ajax.reload();
        abp.notify.success('Müşteri başarıyla güncellendi');
    });

    $('#NewCustomerButton').click(function (e) {
        e.preventDefault();
        createModal.open();
    });

    // Filtre events
    $('#GenderFilter, #IsActiveFilter, #HasDebtFilter').change(function () {
        dataTable.ajax.reload();
    });

    $('#FilterInput').on('input', debounce(function () {
        dataTable.ajax.reload();
    }, 500));

    $('#MinDiscountFilter, #MaxDiscountFilter').on('input', debounce(function () {
        dataTable.ajax.reload();
    }, 500));

    $('#BirthDateFromFilter, #BirthDateToFilter').change(function () {
        dataTable.ajax.reload();
    });

    $('#ClearFiltersButton').click(function () {
        $('#FilterInput').val('');
        $('#GenderFilter').val('');
        $('#IsActiveFilter').val('');
        $('#HasDebtFilter').val('');
        $('#MinDiscountFilter').val('');
        $('#MaxDiscountFilter').val('');
        $('#BirthDateFromFilter').val('');
        $('#BirthDateToFilter').val('');
        dataTable.ajax.reload();
    });

    // İstatistikler
    $('#StatsButton').click(function () {
        beroxAppy.customers.customer.getStats().done(function (stats) {
            var html =
                '<div class="col-md-6">' +
                '<div class="card bg-primary text-white mb-3">' +
                '<div class="card-body text-center">' +
                '<h3 class="card-title">' + stats.totalCustomers + '</h3>' +
                '<p class="card-text">Toplam Müşteri</p>' +
                '</div>' +
                '</div>' +
                '</div>' +
                '<div class="col-md-6">' +
                '<div class="card bg-success text-white mb-3">' +
                '<div class="card-body text-center">' +
                '<h3 class="card-title">' + stats.activeCustomers + '</h3>' +
                '<p class="card-text">Aktif Müşteri</p>' +
                '</div>' +
                '</div>' +
                '</div>' +
                '<div class="col-md-6">' +
                '<div class="card bg-warning text-dark mb-3">' +
                '<div class="card-body text-center">' +
                '<h3 class="card-title">' + stats.customersWithDebt + '</h3>' +
                '<p class="card-text">Borçlu Müşteri</p>' +
                '</div>' +
                '</div>' +
                '</div>' +
                '<div class="col-md-6">' +
                '<div class="card bg-danger text-white mb-3">' +
                '<div class="card-body text-center">' +
                '<h3 class="card-title">₺' + stats.totalDebtAmount.toLocaleString('tr-TR', { minimumFractionDigits: 2 }) + '</h3>' +
                '<p class="card-text">Toplam Borç</p>' +
                '</div>' +
                '</div>' +
                '</div>' +
                '<div class="col-md-6">' +
                '<div class="card bg-info text-white mb-3">' +
                '<div class="card-body text-center">' +
                '<h3 class="card-title">' + stats.newCustomersThisMonth + '</h3>' +
                '<p class="card-text">Bu Ay Yeni</p>' +
                '</div>' +
                '</div>' +
                '</div>' +
                '<div class="col-md-6">' +
                '<div class="card bg-secondary text-white mb-3">' +
                '<div class="card-body text-center">' +
                '<h3 class="card-title">' + stats.birthdaysThisMonth + '</h3>' +
                '<p class="card-text">Bu Ay Doğum Günü</p>' +
                '</div>' +
                '</div>' +
                '</div>';

            $('#StatsContent').html(html);
            $('#StatsModal').modal('show');
        });
    });

    // Bugün doğum günü olanları göster
    $('#BirthdayTodayButton').click(function () {
        var today = new Date();
        beroxAppy.customers.customer.getBirthdayCustomers(today.toISOString()).done(function (result) {
            if (result.items.length > 0) {
                var message = 'Bugün doğum günü olan müşteriler:\n\n';
                result.items.forEach(function (customer) {
                    message += '🎉 ' + customer.fullName + ' (' + customer.phone + ')\n';
                });
                abp.message.info(message, 'Doğum Günü Kutlamaları');
            } else {
                abp.notify.info('Bugün doğum günü olan müşteri bulunmamaktadır.');
            }
        });
    });

    // Excel export
    $('#ExportExcelButton').click(function () {
        // Bu kısım backend'de Excel export metodu gerektirir
        abp.notify.info('Excel export özelliği yakında eklenecek...');
    });

    // Helper functions
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