using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using Steamworks;
using TheLongDriveSyncRadio.Models;

namespace TheLongDriveSyncRadio
{
    public static class AudioSyncManager
    {
        private static string _currentSongFileName = "";
        private static double _currentPlaybackTime = 0.0;
        private static int _currentPlaybackState = 0; // 0 = stopped, 1 = playing, 2 = paused
        private static long _lastSyncTimestamp = 0;
        private static Dictionary<CSteamID, AudioSyncPacket> _remotePlayerSync = new Dictionary<CSteamID, AudioSyncPacket>();
        
        // Délai de synchronisation en millisecondes
        private const int SYNC_INTERVAL = 2000; // Synchronize tous les 2 secondes
        private const int ACCEPTABLE_DRIFT = 500; // Drift acceptable en millisecondes

        public static string GetCurrentSongFileName()
        {
            return _currentSongFileName;
        }

        public static double GetCurrentPlaybackTime()
        {
            return _currentPlaybackTime;
        }

        public static int GetCurrentPlaybackState()
        {
            return _currentPlaybackState;
        }

        public static void SetCurrentSong(string fileName, double startTime = 0.0)
        {
            _currentSongFileName = fileName;
            _currentPlaybackTime = startTime;
            _currentPlaybackState = 1; // playing
            _lastSyncTimestamp = GetServerTimestamp();
            
            MelonLogger.Msg($"[SYNC] ♫ Chanson définie: {fileName} @ {startTime:F2}s");
        }

        public static void UpdatePlaybackState(int state, double currentTime)
        {
            _currentPlaybackState = state;
            _currentPlaybackTime = currentTime;
            string stateStr = state == 1 ? "▶ Playing" : state == 2 ? "⏸ Paused" : "⏹ Stopped";
            MelonLogger.Msg($"[SYNC] État: {stateStr} @ {currentTime:F2}s");
        }

        public static void HandleRemoteSync(CSteamID playerId, AudioSyncPacket packet)
        {
            try
            {
                string playerName = SteamFriends.GetFriendPersonaName(playerId);
                
                if (!_remotePlayerSync.ContainsKey(playerId))
                {
                    _remotePlayerSync[playerId] = packet;
                    MelonLogger.Msg($"[SYNC] Nouvel auditeur ajouté: {playerName}");
                }
                else
                {
                    _remotePlayerSync[playerId] = packet;
                }

                // Calcul du délai réseau
                long currentTimestamp = GetServerTimestamp();
                long networkDelay = currentTimestamp - packet.timestamp;

                // Vérification du drift
                double timeDrift = Math.Abs(packet.currentTime - (_currentPlaybackTime + (networkDelay / 1000.0)));

                if (timeDrift > (ACCEPTABLE_DRIFT / 1000.0))
                {
                    MelonLogger.Msg($"[SYNC] ⚠ Drift {playerName}: {timeDrift:F3}s | Délai: {networkDelay}ms | Chanson: {packet.fileName}");
                    
                    // Si c'est une chanson différente, charger la nouvelle
                    if (packet.fileName != _currentSongFileName)
                    {
                        MelonLogger.Msg($"[SYNC] 🔄 Synchronisation chanson: {packet.fileName}");
                        SetCurrentSong(packet.fileName, packet.currentTime);
                        string songPath = System.IO.Path.Combine(Game.GetCustomRadioPath(), packet.fileName);
                        Game.LoadSong(songPath, (int)packet.currentTime);
                        MelonLogger.Msg($"[AudioSync] Synchronisation avec {playerId}: nouvelle chanson {packet.fileName}");
                    }
                    // Sinon, ajuster le timing
                    else if (timeDrift > (ACCEPTABLE_DRIFT / 1000.0) * 2)
                    {
                        MelonLogger.Msg($"[AudioSync] Ajustement du timing pour {playerId}: {timeDrift}s");
                        UpdatePlaybackState(packet.playbackState, packet.currentTime);
                    }
                }

                MelonLogger.Msg($"[AudioSync] Sync reçue de {playerId}: {packet.fileName} @ {packet.currentTime}s (délai réseau: {networkDelay}ms)");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[AudioSync] Erreur lors du traitement de la synchronisation: {ex.Message}");
            }
        }

        public static void BroadcastSync()
        {
            if (Game.IsServer())
            {
                var lobbyId = Game.GetLobbyId();
                int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
                
                var syncPacket = new AudioSyncPacket
                {
                    fileName = _currentSongFileName,
                    currentTime = _currentPlaybackTime,
                    timestamp = GetServerTimestamp(),
                    playbackState = _currentPlaybackState
                };

                MelonLogger.Msg($"[SYNC] 📡 Broadcast à {memberCount} joueur(s): {_currentSongFileName} @ {_currentPlaybackTime:F2}s");

                for (int iMember = 0; iMember < memberCount; ++iMember)
                {
                    CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, iMember);
                    if (member != SteamUser.GetSteamID())
                    {
                        try
                        {
                            string memberName = SteamFriends.GetFriendPersonaName(member);
                            Game.SendP2P(member, NetworkHelper.ObjectToByteArray(syncPacket));
                            MelonLogger.Msg($"  ✓ → {memberName}");
                        }
                        catch (Exception ex)
                        {
                            MelonLogger.Warning($"  ✗ Erreur: {ex.Message}");
                        }
                    }
                }
            }
        }

        public static void SendSyncRequest()
        {
            if (!Game.IsServer())
            {
                var request = new AudioSyncRequestPacket
                {
                    currentSongFileName = _currentSongFileName
                };
                
                try
                {
                    string serverName = SteamFriends.GetFriendPersonaName(SteamMatchmaking.GetLobbyOwner(Game.GetLobbyId()));
                    Game.SendP2P(SteamMatchmaking.GetLobbyOwner(Game.GetLobbyId()), 
                        NetworkHelper.ObjectToByteArray(request));
                    MelonLogger.Msg($"[SYNC] 🔗 Demande de sync → {serverName}");
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"[SYNC] ✗ Erreur demande sync: {ex.Message}");
                }
            }
        }

        private static long GetServerTimestamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public static void ResetSync()
        {
            _currentSongFileName = "";
            _currentPlaybackTime = 0.0;
            _currentPlaybackState = 0;
            _lastSyncTimestamp = 0;
            _remotePlayerSync.Clear();
        }
        private staic void hugo7()
            {for 3 in e : 
                cout("hugo")
    }
}
