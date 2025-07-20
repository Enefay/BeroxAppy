$(function () {
    var l = abp.localization.getResource('BeroxAppy');
    var $modal = $('#ServiceAssignmentModal');

    function reloadServicesSection() {
        var selectedCategoryId = $('#categoryFilter').val();
        var searchText = $('#serviceSearch').val();
        var employeeId = $('#EmployeeId').val();
        if (!employeeId) {
            return;
        }
        abp.ui.setBusy($modal);

        beroxAppy.employees.employee.getEmployeeServices(employeeId)
            .then(function (assignedResult) {
                $('#assignedServices').empty();
                if (assignedResult.items && assignedResult.items.length > 0) {
                    assignedResult.items.forEach(function (service) {
                        var assignedHtml = `
                            <div class="card mb-2 assigned-service" data-service-id="${service.serviceId}">
                                <div class="card-body p-2">
                                    <div class="d-flex justify-content-between align-items-center">
                                        <div class="flex-grow-1">
                                            <h6 class="card-title mb-1">${service.serviceTitle}</h6>
                                            <small class="text-muted">
                                                ${service.serviceCategoryName} |
                                                ₺${parseFloat(service.servicePrice).toLocaleString('tr-TR', { minimumFractionDigits: 2 })} |
                                                ${service.serviceDuration} dk
                                            </small>
                                        </div>
                                        <button type="button" class="btn btn-sm btn-outline-danger unassign-service ms-2"
                                                data-service-id="${service.serviceId}"
                                                data-service-title="${service.serviceTitle}">
                                            <i class="fas fa-times"></i>
                                        </button>
                                    </div>
                                </div>
                            </div>
                        `;
                        $('#assignedServices').append(assignedHtml);
                    });
                } else {
                    $('#assignedServices').append(`
                        <div class="text-center text-muted py-5" id="noAssignedServices">
                            <i class="fas fa-info-circle fa-2x mb-2"></i>
                            <p>Henüz hizmet atanmamış</p>
                        </div>
                    `);
                }

                var assignedServiceIds = assignedResult.items.map(function (service) {
                    return service.serviceId.toString();
                });

                return beroxAppy.services.service.getList({
                    filter: searchText,
                    categoryId: selectedCategoryId || null,
                    isActive: true,
                    maxResultCount: 100
                }).then(function (result) {
                    $('#availableServices').empty();

                    result.items.forEach(function (service) {
                        if (assignedServiceIds.indexOf(service.id.toString()) === -1) {
                            var serviceHtml = `
                                <div class="card mb-2 available-service"
                                     data-service-id="${service.id}"
                                     data-category-id="${service.categoryId}"
                                     data-service-title="${service.title}">
                                    <div class="card-body p-2">
                                        <div class="d-flex justify-content-between align-items-center">
                                            <div class="flex-grow-1">
                                                <h6 class="card-title mb-1">${service.title}</h6>
                                                <small class="text-muted">
                                                    ${service.categoryName} |
                                                    ₺${parseFloat(service.price).toLocaleString('tr-TR', { minimumFractionDigits: 2 })} |
                                                    ${service.durationMinutes} dk
                                                </small>
                                            </div>
                                            <button type="button" class="btn btn-sm btn-outline-success assign-service ms-2"
                                                    data-service-id="${service.id}"
                                                    data-service-title="${service.title}"
                                                    data-category-name="${service.categoryName}"
                                                    data-service-price="${service.price}"
                                                    data-service-duration="${service.durationMinutes}">
                                                <i class="fas fa-plus"></i>
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            `;
                            $('#availableServices').append(serviceHtml);
                        }
                    });

                    if ($('#availableServices .available-service').length === 0) {
                        $('#availableServices').append(`
                            <div class="text-center text-muted py-5">
                                <i class="fas fa-info-circle fa-2x mb-2"></i>
                                <p>Filtrenize uyan atanabilir hizmet yok</p>
                            </div>
                        `);
                    }
                });
            })
            .always(function () {
                abp.ui.clearBusy($modal);
            });
    }

    $(document).on('click', '.assign-service', function () {
        var $btn = $(this);
        var employeeId = $('#EmployeeId').val();
        var serviceId = $btn.data('service-id');
        var serviceTitle = $btn.data('service-title');

        if (!employeeId) {
            abp.notify.error('Employee ID bulunamadı!');
            return;
        }

        abp.message.confirm(
            serviceTitle + ' hizmetini atamak istediğinizden emin misiniz?',
            'Hizmet Atama'
        ).then(function (confirmed) {
            if (confirmed) {
                abp.ui.setBusy($modal);

                beroxAppy.employees.employee.assignService(employeeId, serviceId)
                    .then(function () {
                        abp.notify.success('Hizmet başarıyla atandı.');
                        reloadServicesSection();
                    })
                    .catch(function (error) {
                        abp.notify.error(error.message || 'Bir hata oluştu!');
                    })
                    .always(function () {
                        abp.ui.clearBusy($modal);
                    });
            }
        });
    });

    $(document).on('click', '.unassign-service', function () {
        var $btn = $(this);
        var employeeId = $('#EmployeeId').val();
        var serviceId = $btn.data('service-id');
        var serviceTitle = $btn.data('service-title');

        if (!employeeId) {
            abp.notify.error('Employee ID bulunamadı!');
            return;
        }

        abp.message.confirm(
            serviceTitle + ' hizmet atamasını kaldırmak istediğinizden emin misiniz?',
            'Hizmet Atama Kaldırma'
        ).then(function (confirmed) {
            if (confirmed) {
                abp.ui.setBusy($modal);

                beroxAppy.employees.employee.unassignService(employeeId, serviceId)
                    .then(function () {
                        abp.notify.success('Hizmet ataması kaldırıldı.');
                        reloadServicesSection();
                    })
                    .catch(function (error) {
                        abp.notify.error(error.message || 'Bir hata oluştu!');
                    })
                    .always(function () {
                        abp.ui.clearBusy($modal);
                    });
            }
        });
    });

    $(document).on('change', '#categoryFilter', reloadServicesSection);

    $(document).on('keyup', '#serviceSearch', reloadServicesSection);

    $(document).on('shown.bs.modal', '#ServiceAssignmentModal', function () {
        reloadServicesSection();
    });

    setTimeout(reloadServicesSection, 500);
});
