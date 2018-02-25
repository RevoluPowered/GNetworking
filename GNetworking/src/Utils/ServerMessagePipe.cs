using System;
using System.Collections;
using System.Threading;
using System.Collections.Generic;
using Lidgren.Network;
using Newtonsoft.Json;
using System.Linq;
using Core;
using Serilog;
using Serilog.Core;

namespace GNetworking
{
    /// <summary>
    /// Server Text Pipe system
    /// Used to send messages using the relevant NetworkSocket to the Server
    /// Simplified for easy usage.
    /// </summary>
    public class ServerMessagePipe : MessagePipe
    {
        /// <summary>
        /// Reference to the Server NetworkSocket
        /// </summary>
        private NetServer server_socket;

        /// <summary>
        /// Creates a Server message MessagePipe
        /// The NetworkSocket is supplied to this MessagePipe so it gives you access to the relevant send functions.
        /// Additional note: BufferCapture is available using this message MessagePipe only
        /// </summary>
        /// <param name="_logger"></param>
        /// <param name="_server_socket"></param>
        /// <returns></returns>
        public ServerMessagePipe( NetServer _server_socket ) : base(_server_socket)
        {
            server_socket = _server_socket;
        }

        /// <summary>
        /// If set the buffer capture can store messages, if they match what was expected.
        /// This will also warn you if you're using the buffer capture without an Instance of it. Make sure to call SetBufferCapture.
        /// </summary>
        protected BufferCapture capture;

        /// <summary>
        /// BufferCapture set
        /// Let's you use the buffer capture system to cache certain sent messages and redirect them to newly joining clients.
        /// </summary>
        /// <param name="_capture"></param>
        public void SetBufferCapture( BufferCapture _capture )
        {
            capture = _capture;
        }

        private void SendFunction<T>(string name, T message, NetDeliveryMethod method, IList<NetConnection> connections = null)
        {
            if (server_socket == null)
            {
                Log.Error("Server: invalid NetworkSocket supplied!");
                return;
            }

            if (connections == null)
            {
                server_socket.SendToAll(
                    GenerateMessage<T>(server_socket, name, message),
                    method
                );
            }
            else
            {
                server_socket.SendMessage(
                    GenerateMessage<T>(server_socket, name, message),
                    connections,
                    method,
                    // todo: investigate why this is required?
                    0
                );
            }

        }

        /// <summary>
        /// Send via Server to Client reliably
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        public void SendReliable<T>( string name, T message, IList<NetConnection> connections = null)
        {
            SendFunction<T>(name, message, NetDeliveryMethod.ReliableSequenced, connections);
        }

        /// <summary>
        /// Sends via Server NetworkSocket to all clients
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        public void Send<T>( string name, T message, IList<NetConnection> connections = null)
        {
            SendFunction<T>(name, message, NetDeliveryMethod.UnreliableSequenced, connections );
        }

        /// <summary>
        /// Send message to specific Client
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        public void SendClient<T>( NetConnection client, string name, T message)
        {
            if(server_socket == null)
            {
                Log.Error("invalid NetworkSocket supplied!");
                return; 
            }

            server_socket.SendMessage(
                GenerateMessage(server_socket, name, message),
                client,
                NetDeliveryMethod.ReliableSequenced
            );
        }

    }
}