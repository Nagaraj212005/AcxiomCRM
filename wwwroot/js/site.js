document.addEventListener("submit", (event) => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || !form.hasAttribute("data-loading") || event.defaultPrevented) {
        return;
    }

    const button = event.submitter;
    if (!(button instanceof HTMLButtonElement) || button.type !== "submit") {
        return;
    }

    button.disabled = true;
    const spinner = document.createElement("span");
    spinner.className = "spinner-border spinner-border-sm me-2";
    spinner.setAttribute("role", "status");
    spinner.setAttribute("aria-hidden", "true");
    button.prepend(spinner);
    button.setAttribute("aria-busy", "true");
});
