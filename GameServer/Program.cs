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
                        break;
                    }
                }
            }

        }
    }
}
