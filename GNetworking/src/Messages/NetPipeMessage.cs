using Serilog;

namespace GNetworking
{
    using System;

    [Serializable]
    public class NetPipeMessage
    {
        public NetPipeMessage( string name, object message )
        {
            this.Name = name;
            this.NetworkEvent = message;
        }

        /// <summary>
        /// Returns the message as the correct type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetMessage<T>() where T: class
        {
            try
            {
                return NetworkEvent as T;
            }
            catch (Exception e)
            {
                Log.Error("NetPipeMessage.GetMessage failed: {err}", e.ToString());
                return null;
            }
        }

        public string Name;
        public object NetworkEvent;
    }
}