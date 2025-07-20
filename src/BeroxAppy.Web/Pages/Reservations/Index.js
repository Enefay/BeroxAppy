var reservations = reservations || {};

(function () {
    let calendar;
    let currentReservation = null;
    let serviceRowIndex = 0;

    // Sayfa yüklendiğinde
    $(function () {
        initializeCalendar();
        bindEvents();
        loadDailySummary();
        loadUpcomingReservations();
    });

    // Takvimi başlat
    function initializeCalendar() {
        const calendarEl = document.getElementById('calendar');

        calendar = new FullCalendar.Calendar(calendarEl, {
            initialView: 'timeGridWeek',
            locale: 'tr',
            height: 'auto',
            headerToolbar: {
                left: 'prev,next today',
                center: 'title',
                right: 'dayGridMonth,timeGridWeek,timeGridDay,listWeek'
            },
            slotMinTime: '08:00:00',
            slotMaxTime: '20:00:00',
            slotDuration: '00:15:00',
            slotLabelInterval: '00:30:00',
            businessHours: {
                daysOfWeek: [1, 2, 3, 4, 5, 6], // Pazartesi - Cumartesi
                startTime: '09:00',
                endTime: '18:00'
            },
            editable: true,
            selectable: true,
            selectMirror: true,
            eventClick: handleEventClick,
            select: handleDateSelect,
            eventDrop: handleEventDrop,
            eventResize: handleEventResize,
            events: loadEvents,
            eventDidMount: function (info) {
                // Tooltip ekle
                $(info.el).tooltip({
                    title: `${info.event.extendedProps.customerName} - ${info.event.extendedProps.services}`,
                    placement: 'top',
                    container: 'body'
                });
            }
        });

        calendar.render();
    }

    // Event'leri yükle
    function loadEvents(fetchInfo, successCallback, failureCallback) {
        const employeeId = $('#filterEmployee').val();

        $.ajax({
            url: '/Reservations?handler=CalendarEvents',
            type: 'GET',
            data: {
                start: fetchInfo.start.toISOString(),
                end: fetchInfo.end.toISOString(),
                employeeId: employeeId || null
            },
            success: function (data) {
                const events = data.map(event => ({
                    id: event.id,
                    title: event.title,
                    start: event.start,
                    end: event.end,
                    color: event.color,
                    extendedProps: {
                        customerName: event.customerName,
                        customerPhone: event.customerPhone,
                        services: event.services,
                        status: event.status,
                        totalPrice: event.totalPrice,
                        isWalkIn: event.isWalkIn
                    }
                }));
                successCallback(events);
            },
            error: function () {
                failureCallback();
            }
        });
    }

    // Event'leri bağla
    function bindEvents() {
        // Yeni rezervasyon
        $('#newReservationBtn').click(function () {
            openReservationModal();
        });

        // Walk-in (Adisyon)
        $('#walkInBtn').click(function () {
            openReservationModal(true);
        });

        // Bugün
        $('#todayBtn').click(function () {
            calendar.today();
        });

        // Filtreler
        $('#filterEmployee, #calendarView').change(function () {
            if (this.id === 'calendarView') {
                calendar.changeView($(this).val());
            } else {
                calendar.refetchEvents();
            }
        });

        // Hizmet ekle
        $('#addServiceBtn').click(function () {
            addServiceRow();
        });

        // Rezervasyon kaydet
        $('#saveReservationBtn').click(function () {
            saveReservation();
        });

        // Rezervasyon düzenle
        $('#editReservationBtn').click(function () {
            const reservationId = $(this).data('reservation-id');
            editReservation(reservationId);
        });

        // Geldi işaretle
        $('#markArrivedBtn').click(function () {
            const reservationId = $(this).data('reservation-id');
            updateReservationStatus(reservationId, 2); // Arrived = 2
        });

        // Gelmedi işaretle
        $('#markNoShowBtn').click(function () {
            const reservationId = $(this).data('reservation-id');
            updateReservationStatus(reservationId, 1); // NoShow = 1
        });

        // Tarih değiştiğinde müsait saatleri güncelle
        $('#reservationDate').change(function () {
            updateAvailableSlots();
        });

        // Fiyat hesaplama
        $('#discountAmount, #extraAmount').on('input', calculateTotalPrice);
    }

    // Rezervasyon modalını aç
    function openReservationModal(isWalkIn = false) {
        currentReservation = null;
        $('#reservationForm')[0].reset();
        $('#reservationId').val('');
        $('#servicesContainer').empty();
        $('#isWalkIn').prop('checked', isWalkIn);
        $('#reservationModalTitle').text(isWalkIn ? 'Yeni Adisyon' : 'Yeni Rezervasyon');

        // Bugünün tarihini set et
        const today = new Date().toISOString().split('T')[0];
        $('#reservationDate').val(today);

        // İlk hizmet satırını ekle
        addServiceRow();

        $('#reservationModal').modal('show');
    }

    // Hizmet satırı ekle
    function addServiceRow() {
        const template = document.getElementById('serviceRowTemplate');
        const clone = template.content.cloneNode(true);
        const row = $(clone.querySelector('.service-row'));

        row.attr('data-index', serviceRowIndex++);

        // Event'leri bağla
        row.find('.service-select').change(function () {
            onServiceChange($(this));
        });

        row.find('.employee-select').change(function () {
            onEmployeeChange($(this));
        });

        row.find('.remove-service-btn').click(function () {
            if ($('#servicesContainer .service-row').length > 1) {
                row.remove();
                calculateTotalPrice();
            } else {
                abp.notify.warn('En az bir hizmet olmalıdır!');
            }
        });

        $('#servicesContainer').append(row);
    }

    // Hizmet değiştiğinde
    function onServiceChange(selectElement) {
        const serviceId = selectElement.val();
        const row = selectElement.closest('.service-row');
        const employeeSelect = row.find('.employee-select');

        if (!serviceId) {
            employeeSelect.html('<option value="">Önce Hizmet Seçin</option>');
            return;
        }

        // Hizmeti verebilen çalışanları getir
        $.ajax({
            url: '/Reservations?handler=EmployeesByService',
            type: 'GET',
            data: { serviceId: serviceId },
            success: function (employees) {
                employeeSelect.html('<option value="">Çalışan Seçin</option>');
                employees.forEach(emp => {
                    employeeSelect.append(`<option value="${emp.id}">${emp.fullName}</option>`);
                });
            }
        });

        // Fiyatı güncelle
        const selectedOption = selectElement.find('option:selected');
        const priceMatch = selectedOption.text().match(/₺([\d,]+)/);
        if (priceMatch) {
            const price = parseFloat(priceMatch[1].replace(',', '.'));
            row.find('.service-price').val(price);
            calculateTotalPrice();
        }
    }

    // Çalışan değiştiğinde
    function onEmployeeChange(selectElement) {
        const employeeId = selectElement.val();
        const row = selectElement.closest('.service-row');
        const serviceId = row.find('.service-select').val();
        const date = $('#reservationDate').val();

        if (!employeeId || !serviceId || !date) {
            row.find('.time-select').html('<option value="">Önce Çalışan Seçin</option>');
            return;
        }

        // Müsait saatleri getir
        $.ajax({
            url: '/Reservations?handler=AvailableSlots',
            type: 'GET',
            data: {
                employeeId: employeeId,
                serviceId: serviceId,
                date: date
            },
            success: function (data) {
                const timeSelect = row.find('.time-select');
                timeSelect.html('<option value="">Saat Seçin</option>');

                data.availableSlots.forEach(slot => {
                    if (slot.isAvailable) {
                        timeSelect.append(`<option value="${slot.startTime}">${slot.display}</option>`);
                    }
                });
            }
        });
    }

    // Rezervasyon kaydet
    function saveReservation() {
        const form = $('#reservationForm');
        if (!form[0].checkValidity()) {
            form[0].reportValidity();
            return;
        }

        // Hizmet detaylarını topla
        const reservationDetails = [];
        let isValid = true;

        $('#servicesContainer .service-row').each(function () {
            const row = $(this);
            const serviceId = row.find('.service-select').val();
            const employeeId = row.find('.employee-select').val();
            const startTime = row.find('.time-select').val();
            const customPrice = row.find('.service-price').val();
            const note = row.find('.service-note').val();

            if (!serviceId || !employeeId || !startTime) {
                isValid = false;
                abp.notify.error('Lütfen tüm hizmet detaylarını doldurun!');
                return false;
            }

            reservationDetails.push({
                serviceId: serviceId,
                employeeId: employeeId,
                startTime: startTime,
                customPrice: customPrice ? parseFloat(customPrice) : null,
                note: note
            });
        });

        if (!isValid) return;

        const data = {
            customerId: $('#customerId').val(),
            note: $('#note').val(),
            reservationDate: $('#reservationDate').val(),
            discountAmount: $('#discountAmount').val() ? parseFloat($('#discountAmount').val()) : null,
            extraAmount: $('#extraAmount').val() ? parseFloat($('#extraAmount').val()) : null,
            isWalkIn: $('#isWalkIn').is(':checked'),
            reservationDetails: reservationDetails
        };

        const reservationId = $('#reservationId').val();
        const url = reservationId
            ? `/api/app/reservation/${reservationId}`
            : '/api/app/reservation';
        const method = reservationId ? 'PUT' : 'POST';

        abp.ui.setBusy('#reservationModal');

        $.ajax({
            url: url,
            type: method,
            contentType: 'application/json',
            data: JSON.stringify(data),
            success: function () {
                $('#reservationModal').modal('hide');
                calendar.refetchEvents();
                loadDailySummary();
                loadUpcomingReservations();
                abp.notify.success('Rezervasyon başarıyla kaydedildi!');
            },
            error: function (xhr) {
                abp.notify.error('Rezervasyon kaydedilemedi!');
            },
            complete: function () {
                abp.ui.clearBusy('#reservationModal');
            }
        });
    }

    // Event'e tıklandığında
    function handleEventClick(info) {
        const reservationId = info.event.id;

        $.ajax({
            url: '/Reservations?handler=ReservationDetails',
            type: 'GET',
            data: { id: reservationId },
            success: function (reservation) {
                showReservationDetails(reservation);
            }
        });
    }

    // Rezervasyon detaylarını göster
    function showReservationDetails(reservation) {
        let detailsHtml = `
            <div class="row mb-3">
                <div class="col-md-6">
                    <strong>Müşteri:</strong> ${reservation.customerName}<br/>
                    <strong>Telefon:</strong> ${reservation.customerPhone}<br/>
                    <strong>Tarih:</strong> ${new Date(reservation.reservationDate).toLocaleDateString('tr-TR')}<br/>
                    <strong>Saat:</strong> ${reservation.reservationTimeDisplay}
                </div>
                <div class="col-md-6">
                    <strong>Durum:</strong> <span class="badge bg-${getStatusBadgeColor(reservation.status)}">${reservation.statusDisplay}</span><br/>
                    <strong>Ödeme:</strong> <span class="badge bg-${getPaymentBadgeColor(reservation.paymentStatus)}">${reservation.paymentStatusDisplay}</span><br/>
                    <strong>Tip:</strong> ${reservation.reservationTypeDisplay}<br/>
                    <strong>Toplam:</strong> ₺${reservation.finalPrice.toFixed(2)}
                </div>
            </div>
            
            <h6 class="mt-3">Hizmetler</h6>
            <div class="table-responsive">
                <table class="table table-sm">
                    <thead>
                        <tr>
                            <th>Hizmet</th>
                            <th>Çalışan</th>
                            <th>Saat</th>
                            <th>Fiyat</th>
                        </tr>
                    </thead>
                    <tbody>`;

        reservation.reservationDetails.forEach(detail => {
            detailsHtml += `
                <tr>
                    <td>${detail.serviceTitle}</td>
                    <td>${detail.employeeName}</td>
                    <td>${detail.timeDisplay}</td>
                    <td>₺${detail.servicePrice.toFixed(2)}</td>
                </tr>`;
        });

        detailsHtml += `
                    </tbody>
                </table>
            </div>`;

        if (reservation.note) {
            detailsHtml += `<div class="mt-3"><strong>Not:</strong> ${reservation.note}</div>`;
        }

        $('#reservationDetailContent').html(detailsHtml);

        // Butonları ayarla
        $('#editReservationBtn, #markArrivedBtn, #markNoShowBtn').data('reservation-id', reservation.id);

        // Duruma göre butonları göster/gizle
        if (reservation.status === 0) { // Pending
            $('#markArrivedBtn, #markNoShowBtn').show();
        } else {
            $('#markArrivedBtn, #markNoShowBtn').hide();
        }

        $('#reservationDetailModal').modal('show');
    }

    // Tarih seçimi
    function handleDateSelect(selectInfo) {
        const start = selectInfo.start;
        const allDay = selectInfo.allDay;

        if (!allDay) {
            $('#reservationDate').val(start.toISOString().split('T')[0]);
            openReservationModal();
            calendar.unselect();
        }
    }

    // Event sürükleme
    function handleEventDrop(info) {
        abp.message.confirm(
            'Rezervasyon tarihini değiştirmek istediğinize emin misiniz?',
            function (confirmed) {
                if (confirmed) {
                    updateReservationDateTime(info.event.id, info.event.start);
                } else {
                    info.revert();
                }
            }
        );
    }

    // Event boyutlandırma
    function handleEventResize(info) {
        info.revert(); // Boyutlandırmaya izin verme
        abp.notify.warn('Rezervasyon süresi değiştirilemez!');
    }

    // Rezervasyon tarih/saatini güncelle
    function updateReservationDateTime(reservationId, newDateTime) {
        // Bu fonksiyon için backend'de özel bir endpoint gerekli
        abp.notify.info('Bu özellik henüz hazır değil.');
        calendar.refetchEvents();
    }

    // Rezervasyon durumunu güncelle
    function updateReservationStatus(reservationId, status) {
        $.ajax({
            url: `/api/app/reservation/${reservationId}/status`,
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(status),
            success: function () {
                $('#reservationDetailModal').modal('hide');
                calendar.refetchEvents();
                loadDailySummary();
                abp.notify.success('Durum güncellendi!');
            }
        });
    }

    // Günlük özeti yükle
    function loadDailySummary() {
        const today = new Date().toISOString().split('T')[0];

        $.ajax({
            url: `/api/app/reservation/daily-report?date=${today}`,
            type: 'GET',
            success: function (data) {
                let summaryHtml = `
                    <div class="d-flex justify-content-between mb-2">
                        <span>Toplam:</span>
                        <strong>${data.totalReservations}</strong>
                    </div>
                    <div class="d-flex justify-content-between mb-2">
                        <span>Tamamlanan:</span>
                        <strong class="text-success">${data.completedReservations}</strong>
                    </div>
                    <div class="d-flex justify-content-between mb-2">
                        <span>Gelmedi:</span>
                        <strong class="text-danger">${data.noShowReservations}</strong>
                    </div>
                    <div class="d-flex justify-content-between mb-2">
                        <span>Adisyon:</span>
                        <strong>${data.walkInReservations}</strong>
                    </div>
                    <hr/>
                    <div class="d-flex justify-content-between">
                        <span>Günlük Ciro:</span>
                        <strong class="text-primary">₺${data.totalRevenue.toFixed(2)}</strong>
                    </div>`;

                $('#dailySummary').html(summaryHtml);
            }
        });
    }

    // Yaklaşan rezervasyonları yükle
    function loadUpcomingReservations() {
        $.ajax({
            url: '/api/app/reservation/upcoming',
            type: 'GET',
            success: function (data) {
                if (data.length === 0) {
                    $('#upcomingReservations').html('<p class="text-muted text-center">Yaklaşan rezervasyon yok</p>');
                    return;
                }

                let html = '<div class="list-group list-group-flush">';

                data.forEach(reservation => {
                    const time = new Date(reservation.reservationDate + 'T' + reservation.startTime);
                    const timeStr = time.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });

                    html += `
                        <a href="#" class="list-group-item list-group-item-action p-2" 
                           onclick="reservations.showReservationDetails('${reservation.id}'); return false;">
                            <div class="d-flex justify-content-between align-items-center">
                                <div>
                                    <strong>${timeStr}</strong> - ${reservation.customerName}
                                    <br/>
                                    <small class="text-muted">${reservation.reservationDetails[0]?.serviceTitle || 'Hizmet'}</small>
                                </div>
                                <span class="badge bg-primary rounded-pill">₺${reservation.finalPrice.toFixed(0)}</span>
                            </div>
                        </a>`;
                });

                html += '</div>';
                $('#upcomingReservations').html(html);
            }
        });
    }

    // Toplam fiyatı hesapla
    function calculateTotalPrice() {
        let total = 0;

        $('.service-price').each(function () {
            const price = parseFloat($(this).val()) || 0;
            total += price;
        });

        const discount = parseFloat($('#discountAmount').val()) || 0;
        const extra = parseFloat($('#extraAmount').val()) || 0;

        total = total - discount + extra;

        $('#totalAmount').val('₺' + total.toFixed(2));
    }

    // Durum badge rengi
    function getStatusBadgeColor(status) {
        switch (status) {
            case 0: return 'warning'; // Pending
            case 1: return 'danger';  // NoShow
            case 2: return 'success'; // Arrived
            default: return 'secondary';
        }
    }

    // Ödeme durumu badge rengi
    function getPaymentBadgeColor(status) {
        switch (status) {
            case 0: return 'warning'; // Pending
            case 1: return 'info';    // Partial
            case 2: return 'success'; // Paid
            case 3: return 'danger';  // Refunded
            default: return 'secondary';
        }
    }

    // Public metodlar
    reservations.showReservationDetails = function (reservationId) {
        $.ajax({
            url: '/Reservations?handler=ReservationDetails',
            type: 'GET',
            data: { id: reservationId },
            success: function (reservation) {
                showReservationDetails(reservation);
            }
        });
    };

})();