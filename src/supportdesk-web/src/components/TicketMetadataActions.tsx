import { useState } from 'react'
import { apiRequest } from '../api/client'
import type { Category, KbArticleListItem, TicketDetail } from '../api/types'
import { useAuth } from '../auth/AuthContext'

interface Props {
  ticket: TicketDetail
  categories: Category[]
  articles: KbArticleListItem[]
  onChanged: () => Promise<void> | void
  onError: (message: string) => void
}

export default function TicketMetadataActions({ ticket, categories, articles, onChanged, onError }: Props) {
  const { can } = useAuth()
  const [categoryId, setCategoryId] = useState(ticket.categoryId ?? '')
  const [tags, setTags] = useState(ticket.tags.join(', '))
  const [articleId, setArticleId] = useState('')
  const [busy, setBusy] = useState(false)

  const run = async (action: () => Promise<unknown>) => {
    setBusy(true)
    try {
      await action()
      await onChanged()
    } catch (exception) {
      onError(exception instanceof Error ? exception.message : 'The update failed.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="card">
      <h2>Classification</h2>

      {can('tickets.update-category') ? (
        <>
          <div className="form-row">
            <label>
              Category
              <select value={categoryId} onChange={(event) => setCategoryId(event.target.value)}>
                <option value="">No category</option>
                {categories.map((category) => (
                  <option key={category.id} value={category.id}>{category.name}</option>
                ))}
              </select>
            </label>
            <button
              type="button"
              className="button"
              disabled={busy || !categoryId}
              onClick={() => run(() => apiRequest(`/tickets/${ticket.id}`, { method: 'PUT', body: { categoryId } }))}
            >
              Update category
            </button>
          </div>
          <div className="form-row">
            <label>
              Tags
              <input value={tags} maxLength={200} onChange={(event) => setTags(event.target.value)} />
            </label>
            <button
              type="button"
              className="button"
              disabled={busy}
              onClick={() => run(() => apiRequest(`/tickets/${ticket.id}`, {
                method: 'PUT',
                body: { tags: tags.split(',').map((value) => value.trim()).filter(Boolean) },
              }))}
            >
              Update tags
            </button>
          </div>
        </>
      ) : null}

      {can('knowledge.view.published') ? (
        <div className="form-row">
          <label>
            Link knowledge article
            <select value={articleId} onChange={(event) => setArticleId(event.target.value)}>
              <option value="">Select an article</option>
              {articles.map((article) => (
                <option key={article.id} value={article.id}>{article.title}</option>
              ))}
            </select>
          </label>
          <button
            type="button"
            className="button"
            disabled={busy || !articleId}
            onClick={() => run(() => apiRequest(`/tickets/${ticket.id}/articles`, { method: 'POST', body: { articleId } }))}
          >
            Link
          </button>
        </div>
      ) : null}

      {ticket.linkedArticles.length > 0 ? (
        <div>
          <strong>Linked articles</strong>
          <ul className="linked-list">
            {ticket.linkedArticles.map((article) => (
              <li key={article.id}>
                {article.title}
                {can('knowledge.view.published') ? (
                  <button
                    type="button"
                    className="button small"
                    disabled={busy}
                    onClick={() => run(() => apiRequest(`/tickets/${ticket.id}/articles/${article.id}`, { method: 'DELETE' }))}
                  >
                    Unlink
                  </button>
                ) : null}
              </li>
            ))}
          </ul>
        </div>
      ) : null}
    </section>
  )
}
