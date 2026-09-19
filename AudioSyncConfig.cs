using System;
using System.IO;
using System.Xml.Linq;
using MelonLoader;

namespace TheLongDriveSyncRadio
{
    public static class AudioSyncConfig
    {
        private static XDocument _configDoc;
        private static string _configPath;

        public static void Initialize()
        {
            try
            {
                _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AudioSyncConfig.xml");
                
                if (!File.Exists(_configPath))
                {
                    MelonLogger.Msg("[AudioConfig] Config par défaut créée");
                    CreateDefaultConfig();
                }
                else
                {
                    _configDoc = XDocument.Load(_configPath);
                }
            }
            catch
            {
                // En cas d'erreur, créer une config vide fonctionnelle
                CreateDefaultConfig();
            }
        }

        private static void CreateDefaultConfig()
        {
            try
            {
                var config = new XDocument(
                    new XElement("configuration",
                        new XElement("audioSync",
                            new XElement("syncInterval", "2000"),
                            new XElement("acceptableDrift", "500"),
                            new XElement("enabled", "true")
                        ),
                        new XElement("audioStreaming",
                            new XElement("chunkSize", "262144"),
                            new XElement("chunkDelay", "100"),
                            new XElement("streamTimeout", "300"),
                            new XElement("enabled", "true")
                        )
                    )
                );

                _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AudioSyncConfig.xml");
                config.Save(_configPath);
                _configDoc = config;
            }
            catch
            {
                // Silencieusement, créer un document vide en mémoire
                _configDoc = new XDocument(new XElement("configuration"));
            }
        }

        public static int GetSyncInterval()
        {
            return GetIntValue("audioSync/syncInterval", 2000);
        }

        public static int GetAcceptableDrift()
        {
            return GetIntValue("audioSync/acceptableDrift", 500);
        }

        public static bool IsAudioSyncEnabled()
        {
            return GetBoolValue("audioSync/enabled", true);
        }

        public static int GetChunkSize()
        {
            return GetIntValue("audioStreaming/chunkSize", 262144);
        }

        public static int GetChunkDelay()
        {
            return GetIntValue("audioStreaming/chunkDelay", 100);
        }

        public static int GetStreamTimeout()
        {
            return GetIntValue("audioStreaming/streamTimeout", 300);
        }

        public static bool IsAudioStreamingEnabled()
        {
            return GetBoolValue("audioStreaming/enabled", true);
        }

        public static int GetMaxRetries()
        {
            return GetIntValue("network/maxRetries", 3);
        }

        public static int GetSendTimeout()
        {
            return GetIntValue("network/sendTimeout", 30);
        }

        public static int GetMaxBufferSize()
        {
            return GetIntValue("network/maxBufferSize", 524288);
        }

        public static string GetCacheDir()
        {
            return GetStringValue("cache/cacheDir", "CustomRadio");
        }

        public static int GetMaxCacheSize()
        {
            return GetIntValue("cache/maxCacheSize", 2048);
        }

        public static bool IsAutoCacheCleanupEnabled()
        {
            return GetBoolValue("cache/autoCleanup", true);
        }

        public static int GetLoggingVerbosity()
        {
            return GetIntValue("logging/verbosity", 2);
        }

        public static bool ShouldLogAudioSync()
        {
            return GetBoolValue("logging/logAudioSync", true);
        }

        public static bool ShouldLogStreaming()
        {
            return GetBoolValue("logging/logStreaming", true);
        }

        public static string GetMinGameVersion()
        {
            return GetStringValue("compatibility/minGameVersion", "2024.1.0");
        }

        public static string GetMinMelonLoaderVersion()
        {
            return GetStringValue("compatibility/minMelonLoaderVersion", "0.5.4");
        }

        public static bool WaitForSync()
        {
            return GetBoolValue("multiplayer/waitForSync", true);
        }

        public static int GetLoadDelay()
        {
            return GetIntValue("multiplayer/loadDelay", 1000);
        }

        public static bool ShowNotifications()
        {
            return GetBoolValue("multiplayer/showNotifications", true);
        }

        public static string[] GetSupportedGameBuildIds()
        {
            try
            {
                if (_configDoc == null)
                    return new[] { "10117180", "16536989" };

                var root = _configDoc.Root;
                var buildIdsElement = SelectElement(root, "compatibility/supportedGameBuildIds");
                
                if (buildIdsElement != null)
                {
                    var buildIds = buildIdsElement.Elements("buildId");
                    var result = new System.Collections.Generic.List<string>();
                    
                    foreach (var id in buildIds)
                    {
                        if (!string.IsNullOrEmpty(id.Value))
                            result.Add(id.Value);
                    }
                    
                    return result.Count > 0 ? result.ToArray() : new[] { "10117180", "16536989" };
                }
            }
            catch
            {
                // Utiliser les valeurs par défaut silencieusement
            }
            return new[] { "10117180", "16536989" };
        }

        public static bool IsGameVersionSupported(string buildId)
        {
            try
            {
                var supportedIds = GetSupportedGameBuildIds();
                return System.Linq.Enumerable.Contains(supportedIds, buildId);
            }
            catch
            {
                return false;
            }
        }

        // Méthodes utilitaires privées

        private static int GetIntValue(string xpath, int defaultValue)
        {
            try
            {
                if (_configDoc == null)
                    return defaultValue;

                var element = SelectElement(_configDoc.Root, xpath);
                if (element != null && int.TryParse(element.Value, out int result))
                    return result;
            }
            catch
            {
                // Utiliser la valeur par défaut silencieusement
            }
            return defaultValue;
        }

        private static bool GetBoolValue(string xpath, bool defaultValue)
        {
            try
            {
                if (_configDoc == null)
                    return defaultValue;

                var element = SelectElement(_configDoc.Root, xpath);
                if (element != null && bool.TryParse(element.Value, out bool result))
                    return result;
            }
            catch
            {
                // Utiliser la valeur par défaut silencieusement
            }
            return defaultValue;
        }

        private static string GetStringValue(string xpath, string defaultValue)
        {
            try
            {
                if (_configDoc == null)
                    return defaultValue;

                var element = SelectElement(_configDoc.Root, xpath);
                if (element != null && !string.IsNullOrEmpty(element.Value))
                    return element.Value;
            }
            catch
            {
                // Utiliser la valeur par défaut silencieusement
            }
            return defaultValue;
        }

        private static XElement SelectElement(XElement root, string xpath)
        {
            if (root == null)
                return null;

            // Simple path parsing: "audioSync/syncInterval"
            var parts = xpath.Split('/');
            var current = root;
            
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;
                    
                current = current.Element(part);
                if (current == null)
                    return null;
            }
            
            return current;
        }
    }
}
