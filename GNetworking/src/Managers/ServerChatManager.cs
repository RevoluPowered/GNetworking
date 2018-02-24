using System.Collections.Generic;
using Core.Service;
using Lidgren.Network;
using RandomNameGeneratorLibrary;
using Serilog;
using System.Linq;
using System.Text.RegularExpressions;
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
                Name = "Global",
                Participants = new List<User>(),
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

        /// <summary>
        /// Get Channel by name
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        private ChatChannel GetChannelByName(string channel)
        {
            return Channels.SingleOrDefault(s => s.Name == channel);
        }

        /// <summary>
        /// Get all the channels this user is a member of
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private List<ChatChannel> GetChannelByUser(User user)
        {
            return Channels.FindAll(channel => channel.Participants.Contains(user));
        }

        public void OnClientConnected(NetConnection client)
        {
            Log.Information("Client has conected {connection}", client);

            var user = new User(_nameGenerator.GenerateRandomFirstName());
            
            Log.Information("Random name assigned to user {name}", user.Nickname);

            Users.Add(user, client);
            
            _globalChannel.Participants.Add(user);

            var userInfo = new UserInfoMessage
            {
                UserData = user,
                ChatChannels = new List<ChatChannel> {
                    _globalChannel
                }
            };

            // send the client a message with their user data
            _server.NetworkPipe.SendClient(client, "UserInfo", userInfo);
            _server.NetworkPipe.SendReliable("ChannelUpdate", _globalChannel);
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

        /// <summary>
        /// Get user by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public User GetuserByName(string name)
        {
            return Users.SingleOrDefault(selector => selector.Key.Nickname == name).Key;
        }

        public void OnClientDisconnected(NetConnection client)
        {
            var user = GetUser(client);

            if (user != null)
            {
                if (Users.Remove(user))
                {
                    Log.Information("Client has left the server {user} on endpoint {connection}", user.Nickname, client.RemoteEndPoint.Address);
                   
                    var deleteChannels = new List<ChatChannel>();
                    foreach (var channel in Channels)
                    {
                        // find the participant by user
                        var participantFind = channel.Participants.Find(u => u == user);
                        if (participantFind != null)
                        {
                            // remove participant from the channel
                            channel.Participants.Remove(participantFind);

                            // queue for deletion if no users left in the group
                            // we can't delete the global channel
                            if (channel != _globalChannel && channel.Participants.Count == 0)
                            {
                                deleteChannels.Add(channel);
                            }
                            else if(channel.Participants.Count > 0) // when the channel still has members update their online member count
                            {
                                _server.NetworkPipe.SendReliable("ChannelUpdate", channel, GetParticipantConnections(channel));
                            }
                        }
                    }

                    // remove channels which have no users in them
                    foreach (var channel in deleteChannels)
                    {
                        Channels.Remove(channel);
                    }
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
            _server.NetworkPipe.On("request-nickname-change", NicknameChangeRequest);
            _server.NetworkPipe.On("request-new-group", RequestNewGroup);
            _server.NetworkPipe.On("request-invite-user", RequestInviteUser);
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
        /// Clean the string - removes all special characters
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string CleanString(string str)
        {
            // remove special chars
            return Regex.Replace(str, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
        }

        /// <summary>
        /// Get channel participant connections, for sending data explicitly to those clients.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        private List<NetConnection> GetParticipantConnections( ChatChannel channel )
        {
            var connections = new List<NetConnection>();
            foreach (var participant in channel.Participants)
            {
                if (Users.ContainsKey(participant))
                {
                    connections.Add(Users[participant]);
                }
            }

            return connections;
        }

        public bool RequestInviteUser(string name, NetConnection sender, NetPipeMessage msg)
        {
            var invite = msg.GetMessage<UserChannelInvite>();

            if (invite != null)
            {
                Log.Information("Processing channel invite {channel} nickname {nickname}", invite.ChannelName, invite.Nickname);

                var user = GetuserByName(invite.Nickname);
                var channel = GetChannelByName(invite.ChannelName);

                if (channel.Participants.Contains(user))
                {
                    // tell client that this user is already in this group
                    _server.NetworkPipe.SendClient(sender, "OnServerNotification", new Message
                    {
                        Text = "Sorry, you can't add a user to the same group twice."
                    });
                    return false;
                }

                if (channel == _globalChannel)
                {
                    _server.NetworkPipe.SendClient(sender, "OnServerNotification", new Message
                    {
                        Text = "Sorry, you can't invite users to the global channel."
                    });
                    return false;
                }

                if (user != null && channel != null)
                {
                    Log.Information("Channel found {channel}, and user found too {user}", channel.Name, user.Nickname);
                    channel.Participants.Add(user);
                    
                    // send all the members of the channel an update with the new member
                    _server.NetworkPipe.SendReliable( "ChannelUpdate", channel, GetParticipantConnections(channel));

                    // tell client that this user is already in this group
                    _server.NetworkPipe.SendClient(sender, "OnServerNotification", new Message
                    {
                        Text = "New user added to the group: " + user.Nickname
                    });
                }
                else
                {
                    Log.Error("Failed to find channel or user {invite}", invite);
                }
            }

            return false;
        }

        public bool RequestNewGroup(string name, NetConnection sender, NetPipeMessage msg)
        {
            var channel = msg.GetMessage<ChatChannel>();

            if (channel != null)
            {
                Log.Information("New group requested: {name}", channel.Name);
                // make sure channel doesn't already exist
                if (GetChannelByName(channel.Name) == null)
                {
                    Channels.Add(channel);
                    channel.Participants = new List<User>
                    {
                        GetUser(sender)
                    };
                    // add this user to the channel
                    _server.NetworkPipe.SendClient(sender, "ChannelUpdate", channel);
                    Log.Information("Group creation completed: {name}", channel.Name);

                    // tell client we created the group, and tell them what to do.
                    _server.NetworkPipe.SendClient(sender, "OnServerNotification", new Message
                    {
                        Text = channel.Name + " <b>group created</b>, it's now in the tabs at the top, click on it! then /invite anotherusername to invite users to your group."
                    });
                }
                else
                {
                    // tell client we couldn't make a group with that name
                    _server.NetworkPipe.SendClient(sender, "OnServerNotification", new Message
                    {
                        Text = "Sorry a group with that name already exists " + channel.Name
                    });
                    Log.Information("Group already exists error {name}", channel.Name);
                    // channel can't be made it already exists.
                    // notify("channel can't be created already exists, try another name")
                }
            }

            return false;
        }

        /// <summary>
        /// Request nickname handler for client
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sender"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool NicknameChangeRequest(string name, NetConnection sender, NetPipeMessage msg)
        {
            var message = msg.GetMessage<User>();
            if (message != null)
            {
                var newNickname = CleanString(message.Nickname);

                if (newNickname.Length > 3)
                {
                    var senderUser = GetUser(sender);
                    var oldnickname = senderUser.Nickname;

                    if(Users.SingleOrDefault(data => data.Key.Nickname == newNickname).Key != null)
                    {
                        Log.Information("Someone tried to change their username and it's already in use.");
                        return false;
                    }
                    else
                    {
                        // update nickname
                        senderUser.Nickname = newNickname;

                        // tell client they have new nickname - only tell the exact client
                        _server.NetworkPipe.SendClient(sender, "OnServerNotification", new Message
                        {
                            Text = "Nickname changed to " + senderUser.Nickname
                        });

                        var channels = GetChannelByUser(senderUser);

                        // update all channels with the new nickname
                        foreach (var channel in channels)
                        {
                            _server.NetworkPipe.SendReliable("ChannelUpdate", channel);
                        }

                        Log.Information("user nickname changed from {oldname} to {nickname}", oldnickname, senderUser.Nickname);

                    }
                }
                else
                {
                    // tell client they have new nickname - only tell the exact client
                    _server.NetworkPipe.SendClient(sender, "OnServerNotification", new Message
                    {
                        Text = "Sorry the new nickname is not long enough"
                    });
                    Log.Information("Someone tried to change their username and it wasn't long enough.");
                }

                return false;
            }
            return true;
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
            var channel = GetChannelByName( chatMessage.ChannelName ) ?? _globalChannel;

            if (channel == null)
            {
                Log.Error("Error channel not found");
            }

            // add conversation message to history
            channel.Messages.Add(chatMessage);

            var participantsCount = channel.Participants.Count;
            var userConnections = new List<NetConnection>();

            // send message to all participants of the channel
            foreach (var user in channel.Participants)
            {
                if (Users.ContainsKey(user))
                {
                    userConnections.Add(Users[user]);
                }
            }

            // send message to all clients
            server.NetworkPipe.SendReliable("say", chatMessage, userConnections);
            return true;
        }
    }

}
