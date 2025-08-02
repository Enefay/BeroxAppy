﻿$(function () {
    var l = abp.localization.getResource('BeroxAppy');
    var paymentService = beroxAppy.services.paymentAppService;

    // Tarih değişikliği
    $('#RefreshButton').on('click', function () {
        var selectedDate = $('#DateFilter').val();
        if (selectedDate) {
            window.location.href = '/Finance/CashRegister?date=' + selectedDate;
        }
    });

    // Enter tuşu ile de yenileme
    $('#DateFilter').on('keypress', function (e) {
        if (e.which === 13) {
            $('#RefreshButton').click();
        }
    });

    // Kasayı kapat butonu
    $('#CloseCashButton').on('click', function () {
        $('#CloseCashModal').modal('show');
    });

    // Kasayı tekrar aç butonu
    $('#ReopenCashButton').on('click', function () {
        abp.message.confirm(
            'Kapatılmış kasayı tekrar açmak istediğinizden emin misiniz? Bu işlem dikkatli yapılmalıdır.',
            'Kasayı Tekrar Aç',
            function (isConfirmed) {
                if (isConfirmed) {
                    abp.ajax({
                        url: '/Finance/CashRegister?handler=ReopenCash',
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

    // Hızlı işlem butonları
    $('#CashInButton').on('click', function () {
        openCashTransactionModal('in', 'Nakit Giriş', 'success', 'plus-circle');
    });

    $('#CashOutButton').on('click', function () {
        openCashTransactionModal('out', 'Nakit Çıkış', 'danger', 'minus-circle');
    });

    $('#CountCashButton').on('click', function () {
        $('#CashCountModal').modal('show');
        calculateCashCount();
    });

    $('#ViewReportButton').on('click', function () {
        var selectedDate = $('#DateFilter').val() || new Date().toISOString().split('T')[0];
        abp.ajax({
            url: '/Finance/CashRegister?handler=CashReport&date=' + selectedDate,
            type: 'GET'
        }).done(function (result) {
            if (result.success) {
                displayCashReport(result.data);
            } else {
                abp.notify.error(result.message);
            }
        });
    });

    // Nakit işlem modalını aç
    function openCashTransactionModal(type, title, headerClass, icon) {
        $('#TransactionType').val(type);
        $('#TransactionModalTitle').html('<i class="fas fa-' + icon + ' me-2"></i>' + title);
        $('#TransactionModalHeader').removeClass().addClass('modal-header bg-' + headerClass + ' text-white');
        $('#TransactionSaveButton').removeClass().addClass('btn btn-' + headerClass);

        if (type === 'in') {
            $('#TransactionDescription').attr('placeholder', 'Örn: Ekstra gelir, borç tahsilatı...');
        } else {
            $('#TransactionDescription').attr('placeholder', 'Örn: Personel avansı, kırtasiye...');
        }

        $('#CashTransactionModal').modal('show');
        $('#TransactionAmount').focus();
    }

    // Nakit işlem kaydet
    $('#TransactionSaveButton').on('click', function () {
        var form = $('#CashTransactionForm');

        if (!form[0].checkValidity()) {
            form[0].reportValidity();
            return;
        }

        var type = $('#TransactionType').val();
        var amount = parseFloat($('#TransactionAmount').val());
        var description = $('#TransactionDescription').val();
        var note = $('#TransactionNote').val();

        if (amount <= 0) {
            abp.notify.error('Tutar sıfırdan büyük olmalıdır!');
            return;
        }

        abp.ui.setBusy($('#CashTransactionModal'));

        abp.ajax({
            url: '/Finance/CashRegister?handler=CashTransaction',
            type: 'POST',
            data: {
                transactionType: type,
                amount: amount,
                description: description,
                note: note
            },
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            }
        }).done(function (result) {
            if (result.success) {
                abp.notify.success(result.message);
                $('#CashTransactionModal').modal('hide');
                window.location.reload();
            } else {
                abp.notify.error(result.message);
            }
        }).always(function () {
            abp.ui.clearBusy($('#CashTransactionModal'));
        });
    });

    // Kasa kapanış fark kontrolü
    $('#ActualClosingBalance').on('input', function () {
        var theoretical = parseFloat($('#ActualClosingBalance').data('theoretical') || 0);
        var actual = parseFloat($(this).val() || 0);
        var difference = actual - theoretical;

        if (Math.abs(difference) > 0.01) {
            $('#DifferenceAlert').show();
            $('#DifferenceAmount').html(
                '<strong>Fark: ' + (difference >= 0 ? '+' : '') + '₺' + difference.toFixed(2) + '</strong>'
            );
        } else {
            $('#DifferenceAlert').hide();
        }
    });

    // Kasa kapama kaydet
    $('#CloseCashSaveButton').on('click', function () {
        var actualBalance = parseFloat($('#ActualClosingBalance').val());
        var note = $('#ClosingNote').val();

        if (isNaN(actualBalance) || actualBalance < 0) {
            abp.notify.error('Geçerli bir kapanış bakiyesi giriniz!');
            return;
        }

        abp.message.confirm(
            'Kasayı kapatmak istediğinizden emin misiniz? Bu işlem geri alınamaz.',
            'Kasa Kapama Onayı',
            function (isConfirmed) {
                if (isConfirmed) {
                    abp.ui.setBusy($('#CloseCashModal'));

                    abp.ajax({
                        url: '/Finance/CashRegister?handler=CloseCash',
                        type: 'POST',
                        data: {
                            actualClosingBalance: actualBalance,
                            note: note
                        },
                        headers: {
                            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                        }
                    }).done(function (result) {
                        if (result.success) {
                            abp.notify.success(result.message);
                            $('#CloseCashModal').modal('hide');
                            window.location.reload();
                        } else {
                            abp.notify.error(result.message);
                        }
                    }).always(function () {
                        abp.ui.clearBusy($('#CloseCashModal'));
                    });
                }
            }
        );
    });

    // Kasa sayım hesaplama
    function calculateCashCount() {
        $('.cash-count, #CoinsAmount').on('input', function () {
            var total = 0;

            $('.cash-count').each(function () {
                var count = parseInt($(this).val()) || 0;
                var value = parseFloat($(this).data('value')) || 0;
                total += count * value;
            });

            var coins = parseFloat($('#CoinsAmount').val()) || 0;
            total += coins;

            $('#CountTotal').text('₺' + total.toFixed(2));
        });
    }

    // Modal temizlik işlemleri
    $('#CashTransactionModal').on('hidden.bs.modal', function () {
        $('#CashTransactionForm')[0].reset();
    });

    $('#CloseCashModal').on('hidden.bs.modal', function () {
        $('#CloseCashForm')[0].reset();
        $('#DifferenceAlert').hide();
    });

    $('#CashCountModal').on('hidden.bs.modal', function () {
        $('#CashCountForm')[0].reset();
        $('#CountTotal').text('₺0.00');
    });

    // Kasa sayım modalı açıldığında teorik bakiyeyi set et
    $('#CashCountModal').on('shown.bs.modal', function () {
        var theoretical = $('.text-primary h4').text().replace('₺', '').replace(',', '');
        $('#ActualClosingBalance').data('theoretical', parseFloat(theoretical));
        calculateCashCount();
    });

    // Rapor gösterme fonksiyonu
    function displayCashReport(report) {
        var reportHtml = '<div class="alert alert-info">';
        reportHtml += '<h6><i class="fas fa-chart-bar me-2"></i>Günlük Kasa Raporu</h6>';
        reportHtml += '<div class="row">';
        reportHtml += '<div class="col-3"><strong>Açılış:</strong> ₺' + report.openingBalance.toFixed(2) + '</div>';
        reportHtml += '<div class="col-3"><strong>Giriş:</strong> ₺' + report.totalCashIn.toFixed(2) + '</div>';
        reportHtml += '<div class="col-3"><strong>Çıkış:</strong> ₺' + report.totalCashOut.toFixed(2) + '</div>';
        reportHtml += '<div class="col-3"><strong>Kapanış:</strong> ₺' + report.actualClosing.toFixed(2) + '</div>';
        reportHtml += '</div></div>';

        abp.message.info(reportHtml, 'Kasa Raporu');
    }

    // Otomatik yenileme (5 dakikada bir, sadece bugün için)
    var selectedDate = $('#DateFilter').val();
    var today = new Date().toISOString().split('T')[0];

    if (selectedDate === today) {
        setInterval(function () {
            // Modal açık değilse ve sayfa aktifse yenile
            if (!$('.modal').hasClass('show') && document.hasFocus()) {
                window.location.reload();
            }
        }, 300000); // 5 dakika
    }

    // Sayfa yüklendiğinde teorik bakiyeyi data attribute olarak sakla
    $(document).ready(function () {
        var theoretical = $('.text-primary h4').text().replace('₺', '').replace(',', '');
        $('#ActualClosingBalance').data('theoretical', parseFloat(theoretical) || 0);
    });
});