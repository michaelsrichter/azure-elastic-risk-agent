using Azure.AI.Agents.Persistent;

namespace ElasticOn.RiskAgent.Demo.Functions.Services;

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
    /// Gets or creates a dynamic agent by name, with custom instructions and MCP tools.
    /// If an agent with the given name already exists, it is updated if instructions or tools have changed.
    /// </summary>
    /// <param name="agentName">Unique agent name to look up or create.</param>
    /// <param name="agentInstructions">System instructions for the agent.</param>
    /// <param name="mcpTools">List of MCP tool names the agent is allowed to use.</param>
    /// <returns>The agent ID</returns>
    Task<string> GetOrCreateDynamicAgentAsync(string agentName, string agentInstructions, IList<string> mcpTools);

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
