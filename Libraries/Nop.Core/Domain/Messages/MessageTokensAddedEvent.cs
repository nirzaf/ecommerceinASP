using System.Collections.Generic;

namespace Nop.Core.Domain.Messages
{
    /// <summary>
    /// A container for tokens that are added.
    /// </summary>
    /// <typeparam name="U">Type</typeparam>
    public class MessageTokensAddedEvent<U>
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="tokens">Tokens</param>
        public MessageTokensAddedEvent(MessageTemplate message, IList<U> tokens)
        {
            Message = message;
            Tokens = tokens;
        }

        /// <summary>
        /// Message
        /// </summary>
        public MessageTemplate Message { get; }

        /// <summary>
        /// Tokens
        /// </summary>
        public IList<U> Tokens { get; }
    }
}