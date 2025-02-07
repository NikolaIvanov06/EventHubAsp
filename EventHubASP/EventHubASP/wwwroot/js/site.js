    $('#inboxDropdown').on('shown.bs.dropdown', function () {
        fetchNotifications();
    });
    function loadNews() {
        $.get('/News/NewsFeed', function (data) {
            $('#news-container').html(data);
        });
    }

