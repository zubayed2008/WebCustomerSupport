namespace SupportDesk.Domain;

public enum TicketStatus
{
    New = 0,
    Open = 1,
    PendingCustomer = 2,
    PendingInternal = 3,
    Resolved = 4,
    Closed = 5,
}

public enum TicketPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3,
}

public enum CommentVisibility
{
    Public = 0,
    Internal = 1,
}

public enum KbArticleStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2,
}

public enum KbVisibility
{
    Internal = 0,
    Customer = 1,
}

public enum NotificationType
{
    TicketAssigned = 0,
    TicketReplied = 1,
    TicketStatusChanged = 2,
    TicketDueSoon = 3,
    TicketOverdue = 4,
    KnowledgePublished = 5,
    AiSuggestionReady = 6,
}

public enum OutboxStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
}

public enum AiSuggestionKind
{
    Classification = 0,
    Summary = 1,
    KnowledgeRetrieval = 2,
    ReplyDraft = 3,
}

public enum AiSuggestionStatus
{
    Queued = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Stale = 4,
}

public enum ActivityType
{
    Created = 0,
    StatusChanged = 1,
    Assigned = 2,
    PriorityChanged = 3,
    CategoryChanged = 4,
    TagAdded = 5,
    TagRemoved = 6,
    PublicReply = 7,
    InternalNote = 8,
    AttachmentAdded = 9,
    SlaDeadlineChanged = 10,
    Resolved = 11,
    Reopened = 12,
    Closed = 13,
    ArticleLinked = 14,
}
