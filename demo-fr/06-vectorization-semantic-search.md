
# 6. Vectorisation &amp; Recherche sémantique

## Le champ semantic_text d’Elastic et les embeddings Azure OpenAI

### Structure du mapping de l’index

L’index Elasticsearch inclut les champs clés suivants :

- **`chunk`** – Contenu texte brut (type : `text`)
  - Copié automatiquement vers `semantic_chunk` via la directive `copy_to`
- **`semantic_chunk`** – Champ de texte sémantique qui génère automatiquement les embeddings (type : `semantic_text`)
  - Utilise un endpoint d’inférence Azure OpenAI
  - Stratégie de segmentation : aucune (gérée par Azure Function)
- **Champs de métadonnées :**
  - `id`, `filenameWithExtension`, `fullPath`, `link`, `versionNumber` (texte avec sous‑champs keyword)
  - `pageNumber`, `pageChunkNumber` (long)
  - `created`, `modified` (date)

### Capacités de recherche hybride

- **Recherche par mots‑clés** : requêtes directes sur le champ `chunk` pour les correspondances exactes  
- **Recherche sémantique** : recherche par similarité vectorielle sur `semantic_chunk` pour les correspondances conceptuelles  
- **Recherche hybride** : combine les deux approches pour une pertinence optimale  

### Principaux avantages

- **Aucun code d’embedding personnalisé** : Azure Function envoie uniquement le texte ; Elasticsearch gère automatiquement la vectorisation  
- **Intégration avec les endpoints d’inférence** : connecté de manière transparente aux modèles d’embedding Azure OpenAI  
- **Synchronisation automatique** : `copy_to` garantit que `chunk` et `semantic_chunk` restent alignés  
- **Adapté à l’entreprise** : embeddings en 3072 dimensions offrant une compréhension sémantique de haute qualité  

---

## Navigation

- ./05-ingestion-pipeline.md
- ./README.md
- ./07-building-ai-agent.md
