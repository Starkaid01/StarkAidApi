using System;
using System.Collections.Generic;

namespace StarkAid.Api.DTOs.V1.Support;

public enum SupportMessageType
{
    UserInput,
    AgentThought, // Uso interno, nunca enviado ao app
    AgentQuestion,
    AgentActionProposal,
    AgentActionResult,
    AssistantResponse,
    Error
}

public enum SupportFeedback
{
    Positive,        // resolveu, funcionou
    Negative,        // não resolveu, mesma coisa, ainda não
    Neutral,         // ok, entendi
    Confirmation,    // sim, pode
    Denial           // não quero, agora não
}

public class SupportMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public SupportMessageType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ContextTitle { get; set; }
    public string? ActionProposed { get; set; }
    public string? ActionToExecute { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SupportConversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string ContextTitle { get; set; } = string.Empty;
    public List<SupportMessage> Messages { get; set; } = new();
    public string? PendingAction { get; set; }
    public string? PendingActionOrigem { get; set; }
    public string CurrentStage { get; set; } = "Idle"; // Idle, Diagnosing, WaitingForConfirmation, ExecutingAction, WaitingForFeedback, Resolved, Escalated
    public List<string> AttemptedActions { get; set; } = new();
    public string? LastOutcome { get; set; }

    public void AddMessage(SupportMessage message)
    {
        Messages.Add(message);
        if (Messages.Count > 8)
        {
            Messages.RemoveAt(0);
        }
    }
}
