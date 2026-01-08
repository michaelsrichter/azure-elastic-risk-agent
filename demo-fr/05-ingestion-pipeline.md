
# 5. Pipeline d’ingestion

## Indexation des PDF : Power Automate + Azure Functions + Elastic

- Power Automate peut se déclencher lorsqu’un PDF est téléversé ou mis à jour dans SharePoint.
- Azure Function :
  - Récupère le PDF.
  - Extrait le texte et le segmente pour une meilleure recherche.
  - Envoie les segments à Elasticsearch pour l’indexation.
- Elastic génère automatiquement les embeddings via le champ `semantic_text`, ce qui réduit la complexité.
---

```mermaid
%%{init: {'theme':'base', 'themeVariables': { 'fontSize':'16px', 'fontFamily':'arial'}}}%%
flowchart TB
  subgraph Source["📁 SOURCE DE DONNÉES"]
    SP["&lt;b&gt;SharePoint&lt;/b&gt;&lt;br/&gt;&lt;br/&gt;Bibliothèques&lt;br/&gt;de documents PDF"]
  end

  subgraph Orchestration["⚡ ORCHESTRATION"]
    PA["&lt;b&gt;Power Automate&lt;/b&gt;&lt;br/&gt;&lt;br/&gt;Déclencheur de téléversement&lt;br/&gt;Extraction des propriétés"]
  end

  subgraph Processing["🔧 TRAITEMENT DU TEXTE"]
    AF["&lt;b&gt;Azure Function&lt;/b&gt;&lt;br/&gt;&lt;br/&gt;① Télécharger PDF&lt;br/&gt;② Extraire texte&lt;br/&gt;③ Segmenter contenu&lt;br/&gt;④ Envoyer à Elasticsearch"]
  end

  subgraph Storage["💾 ELASTICSEARCH"]
    ES["&lt;b&gt;Index Elasticsearch&lt;/b&gt;&lt;br/&gt;&lt;br/&gt;Documents + Métadonnées&lt;br/&gt;⬇️&lt;br/&gt;&lt;b&gt;Champ semantic_text&lt;/b&gt;&lt;br/&gt;Génère automatiquement Embeddings"]
  end

  %% Flow
  SP ==&gt;|Téléversement PDF| PA
  PA ==&gt;|Propriétés&lt;br/&gt;du fichier &amp; contenu| AF
  AF ==&gt;|Segments de texte&lt;br/&gt;+ métadonnées| ES

  %% Annotations
  AF -.-|Stratégie de segmentation :&lt;br/&gt;Taille + chevauchement| AF
  ES -.-|Vectorisation automatique&lt;br/&gt;Pas d’embeddings manuels| ES

  %% Styling with bold colors and high contrast
  classDef sourceStyle fill:#0078D4,stroke:#004578,stroke-width:3px,color:#fff,font-weight:bold;
  classDef orchestrationStyle fill:#742774,stroke:#4B1A4B,stroke-width:3px,color:#fff,font-weight:bold;
  classDef processingStyle fill:#00A4EF,stroke:#006D9E,stroke-width:3px,color:#fff,font-weight:bold;
  classDef storageStyle fill:#FEC514,stroke:#CB9A10,stroke-width:3px,color:#000,font-weight:bold;
  
  class SP sourceStyle;
  class PA orchestrationStyle;
  class AF processingStyle;
  class ES storageStyle;
