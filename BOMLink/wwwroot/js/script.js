// Fubction to toggle the theme
document.addEventListener("DOMContentLoaded", function () {
    const themeToggle = document.getElementById("themeToggle");
    const themeIcon = document.getElementById("theme-icon");

    if (!themeToggle || !themeIcon) return;

    function setTheme(theme) {
        document.documentElement.setAttribute("data-bs-theme", theme);

        try {
            localStorage.setItem("theme", theme);
        } catch (error) {
            console.warn("LocalStorage blocked, using sessionStorage.");
            sessionStorage.setItem("theme", theme);
        }

        console.log("Saved Theme:", theme);
        themeIcon.className = theme === "dark" ? "fa-solid fa-moon" : "fa-solid fa-sun";
        themeToggle.checked = (theme === "dark");
    }

    let savedTheme = "light";

    try {
        savedTheme = localStorage.getItem("theme") || sessionStorage.getItem("theme") || "light";
    } catch (error) {
        console.warn("Storage access issue. Falling back to default.");
        savedTheme = "light";
    }

    console.log("Loaded Theme:", savedTheme);
    setTheme(savedTheme);

    themeToggle.addEventListener("change", function () {
        const newTheme = themeToggle.checked ? "dark" : "light";
        setTheme(newTheme);
    });
});

// Function to display a temporary message
setTimeout(function () {
    var messageElement = document.getElementById('tempMessage');
    if (messageElement) {
        messageElement.style.display = 'none';
    }
}, 5000);

// Function to display a tooltip
document.addEventListener("DOMContentLoaded", function () {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
});


