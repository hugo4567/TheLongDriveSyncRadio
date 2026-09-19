using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MelonLoader;
using Steamworks;
using TheLongDriveSyncRadio.Models;

namespace TheLongDriveSyncRadio
{
    public static class AudioStreamingManager
    {
        private static Dictionary<uint, StreamingAudioState> _activeStreams = new Dictionary<uint, StreamingAudioState>();
        private static Random _rand = new Random();

        private class StreamingAudioState
        {
            public string FileName { get; set; }
            public byte[] FullData { get; set; }
            public Dictionary<int, StreamingAudioPacket> ReceivedChunks { get; set; }
            public long CreatedAt { get; set; }
        }

        public static async Task StreamAudioFileAsync(CSteamID targetId, string fileName, string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    MelonLogger.Warning($"[STREAM] ✗ Fichier non trouvé: {filePath}");
                    return;
                }

                string targetName = SteamFriends.GetFriendPersonaName(targetId);
                var fileInfo = new FileInfo(filePath);
                byte[] audioData = await Task.Run(() => File.ReadAllBytes(filePath));

                if (audioData == null || audioData.Length == 0)
                {
                    MelonLogger.Warning($"[STREAM] ✗ Données vides: {fileName}");
                    return;
                }

                const int CHUNK_SIZE = 256 * 1024; // 256KB par chunk
                int numChunks = (int)Math.Ceiling((double)audioData.Length / CHUNK_SIZE);
                uint streamId = (uint)_rand.Next();

                MelonLogger.Msg($"[STREAM] 📤 Flux: {fileName} ({fileInfo.Length / 1048576}MB) → {targetName} | {numChunks} chunks");

                for (int i = 0; i < numChunks; i++)
                {
                    int offset = i * CHUNK_SIZE;
                    int chunkDataLength = Math.Min(CHUNK_SIZE, audioData.Length - offset);
                    byte[] chunkData = new byte[chunkDataLength];
                    Array.Copy(audioData, offset, chunkData, 0, chunkDataLength);

                    var packet = new StreamingAudioPacket
                    {
                        fileName = fileName,
                        audioData = chunkData,
                        sequenceNumber = i,
                        isLastChunk = (i == numChunks - 1),
                        totalSize = audioData.Length
                    };

                    try
                    {
                        Game.SendP2P(targetId, NetworkHelper.ObjectToByteArray(packet));
                        
                        if (i % 20 == 0 || i == numChunks - 1)
                        {
                            MelonLogger.Msg($"  ├─ [{i + 1}/{numChunks}] Chunk {i} ({(i * CHUNK_SIZE) / 1048576}MB / {fileInfo.Length / 1048576}MB)");
                        }
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning($"  ✗ Chunk {i}: {ex.Message}");
                    }

                    // Délai pour éviter la congestion réseau
                    await Task.Delay(100);
                }

                MelonLogger.Msg($"[STREAM] ✓ {fileName} streaming complète vers {targetName}");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[STREAM] ✗ Erreur: {ex.Message}");
            }
        }

        public static void HandleStreamingChunk(CSteamID senderId, StreamingAudioPacket packet)
        {
            try
            {
                uint streamId = (uint)senderId.m_SteamID;
                string senderName = SteamFriends.GetFriendPersonaName(senderId);
                
                if (!_activeStreams.ContainsKey(streamId))
                {
                    _activeStreams[streamId] = new StreamingAudioState
                    {
                        FileName = packet.fileName,
                        ReceivedChunks = new Dictionary<int, StreamingAudioPacket>(),
                        CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };
                    
                    MelonLogger.Msg($"[STREAM] 📥 Réception: {packet.fileName} de {senderName} | Total: {packet.totalSize / 1048576}MB");
                }

                var state = _activeStreams[streamId];
                
                // Vérifier les doublons
                if (state.ReceivedChunks.ContainsKey(packet.sequenceNumber))
                {
                    return;
                }

                state.ReceivedChunks[packet.sequenceNumber] = packet;
                
                if (packet.sequenceNumber % 20 == 0 || packet.isLastChunk)
                {
                    MelonLogger.Msg($"  ├─ [{packet.sequenceNumber}] Chunk reçu de {senderName} ({(packet.sequenceNumber * 256) / 1024}KB reçus)");
                }

                // Si c'est le dernier chunk, assembler les données
                if (packet.isLastChunk)
                {
                    _ = Task.Run(() => AssembleAndSaveStreamAsync(streamId, state, packet));
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[STREAM] ✗ Erreur chunk: {ex.Message}");
            }
        }

        private static async Task AssembleAndSaveStreamAsync(uint streamId, StreamingAudioState state, StreamingAudioPacket lastPacket)
        {
            try
            {
                // Vérifier que tous les chunks sont reçus
                int expectedChunks = (int)Math.Ceiling((double)lastPacket.totalSize / (256 * 1024));
                if (state.ReceivedChunks.Count < expectedChunks)
                {
                    MelonLogger.Warning($"[STREAM] ⚠ Chunks manquants: {state.FileName} ({state.ReceivedChunks.Count}/{expectedChunks})");
                    return;
                }
                
                MelonLogger.Msg($"[STREAM] ✓ Assemblage: {state.FileName} ({lastPacket.totalSize / 1048576}MB)");
                // Assembler les données dans l'ordre
                var sortedChunks = state.ReceivedChunks.OrderBy(x => x.Key).ToList();
                byte[] fullData = new byte[lastPacket.totalSize];
                int offset = 0;

                foreach (var chunk in sortedChunks)
                {
                    Array.Copy(chunk.Value.audioData, 0, fullData, offset, chunk.Value.audioData.Length);
                    offset += chunk.Value.audioData.Length;
                }

                // Sauvegarder le fichier
                string savePath = Path.Combine(Game.GetCustomRadioPath(), state.FileName);
                await Task.Run(() => File.WriteAllBytes(savePath, fullData));

                MelonLogger.Msg($"[AudioStream] Fichier assemblé et sauvegardé: {state.FileName}");

                // Ajouter à la liste des fichiers audio
                var audioPacket = new AudioFilePacket
                {
                    fileName = state.FileName,
                    data = fullData,
                    fileSize = fullData.Length,
                    requiresStreaming = true,
                    filePath = savePath
                };

                if (!ModMain.AudioFilesData.Any(a => a.fileName == audioPacket.fileName))
                {
                    ModMain.AudioFilesData.Add(audioPacket);
                }

                // Nettoyer
                _activeStreams.Remove(streamId);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[AudioStream] Erreur lors de l'assemblage du stream: {ex.Message}");
            }
        }

        public static void CleanupOldStreams()
        {
            try
            {
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long timeout = 5 * 60 * 1000; // 5 minutes timeout

                var oldStreams = _activeStreams
                    .Where(x => (currentTime - x.Value.CreatedAt) > timeout)
                    .ToList();

                foreach (var stream in oldStreams)
                {
                    MelonLogger.Msg($"[AudioStream] Nettoyage du stream expiré: {stream.Value.FileName}");
                    _activeStreams.Remove(stream.Key);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[AudioStream] Erreur lors du nettoyage: {ex.Message}");
            }
        }
    }
}
