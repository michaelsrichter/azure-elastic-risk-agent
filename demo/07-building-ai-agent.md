# 7. Building the AI Agent

## Microsoft Agent Framework & Tool-Oriented Design

### Azure AI Foundry

[Azure AI Foundry](https://azure.microsoft.com/en-us/products/ai-foundry) is Microsoft's unified AI platform for building, evaluating, and deploying generative AI applications. It provides:

- **Agent Development**: Pre-built templates and SDKs for creating AI agents
- **Model Management**: Access to OpenAI models (GPT-4o, GPT-4o-mini) and open-source alternatives
- **Enterprise Integration**: Seamless connectivity with Azure services and M365
- **Responsible AI Tools**: Built-in content safety, prompt shields, and evaluation frameworks
- **Production Deployment**: Scalable infrastructure for enterprise AI workloads

### Microsoft Agent Framework

The [Microsoft Agent Framework](https://devblogs.microsoft.com/foundry/introducing-microsoft-agent-framework-the-open-source-engine-for-agentic-ai-apps/) is an open-source engine that powers agentic AI applications with:

- **Multi-Turn Conversations** with state management
- **Tool Calling & Orchestration** via Model Context Protocol (MCP)
- **Streaming Responses** for real-time interactions
- **Multi-Model Support** across various LLMs
- **Enterprise Security** and content safety guardrails

### How It Works in This Solution

1. **User Query** → Agent analyzes intent
2. **Tool Selection** → Dynamically calls Elasticsearch search tools via MCP
3. **Result Synthesis** → Combines search results with LLM reasoning
4. **Response Delivery** → Returns contextual, grounded answers

```mermaid
%%{init: {'theme':'base', 'themeVariables': { 'fontSize':'16px', 'fontFamily':'arial'}}}%%
flowchart TD
  A["<b>1. User Query</b><br/>💬<br/>Natural Language Input"]
  B["<b>2. AI Agent</b><br/>🤖<br/>LLM Reasoning Engine"]
  C{"<b>3. Need External Data?</b><br/>🔍<br/>Decision Point"}
  D["<b>4. MCP Tool Discovery</b><br/>🔧<br/>Available Tools Query"]
  E["<b>5. Elastic Search Tool</b><br/>🔎<br/>Hybrid Search Execution"]
  F["<b>6. Results to Agent</b><br/>📊<br/>Documents + Metadata"]
  G["<b>7. Compose Response</b><br/>✍️<br/>LLM Synthesis"]
  H["<b>8. Response to User</b><br/>💬<br/>Contextual Answer"]

  A ==>|Query| B
  B ==>|Analyze| C
  C ==>|Yes| D
  D ==>|Discover| E
  E ==>|Search| F
  F ==>|Context| G
  C -.->|No| G
  G ==>|Answer| H

  %% Styling with bold colors and high contrast
  classDef userStyle fill:#0078D4,stroke:#004578,stroke-width:3px,color:#fff,font-weight:bold;
  classDef agentStyle fill:#5E5E5E,stroke:#2C2C2C,stroke-width:3px,color:#fff,font-weight:bold;
  classDef decisionStyle fill:#FFB900,stroke:#A77800,stroke-width:3px,color:#000,font-weight:bold;
  classDef mcpStyle fill:#742774,stroke:#4B1A4B,stroke-width:3px,color:#fff,font-weight:bold;
  classDef elasticStyle fill:#FEC514,stroke:#CB9A10,stroke-width:3px,color:#000,font-weight:bold;
  classDef resultsStyle fill:#00B294,stroke:#00786B,stroke-width:3px,color:#fff,font-weight:bold;
  classDef composeStyle fill:#8B5CF6,stroke:#6B21A8,stroke-width:3px,color:#fff,font-weight:bold;
  classDef responseStyle fill:#107C10,stroke:#0B5A0B,stroke-width:3px,color:#fff,font-weight:bold;
  
  class A userStyle;
  class B agentStyle;
  class C decisionStyle;
  class D mcpStyle;
  class E elasticStyle;
  class F resultsStyle;
  class G composeStyle;
  class H responseStyle;
```

### Extensibility

Supports adding new data sources, AI services, custom logic, and multi-agent patterns.

---

## Navigation

- [← Previous: Vectorization & Semantic Search](./06-vectorization-semantic-search.md)
- [Back to Demo Index](./README.md)
- [Next: Elastic MCP Integration →](./08-elastic-mcp-integration.md)
