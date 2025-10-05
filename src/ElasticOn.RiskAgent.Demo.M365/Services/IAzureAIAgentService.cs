using Azure.AI.Agents.Persistent;
using Microsoft.Agents.AI;

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
    /// Gets or creates an agent with the specified ID
    /// </summary>
    /// <param name="agentId">Optional agent ID. If null or agent doesn't exist, creates a new agent.</param>
    /// <returns>The agent ID</returns>
    Task<string> GetOrCreateAgentAsync(string? agentId = null);

    /// <summary>
    /// Gets the Elastic API key from configuration
    /// </summary>
    /// <returns>The Elastic API key</returns>
    string GetElasticApiKey();

    /// <summary>
    /// Creates MCP tool resources with authentication headers
    /// </summary>
    /// <returns>Configured ToolResources</returns>
    ToolResources CreateMcpToolResources();
}
