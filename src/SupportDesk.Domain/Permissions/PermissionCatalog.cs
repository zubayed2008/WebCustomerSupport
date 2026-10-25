namespace SupportDesk.Domain.Permissions;

/// <summary>
/// Permission names used by endpoints. Administrators may create additional
/// custom permissions at runtime; these names are the ones enforced by code.
/// </summary>
public static class Permissions
{
    public const string TicketsViewOwn = "tickets.view.own";
    public const string TicketsViewAll = "tickets.view.all";
    public const string TicketsCreate = "tickets.create";
    public const string TicketsReply = "tickets.reply";
    public const string TicketsNote = "tickets.note";
    public const string TicketsAssign = "tickets.assign";
    public const string TicketsUpdateStatus = "tickets.update-status";
    public const string TicketsUpdatePriority = "tickets.update-priority";
    public const string TicketsUpdateCategory = "tickets.update-category";
    public const string TicketsAttach = "tickets.attach";

    public const string KnowledgeViewPublished = "knowledge.view.published";
    public const string KnowledgeViewAll = "knowledge.view.all";
    public const string KnowledgeCreate = "knowledge.create";
    public const string KnowledgePublish = "knowledge.publish";
    public const string KnowledgeArchive = "knowledge.archive";

    public const string SlaView = "sla.view";
    public const string SlaManage = "sla.manage";

    public const string ReportsViewOwn = "reports.view.own";
    public const string ReportsViewAll = "reports.view.all";

    public const string UsersView = "users.view";
    public const string UsersManage = "users.manage";
    public const string RolesView = "roles.view";
    public const string RolesManage = "roles.manage";
    public const string PermissionsManage = "permissions.manage";
    public const string AuditView = "audit.view";

    public const string AiUse = "ai.use";
    public const string AiReview = "ai.review";

    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(TicketsViewOwn, "Tickets", "View tickets the user requested."),
        new(TicketsViewAll, "Tickets", "View and filter every ticket queue."),
        new(TicketsCreate, "Tickets", "Create tickets and customer intake."),
        new(TicketsReply, "Tickets", "Post public replies visible to the requester."),
        new(TicketsNote, "Tickets", "Post internal notes hidden from customers."),
        new(TicketsAssign, "Tickets", "Claim or reassign tickets."),
        new(TicketsUpdateStatus, "Tickets", "Move tickets through the workflow."),
        new(TicketsUpdatePriority, "Tickets", "Change ticket priority."),
        new(TicketsUpdateCategory, "Tickets", "Change ticket category and tags."),
        new(TicketsAttach, "Tickets", "Upload and download ticket attachments."),
        new(KnowledgeViewPublished, "Knowledge", "Read published customer-visible articles."),
        new(KnowledgeViewAll, "Knowledge", "Read drafts, internal, and archived articles."),
        new(KnowledgeCreate, "Knowledge", "Create and edit knowledge articles."),
        new(KnowledgePublish, "Knowledge", "Publish or unpublish knowledge articles."),
        new(KnowledgeArchive, "Knowledge", "Archive knowledge articles."),
        new(SlaView, "Service levels", "View SLA policies and calendars."),
        new(SlaManage, "Service levels", "Create and change SLA policies and calendars."),
        new(ReportsViewOwn, "Reporting", "View personal ticket summaries."),
        new(ReportsViewAll, "Reporting", "View operational dashboards."),
        new(UsersView, "Administration", "List users and their roles."),
        new(UsersManage, "Administration", "Create, activate, deactivate, and assign roles to users."),
        new(RolesView, "Administration", "List roles and granted permissions."),
        new(RolesManage, "Administration", "Create, rename, and delete roles."),
        new(PermissionsManage, "Administration", "Create permissions and grant or revoke them on roles."),
        new(AuditView, "Administration", "Read administrative and ticket audit records."),
        new(AiUse, "AI assistance", "Request AI suggestions for tickets."),
        new(AiReview, "AI assistance", "Provide feedback on AI suggestions."),
    ];
}

public sealed record PermissionDefinition(string Name, string Group, string Description);

public static class SystemRoles
{
    public const string Administrator = "Administrator";
    public const string Manager = "Manager";
    public const string Agent = "Agent";
    public const string Customer = "Customer";
}
