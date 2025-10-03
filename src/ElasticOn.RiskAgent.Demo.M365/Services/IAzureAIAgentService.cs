using Azure.AI.Agents.Persistent;

namespace ElasticOn.RiskAgent.Demo.M365.Services;

/// <summary>
/// Interface for Azure AI Foundry Agent service
/// </summary>
public interface IAzureAIAgentService
{
    /// <summary>
    /// Gets the PersistentAgentsClient
    /// </summary>
    PersistentAgentsClient GetClient();

    /// <summary>
    /// Gets the agent ID from configuration
    /// </summary>
    string GetAgentId();
}
