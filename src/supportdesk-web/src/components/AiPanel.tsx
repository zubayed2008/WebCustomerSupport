import { useCallback, useEffect, useState } from 'react'
import { apiRequest } from '../api/client'
import type { AiSuggestion, AiSuggestionKind } from '../api/types'
import { useAuth } from '../auth/AuthContext'

const kinds: { value: AiSuggestionKind; label: string }[] = [
  { value: 'Classification', label: 'Suggest classification' },
  { value: 'Summary', label: 'Summarize conversation' },
  { value: 'KnowledgeRetrieval', label: 'Find knowledge' },
  { value: 'ReplyDraft', label: 'Draft a reply' },
]

export default function AiPanel({ ticketId }: { ticketId: string }) {
  const { can } = useAuth()
  const [suggestions, setSuggestions] = useState<AiSuggestion[]>([])
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    try {
      const result = await apiRequest<AiSuggestion[]>(`/tickets/${ticketId}/ai-suggestions`)
      setSuggestions(result)
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to load AI suggestions.')
    }
  }, [ticketId])

  useEffect(() => {
    if (can('ai.use')) {
      void load()
    }
  }, [can, load])

  useEffect(() => {
    const pending = suggestions.some((item) => item.status === 'Queued' || item.status === 'Running')
    if (!pending) {
      return
    }

    const timer = window.setInterval(() => void load(), 3000)
    return () => window.clearInterval(timer)
  }, [suggestions, load])

  if (!can('ai.use')) {
    return null
  }

  const request = async (kind: AiSuggestionKind) => {
    setBusy(true)
    setError(null)
    try {
      await apiRequest(`/tickets/${ticketId}/ai-suggestions`, { method: 'POST', body: { kind } })
      await load()
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'The AI request failed.')
    } finally {
      setBusy(false)
    }
  }

  const feedback = async (id: string, rating: number) => {
    await apiRequest(`/ai-suggestions/${id}/feedback`, { method: 'POST', body: { rating, comment: null } })
    await load()
  }

  const copyDraft = async (text: string) => {
    try {
      await navigator.clipboard.writeText(text)
    } catch {
      setError('Clipboard access was denied. Select and copy the text manually.')
    }
  }

  return (
    <section className="card ai-panel">
      <div className="page-header">
        <h2>AI assistance</h2>
        <span className="muted small">Human review required before sending</span>
      </div>
      {error ? <div className="alert error">{error}</div> : null}
      <div className="row-actions wrap">
        {kinds.map((kind) => (
          <button key={kind.value} type="button" className="button" disabled={busy} onClick={() => request(kind.value)}>
            {kind.label}
          </button>
        ))}
      </div>
      <ul className="list">
        {suggestions.map((suggestion) => (
          <li key={suggestion.id} className="list-item column">
            <div className="row-actions wrap">
              <strong>{suggestion.kind}</strong>
              <span className={`pill status-${suggestion.status.toLowerCase()}`}>{suggestion.status}</span>
              <span className="muted small">{suggestion.provider}{suggestion.model ? ` / ${suggestion.model}` : ''}</span>
              {suggestion.latencyMs ? <span className="muted small">{suggestion.latencyMs} ms</span> : null}
            </div>
            {suggestion.status === 'Failed' ? <div className="alert error">{suggestion.error}</div> : null}
            {suggestion.status === 'Stale' ? <div className="alert warning">The ticket changed after this suggestion was requested.</div> : null}
            {suggestion.outputText ? <pre className="ai-output">{suggestion.outputText}</pre> : null}
            {suggestion.citations.length > 0 ? (
              <div>
                <strong>Citations</strong>
                <ul>
                  {suggestion.citations.map((citation) => (
                    <li key={citation.articleId}>
                      {citation.title} — {citation.snippet}
                    </li>
                  ))}
                </ul>
              </div>
            ) : null}
            {suggestion.status === 'Completed' ? (
              <div className="row-actions wrap">
                <button type="button" className="button small" onClick={() => copyDraft(suggestion.outputText ?? '')}>
                  Copy draft
                </button>
                {can('ai.review') ? (
                  <>
                    <button type="button" className="button small" onClick={() => feedback(suggestion.id, 5)}>Helpful</button>
                    <button type="button" className="button small" onClick={() => feedback(suggestion.id, 1)}>Not helpful</button>
                    {suggestion.feedbackRating ? <span className="muted small">Rated {suggestion.feedbackRating}/5</span> : null}
                  </>
                ) : null}
              </div>
            ) : null}
          </li>
        ))}
        {suggestions.length === 0 ? <li className="muted">No suggestions requested yet.</li> : null}
      </ul>
    </section>
  )
}
