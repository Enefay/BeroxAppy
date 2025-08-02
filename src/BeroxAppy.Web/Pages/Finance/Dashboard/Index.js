$(function () {
    var l = abp.localization.getResource('BeroxAppy');
    var financeService = beroxAppy.services.financeAppService;

    // Tarih değişikliği
    $('#RefreshButton').on('click', function () {
        var selectedDate = $('#DateFilter').val();
        if (selectedDate) {
            window.location.href = '/Finance/Dashboard?date=' + selectedDate;
        }
    });

    // Enter tuşu ile de yenileme
    $('#DateFilter').on('keypress', function (e) {
        if (e.which === 13) {
            $('#RefreshButton').click();
        }
    });

    // Kasayı kapat
    $('#CloseCashButton').on('click', function () {
        abp.message.confirm(
            'Kasayı kapatmak istediğinizden emin misiniz?',
            'Kasa Kapama',
            function (isConfirmed) {
                if (isConfirmed) {
                    abp.ajax({
                        url: '/Finance/Dashboard?handler=CloseCash',
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
                    });
                }
            }
        );
    });

    // Komisyon ödeme modal açma
    $(document).on('click', '.pay-commission-btn', function () {
        var employeeId = $(this).data('employee-id');
        var employeeName = $(this).data('employee-name');
        var amount = $(this).data('amount');

        $('#EmployeeId').val(employeeId);
        $('#EmployeeName').val(employeeName);
        $('#PendingAmount').val('₺' + parseFloat(amount).toLocaleString('tr-TR', { minimumFractionDigits: 2 }));
        $('#PaymentAmount').val(amount).attr('max', amount);

        $('#PayCommissionModal').modal('show');
    });

    // Hızlı komisyon ödeme butonu
    $('#QuickCommissionButton').on('click', function () {
        // İlk çalışanı seç veya seçim modalı aç
        var firstCommissionBtn = $('.pay-commission-btn').first();
        if (firstCommissionBtn.length > 0) {
            firstCommissionBtn.click();
        } else {
            abp.notify.info('Ödenecek komisyon bulunmuyor.');
        }
    });

    // Komisyon ödeme kaydetme
    $('#PayCommissionSaveButton').on('click', function () {
        var form = $('#PayCommissionForm');

        if (!form[0].checkValidity()) {
            form[0].reportValidity();
            return;
        }

        var employeeId = $('#EmployeeId').val();
        var amount = parseFloat($('#PaymentAmount').val());
        var paymentMethod = parseInt($('#PaymentMethod').val());
        var note = $('#PaymentNote').val();

        if (!employeeId || amount <= 0 || isNaN(paymentMethod)) {
            abp.notify.error('Lütfen tüm gerekli alanları doldurun.');
            return;
        }

        abp.ui.setBusy($('#PayCommissionModal'));

        financeService.payEmployeeCommission(employeeId, amount, paymentMethod, note)
            .done(function () {
                abp.notify.success('Komisyon ödemesi başarıyla kaydedildi.');
                $('#PayCommissionModal').modal('hide');
                window.location.reload();
            })
            .fail(function (error) {
                abp.notify.error(error.message || 'Komisyon ödemesi sırasında bir hata oluştu.');
            })
            .always(function () {
                abp.ui.clearBusy($('#PayCommissionModal'));
            });
    });

    // Modal temizleme
    $('#PayCommissionModal').on('hidden.bs.modal', function () {
        $('#PayCommissionForm')[0].reset();
        $('#EmployeeId').val('');
    });

    // Hızlı işlem butonları
    $('abp-button[text="Rezervasyon Oluştur"]').on('click', function () {
        window.location.href = '/Reservations/CreateEditModal';
    });

    $('abp-button[text="Ödeme Kaydet"]').on('click', function () {
        window.location.href = '/Payments';
    });

    $('abp-button[text="Raporlar"]').on('click', function () {
        window.location.href = '/Finance/Reports';
    });

    // Otomatik yenileme (5 dakikada bir)
    setInterval(function () {
        if (!$('#PayCommissionModal').hasClass('show')) {
            // Modal açık değilse yenile
            window.location.reload();
        }
    }, 300000); // 5 dakika
});