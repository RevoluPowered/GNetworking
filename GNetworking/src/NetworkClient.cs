using System;
using System.Threading;
using System.Net;
using Lidgren.Network;
using Core.Service;
using Serilog;

namespace GNetworking
{
#if GAME_CLIENT
    using UnityEngine;
    public class NetworkClient : GameService, IDisposable
	{
	    public NetClient NetworkSocket { get; private set; }
	    public NetPeerConfiguration NetConfiguration { get; private set; }
	    public ClientMessagePipe MessagePipe { get; private set; }
        
        // todo: rewrite this system to handle client ID matching
	    [Obsolete("this needs updated to be simpler", false)]
	    public int clientID = -1;

        public NetworkClient() : base("Network Client Service")
        {

        }

        private void Message( object peer )
		{
            if(this.NetworkSocket == null) return; // no more processing can happen.

			NetIncomingMessage message;
			
			while((message = this.NetworkSocket.ReadMessage()) != null)
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

            this.NetConfiguration = new NetPeerConfiguration("unity");
	        this.NetworkSocket = new NetClient(NetConfiguration);

	    //    this.MessagePipe = new ClientMessagePipe(Log, NetworkSocket);

	        this.NetworkSocket.RegisterReceivedCallback(new SendOrPostCallback(Message));
	        this.NetworkSocket.Start();

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
            NetworkSocket = null;
        }

	    public void Dispose()
	    {
            Shutdown();
	    }


        protected void UpdateClientConnectionState(
	        NetConnectionStatus s,
	        NetIncomingMessage message,
	        MessagePipe pipe)
	    {
	        switch (s)
	        {
	            case NetConnectionStatus.Connected:
	                break;
	            case NetConnectionStatus.Disconnected:
	                if (Application.isEditor)
	                {
	                    Log.Information("[Editor only] Server is down, but not changing your scene as you may be exiting the game.");
	                }
	                else
	                {
	                    // Handle disconnects properly
	                    UnityEngine.SceneManagement.SceneManager.LoadScene("Scenes/Core/ServerList Procedural");
	                }
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
                    pipe.Logger.Debug("NET: {message}", message.ReadString());
                    break;
                case NetIncomingMessageType.ErrorMessage:
                    pipe.Logger.Error("NET: {message}", message.ReadString());
                    break;
                case NetIncomingMessageType.WarningMessage:
                    pipe.Logger.Warning("NET: {message}", message.ReadString());
                    break;
                case NetIncomingMessageType.VerboseDebugMessage:
                    pipe.Logger.Debug("NET: {message}", message.ReadString());
                    break;
                case NetIncomingMessageType.StatusChanged:

                    NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();
                    UpdateClientConnectionState(status, message, pipe);
                    break;
                case NetIncomingMessageType.Data:
                    string packet = message.ReadString();

                    try
                    {
                        pipe.Receive(packet, message.SenderConnection);
                    }
                    catch (System.Exception e)
                    {
                        pipe.Logger.Error("Exception: {e} with packet data: {packet}", e, packet);
                    }
                    break;
                default:
                    break;
            }
        }
    }
#endif // GAME_CLIENT
}