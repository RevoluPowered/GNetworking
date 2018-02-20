namespace GNetworking
{
    /// <summary>
    /// The base class for event messages
    /// </summary>
    public abstract class BaseMessage
    {}

    /// <summary>
    /// Player context text
    /// </summary>
    /// <inheritdoc cref="BaseMessage"/>
    public class PlayerMessage : BaseMessage
    {
        /// <summary>
        /// Initializes as Instance of PlayerMessage
        /// </summary>
        /// <param name="clientId">The ID of the player who sent the text</param>
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
    /// Chat event text
    /// </summary>
    /// <inheritdoc cref="PlayerMessage"/>
    public class ChatEvent : PlayerMessage
    {
        /// <summary>
        /// Initializes a chat event
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="text"></param>
        /// <inheritdoc cref="PlayerMessage"/>
        public ChatEvent(int clientId, string text) : base(clientId)
        {
            this.Text = text;
        }

        /// <summary>
        /// The player text
        /// </summary>
        public string Text { get; private set; }
    }

    public class ChatMessage : ChatEvent
    {
        public ChatMessage(int clientId, string text, int channel = -1) : base(clientId, text)
        {

        }

        /// <summary>
        /// Channel is -1 by default for global chat.
        /// </summary>
        public int Channel { get; private set; } = -1;
    }
}
