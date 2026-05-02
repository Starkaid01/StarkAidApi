using System;
using System.Collections.Generic;

namespace StarkAid.Api.Services.V1.Suporte
{
    public enum ChatState
    {
        Disconnected,
        Connecting,
        Connected,
        SessionStarted,
        InConversation,
        SessionEnded
    }

    public class ConversationMessage
    {
        public string Role { get; set; } // user | assistant | system
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum SupportStage
    {
        Idle,
        Diagnosing,
        WaitingForActionConfirmation, // Waiting for "Sim"
        WaitingForActionFeedback,     // Waiting for "Resolveu?" response
        Escalated
    }

    public class ConversationContext
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public ChatState State { get; set; }
        
        // Diagnostic State
        public SupportStage CurrentStage { get; set; } = SupportStage.Idle;
        public string PendingAction { get; set; } // The action we are asking permission for (e.g., "ClearCache")
        public List<string> AttemptedActions { get; set; } = new List<string>(); // History of what we tried: ["ClearCache", "Restart"]
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(); 

        public List<ConversationMessage> History { get; set; } = new List<ConversationMessage>();
        public bool MaintenanceConsentRequested { get; set; } // Legacy flag, try to map to Stage
        public bool MaintenanceAuthorized { get; set; }
        
        public DateTime SessionStartTime { get; set; }
        public DateTime LastActivity { get; set; }
    }
}
