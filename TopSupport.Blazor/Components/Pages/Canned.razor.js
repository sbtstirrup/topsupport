// Colocated JS module for Canned.razor. The canned-response body is a plain <textarea> (not
// contenteditable), so inserting a placeholder token at the cursor needs direct access to
// selectionStart/selectionEnd -- Blazor's data binding alone can't do this. Mirrors the demo's
// ACT['cn-ph'] handler (splice the token into the tracked caret position, then restore the
// selection so repeated inserts land where the user expects).

export function insertAtCursor(textarea, text) {
    if (!textarea) return textarea?.value ?? "";

    const start = textarea.selectionStart ?? textarea.value.length;
    const end = textarea.selectionEnd ?? textarea.value.length;
    const value = textarea.value ?? "";

    textarea.value = value.slice(0, start) + text + value.slice(end);

    const caret = start + text.length;
    textarea.focus();
    textarea.setSelectionRange(caret, caret);

    // Blazor's @bind:event="oninput" only picks up changes dispatched through a real input
    // event, not direct .value assignment.
    textarea.dispatchEvent(new Event("input", { bubbles: true }));

    return textarea.value;
}
