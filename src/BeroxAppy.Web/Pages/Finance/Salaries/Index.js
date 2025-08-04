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
        showEmployeeDetail(employeeId);
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

    // Bireysel ödeme tutarı değiştiğinde toplam tutarı güncelle
    $(document).on('input', '.individual-payment-amount', function () {
        updateTotalFromIndividual();
    });

    // Eşit dağıt butonu
    $(document).on('click', '#DistributeEquallyButton', function () {
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

        var totalAmount = selectedEmployees.reduce((sum, emp) => {
            return sum + Number(
                String(emp.amount).replace('.', '').replace(',', '.')
            );
        }, 0);

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
        if (isMultiple) {
            calculateDistribution();
        }

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
                    <td>
                        <div class="d-flex align-items-center">
                            <strong>${emp.employeeName}</strong>
                        </div>
                    </td>
                    <td>
                        <span class="text-muted">₺${emp.amount.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</span>
                    </td>
                    <td>
                        <div class="input-group input-group-sm" style="max-width: 150px;">
                            <span class="input-group-text">₺</span>
                            <input type="number" 
                                   class="form-control individual-payment-amount" 
                                   data-employee-id="${emp.employeeId}"
                                   data-max-amount="${emp.amount}"
                                   value="${employeePayment.toFixed(2)}" 
                                   min="0" 
                                   max="${emp.amount}" 
                                   step="0.01">
                        </div>
                        <small class="text-muted">Max: ₺${emp.amount.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</small>
                    </td>
                </tr>
            `;
        });

        var tableHeader = `
            <div class="d-flex justify-content-between align-items-center mb-2">
                <h6 class="mb-0">Ödeme Dağılımı</h6>
                <button type="button" class="btn btn-sm btn-outline-primary" id="DistributeEquallyButton">
                    <i class="fas fa-balance-scale me-1"></i>Eşit Dağıt
                </button>
            </div>
        `;

        $('#PaymentDistribution .mb-3').first().html(tableHeader);
        $('#DistributionTableBody').html(distributionHtml);
    }

    function updateTotalFromIndividual() {
        var totalIndividual = 0;

        $('.individual-payment-amount').each(function () {
            var amount = parseFloat($(this).val()) || 0;
            var maxAmount = parseFloat($(this).data('max-amount')) || 0;

            // Maksimum tutarı aşmasını engelle
            if (amount > maxAmount) {
                $(this).val(maxAmount.toFixed(2));
                amount = maxAmount;
                abp.notify.warn(`${$(this).closest('tr').find('strong').text()} için maksimum tutar ₺${maxAmount.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}'dir.`);
            }

            totalIndividual += amount;
        });

        // Toplam ödeme tutarını güncelle
        $('#PaymentAmount').val(totalIndividual.toFixed(2));

        // Toplam maaş tutarından fazla olamaz kontrolü
        var totalSalary = selectedEmployees.reduce((sum, emp) => sum + emp.amount, 0);
        if (totalIndividual > totalSalary) {
            abp.notify.warn('Toplam ödeme tutarı, toplam maaş tutarından fazla olamaz!');
        }
    }

    function saveSalaryPayment() {
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

        // Toplu ödeme için bireysel tutarları al
        var employeeSalaries = [];
        if (selectedEmployees.length > 1) {
            var totalIndividualPayments = 0;

            $('.individual-payment-amount').each(function () {
                var employeeId = $(this).data('employee-id');
                var amount = parseFloat($(this).val()) || 0;
                var maxAmount = parseFloat($(this).data('max-amount')) || 0;

                if (amount > maxAmount) {
                    abp.notify.error(`${$(this).closest('tr').find('strong').text()} için ödeme tutarı maksimum tutarı aşıyor!`);
                    return false;
                }

                var employee = selectedEmployees.find(emp => emp.employeeId === employeeId);
                if (employee && amount > 0) {
                    employeeSalaries.push({
                        employeeId: employeeId,
                        employeeName: employee.employeeName,
                        amount: amount
                    });
                    totalIndividualPayments += amount;
                }
            });

            if (employeeSalaries.length === 0) {
                abp.notify.error('En az bir çalışan için ödeme tutarı girilmelidir.');
                return;
            }

            paymentAmount = totalIndividualPayments;
        } else {
            // Tekil ödeme
            employeeSalaries = selectedEmployees.map(function (emp) {
                return {
                    employeeId: emp.employeeId,
                    employeeName: emp.employeeName,
                    amount: Math.min(paymentAmount, parseFloat(emp.amount.toString().replace(',', '.')))
                };
            });
        }

        var requestData = {
            employeeSalaries: employeeSalaries,
            paymentMethod: paymentMethod,
            paymentAmount: paymentAmount
        };

        abp.ui.setBusy($('#PaySalaryModal'));

        console.log("Gönderilen veri:", requestData);

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

    function showEmployeeDetail(employeeId) {
        abp.ui.setBusy($('#EmployeeDetailModal'));

        abp.ajax({
            url: '/Finance/Salaries/Index?handler=EmployeeDetail',
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
                            <h6 class="fw-bold mb-3">Maaş Özeti (Son 6 Ay)</h6>
                            <div class="row">
                                <div class="col-md-3">
                                    <div class="text-center p-3 bg-light rounded">
                                        <h5 class="text-success mb-1">₺${performance.totalSalaryPaid.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</h5>
                                        <small class="text-muted">Ödenen Maaş</small>
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
                                        <h5 class="text-warning mb-1">₺${performance.totalBonusPaid.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</h5>
                                        <small class="text-muted">Bonus/Kesinti</small>
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="text-center p-3 bg-light rounded">
                                        <h5 class="text-primary mb-1">${performance.paymentCount}</h5>
                                        <small class="text-muted">Ödeme Sayısı</small>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-12">
                            <h6 class="fw-bold mb-3">Son Maaş Ödemeleri</h6>
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
                                                    <td><strong>₺${payment.salaryAmount.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</strong></td>
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