import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { apiRequest } from '../api/client'
import KnowledgeDetail from '../components/KnowledgeDetail'
import type { KbArticle, KbArticleListItem, KbArticleStatus, KbVisibility, PagedResult } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { formatDate } from '../utils/format'

export default function KnowledgePage() {
  const { can } = useAuth()
  const [items, setItems] = useState<KbArticleListItem[]>([])
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<KbArticleStatus | ''>(can('knowledge.view.all') ? '' : 'Published')
  const [selected, setSelected] = useState<KbArticle | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [showCreate, setShowCreate] = useState(false)
  const [title, setTitle] = useState('')
  const [summary, setSummary] = useState('')
  const [body, setBody] = useState('')
  const [visibility, setVisibility] = useState<KbVisibility>('Internal')

  const load = useCallback(async () => {
    try {
      const params = new URLSearchParams({ pageSize: '50' })
      if (search.trim()) params.set('search', search.trim())
      if (status) params.set('status', status)
      const result = await apiRequest<PagedResult<KbArticleListItem>>(`/knowledge?${params.toString()}`)
      setItems(result.items)
      setError(null)
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to load knowledge articles.')
    }
  }, [search, status])

  useEffect(() => {
    void load()
  }, [load])

  const open = async (id: string) => {
    try {
      setSelected(await apiRequest<KbArticle>(`/knowledge/${id}`))
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to load the article.')
    }
  }

  const create = async (event: FormEvent) => {
    event.preventDefault()
    try {
      const article = await apiRequest<KbArticle>('/knowledge', {
        method: 'POST',
        body: { title, summary, body, visibility },
      })
      setTitle('')
      setSummary('')
      setBody('')
      setShowCreate(false)
      await load()
      setSelected(article)
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Unable to create the article.')
    }
  }

  return (
    <div>
      <div className="page-header">
        <div>
          <h1>Knowledge base</h1>
          <p className="page-subtitle">Reusable answers and internal guidance for your team.</p>
        </div>
        {can('knowledge.create') ? (
          <button type="button" className="button primary" onClick={() => setShowCreate((value) => !value)}>
            {showCreate ? 'Cancel' : 'New article'}
          </button>
        ) : null}
      </div>

      {error ? <div className="alert error">{error}</div> : null}

      <form className="filters" onSubmit={(event) => { event.preventDefault(); void load() }}>
        <label>
          Search
          <input value={search} maxLength={100} onChange={(event) => setSearch(event.target.value)} />
        </label>
        {can('knowledge.view.all') ? (
          <label>
            Status
            <select value={status} onChange={(event) => setStatus(event.target.value as KbArticleStatus | '')}>
              <option value="">Any status</option>
              <option value="Draft">Draft</option>
              <option value="Published">Published</option>
              <option value="Archived">Archived</option>
            </select>
          </label>
        ) : null}
        <button type="submit" className="button">Search</button>
      </form>

      {showCreate ? (
        <form className="card form" onSubmit={create}>
          <h2>Create article</h2>
          <label>
            Title
            <input value={title} required maxLength={200} onChange={(event) => setTitle(event.target.value)} />
          </label>
          <label>
            Summary
            <input value={summary} maxLength={500} onChange={(event) => setSummary(event.target.value)} />
          </label>
          <label>
            Body
            <textarea value={body} required maxLength={50000} rows={8} onChange={(event) => setBody(event.target.value)} />
          </label>
          <label>
            Visibility
            <select value={visibility} onChange={(event) => setVisibility(event.target.value as KbVisibility)}>
              <option value="Internal">Internal</option>
              <option value="Customer">Customer visible</option>
            </select>
          </label>
          <button type="submit" className="button primary">Save draft</button>
        </form>
      ) : null}

      <div className="two-column">
        <section className="card">
          <h2>Articles</h2>
          <ul className="list">
            {items.map((article) => (
              <li key={article.id} className={selected?.id === article.id ? 'list-item selected' : 'list-item'}>
                <div>
                  <button type="button" className="link-button" onClick={() => open(article.id)}>{article.title}</button>
                  <div className="muted small">
                    {article.visibility} · {article.authorName} · {formatDate(article.updatedAt)}
                  </div>
                </div>
                <span className={`pill status-${article.status.toLowerCase()}`}>{article.status}</span>
              </li>
            ))}
            {items.length === 0 ? <li className="empty-state">No articles found.</li> : null}
          </ul>
        </section>
        <KnowledgeDetail
          article={selected}
          onChanged={async (article) => {
            setSelected(article)
            await load()
          }}
          onError={setError}
        />
      </div>
    </div>
  )
}
