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
    public class ServerChatManager : GameService
    {
        public Dictionary<NetConnection, User> Users = new Dictionary<NetConnection, User>();
        public List<ChatChannel> Channels = new List<ChatChannel>();
        private readonly NetworkServer _server;
        private readonly PersonNameGenerator _nameGenerator;
        private readonly ChatChannel _globalChannel;
        public ServerChatManager() : base("chat service")
        {
            _globalChannel = new ChatChannel
            {
                Name = "Global",
                Participants = null, // Include everyone
            };

            Channels.Add(_globalChannel);

            // random name generator
            _nameGenerator = new PersonNameGenerator();

            // server configuration for the messages we need to handle
            _server = GameServiceManager.GetService<NetworkServer>();

            // connection handlers - mapping connections to user information
            _server.OnClientConnectionSuccessful += OnClientConnected;
            _server.OnClientDisconnected += OnClientDisconnected;



        }

        public void OnClientConnected(NetConnection client)
        {
            Log.Information("Client has conected {connection}", client);

            var user = new User(_nameGenerator.GenerateRandomFirstName());

            Log.Information("Random name assigned to user {name}", user.Nickname);
            Users.Add(client, user);

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
            _server.NetworkPipe.SendClient(client, "UserInfo", userInfo);
        }

        public void OnClientDisconnected(NetConnection client)
        {
            if (Users.ContainsKey(client))
            {
                var user = Users[client];
                Users.Remove(client);
                Log.Information("Client has left the _server {user} on endpoint {connection}", user.Nickname, client);
            }
            else
            {
                Log.Error("Failed to remove user, not found with connection {connection}", client);
            }
        }

        public override void Start()
        {
            // register network messages which the server can handle
            _server.NetworkPipe.On("say", SayMessageReceived);

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
        public bool SayMessageReceived(string name, NetConnection sender, NetPipeMessage msg)
        {
            // Retrieve the message
            var chatMessage = msg.GetMessage<Message>();

            // this can be null if someone is sending data which is erroneous or they're sending the wrong arguments
            if (chatMessage == null) return false;

            // make sure this user has been registered properly and has a valid session
            if (Users.ContainsKey(sender))
            {
                // apply the user info to the message (only safe to do serverside)
                chatMessage.User = Users[sender];
                Log.Debug("Found valid user for {sender} attaching User to message for retransmission", sender);
            }
            else
            {
                // exit, someone is probably trying to fake being another user
                Log.Error("Someone caught trying to be another user: {sender}", sender);
                return false;
            }

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
