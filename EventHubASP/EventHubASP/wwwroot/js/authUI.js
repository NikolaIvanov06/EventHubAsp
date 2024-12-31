document.addEventListener("DOMContentLoaded", () => {
    const forms = document.querySelectorAll("form");

    forms.forEach((form) => {
        form.addEventListener("submit", (e) => {
            const inputs = form.querySelectorAll("input");
            let isValid = true;

            inputs.forEach((input) => {
                if (input.value.trim() === "") {
                    isValid = false;
                    input.classList.add("error-border");
                } else {
                    input.classList.remove("error-border");
                }
            });

            if (!isValid) {
                e.preventDefault();
                alert("Please fill in all fields.");
            }
        });
    });
});
