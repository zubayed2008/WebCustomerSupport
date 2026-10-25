const API_BASE = '/api/v1'
let csrfToken: string | null = null

export class ApiError extends Error {
  status: number
  detail: string | null

  constructor(status: number, message: string, detail: string | null = null) {
    super(message)
    this.status = status
    this.detail = detail
  }
}

async function fetchCsrfToken(): Promise<string> {
  const response = await fetch(`${API_BASE}/auth/antiforgery`, { credentials: 'include' })
  if (!response.ok) {
    throw new ApiError(response.status, 'Unable to start a secure session.')
  }
  const data = (await response.json()) as { token: string }
  csrfToken = data.token
  return csrfToken
}

export function clearCsrfToken(): void {
  csrfToken = null
}

interface RequestOptions {
  method?: string
  body?: unknown
  headers?: Record<string, string>
  rawBody?: BodyInit
  retryOnCsrfFailure?: boolean
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const method = options.method ?? 'GET'
  const headers: Record<string, string> = { Accept: 'application/json', ...options.headers }
  const unsafe = !['GET', 'HEAD', 'OPTIONS'].includes(method.toUpperCase())

  if (unsafe) {
    const token = csrfToken ?? (await fetchCsrfToken())
    headers['X-CSRF-TOKEN'] = token
  }

  let body: BodyInit | undefined = options.rawBody
  if (options.body !== undefined && body === undefined) {
    headers['Content-Type'] = 'application/json'
    body = JSON.stringify(options.body)
  }

  const response = await fetch(`${API_BASE}${path}`, {
    method,
    headers,
    body,
    credentials: 'include',
  })

  if (response.status === 400 && unsafe && options.retryOnCsrfFailure !== false) {
    clearCsrfToken()
    const fresh = await fetchCsrfToken()
    headers['X-CSRF-TOKEN'] = fresh
    return apiRequest<T>(path, { ...options, headers, retryOnCsrfFailure: false })
  }

  if (response.status === 401) {
    // Sign-in failures (wrong password, temporary lockout) return a 401 with a
    // ProblemDetails body. Read it before assuming the session simply expired so
    // the specific reason reaches the user.
    let detail: string | null = null
    let title: string | null = null
    try {
      const problem = (await response.json()) as { title?: string; detail?: string }
      title = problem.title ?? null
      detail = problem.detail ?? null
    } catch {
      // A 401 without a body means the authentication cookie is no longer valid.
    }

    if (detail || title) {
      throw new ApiError(response.status, detail ?? title ?? 'Sign in failed.', detail)
    }

    window.dispatchEvent(new CustomEvent('supportdesk:unauthenticated'))
    throw new ApiError(401, 'Your session has expired. Sign in again.')
  }

  if (!response.ok) {
    let detail: string | null = null
    let title = `Request failed with status ${response.status}`
    try {
      const problem = (await response.json()) as { title?: string; detail?: string }
      title = problem.title ?? title
      detail = problem.detail ?? null
    } catch {
      // Non-JSON error responses are reported with the status only.
    }

    // Prefer the specific detail (for example a temporary lockout) so users are
    // not left guessing why a valid sign-in was rejected.
    throw new ApiError(response.status, detail ?? title, detail)
  }

  if (response.status === 204) {
    return undefined as T
  }

  const contentType = response.headers.get('content-type') ?? ''
  if (!contentType.includes('application/json')) {
    return (await response.text()) as T
  }

  return (await response.json()) as T
}

export async function downloadAttachment(ticketId: string, attachmentId: string, fileName: string): Promise<void> {
  const response = await fetch(`${API_BASE}/tickets/${ticketId}/attachments/${attachmentId}`, { credentials: 'include' })
  if (!response.ok) {
    throw new ApiError(response.status, 'Unable to download the attachment.')
  }

  const blob = await response.blob()
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  document.body.appendChild(anchor)
  anchor.click()
  anchor.remove()
  URL.revokeObjectURL(url)
}

export async function uploadAttachment(ticketId: string, file: File, commentId?: string): Promise<void> {
  const token = csrfToken ?? (await fetchCsrfToken())
  const form = new FormData()
  form.append('file', file)
  const query = commentId ? `?commentId=${commentId}` : ''
  const response = await fetch(`${API_BASE}/tickets/${ticketId}/attachments${query}`, {
    method: 'POST',
    body: form,
    credentials: 'include',
    headers: { 'X-CSRF-TOKEN': token },
  })

  if (!response.ok) {
    let detail = 'The attachment could not be uploaded.'
    try {
      const problem = (await response.json()) as { detail?: string; title?: string }
      detail = problem.detail ?? problem.title ?? detail
    } catch {
      // Keep the default message.
    }
    throw new ApiError(response.status, detail)
  }
}
