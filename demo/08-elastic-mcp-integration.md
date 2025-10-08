# 8. Elastic MCP Integration

## Agent Builder, MCP Server, and Search Tool

### Elasticsearch Agent Builder

[Elasticsearch Agent Builder](https://www.elastic.co/search-labs/blog/ai-agentic-workflows-elastic-ai-agent-builder) enables no-code creation of AI-ready search tools that expose via MCP protocol. Key benefits:

- **No-Code Tool Creation**: Build search tools in Elastic's UI without custom code
- **Hybrid Search**: Automatically combines semantic (vector) and keyword search
- **Dynamic Discovery**: Tools are exposed via MCP and discovered by AI agents at runtime
- **Enterprise Security**: Inherits Elasticsearch RBAC and security policies

### How It Works

1. **Configure** search tool in Agent Builder UI (fields: `chunk`, `semantic_chunk`)
2. **Expose** tool via MCP endpoint (`azure_elastic_risk_agent_search_docs`)
3. **Call** from AI Agent with natural language parameters
4. **Execute** hybrid query and return scored results

### Under the Hood: ES|QL Query

When the agent calls the search tool, Elastic executes this ES|QL query:

```esql
FROM power-automate-3 METADATA _score
| WHERE match(semantic_chunk, ?semanticrequest, { "boost": 0.75 })
    OR match(chunk, ?keyword, { "boost": 0.25 })
| KEEP  chunk, pageNumber, pageChunkNumber, link, created, _score
| SORT _score DESC
| LIMIT 25
```

**What's happening:**
- **Semantic search** on `semantic_chunk` (75% weight) for conceptual matching
- **Keyword search** on `chunk` (25% weight) for exact term matching
- Returns top 25 results with metadata and relevance scores
- Agent Builder handles parameter binding (`?semanticrequest`, `?keyword`)

### Benefits

- ✅ No custom search API code required
- ✅ Update queries without redeploying agents
- ✅ Leverage Elastic's query optimization
- ✅ Easy to add or modify search tools

---

## Navigation

- [← Previous: Building the AI Agent](./07-building-ai-agent.md)
- [Back to Demo Index](./README.md)
- [Next: Deployment in Microsoft Teams →](./09-teams-deployment.md)
