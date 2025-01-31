$(document).ready(function () {
    function fetchNotifications() {
        $.get('/Notifications/GetUserNotifications', function (data) {
            if (data.length > 0) {
                $('#notificationDot').show();
                let notificationHtml = '';
                data.forEach(notification => {
                    notificationHtml += `<li class="dropdown-item">${notification.message}</li>`;
                });
                $('#notificationList').html(notificationHtml);
            } else {
                $('#notificationDot').hide();
                $('#notificationList').html('<li class="dropdown-item text-center">No new notifications</li>');
            }
        });
    }

    fetchNotifications();

    $('#inboxDropdown').on('shown.bs.dropdown', function () {
        fetchNotifications();
    });
    function loadNews() {
        $.get('/News/NewsFeed', function (data) {
            $('#news-container').html(data); // Assuming the server returns an HTML partial
        });
    }

});
