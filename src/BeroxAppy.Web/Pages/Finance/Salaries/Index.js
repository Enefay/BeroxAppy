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
        $('.salary-checkbox:visible').prop('checked', isChecked);

        // Diğer select all checkbox'ını da senkronize et
        if (this.id === 'SelectAllCheckbox') {
            $('#HeaderSelectAll').prop('checked', isChecked);
        } else {
            $('#SelectAllCheckbox').prop('checked', isChecked);
        }

        updateSelectedEmployees();
    });

    // Tekil checkbox seçimi
    $(document).on('change', '.salary-checkbox', function () {
        updateSelectedEmployees();

        // Tüm checkbox'lar seçiliyse select all'ı da seç
        var totalVisible = $('.salary-checkbox:visible').length;
        var selectedVisible = $('.salary-checkbox:visible:checked').length;

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
        showEmployeeSalaryDetail(employeeId);
    });

    // Ödeme modalı kaydet
    $('#PaySalarySaveButton').on('click', function () {
        saveSalaryPayment();
    });

    // Modal kapanma olayı
    $('#PaySalaryModal').on('hidden.bs.modal', function () {
        resetPaymentModal();
    });

    // Ödeme tutarı değiştiğinde dağılımı hesapla
    $('#PaymentAmount').on('input', function () {
        calculateDistribution();
    });

    // Fonksiyonlar
    function applyFilters() {
        var showOnlyDue = $('#ShowOnlyDue').is(':checked');
        var periodFilter = $('#SalaryPeriodFilter').val();
        var nameFilter = $('#EmployeeNameFilter').val().toLowerCase();

        $('.salary-row').each(function () {
            var $row = $(this);
            var employeeName = $row.data('employee-name').toLowerCase();
            var isDue = $row.hasClass('is-due');
            var period = $row.data('period').toString();

            var showRow = true;

            // Sadece vadesi gelenler filtresi
            if (showOnlyDue && !isDue) {
                showRow = false;
            }

            // Dönem filtresi
            if (periodFilter && period !== periodFilter) {
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

        $('.salary-checkbox:checked').each(function () {
            var $checkbox = $(this);
            var $row = $checkbox.closest('.salary-row');

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

        var totalAmount = selectedEmployees.reduce((sum, emp) => sum + emp.amount, 0);
        var isMultiple = selectedEmployees.length > 1;

        // Modal başlığı
        $('#ModalTitle').text(isMultiple ?
            `Toplu Maaş Ödemesi (${selectedEmployees.length} Çalışan)` :
            'Maaş Ödemesi'
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
        $('#TotalSalaryAmount').val('₺' + totalAmount.toLocaleString('tr-TR', { minimumFractionDigits: 2 }));
        $('#PaymentAmount').val(totalAmount).attr('max', totalAmount);

        // Dağılım tablosunu göster/gizle
        $('#PaymentDistribution').toggle(isMultiple);

        // Buton metnini ayarla
        $('#PayButtonText').text(isMultiple ? 'Toplu Ödeme Yap' : 'Ödeme Yap');

        // Dağılımı hesapla
        calculateDistribution();

        $('#PaySalaryModal').modal('show');
    }

    function calculateDistribution() {
        if (selectedEmployees.length <= 1) return;

        var paymentAmount = parseFloat($('#PaymentAmount').val()) || 0;
        var totalSalary = selectedEmployees.reduce((sum, emp) => sum + emp.amount, 0);

        if (totalSalary === 0) return;

        var distributionHtml = '';
        var distributedTotal = 0;

        selectedEmployees.forEach(function (emp, index) {
            var ratio = emp.amount / totalSalary;
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

    function saveSalaryPayment() {
        var form = $('#PaySalaryForm');

        if (!form[0].checkValidity()) {
            form[0].reportValidity();
            return;
        }

        var paymentAmount = parseFloat($('#PaymentAmount').val());
        var paymentMethod = parseInt($('#PaymentMethod').val());
        var note = $('#PaymentNote').val();

        if (paymentAmount <= 0 || isNaN(paymentMethod)) {
            abp.notify.error('Lütfen tüm gerekli alanları doldurun.');
            return;
        }

        // Ödeme dağılımını hesapla
        var totalSalary = selectedEmployees.reduce((sum, emp) => sum + emp.amount, 0);
        var employeeSalaries = [];
        var distributedTotal = 0;

        selectedEmployees.forEach(function (emp, index) {
            var ratio = emp.amount / totalSalary;
            var employeePayment = index === selectedEmployees.length - 1 ?
                paymentAmount - distributedTotal :
                Math.round(paymentAmount * ratio * 100) / 100;

            distributedTotal += employeePayment;

            employeeSalaries.push({
                employeeId: emp.employeeId,
                employeeName: emp.employeeName,
                amount: employeePayment
            });
        });

        var requestData = {
            employeeSalaries: employeeSalaries,
            paymentMethod: paymentMethod,
            note: note
        };

        abp.ui.setBusy($('#PaySalaryModal'));

        abp.ajax({
            url: '/Finance/Salaries/Index?handler=PaySalary',
            type: 'POST',
            data: JSON.stringify(requestData),
            contentType: 'application/json',
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            }
        }).done(function (result) {
            if (result.success) {
                abp.notify.success(result.message);
                $('#PaySalaryModal').modal('hide');
                refreshData();
            } else {
                abp.notify.error(result.message);
            }
        }).fail(function (error) {
            abp.notify.error('Maaş ödemesi sırasında bir hata oluştu.');
        }).always(function () {
            abp.ui.clearBusy($('#PaySalaryModal'));
        });
    }

    function showEmployeeSalaryDetail(employeeId) {
        abp.ui.setBusy($('#EmployeeSalaryDetailModal'));

        abp.ajax({
            url: '/Finance/Salaries/Index?handler=EmployeeSalaryDetail',
            type: 'GET',
            data: { employeeId: employeeId }
        }).done(function (result) {
            if (result.success) {
                var data = result.data;

                $('#DetailEmployeeName').text(data.employeeName);

                var contentHtml = `
                    <div class="row mb-4">
                        <div class="col-md-12">
                            <h6 class="fw-bold mb-3">Maaş Özeti (Son 6 Ay)</h6>
                            <div class="row">
                                <div class="col-md-3">
                                    <div class="text-center p-3 bg-light rounded">
                                        <h5 class="text-success mb-1">₺${data.totalSalaryPaid.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</h5>
                                        <small class="text-muted">Ödenen Maaş</small>
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="text-center p-3 bg-light rounded">
                                        <h5 class="text-info mb-1">₺${data.totalCommissionPaid.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</h5>
                                        <small class="text-muted">Ödenen Komisyon</small>
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="text-center p-3 bg-light rounded">
                                        <h5 class="text-warning mb-1">₺${data.totalBonusPaid.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</h5>
                                        <small class="text-muted">Bonus/Kesinti</small>
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="text-center p-3 bg-light rounded">
                                        <h5 class="text-primary mb-1">${data.paymentCount}</h5>
                                        <small class="text-muted">Ödeme Sayısı</small>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-12">
                            <h6 class="fw-bold mb-3">Son Maaş Ödemeleri</h6>
                            ${data.paymentHistory && data.paymentHistory.length > 0 ? `
                                <div class="table-responsive">
                                    <table class="table table-sm">
                                        <thead>
                                            <tr>
                                                <th>Dönem</th>
                                                <th>Ödeme Tarihi</th>
                                                <th>Tutar</th>
                                                <th>Yöntem</th>
                                                <th>Not</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            ${data.paymentHistory.map(payment => `
                                                <tr>
                                                    <td>
                                                        <small>
                                                            ${new Date(payment.periodStart).toLocaleDateString('tr-TR')} - 
                                                            ${new Date(payment.periodEnd).toLocaleDateString('tr-TR')}
                                                        </small>
                                                    </td>
                                                    <td>${new Date(payment.paymentDate).toLocaleDateString('tr-TR')}</td>
                                                    <td><strong>₺${payment.amount.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</strong></td>
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
                                    <p class="mb-0">Henüz maaş ödemesi yapılmamış</p>
                                </div>
                            `}
                        </div>
                    </div>
                `;

                $('#EmployeeSalaryDetailContent').html(contentHtml);
                $('#EmployeeSalaryDetailModal').modal('show');
            } else {
                abp.notify.error(result.message);
            }
        }).fail(function () {
            abp.notify.error('Çalışan detayları yüklenirken hata oluştu.');
        }).always(function () {
            abp.ui.clearBusy($('#EmployeeSalaryDetailModal'));
        });
    }

    function refreshData() {
        abp.ui.setBusy($('#SalariesTable'));

        abp.ajax({
            url: '/Finance/Salaries/Index?handler=RefreshData',
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
            abp.ui.clearBusy($('#SalariesTable'));
        });
    }

    function resetPaymentModal() {
        $('#PaySalaryForm')[0].reset();
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

    // Dönem filtresinde değişiklik olduğunda filtreleri uygula
    $('#SalaryPeriodFilter').on('change', function () {
        applyFilters();
    });

    // Vadesi gelenler checkbox değişiminde
    $('#ShowOnlyDue').on('change', function () {
        applyFilters();
    });
});