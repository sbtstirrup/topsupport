// Colocated JS module for RichTextEditor.razor. Contenteditable + document.execCommand,
// matching the rte()/ACT.rte behaviour in topsupport-app.js (bold/italic/underline/strike/
// lists/link via execCommand; link is the only toolbar command here that needs a prompt --
// kept to a plain window.prompt for simplicity since the demo's popover form is out of scope
// for this shared-infrastructure pass).

const editors = new Map();

export function initEditor(element, dotNetRef, initialHtml) {
    element.innerHTML = initialHtml || "";
    const handler = () => dotNetRef.invokeMethodAsync("OnContentChanged", element.innerHTML);
    element.addEventListener("input", handler);
    editors.set(element, handler);
}

export function getHtml(element) {
    return element.innerHTML;
}

export function setHtml(element, html) {
    element.innerHTML = html || "";
}

/// Appends HTML at the end of the editor's current content rather than replacing it --
/// used for "insert canned response without overwriting what's already been typed".
export function appendHtml(element, html) {
    element.innerHTML = (element.innerHTML || "") + (html || "");
}

export function execCommand(element, command, value) {
    element.focus();
    if (command === "link") {
        const url = (window.prompt("Link address (https://)") || "").trim();
        // Same schemes the Api's TicketHtmlSanitizer keeps: never javascript: or data: links.
        if (!/^(https?:\/\/|mailto:)/i.test(url)) return;
        document.execCommand("createLink", false, url);
        return;
    }
    document.execCommand(command, false, value ?? null);
}

export function destroy(element) {
    const handler = editors.get(element);
    if (handler) {
        element.removeEventListener("input", handler);
        editors.delete(element);
    }
}
