$(function () {
    var $modal = $('#WorkingHoursModal');

    // Aktif checkbox değiştiğinde satırın inputlarını enable/disable et
    $(document).on('change', '#WorkingHoursTable .is-active', function () {
        var $tr = $(this).closest('tr');
        var enabled = $(this).is(':checked');
        $tr.find('input[type="time"]').prop('disabled', !enabled);
    });

    // Kaydet
    $(document).on('click', '#SaveWorkingHoursBtn', function () {
        var employeeId = $('#EmployeeId').val();
        var workingHours = [];

        $('#WorkingHoursTable tbody tr').each(function () {
            var $tr = $(this);
            var dayOfWeek = parseInt($tr.data('day'));
            var isActive = $tr.find('.is-active').is(':checked');
            var start = $tr.find('.start-time').val();
            var end = $tr.find('.end-time').val();
            var breakStart = $tr.find('.break-start').val();
            var breakEnd = $tr.find('.break-end').val();

            // Saatler boşsa atlama
            if (!isActive) return;

            workingHours.push({
                employeeId: employeeId,
                dayOfWeek: dayOfWeek,
                startTime: start ? start + ":00" : null,
                endTime: end ? end + ":00" : null,
                breakStartTime: breakStart ? breakStart + ":00" : null,
                breakEndTime: breakEnd ? breakEnd + ":00" : null,
                isActive: isActive
            });
        });

        abp.ui.setBusy($modal);

        beroxAppy.employees.employee.setWorkingHours(employeeId, workingHours)
            .then(function () {
                abp.notify.success('Çalışma saatleri kaydedildi.');
                $modal.modal('hide');
                // Ana tabloyu da reload etmek istersen: window.employeeDataTable?.ajax.reload();
            })
            .catch(function (error) {
                abp.notify.error(error.message || 'Bir hata oluştu!');
            })
            .always(function () {
                abp.ui.clearBusy($modal);
            });
    });


    // Tüm günleri aktif yap
    $(document).on('click', '#activateAllDays', function () {
        $('#WorkingHoursTable .is-active').each(function () {
            $(this).prop('checked', true).trigger('change');
        });
    });

    // Tüm günleri pasif yap
    $(document).on('click', '#deactivateAllDays', function () {
        $('#WorkingHoursTable .is-active').each(function () {
            $(this).prop('checked', false).trigger('change');
        });
    });

    // Bir günün saatlerini diğer günlere uygula
    $(document).on('click', '.copy-day', function () {
        var $tr = $(this).closest('tr');
        var dayOfWeek = $tr.data('day');
        var start = $tr.find('.start-time').val();
        var end = $tr.find('.end-time').val();
        var breakStart = $tr.find('.break-start').val();
        var breakEnd = $tr.find('.break-end').val();

        // Aktif olan tüm günlere uygula (kendisi hariç)
        $('#WorkingHoursTable tbody tr').each(function () {
            var $row = $(this);
            if ($row.data('day') !== dayOfWeek) {
                $row.find('.start-time').val(start);
                $row.find('.end-time').val(end);
                $row.find('.break-start').val(breakStart);
                $row.find('.break-end').val(breakEnd);
                $row.find('.is-active').prop('checked', true).trigger('change');
            }
        });
        abp.notify.info("Bu günün saatleri tüm günlere uygulandı.");
    });

});
