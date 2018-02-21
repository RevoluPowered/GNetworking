﻿using System.Collections.Generic;
using Core.Service;
using Lidgren.Network;
using RandomNameGeneratorLibrary;
using Serilog;
using System.Linq;
using GNetworking.Data;
using GNetworking.Messages;


namespace GNetworking.Managers
{
    public class ServerChatManager : GameService
    {
        public Dictionary<User, NetConnection > Users = new Dictionary<User, NetConnection>();
        public List<ChatChannel> Channels = new List<ChatChannel>();
        private readonly NetworkServer _server;
        private readonly PersonNameGenerator _nameGenerator;
        private readonly ChatChannel _globalChannel;
        public ServerChatManager() : base("chat service")
        {
            _globalChannel = new ChatChannel
            {
                Name = "Main",
                Participants = new List<User>(), // Include everyone
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
            Users.Add(user, client);

            var userInfo = new UserInfoMessage
            {
                UserData = user
            };

            _globalChannel.Participants.Add(user);

            // send the client a message with their user data
            _server.NetworkPipe.SendClient(client, "UserInfo", userInfo);
        }

        /// <summary>
        /// Returns the user by connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public User GetUser( NetConnection connection )
        {
            return Users.SingleOrDefault(selector => selector.Value == connection).Key;
        }

        public void OnClientDisconnected(NetConnection client)
        {
            var user = GetUser(client);

            if (user != null)
            {

                if (Users.Remove(user))
                {
                    Log.Information("Client has left the _server {user} on endpoint {connection}", user, client);
                    _globalChannel.Participants.Remove(user);
                }
                else
                {
                    Log.Error("Failed to remove user from dictionary {connection} user info {user}", client, user);
                }
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

            // get the sender user
            var senderUser = GetUser(sender);

            // make sure this user has been registered properly and has a valid session
            if (senderUser != null)
            {
                // apply the user info to the message (only safe to do serverside)
                chatMessage.User = senderUser;
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

            // if channel is not supplied then it's for the global channel
            var channel = chatMessage.Channel ?? _globalChannel;

            // add conversation message to history
            channel.Messages.Add(chatMessage);

            var participantsCount = channel.Participants.Count;
            var userConnections = new List<NetConnection>();

            // send message to all participants of the channel
            foreach (var user in channel.Participants)
            {
                userConnections.Add(Users[user]);
            }

            // send message to all clients
            server.NetworkPipe.SendReliable("say", chatMessage, userConnections);
            return true;
        }
    }

}
