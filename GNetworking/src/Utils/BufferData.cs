using System;
using System.Collections;
using System.Threading;
using System.Collections.Generic;
using Lidgren.Network;
using Newtonsoft.Json;
using System.Linq;
using Core;

namespace GNetworking
{
    public class BufferData
    {
        public BufferData(int playerid, string name, params object[] args)
        {
            mName = name;
            mArgs = args;
            player_id_context = playerid;
        }
        public int player_id_context;
        public string mName;
        public object[] mArgs;
    }
}