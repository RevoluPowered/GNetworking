using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            // Retrieve the message
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
            // register our server service, and reference it
            var server = GameServiceManager.RegisterService( new NetworkServer(27015, 20));

            // start logging and network server service
            GameServiceManager.StartServices();

            // register network messages which the server can handle
            server.NetworkPipe.On("say", SayMessageReceived);


            Thread server_thread = new Thread(UpdateServer);
            server_thread.Start();

            Console.WriteLine("type quit and press enter to exit");

            while (true)
            {
                String input = Console.ReadLine();

                if (input == "exit" || input == "quit")
                {
                    lock (close_server)
                    {
                        close_server = true;
                        break;
                    }
                }
            }

            // shutdown properly and exit
            GameServiceManager.Shutdown();
        }

        public static object close_server = false;
        public static void UpdateServer()
        {

            // wait until exit recieved from other thread
            while (true)
            {
                GameServiceManager.UpdateServices();

                // wait 
                System.Threading.Thread.Sleep(1000 / 60);

                // prevent deadlocking
                lock (close_server)
                {
                    if ((bool)close_server)
                    {
                        // non async
                        break;
                    }
                }
            }

        }
    }
}
