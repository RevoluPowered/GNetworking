// LICENSE
// GNetworking, SimpleUnityClient and GameServer are property of Gordon Alexander MacPherson
// No warantee is provided with this code, and no liability shall be granted under any circumstances.
// All rights reserved GORDONITE LTD 2018 ? Gordon Alexander MacPherson.

using System;
using Serilog;
using System.Collections.Generic;
using Lidgren.Network;
using Newtonsoft.Json;

namespace GNetworking
{
    /// <summary>
    /// Internal Text Pipe System
    /// </summary>
    public class MessagePipe
    {
        /// <summary>
        /// MessagePipe
        /// </summary>
        public MessagePipe(NetPeer socket)
        {
            AesEncryption = new NetAESEncryption(socket, EncryptionKey);

            this.eventHandlers = new List<KeyValuePair<string, Func<string, NetConnection, NetPipeMessage, bool>>>();

            // FIX for unity types not transcoding across messsage MessagePipe due to infinite references
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Formatting =
                    Newtonsoft.Json.Formatting.None, // Indented is useful for debugging, turn off on production build
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Error,
                TypeNameHandling = TypeNameHandling.All
            };
        }

        private readonly string EncryptionKey = "48984230948094823-21387123987129837129873";
        protected readonly NetAESEncryption AesEncryption;

        /// <summary>
        /// event handler list
        /// </summary>
        public List<KeyValuePair<string, Func<string, NetConnection, NetPipeMessage, bool>>> eventHandlers
        {
            get;
            private set;
        }

        /// <summary>
        /// Register event function
        /// </summary>
        /// <param name="name"></param>
        /// <param name="e"></param>
        public void On(string name, Func<string, NetConnection, NetPipeMessage, bool> e)
        {
            Log.Debug("on called with event {name}", name);
            var kvp = new KeyValuePair<string, Func<string, NetConnection, NetPipeMessage, bool>>(name, e);
            this.eventHandlers.Add(kvp);
        }

        /// <summary>
        /// Call an event (Will only execute if registered using On)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        public void Call(string name, NetConnection sender, NetPipeMessage message)
        {
            // todo: investigate this system for errors
            for (var x = 0; x < eventHandlers.Count; x++)
            {
                var kvp = this.eventHandlers[x];

                if (kvp.Key == name)
                {
                    Log.Debug("[MessagePipe] message name {name} and message {message}", name, message);
                    kvp.Value(name, sender, message);
                }
            }
        }

        /// <summary>
        /// Generates a message
        /// </summary>
        /// <param name="socket">the NetworkSocket context</param>
        /// <param name="message">the message to send</param>
        /// <returns>outgoing message</returns>
        protected NetOutgoingMessage GenerateMessage<T>(NetPeer socket, string name, T message)
        {
            Log.Information("message: {msg}", message);

            // Encrypt our message
            var netOutgoingMessage = GenerateMessage(socket, name, new NetPipeMessage(name, message));
            netOutgoingMessage.Encrypt(AesEncryption);

            // return the net message 
            return netOutgoingMessage;
        }

        protected NetOutgoingMessage GenerateMessage(NetPeer socket, string name, NetPipeMessage message)
        {
            // convert it to json and then create the message
            var messageJson = JsonConvert.SerializeObject(message, Formatting.None);

            // return the net message 
            return socket.CreateMessage(messageJson);
        }

        /// <summary>
        /// Called automatically by the Server/Client/Other when it recieve+s a MessagePipe packet
        /// </summary>
        /// <param name="netmessage"></param>
        public void Receive(NetIncomingMessage _message)
        {
            _message.Decrypt(AesEncryption);
            var sender = _message.SenderConnection;
            var netmessage = _message.ReadString();

            Log.Debug("packet receive {data}, sent by {sender}", netmessage, sender);

            var message = JsonConvert.DeserializeObject<NetPipeMessage>(netmessage);

            if (message == null)
            {
                Log.Error("failed to convert json {message}", netmessage);
            }
            else
            {
                // todo: make this not use NetPipeMessage.Name - can cause issues when you want to send messages from one pipe to another.
                Call(message.Name, sender, message);
            }
        }
    }
}