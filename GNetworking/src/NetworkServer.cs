// LICENSE
// GNetworking, SimpleUnityClient and GameServer are property of Gordon Alexander MacPherson
// No warantee is provided with this code, and no liability shall be granted under any circumstances.
// All rights reserved GORDONITE LTD 2018 ? Gordon Alexander MacPherson.

using Serilog;

namespace GNetworking
{
    using System;
    using Lidgren.Network;
    using Core;
    using Core.Service;

    public class NetworkServer : GameService, IDisposable
	{
        /// <summary>
        /// Network Socket
        /// </summary>
	    public NetServer NetworkSocket { get; private set; }

        /// <summary>
        /// Net configuration
        /// </summary>
	    public NetPeerConfiguration NetConfiguration { get; private set; }

        /// <summary>
        /// Max players
        /// </summary>
	    public int MaxPlayers { get; private set; }

        /// <summary>
        /// Listen Port
        /// </summary>
	    public int ListenPort { get; private set; }

        /// <summary>
        /// Network pipe
        /// </summary>
	    public ServerMessagePipe NetworkPipe { get; private set; }

	    /// <summary>
	    /// When the client connection is properly connected to the server this is called
	    /// </summary>
	    public Action<NetConnection> OnClientConnectionSuccessful;

        /// <summary>
        /// When the client is disconnected this action is called.
        /// </summary>
        public Action<NetConnection> OnClientDisconnected;

        /// <summary>
        /// Enable NAT discovery
        /// </summary>
	    public bool NetworkDiscovery = true;

        public NetworkServer(int port, int maxPlayers) : base("Network server service")
        {
            this.ListenPort = 27015;
            this.MaxPlayers = 20;
        }

	    public override void Start()
	    {
	        Log.Information("Started logging NetworkServer.");

	        NetConfiguration = new NetPeerConfiguration("unity")
	        {
	            MaximumConnections = MaxPlayers,
	            Port = 27015
	        };

	        NetConfiguration.EnableMessageType(NetIncomingMessageType.NatIntroductionSuccess);
            
	        if (NetworkDiscovery)
	        {
	            NetConfiguration.EnableUPnP = true;
	        }

            NetworkSocket = new NetServer(this.NetConfiguration);
	        NetworkPipe = new ServerMessagePipe(this.NetworkSocket);
            NetworkSocket.Start();
        }

        public override void Update()
        {
			if(NetworkSocket == null) return; // no more processing can happen.
            NetIncomingMessage message;

			while((message = this.NetworkSocket.ReadMessage()) != null)
			{
				ProcessMessage(message, NetworkPipe);
			}
			
            this.NetworkSocket.Recycle( message );
        }

	    public void Dispose()
	    {
            Shutdown();
	    }

	    public override void Stop()
	    {
            Shutdown();
	    }

        public void Shutdown()
        {
	        Log.Information("Server socket closing...");
            NetworkSocket?.Shutdown("Server going down for quit");

            NetworkSocket = null;
            NetworkPipe = null;
            NetConfiguration = null;
        }

        /// <summary>
        /// Process message on NetworkPipe
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
                    UpdateServerConnectionState(status, message, pipe);

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

	    protected void UpdateServerConnectionState(
	        NetConnectionStatus s,
	        NetIncomingMessage message,
	        MessagePipe pipe)
	    {
	        switch (s)
	        {
	            case NetConnectionStatus.Connected:
	                Log.Debug("player connected: {connection}", message.SenderConnection);
	                OnClientConnectionSuccessful?.Invoke( message.SenderConnection );
	                break;
	            case NetConnectionStatus.Disconnected:
	                Log.Debug("player disconnected: {connection}", message.SenderConnection);
                    OnClientDisconnected?.Invoke( message.SenderConnection );
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
    }
}