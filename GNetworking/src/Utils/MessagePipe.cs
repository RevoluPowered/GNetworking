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
        public MessagePipe()
        {
            this.eventHandlers = new List<KeyValuePair<string, Func<string, NetConnection, NetPipeMessage,bool>>>();
            
            // FIX for unity types not transcoding across messsage MessagePipe due to infinite references
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings {
                Formatting = Newtonsoft.Json.Formatting.None, // Indented is useful for debugging, turn off on production build
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Error,
                TypeNameHandling = TypeNameHandling.All
            };
        }

        /// <summary>
        /// event handler list
        /// </summary>
        public List<KeyValuePair<string, Func<string, NetConnection, NetPipeMessage,bool>>> eventHandlers { get; private set; }

        /// <summary>
        /// Register event function
        /// </summary>
        /// <param name="name"></param>
        /// <param name="e"></param>
        public void On( string name, Func<string, NetConnection, NetPipeMessage, bool> e)
        {
            Log.Debug("on called with event {name}",name);
            var kvp = new KeyValuePair<string, Func<string, NetConnection, NetPipeMessage, bool>>(name, e);
            this.eventHandlers.Add(kvp);
        }

        /// <summary>
        /// Call an event (Will only execute if registered using On)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        public void Call( string name, NetConnection sender, NetPipeMessage message )
        {            
            // todo: investigate this system for errors
            for( var x = 0; x < eventHandlers.Count; x++)
            {
                var kvp = this.eventHandlers[x];
                
                if(kvp.Key == name)
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
            // return the net message 
            return GenerateMessage(socket, name, new NetPipeMessage(name, message));
        }

        protected NetOutgoingMessage GenerateMessage(NetPeer socket, string name, NetPipeMessage message)
        {
            // convert it to json and then create the message
            var messageJson = JsonConvert.SerializeObject( message, Formatting.None);

            // return the net message 
            return socket.CreateMessage(messageJson);
        }

        /// <summary>
        /// Called automatically by the Server/Client/Other when it recieve+s a MessagePipe packet
        /// </summary>
        /// <param name="netmessage"></param>
        public void Receive( string netmessage, NetConnection sender )
        {
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