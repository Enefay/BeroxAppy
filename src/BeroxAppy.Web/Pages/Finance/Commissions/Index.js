$(function () {
    var l = abp.localization.getResource('BeroxAppy');
    var financeService = beroxAppy.services.financeAppService;
    var selectedEmployees = [];

    // Sayfa yüklendikinde filtreleri uygula
    applyFilters();

    // Yenile butonu
    $('#RefreshButton').on('click', function () {
        refreshData();
    });

    // Filtreleme
    $('#ApplyFiltersButton').on('click', function () {
        applyFilters();
    });

    // Enter tuşu ile filtreleme
    $('#EmployeeNameFilter').on('keypress', function (e) {
        if (e.which === 13) {
            applyFilters();
        }
    });

    // Tümünü seç checkbox'ları
    $('#SelectAllCheckbox, #HeaderSelectAll').on('change', function () {
        var isChecked = $(this).is(':checked');
        $('.commission-checkbox:visible').prop('checked', isChecked);

        // Diğer select all checkbox'ını da senkronize et
        if (this.id === 'SelectAllCheckbox') {
            $('#HeaderSelectAll').prop('checked', isChecked);
        } else {
            $('#SelectAllCheckbox').prop('checked', isChecked);
        }

        updateSelectedEmployees();
    });

    // Tekil checkbox seçimi
    $(document).on('change', '.commission-checkbox', function () {
        updateSelectedEmployees();

        // Tüm checkbox'lar seçiliyse select all'ı da seç
        var totalVisible = $('.commission-checkbox:visible').length;
        var selectedVisible = $('.commission-checkbox:visible:checked').length;

        $('#SelectAllCheckbox, #HeaderSelectAll').prop('checked', totalVisible > 0 && selectedVisible === totalVisible);
    });

    // Tekil ödeme butonu
    $(document).on('click', '.pay-single-btn', function () {
        var employeeId = $(this).data('employee-id');
        var employeeName = $(this).data('employee-name');
        var amount = $(this).data('amount');

        selectedEmployees = [{
            employeeId: employeeId,
            employeeName: employeeName,
            amount: amount
        }];

        showPaymentModal();
    });

    // Seçilenleri öde butonu
    $('#PaySelectedButton').on('click', function () {
        if (selectedEmployees.length === 0) {
            abp.notify.warn('Lütfen ödeme yapılacak çalışanları seçin.');
            return;
        }
        showPaymentModal();
    });

    // Detay görüntüleme
    $(document).on('click', '.view-details-btn', function () {
        var employeeId = $(this).data('employee-id');
        showEmployeeDetail(employeeId);
    });

    // Ödeme modalı kaydet
    $('#PayCommissionSaveButton').on('click', function () {
        saveCommissionPayment();
    });

    // Modal kapanma olayı
    $('#PayCommissionModal').on('hidden.bs.modal', function () {
        resetPaymentModal();
    });

    // Ödeme tutarı değiştiğinde dağılımı hesapla
    $('#PaymentAmount').on('input', function () {
        calculateDistribution();
    });

    // Fonksiyonlar
    function applyFilters() {
        var showOnlyWithCommissions = $('#ShowOnlyWithCommissions').is(':checked');
        var minAmount = parseFloat($('#MinAmountFilter').val()) || 0;
        var nameFilter = $('#EmployeeNameFilter').val().toLowerCase();

        $('.commission-row').each(function () {
            var $row = $(this);
            var employeeName = $row.data('employee-name').toLowerCase();
            var amount = parseFloat($row.data('amount')) || 0;
            var hasCommission = $row.hasClass('has-commission');

            var showRow = true;

            // Sadece komisyonu olanlar filtresi
            if (showOnlyWithCommissions && !hasCommission) {
                showRow = false;
            }

            // Minimum tutar filtresi
            if (amount < minAmount) {
                showRow = false;
            }

            // İsim filtresi
            if (nameFilter && !employeeName.includes(nameFilter)) {
                showRow = false;
            }

            $row.toggle(showRow);
        });

        // Seçimleri güncelle
        updateSelectedEmployees();
    }

    function updateSelectedEmployees() {
        selectedEmployees = [];

        $('.commission-checkbox:checked').each(function () {
            var $checkbox = $(this);
            var $row = $checkbox.closest('.commission-row');

            selectedEmployees.push({
                employeeId: $checkbox.val(),
                employeeName: $row.data('employee-name'),
                amount: parseFloat($row.data('amount')) || 0
            });
        });

        // Seçilenleri öde butonunu güncelle
        $('#PaySelectedButton').prop('disabled', selectedEmployees.length === 0);

        if (selectedEmployees.length > 0) {
            var totalAmount = selectedEmployees.reduce((sum, emp) => sum + emp.amount, 0);
            $('#PaySelectedButton').html(
                `<i class="fas fa-money-bill me-1"></i>Seçilenleri Öde (${selectedEmployees.length} - ₺${totalAmount.toLocaleString('tr-TR', { minimumFractionDigits: 2 })})`
            );
        } else {
            $('#PaySelectedButton').html('<i class="fas fa-money-bill me-1"></i>Seçilenleri Öde');
        }
    }

    function showPaymentModal() {
        if (selectedEmployees.length === 0) return;

        var totalAmount = selectedEmployees.reduce((sum, emp) => {
            return sum + Number(
                String(emp.amount).replace('.', '').replace(',', '.')
            );
        }, 0);

        var isMultiple = selectedEmployees.length > 1;

        console.log("totalAmount", totalAmount)

        // Modal başlığı
        $('#ModalTitle').text(isMultiple ?
            `Toplu Komisyon Ödemesi (${selectedEmployees.length} Çalışan)` :
            'Komisyon Ödemesi'
        );

        // Seçili çalışanları göster
        var employeeListHtml = '';
        selectedEmployees.forEach(function (emp) {
            employeeListHtml += `
                <div class="d-flex justify-content-between align-items-center mb-2 p-2 bg-white rounded border">
                    <div>
                        <strong>${emp.employeeName}</strong>
                    </div>
                    <div>
                        <span class="badge bg-success">₺${emp.amount.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</span>
                    </div>
                </div>
            `;
        });
        $('#SelectedEmployeesList').html(employeeListHtml);

        // Tutarları ayarla
        //$('#TotalCommissionAmount').val('₺' + totalAmount.toLocaleString('tr-TR', { minimumFractionDigits: 2 }));

        $('#TotalCommissionAmount').val(totalAmount.toLocaleString('tr-TR', {
            style: 'currency',
            currency: 'TRY',
            minimumFractionDigits: 2
        }).replace('₺', '').trim());

        $('#PaymentAmount').val(totalAmount).attr('max', totalAmount);

        // Dağılım tablosunu göster/gizle
        $('#PaymentDistribution').toggle(isMultiple);

        // Buton metnini ayarla
        $('#PayButtonText').text(isMultiple ? 'Toplu Ödeme Yap' : 'Ödeme Yap');

        // Dağılımı hesapla
        calculateDistribution();

        $('#PayCommissionModal').modal('show');
    }

    function calculateDistribution() {
        if (selectedEmployees.length <= 1) return;

        var paymentAmount = parseFloat($('#PaymentAmount').val()) || 0;

        var totalCommission = selectedEmployees.reduce((sum, emp) => sum + emp.amount, 0);

        if (totalCommission === 0) return;

        var distributionHtml = '';
        var distributedTotal = 0;

        selectedEmployees.forEach(function (emp, index) {
            var ratio = emp.amount / totalCommission;
            var employeePayment = index === selectedEmployees.length - 1 ?
                paymentAmount - distributedTotal : // Son çalışana kalan tutarı ver
                Math.round(paymentAmount * ratio * 100) / 100;

            distributedTotal += employeePayment;

            distributionHtml += `
                <tr>
                    <td>${emp.employeeName}</td>
                    <td>₺${emp.amount.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</td>
                    <td><strong>₺${employeePayment.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</strong></td>
                </tr>
            `;
        });

        $('#DistributionTableBody').html(distributionHtml);
    }

    function saveCommissionPayment() {

        var paymentAmount = parseFloat($('#PaymentAmount').val().replace(',', '.'));
        var paymentMethod = parseInt($('#PaymentMethod').val());

        // Validasyon
        if (isNaN(paymentAmount)) {
            abp.notify.error('Lütfen geçerli bir ödeme tutarı girin');
            return;
        }
        var paymentMethodStr = $('#PaymentMethod').val();
        if (!paymentMethodStr) {
            abp.notify.error('Lütfen ödeme yöntemi seçin.');
            return;
        }
     

        var requestData = {
            employeeCommissions: selectedEmployees.map(function (emp) {
                return {
                    employeeId: emp.employeeId,
                    employeeName: emp.employeeName,
                    amount: parseFloat(emp.amount.toString().replace(',', '.'))
                };
            }),
            paymentMethod: paymentMethod,
            paymentAmount: parseFloat(paymentAmount)
        };


        abp.ui.setBusy($('#PayCommissionModal'));

        console.log("Gönderilen veri:", requestData);

        abp.ajax({
            url: '/Finance/Commissions/Index?handler=PayCommission',
            type: 'POST',
            data: JSON.stringify(requestData),
            contentType: 'application/json',
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            }
        }).done(function (result) {
            if (result.success) {
                abp.notify.success(result.message);
                $('#PayCommissionModal').modal('hide');
                refreshData();
            } else {
                abp.notify.error(result.message);
            }
        }).fail(function (error) {
            abp.notify.error('Komisyon ödemesi sırasında bir hata oluştu.');
        }).always(function () {
            abp.ui.clearBusy($('#PayCommissionModal'));
        });
    }

    function showEmployeeDetail(employeeId) {
        abp.ui.setBusy($('#EmployeeDetailModal'));

        abp.ajax({
            url: '/Finance/Commissions/Index?handler=EmployeeDetail',
            type: 'GET',
            data: { employeeId: employeeId }
        }).done(function (result) {
            if (result.success) {
                var data = result.data;
                var performance = data.performance;
                var recentPayments = data.recentPayments;

                $('#DetailEmployeeName').text(performance.employeeName);

                var contentHtml = `
                    <div class="row mb-4">
                        <div class="col-md-12">
                            <h6 class="fw-bold mb-3">Performans Özeti (Son 3 Ay)</h6>
                            <div class="row">
                                <div class="col-md-3">
                                    <div class="text-center p-3 bg-light rounded">
                                        <h5 class="text-success mb-1">₺${performance.totalCommissionEarned.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</h5>
                                        <small class="text-muted">Kazanılan Komisyon</small>
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="text-center p-3 bg-light rounded">
                                        <h5 class="text-info mb-1">₺${performance.totalCommissionPaid.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</h5>
                                        <small class="text-muted">Ödenen Komisyon</small>
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="text-center p-3 bg-light rounded">
                                        <h5 class="text-warning mb-1">₺${(performance.totalCommissionEarned - performance.totalCommissionPaid).toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</h5>
                                        <small class="text-muted">Kalan Komisyon</small>
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="text-center p-3 bg-light rounded">
                                        <h5 class="text-primary mb-1">${performance.serviceCount}</h5>
                                        <small class="text-muted">Toplam Hizmet</small>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-12">
                            <h6 class="fw-bold mb-3">Son Komisyon Ödemeleri</h6>
                            ${recentPayments.length > 0 ? `
                                <div class="table-responsive">
                                    <table class="table table-sm">
                                        <thead>
                                            <tr>
                                                <th>Tarih</th>
                                                <th>Tutar</th>
                                                <th>Yöntem</th>
                                                <th>Not</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            ${recentPayments.map(payment => `
                                                <tr>
                                                    <td>${new Date(payment.paymentDate).toLocaleDateString('tr-TR')}</td>
                                                    <td><strong>₺${payment.commissionAmount.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</strong></td>
                                                    <td><span class="badge bg-secondary">${payment.paymentMethodDisplay}</span></td>
                                                    <td><small>${payment.note || '-'}</small></td>
                                                </tr>
                                            `).join('')}
                                        </tbody>
                                    </table>
                                </div>
                            ` : `
                                <div class="text-center text-muted py-3">
                                    <i class="fas fa-info-circle fa-2x mb-2"></i>
                                    <p class="mb-0">Henüz komisyon ödemesi yapılmamış</p>
                                </div>
                            `}
                        </div>
                    </div>
                `;

                $('#EmployeeDetailContent').html(contentHtml);
                $('#EmployeeDetailModal').modal('show');
            } else {
                abp.notify.error(result.message);
            }
        }).fail(function () {
            abp.notify.error('Çalışan detayları yüklenirken hata oluştu.');
        }).always(function () {
            abp.ui.clearBusy($('#EmployeeDetailModal'));
        });
    }

    function refreshData() {
        abp.ui.setBusy($('#CommissionsTable'));

        abp.ajax({
            url: '/Finance/Commissions/Index?handler=RefreshData',
            type: 'POST',
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            }
        }).done(function (result) {
            if (result.success) {
                abp.notify.success(result.message);
                window.location.reload();
            } else {
                abp.notify.error(result.message);
            }
        }).fail(function () {
            abp.notify.error('Veriler yenilenirken hata oluştu.');
        }).always(function () {
            abp.ui.clearBusy($('#CommissionsTable'));
        });
    }

    function resetPaymentModal() {
        $('#PayCommissionForm')[0].reset();
        $('#SelectedEmployeesList').empty();
        $('#DistributionTableBody').empty();
        $('#PaymentAmount').removeAttr('max');
        selectedEmployees = [];
    }

    // Klavye kısayolları
    $(document).on('keydown', function (e) {
        // Ctrl+A: Tümünü seç
        if (e.ctrlKey && e.key === 'a' && !$(e.target).is('input, textarea')) {
            e.preventDefault();
            $('#SelectAllCheckbox').prop('checked', true).trigger('change');
        }

        // Ctrl+P: Seçilenleri öde
        if (e.ctrlKey && e.key === 'p' && selectedEmployees.length > 0) {
            e.preventDefault();
            $('#PaySelectedButton').click();
        }

        // F5: Yenile
        if (e.key === 'F5') {
            e.preventDefault();
            refreshData();
        }
    });

    // Tooltip'leri aktifleştir
    $('[data-bs-toggle="tooltip"]').tooltip();

    // Otomatik yenileme (isteğe bağlı - 5 dakikada bir)
    /*
    setInterval(function () {
        if (!$('#PayCommissionModal').hasClass('show') && !$('#EmployeeDetailModal').hasClass('show')) {
            refreshData();
        }
    }, 300000); // 5 dakika
    */
});