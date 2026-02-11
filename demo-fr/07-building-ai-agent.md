
# 7. Création de l’agent IA

## Microsoft Agent Framework &amp; Conception orientée outils

### Azure AI Foundry

[Azure AI Foundry](https://azure.microsoft.com/en-us/products/ai-foundry) est la plateforme unifiée de Microsoft pour créer, évaluer et déployer des applications d’IA générative. Elle offre :

- **Développement d’agents** : Modèles préconstruits et SDK pour créer des agents IA  
- **Gestion des modèles** : Accès aux modèles OpenAI (GPT‑4o, GPT‑4o‑mini) et aux alternatives open‑source  
- **Intégration entreprise** : Connexion fluide avec les services Azure et M365  
- **Outils d’IA responsable** : Sécurité de contenu intégrée, protections de prompts et cadres d’évaluation  
- **Déploiement en production** : Infrastructure scalable pour les charges de travail IA d’entreprise  

### Microsoft Agent Framework

Le [Microsoft Agent Framework](https://devblogs.microsoft.com/foundry/introducing-microsoft-agent-framework-the-open-source-engine-for-agentic-ai-apps/) est un moteur open‑source qui alimente les applications IA agentiques avec :

- **Conversations multi‑tours** avec gestion d’état  
- **Appels d’outils &amp; orchestration** via Model Context Protocol (MCP)  
- **Réponses en streaming** pour des interactions en temps réel  
- **Support multi‑modèles** pour divers LLM  
- **Sécurité entreprise** et garde‑fous de sûreté du contenu  

### Fonctionnement dans cette solution

1. **Requête utilisateur** → L’agent analyse l’intention  
2. **Sélection d’outil** → Appelle dynamiquement les outils Elastic via MCP  
3. **Synthèse des résultats** → Combine les résultats de recherche avec le raisonnement LLM  
4. **Livraison de la réponse** → Renvoie des réponses contextualisées et justifiées  

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
