namespace GNetworking
{
    /// <summary>
    /// The base class for event messages
    /// </summary>
    public abstract class BaseMessage
    {}

    /// <summary>
    /// Player context message
    /// </summary>
    /// <inheritdoc cref="BaseMessage"/>
    public class PlayerMessage : BaseMessage
    {
        /// <summary>
        /// Initializes as Instance of PlayerMessage
        /// </summary>
        /// <param name="clientId">The ID of the player who sent the message</param>
        public PlayerMessage( int clientId )
        {
            this.Client = clientId;
        }

        /// <summary>
        /// The Client ID
        /// </summary>
        public int Client { get; private set; }
    }


    /// <summary>
    /// Chat event message
    /// </summary>
    /// <inheritdoc cref="PlayerMessage"/>
    public class ChatEvent : PlayerMessage
    {
        /// <summary>
        /// Initializes a chat event
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="message"></param>
        /// <inheritdoc cref="PlayerMessage"/>
        public ChatEvent(int clientId, string message) : base(clientId)
        {
            this.Message = message;
        }

        /// <summary>
        /// The player message
        /// </summary>
        public string Message { get; private set; }
    }
}
