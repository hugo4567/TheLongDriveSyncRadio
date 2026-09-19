using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using MelonLoader;
using Steamworks;

namespace TheLongDriveSyncRadio
{
    public static class NetworkHelper
    {
        private static readonly SemaphoreSlim NetworkSemaphore = new SemaphoreSlim(3);

        private static byte[] CompressData(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                gzipStream.Write(data, 0, data.Length);
                gzipStream.Close();
                return compressedStream.ToArray();
            }
        }

        private static byte[] DecompressData(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                gzipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }
        
        public static byte[] ObjectToByteArray(Object obj)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    var bf = new BinaryFormatter();
                    bf.Serialize(ms, obj);
                    var rawData = ms.ToArray();
                    
                    // Compression si la taille le justifie (>1KB)
                    if (rawData.Length > 1024)
                    {
                        var compressed = CompressData(rawData);
                        if (compressed.Length < rawData.Length)
                            return compressed;
                    }
                    return rawData;
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("Erreur lors de la sérialisation : " + e.Message);
                throw;
            }
        }
        
        public static Object ByteArrayToObject(byte[] arrBytes)
        {
            try
            {
                // Tente de décompresser si les données sont compressées
                byte[] decompressedData;
                try
                {
                    decompressedData = DecompressData(arrBytes);
                }
                catch (InvalidDataException)
                {
                    // Si la décompression échoue, utilise les données brutes
                    decompressedData = arrBytes;
                }

                using (var memStream = new MemoryStream(decompressedData))
                {
                    var binForm = new BinaryFormatter();
                    memStream.Position = 0;
                    return binForm.Deserialize(memStream);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("Erreur lors de la désérialisation : " + e.Message);
                throw;
            }
        }

        public static async Task<byte[]> ObjectToByteArrayAsync(Object obj, CancellationToken token)
        {
            await NetworkSemaphore.WaitAsync(token);
            try
            {
                return ObjectToByteArray(obj);
            }
            finally
            {
                NetworkSemaphore.Release();
            }
        }

        public static async Task<Object> ByteArrayToObjectAsync(byte[] arrBytes, CancellationToken token)
        {
            await NetworkSemaphore.WaitAsync(token);
            try
            {
                return ByteArrayToObject(arrBytes);
            }
            finally
            {
                NetworkSemaphore.Release();
            }
        }

        public static async Task<bool> SendWithRetryAsync(byte[] data, string targetId, CancellationToken token)
        {
            const int MAX_RETRIES = 3;
            int retryCount = 0;
            int currentDelay = 1000; // 1 seconde

            while (retryCount < MAX_RETRIES && !token.IsCancellationRequested)
            {
                try
                {
                    if (Game.IsInitialized)
                    {
                        Game.SendP2P(new CSteamID(ulong.Parse(targetId)), data);
                        return true;
                    }
                }
                catch (Exception e)
                {
                    MelonLogger.Error(String.Format("Erreur d'envoi (tentative {0}/{1}): {2}", 
                        retryCount + 1, MAX_RETRIES, e.Message));
                    
                    if (retryCount >= MAX_RETRIES - 1)
                        break;
                }

                retryCount++;
                await Task.Delay(currentDelay, token);
                currentDelay *= 2; // Délai exponentiel
            }

            return false;
        }

        public static class MemoryManager
        {
            private static readonly long MaxMemoryUsage = 1024L * 1024L * 1024L; // 1 GB
            private static readonly long WarningThreshold = MaxMemoryUsage * 80 / 100; // 80%
            private static readonly long CriticalThreshold = MaxMemoryUsage * 90 / 100; // 90%
            private static readonly TimeSpan MinTimeBetweenCollections = TimeSpan.FromSeconds(30);
            private static DateTime lastCollection = DateTime.MinValue;
            private static long peakMemoryUsage = 0;

            public static async Task<bool> CheckMemoryAvailable(long requiredSize, bool aggressive = false)
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var currentUsage = process.PrivateMemorySize64;
                var available = MaxMemoryUsage - currentUsage;

                if (currentUsage > peakMemoryUsage)
                {
                    peakMemoryUsage = currentUsage;
                    MelonLogger.Msg("Nouveau pic d'utilisation mémoire: " + (peakMemoryUsage / 1048576) + "MB");
                }

                if (available < requiredSize || aggressive)
                {
                    if (DateTime.Now - lastCollection > MinTimeBetweenCollections)
                    {
                        await OptimizeMemoryAsync();
                        available = MaxMemoryUsage - process.PrivateMemorySize64;
                    }
                }

                return available >= requiredSize;
            }

            private static async Task OptimizeMemoryAsync()
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var currentUsage = process.PrivateMemorySize64;

                if (currentUsage > WarningThreshold)
                {
                    MelonLogger.Warning("Utilisation mémoire élevée: " + (currentUsage / 1048576) + "MB");
                    
                    if (currentUsage > CriticalThreshold)
                    {
                        MelonLogger.Error("Niveau critique de mémoire atteint, nettoyage forcé...");
                        GC.Collect(2, GCCollectionMode.Forced, true);
                    }
                    else
                    {
                        GC.Collect(1, GCCollectionMode.Optimized);
                    }

                    await Task.Delay(100); // Attendre la finalisation du GC
                    GC.WaitForPendingFinalizers();
                    
                    var newUsage = process.PrivateMemorySize64;
                    var saved = (currentUsage - newUsage) / 1048576.0;
                    if (saved > 0)
                    {
                        MelonLogger.Msg("Mémoire libérée: " + saved.ToString("F2") + "MB");
                    }
                }

                lastCollection = DateTime.Now;
            }

            public static void OptimizeMemory()
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var currentUsage = process.PrivateMemorySize64;
                
                // Ne lance pas l'optimisation si la dernière est trop récente
                if (DateTime.Now - lastCollection <= MinTimeBetweenCollections)
                    return;

                if (currentUsage > WarningThreshold)
                {
                    _ = OptimizeMemoryAsync();
                }
            }
        }
    }
}