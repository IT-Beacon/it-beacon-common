using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Diagnostics;

namespace it_beacon_common.Config
{
    /// <summary>
    /// Represents a single QuickShortcut button configuration.
    /// </summary>
    public class QuickShortcut
    {
        public string Glyph { get; set; } = "&#xE713;"; // Default glyph (settings)
        public string ToolTip { get; set; } = "Default Tooltip";
        public string Url { get; set; } = "https://itservices.cvad.unt.edu";
    }

    /// <summary>
    /// Static class to load and access settings from Config/settings.xml.
    /// </summary>
    public static class ConfigManager
    {
        private static XmlDocument? _config;
        private static bool _isLoaded = false;
        private static readonly string _configFilePath = Path.Combine(AppContext.BaseDirectory, "Config", "settings.xml");

        /// <summary>
        /// Loads the settings.xml file. Must be called once on startup.
        /// </summary>
        public static void LoadConfig()
        {
            if (_isLoaded) return;

            try
            {
                if (File.Exists(_configFilePath))
                {
                    _config = new XmlDocument();
                    _config.Load(_configFilePath);
                    _isLoaded = true;
                    Debug.WriteLine($"[ConfigManager] Successfully loaded config from: {_configFilePath}");
                }
                else
                {
                    Debug.WriteLine($"[ConfigManager] ERROR: Config file not found at: {_configFilePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ConfigManager] ERROR: Failed to load config file: {ex.Message}");
                _config = null; // Ensure config is null on failure
            }
        }

        /// <summary>
        /// Gets a string value from the config file.
        /// </summary>
        /// <param name="xPath">The XPath to the setting (e.g., "/Settings/Application/Name").</param>
        /// <param name="defaultValue">The value to return if the key is not found.</param>
        /// <returns>The setting value or the default value.</returns>
        public static string GetString(string xPath, string defaultValue = "")
        {
            if (!_isLoaded || _config == null) return defaultValue;

            try
            {
                var node = _config.SelectSingleNode(xPath);
                return node?.InnerText ?? defaultValue;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ConfigManager] ERROR reading string from XPath '{xPath}': {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Gets a boolean value from the config file.
        /// </summary>
        /// <param name="xPath">The XPath to the setting (e.g., "/Settings/PopupWindow/ShowNetworkInfo").</param>
        /// <param name="defaultValue">The value to return if the key is not found.</param>
        /// <returns>The setting value or the default value.</returns>
        public static bool GetBool(string xPath, bool defaultValue = false)
        {
            string val = GetString(xPath, defaultValue.ToString());
            if (bool.TryParse(val, out bool result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Gets the list of configured quick shortcuts.
        /// </summary>
        /// <returns>A list of QuickShortcut objects.</returns>
        public static List<QuickShortcut> GetQuickShortcuts()
        {
            var shortcuts = new List<QuickShortcut>();
            if (!_isLoaded || _config == null) return shortcuts;

            try
            {
                var nodes = _config.SelectNodes("/Settings/QuickShortcuts/Shortcut");
                if (nodes == null) return shortcuts;

                foreach (XmlNode node in nodes)
                {
                    shortcuts.Add(new QuickShortcut
                    {
                        Glyph = node["Glyph"]?.InnerText ?? "&#xE713;",
                        ToolTip = node["ToolTip"]?.InnerText ?? "Link",
                        Url = node["Url"]?.InnerText ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ConfigManager] ERROR reading QuickShortcuts: {ex.Message}");
            }

            return shortcuts;
        }
    }
}

