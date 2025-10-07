using Azure.AI.Agents.Persistent;
using Azure.Identity;

namespace ElasticOn.RiskAgent.Demo.M365.Services;

/// <summary>
/// Service for managing Azure AI Foundry Agent
/// </summary>
public class AzureAIAgentService : IAzureAIAgentService
{
    private readonly PersistentAgentsClient _client;
    private readonly ILogger<AzureAIAgentService> _logger;
    private readonly string _endpoint;
    private readonly string _model;
    private readonly string? _configuredAgentId;
    private readonly string _agentName;
    private readonly string _agentInstructions;
    private readonly string _mcpServerLabel;
    private readonly string _mcpServerUrl;
    private readonly List<string> _mcpAllowedTools;
    private readonly string _elasticApiKey;

    public AzureAIAgentService(IConfiguration configuration, ILogger<AzureAIAgentService> logger)
    {
        _logger = logger;
        
        // Read endpoint from environment variable or configuration
        _endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT") 
            ?? configuration["AIServicesProjectEndpoint"]
            ?? throw new InvalidOperationException("AZURE_FOUNDRY_PROJECT_ENDPOINT is not set.");

        _model = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_MODEL_ID") 
            ?? configuration["AIServicesModelId"]
            ?? "gpt-4.1-mini";

        // Read optional pre-configured Agent ID from configuration
        _configuredAgentId = configuration["AIServicesAgentID"];

        // Read agent configuration
        _agentName = configuration["AIServicesAgentName"] 
            ?? throw new InvalidOperationException("AIServicesAgentName is not configured in appsettings.json");
        
        _agentInstructions = configuration["AIServicesAgentInstructions"] 
            ?? throw new InvalidOperationException("AIServicesAgentInstructions is not configured in appsettings.json");

        // Read MCP tool configuration
        _mcpServerLabel = configuration["AIServicesMCPToolServerLabel"] 
            ?? throw new InvalidOperationException("AIServicesMCPToolServerLabel is not configured in appsettings.json");
        
        _mcpServerUrl = configuration["AIServicesMCPToolServerUrl"] 
            ?? throw new InvalidOperationException("AIServicesMCPToolServerUrl is not configured in appsettings.json");
        
        // Read allowed tools from indexed configuration keys
        _mcpAllowedTools = new List<string>();
        int i = 0;
        while (true)
        {
            var tool = configuration[$"AIServicesMCPToolAllowedTools{i}"];
            if (string.IsNullOrEmpty(tool))
                break;
            _mcpAllowedTools.Add(tool);
            i++;
        }
        
        if (_mcpAllowedTools.Count == 0)
            throw new InvalidOperationException("AIServicesMCPToolAllowedTools is not configured in appsettings.json");

        // Read Elastic API key
        _elasticApiKey = configuration["AIServicesElasticApiKey"] 
            ?? throw new InvalidOperationException("AIServicesElasticApiKey is not configured in appsettings.json");

        _logger.LogInformation("Initializing Azure AI Agent Service with endpoint: {Endpoint}", _endpoint);
        
        // Create the PersistentAgentsClient using AzureCliCredential
        _client = new PersistentAgentsClient(_endpoint, new AzureCliCredential());
    }

    /// <summary>
    /// Gets the PersistentAgentsClient
    /// </summary>
    public PersistentAgentsClient GetClient()
    {
        return _client;
    }

    /// <summary>
    /// Gets or creates an agent with the specified ID
    /// </summary>
    /// <param name="agentId">Optional agent ID. If null or agent doesn't exist, creates a new agent.</param>
    /// <returns>The agent ID</returns>
    public async Task<string> GetOrCreateAgentAsync(string? agentId = null)
    {
        // First priority: Check if there's a pre-configured Agent ID in appsettings
        if (!string.IsNullOrEmpty(_configuredAgentId))
        {
            try
            {
                var existingAgent = await _client.Administration.GetAgentAsync(_configuredAgentId);
                if (existingAgent != null)
                {
                    _logger.LogInformation("Using pre-configured agent from appsettings with ID: {AgentId}", _configuredAgentId);
                    return _configuredAgentId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Pre-configured agent with ID {AgentId} not found. Will create new agent.", _configuredAgentId);
            }
        }

        // Second priority: If agentId parameter is provided, try to retrieve it
        if (!string.IsNullOrEmpty(agentId))
        {
            try
            {
                var existingAgent = await _client.Administration.GetAgentAsync(agentId);
                if (existingAgent != null)
                {
                    _logger.LogInformation("Using existing agent with ID: {AgentId}", agentId);
                    return agentId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Agent with ID {AgentId} not found. Creating new agent.", agentId);
            }
        }

        // No existing agent found, create new agent with MCP tool configuration
        _logger.LogInformation("Creating new agent with name: {AgentName}", _agentName);
        
        var mcpTool = CreateMcpToolDefinition();
        var toolResources = CreateMcpToolResources();
        
        var agentMetadata = await _client.Administration.CreateAgentAsync(
            model: _model,
            name: _agentName,
            instructions: _agentInstructions,
            tools: [mcpTool],
            toolResources: toolResources);

        var newAgentId = agentMetadata.Value.Id;
        _logger.LogInformation("Successfully created agent with ID: {AgentId}", newAgentId);
        
        return newAgentId;
    }

    /// <summary>
    /// Creates MCP tool definition with configured allowed tools
    /// </summary>
    private MCPToolDefinition CreateMcpToolDefinition()
    {
        var mcpTool = new MCPToolDefinition(
            serverLabel: _mcpServerLabel,
            serverUrl: _mcpServerUrl);

        foreach (var tool in _mcpAllowedTools)
        {
            mcpTool.AllowedTools.Add(tool);
        }

        return mcpTool;
    }

    /// <summary>
    /// Gets the Elastic API key from configuration
    /// </summary>
    /// <returns>The Elastic API key</returns>
    public string GetElasticApiKey() => _elasticApiKey;

    /// <summary>
    /// Creates MCP tool resources with authentication headers
    /// </summary>
    /// <returns>Configured ToolResources</returns>
    public ToolResources CreateMcpToolResources()
    {
        var mcpToolResource = new MCPToolResource(serverLabel: _mcpServerLabel)
        {
            RequireApproval = new MCPApproval("never")
        };
        
        // Add authorization header for Elastic API
        mcpToolResource.UpdateHeader("Authorization", $"ApiKey {_elasticApiKey}");
        
        _logger.LogDebug("Created MCP tool resources with server label: {ServerLabel}", _mcpServerLabel);
        
        return mcpToolResource.ToToolResources();
    }
}
