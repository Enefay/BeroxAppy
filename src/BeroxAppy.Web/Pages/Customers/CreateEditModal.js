// Pages/Customers/CreateEditModal.js
abp.modals.CustomerCreateEditModal = function () {

    function initModal(modalManager, args) {
        var $form = modalManager.getForm();

        // Telefon numarası formatlaması
        var $phoneInput = $form.find('input[name="Customer.Phone"]');
        $phoneInput.on('input', function () {
            var value = $(this).val().replace(/\D/g, '');
            if (value.length > 10) {
                value = value.substring(0, 10);
            }
            $(this).val(value); // Sadece rakamları kaydet, format yok
        });

        // Instagram username formatlaması
        var $instagramInput = $form.find('input[name="Customer.Instagram"]');
        $instagramInput.on('input', function () {
            var value = $(this).val();
            if (value && !value.startsWith('@')) {
                $(this).val('@' + value);
            }
        });

        // Yaş hesaplama ve gösterme
        var $birthDateInput = $form.find('input[name="Customer.BirthDate"]');
        $birthDateInput.on('change', function () {
            var birthDate = new Date($(this).val());
            if (birthDate) {
                var today = new Date();
                var age = today.getFullYear() - birthDate.getFullYear();
                var monthDiff = today.getMonth() - birthDate.getMonth();

                if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
                    age--;
                }

                // Yaş bilgisini göster
                var $ageDisplay = $form.find('.age-display');
                if ($ageDisplay.length === 0) {
                    $ageDisplay = $('<div class="form-text age-display"></div>');
                    $(this).parent().append($ageDisplay);
                }

                if (age >= 0 && age <= 120) {
                    $ageDisplay.text(age + ' yaşında').removeClass('text-danger').addClass('text-success');
                } else {
                    $ageDisplay.text('Geçersiz tarih').removeClass('text-success').addClass('text-danger');
                }
            }
        });

        // Form validation
        $form.validate({
            rules: {
                'Customer.FullName': {
                    required: true,
                    maxlength: 100
                },
                'Customer.Phone': {
                    required: true,
                    minlength: 10,
                    maxlength: 10,
                    digits: true
                },
                'Customer.Email': {
                    email: true,
                    maxlength: 100
                },
                'Customer.Instagram': {
                    maxlength: 50
                },
                'Customer.Note': {
                    maxlength: 1000
                },
                'Customer.DiscountRate': {
                    min: 0,
                    max: 100,
                    number: true
                },
                'Customer.TotalDebt': {
                    min: 0,
                    number: true
                }
            },
            messages: {
                'Customer.Phone': {
                    required: 'Telefon numarası gereklidir',
                    minlength: 'Telefon numarası 10 haneli olmalıdır',
                    maxlength: 'Telefon numarası 10 haneli olmalıdır'
                },
                'Customer.DiscountRate': {
                    min: 'İndirim oranı 0\'dan küçük olamaz',
                    max: 'İndirim oranı 100\'den büyük olamaz'
                },
                'Customer.TotalDebt': {
                    min: 'Borç tutarı negatif olamaz'
                }
            }
        });

        // İlk yüklemede doğum tarihi varsa yaş hesapla
        $birthDateInput.trigger('change');
    };

    return {
        initModal: initModal
    };
};