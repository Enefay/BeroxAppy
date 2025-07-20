$(function () {
    var l = abp.localization.getResource('BeroxAppy');
    var createModal = new abp.ModalManager(abp.appPath + 'Employees/CreateEditModal');
    var serviceAssignmentModal = new abp.ModalManager(abp.appPath + 'Employees/ServiceAssignmentModal');

    var dataTable = $('#EmployeesTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: true,
            order: [[1, "asc"]],
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
                var employeeType = $('#EmployeeTypeFilter').val() || null;
                var serviceGender = $('#ServiceGenderFilter').val() || null;
                var isActive = $('#IsActiveFilter').val() || null;
                var hasUser = $('#HasUserFilter').val() || null;
                var canTakeOnlineReservation = $('#CanTakeOnlineReservationFilter').val() || null;
                var minSalary = parseFloat($('#MinSalaryFilter').val()) || null;
                var maxSalary = parseFloat($('#MaxSalaryFilter').val()) || null;

                beroxAppy.employees.employee.getList({
                    filter: filter,
                    employeeType: employeeType ? parseInt(employeeType) : null,
                    serviceGender: serviceGender ? parseInt(serviceGender) : null,
                    isActive: isActive ? isActive === 'true' : null,
                    hasUser: hasUser ? hasUser === 'true' : null,
                    canTakeOnlineReservation: canTakeOnlineReservation ? canTakeOnlineReservation === 'true' : null,
                    minSalary: minSalary,
                    maxSalary: maxSalary,
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
                                //visible: abp.auth.isGranted('BeroxAppy.Employees.Edit'),
                                action: function (data) {
                                    createModal.open({ id: data.record.id });
                                }
                            },
                            {
                                text: 'Hizmet Ata',
                                //visible: abp.auth.isGranted('BeroxAppy.Employees.Edit'),
                                action: function (data) {
                                    serviceAssignmentModal.open({ employeeId: data.record.id });
                                }
                            },
                            {
                                text: 'Çalışma Saatleri',
                                //visible: abp.auth.isGranted('BeroxAppy.Employees.Edit'),
                                action: function (data) {
                                    // Çalışma saatleri modalı buraya eklenebilir
                                    abp.notify.info('Çalışma saatleri özelliği yakında eklenecek.');
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
                                        'Bu çalışanı ' + message + ' yapmak istediğinizden emin misiniz?'
                                    ).then(function (confirmed) {
                                        if (confirmed) {
                                            beroxAppy.employees.employee
                                                .setActiveStatus(data.record.id, newStatus)
                                                .then(function () {
                                                    abp.notify.success('Çalışan durumu güncellendi');
                                                    dataTable.ajax.reload();
                                                });
                                        }
                                    });
                                }
                            },
                            {
                                text: 'Sil',
                                //visible: abp.auth.isGranted('BeroxAppy.Employees.Delete'),
                                confirmMessage: function (data) {
                                    return 'Bu çalışanı silmek istediğinizden emin misiniz: ' + data.record.fullName;
                                },
                                action: function (data) {
                                    beroxAppy.employees.employee
                                        .delete(data.record.id)
                                        .then(function () {
                                            abp.notify.success('Çalışan başarıyla silindi.');
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
                        var html = '<div class="d-flex align-items-center">';
                        html += '<div class="me-2" style="width: 12px; height: 12px; background-color: ' + row.calendarColor + '; border-radius: 50%;"></div>';
                        html += '<div>';
                        html += '<strong>' + data + '</strong>';
                        if (row.email) {
                            html += '<br><small class="text-muted"><i class="fas fa-envelope me-1"></i>' + row.email + '</small>';
                        }
                        html += '</div></div>';
                        return html;
                    }
                },
                {
                    title: 'İletişim',
                    data: "phone",
                    render: function (data, type, row) {
                        var html = '<i class="fas fa-phone text-primary"></i> ' + data;
                        if (row.email) {
                            html += '<br><i class="fas fa-envelope text-info"></i> ' + row.email;
                        }
                        return html;
                    }
                },
                {
                    title: 'Hizmet Cinsiyet',
                    data: "serviceGenderDisplay",
                    render: function (data) {
                        var colorClass = '';
                        switch (data) {
                            case 'Erkek':
                                colorClass = 'bg-primary';
                                break;
                            case 'Kadın':
                                colorClass = 'bg-danger';
                                break;
                            case 'Unisex':
                                colorClass = 'bg-success';
                                break;
                            default:
                                colorClass = 'bg-secondary';
                        }
                        return '<span class="badge ' + colorClass + '">' + data + '</span>';
                    }
                },
                {
                    title: 'Maaş',
                    data: "fixedSalary",
                    render: function (data) {
                        return '₺' + data.toLocaleString('tr-TR', { minimumFractionDigits: 2 });
                    }
                },
                {
                    title: 'Komisyon',
                    data: "serviceCommissionRate",
                    render: function (data, type, row) {
                        return 'H: %' + data + '<br>' +
                            'Ü: %' + row.productCommissionRate + '<br>' +
                            'P: %' + row.packageCommissionRate;
                    }
                },
                {
                    title: 'Kullanıcı',
                    data: "userStatus",
                    render: function (data, type, row) {
                        var colorClass = '';
                        var iconClass = '';

                        switch (data) {
                            case 'Aktif':
                                colorClass = 'bg-success';
                                iconClass = 'fas fa-user-check';
                                break;
                            case 'Pasif':
                                colorClass = 'bg-warning';
                                iconClass = 'fas fa-user-times';
                                break;
                            case 'Kullanıcı Yok':
                                colorClass = 'bg-secondary';
                                iconClass = 'fas fa-user-plus';
                                break;
                            default:
                                colorClass = 'bg-danger';
                                iconClass = 'fas fa-exclamation-triangle';
                        }

                        return '<span class="badge ' + colorClass + '"><i class="' + iconClass + '"></i> ' + data + '</span>';
                    }
                },
                {
                    title: 'Online Rezervasyon',
                    data: "canTakeOnlineReservation",
                    render: function (data) {
                        if (data) {
                            return '<span class="badge bg-success"><i class="fas fa-check"></i> Evet</span>';
                        } else {
                            return '<span class="badge bg-secondary"><i class="fas fa-times"></i> Hayır</span>';
                        }
                    }
                },
                {
                    title: 'Durum',
                    data: "isActive",
                    render: function (data) {
                        if (data) {
                            return '<span class="badge bg-success"><i class="fas fa-check"></i> Aktif</span>';
                        } else {
                            return '<span class="badge bg-danger"><i class="fas fa-times"></i> Pasif</span>';
                        }
                    }
                },
                {
                    title: 'Oluşturulma',
                    data: "creationTime",
                    render: function (data) {
                        return luxon.DateTime.fromISO(data, {
                            locale: abp.localization.currentCulture.name
                        }).toLocaleString(luxon.DateTime.DATETIME_SHORT);
                    }
                }
            ]
        })
    );

    // Store dataTable reference globally for modal refresh
    window.employeeDataTable = dataTable;

    createModal.onResult(function () {
        dataTable.ajax.reload();
    });

    serviceAssignmentModal.onResult(function () {
        dataTable.ajax.reload();
    });

    $('#NewEmployeeButton').click(function (e) {
        e.preventDefault();
        createModal.open();
    });

    // Filtreleme işlemleri
    $('#EmployeeTypeFilter').change(function () {
        dataTable.ajax.reload();
    });

    $('#ServiceGenderFilter').change(function () {
        dataTable.ajax.reload();
    });

    $('#IsActiveFilter').change(function () {
        dataTable.ajax.reload();
    });

    $('#HasUserFilter').change(function () {
        dataTable.ajax.reload();
    });

    // Arama
    $('#EmployeeSearchInput').on('keyup', function () {
        dataTable.search(this.value).draw();
    });
});