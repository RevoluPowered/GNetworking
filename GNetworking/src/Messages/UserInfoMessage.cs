using System.Collections.Generic;
using GNetworking.Data;

namespace GNetworking.Messages
{
    public class UserInfoMessage : BaseMessage
    {
        public User UserData { get; set; }
        public List<ChatChannel> AssignedChannels { get; set; }
    }
}
