// LICENSE
// GNetworking, SimpleUnityClient and GameServer are property of Gordon Alexander MacPherson
// No warantee is provided with this code, and no liability shall be granted under any circumstances.
// All rights reserved GORDONITE LTD 2018 � Gordon Alexander MacPherson.

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
