var reservations = reservations || {};

// ABP ModalManager ile Razor Page modalını tanımla
var reservationDetailModal = new abp.ModalManager('/Reservations/ReservationDetailModal');
var createEditModal = new abp.ModalManager(abp.appPath + 'Reservations/CreateEditModal');
var completeReservationModal = new abp.ModalManager('/Reservations/CompleteReservationModal');


reservations.openEditModal = function (reservationId) {
    createEditModal.open({ id: reservationId });
};

reservations.completeReservation = function (reservationId) {
    completeReservationModal.open({ id: reservationId });
};

reservations.markAsNoShow = function (reservationId) {
    abp.message.confirm(
        'Müşteri gelmedi olarak işaretlenecek. Bu işlem geri alınamaz!',
        'Uyarı',
        function (confirmed) {
            if (confirmed) {
                $.ajax({
                    url: `/api/app/reservation/${reservationId}/mark-as-no-show`,
                    type: 'POST',
                    success: function () {
                        abp.notify.warn('Müşteri gelmedi olarak işaretlendi!');
                        reservationDetailModal.close();
                        if (typeof calendar !== 'undefined') {
                            calendar.refetchEvents();
                        }
                        if (typeof loadDailySummary === 'function') {
                            loadDailySummary();
                        }
                    },
                    error: function (xhr) {
                        const error = xhr.responseJSON?.error?.message || 'Bir hata oluştu!';
                        abp.notify.error(error);
                    }
                });
            }
        }
    );
};


(function () {
    let calendar;

    // Sayfa yüklendiğinde
    $(function () {

        abp.event.on('app.reservation.saved', function (response) {

            reservationDetailModal.close();
            completeReservationModal.close();
            calendar.refetchEvents();
            loadDailySummary();
            loadUpcomingReservations();
            abp.notify.success('Rezervasyon başarıyla kaydedildi!');
        });

        initializeCalendar();
        bindEvents();
        loadDailySummary();
        loadUpcomingReservations();
    });

    // Takvimi başlat
    function initializeCalendar() {
        const calendarEl = document.getElementById('calendar');

        calendar = new FullCalendar.Calendar(calendarEl, {
            //initialView: 'timeGridWeek',
            locale: 'tr',
            buttonText: {   
                today: 'Bugün',
                month: 'Ay',
                week: 'Hafta',
                day: 'Gün',
                list: 'Liste'
            },
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

        // Listeden tıklama için public fonksiyon:
        reservations.showReservationDetails = showReservationDetails;
    }

    // FullCalendar event'ine tıklandığında rezervasyon detay modalını aç
    function handleEventClick(info) {
        const reservationId = info.event.id;
        showReservationDetails(reservationId);
    }

    // Rezervasyon detay modalını açan fonksiyon
    function showReservationDetails(reservationId) {
        reservationDetailModal.open({ id: reservationId });
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
        abp.notify.info('Bu özellik henüz hazır değil.');
        calendar.refetchEvents();
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

    // Yeni rezervasyon modalını aç
    function openReservationModal(isWalkIn) {
        createEditModal.open({
            id: null
        });
    }


})();
