using Steamworks;
using UnityEngine;

namespace TheLongDriveSyncRadio
{
    public static class Game
    {
        public static bool IsInitialized { get; private set; }

        public static void Initialize()
        {
            var hasSettings = GameAdapter.Settings.Instance != null;
            var hasSNS = GameAdapter.SNS.Instance != null;
            var hasRadio = GameAdapter.Radio.Instance != null;
            IsInitialized = hasSettings && hasSNS && hasRadio;
        }

        public static void Update()
        {
            Initialize();
        }

        public static string GetCustomRadioPath()
        {
            return GameAdapter.Settings.CustomRadioPath;
        }

        public static void LoadSong(string path, int index = 0)
        {
            GameAdapter.Radio.LoadSong(path, index);
        }

        public static void SendP2P(CSteamID id, byte[] data)
        {
            GameAdapter.Settings.SendP2P(id.m_SteamID.ToString(), data);
        }

        public static bool IsServer()
        {
            return GameAdapter.SNS.IsServer;
        }

        public static CSteamID GetLobbyId()
        {
            return GameAdapter.SNS.LobbyId;
        }
    }
}
