// LICENSE
// GNetworking, SimpleUnityClient and GameServer are property of Gordon Alexander MacPherson
// No warantee is provided with this code, and no liability shall be granted under any circumstances.
// All rights reserved GORDONITE LTD 2018 ? Gordon Alexander MacPherson.

using Core.Service;
using Lidgren.Network;
using Serilog;
using System;
using System.Net;
using System.Threading;

namespace GNetworking
{
    public class NetworkClient : GameService, IDisposable
    {
        public NetClient NetworkSocket { get; private set; }
        public NetPeerConfiguration NetConfiguration { get; private set; }
        public ClientMessagePipe MessagePipe { get; private set; }
        
        public NetworkClient() : base("Network Client Service")
        {

        }

        private void Message(object peer)
        {
            if (this.NetworkSocket == null) return; // no more processing can happen.

            NetIncomingMessage message;

            while ((message = this.NetworkSocket.ReadMessage()) != null)
            {
                ProcessMessage(message, this.MessagePipe);
            }
            // todo: memory leak
            this.NetworkSocket.Recycle(message);
        }

        public void Connect(IPAddress address, int port)
        {
            try
            {
                var ep = new IPEndPoint(address, port);
                var hail = NetworkSocket.CreateMessage("unity-Client: request open connection");

                Log.Information("attempting connection to: {ep}", ep.ToString());

                NetworkSocket.Connect(ep, hail);
            }
            catch (System.Exception e)
            {
                Log.Error("Error: {e}", e.ToString());
            }
        }

        public override void Stop()
        {
            Shutdown();
        }

        public override void Start()
        {
            Log.Information("Started logging NetworkClient.");

            NetConfiguration = new NetPeerConfiguration("unity");
            NetworkSocket = new NetClient(NetConfiguration);
            MessagePipe = new ClientMessagePipe(NetworkSocket);

            NetworkSocket.RegisterReceivedCallback(new SendOrPostCallback(Message));
            NetworkSocket.Start();

            Log.Information("NetworkClient initialised");
        }

        public override void Update()
        {
        }

        /// <summary>
        /// Shutdown the network client
        /// </summary>
        public void Shutdown()
        {
            Log.Information("stopping Client NetworkSocket...");
            NetworkSocket?.Shutdown("Client application exited");

            NetworkSocket = null;
            NetConfiguration = null;
            MessagePipe = null;
        }

        public void Dispose()
        {
            Shutdown();
        }

        /// <summary>
        /// When the client connection is properly connected to the server this is called
        /// </summary>
        public Action OnClientConnectionSuccessful;

        /// <summary>
        /// When the client is disconnected this action is called.
        /// </summary>
        public Action OnClientDisconnected;

        protected void UpdateClientConnectionState(
            NetConnectionStatus s,
            NetIncomingMessage message,
            MessagePipe pipe)
        {
            switch (s)
            {
                case NetConnectionStatus.Connected:
                    OnClientConnectionSuccessful?.Invoke();
                    break;
                case NetConnectionStatus.Disconnected:
                    OnClientDisconnected?.Invoke();
                    break;
                case NetConnectionStatus.Disconnecting:
                case NetConnectionStatus.InitiatedConnect:
                case NetConnectionStatus.None:
                case NetConnectionStatus.ReceivedInitiation:
                case NetConnectionStatus.RespondedConnect:
                case NetConnectionStatus.RespondedAwaitingApproval:
                    break;
            }

            Log.Information("Updated connection status: " + s.ToString());
        }

        /// <summary>
        /// Process message on MessagePipe
        /// This is generic between Client and Server.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="pipe"></param>
        protected void ProcessMessage(NetIncomingMessage message, MessagePipe pipe)
        {
            switch (message.MessageType)
            {
                case NetIncomingMessageType.DebugMessage:
                    Log.Debug("NET: {message}", message.ReadString());
                    break;

                case NetIncomingMessageType.ErrorMessage:
                    Log.Error("NET: {message}", message.ReadString());
                    break;

                case NetIncomingMessageType.WarningMessage:
                    Log.Warning("NET: {message}", message.ReadString());
                    break;

                case NetIncomingMessageType.VerboseDebugMessage:
                    Log.Debug("NET: {message}", message.ReadString());
                    break;

                case NetIncomingMessageType.StatusChanged:

                    NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();
                    UpdateClientConnectionState(status, message, pipe);
                    break;

                case NetIncomingMessageType.Data:
                    try
                    {
                        pipe.Receive(message);
                    }
                    catch (System.Exception e)
                    {
                        Log.Error("Exception: {e}", e);
                    }
                    break;

                default:
                    break;
            }
        }
    }
}