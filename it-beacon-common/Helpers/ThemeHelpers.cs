using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace it_beacon_common.Helpers
{
    /// <summary>
    /// Provides utilities for working with Windows light/dark mode themes.
    /// </summary>
    public static class ThemeHelpers
    {
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string RegistryValueName = "AppsUseLightTheme";

        /// <summary>
        /// Gets whether the system is currently using Light theme.
        /// </summary>
        public static bool IsLightTheme
        {
            get
            {
                try
                {
                    using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
                    object? registryValueObject = key?.GetValue(RegistryValueName);

                    if (registryValueObject == null)
                        return true; // Default to light if missing

                    int registryValue = (int)registryValueObject;
                    return registryValue > 0;
                }
                catch
                {
                    return true; // Fail safe: assume light
                }
            }
        }

        /// <summary>
        /// Applies the correct theme ResourceDictionary (BeaconUI.xaml) to the application.
        /// Call this at startup and on theme changes.
        /// </summary>
        public static void ApplyTheme(ResourceDictionary lightTheme, ResourceDictionary darkTheme)
        {
            var appResources = Application.Current.Resources.MergedDictionaries;

            // Remove any existing theme dictionaries
            for (int i = appResources.Count - 1; i >= 0; i--)
            {
                var dict = appResources[i];
                if (dict.Source != null &&
                    (dict.Source.OriginalString.Contains("BeaconUI.Light.xaml") ||
                     dict.Source.OriginalString.Contains("BeaconUI.Dark.xaml")))
                {
                    appResources.RemoveAt(i);
                }
            }

            // Add the correct theme dictionary
            appResources.Add(IsLightTheme ? lightTheme : darkTheme);
        }

        /// <summary>
        /// Watches for registry changes and applies theme updates automatically.
        /// </summary>
        public static void RegisterThemeChangeListener(Action onThemeChanged)
        {
            SystemEvents.UserPreferenceChanged += (s, e) =>
            {
                if (e.Category == UserPreferenceCategory.General)
                {
                    onThemeChanged?.Invoke();
                }
            };
        }
    }
}
