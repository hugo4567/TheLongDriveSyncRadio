using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MelonLoader;
using Steamworks;
using TheLongDriveSyncRadio.Models;

namespace TheLongDriveSyncRadio
{
    public static class NetworkManager
    {
        private static Random _rand = new Random();
        public static List<CSteamID> AlreadySend = new List<CSteamID>();
        public static Dictionary<uint, List<PacketPart>> ReceivedParts = new Dictionary<uint, List<PacketPart>>();        public static async Task SendFileAsync(CSteamID targetId, AudioFileRequestPacket request)
        {
            string targetName = SteamFriends.GetFriendPersonaName(targetId);
            MelonLogger.Msg($"[RÉSEAU] Envoi de fichiers vers: {targetName} ({targetId.m_SteamID})");
            
            // Obtenir les paramètres optimisés pour la version actuelle
            var networkParams = GameVersionManager.GetOptimizedNetworkParameters();
            var chunkSize = networkParams["chunkSize"];
            var chunkDelay = networkParams["chunkDelay"];
            
            foreach (var audioFile in ModMain.AudioFilesData)
            {
                if (request.excludes.Contains(audioFile.fileName))
                {
                    MelonLogger.Msg($"  ○ {audioFile.fileName} - déjà présent chez {targetName}");
                    continue;
                }

                MelonLogger.Msg($"  ⬆ {audioFile.fileName} ({audioFile.fileSize / 1048576}MB) → {targetName}");

                // Vérifie la mémoire disponible
                if (!await NetworkHelper.MemoryManager.CheckMemoryAvailable(audioFile.fileSize * 2))
                {
                    MelonLogger.Warning($"  ✗ Mémoire insuffisante pour {audioFile.fileName}");
                    continue;
                }

                var fullAudioPacket = await NetworkHelper.ObjectToByteArrayAsync(audioFile, CancellationToken.None);
                var numChunks = (int)Math.Ceiling((double)fullAudioPacket.Length / chunkSize);
                
                MelonLogger.Msg($"  ├─ Fichier: {audioFile.fileSize / 1048576}MB → {numChunks} chunks ({chunkSize / 1024}KB chacun)");

                var packetId = (uint)_rand.Next();

                for (uint i = 0; i < numChunks; i++)
                {
                    int offset = (int)i * chunkSize;
                    int size = Math.Min(chunkSize, fullAudioPacket.Length - offset);
                    var data = new byte[size];
                    Array.Copy(fullAudioPacket, offset, data, 0, size);

                    var packet = new PacketPart
                    {
                        size = (uint)numChunks,
                        pId = packetId,
                        pIndex = i,
                        data = data
                    };
                    
                    if (i % 10 == 0 || i == numChunks - 1)
                    {
                        MelonLogger.Msg($"  ├─ [{i + 1}/{numChunks}] Chunk {i}");
                    }
                    
                    Thread.Sleep(chunkDelay);
                    Game.SendP2P(targetId, NetworkHelper.ObjectToByteArray(packet));
                }
                
                MelonLogger.Msg($"  ✓ {audioFile.fileName} envoyé complètement à {targetName}");
                Thread.Sleep(2000);
            }
            
            MelonLogger.Msg($"[RÉSEAU] Transfert terminé vers {targetName}");
        }

        public static void SendRadioPacket(string fileName)
        {
            if (Game.IsServer())
            {
                var lobbyId = Game.GetLobbyId();
                int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
                
                MelonLogger.Msg($"[RADIO] Diffusion: {fileName} vers {memberCount} joueurs");

                for (int iMember = 0; iMember < memberCount; ++iMember)
                {
                    CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, iMember);
                    if (member != SteamUser.GetSteamID())
                    {
                        string memberName = SteamFriends.GetFriendPersonaName(member);
                        var packet = new RadioPacket { fileName = fileName };
                        Game.SendP2P(member, NetworkHelper.ObjectToByteArray(packet));
                        MelonLogger.Msg($"  ✓ {fileName} → {memberName}");
                    }
                }
            }
        }

        public static void SendFileRequest()
        {
            AlreadySend = new List<CSteamID>();
            
            if (!Game.IsServer())
            {
                var requestFiles = new AudioFileRequestPacket
                {
                    excludes = ModMain.AudioFilesData.Select(a => a.fileName).ToArray()
                };
                
                MelonLogger.Msg("Demande de fichiers audio au serveur");
                Game.SendP2P(SteamMatchmaking.GetLobbyOwner(Game.GetLobbyId()), NetworkHelper.ObjectToByteArray(requestFiles));
            }
        }        public static async Task<bool> HandleMessageAsync(CSteamID senderId, byte[] data, GameSNS.msgType msgType)
        {
            if (msgType != GameSNS.msgType.radio)
                return true;
            
            try
            {
                var rawData = data.Skip(1).ToArray();
                var obj = NetworkHelper.ByteArrayToObject(rawData);
                var objType = obj.GetType();

                if (objType == typeof(AudioFileRequestPacket))
                {
                    if (AlreadySend.Contains(senderId))
                        return false;
                      AlreadySend.Add(senderId);
                    var request = (AudioFileRequestPacket)obj;
                    _ = Task.Run(() => SendFileAsync(senderId, request));
                }
                else if (objType == typeof(RadioPacket))
                {
                    var radioPacket = (RadioPacket)obj;
                    if (ModMain.AudioFilesData.All(a => a.fileName != radioPacket.fileName))
                        return false;
                    
                    MelonLogger.Msg("Lecture : " + radioPacket.fileName);
                    Game.LoadSong(Path.Combine(Game.GetCustomRadioPath(), radioPacket.fileName));
                    AudioSyncManager.SetCurrentSong(radioPacket.fileName, 0.0);
                }
                else if (objType == typeof(AudioSyncPacket))
                {
                    var syncPacket = (AudioSyncPacket)obj;
                    AudioSyncManager.HandleRemoteSync(senderId, syncPacket);
                    return false;
                }
                else if (objType == typeof(AudioSyncRequestPacket))
                {
                    if (Game.IsServer())
                    {
                        AudioSyncManager.BroadcastSync();
                    }
                    return false;
                }
                else if (objType == typeof(StreamingAudioPacket))
                {
                    var streamPacket = (StreamingAudioPacket)obj;
                    AudioStreamingManager.HandleStreamingChunk(senderId, streamPacket);
                    return false;
                }
                else if (objType == typeof(PacketPart))
                {
                    var part = (PacketPart)obj;
                    if (ReceivedParts.ContainsKey(part.pId))
                    {
                        if (ReceivedParts[part.pId].Any(x => x.pIndex == part.pIndex)) 
                            return false;
                        
                        ReceivedParts[part.pId].Add(part);
                    }
                    else
                    {
                        if (part.pIndex > 10)
                            return false;

                        ReceivedParts.Add(part.pId, new List<PacketPart> {part});
                    }
                    
                    MelonLogger.Msg("Paquet reçu : " + part.pId + " pour l'index : " + part.pIndex + " sur " + (part.size - 1));

                    if (ReceivedParts[part.pId].Count < part.size)
                        return false;

                    MelonLogger.Msg("Tentative de recombinaison des données");

                    var fullPacket = ReceivedParts[part.pId].OrderBy(p => p.pIndex).ToArray();
                    var combinedData = new byte[0];

                    foreach (var p in fullPacket)
                    {
                        combinedData = combinedData.Concat(p.data).ToArray();
                    }

                    ReceivedParts.Remove(part.pId);

                    try
                    {
                        obj = NetworkHelper.ByteArrayToObject(combinedData);
                        MelonLogger.Msg("Recombinaison terminée");
                          if (obj is AudioFilePacket audioPacket)
                        {
                            // Validation des données
                            if (string.IsNullOrEmpty(audioPacket.fileName))
                            {
                                MelonLogger.Error("Nom de fichier invalide dans le paquet audio");
                                return false;
                            }
                            
                            if (audioPacket.data == null || audioPacket.data.Length == 0)
                            {
                                MelonLogger.Error("Données invalides pour le fichier " + audioPacket.fileName);
                                return false;
                            }
                            
                            if (audioPacket.data.Length > audioPacket.fileSize)
                            {
                                MelonLogger.Error("Taille des données incohérente pour " + audioPacket.fileName);
                                return false;
                            }
                            
                            string ext = Path.GetExtension(audioPacket.fileName).ToLower();
                            if (!new[] { ".mp3", ".wav", ".ogg" }.Contains(ext))
                            {
                                MelonLogger.Error("Type de fichier non supporté : " + ext);
                                return false;
                            }

                            // Vérifie la mémoire disponible
                            if (!await NetworkHelper.MemoryManager.CheckMemoryAvailable(audioPacket.data.Length * 2))
                            {
                                MelonLogger.Error("Mémoire insuffisante pour traiter " + audioPacket.fileName);
                                return false;
                            }

                            MelonLogger.Msg("Écriture du fichier " + audioPacket.fileName + ", taille : " + (audioPacket.data.Length / 1048576) + "MiB");
                            _ = ModMain.SaveReceivedFile(audioPacket);
                        }
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Error("Erreur : " + e);
                    }
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("Erreur lors du traitement du message : " + e.Message);
                return true;
            }

            return false;
        }
    }
}
