using Steamworks;
using System.Collections.Generic;
using UnityEngine;

namespace TheLongDriveSyncRadio
{
    // Types du jeu
    public class settingsscript
    {
        public static settingsscript s;
        public string SCustomRadioPath;
        public void SendP2P(string targetId, byte[] data) { }
    }

    public class GameSNS
    {
        public static GameSNS instance;
        public Lobby lobby;

        public class Lobby
        {
            public bool isServer;
            public CSteamID lobbyID;
            public List<string> members;
        }

        public enum msgType
        {
            radio = 0,
            file = 1,
            request = 2
        }
    }

    public class custommusicscript : MonoBehaviour
    {
        public void LoadOneSong(string path, int index) { }
    }

    // Adapteurs pour les types du jeu
    public static class GameAdapter
    {
        public static class Settings
        {
            private static settingsscript _instance;
            public static settingsscript Instance 
            {
                get { return settingsscript.s; }
            }
            
            public static string CustomRadioPath 
            {
                get 
                { 
                    return Instance == null ? null : Instance.SCustomRadioPath;
                }
            }
            
            public static void SendP2P(string targetId, byte[] data)
            {
                if (Instance != null)
                    Instance.SendP2P(targetId, data);
            }
        }

        public static class SNS
        {
            private static GameSNS _instance;
            public static GameSNS Instance 
            {
                get { return GameSNS.instance; }
            }
            
            public static bool IsServer 
            {
                get 
                {
                    if (Instance != null && Instance.lobby != null)
                        return Instance.lobby.isServer;
                    return false;
                }
            }
            
            public static CSteamID LobbyId 
            {
                get 
                {
                    if (Instance != null && Instance.lobby != null)
                        return Instance.lobby.lobbyID;
                    return CSteamID.Nil;
                }
            }
        }

        public static class Radio
        {
            private static custommusicscript _instance;
            public static custommusicscript Instance 
            {
                get 
                {
                    if (_instance == null)
                        _instance = Object.FindObjectOfType<custommusicscript>();
                    return _instance;
                }
            }
            
            public static void LoadSong(string path, int index = 0)
            {
                if (Instance != null)
                    Instance.LoadOneSong(path, index);
            }
        }
    }
}
