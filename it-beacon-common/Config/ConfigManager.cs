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

    // --- NEW PUBLIC CLASS ---
    /// <summary>
    /// A simple class to represent a key-value pair for the settings window.
    /// </summary>
    public class SettingItem
    {
        public string Category { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
    // --- END OF NEW ---


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

        // --- NEW PUBLIC METHOD ---
        /// <summary>
        /// Reads the entire loaded XML config and flattens it into a list for display.
        /// </summary>
        /// <returns>A list of SettingItem objects.</returns>
        public static List<SettingItem> GetAllSettings()
        {
            var settings = new List<SettingItem>();
            if (!_isLoaded || _config == null)
            {
                settings.Add(new SettingItem { Category = "Error", Key = "ConfigManager", Value = "Config file not found or not loaded." });
                settings.Add(new SettingItem { Category = "Error", Key = "Expected Path", Value = _configFilePath });
                return settings;
            }

            try
            {
                var root = _config.SelectSingleNode("/Settings");
                if (root == null)
                {
                    settings.Add(new SettingItem { Category = "Error", Key = "Parsing", Value = "Could not find /Settings root node." });
                    return settings;
                }

                foreach (XmlNode categoryNode in root.ChildNodes)
                {
                    if (categoryNode.NodeType != XmlNodeType.Element) continue;

                    if (categoryNode.Name == "QuickShortcuts")
                    {
                        int i = 1;
                        foreach (XmlNode shortcutNode in categoryNode.SelectNodes("Shortcut"))
                        {
                            string categoryName = $"QuickShortcut {i++}";
                            if (shortcutNode.ChildNodes != null)
                            {
                                foreach (XmlNode shortcutSetting in shortcutNode.ChildNodes)
                                {
                                    if (shortcutSetting.NodeType == XmlNodeType.Element)
                                    {
                                        settings.Add(new SettingItem
                                        {
                                            Category = categoryName,
                                            Key = shortcutSetting.Name,
                                            Value = shortcutSetting.InnerText
                                        });
                                    }
                                }
                            }
                        }
                    }
                    else if (categoryNode.ChildNodes.Count > 0 && categoryNode.FirstChild.NodeType == XmlNodeType.Element)
                    {
                        // Node has child elements (e.g., SnipeIT, PopupWindow)
                        foreach (XmlNode settingNode in categoryNode.ChildNodes)
                        {
                            if (settingNode.NodeType == XmlNodeType.Element)
                            {
                                // Special case to mask API key
                                string val = (settingNode.Name.ToLower() == "apikey")
                                             ? "****************"
                                             : settingNode.InnerText;

                                settings.Add(new SettingItem
                                {
                                    Category = categoryNode.Name,
                                    Key = settingNode.Name,
                                    Value = val
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                settings.Add(new SettingItem { Category = "Error", Key = "Parsing", Value = ex.Message });
            }
            return settings;
        }
        // --- END OF NEW ---

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


