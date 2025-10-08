# 6. Vectorization & Semantic Search

## Elastic's semantic_text Field and Azure OpenAI Embeddings

### Index Mapping Structure

The Elasticsearch index includes the following key fields:

- **`chunk`** - Raw text content (type: `text`)
  - Automatically copied to `semantic_chunk` via `copy_to` directive
- **`semantic_chunk`** - Semantic text field that auto-generates embeddings (type: `semantic_text`)
  - Uses Azure OpenAI inference endpoint
  - Chunking strategy: none (handled by Azure Function)
- **Metadata fields:**
  - `id`, `filenameWithExtension`, `fullPath`, `link`, `versionNumber` (text with keyword subfields)
  - `pageNumber`, `pageChunkNumber` (long)
  - `created`, `modified` (date)

### Hybrid Search Capabilities

- **Keyword Search**: Direct queries on `chunk` field for exact matching
- **Semantic Search**: Vector similarity search on `semantic_chunk` for conceptual matching
- **Hybrid Search**: Combines both approaches for optimal relevance

### Key Benefits

- **Zero Custom Embedding Code**: Azure Function only sends text; Elasticsearch handles vectorization automatically
- **Inference Endpoint Integration**: Seamlessly connected to Azure OpenAI embedding models
- **Automatic Synchronization**: `copy_to` ensures `chunk` and `semantic_chunk` stay in sync
- **Enterprise-Ready**: 3072-dimensional embeddings provide high-quality semantic understanding

---

## Navigation

- [← Previous: Ingestion Pipeline](./05-ingestion-pipeline.md)
- [Back to Demo Index](./README.md)
- [Next: Building the AI Agent →](./07-building-ai-agent.md)
