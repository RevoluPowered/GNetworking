// LICENSE
// GNetworking, SimpleUnityClient and GameServer are property of Gordon Alexander MacPherson
// No warantee is provided with this code, and no liability shall be granted under any circumstances.
// All rights reserved GORDONITE LTD 2018 ? Gordon Alexander MacPherson.

using Core.Service;
using Serilog;
using Serilog.Events;
using Serilog.Core;
namespace Core
{
    /// <summary>
    /// Logging system for the game
    /// Works for Server and Client
    /// </summary>
    public class Logging : GameService
    {
        public Logging() : base("logging service")
        {}

        public override void Start()
        {
            InitLogging("Client.log");
        }

        public override void Stop()
        {
            Log.CloseAndFlush();
        }

        public override void Update()
        {
            // nothing needed here
        }

        public bool UnityLogging = false;

        public LoggingLevelSwitch loggingLevel = null;

        /// <summary>
        /// Set the logging level at runtime
        /// Useful when you experience bugs because you can reproduce them and spit them into the logfile.
        /// </summary>
        /// <param name="level"></param>
        public void SetLoggingLevel( LogEventLevel level )
        {
            loggingLevel.MinimumLevel = level;
        }
        
        /// <summary>
        /// Initialise the logger
        /// </summary>
        /// <param name="filename"></param>
        public void InitLogging( string filename )
        {
            if(loggingLevel == null)
            {
                loggingLevel = new LoggingLevelSwitch( LogEventLevel.Debug );
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(loggingLevel)
                .WriteTo.File(filename)
				.WriteTo.Console()
                .CreateLogger();

            Log.Information("logging service started...");
        }
    }
}