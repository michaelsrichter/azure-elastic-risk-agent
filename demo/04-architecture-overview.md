# 4. Solution Architecture Overview

## Big Picture: Data → AI Agent → Teams


- High-level flow:
  - SharePoint → Power Automate → Azure Function → Elasticsearch.
  - AI Agent (Microsoft Agent Framework) → MCP Tools → Teams/M365 Copilot.
- Key principles:
  - Keep data inside Azure.
  - Use enterprise authentication and Teams for user access.
  - Add a Responsible AI layer for safety and compliance.

```mermaid
%%{init: {'theme':'base', 'themeVariables': { 'fontSize':'16px', 'fontFamily':'arial'}}}%%
flowchart TB
  %% Nodes
  SP["<b>SharePoint</b><br/>(Data Source)<br/>📁"]
  PA["<b>Power Automate</b><br/>(Orchestration)<br/>⚡"]
  AF["<b>Azure Function</b><br/>(Ingest & Transform)<br/>⚙️"]
  ES["<b>Elasticsearch</b><br/>(Azure Native - Elastic)<br/>🔍"]
  AG["<b>AI Agent</b><br/>(Microsoft Agent Framework)<br/>🤖"]
  TE["<b>Teams / M365 Copilot</b><br/>(User Interface)<br/>💬"]

  %% Flow - Using thick arrows
  SP ==>|For Each PDF| PA
  PA ==>|File Properties<br/>& Content| AF
  AF ==>|Processed<br/>Documents| ES
  ES ==>|Search Results<br/>via MCP| AG
  AG ==>|AI Response| TE

  %% Security annotation
  AF -.-|🔐 Azure Private Link| ES

  %% Styling with bold colors and high contrast
  classDef sharepoint fill:#0078D4,stroke:#004578,stroke-width:3px,color:#fff,font-weight:bold;
  classDef automation fill:#742774,stroke:#4B1A4B,stroke-width:3px,color:#fff,font-weight:bold;
  classDef azureFunc fill:#00A4EF,stroke:#006D9E,stroke-width:3px,color:#fff,font-weight:bold;
  classDef elastic fill:#FEC514,stroke:#CB9A10,stroke-width:3px,color:#000,font-weight:bold;
  classDef microsoft fill:#5E5E5E,stroke:#2C2C2C,stroke-width:3px,color:#fff,font-weight:bold;
  classDef teams fill:#464EB8,stroke:#2C3576,stroke-width:3px,color:#fff,font-weight:bold;
  
  class SP sharepoint;
  class PA automation;
  class AF azureFunc;
  class ES elastic;
  class AG microsoft;
  class TE teams;
```


---

## Navigation

- [← Previous: Why Elasticsearch for Vector Search](./03-why-elasticsearch.md)
- [Back to Demo Index](./README.md)
- [Next: Ingestion Pipeline →](./05-ingestion-pipeline.md)
