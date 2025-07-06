abp.modals.ServiceCreateEditModal = function () {

    function initModal(modalManager, args) {
        var $form = modalManager.getForm();

        // Süre değiştiğinde önizleme
        var $duration = $form.find('input[name="Service.DurationMinutes"]');
        $duration.on('input', function () {
            var minutes = parseInt($(this).val()) || 0;
            var preview = formatDuration(minutes);

            // Süre önizlemesi ekle
            var $preview = $form.find('.duration-preview');
            if ($preview.length === 0) {
                $preview = $('<div class="form-text duration-preview"></div>');
                $(this).parent().append($preview);
            }
            $preview.text(preview);
        });

        // Form validation
        $form.validate({
            rules: {
                'Service.Title': {
                    required: true,
                    maxlength: 200
                },
                'Service.Description': {
                    maxlength: 500
                },
                'Service.DurationMinutes': {
                    required: true,
                    min: 1,
                    max: 1440,
                    digits: true
                },
                'Service.Price': {
                    required: true,
                    min: 0.01,
                    number: true
                }
            },
            messages: {
                'Service.DurationMinutes': {
                    min: 'Süre en az 1 dakika olmalıdır',
                    max: 'Süre en fazla 1440 dakika (24 saat) olabilir'
                },
                'Service.Price': {
                    min: 'Fiyat 0\'dan büyük olmalıdır'
                }
            }
        });

        // İlk yüklemede süre önizlemesi
        $duration.trigger('input');
    };

    function formatDuration(minutes) {
        if (minutes < 60) {
            return minutes + ' dakika';
        }

        var hours = Math.floor(minutes / 60);
        var remainingMinutes = minutes % 60;

        if (remainingMinutes === 0) {
            return hours + ' saat';
        }

        return hours + ' saat ' + remainingMinutes + ' dakika';
    }

    return {
        initModal: initModal
    };
};