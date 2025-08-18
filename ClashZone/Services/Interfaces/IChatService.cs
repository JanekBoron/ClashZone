using ClashZone.ViewModels;

public interface IChatService
{
    /// <summary>
    /// Retrieves the chat view model for a tournament and user.  Includes
    /// general chat messages, team messages (if applicable) and problem
    /// report messages.  The isAdmin flag should be passed from the
    /// controller using User.IsInRole("Admin").
    /// </summary>
    /// <param name="tournamentId">The identifier of the tournament.</param>
    /// <param name="userId">The identifier of the current user (null when not logged in).</param>
    /// <param name="isAdmin">Indicates whether the current user is an administrator.</param>
    Task<ChatViewModel?> GetChatAsync(int tournamentId, string? userId, bool isAdmin);

    /// <summary>
    /// Posts a new general or team message to the chat.
    /// </summary>
    Task PostMessageAsync(int tournamentId, string userId, string message, bool toTeam);

    /// <summary>
    /// Posts a new problem report message.  For non‑administrators the
    /// message is associated with the user's team.  Administrators must
    /// specify a teamId to route the report appropriately.
    /// </summary>
    /// <param name="tournamentId">The identifier of the tournament.</param>
    /// <param name="userId">The identifier of the user posting the message.</param>
    /// <param name="message">The message content.</param>
    /// <param name="teamId">The team for which this report applies.</param>
    /// <param name="isAdmin">Indicates whether the current user is an administrator.</param>
    Task PostReportMessageAsync(int tournamentId, string userId, string message, int? teamId, bool isAdmin);
}