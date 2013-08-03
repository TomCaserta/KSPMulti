using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Reflection;
using Lidgren.Network;
using System.IO;


namespace KSPMulti
{
    class KSPClient
    {
        //Hoster
        private static Dictionary<NetConnection, KSPClient> connectionToClient = new Dictionary<NetConnection, KSPClient>();
        public static List<String> allVessels = new List<String>();

        public static List<int> notMyVessel = new List<int>();
        //Client
        private static Dictionary<String, KSPClient> clients = new Dictionary<String, KSPClient>();

        public Dictionary<String, MultiplayerVessel> mpVessel = new Dictionary<String, MultiplayerVessel>();
        public String name;

        public KSPClient(NetConnection clientConnection, String name)
        {
            connectionToClient.Add(clientConnection, this);
            this.name = name;
        }
        public KSPClient(String name)
        {
            this.name = name;
            clients.Add(name, this);
        }

        public bool vesselExist (int vesselID) {
            return mpVessel.ContainsKey(this.name + ":" + vesselID);
        }

        public void addVessel (MultiplayerVessel vessel)
        {
            mpVessel.Add(this.name + ":" + vessel.vessel.GetInstanceID(), vessel);
            allVessels.Add(this.name + ":" + vessel.vessel.GetInstanceID());
            notMyVessel.Add(vessel.vessel.GetHashCode());
        }
        public void addVessel(String vesselName)
        {
            allVessels.Add(this.name + ":" + vesselName);
        }
        // Static shit
        public static bool clientExist(NetConnection clientConnection)
        {
            return connectionToClient.ContainsKey(clientConnection);
        }
        public static KSPClient getClient (NetConnection clientConnection)
        {
            KSPClient returnVal;
            connectionToClient.TryGetValue(clientConnection, out returnVal);
            // FUCK IT THIS SHOULDNT EVER BE NULL ANYWAY
            return returnVal;

        }
        public static KSPClient getClient(String clientName)
        {
            KSPClient returnVal;
            clients.TryGetValue(clientName, out returnVal);
            return returnVal;

        }
        public static bool isMyVessel (int vesselHashCode)
        {
            KSPMultiplayer.print("I have no idea what I am doing.");
            return !notMyVessel.Contains(vesselHashCode);
        }
        public static bool nameExist(String name)
        {
            foreach (System.Collections.Generic.KeyValuePair<NetConnection, KSPClient> client in connectionToClient)
            {
                if (client.Value.name == name)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool hostVesselExist (String username, String vesselName) {
            return allVessels.Contains(username + ":" + vesselName);
        }

    }
}
