using System.Collections.Generic;
using GNetworking.Data;

namespace GNetworking.Data
{
    public class ChatChannel
    {
        public string Name { get; set; }
        public List<User> Participants { get; set; }
        public List<Message> Messages = new List<Message>();
    }
}
