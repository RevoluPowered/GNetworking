using System;
using System.Threading;
using Core.Service;

// Gordon's Networking layer
using GNetworking;
using GNetworking.Managers;

namespace GameServer
{
    class Program
    {
        public static void Main(string[] args)
        {
            // create server socket handler
            var server = GameServiceManager.RegisterService( new NetworkServer(27015, 20));

            // create chat system handler
            var chatSystem = GameServiceManager.RegisterService(new ServerChatManager());

            // start logging and network server service
            GameServiceManager.StartServices();
            

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

        /// <summary>
        /// close server flag for thread
        /// </summary>
        public static object close_server = false;

        /// <summary>
        /// Update server thread
        /// </summary>
        public static void UpdateServer()
        {
            bool server_close = (bool) close_server;

            // wait until exit recieved from other thread
            while (!server_close)
            {
                GameServiceManager.UpdateServices();

                // wait 
                System.Threading.Thread.Sleep(1000 / 60);

                // update close status
                lock (close_server)
                {
                    server_close = (bool) close_server;
                }

            }
        }
    }
}
