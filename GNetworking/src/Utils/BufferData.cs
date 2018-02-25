// LICENSE
// GNetworking, SimpleUnityClient and GameServer are property of Gordon Alexander MacPherson
// No warantee is provided with this code, and no liability shall be granted under any circumstances.
// All rights reserved GORDONITE LTD 2018 ? Gordon Alexander MacPherson.

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