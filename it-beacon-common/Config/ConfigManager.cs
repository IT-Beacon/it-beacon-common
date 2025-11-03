using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Globalization; // Needed for hex conversion

namespace it_beacon_common.Config
{
    /// <summary>
    /// Represents a single QuickShortcut button configuration.
    /// </summary>
    public class QuickShortcut
    {
        public string Glyph { get; set; } = "\uE713"; // Default glyph (settings icon)
        public string ToolTip { get; set; } = "Default Tooltip";
        public string Url { get; set; } = "https://itservices.cvad.unt.edu";
    }

    /// <summary>
    /// A class to represent a key-value pair for the settings window.
    /// </summary>
    public class SettingItem : INotifyPropertyChanged
    {
        private string _value = string.Empty;
        private string _originalValue = string.Empty;

        public string Category { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// A hint for the UI on how to render this setting.
        /// (e.g., "string", "bool", "glyph")
        /// </summary>
        public string DisplayType { get; set; } = "string";

        // This property will hold the *raw* string (e.g., E8F2)
        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    IsDirty = true;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsReadOnly { get; set; } = false;
        public bool IsDirty { get; private set; } = false;

        public void SetOriginalValue(string value)
        {
            _value = value;
            _originalValue = value;
            IsDirty = false;
        }

        public void AcceptChanges()
        {
            _originalValue = _value;
            IsDirty = false;
        }

        public void RevertChanges()
        {
            Value = _originalValue;
            IsDirty = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    /// <summary>
    /// Static class to load and access settings from Config/settings.xml.
    /// </summary>
    public static class ConfigManager
    {
        private static XmlDocument? _config;
        private static bool _isLoaded = false;
        private static readonly string _configFilePath = Path.Combine(AppContext.BaseDirectory, "Config", "settings.xml");
        private static readonly object _configLock = new object();

        // ... LoadConfig, GetString, GetBool ... remain unchanged ...
        #region Standard Load/Get Methods
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
                _config = null;
            }
        }

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

        public static bool GetBool(string xPath, bool defaultValue = false)
        {
            string val = GetString(xPath, defaultValue.ToString());
            if (bool.TryParse(val, out bool result))
            {
                return result;
            }
            return defaultValue;
        }
        #endregion

        // --- MODIFIED ---
        /// <summary>
        /// Gets the list of configured quick shortcuts.
        /// This method reads the raw hex (e.g., E8F2) and converts it
        /// into the actual Unicode character (e.g., \uE8F2) for the UI.
        /// </summary>
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
                    string rawGlyph = node["Glyph"]?.InnerText ?? "E713";
                    string glyphChar;

                    try
                    {
                        // --- THIS IS THE FIX ---
                        // Convert hex string "E8F2" into the integer
                        int glyphCode = int.Parse(rawGlyph, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        // Cast the integer to a char, and then to a string
                        glyphChar = ((char)glyphCode).ToString();
                        // --- END OF FIX ---
                    }
                    catch
                    {
                        glyphChar = "\uE713"; // Fallback to default icon on error
                    }

                    shortcuts.Add(new QuickShortcut
                    {
                        Glyph = glyphChar, // This now holds the actual character
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


        // --- UNCHANGED ---
        /// <summary>
        /// Reads the entire loaded XML config and flattens it into a list for display
        /// in the Settings Window. This method uses the raw, un-formatted values.
        /// </summary>
        public static List<SettingItem> GetAllSettings()
        {
            var settings = new List<SettingItem>();
            if (!_isLoaded || _config == null)
            {
                settings.Add(new SettingItem { Category = "Error", Key = "ConfigManager", Value = "Config file not found or not loaded.", IsReadOnly = true });
                settings.Add(new SettingItem { Category = "Error", Key = "Expected Path", Value = _configFilePath, IsReadOnly = true });
                return settings;
            }

            try
            {
                var root = _config.SelectSingleNode("/Settings");
                if (root == null)
                {
                    settings.Add(new SettingItem { Category = "Error", Key = "Parsing", Value = "Could not find /Settings root node.", IsReadOnly = true });
                    return settings;
                }

                if (root.ChildNodes == null)
                {
                    settings.Add(new SettingItem { Category = "Info", Key = "Parsing", Value = "Settings file is empty.", IsReadOnly = true });
                    return settings;
                }

                foreach (XmlNode categoryNode in root.ChildNodes)
                {
                    if (categoryNode.NodeType != XmlNodeType.Element) continue;

                    if (categoryNode.Name == "QuickShortcuts")
                    {
                        int i = 1;
                        var shortcutNodes = categoryNode.SelectNodes("Shortcut");
                        if (shortcutNodes != null)
                        {
                            foreach (XmlNode shortcutNode in shortcutNodes)
                            {
                                string categoryName = $"QuickShortcut {i++}";
                                if (shortcutNode.ChildNodes != null)
                                {
                                    foreach (XmlNode shortcutSetting in shortcutNode.ChildNodes)
                                    {
                                        if (shortcutSetting.NodeType == XmlNodeType.Element)
                                        {
                                            bool isGlyph = shortcutSetting.Name == "Glyph";
                                            // This correctly reads the raw hex "E8F2"
                                            string value = shortcutSetting.InnerText;

                                            settings.Add(new SettingItem
                                            {
                                                Category = categoryName,
                                                Key = shortcutSetting.Name,
                                                IsReadOnly = false,
                                                DisplayType = isGlyph ? "glyph" : "string"
                                            }.Also(s => s.SetOriginalValue(value)));
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (categoryNode.ChildNodes.Count > 0 && categoryNode.FirstChild?.NodeType == XmlNodeType.Element)
                    {
                        // Node has child elements (e.g., SnipeIT, PopupWindow)
                        foreach (XmlNode settingNode in categoryNode.ChildNodes)
                        {
                            if (settingNode.NodeType == XmlNodeType.Element)
                            {
                                bool isApiKey = settingNode.Name.ToLower() == "apikey";
                                string val = isApiKey ? "****************" : settingNode.InnerText;

                                string displayType = "string";
                                if (bool.TryParse(settingNode.InnerText, out _))
                                {
                                    displayType = "bool";
                                }

                                settings.Add(new SettingItem
                                {
                                    Category = categoryNode.Name,
                                    Key = settingNode.Name,
                                    IsReadOnly = isApiKey,
                                    DisplayType = displayType
                                }.Also(s => s.SetOriginalValue(val)));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                settings.Add(new SettingItem { Category = "Error", Key = "Parsing", Value = ex.Message, IsReadOnly = true });
            }
            return settings;
        }

        // --- UNCHANGED ---
        /// <summary>
        /// Saves all changed settings back to the settings.xml file.
        /// It expects the raw, un-formatted values from the Settings Window.
        /// </summary>
        public static bool SaveAllSettings(IEnumerable<SettingItem> settings)
        {
            if (_config == null) return false;

            var changedSettings = settings.Where(s => s.IsDirty && !s.IsReadOnly).ToList();
            if (changedSettings.Count == 0) return true; // Nothing to save

            lock (_configLock)
            {
                try
                {
                    foreach (var item in changedSettings)
                    {
                        XmlNode? targetNode = null;

                        if (item.Category.StartsWith("QuickShortcut"))
                        {
                            if (int.TryParse(item.Category.Split(' ').LastOrDefault(), out int index))
                            {
                                var shortcutNodes = _config.SelectNodes("/Settings/QuickShortcuts/Shortcut");
                                if (shortcutNodes != null && index - 1 < shortcutNodes.Count)
                                {
                                    targetNode = shortcutNodes[index - 1]?.SelectSingleNode(item.Key);
                                }
                            }
                        }
                        else
                        {
                            string xPath = $"/Settings/{item.Category}/{item.Key}";
                            targetNode = _config.SelectSingleNode(xPath);
                        }

                        if (targetNode != null)
                        {
                            // This correctly saves the raw hex "E8F2"
                            if (item.DisplayType == "bool")
                            {
                                targetNode.InnerText = item.Value.ToLower();
                            }
                            else
                            {
                                targetNode.InnerText = item.Value;
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"[ConfigManager] WARNING: Could not find node to save for setting: {item.Category}/{item.Key}");
                        }
                    }

                    _config.Save(_configFilePath);

                    foreach (var item in changedSettings)
                    {
                        item.AcceptChanges();
                    }

                    Debug.WriteLine($"[ConfigManager] Successfully saved {changedSettings.Count} settings to: {_configFilePath}");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ConfigManager] ERROR: Failed to save config file: {ex.Message}");
                    return false;
                }
            }
        }
    }

    // Helper extension (unchanged)
    internal static class ObjectExtensions
    {
        public static T Also<T>(this T obj, Action<T> action)
        {
            action(obj);
            return obj;
        }
    }
}