
# 4. Vue d’ensemble de l’architecture de la solution

## Vue globale : Données → Agent IA → Teams

- Flux général :
  - SharePoint → Power Automate → Azure Function → Elasticsearch.
  - Agent IA (Microsoft Agent Framework) → Outils MCP → Teams/M365 Copilot.
- Principes clés :
  - Garder les données dans Azure.
  - Utiliser l’authentification d’entreprise et Teams pour l’accès utilisateur.
  - Ajouter une couche d’IA responsable pour la sécurité et la conformité.

```mermaid
%%{init: {'theme':'base', 'themeVariables': { 'fontSize':'16px', 'fontFamily':'arial'}}}%%
flowchart TB
  %% Nodes
  SP["&lt;b&gt;SharePoint&lt;/b&gt;&lt;br/&gt;(Source de données)&lt;br/&gt;📁"]
  PA["&lt;b&gt;Power Automate&lt;/b&gt;&lt;br/&gt;(Orchestration)&lt;br/&gt;⚡"]
  AF["&lt;b&gt;Azure Function&lt;/b&gt;&lt;br/&gt;(Ingestion &amp; Transformation)&lt;br/&gt;⚙️"]
  ES["&lt;b&gt;Elasticsearch&lt;/b&gt;&lt;br/&gt;(Azure Native - Elastic)&lt;br/&gt;🔍"]
  AG["&lt;b&gt;Agent IA&lt;/b&gt;&lt;br/&gt;(Microsoft Agent Framework)&lt;br/&gt;🤖"]
  TE["&lt;b&gt;Teams / M365 Copilot&lt;/b&gt;&lt;br/&gt;(Interface utilisateur)&lt;br/&gt;💬"]

  %% Flow - Using thick arrows
  SP ==&gt;|Pour chaque PDF| PA
  PA ==&gt;|Propriétés&lt;br/&gt;du fichier &amp; contenu| AF
  AF ==&gt;|Documents&lt;br/&gt;traités| ES
  ES ==&gt;|Résultats de recherche&lt;br/&gt;via MCP| AG
  AG ==&gt;|Réponse IA| TE

  %% Security annotation
  AF -.-|🔐 Azure Private Link| ES

  %% Styling
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

