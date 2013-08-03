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


    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class KSPMultiplayer : MonoBehaviour
    {
        public static int ver = 1;
        bool isLoaded = false;
        public void Update()
        {
            
            KSPMPlayer.ClientHandleMessages();
            KSPMPlayer.HostHandleMessages();
        }
        public void OnGUI()
        {  

        }
        public void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
        }
       
    }

    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class ConnectionWindow : MonoBehaviour
    {
        public static String IP = "127.0.0.1";
        public static String Port = "25566";
        public static String username = "";
        public static bool HostEnabled = true;
        public static bool ClientEnabled = true;
        public static bool NicknameEnabled = true;
        private static int windowHeight = 100;
        private static int windowWidth = 400;
        private Rect windowLocation = new Rect(Screen.width - windowWidth, (Screen.height / 2) - (windowHeight / 2), windowWidth, windowHeight);
        public ConnectionWindow()  {
            
        }

        public void OnGUI()
        {
            GUI.skin = HighLogic.Skin;
            windowLocation = GUILayout.Window(1, windowLocation, renderWindow, "Multiplayer", GUILayout.ExpandWidth(true));
        }

        public void renderWindow(int windowID)
        {
            GUI.skin = HighLogic.Skin;
            GUILayout.BeginVertical();
            GUILayout.Label("IP Address:");
            IP = GUILayout.TextArea(IP);
            GUILayout.Label("Port:");
            Port = GUILayout.TextArea(Port);

            GUI.enabled = NicknameEnabled;
            GUILayout.Label("Nickname:");
            username = GUILayout.TextArea(username);
            GUI.enabled = ClientEnabled;
            if (GUILayout.Button("Connect"))
            {
                KSPMPlayer.ClientConnect(IP, Port);
            }
            GUI.enabled = HostEnabled;
            if (GUILayout.Button("Host Server"))
            {
                KSPMPlayer.HostServer(Port);
            }
            GUI.enabled = true;
            if (GUILayout.Button("Test Serialization"))
            {
                KSPMPlayer.SendVessels();
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
        public void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
        }
    }
    
    class MultiplayerVessel
    {
        public Vessel vessel;
        bool isLoaded = false;
        ShipConstruct ship;

        public MultiplayerVessel(String name)
        {
            // Save state to reload into because the stupid methods below changes them. Who the fuck changes global methods inside class methods anyway. 
            Vessel activeVessel = FlightGlobals.ActiveVessel;
            List<Part> activeParts = activeVessel.parts;
            Part activeRootPart = activeVessel.rootPart;

            // ShipConstruct nship = ShipConstruction.LoadShip(name);

            ConfigNode root = ConfigNode.Load(name);
            if (root != null)
            {
                if (root.nodes.Count > 0)
                {
                    ship = new ShipConstruct();
                    ship.LoadShip(root);

                    ShipConstruction.PutShipToGround(ship, activeVessel.transform);

                    //foreach (Part p in activeVessel.parts) p.gameObject.SetActive(false);
                    Staging.SetStageCount(activeParts);

                    EditorLogic.startPod = activeRootPart;

                    activeRootPart.gameObject.SetActive(true);

                    initializeVessel();

                    isLoaded = true;
                }
            }
            else
            {
                KSPMulti.KSPMultiplayer.print("ROOT IS NULL");
            }

        }

        public void initializeVessel()
        {
            Part localRoot = ship.parts[0].localRoot;
            Vessel v = localRoot.gameObject.AddComponent<Vessel>();
            v.id = Guid.NewGuid();
            v.vesselName = ship.shipName;
            v.Landed = true;
            v.landedAt = "In Multiplayer Flight";
            v.Initialize(true);
            Part part7 = ShipConstruction.findFirstPod_Placeholder(ship.parts[0]);
            if (part7 != null)
            {
                v.SetReferenceTransform(part7);
            }
            else
            {
                Part part8 = ShipConstruction.findFirstControlSource(v);
                if (part8 != null)
                {
                    v.SetReferenceTransform(part8);
                }
            }
            this.vessel = v;
        }
    }
   
    public enum PacketTypes
    {
        ServerAuthenticate,
        ServerUsernameTaken,
        ServerConnectionSuccessful,
        ClientVesselRegister,
        ServerNewUser
    }
    public interface KSPPacket
    {
        PacketTypes MessageType { get; }

        void Encode(NetOutgoingMessage om);

        void Decode(NetIncomingMessage im);

    }
    class ServerConnectionSuccessful : KSPPacket
    {
        public ServerConnectionSuccessful()
        {

        }

        public PacketTypes MessageType
        {
            get { return PacketTypes.ServerConnectionSuccessful; }
        }
        public void Encode(NetOutgoingMessage om)
        {
            //Nothing to encode
        }
        public void Decode(NetIncomingMessage im)
        {
            // Nothing to decode
        }
        public static void HandleMessage(NetIncomingMessage im)
        {
            try
            {
              /*  string applicationRootPath = KSPUtil.ApplicationRootPath;
                string path = System.IO.Path.Combine(applicationRootPath, "KSPMultiplayer");
                System.IO.Directory.Delete(path, true);
                */
            }
            catch
            {
                KSPMultiplayer.print("Deletion of multiplayer folder failed");
            }
            ConnectionWindow.ClientEnabled = false;
            ConnectionWindow.NicknameEnabled = false;
            HighLogic.SaveFolder = "KSPMultiplayer";
            HighLogic.LoadScene(GameScenes.SPACECENTER);
        }
    }
    class ServerUsernameTaken : KSPPacket
    {
        public ServerUsernameTaken()
        {

        }

        public PacketTypes MessageType
        {
            get { return PacketTypes.ServerUsernameTaken; }
        }
        public void Encode(NetOutgoingMessage om)
        {
            //Nothing to encode
        }
        public void Decode(NetIncomingMessage im)
        {
            // Nothing to decode
        }
        public static void HandleMessage(NetIncomingMessage im)
        {
            PopupDialog.SpawnPopupDialog("Server Error", "Username has already been taken", "Okay", true, HighLogic.Skin);
        }
    }
    class ServerNewUser : KSPPacket
    {
        String username;
        public ServerNewUser() { }
        public ServerNewUser(String username)
        {
            this.username = username;
        }

        public PacketTypes MessageType
        {
            get { return PacketTypes.ServerNewUser; }
        }
        public void Encode(NetOutgoingMessage om)
        {
            om.Write(this.username);
        }
        public void Decode(NetIncomingMessage im)
        {
            this.username = im.ReadString();
        }
        public static void HandleMessage(NetIncomingMessage im)
        {
            ServerNewUser packet = new ServerNewUser();
            packet.Decode(im);
            new KSPClient(packet.username);
        }
    }
    class ClientVesselRegister : KSPPacket
    {
        String playerName = "";
        String vInfo = "";
        String vesselName = "";
        float positionX = 0;
        float positionY = 0;
        float positionZ = 0;
        float rotationX = 0;
        float rotationY = 0;
        float rotationZ = 0;
        float rotationW = 0;
        public ClientVesselRegister()
        {

        }
        public ClientVesselRegister(String vesselName, String vInfo, String playerName, Vector3 position, Quaternion rotation)
        {
            this.vesselName = vesselName;
            this.vInfo = vInfo;
            this.playerName = playerName;
            this.positionX = position.x;
            this.positionY = position.y;
            this.positionZ = position.z;
            this.rotationX = rotation.x;
            this.rotationY = rotation.y;
            this.rotationZ = rotation.z;
            this.rotationW = rotation.w;
        }

        public PacketTypes MessageType
        {
            get { return PacketTypes.ClientVesselRegister; }
        }
        public void Encode(NetOutgoingMessage om)
        {
            om.Write(vesselName);
            om.Write(vInfo);
            om.Write(playerName);
            om.Write(positionX);
            om.Write(positionY);
            om.Write(positionZ);
            om.Write(rotationX);
            om.Write(rotationY);
            om.Write(rotationZ);
            om.Write(rotationW);
        }
        public void Decode(NetIncomingMessage im)
        {
            this.vesselName = im.ReadString();
            this.vInfo = im.ReadString();
            this.playerName = im.ReadString();
            this.positionX = im.ReadFloat();
            this.positionY = im.ReadFloat();
            this.positionZ = im.ReadFloat();
            this.rotationX = im.ReadFloat();
            this.rotationY = im.ReadFloat();
            this.rotationZ = im.ReadFloat();
            this.rotationW = im.ReadFloat();
        }
        public static void HandleMessage(NetIncomingMessage im, bool hostMessage = false)
        {

            ClientVesselRegister cvr = new ClientVesselRegister();
            cvr.Decode(im);
            if (KSPMPlayer.isHost && hostMessage)
            {
                KSPClient kspClient = KSPClient.getClient(im.SenderConnection);
                if (kspClient.name == cvr.playerName && !KSPClient.hostVesselExist(kspClient.name, cvr.vesselName))
                {
                    kspClient.addVessel(cvr.vesselName);
                    // DO SOME SANITATION OF PATH HERE
                    KSPMPlayer.HostSendToAll(new ClientVesselRegister(cvr.vesselName, cvr.vInfo, cvr.playerName, new Vector3(cvr.positionX, cvr.positionY, cvr.positionZ), new Quaternion(cvr.rotationX, cvr.rotationY, cvr.rotationZ, cvr.rotationW)), im.SenderConnection);
                }
            }
            else
            {
                KSPMultiplayer.print("CREATING SHIT" + cvr.playerName + ".." + cvr.vesselName + "_V2.craft");
                KSPMultiplayer.print("Hmm DOES THIS FILE EXIST? I THINK: " + File.Exists("Plugins/KSPMulti/TempFiles/" + cvr.playerName + ".." + cvr.vesselName + cvr.GetHashCode() + "_V2.craft"));
                FileStream fs = new FileStream("Plugins/KSPMulti/TempFiles/" + cvr.playerName + ".." + cvr.vesselName + cvr.GetHashCode() + "_V2.craft", FileMode.CreateNew);

                byte[] text = new UTF8Encoding(true).GetBytes(cvr.vInfo);
                fs.Write(text, 0, text.Length);
                fs.Close(); 
               
                // Only way to ensure I dont have to rewrite a ton of decompiled shit from KSP. :(
                MultiplayerVessel aNewVessel = new MultiplayerVessel("./Plugins/KSPMulti/TempFiles/" + cvr.playerName + ".." + cvr.vesselName + cvr.GetHashCode() + "_V2.craft");
                aNewVessel.vessel.SetPosition(new Vector3(cvr.positionX, cvr.positionY, cvr.positionZ), true);
                aNewVessel.vessel.SetRotation(new Quaternion(cvr.rotationX, cvr.rotationY, cvr.rotationZ, cvr.rotationW));
                KSPClient ksCli = KSPClient.getClient(cvr.playerName);
                ksCli.addVessel(aNewVessel);
            }
        }
    }
    class ServerAuthenticate : KSPPacket
    {
        public String name;
        public String version;
        public ServerAuthenticate(NetIncomingMessage im)
        {
            this.Decode(im);       
        }
        public ServerAuthenticate()
        {

        }
        public ServerAuthenticate(String name, String version)
        {
            this.name = name;
            this.version = version;
        }
        public PacketTypes MessageType
        {
            get { return PacketTypes.ServerAuthenticate; }
        }
        public void Decode(NetIncomingMessage im)
        {
            this.name = im.ReadString();
            this.version = im.ReadString();
        }

        public void Encode(NetOutgoingMessage om)
        {
            om.Write(this.name);
            om.Write(this.version);
        }
        public static void HandleMessage (NetIncomingMessage im, bool hostMessage = false) {
            ServerAuthenticate packet = new ServerAuthenticate(im);
            if (KSPMPlayer.isHost && hostMessage == true)
            {
                if (!KSPClient.clientExist(im.SenderConnection))
                {
                    if (KSPClient.nameExist(packet.name))
                    {
                        KSPMPlayer.HostSendMessage(im.SenderConnection, new ServerUsernameTaken());
                        im.SenderConnection.Disconnect("Change your username");
                    }
                    else
                    {
                        new KSPClient(im.SenderConnection, packet.name);
                        KSPMPlayer.HostSendMessage(im.SenderConnection, new ServerConnectionSuccessful());
                    }
                }

            }
            else
            {
                KSPMultiplayer.print("Client SHOULD NEVER GET THIS PACKET TYPE WTF IS WRONG WITH YOU RUINING THE GAME LIKE THAT");
            }
        }
    }
    class KSPMPlayer
    {
        public static NetServer netServer;
        public static NetClient netClient;
        public static bool isHost = false;

        public static int serverVersion = 1;


        public static void HostServer(String port)
        {
            if (netServer == null)
            {
                int numPort;
                try { numPort = Convert.ToInt32(port); }
                catch (FormatException e) { KSPMultiplayer.print("User tried to input a string"); numPort = 25566; }
                catch (OverflowException e) { KSPMultiplayer.print("User tried to input a too high value"); numPort = 25566; }

                NetPeerConfiguration config = new NetPeerConfiguration("KSPMultiplayer " + KSPMPlayer.serverVersion)
                 {

                     Port = numPort,
                 };
                ConnectionWindow.IP = "127.0.0.1";
                ConnectionWindow.HostEnabled = false;
                config.EnableMessageType(NetIncomingMessageType.WarningMessage);
                config.EnableMessageType(NetIncomingMessageType.VerboseDebugMessage);
                config.EnableMessageType(NetIncomingMessageType.ErrorMessage);
                config.EnableMessageType(NetIncomingMessageType.Error);
                config.EnableMessageType(NetIncomingMessageType.DebugMessage);
                config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
                netServer = new NetServer(config);
                netServer.Start();
                isHost = true;
            }
        }
        public static void ClientConnect(String IP, String port)
        {
            if (netClient != null)
            {
                netClient.Disconnect("Client reconnecting");
            }
            int numPort;
            try { numPort = Convert.ToInt32(port); }
            catch (FormatException e) {  KSPMultiplayer.print("User tried to input a string"); numPort = 25566; }
            catch (OverflowException e) {  KSPMultiplayer.print("User tried to input a too high value");  numPort = 25566;  }

            NetPeerConfiguration config = new NetPeerConfiguration("KSPMultiplayer " + KSPMPlayer.serverVersion);

            config.EnableMessageType(NetIncomingMessageType.WarningMessage);
            config.EnableMessageType(NetIncomingMessageType.VerboseDebugMessage);
            config.EnableMessageType(NetIncomingMessageType.ErrorMessage);
            config.EnableMessageType(NetIncomingMessageType.Error);
            config.EnableMessageType(NetIncomingMessageType.DebugMessage);
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            netClient = new NetClient(config);
            netClient.Start();
            netClient.Connect(new IPEndPoint(NetUtility.Resolve(IP), numPort));
        }


        public static void HostHandleMessages()
        {
            if (netServer != null)
            {
                NetIncomingMessage incomingMessage;

                while ((incomingMessage = netServer.ReadMessage()) != null)
                {
                    switch (incomingMessage.MessageType)
                    {
                        case NetIncomingMessageType.VerboseDebugMessage:
                        case NetIncomingMessageType.DebugMessage:
                        case NetIncomingMessageType.WarningMessage:
                        case NetIncomingMessageType.ErrorMessage:
                            Console.WriteLine(incomingMessage.ReadString());
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            switch ((NetConnectionStatus)incomingMessage.ReadByte())
                            {
                                case NetConnectionStatus.RespondedAwaitingApproval:
                                    incomingMessage.SenderConnection.Approve();
                                    break;
                            }
                            break;
                        case NetIncomingMessageType.Data:
                            var gameMessageType = (PacketTypes)incomingMessage.ReadByte();
                            switch (gameMessageType)
                            {
                                case PacketTypes.ServerAuthenticate:
                                    ServerAuthenticate.HandleMessage(incomingMessage, true);
                                    break;
                                case PacketTypes.ClientVesselRegister:
                                    ClientVesselRegister.HandleMessage(incomingMessage, true);
                                    break;

                            }
                            break;
                    }
                    netServer.Recycle(incomingMessage);
                }
            }
        }
        public static void HostSendMessage(NetConnection client,KSPPacket packet)
        {
            NetOutgoingMessage om = netServer.CreateMessage();
            om.Write((byte)packet.MessageType);
            packet.Encode(om);
            client.SendMessage(om, NetDeliveryMethod.ReliableOrdered, 0);
        }

        public static void HostSendToAll(KSPPacket packet, NetConnection except = null)
        {
            NetOutgoingMessage om = netServer.CreateMessage();
            om.Write((byte)packet.MessageType);
            packet.Encode(om);
            if (except != null)
            {
                netServer.SendToAll(om, except, NetDeliveryMethod.ReliableOrdered, 0);
            }
            else
            {
                netServer.SendToAll(om, NetDeliveryMethod.ReliableOrdered);
            }
        }

        public static void ClientHandleMessages()
        {
            if (netClient != null)
            {
                NetIncomingMessage incomingMessage;

                while ((incomingMessage = netClient.ReadMessage()) != null)
                {
                    switch (incomingMessage.MessageType)
                    {
                        case NetIncomingMessageType.VerboseDebugMessage:
                        case NetIncomingMessageType.DebugMessage:
                        case NetIncomingMessageType.WarningMessage:
                        case NetIncomingMessageType.ErrorMessage:
                            Console.WriteLine(incomingMessage.ReadString());
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            switch ((NetConnectionStatus)incomingMessage.ReadByte())
                            {
                                case NetConnectionStatus.Connected:
                                    ServerAuthenticate sm = new ServerAuthenticate();
                                    KSPMultiplayer.print("CONNECTION CONNECTED!");
                                    sm.name = ConnectionWindow.username;
                                    sm.version = "1";
                                    ClientSendMessage(sm);
                                    // I DONT EVEN REMEMBER WRITING THIS
                                    break;
                            }
                            break;
                        case NetIncomingMessageType.Data:
                            var gameMessageType = (PacketTypes)incomingMessage.ReadByte();
                            switch (gameMessageType)
                            {
                                case PacketTypes.ServerUsernameTaken:
                                    ServerUsernameTaken.HandleMessage(incomingMessage);
                                    break;
                                case PacketTypes.ServerConnectionSuccessful:
                                    ServerConnectionSuccessful.HandleMessage(incomingMessage);
                                   
                                    break;
                                case PacketTypes.ClientVesselRegister:
                                    ClientVesselRegister.HandleMessage(incomingMessage);
                                    break;
                            }
                            break;
                    }
                    netClient.Recycle(incomingMessage);
                }
            }
        }

        public static void ClientSendMessage(KSPPacket packet)
        {
            NetOutgoingMessage om = netClient.CreateMessage();
            om.Write((byte)packet.MessageType);
            packet.Encode(om);
            netClient.SendMessage(om, NetDeliveryMethod.ReliableOrdered);
        }
  

        public static void SendVessels()
        {
            if (FlightGlobals.Vessels != null)
            {
                foreach (Vessel v in FlightGlobals.Vessels)
                {

                    KSPMultiplayer.print("RIGHTYO");
                    if (KSPClient.isMyVessel(v.GetHashCode()))
                    {
                        // FUCK SAKE ERROR IN SQUADS CODE FUCKING LOVELY.
                        if (v.parts == null)
                        {
                            KSPMultiplayer.print("ITS NULL. RUN FOR YOUR LIFES");
                        }

                        KSPMultiplayer.print("OKAY good");
                        new ShipBackup(v.parts).SaveShip("./Plugins/KSPMulti/TempFiles/" + v.GetHashCode() + "_V2.craft");
                        // Honestly this is the easiest way of doing it. 
                        // FUCK IT COPY AND PASTE MSDN
                        try
                        {

                            KSPMultiplayer.print("OKAY SENDING");
                            using (StreamReader sr = new StreamReader("./Plugins/KSPMulti/TempFiles/" + v.GetHashCode() + "_V2.craft"))
                            {
                                String entireFile = sr.ReadToEnd();
                                KSPMPlayer.ClientSendMessage(new ClientVesselRegister(v.name, entireFile, ConnectionWindow.username, v.GetWorldPos3D(), v.rootPart.transform.rotation));
                            }
                        }
                        catch (Exception e)
                        {
                            KSPMultiplayer.print("THIS IS WHERE I WOULD PROBABLY TELL YOU THERE IS AN ERROR");
                        }
                    }
               }
            }
        }
    }
}
