$(function () {
    var l = abp.localization.getResource('BeroxAppy');
    var $modal = $('#ServiceAssignmentModal');

    // Hizmet atama
    $(document).on('click', '.assign-service', function () {
        var $btn = $(this);
        var employeeId = $('#EmployeeId').val(); // Her tıklamada yeniden al
        var serviceId = $btn.data('service-id');
        var serviceTitle = $btn.data('service-title');
        var categoryName = $btn.data('category-name');
        var servicePrice = $btn.data('service-price');
        var serviceDuration = $btn.data('service-duration');

        console.log("Assign Service - EmployeeId:", employeeId, "ServiceId:", serviceId);

        if (!employeeId || employeeId === '') {
            abp.notify.error('Employee ID bulunamadı!');
            return;
        }

        abp.message.confirm(
            serviceTitle + ' hizmetini atamak istediğinizden emin misiniz?',
            'Hizmet Atama'
        ).then(function (confirmed) {
            if (confirmed) {
                abp.ui.setBusy($modal);

                // ABP proxy kullanımı
                beroxAppy.employees.employee.assignService(employeeId, serviceId)
                    .then(function (result) {
                        abp.notify.success('Hizmet başarıyla atandı.');
                        moveServiceToAssigned($btn.closest('.available-service'), {
                            serviceId: serviceId,
                            serviceTitle: serviceTitle,
                            categoryName: categoryName,
                            servicePrice: servicePrice,
                            serviceDuration: serviceDuration
                        });
                    })
                    .catch(function (error) {
                        var errorMessage = 'Bir hata oluştu!';
                        if (error.message) {
                            errorMessage = error.message;
                        }
                        abp.notify.error(errorMessage);
                    })
                    .finally(function () {
                        abp.ui.clearBusy($modal);
                    });
            }
        });
    });

    // Hizmet atama kaldırma
    $(document).on('click', '.unassign-service', function () {
        var $btn = $(this);
        var employeeId = $('#EmployeeId').val(); // Her tıklamada yeniden al
        var serviceId = $btn.data('service-id');
        var serviceTitle = $btn.data('service-title');

        console.log("Unassign Service - EmployeeId:", employeeId, "ServiceId:", serviceId);

        if (!employeeId || employeeId === '') {
            abp.notify.error('Employee ID bulunamadı!');
            return;
        }

        abp.message.confirm(
            serviceTitle + ' hizmet atamasını kaldırmak istediğinizden emin misiniz?',
            'Hizmet Atama Kaldırma'
        ).then(function (confirmed) {
            if (confirmed) {
                abp.ui.setBusy($modal);

                // ABP proxy kullanımı
                beroxAppy.employees.employee.unassignService(employeeId, serviceId)
                    .then(function (result) {
                        abp.notify.success('Hizmet ataması kaldırıldı.');
                        moveServiceToAvailable($btn.closest('.assigned-service'), serviceId);
                    })
                    .catch(function (error) {
                        var errorMessage = 'Bir hata oluştu!';
                        if (error.message) {
                            errorMessage = error.message;
                        }
                        abp.notify.error(errorMessage);
                    })
                    .finally(function () {
                        abp.ui.clearBusy($modal);
                    });
            }
        });
    });

    // Kategori filtresi
    $('#categoryFilter').on('change', function () {
        filterServices();
    });

    // Arama filtresi
    $('#serviceSearch').on('keyup', function () {
        filterServices();
    });

    // Servisleri filtrele
    function filterServices() {
        var selectedCategoryId = $('#categoryFilter').val();
        var searchText = $('#serviceSearch').val().toLowerCase().trim();

        $('.available-service').each(function () {
            var $service = $(this);
            var categoryId = $service.data('category-id');
            var serviceTitle = $service.data('service-title');

            // String olarak gelen service title'ı lowercase yap
            var serviceTitleLower = (serviceTitle || '').toString().toLowerCase();

            var categoryMatch = !selectedCategoryId || selectedCategoryId === '' || categoryId.toString() === selectedCategoryId;
            var textMatch = !searchText || searchText === '' || serviceTitleLower.indexOf(searchText) !== -1;

            if (categoryMatch && textMatch) {
                $service.show();
            } else {
                $service.hide();
            }
        });
    }

    // Servisi atanmış listesine taşı
    function moveServiceToAssigned($serviceCard, serviceData) {
        // Boş mesajı kaldır
        $('#noAssignedServices').remove();

        // Mevcut kartı kaldır
        $serviceCard.fadeOut(300, function () {
            $(this).remove();
        });

        // Yeni kart oluştur
        var assignedServiceHtml = `
            <div class="card mb-2 assigned-service" data-service-id="${serviceData.serviceId}" style="display: none;">
                <div class="card-body p-2">
                    <div class="d-flex justify-content-between align-items-center">
                        <div class="flex-grow-1">
                            <h6 class="card-title mb-1">${serviceData.serviceTitle}</h6>
                            <small class="text-muted">
                                ${serviceData.categoryName} | 
                                ₺${parseFloat(serviceData.servicePrice).toLocaleString('tr-TR', { minimumFractionDigits: 2 })} | 
                                ${serviceData.serviceDuration} dk
                            </small>
                        </div>
                        <button type="button" class="btn btn-sm btn-outline-danger unassign-service ms-2" 
                                data-service-id="${serviceData.serviceId}" 
                                data-service-title="${serviceData.serviceTitle}">
                            <i class="fas fa-times"></i>
                        </button>
                    </div>
                </div>
            </div>
        `;

        $('#assignedServices').append(assignedServiceHtml);
        $('#assignedServices .assigned-service:last').fadeIn(300);
    }

    // Servisi mevcut listesine taşı
    function moveServiceToAvailable($serviceCard, serviceId) {
        // Atanmış kartı kaldır
        $serviceCard.fadeOut(300, function () {
            $(this).remove();

            // Eğer atanmış hizmet kalmadıysa boş mesaj göster
            if ($('#assignedServices .assigned-service').length === 0) {
                var emptyHtml = `
                    <div class="text-center text-muted py-5" id="noAssignedServices">
                        <i class="fas fa-info-circle fa-2x mb-2"></i>
                        <p>Henüz hizmet atanmamış</p>
                    </div>
                `;
                $('#assignedServices').append(emptyHtml);
            }
        });

        // Servisi available listesine geri ekle
        loadAvailableServices();
    }

    // Available servisleri yeniden yükle
    function loadAvailableServices() {
        var employeeId = $('#EmployeeId').val();

        if (!employeeId || employeeId === '') {
            console.error('EmployeeId bulunamadı!');
            return;
        }

        beroxAppy.services.service.getActiveList()
            .then(function (result) {
                // Atanmış hizmet ID'lerini al
                var assignedServiceIds = [];
                $('.assigned-service').each(function () {
                    assignedServiceIds.push($(this).data('service-id').toString());
                });

                // Available services alanını temizle
                $('#availableServices').empty();

                // Atanmamış hizmetleri filtrele ve ekle
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

                // Filtreleri yeniden uygula
                filterServices();
            })
            .catch(function (error) {
                console.error('Available services yüklenirken hata:', error);
            });
    }
});