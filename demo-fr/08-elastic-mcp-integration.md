
# 8. Intégration Elastic MCP

## Agent Builder, serveur MCP et outil de recherche

### Elasticsearch Agent Builder

https://www.elastic.co/search-labs/blog/ai-agentic-workflows-elastic-ai-agent-builder permet la création sans code d’outils de recherche prêts pour l’IA et exposés via le protocole MCP. Principaux avantages :

- **Création d’outils sans code** : Construisez des outils de recherche dans l’UI Elastic sans écrire de code  
- **Recherche hybride** : Combine automatiquement recherche sémantique (vecteur) et recherche par mots-clés  
- **Découverte dynamique** : Les outils sont exposés via MCP et découverts par les agents IA à l’exécution  
- **Sécurité entreprise** : Hérite du RBAC Elastic et des politiques de sécurité  

### Fonctionnement

1. **Configurer** l’outil de recherche dans l’UI Agent Builder (champs : `chunk`, `semantic_chunk`)  
2. **Exposer** l’outil via un endpoint MCP (`azure_elastic_risk_agent_search_docs`)  
3. **Appeler** depuis l’agent IA avec des paramètres en langage naturel  
4. **Exécuter** la requête hybride et renvoyer les résultats scorés  

### Sous le capot : requête ES|QL

Lorsque l’agent appelle l’outil de recherche, Elastic exécute cette requête ES|QL :

```esql
FROM power-automate-3 METADATA _score
| WHERE match(semantic_chunk, ?semanticrequest, { "boost": 0.75 })
    OR match(chunk, ?keyword, { "boost": 0.25 })
| KEEP  chunk, pageNumber, pageChunkNumber, link, created, _score
| SORT _score DESC
| LIMIT 25
