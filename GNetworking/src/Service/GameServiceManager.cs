using System.Diagnostics;

namespace Core.Service
{
    using System;
    using System.Collections.Generic;
    using Serilog;
    /// <summary>
    /// Game service manager
    /// </summary>
    /// <inheritdoc cref="StaticInstance{T}"/>
    public class GameServiceManager
    {
        /// <summary>
        /// Initializes a service manager
        /// </summary>
        public GameServiceManager()
        {
            this.Services = new List<GameService>
            {
                // logging service is always available in games
                new Logging()
            };
        }

        #region singleton
        public static readonly GameServiceManager Instance = new GameServiceManager();
        
        /// <summary>
        /// Shutdown the service manager - prep for quit
        /// </summary>
        public static void Shutdown()
        {
            // stop and shutdown
            Instance.StopServices();
        }
        #endregion

        /// <summary>
        /// Register the game service to the service manager
        /// </summary>
        /// <param name="service">The service which needs added</param>
        public T RegisterService<T>(T service) where T: GameService
        {
            if (Services.Contains(service))
            {
                throw new Exception("Service already registered: " + service.Name);
            }
            else
            {
                Services.Add(service);
            }

            // for event chaining
            return service;
        }


        // This shows you how this works:
        // How unity GetComponent<T> works.
        // public T GetComponent<T>() where T : Component { return this.GetComponent(typeof(T)) as T; }

        /// <summary>
        /// Returns a service of specified type if it exists
        /// </summary>
        /// <typeparam name="T">The service type</typeparam>
        /// <returns>A service you can start and stop</returns>
        public T Service<T>() where T : GameService
        {
            Log.Debug("data expected: {type}", typeof(T));
            foreach (var sv in Services)
            {
                if (sv.GetType() == typeof(T))
                {
                    return sv as T;
                }
                else
                {
                    Log.Debug("failed match: {match} ", sv);
                }

            }

            // null or nothing
            return default(T);
        }

        /// <summary>
        /// Returns service of specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetService<T>() where T : GameService
        {
            return Instance.Service<T>();
        }


        /// <summary>
        /// Called to update services
        /// </summary>
        public void UpdateServices()
        {
            foreach (var service in Services)
            {
                service.Update();
            }
        }

        /// <summary>
        /// Start a service by name
        /// todo: this needs fleshed out for handling service dependencies
        /// </summary>
        /// <param name="name">name of the service</param>
        /// <param name="start">the state of the service</param>
        /// <returns></returns>
        private bool ServiceState(string name, bool start)
        {
            foreach (var service in Services)
            {
                if (service.Name != name) continue;

                if (start)
                {
                    service.Start();
                }
                else
                {
                    service.Stop();
                }

                return true;

            }

            return false;
        }

        /// <summary>
        /// Start the service by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool StartService(string name)
        {
            return ServiceState(name, true);
        }

        /// <summary>
        /// Stop service
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool StopService(string name)
        {
            return ServiceState(name, false);
        }

        /// <summary>
        /// Starts all services which aren't running
        /// </summary>
        public void StartServices()
        {
            foreach (var service in Services)
            {
                try
                {
                    service.Start();
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }
            }
        }

        /// <summary>
        /// Stop all the game services
        /// </summary>
        public void StopServices()
        {
            foreach (var service in Services)
            {
                try
                {
                    service.Stop();
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }
            }
        }

        /// <summary>
        /// The active service list
        /// </summary>
        public List<GameService> Services { get; private set; }
    }
}
