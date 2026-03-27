const storageKey = "zerodawn-theme";

export function getTheme() {
    return window.localStorage.getItem(storageKey)
        ?? document.documentElement.getAttribute("data-theme")
        ?? "";
}

export function setTheme(theme) {
    if (!theme) {
        document.documentElement.removeAttribute("data-theme");
        window.localStorage.removeItem(storageKey);
        return;
    }

    document.documentElement.setAttribute("data-theme", theme);
    window.localStorage.setItem(storageKey, theme);
}
