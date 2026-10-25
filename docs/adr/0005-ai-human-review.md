# ADR 0005: AI Suggestions Require Human Review

- **Status:** Accepted
- **Context:** AI output can be wrong, can leak content, and can be manipulated by text embedded in tickets or knowledge articles (prompt injection). Automatically sending generated replies would create direct customer risk.
- **Decision:** AI runs asynchronously through a provider interface, receives only content the requesting user is authorized to read, and must ground factual claims in cited knowledge articles. Suggestions are stored with the input revision, provider, model, prompt version, citations, and latency. Agents must copy or edit a draft into a normal public reply; there is no autonomous send path. Suggestions whose ticket content changed are marked stale. A deterministic fake provider is the default so tests and offline demos never depend on a live model.
- **Consequences:** AI is assistive rather than authoritative, failures never block ticket handling, and reviewers can audit what the model saw and produced. Evaluation fixtures and feedback ratings support quality measurement. Live providers must be configured with secrets and cannot train on support content.
