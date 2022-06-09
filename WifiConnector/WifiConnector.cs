using ManagedNativeWifi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WifiConnector
{
    public class WifiConnector
    {
        public WifiConnector()
        {

        }

        public EventHandler ConnectedToWifi;

        /// <summary>
        /// This method gets all of the available WiFi networks through the adapter or built in wifi card
        /// </summary>
        /// <returns>returns a list of the available networks. Each AvailableNetworkPack has an SSID, ProfileName, 
        /// Interface it is connected or seen through, BSS type that is needed for connection </returns>
        private List<ManagedNativeWifi.AvailableNetworkPack> GetNetworks()
        {
            List<AvailableNetworkPack> availableNetworks = new List<AvailableNetworkPack>();
            //gets the available networks
            //puts them in a list
            try
            {
                availableNetworks = NativeWifi.EnumerateAvailableNetworks().ToList();
            }
            catch
            {
                //return the empty list
                return availableNetworks;
            }

            //return the list
            return availableNetworks;
        }
        public async Task<bool> DisconnectFromNetworkAsync(string ssid)
        {
            //keeps track of whether or not the disconnection was successful
            bool success = false;
            //stores the network that we want to disconnect from
            AvailableNetworkPack toConnect;

            //gets the available networks
            List<ManagedNativeWifi.AvailableNetworkPack> availableNetworks = GetNetworks();

            //if are no available networks, we return false
            if (availableNetworks.Count == 0)
            {
                return false;
            }

            //go through the list of available networks and find the one with a matching SSID
            foreach (AvailableNetworkPack i in availableNetworks)
            {
                //if the network matches the given SSID
                if (i.Ssid.ToString() == ssid)
                {
                    toConnect = i;
                    //we try to disconnect the interface from the given network
                    success = await ManagedNativeWifi.NativeWifi.DisconnectNetworkAsync(toConnect.Interface.Id, new System.TimeSpan(0, 0, 0, 5));
                    break;
                }

            }
            //return whether successful or not
            return success;
        }
        public async Task<bool> DisconnectFromNetworkAsync(InterfaceInfo connectionInterface)
        {
            bool success;
            success = await ManagedNativeWifi.NativeWifi.DisconnectNetworkAsync(connectionInterface.Id, new System.TimeSpan(0, 0, 0, 5));
            return success;

        }
        public async Task<bool> ConnectToNetworkAsync(string ssid)
        {
            //keeps track of whether or not the connection was successful
            bool success = false;
            //stores the network that we can connect to
            AvailableNetworkPack toConnect;

            //gets the available networks
            List<ManagedNativeWifi.AvailableNetworkPack> availableNetworks = GetNetworks();

            //if are no available networks, we return false
            if (availableNetworks.Count == 0)
            {
                return false;
            }

            //go through the list of available networks and find the one with a matching SSID
            foreach (AvailableNetworkPack i in availableNetworks)
            {
                //if the network matches the given SSID
                if (i.Ssid.ToString() == ssid)
                {
                    toConnect = i;

                    //if the signal strength is low, do not connect
                    if (toConnect.SignalQuality < 40)
                    {
                        Console.WriteLine("Signal Quality Low for " + toConnect.Ssid.ToString() + ": " + toConnect.SignalQuality);
                        //disconnect if the quality is low and we are already connected
                        await DisconnectFromNetworkAsync(toConnect.Interface);
                        return false;
                    }

                    //check what networks are already connection
                    List<NetworkIdentifier> connectedNetworks = ManagedNativeWifi.NativeWifi.EnumerateConnectedNetworkSsids().ToList();

                    foreach (NetworkIdentifier j in connectedNetworks)
                    {
                        //if we are already connected to the one we want to be connected to, break
                        if (j.ToString() == toConnect.Ssid.ToString())
                        {
                            return true;
                        }
                    }

                    //if we are not already connected to the network that we want to be, disconnect
                    await DisconnectFromNetworkAsync(toConnect.Interface);

                    //we try to connect the interface to the given network
                    success = await ManagedNativeWifi.NativeWifi.ConnectNetworkAsync(toConnect.Interface.Id, toConnect.Ssid.ToString(), toConnect.BssType, new System.TimeSpan(0, 0, 0, 5));

                    //gets the interface that we just tried to connect to
                    var targetInterface = ManagedNativeWifi.NativeWifi.EnumerateInterfaces().First(x => x.Id == toConnect.Interface.Id);

                    if (targetInterface.State == InterfaceState.Connected)
                    {
                        success = true;
                    }
                    else
                    {
                        success = false;
                    }

                    break;
                }

            }

            if (success)
            {
                ConnectedToWifi.Invoke(this, new EventArgs());
            }

            //return whether successful or not
            return success;

        }
    }
}
