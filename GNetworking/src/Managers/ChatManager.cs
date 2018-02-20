using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Service;
using GNetworking.Data;
using GNetworking.Messages;
using Lidgren.Network;
using RandomNameGeneratorLibrary;
using Serilog;

namespace GNetworking.Managers
{
    public class ChatManager : GameService
    {
        public Dictionary<NetConnection, User> Users = new Dictionary<NetConnection, User>();
        public List<ChatChannel> Channels = new List<ChatChannel>();
        private NetworkServer server;
        private readonly PersonNameGenerator _nameGenerator;
        private readonly ChatChannel _globalChannel;
        public ChatManager() : base("chat service")
        {
            // random name generator
            _nameGenerator = new PersonNameGenerator();
            server = GameServiceManager.GetService<NetworkServer>();
            server.OnClientConnectionSuccessful += OnClientConnected;
            server.OnClientDisconnected += OnClientDisconnected;
            _globalChannel = new ChatChannel
            {
                Name = "Global",
                Participants = null, // Include everyone
            };

            Channels.Add(_globalChannel);
        }

        public void OnClientConnected(NetConnection client)
        {
            Log.Information("Client has conected {connection}", client);
            var user = new User(_nameGenerator.GenerateRandomFirstName());
            Log.Information("Random name assigned to user {name}", user.Nickname);
            Users.Add(client, user);

            var server = GameServiceManager.GetService<NetworkServer>();

            var userInfo = new UserInfoMessage
            {
                UserData = user,
                // assign default user channels
                AssignedChannels = new List<ChatChannel>
                {
                    _globalChannel
                }
            };

            // send the client a message with their user data
            server.NetworkPipe.SendClient(client, "userInfo", userInfo);
        }

        public void OnClientDisconnected(NetConnection client)
        {
            if (Users.ContainsKey(client))
            {
                var user = Users[client];
                Users.Remove(client);
                Log.Information("Client has left the server {user} on endpoint {connection}", user.Nickname, client);
            }
            else
            {
                Log.Error("Failed to remove user, not found with connection {connection}", client);
            }
        }

        public override void Start()
        {
            // load database
        }

        public override void Stop()
        {
            // close database and save
        }

        public override void Update()
        {
            // nothing to do here
        }

        /// <summary>
        /// Say Text Handler
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sender"></param>
        /// <param name="msg"></param>
        public static bool SayMessageReceived(string name, NetConnection sender, NetPipeMessage msg)
        {
            // Retrieve the message
            var chatMessage = msg.GetMessage<ChatMessage>();

            // this can be null if someone is sending data which is erroneous or they're sending the wrong arguments
            if (chatMessage == null) return false;

            // process message and send to clients which should recieve the message
            var server = GameServiceManager.GetService<NetworkServer>();

            Log.Information("Recieved message:" + chatMessage.Text);

            // todo: add other channel support

            // send message to all clients
            server.NetworkPipe.SendReliable("say", chatMessage);

            return true;
        }
    }

}
