
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
  A["&lt;b&gt;1. Requête utilisateur&lt;/b&gt;&lt;br/&gt;💬&lt;br/&gt;Entrée en langage naturel"]
  B["&lt;b&gt;2. Agent IA&lt;/b&gt;&lt;br/&gt;🤖&lt;br/&gt;Moteur de raisonnement LLM"]
  C{"&lt;b&gt;3. Besoin de données externes ?&lt;/b&gt;&lt;br/&gt;🔍&lt;br/&gt;Point de décision"}
  D["&lt;b&gt;4. Découverte des outils MCP&lt;/b&gt;&lt;br/&gt;🔧&lt;br/&gt;Requête des outils disponibles"]
  E["&lt;b&gt;5. Outil de recherche Elastic&lt;/b&gt;&lt;br/&gt;🔎&lt;br/&gt;Exécution de la recherche hybride"]
  F["&lt;b&gt;6. Résultats vers l’agent&lt;/b&gt;&lt;br/&gt;📊&lt;br/&gt;Documents + Métadonnées"]
  G["&lt;b&gt;7. Composer réponse&lt;/b&gt;&lt;br/&gt;✍️&lt;br/&gt;Synthèse LLM"]
  H["&lt;b&gt;8. Réponse à l’utilisateur&lt;/b&gt;&lt;br/&gt;💬&lt;br/&gt;Réponse contextuelle"]

  A ==&gt;|Requête| B
  B ==&gt;|Analyse| C
  C ==&gt;|Oui| D
  D ==&gt;|Découverte| E
  E ==&gt;|Recherche| F
  F ==&gt;|Contexte| G
  C -.-&gt;|Non| G
  G ==&gt;|Réponse| H

  %% Styling with bold colors and high contrast
  classDef userStyle fill:#0078D4,stroke:#004578,stroke-width:3px,color:#fff,font-weight:bold;
  classDef agentStyle fill:#5E5E5E,stroke:#2C2C2C,stroke-width:3px;color:#fff,font-weight:bold;
  classDef decisionStyle fill:#FFB900,stroke:#A77800,stroke-width:3px;color:#000,font-weight:bold;
  classDef mcpStyle fill:#742774,stroke:#4B1A4B,stroke-width:3px,color:#fff,font-weight:bold;
  classDef elasticStyle fill:#FEC514,stroke:#CB9A10,stroke-width:3px,color:#000,font-weight:bold;
  classDef resultsStyle fill:#00B294,stroke:#00786B,stroke-width:3px;color:#fff,font-weight:bold;
  classDef composeStyle fill:#8B5CF6,stroke:#6B21A8,stroke-width:3px,color:#fff,font-weight:bold;
  classDef responseStyle fill:#107C10,stroke:#0B5A0B,stroke-width:3px,color:#fff,font-weight:bold;
  
