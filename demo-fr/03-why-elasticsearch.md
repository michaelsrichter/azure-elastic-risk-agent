
# 3. Pourquoi Elasticsearch pour la recherche vectorielle

## Elasticsearch vs. autres magasins vectoriels

### Pourquoi Contoso a choisi Elasticsearch

Contoso a sélectionné Elasticsearch pour cette solution en fonction de plusieurs facteurs clés :

**Expertise existante**
- Équipes déjà expérimentées avec Elastic pour l’observabilité et l’analyse des journaux
- Réduit la courbe d’apprentissage et accélère le développement
- Tire parti des connaissances opérationnelles déjà présentes

**Fonctionnalités avancées d’IA**
- **Recherche hybride** : Combine la recherche sémantique (vecteur) et la recherche par mots-clés (BM25) pour une pertinence optimale
- **Intégration Azure OpenAI** : Prise en charge native des embeddings Azure OpenAI via des points de terminaison d’inférence
- **Champ semantic_text** : Vectorisation automatique sans code d’embedding personnalisé
- **Model Context Protocol (MCP)** : Constructeur d’agents intégré pour créer des outils de recherche découvrables

**Service ISV natif Azure**  
Elasticsearch est disponible en tant que [service ISV natif Azure](https://learn.microsoft.com/en-us/azure/partner-solutions/elastic/overview), offrant :

- **Facturation unifiée** : Facture unique via Azure Marketplace, utilisant crédits et engagements Azure
- **Gestion intégrée** : Déploiement et gestion directement depuis le portail Azure
- **Azure Private Link** : Connectivité privée et sécurisée sans exposition à Internet public
- **Conformité** : Bénéficie des certifications de conformité Azure et exigences de résidence des données
- **Single Sign-On** : Intégration Microsoft Entra ID (Azure AD) pour une authentification unifiée


