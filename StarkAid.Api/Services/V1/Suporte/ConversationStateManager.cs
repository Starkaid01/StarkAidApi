using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace StarkAid.Api.Services.V1.Suporte
{
    public class ConversationStateManager
    {
        // Thread-safe dictionary to store conversation contexts by UserId
        private readonly ConcurrentDictionary<string, ConversationContext> _conversations = new();

        public ConversationContext GetOrCreateContext(string userId, string userName)
        {
            return _conversations.GetOrAdd(userId, _ => new ConversationContext
            {
                UserId = userId,
                UserName = userName,
                State = ChatState.Connected,
                SessionStartTime = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                History = new List<ConversationMessage>()
            });
        }

        public ConversationContext? GetContext(string userId)
        {
            if (_conversations.TryGetValue(userId, out var context))
            {
                return context;
            }
            return null;
        }

        public void UpdateState(string userId, ChatState newState)
        {
            if (_conversations.TryGetValue(userId, out var context))
            {
                context.State = newState;
                context.LastActivity = DateTime.UtcNow;
            }
        }

        public void AddMessage(string userId, ConversationMessage message)
        {
            if (_conversations.TryGetValue(userId, out var context))
            {
                context.History.Add(message);
                context.LastActivity = DateTime.UtcNow;
            }
        }

        public void RemoveSession(string userId)
        {
            _conversations.TryRemove(userId, out _);
        }
        
        public void SetMaintenanceConsent(string userId, bool requested, bool authorized)
        {
            if (_conversations.TryGetValue(userId, out var context))
            {
                context.MaintenanceConsentRequested = requested;
                context.MaintenanceAuthorized = authorized;
            }
        }
    }
}
