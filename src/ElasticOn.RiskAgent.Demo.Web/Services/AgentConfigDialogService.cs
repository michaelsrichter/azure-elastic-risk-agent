namespace ElasticOn.RiskAgent.Demo.Web.Services;

/// <summary>
/// Simple event service to allow NavMenu to request the Internal page to open the agent configuration dialog.
/// </summary>
public class AgentConfigDialogService
{
    public event Action? OnOpenRequested;

    public void RequestOpen()
    {
        OnOpenRequested?.Invoke();
    }
}
