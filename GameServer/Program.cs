using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core;
using Core.Service;
using GNetworking;
using Lidgren.Network;
using Serilog;

namespace GameServer
{
    /// <summary>
    /// extend existing global chat message to add channel support
    /// channel -1 is global chat, anything else is handled as a direct message
    /// </summary>
    public class ChatMessage : ChatEvent
    {
        public ChatMessage(int clientId, string message, int channel = 0) : base(clientId, message)
        {

        }

        public int Channel = -1;
    }

    class Program
    {
        /// <summary>
        /// Say Message Handler
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sender"></param>
        /// <param name="msg"></param>
        public static bool SayMessageReceived(string name, NetConnection sender, NetPipeMessage msg)
        {
            var message = msg.GetMessage<ChatMessage>();

            // this can be null if someone is sending data which is erroneous or they're sending the wrong arguments
            if (message == null) return false;

            // process message and send to clients which should recieve the message
            var server = GameServiceManager.GetService<NetworkServer>();

            var messagePipe = server.NetworkPipe;

            // todo: add other channel support
            messagePipe.Send("server-message", message);

            return true;
        }

        public static void Main(string[] args)
        {
            var serviceManager = new GameServiceManager();
            var server = serviceManager.RegisterService( new NetworkServer(27015, 20));

            // start logging and network server service
            serviceManager.StartServices();

            // register network messages which the server can handle
            server.NetworkPipe.On("say", SayMessageReceived);

            // wait for key to exit
            Log.Information("Press any key and enter to exit.");
            Console.ReadLine();

            // shutdown properly and exit
            serviceManager.StopServices();
        }
    }
}
