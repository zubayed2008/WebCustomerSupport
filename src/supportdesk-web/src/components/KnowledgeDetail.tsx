import { apiRequest } from '../api/client'
import type { KbArticle } from '../api/types'
import { useAuth } from '../auth/AuthContext'

interface Props {
  article: KbArticle | null
  onChanged: (article: KbArticle) => Promise<void> | void
  onError: (message: string) => void
}

export default function KnowledgeDetail({ article, onChanged, onError }: Props) {
  const { can } = useAuth()

  if (!article) {
    return (
      <section className="card">
        <h2>Article detail</h2>
        <p className="muted">Select an article to view its content.</p>
      </section>
    )
  }

  const action = async (path: string) => {
    try {
      const updated = await apiRequest<KbArticle>(`/knowledge/${article.id}/${path}`, { method: 'POST' })
      await onChanged(updated)
    } catch (exception) {
      onError(exception instanceof Error ? exception.message : 'The article action failed.')
    }
  }

  return (
    <section className="card">
      <h2>Article detail</h2>
      <h3>{article.title}</h3>
      <p className="muted">{article.summary}</p>
      <p className="preserve-lines">{article.body}</p>
      <div className="row-actions wrap">
        {can('knowledge.publish') && article.status !== 'Published' ? (
          <button type="button" className="button primary" onClick={() => action('publish')}>Publish</button>
        ) : null}
        {can('knowledge.archive') && article.status !== 'Archived' ? (
          <button type="button" className="button" onClick={() => action('archive')}>Archive</button>
        ) : null}
      </div>
    </section>
  )
}
