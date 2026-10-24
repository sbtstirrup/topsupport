using Microsoft.AspNetCore.Components;

namespace TopSupport.Blazor.Services;

/// <summary>
/// Scoped modal host state, mirroring S.modal/openModal/closeModal in topsupport-app.js but
/// expressed as a RenderFragment instead of a MODALS[type](data) lookup -- callers build their
/// own modal markup (title/body/footer) as a RenderFragment and pass it to Show. ModalHost.razor
/// subscribes to Changed and renders the current fragment inside the .modal-back/.modal chrome.
/// </summary>
public class ModalState
{
    public RenderFragment? Content { get; private set; }
    public bool IsOpen => Content is not null;

    /// <summary>Optional wide variant (mirrors the demo's modal.wide, e.g. for AgentTicketDetail's
    /// bigger modals). Defaults to the standard 560px-max-width dialog.</summary>
    public bool Wide { get; private set; }

    public event Action? Changed;

    public void Show(RenderFragment content, bool wide = false)
    {
        Content = content;
        Wide = wide;
        Changed?.Invoke();
    }

    public void Close()
    {
        Content = null;
        Wide = false;
        Changed?.Invoke();
    }
}
