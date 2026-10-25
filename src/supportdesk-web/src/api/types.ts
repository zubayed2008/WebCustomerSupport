export type TicketStatus = 'New' | 'Open' | 'PendingCustomer' | 'PendingInternal' | 'Resolved' | 'Closed'
export type TicketPriority = 'Low' | 'Normal' | 'High' | 'Urgent'
export type CommentVisibility = 'Public' | 'Internal'
export type KbArticleStatus = 'Draft' | 'Published' | 'Archived'
export type KbVisibility = 'Internal' | 'Customer'
export type AiSuggestionKind = 'Classification' | 'Summary' | 'KnowledgeRetrieval' | 'ReplyDraft'
export type AiSuggestionStatus = 'Queued' | 'Running' | 'Completed' | 'Failed' | 'Stale'

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface CurrentUser {
  id: string
  email: string
  displayName: string
  roles: string[]
  permissions: string[]
}

export interface TicketListItem {
  id: string
  number: string
  subject: string
  status: TicketStatus
  priority: TicketPriority
  categoryName: string | null
  requesterId: string
  requesterName: string
  assigneeId: string | null
  assigneeName: string | null
  createdAt: string
  updatedAt: string
  firstResponseDueAt: string | null
  resolutionDueAt: string | null
  isDueSoon: boolean
  isOverdue: boolean
  commentCount: number
}

export interface TicketComment {
  id: string
  authorId: string
  authorName: string
  body: string
  visibility: CommentVisibility
  isFirstResponse: boolean
  createdAt: string
  attachments: TicketAttachment[]
}

export interface TicketActivity {
  id: string
  type: string
  actorId: string | null
  actorName: string | null
  field: string | null
  oldValue: string | null
  newValue: string | null
  createdAt: string
}

export interface TicketAttachment {
  id: string
  fileName: string
  contentType: string
  sizeBytes: number
  uploadedById: string
  createdAt: string
}

export interface LinkedArticle {
  id: string
  title: string
  slug: string
  status: KbArticleStatus
}

export interface TicketDetail {
  id: string
  number: string
  subject: string
  description: string
  status: TicketStatus
  priority: TicketPriority
  categoryId: string | null
  categoryName: string | null
  requesterId: string
  requesterName: string
  assigneeId: string | null
  assigneeName: string | null
  tags: string[]
  createdAt: string
  updatedAt: string
  firstResponseDueAt: string | null
  resolutionDueAt: string | null
  firstRespondedAt: string | null
  resolvedAt: string | null
  closedAt: string | null
  isDueSoon: boolean
  isOverdue: boolean
  responseBreached: boolean
  resolutionBreached: boolean
  pausedMinutes: number
  rowVersion: number
  comments: TicketComment[]
  activities: TicketActivity[]
  attachments: TicketAttachment[]
  linkedArticles: LinkedArticle[]
}

export interface Category {
  id: string
  name: string
  description: string | null
  sortOrder: number
  isActive: boolean
}

export interface Assignee {
  id: string
  displayName: string
  email: string
  roles: string[]
}

export interface KbArticleListItem {
  id: string
  slug: string
  title: string
  summary: string
  status: KbArticleStatus
  visibility: KbVisibility
  authorName: string
  updatedAt: string
  publishedAt: string | null
}

export interface KbArticle extends KbArticleListItem {
  body: string
  authorId: string
  publisherId: string | null
  createdAt: string
  rowVersion: number
}

export interface QueueMetric {
  label: string
  count: number
}

export interface AgentWorkload {
  agentId: string | null
  agentName: string
  activeTickets: number
  dueSoon: number
  overdue: number
}

export interface TrendPoint {
  date: string
  created: number
  resolved: number
}

export interface ReportOverview {
  openTickets: number
  unassignedTickets: number
  dueSoonTickets: number
  overdueTickets: number
  resolvedInPeriod: number
  averageFirstResponseMinutes: number
  averageResolutionMinutes: number
  byStatus: QueueMetric[]
  byPriority: QueueMetric[]
  agentWorkload: AgentWorkload[]
  volumeTrend: TrendPoint[]
}

export interface AiCitation {
  articleId: string
  title: string
  slug: string
  snippet: string
}

export interface AiSuggestion {
  id: string
  ticketId: string
  kind: AiSuggestionKind
  status: AiSuggestionStatus
  inputRevision: string
  outputText: string | null
  citations: AiCitation[]
  error: string | null
  provider: string
  model: string | null
  promptVersion: string
  latencyMs: number | null
  feedbackRating: number | null
  createdAt: string
  completedAt: string | null
}

export interface Notification {
  id: string
  type: string
  title: string
  body: string | null
  link: string | null
  isRead: boolean
  createdAt: string
  readAt: string | null
}

export interface UserSummary {
  id: string
  email: string
  displayName: string
  isActive: boolean
  roles: string[]
  createdAt: string
}

export interface Role {
  id: string
  name: string
  isSystem: boolean
  permissions: string[]
}

export interface Permission {
  id: string
  name: string
  group: string
  description: string
  isSystem: boolean
}

export interface SlaPolicy {
  id: string
  name: string
  priority: TicketPriority
  firstResponseMinutes: number
  resolutionMinutes: number
  dueSoonMinutes: number
  version: number
  isActive: boolean
  businessCalendarId: string
  businessCalendarName: string
}

export interface BusinessCalendar {
  id: string
  name: string
  timeZoneId: string
  isActive: boolean
  workingDays: { day: string; intervals: { start: string; end: string }[] }[]
  holidays: { id: string; date: string; name: string }[]
}
