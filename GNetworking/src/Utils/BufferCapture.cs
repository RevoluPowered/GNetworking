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
using Serilog;

namespace GNetworking
{
    public class BufferCapture
    {
        private MessagePipe pipe;
        private List<BufferData> messages;

        private List<string> filters;

        /// <summary>
        /// Buffer capture stores all network messages which have been sent with the specified filter.
        /// It supports multiple filters too!
        /// </summary>
        /// <param name="_pipe"></param>
        public BufferCapture(MessagePipe _pipe)
        {
            pipe = _pipe;
            messages = new List<BufferData>();
            filters = new List<string>();
        }

        /// <summary>
        /// On Call - called by the NetworkPipe when any data is sent!
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sender"></param>
        /// <param name="args"></param>      
        public void OnCall(int playerid, string name, params object[] args)
        {
            if (filters.Contains(name))
            {
                Log.Information("BufferCapture: stored data send for {name} will retransmit to any players who rejoin.", name);
                messages.Add(new BufferData(playerid, name, args));
            }
        }

        /// <summary>
        /// Returns the message queue
        /// </summary>
        /// <returns></returns>
        public List<BufferData> getMessages()
        {
            return messages;
        }

        public void RemoveDisconnectedMessages(int playerID)
        {
            foreach (BufferData message in new List<BufferData>(messages))
            {
                if (message.player_id_context == playerID)
                {
                    Log.Debug("BufferCapture: removing events for: {playerid}", playerID);

                    messages.Remove(message);
                }
            }
        }

        public void RemoveMessages(int playerID, string message_name)
        {
            foreach (BufferData message in new List<BufferData>(messages))
            {
                if (message.player_id_context == playerID && message.mName == message_name)
                {
                    Log.Debug("BufferCapture: removing events for: {playerid}", playerID);

                    messages.Remove(message);
                }
            }
        }

        // set which events require this filter
        public void Filter(string name)
        {
            Log.Information("BufferCapture: storing events for filter: {name}", name);
            filters.Add(name);
        }
    }
}