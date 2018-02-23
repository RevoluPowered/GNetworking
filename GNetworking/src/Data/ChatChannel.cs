using System.Collections.Generic;
using GNetworking.Data;

namespace GNetworking.Data
{
    public class ChatChannel
    {
        public string Name { get; set; }
        public List<User> Participants { get; set; } = new List<User>();
        public List<Message> Messages { get; set; } = new List<Message>();
    }
}
