using System;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace ElasticOn.RiskAgent.Demo.M365.Services;

/// <summary>
/// Service for managing Azure AI Foundry Agent
/// </summary>
public class AzureAIAgentService : IAzureAIAgentService
{
    private readonly PersistentAgentsClient _client;
    private readonly string _agentId;
    private readonly ILogger<AzureAIAgentService> _logger;

    public AzureAIAgentService(IConfiguration configuration, ILogger<AzureAIAgentService> logger)
    {
        _logger = logger;
        
        // Read configuration
        var projectEndpoint = configuration["AIServices:ProjectEndpoint"];
        _agentId = configuration["AIServices:AgentID"];

        if (string.IsNullOrEmpty(projectEndpoint))
        {
            throw new InvalidOperationException("AIServices:ProjectEndpoint is not configured in appsettings.json");
        }

        if (string.IsNullOrEmpty(_agentId))
        {
            throw new InvalidOperationException("AIServices:AgentID is not configured in appsettings.json");
        }

        _logger.LogInformation("Initializing Azure AI Agent Service with endpoint: {Endpoint}", projectEndpoint);
        
        // Create the PersistentAgentsClient using DefaultAzureCredential
        // This will automatically use the appropriate credentials based on the environment
        _client = new PersistentAgentsClient(projectEndpoint, new DefaultAzureCredential());
    }

    /// <summary>
    /// Gets the PersistentAgentsClient
    /// </summary>
    public PersistentAgentsClient GetClient()
    {
        return _client;
    }

    /// <summary>
    /// Gets the agent ID from configuration
    /// </summary>
    public string GetAgentId()
    {
        return _agentId;
    }
}
