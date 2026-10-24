// Colocated JS module for ModalHost.razor, following the same @Assets[...] +
// IJSRuntime.InvokeAsync<IJSObjectReference>("import", ...) pattern as ReconnectModal.razor.js
// (the project's only prior JS-interop precedent). Handles Escape-to-close and click-on-backdrop
// -to-close; focus trapping is intentionally not implemented (nice-to-have, not required).

let keydownHandler = null;

export function attach(backdropElement, dotNetRef) {
    keydownHandler = (e) => {
        if (e.key === "Escape") {
            dotNetRef.invokeMethodAsync("OnEscape");
        }
    };
    document.addEventListener("keydown", keydownHandler);
}

export function detach() {
    if (keydownHandler) {
        document.removeEventListener("keydown", keydownHandler);
        keydownHandler = null;
    }
}
