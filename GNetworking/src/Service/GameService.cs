// LICENSE
// GNetworking, SimpleUnityClient and GameServer are property of Gordon Alexander MacPherson
// No warantee is provided with this code, and no liability shall be granted under any circumstances.
// All rights reserved GORDONITE LTD 2018 � Gordon Alexander MacPherson.

namespace Core.Service
{
    using System;
    using System.Collections.Generic;
    using Serilog;

    public abstract class GameService
    {
        /// <summary>
        /// Initializes an Instance of the game service
        /// </summary>
        /// <param name="name">The service name</param>
        /// <param name="autoStart">Should the service auto start when registered?</param>
        protected GameService(string name, bool autoStart = false)
        {
            // Set the name of the service
            this.Name = name;
            this.AutoStart = autoStart;
        }

        /// <summary>
        /// Auto start service when registered?
        /// </summary>
        public bool AutoStart { get; private set; }

        /// <summary>
        /// Human readable name of the service
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Called when the service starts
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// Called when the service stops
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// Called when the service updates
        /// </summary>
        public abstract void Update();
    }
}
