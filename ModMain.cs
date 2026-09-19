using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MelonLoader;
using Steamworks;
using TheLongDriveSyncRadio.Models;

namespace TheLongDriveSyncRadio
{
    public class ModMain : MelonMod
    {
        public static readonly List<AudioFilePacket> AudioFilesData = new List<AudioFilePacket>();
        private static bool _initialized = false;
        
        [Obsolete]
        public override void OnApplicationStart()
        {
            MelonLogger.Msg("=== TheLongDriveSyncRadio v2.0+ === Démarrage du mod...");
            
            try
            {
                // Initialiser et valider la compatibilité de version
                try
                {
                    GameVersionManager.Initialize();
                    GameVersionManager.ApplyVersionSpecificPatches();
                    
                    if (GameVersionManager.ValidateMultiplayerCompatibility())
                    {
                        MelonLogger.Msg("✓ Compatibilité multijoueur validée");
                    }
                    else
                    {
                        MelonLogger.Warning("⚠ Compatibilité multijoueur non validée - quelques fonctionnalités peuvent être limitées");
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"⚠ Gestion version: {ex.Message}");
                }

                // Initialiser la configuration
                try
                {
                    AudioSyncConfig.Initialize();
                    MelonLogger.Msg("✓ Configuration Audio Sync initialisée");
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"⚠ Configuration: {ex.Message}");
                }

                // Initialiser les gestionnaires
                try
                {
                    if (AudioSyncConfig.IsAudioSyncEnabled())
                    {
                        MelonLogger.Msg("✓ Synchronisation Audio: ACTIVÉE");
                    }
                    else
                    {
                        MelonLogger.Msg("○ Synchronisation Audio: désactivée");
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"⚠ Audio Sync: {ex.Message}");
                }

                try
                {
                    if (AudioSyncConfig.IsAudioStreamingEnabled())
                    {
                        MelonLogger.Msg("✓ Streaming Audio: ACTIVÉ");
                    }
                    else
                    {
                        MelonLogger.Msg("○ Streaming Audio: désactivé");
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"⚠ Audio Streaming: {ex.Message}");
                }

                // Appliquer les patchs Harmony
                try
                {
                    var harmony = new HarmonyLib.Harmony("com.TheLongDriveSyncRadio");
                    harmony.PatchAll();
                    MelonLogger.Msg("✓ Patchs Harmony appliqués");
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"⚠ Harmony patches: {ex.Message}");
                }
                
                _initialized = true;
                MelonLogger.Msg("=== ✓ TheLongDriveSyncRadio OPÉRATIONNEL ===");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"⚠ Erreur globale: {ex.Message}\n{ex.StackTrace}");
                MelonLogger.Msg("=== ⚠ TheLongDriveSyncRadio lancé en mode dégradé ===");
            }
        }

        public override void OnUpdate()
        {
            if (!_initialized) return;
            
            try
            {
                // Nettoyage périodique des vieux streams
                AudioStreamingManager.CleanupOldStreams();
            }
            catch
            {
                // Ignorer silencieusement les erreurs de nettoyage
            }
        }

        public static async Task SaveReceivedFile(AudioFilePacket packet)
        {
            try
            {
                if (string.IsNullOrEmpty(packet.fileName))
                {
                    MelonLogger.Warning("⚠ Tentative de sauvegarde avec nom de fichier vide");
                    return;
                }

                var path = Path.Combine(GameAdapter.Settings.CustomRadioPath, packet.fileName);
                await Task.Run(() => File.WriteAllBytes(path, packet.data));
                
                // Éviter les doublons
                if (!AudioFilesData.Any(a => a.fileName == packet.fileName))
                {
                    AudioFilesData.Add(packet);
                }
                
                long sizeMB = packet.fileSize / 1048576;
                if (sizeMB == 0) sizeMB = 1;
                
                MelonLogger.Msg($"✓ Fichier reçu: {packet.fileName} ({sizeMB}MB)");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"⚠ Sauvegarde {packet.fileName}: {ex.Message}");
            }
        }
    }
}
