var reservations = reservations || {};

var createEditModal = new abp.ModalManager(abp.appPath + 'Reservations/CreateEditModal');

(function () {
    let calendar;
    let currentReservation = null;

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

        // Rezervasyon düzenle
        $(document).on('click', '#editReservationBtn', function () {
            const reservationId = $(this).data('reservation-id');
            $('#reservationDetailModal').modal('hide');
            setTimeout(() => {
                editReservation(reservationId);
            }, 500); // Modal kapanma animasyonunu bekle
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
    }

    // Rezervasyon modalını aç
    function openReservationModal(isWalkIn = false) {
        createEditModal.open({ id: null });
    }

    function editReservation(reservationId) {
        createEditModal.open({ id: reservationId });
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
        createEditModal.open({ id: null });
        calendar.unselect();
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
            url: '/api/app/reservation/upcoming-reservations/2',
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

    createEditModal.onResult(function () {
        calendar.refetchEvents();
        loadDailySummary();
        loadUpcomingReservations();
        abp.notify.success('Rezervasyon başarıyla kaydedildi!');
    });

})();