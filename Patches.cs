using System;
using System.IO;
using HarmonyLib;
using MelonLoader;
using Steamworks;

namespace TheLongDriveSyncRadio
{
    // NOTE: Les patchs GameSNS sont commentés car ces méthodes n'existent que comme types adaptateurs
    // Seuls les patchs custommusicscript sont actifs pour capturer les changements de chanson
    
    /*
    [HarmonyPatch(typeof(GameSNS), "RGeneralSyncMessage")]
    public class Patch
    {
        private static bool Prefix(object __instance, CSteamID _id, byte[] _bytes, GameSNS.msgType _type)
        {
            return NetworkManager.HandleMessageAsync(_id, _bytes, _type).GetAwaiter().GetResult();
        }
    }

    [HarmonyPatch(typeof(GameSNS), "SAskStartStuff")]
    public class PatchStart
    {
        private static void Prefix(object __instance)
        {
            NetworkManager.SendFileRequest();
            AudioSyncManager.SendSyncRequest();
        }
    }
    */
    
    [HarmonyPatch(typeof(custommusicscript), "LoadOneSong", new[] { typeof(string), typeof(int) })]
    public class PatchRadioCustomSend
    {
        private static void Prefix(object __instance, string path, int index)
        {
            // Vérifier la compatibilité avant d'envoyer
            if (!GameVersionManager.IsVersionSupported())
            {
                MelonLogger.Warning("⚠ Mod non compatible avec cette version du jeu");
                return;
            }

            string fileName = Path.GetFileName(path);
            NetworkManager.SendRadioPacket(fileName);
            AudioSyncManager.SetCurrentSong(fileName, index);
        }
    }

    // Patch de compatibilité multijoueur pour les versions 10117180 et 16536989
    [HarmonyPatch(typeof(settingsscript), "SendP2P")]
    public class PatchP2PCompatibility
    {
        private static void Prefix(string targetId, byte[] data)
        {
            try
            {
                // Ajouter la validation de compatibilité pour P2P
                if (!GameVersionManager.ValidateMultiplayerCompatibility())
                {
                    MelonLogger.Warning("⚠ Envoi P2P: compatibilité multijoueur non validée");
                }

                // Log version-specific P2P metrics
                var currentBuild = GameVersionManager.GetCurrentGameBuildId();
                if (data != null && data.Length > 0)
                {
                    MelonLogger.Msg($"[P2P v{currentBuild}] Envoi {data.Length} bytes vers {targetId}");
                }
            }
            catch
            {
                // Continuer sans interférence
            }
        }
    }

    // NOTE: Le patch Update est commenté car custommusicscript.Update() n'existe pas/n'est pas accessible
    // La synchronisation se fait via LoadOneSong lors du chargement de chanson
    /*
    [HarmonyPatch(typeof(custommusicscript), "Update")]
    public class PatchRadioUpdate
    {
        private static float _lastSyncTime = 0f;
        private const float SYNC_INTERVAL = 2f;

        private static void Postfix(object __instance)
        {
            try
            {
                if (__instance == null) return;
                
                _lastSyncTime += UnityEngine.Time.deltaTime;
                if (_lastSyncTime >= SYNC_INTERVAL)
                {
                    _lastSyncTime = 0f;
                    AudioSyncManager.BroadcastSync();
                }
            }
            catch
            {
                // Silencieusement, continuer sans sync
            }
        }
    }
    */
}


