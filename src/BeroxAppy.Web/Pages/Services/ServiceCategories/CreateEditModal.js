abp.modals.ServiceCategoryCreateEditModal = function () {

    function initModal(modalManager, args) {
        var $form = modalManager.getForm();

        // Color picker sync
        var $colorPicker = $form.find('input[type="color"]');
        var $colorText = $form.find('input[type="text"][name="ServiceCategory.Color"]');

        $colorPicker.on('change', function () {
            $colorText.val($(this).val());
        });

        $colorText.on('change', function () {
            var color = $(this).val();
            if (color.match(/^#[0-9A-F]{6}$/i)) {
                $colorPicker.val(color);
            }
        });

        // Form validation
        $form.validate({
            rules: {
                'ServiceCategory.Name': {
                    required: true,
                    maxlength: 100
                },
                'ServiceCategory.Color': {
                    maxlength: 50
                },
                'ServiceCategory.DisplayOrder': {
                    min: 0,
                    digits: true
                }
            }
        });
    };

    return {
        initModal: initModal
    };
};