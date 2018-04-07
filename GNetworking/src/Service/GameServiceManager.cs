// LICENSE
// GNetworking, SimpleUnityClient and GameServer are property of Gordon Alexander MacPherson
// No warantee is provided with this code, and no liability shall be granted under any circumstances.
// All rights reserved GORDONITE LTD 2018 � Gordon Alexander MacPherson.

using System.Linq;

namespace Core.Service
{
    using System;
    using System.Collections.Generic;
    using Serilog;

    public static class GameServiceManager
    {
        /// <summary>
        /// The active service list
        /// </summary>
        public static List<GameService> Services { get; private set; } = new List<GameService>
        {
            // we always have a logging service
            new Logging()
        };
        
        /// <summary>
        /// Assigns the game service to the service manager
        /// </summary>
        /// <param name="service">The service which needs added</param>
        public static T RegisterService<T>(T service) where T: GameService
        {
            // sanity check it isn't already registered
            var exists = Services.SingleOrDefault(s => s.GetType() == service.GetType());

            if (exists != null || Services.Contains(service))
            {
                Log.Error("You can't add a dulplicate service, ignoring - instance thrown away, returning existing copy!");
                return (T) exists;
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
        public static T GetService<T>() where T : GameService
        {
            Log.Debug("data expected: {type}", typeof(T));
            foreach (var sv in Services)
            {
                if (sv.GetType() == typeof(T))
                {
                    Log.Debug("found service: {match} ", typeof(T));
                    return sv as T;
                }
            }

            Log.Debug("failed to retrieve service: {match} ", typeof(T));
            // null or nothing
            return default(T);
        }

        /// <summary>
        /// Called to update services
        /// </summary>
        public static void UpdateServices()
        {
            // only update activated services
            foreach (var service in Services.FindAll(s => s.Status))
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
        private static bool ServiceState(string name, bool start)
        {
            foreach (var service in Services)
            {
                if (service.Name != name) continue;

                if (start)
                {
                    // if the service is not started start it
                    if (!service.Status)
                    {
                        service.Start();
                        service.Status = true;
                        return true;
                    }
                }
                else
                {
                    // make sure the service has been started
                    if (service.Status)
                    {
                        service.Stop();
                        service.Status = false;
                        return true;
                    }
                }

                Log.Error("Service {name} cannot be started or stopped, state is {status} and request was to change it to {start}", name, service.Status, start);
                return false;

            }

            return false;
        }

        /// <summary>
        /// Start the service by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool StartService(string name)
        {
            return ServiceState(name, true);
        }

        /// <summary>
        /// Stop service
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool StopService(string name)
        {
            return ServiceState(name, false);
        }

        /// <summary>
        /// Starts all services which aren't running
        /// </summary>
        public static void StartServices()
        {
            // start all non started services
            foreach (var service in Services.FindAll(s => !s.Status))
            {
                try
                {
                    service.Start();
                    service.Status = true;
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
        public static void StopServices()
        {
            foreach (var service in Services.FindAll(s => s.Status))
            {
                try
                {
                    service.Stop();
                    service.Status = false;
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }
            }
        }

        /// <summary>
        /// Shutdown the service manager - prep for quit
        /// </summary>
        public static void Shutdown()
        {
            // stop and shutdown
            StopServices();
        }
    }
}
