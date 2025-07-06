// Pages/Employees/CreateEditModal.js
abp.modals.EmployeeCreateEditModal = function () {

    function initModal(modalManager, args) {
        var $form = modalManager.getForm();

        // Telefon numarası formatlaması
        var $phoneInput = $form.find('input[name="Employee.Phone"]');
        $phoneInput.on('input', function () {
            var value = $(this).val().replace(/\D/g, '');
            if (value.length > 10) {
                value = value.substring(0, 10);
            }
            $(this).val(value); // Sadece rakamları kaydet
        });

        // Color picker sync
        var $colorPicker = $form.find('input[type="color"]');
        var $colorText = $form.find('input[type="text"][name="Employee.CalendarColor"]');

        $colorPicker.on('change', function () {
            $colorText.val($(this).val());
        });

        $colorText.on('change', function () {
            var color = $(this).val();
            if (color.match(/^#[0-9A-F]{6}$/i)) {
                $colorPicker.val(color);
            }
        });

        // UserName/Password dependency
        var $userNameInput = $form.find('input[name="Employee.UserName"]');
        var $passwordInput = $form.find('input[name="Employee.Password"]');

        $userNameInput.on('input', function () {
            var hasUserName = $(this).val().trim() !== '';
            $passwordInput.prop('required', hasUserName);

            if (hasUserName) {
                $passwordInput.closest('.mb-3').find('label').html('Şifre *');
            } else {
                $passwordInput.closest('.mb-3').find('label').html('Şifre');
            }
        });

        // Komisyon oranları toplamı uyarısı
        var $commissionInputs = $form.find('input[name*="CommissionRate"]');
        $commissionInputs.on('input', function () {
            var serviceRate = parseFloat($form.find('input[name="Employee.ServiceCommissionRate"]').val()) || 0;
            var productRate = parseFloat($form.find('input[name="Employee.ProductCommissionRate"]').val()) || 0;
            var packageRate = parseFloat($form.find('input[name="Employee.PackageCommissionRate"]').val()) || 0;

            var total = serviceRate + productRate + packageRate;

            // Uyarı göster
            var $warning = $form.find('.commission-warning');
            if ($warning.length === 0) {
                $warning = $('<div class="alert alert-warning commission-warning mt-2" style="display: none;"></div>');
                $commissionInputs.last().closest('.mb-3').append($warning);
            }

            if (total > 100) {
                $warning.html('<i class="fas fa-exclamation-triangle me-1"></i>Toplam komisyon oranı %100\'ü aşıyor! (Toplam: %' + total.toFixed(1) + ')').show();
            } else {
                $warning.hide();
            }
        });

        // Form validation
        $form.validate({
            rules: {
                'Employee.FirstName': {
                    required: true,
                    maxlength: 50
                },
                'Employee.LastName': {
                    required: true,
                    maxlength: 50
                },
                'Employee.Phone': {
                    required: true,
                    minlength: 10,
                    maxlength: 10,
                    digits: true
                },
                'Employee.Email': {
                    email: true,
                    maxlength: 100
                },
                'Employee.UserName': {
                    maxlength: 50
                },
                'Employee.Password': {
                    minlength: 6,
                    maxlength: 100
                },
                'Employee.CalendarColor': {
                    required: true,
                    maxlength: 7
                },
                'Employee.FixedSalary': {
                    min: 0,
                    number: true
                },
                'Employee.ServiceCommissionRate': {
                    min: 0,
                    max: 100,
                    number: true
                },
                'Employee.ProductCommissionRate': {
                    min: 0,
                    max: 100,
                    number: true
                },
                'Employee.PackageCommissionRate': {
                    min: 0,
                    max: 100,
                    number: true
                }
            },
            messages: {
                'Employee.Phone': {
                    required: 'Telefon numarası gereklidir',
                    minlength: 'Telefon numarası 10 haneli olmalıdır',
                    maxlength: 'Telefon numarası 10 haneli olmalıdır',
                    digits: 'Sadece rakam giriniz'
                },
                'Employee.Password': {
                    minlength: 'Şifre en az 6 karakter olmalıdır'
                },
                'Employee.ServiceCommissionRate': {
                    max: 'Komisyon oranı en fazla %100 olabilir'
                },
                'Employee.ProductCommissionRate': {
                    max: 'Komisyon oranı en fazla %100 olabilir'
                },
                'Employee.PackageCommissionRate': {
                    max: 'Komisyon oranı en fazla %100 olabilir'
                }
            }
        });

        // İlk yüklemede kontroller
        $userNameInput.trigger('input');
        $commissionInputs.trigger('input');
    };

    return {
        initModal: initModal
    };
};