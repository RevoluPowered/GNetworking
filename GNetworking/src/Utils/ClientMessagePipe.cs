
using System;
using System.Collections;
using System.Threading;
using System.Collections.Generic;
using Lidgren.Network;
using Newtonsoft.Json;
using System.Linq;
using Core;
using Serilog;

namespace GNetworking
{
    /// <summary>
    /// Client Text Pipe system
    /// Used to send messages using the relevant NetworkSocket to the Server
    /// Simplified for easy usage.
    /// </summary>
    public class ClientMessagePipe : MessagePipe
    {
        /// <summary>
        /// Creates a Client message MessagePipe
        /// The NetworkSocket is supplied to this MessagePipe so it gives you access to the relevant send functions.
        /// </summary>
        /// <param name="_logger"></param>
        /// <param name="clientSocket"></param>
        /// <returns></returns>
        public ClientMessagePipe( NetClient clientSocket ) : base()
        {
            this.clientSocket = clientSocket;
        }

        /// <summary>
        /// Reference to the Client NetworkSocket
        /// </summary>
        private readonly NetClient clientSocket;

        /// <summary>
        /// Sends via Client NetworkSocket to the Server
        /// Uses: ReliableOrdered TCP packets
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public void Send<T>( string name, T message) where T: BaseMessage
        {
            if(clientSocket == null)
            {
                Log.Error("Client: invalid NetworkSocket supplied!");
                return;
            }
            
            clientSocket.SendMessage(
                GenerateMessage<T>(clientSocket, name, message),
                NetDeliveryMethod.UnreliableSequenced
            );
        }

        /// <summary>
        /// Sends via Client to Server reliably
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="name"></param>
        /// <param name="args"></param>
        public void SendReliable<T>( string name, T message ) where T: BaseMessage
        {
            if(clientSocket == null)
            {
                Log.Error("Client: invalid NetworkSocket supplied!");
                return;
            }
            
            clientSocket.SendMessage(
                GenerateMessage<T>(clientSocket, name, message),
                NetDeliveryMethod.ReliableSequenced
            );
        }
    }
}