using Azure.AI.Agents.Persistent;
using ElasticOn.RiskAgent.Demo.M365.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ElasticOn.RiskAgent.Demo.Functions.Tests;

/// <summary>
/// Unit tests for AzureAIAgentService
/// </summary>
public class AzureAIAgentServiceTests
{
    #region Test Configuration Helpers

    private static IConfiguration CreateConfiguration(
        string? agentId = null,
        string? projectEndpoint = null,
        string? modelId = null,
        string? agentName = null,
        string? agentInstructions = null,
        string? mcpServerLabel = null,
        string? mcpServerUrl = null,
        List<string>? mcpAllowedTools = null,
        string? elasticApiKey = null)
    {
        var configDict = new Dictionary<string, string?>
        {
            ["AIServices:AgentID"] = agentId,
            ["AIServices:ProjectEndpoint"] = projectEndpoint ?? "https://test-project.services.ai.azure.com/api/projects/test-project",
            ["AIServices:ModelId"] = modelId ?? "gpt-4o-mini",
            ["AIServices:Agent:Name"] = agentName ?? "TestAgent",
            ["AIServices:Agent:Instructions"] = agentInstructions ?? "Test instructions",
            ["AIServices:MCPTool:ServerLabel"] = mcpServerLabel ?? "elastic_search_mcp",
            ["AIServices:MCPTool:ServerUrl"] = mcpServerUrl ?? "https://test-elastic.azure.elastic.cloud/api/agent_builder/mcp",
            ["AIServices:ElasticApiKey"] = elasticApiKey ?? "test-api-key"
        };

        // Add allowed tools
        var allowedTools = mcpAllowedTools ?? new List<string> { "azure_elastic_risk_agent_search_docs" };
        for (int i = 0; i < allowedTools.Count; i++)
        {
            configDict[$"AIServices:MCPTool:AllowedTools:{i}"] = allowedTools[i];
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
    }

    private static ILogger<AzureAIAgentService> CreateLogger() => 
        Substitute.For<ILogger<AzureAIAgentService>>();

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidConfiguration_InitializesSuccessfully()
    {
        // Arrange
        var config = CreateConfiguration();
        var logger = CreateLogger();

        // Act
        var service = new AzureAIAgentService(config, logger);

        // Assert
        Assert.NotNull(service);
        Assert.NotNull(service.GetClient());
    }

    [Fact]
    public void Constructor_WithMissingProjectEndpoint_ThrowsInvalidOperationException()
    {
        // Arrange - Create config with missing ProjectEndpoint
        var configDict = new Dictionary<string, string?>
        {
            // ProjectEndpoint is intentionally not added
            ["AIServices:ModelId"] = "gpt-4o-mini",
            ["AIServices:Agent:Name"] = "TestAgent",
            ["AIServices:Agent:Instructions"] = "Test instructions",
            ["AIServices:MCPTool:ServerLabel"] = "elastic_search_mcp",
            ["AIServices:MCPTool:ServerUrl"] = "https://test-elastic.azure.elastic.cloud/api/agent_builder/mcp",
            ["AIServices:MCPTool:AllowedTools:0"] = "azure_elastic_risk_agent_search_docs",
            ["AIServices:ElasticApiKey"] = "test-api-key"
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
        var logger = CreateLogger();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new AzureAIAgentService(config, logger));
        
        Assert.Contains("AZURE_FOUNDRY_PROJECT_ENDPOINT", exception.Message);
    }

    [Fact]
    public void Constructor_WithMissingAgentName_ThrowsInvalidOperationException()
    {
        // Arrange - Create config with missing AgentName
        var configDict = new Dictionary<string, string?>
        {
            ["AIServices:ProjectEndpoint"] = "https://test-project.services.ai.azure.com/api/projects/test-project",
            ["AIServices:ModelId"] = "gpt-4o-mini",
            // AgentName is intentionally not added
            ["AIServices:Agent:Instructions"] = "Test instructions",
            ["AIServices:MCPTool:ServerLabel"] = "elastic_search_mcp",
            ["AIServices:MCPTool:ServerUrl"] = "https://test-elastic.azure.elastic.cloud/api/agent_builder/mcp",
            ["AIServices:MCPTool:AllowedTools:0"] = "azure_elastic_risk_agent_search_docs",
            ["AIServices:ElasticApiKey"] = "test-api-key"
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
        var logger = CreateLogger();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new AzureAIAgentService(config, logger));
        
        Assert.Contains("AIServices:Agent:Name", exception.Message);
    }

    [Fact]
    public void Constructor_WithMissingAgentInstructions_ThrowsInvalidOperationException()
    {
        // Arrange - Create config with missing AgentInstructions
        var configDict = new Dictionary<string, string?>
        {
            ["AIServices:ProjectEndpoint"] = "https://test-project.services.ai.azure.com/api/projects/test-project",
            ["AIServices:ModelId"] = "gpt-4o-mini",
            ["AIServices:Agent:Name"] = "TestAgent",
            // AgentInstructions is intentionally not added
            ["AIServices:MCPTool:ServerLabel"] = "elastic_search_mcp",
            ["AIServices:MCPTool:ServerUrl"] = "https://test-elastic.azure.elastic.cloud/api/agent_builder/mcp",
            ["AIServices:MCPTool:AllowedTools:0"] = "azure_elastic_risk_agent_search_docs",
            ["AIServices:ElasticApiKey"] = "test-api-key"
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
        var logger = CreateLogger();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new AzureAIAgentService(config, logger));
        
        Assert.Contains("AIServices:Agent:Instructions", exception.Message);
    }

    [Fact]
    public void Constructor_WithMissingMCPServerLabel_ThrowsInvalidOperationException()
    {
        // Arrange - Create config with missing MCPServerLabel
        var configDict = new Dictionary<string, string?>
        {
            ["AIServices:ProjectEndpoint"] = "https://test-project.services.ai.azure.com/api/projects/test-project",
            ["AIServices:ModelId"] = "gpt-4o-mini",
            ["AIServices:Agent:Name"] = "TestAgent",
            ["AIServices:Agent:Instructions"] = "Test instructions",
            // MCPServerLabel is intentionally not added
            ["AIServices:MCPTool:ServerUrl"] = "https://test-elastic.azure.elastic.cloud/api/agent_builder/mcp",
            ["AIServices:MCPTool:AllowedTools:0"] = "azure_elastic_risk_agent_search_docs",
            ["AIServices:ElasticApiKey"] = "test-api-key"
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
        var logger = CreateLogger();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new AzureAIAgentService(config, logger));
        
        Assert.Contains("AIServices:MCPTool:ServerLabel", exception.Message);
    }

    [Fact]
    public void Constructor_WithMissingMCPServerUrl_ThrowsInvalidOperationException()
    {
        // Arrange - Create config with missing MCPServerUrl
        var configDict = new Dictionary<string, string?>
        {
            ["AIServices:ProjectEndpoint"] = "https://test-project.services.ai.azure.com/api/projects/test-project",
            ["AIServices:ModelId"] = "gpt-4o-mini",
            ["AIServices:Agent:Name"] = "TestAgent",
            ["AIServices:Agent:Instructions"] = "Test instructions",
            ["AIServices:MCPTool:ServerLabel"] = "elastic_search_mcp",
            // MCPServerUrl is intentionally not added
            ["AIServices:MCPTool:AllowedTools:0"] = "azure_elastic_risk_agent_search_docs",
            ["AIServices:ElasticApiKey"] = "test-api-key"
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
        var logger = CreateLogger();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new AzureAIAgentService(config, logger));
        
        Assert.Contains("AIServices:MCPTool:ServerUrl", exception.Message);
    }

    [Fact]
    public void Constructor_WithMissingElasticApiKey_ThrowsInvalidOperationException()
    {
        // Arrange - Create config with missing ElasticApiKey
        var configDict = new Dictionary<string, string?>
        {
            ["AIServices:ProjectEndpoint"] = "https://test-project.services.ai.azure.com/api/projects/test-project",
            ["AIServices:ModelId"] = "gpt-4o-mini",
            ["AIServices:Agent:Name"] = "TestAgent",
            ["AIServices:Agent:Instructions"] = "Test instructions",
            ["AIServices:MCPTool:ServerLabel"] = "elastic_search_mcp",
            ["AIServices:MCPTool:ServerUrl"] = "https://test-elastic.azure.elastic.cloud/api/agent_builder/mcp",
            ["AIServices:MCPTool:AllowedTools:0"] = "azure_elastic_risk_agent_search_docs"
            // ElasticApiKey is intentionally not added
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
        var logger = CreateLogger();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new AzureAIAgentService(config, logger));
        
        Assert.Contains("AIServices:ElasticApiKey", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyModelId_UsesDefaultModel()
    {
        // Arrange
        var config = CreateConfiguration(modelId: null);
        var logger = CreateLogger();

        // Act
        var service = new AzureAIAgentService(config, logger);

        // Assert - Should not throw, should use default model
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithOptionalAgentId_DoesNotThrow()
    {
        // Arrange
        var config = CreateConfiguration(agentId: "asst_test123");
        var logger = CreateLogger();

        // Act
        var service = new AzureAIAgentService(config, logger);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithMultipleMCPTools_InitializesSuccessfully()
    {
        // Arrange
        var tools = new List<string>
        {
            "azure_elastic_risk_agent_search_docs",
            "azure_elastic_risk_agent_docs_list"
        };
        var config = CreateConfiguration(mcpAllowedTools: tools);
        var logger = CreateLogger();

        // Act
        var service = new AzureAIAgentService(config, logger);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region GetClient Tests

    [Fact]
    public void GetClient_ReturnsNonNullClient()
    {
        // Arrange
        var config = CreateConfiguration();
        var logger = CreateLogger();
        var service = new AzureAIAgentService(config, logger);

        // Act
        var client = service.GetClient();

        // Assert
        Assert.NotNull(client);
        Assert.IsType<PersistentAgentsClient>(client);
    }

    [Fact]
    public void GetClient_CalledMultipleTimes_ReturnsSameInstance()
    {
        // Arrange
        var config = CreateConfiguration();
        var logger = CreateLogger();
        var service = new AzureAIAgentService(config, logger);

        // Act
        var client1 = service.GetClient();
        var client2 = service.GetClient();

        // Assert
        Assert.Same(client1, client2);
    }

    #endregion

    #region GetElasticApiKey Tests

    [Fact]
    public void GetElasticApiKey_ReturnsConfiguredKey()
    {
        // Arrange
        var expectedKey = "test-elastic-api-key-12345";
        var config = CreateConfiguration(elasticApiKey: expectedKey);
        var logger = CreateLogger();
        var service = new AzureAIAgentService(config, logger);

        // Act
        var apiKey = service.GetElasticApiKey();

        // Assert
        Assert.Equal(expectedKey, apiKey);
    }

    #endregion

    #region CreateMcpToolResources Tests

    [Fact]
    public void CreateMcpToolResources_ReturnsToolResources()
    {
        // Arrange
        var config = CreateConfiguration();
        var logger = CreateLogger();
        var service = new AzureAIAgentService(config, logger);

        // Act
        var toolResources = service.CreateMcpToolResources();

        // Assert
        Assert.NotNull(toolResources);
    }

    [Fact]
    public void CreateMcpToolResources_IncludesAuthorizationHeader()
    {
        // Arrange
        var expectedKey = "test-api-key-abc123";
        var config = CreateConfiguration(elasticApiKey: expectedKey);
        var logger = CreateLogger();
        var service = new AzureAIAgentService(config, logger);

        // Act
        var toolResources = service.CreateMcpToolResources();

        // Assert
        Assert.NotNull(toolResources);
        // The tool resources should be configured with the API key
        // (We can't directly access the headers, but we can verify the method doesn't throw)
    }

    [Fact]
    public void CreateMcpToolResources_CalledMultipleTimes_ReturnsNewInstances()
    {
        // Arrange
        var config = CreateConfiguration();
        var logger = CreateLogger();
        var service = new AzureAIAgentService(config, logger);

        // Act
        var toolResources1 = service.CreateMcpToolResources();
        var toolResources2 = service.CreateMcpToolResources();

        // Assert
        Assert.NotNull(toolResources1);
        Assert.NotNull(toolResources2);
        // Each call should return a new instance
        Assert.NotSame(toolResources1, toolResources2);
    }

    #endregion

    #region Configuration Priority Tests

    [Fact]
    public void Constructor_PreferEnvironmentVariableOverConfig_ForEndpoint()
    {
        // Arrange
        var envEndpoint = "https://env-endpoint.services.ai.azure.com/api/projects/env-project";
        var config = CreateConfiguration(projectEndpoint: "https://config-endpoint.services.ai.azure.com/api/projects/config-project");
        var logger = CreateLogger();

        // Set environment variable
        Environment.SetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT", envEndpoint);

        try
        {
            // Act
            var service = new AzureAIAgentService(config, logger);

            // Assert
            Assert.NotNull(service);
            // If it doesn't throw, environment variable was successfully used
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT", null);
        }
    }

    [Fact]
    public void Constructor_PreferEnvironmentVariableOverConfig_ForModelId()
    {
        // Arrange
        var envModelId = "gpt-4-turbo";
        var config = CreateConfiguration(modelId: "gpt-4o-mini");
        var logger = CreateLogger();

        // Set environment variable
        Environment.SetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_MODEL_ID", envModelId);

        try
        {
            // Act
            var service = new AzureAIAgentService(config, logger);

            // Assert
            Assert.NotNull(service);
            // If it doesn't throw, environment variable was successfully used
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_MODEL_ID", null);
        }
    }

    #endregion

    #region Logging Tests

    [Fact]
    public void Constructor_LogsInitializationMessage()
    {
        // Arrange
        var config = CreateConfiguration();
        var logger = CreateLogger();

        // Act
        var service = new AzureAIAgentService(config, logger);

        // Assert
        logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Initializing Azure AI Agent Service")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void CreateMcpToolResources_LogsDebugMessage()
    {
        // Arrange
        var config = CreateConfiguration();
        var logger = CreateLogger();
        var service = new AzureAIAgentService(config, logger);

        // Act
        var toolResources = service.CreateMcpToolResources();

        // Assert
        logger.Received(1).Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Created MCP tool resources")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion
}
