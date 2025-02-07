const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

async function startSignalR() {
    try {
        await connection.start();
        console.log("SignalR Connected");
    } catch (err) {
        console.error("SignalR Connection Error:", err.toString());
        setTimeout(startSignalR, 5000);
    }
}

startSignalR();

connection.on("ReceiveNotification", (message) => {
    let notificationDot = document.getElementById("notificationDot");
    if (notificationDot) {
        notificationDot.style.display = "inline";
    }
});

connection.on("ReceiveNews", (news) => {
    let newsFeed = document.getElementById("newsFeed");
    if (newsFeed) {
        let newsItem = document.createElement("div");
        newsItem.className = "news-item";
        newsItem.innerHTML = `
            <h3>${news.title}</h3>
            <p>${news.content}</p>
            <small>Published on: ${new Date(news.publishedDate).toLocaleString()}</small>
        `;
        newsFeed.prepend(newsItem);

        fetch("/api/notification/mark-read", { method: "POST" });
    } else {
        let notificationDot = document.getElementById("notificationDot");
        if (notificationDot) {
            notificationDot.style.display = "inline";
        }
    }
});

async function checkNotifications() {
    try {
        let response = await fetch("/api/notification/unread-count");
        let data = await response.json();
        let notificationDot = document.getElementById("notificationDot");

        if (notificationDot) {
            notificationDot.style.display = data.count > 0 ? "inline" : "none";
        }
    } catch (error) {
        console.error("Error fetching notifications:", error);
    }
}

function waitForElements(selector, callback) {
    let elements = document.querySelector(selector);
    if (elements) {
        callback();
    } else {
        setTimeout(() => waitForElements(selector, callback), 100);
    }
}

document.addEventListener("DOMContentLoaded", () => {
    waitForElements("#notificationDot", checkNotifications);

    waitForElements("#inboxLink", () => {
        document.getElementById("inboxLink").addEventListener("click", async () => {
            await fetch("/api/notification/mark-read", { method: "POST" });
            let notificationDot = document.getElementById("notificationDot");
            if (notificationDot) {
                notificationDot.style.display = "none";
            }
        });
    });
});