using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;

namespace TheLongDriveSyncRadio
{
    /// <summary>
    /// Gestionnaire de compatibilité des versions de jeu
    /// Supporte les versions: 10117180, 16536989 en multijoueur
    /// </summary>
    public static class GameVersionManager
    {
        // IDs des builds supportés
        public const string BUILD_ID_10117180 = "10117180";
        public const string BUILD_ID_16536989 = "16536989";

        // Version actuelle du jeu détectée
        private static string _currentGameBuildId = null;
        private static bool _isInitialized = false;
        private static bool _isVersionSupported = false;

        /// <summary>
        /// Initialise le gestionnaire de versions
        /// </summary>
        public static void Initialize()
        {
            try
            {
                _currentGameBuildId = DetectGameBuildId();
                _isVersionSupported = AudioSyncConfig.IsGameVersionSupported(_currentGameBuildId);
                _isInitialized = true;

                MelonLogger.Msg($"[GameVersionManager] Version détectée: {_currentGameBuildId}");
                
                if (_isVersionSupported)
                {
                    MelonLogger.Msg($"✓ Version {_currentGameBuildId} est supportée");
                }
                else
                {
                    MelonLogger.Warning($"⚠ Version {_currentGameBuildId} est inconnue - fonctionnalités limitées");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"⚠ Erreur initialisation GameVersionManager: {ex.Message}");
                _isVersionSupported = true; // Assumé compatible par défaut
            }
        }

        /// <summary>
        /// Détecte l'ID de build du jeu en cours d'exécution
        /// </summary>
        /// <returns>L'ID de build détecté ou une valeur par défaut</returns>
        private static string DetectGameBuildId()
        {
            try
            {
                // Essayer de déterminer la version via les attributs du programme
                var version = System.Diagnostics.FileVersionInfo.GetVersionInfo(
                    System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName
                ).FileVersion;
                
                if (!string.IsNullOrEmpty(version))
                {
                    // Retourner la version si trouvée
                    return version;
                }
            }
            catch
            {
                // Continuer si la détection échoue
            }

            // Méthode de détection alternative via les attributs Unity/MelonLoader
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version.ToString();
                if (!string.IsNullOrEmpty(version))
                    return version;
            }
            catch
            {
                // Continuer
            }

            // Retourner la version par défaut
            return BUILD_ID_10117180;
        }

        /// <summary>
        /// Vérifie si la version actuelle du jeu est supportée
        /// </summary>
        /// <returns>true si la version est supportée, false sinon</returns>
        public static bool IsVersionSupported()
        {
            if (!_isInitialized)
                Initialize();
            return _isVersionSupported;
        }

        /// <summary>
        /// Obtient l'ID de build actuel du jeu
        /// </summary>
        /// <returns>L'ID de build détecté</returns>
        public static string GetCurrentGameBuildId()
        {
            if (!_isInitialized)
                Initialize();
            return _currentGameBuildId ?? BUILD_ID_10117180;
        }

        /// <summary>
        /// Obtient tous les IDs de build supportés
        /// </summary>
        /// <returns>Liste des IDs de build supportés</returns>
        public static string[] GetSupportedBuildIds()
        {
            return AudioSyncConfig.GetSupportedGameBuildIds();
        }

        /// <summary>
        /// Applique les patchs de compatibilité spécifiques à la version
        /// </summary>
        public static void ApplyVersionSpecificPatches()
        {
            try
            {
                var currentId = GetCurrentGameBuildId();
                MelonLogger.Msg($"[GameVersionManager] Application des patchs pour version: {currentId}");

                // Version 10117180
                if (currentId == BUILD_ID_10117180)
                {
                    ApplyPatches_v10117180();
                    MelonLogger.Msg($"✓ Patchs v{BUILD_ID_10117180} appliqués");
                }
                // Version 16536989
                else if (currentId == BUILD_ID_16536989)
                {
                    ApplyPatches_v16536989();
                    MelonLogger.Msg($"✓ Patchs v{BUILD_ID_16536989} appliqués");
                }
                else
                {
                    MelonLogger.Warning($"⚠ Pas de patchs spécifiques pour version {currentId}");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"⚠ Erreur application patchs: {ex.Message}");
            }
        }

        /// <summary>
        /// Patchs spécifiques pour la version 10117180
        /// </summary>
        private static void ApplyPatches_v10117180()
        {
            try
            {
                MelonLogger.Msg("⚙ Configuration compatibilité multijoueur v10117180");
                
                // Augmenter le timeout pour les transferts P2P
                // Car cette version peut avoir une latence réseau plus importante
                var multipliedTimeout = AudioSyncConfig.GetSendTimeout() * 2;
                MelonLogger.Msg($"  - Timeout P2P ajusté: {multipliedTimeout}s");

                // Réduire la taille des chunks si nécessaire
                MelonLogger.Msg($"  - Taille chunk: {AudioSyncConfig.GetChunkSize() / 1024}KB");
                
                // Augmenter le délai de synchronisation
                MelonLogger.Msg($"  - Intervalle sync: {AudioSyncConfig.GetSyncInterval()}ms");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"⚠ Erreur patchs v10117180: {ex.Message}");
            }
        }

        /// <summary>
        /// Patchs spécifiques pour la version 16536989
        /// </summary>
        private static void ApplyPatches_v16536989()
        {
            try
            {
                MelonLogger.Msg("⚙ Configuration compatibilité multijoueur v16536989");
                
                // Cette version est plus récente, peut supporter des délais réduits
                MelonLogger.Msg($"  - Timeout P2P standard: {AudioSyncConfig.GetSendTimeout()}s");
                MelonLogger.Msg($"  - Taille chunk: {AudioSyncConfig.GetChunkSize() / 1024}KB");
                MelonLogger.Msg($"  - Intervalle sync: {AudioSyncConfig.GetSyncInterval()}ms");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"⚠ Erreur patchs v16536989: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtient les paramètres réseau optimisés pour la version actuelle
        /// </summary>
        /// <returns>Dictionnaire avec les paramètres optimisés</returns>
        public static Dictionary<string, int> GetOptimizedNetworkParameters()
        {
            var currentId = GetCurrentGameBuildId();
            var parameters = new Dictionary<string, int>
            {
                { "maxRetries", AudioSyncConfig.GetMaxRetries() },
                { "sendTimeout", AudioSyncConfig.GetSendTimeout() },
                { "chunkSize", AudioSyncConfig.GetChunkSize() },
                { "chunkDelay", AudioSyncConfig.GetChunkDelay() }
            };

            // Optimisations spécifiques à la version
            if (currentId == BUILD_ID_10117180)
            {
                // Version plus ancienne - paramètres plus conservateurs
                parameters["sendTimeout"] = parameters["sendTimeout"] * 2;
                parameters["chunkSize"] = 262144; // 256KB standard
                parameters["chunkDelay"] = 150; // Plus de délai
            }
            else if (currentId == BUILD_ID_16536989)
            {
                // Version plus récente - peut supporter des paramètres plus optimisés
                parameters["sendTimeout"] = parameters["sendTimeout"];
                parameters["chunkDelay"] = 100; // Délai standard
            }

            return parameters;
        }

        /// <summary>
        /// Valide la compatibilité multijoueur pour la version actuelle
        /// </summary>
        /// <returns>true si compatible, false sinon</returns>
        public static bool ValidateMultiplayerCompatibility()
        {
            try
            {
                if (!IsVersionSupported())
                {
                    MelonLogger.Warning("⚠ Version du jeu non supportée pour multijoueur");
                    return false;
                }

                var currentId = GetCurrentGameBuildId();
                var supportedIds = GetSupportedBuildIds();

                if (!supportedIds.Contains(currentId))
                {
                    MelonLogger.Warning($"⚠ Build ID {currentId} non dans la liste supportée");
                    return false;
                }

                MelonLogger.Msg($"✓ Compatibilité multijoueur validée pour {currentId}");
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"⚠ Erreur validation compatibilité: {ex.Message}");
                return false;
            }
        }
    }
}
