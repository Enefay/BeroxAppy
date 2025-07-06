// Pages/Employees/Index.js
$(function () {

    var createModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'Employees/CreateEditModal',
        modalClass: 'EmployeeCreateEditModal'
    });

    var editModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'Employees/CreateEditModal',
        modalClass: 'EmployeeCreateEditModal'
    });

    var workingHoursModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'Employees/WorkingHoursModal',
        modalClass: 'EmployeeWorkingHoursModal'
    });

    var serviceAssignmentModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'Employees/ServiceAssignmentModal',
        modalClass: 'EmployeeServiceAssignmentModal'
    });

    var dataTable = $('#EmployeesTable').DataTable(
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
                                iconStyle: 'fas fa-edit',
                                action: function (data) {
                                    editModal.open({ id: data.record.id });
                                }
                            },
                            {
                                text: 'Çalışma Saatleri',
                                iconStyle: 'fas fa-clock',
                                action: function (data) {
                                    workingHoursModal.open({ employeeId: data.record.id });
                                }
                            },
                            {
                                text: 'Hizmet Atama',
                                iconStyle: 'fas fa-tasks',
                                action: function (data) {
                                    serviceAssignmentModal.open({ employeeId: data.record.id });
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
                                text: function (data) {
                                    return data.record.hasUser ? 'Kullanıcı Yönetimi' : 'Kullanıcı Oluştur';
                                },
                                iconStyle: function (data) {
                                    return data.record.hasUser ? 'fas fa-user-cog' : 'fas fa-user-plus';
                                },
                                action: function (data) {
                                    if (data.record.hasUser) {
                                        showUserManagementMenu(data.record);
                                    } else {
                                        createUserForEmployee(data.record.id);
                                    }
                                }
                            },
                            {
                                text: function (data) {
                                    return data.record.isActive ? 'Pasif Yap' : 'Aktif Yap';
                                },
                                iconStyle: function (data) {
                                    return data.record.isActive ? 'fas fa-eye-slash' : 'fas fa-eye';
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
                                iconStyle: 'fas fa-trash',
                                confirmMessage: function (data) {
                                    return 'Bu çalışanı silmek istediğinizden emin misiniz? Çalışan: ' + data.record.fullName;
                                },
                                action: function (data) {
                                    beroxAppy.employees.employee
                                        .delete(data.record.id)
                                        .then(function () {
                                            abp.notify.success('Çalışan başarıyla silindi');
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
                    title: 'Tip',
                    data: "employeeTypeDisplay",
                    render: function (data, type, row) {
                        var badgeClass = '';
                        switch (row.employeeType) {
                            case 0: badgeClass = 'bg-primary'; break;   // Staff
                            case 1: badgeClass = 'bg-info'; break;      // Secretary
                            case 2: badgeClass = 'bg-warning'; break;   // Manager
                            case 3: badgeClass = 'bg-secondary'; break; // Device
                        }
                        return '<span class="badge ' + badgeClass + '">' + data + '</span>';
                    }
                },
                {
                    title: 'Hizmet Cinsiyeti',
                    data: "serviceGenderDisplay",
                    render: function (data, type, row) {
                        var badgeClass = '';
                        switch (row.serviceGender) {
                            case 0: badgeClass = 'bg-secondary'; break; // Unisex
                            case 1: badgeClass = 'bg-primary'; break;   // Male
                            case 2: badgeClass = 'bg-danger'; break;    // Female
                        }
                        return '<span class="badge ' + badgeClass + '">' + data + '</span>';
                    }
                },
                {
                    title: 'Maaş',
                    data: "fixedSalary",
                    render: function (data) {
                        if (data > 0) {
                            return '<strong>₺' + data.toLocaleString('tr-TR', { minimumFractionDigits: 0 }) + '</strong>';
                        }
                        return '<span class="text-muted">-</span>';
                    }
                },
                {
                    title: 'Komisyon',
                    data: null,
                    orderable: false,
                    render: function (data, type, row) {
                        var html = '<div>';
                        if (row.serviceCommissionRate > 0) {
                            html += '<small class="badge bg-info me-1">H: %' + row.serviceCommissionRate + '</small>';
                        }
                        if (row.productCommissionRate > 0) {
                            html += '<small class="badge bg-warning me-1">Ü: %' + row.productCommissionRate + '</small>';
                        }
                        if (row.packageCommissionRate > 0) {
                            html += '<small class="badge bg-success me-1">P: %' + row.packageCommissionRate + '</small>';
                        }
                        if (html === '<div>') {
                            html += '<span class="text-muted">-</span>';
                        }
                        html += '</div>';
                        return html;
                    }
                },
                {
                    title: 'Kullanıcı',
                    data: "userStatus",
                    render: function (data, type, row) {
                        var badgeClass = '';
                        var icon = '';

                        switch (data) {
                            case 'Aktif':
                                badgeClass = 'bg-success';
                                icon = 'fas fa-user-check';
                                break;
                            case 'Pasif':
                                badgeClass = 'bg-warning';
                                icon = 'fas fa-user-times';
                                break;
                            case 'Kullanıcı Yok':
                                badgeClass = 'bg-secondary';
                                icon = 'fas fa-user-slash';
                                break;
                            default:
                                badgeClass = 'bg-danger';
                                icon = 'fas fa-exclamation-triangle';
                        }

                        return '<span class="badge ' + badgeClass + '"><i class="' + icon + ' me-1"></i>' + data + '</span>';
                    }
                },
                {
                    title: 'Durum',
                    data: "isActive",
                    render: function (data, type, row) {
                        var badge = '';
                        if (data) {
                            badge = '<span class="badge bg-success">Aktif</span>';
                        } else {
                            badge = '<span class="badge bg-danger">Pasif</span>';
                        }

                        // Online rezervasyon durumu
                        if (data && row.canTakeOnlineReservation) {
                            badge += '<br><small class="badge bg-info mt-1">Online Rez.</small>';
                        }

                        return badge;
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
        abp.notify.success('Çalışan başarıyla oluşturuldu');
    });

    editModal.onResult(function () {
        dataTable.ajax.reload();
        abp.notify.success('Çalışan başarıyla güncellendi');
    });

    workingHoursModal.onResult(function () {
        abp.notify.success('Çalışma saatleri güncellendi');
    });

    serviceAssignmentModal.onResult(function () {
        abp.notify.success('Hizmet atamaları güncellendi');
    });

    $('#NewEmployeeButton').click(function (e) {
        e.preventDefault();
        createModal.open();
    });

    // Filtre events
    $('#EmployeeTypeFilter, #ServiceGenderFilter, #IsActiveFilter, #HasUserFilter, #CanTakeOnlineReservationFilter').change(function () {
        dataTable.ajax.reload();
    });

    $('#FilterInput').on('input', debounce(function () {
        dataTable.ajax.reload();
    }, 500));

    $('#MinSalaryFilter, #MaxSalaryFilter').on('input', debounce(function () {
        dataTable.ajax.reload();
    }, 500));

    $('#ClearFiltersButton').click(function () {
        $('#FilterInput').val('');
        $('#EmployeeTypeFilter').val('');
        $('#ServiceGenderFilter').val('');
        $('#IsActiveFilter').val('');
        $('#HasUserFilter').val('');
        $('#CanTakeOnlineReservationFilter').val('');
        $('#MinSalaryFilter').val('');
        $('#MaxSalaryFilter').val('');
        dataTable.ajax.reload();
    });

    // Helper functions
    function createUserForEmployee(employeeId) {
        abp.message.inputDialog({
            title: 'Kullanıcı Oluştur',
            text: 'Çalışan için kullanıcı hesabı oluşturmak istediğinizden emin misiniz?',
            inputPlaceholder: 'Kullanıcı adı giriniz...',
            inputRequired: true
        }).then(function (result) {
            if (result.value) {
                abp.message.inputDialog({
                    title: 'Şifre Belirle',
                    text: 'Kullanıcı şifresini giriniz:',
                    inputType: 'password',
                    inputPlaceholder: 'Şifre (en az 6 karakter)',
                    inputRequired: true
                }).then(function (passwordResult) {
                    if (passwordResult.value && passwordResult.value.length >= 6) {
                        beroxAppy.employees.employee
                            .createUser(employeeId, result.value, passwordResult.value)
                            .then(function () {
                                abp.notify.success('Kullanıcı başarıyla oluşturuldu');
                                dataTable.ajax.reload();
                            });
                    } else {
                        abp.message.error('Şifre en az 6 karakter olmalıdır!');
                    }
                });
            }
        });
    }

    function showUserManagementMenu(employee) {
        var items = [
            {
                text: employee.userStatus === 'Aktif' ? 'Kullanıcıyı Pasif Yap' : 'Kullanıcıyı Aktif Yap',
                action: function () {
                    var newStatus = employee.userStatus !== 'Aktif';
                    beroxAppy.employees.employee
                        .setUserActiveStatus(employee.id, newStatus)
                        .then(function () {
                            abp.notify.success('Kullanıcı durumu güncellendi');
                            dataTable.ajax.reload();
                        });
                }
            },
            {
                text: 'Şifre Sıfırla',
                action: function () {
                    abp.message.inputDialog({
                        title: 'Yeni Şifre',
                        text: 'Yeni şifreyi giriniz:',
                        inputType: 'password',
                        inputPlaceholder: 'Yeni şifre (en az 6 karakter)',
                        inputRequired: true
                    }).then(function (result) {
                        if (result.value && result.value.length >= 6) {
                            beroxAppy.employees.employee
                                .resetUserPassword(employee.id, result.value)
                                .then(function () {
                                    abp.notify.success('Şifre başarıyla sıfırlandı');
                                });
                        } else {
                            abp.message.error('Şifre en az 6 karakter olmalıdır!');
                        }
                    });
                }
            }
        ];

        // Context menu göster (basit implementasyon)
        var menu = items.map(item => `<button class="dropdown-item" onclick="(${item.action})()">${item.text}</button>`).join('');
        abp.message.info('Kullanıcı yönetimi seçenekleri için sağ tık menüsü yakında eklenecek...');
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