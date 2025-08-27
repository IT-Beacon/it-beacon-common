using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace it_beacon_common.Helpers
{
    /// <summary>
    /// Provides utilities for working with Windows light/dark mode themes.
    /// </summary>
    public static class ThemeHelper
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
        /// Applies BeaconUI.xaml to the Application resources.
        /// </summary>
        public static void ApplyTheme()
        {
            var appResources = Application.Current.Resources.MergedDictionaries;

            // Remove existing BeaconUI dictionary if already present
            for (int i = appResources.Count - 1; i >= 0; i--)
            {
                var dict = appResources[i];
                if (dict.Source != null &&
                    dict.Source.OriginalString.Contains("BeaconUI.xaml", StringComparison.OrdinalIgnoreCase))
                {
                    appResources.RemoveAt(i);
                }
            }

            // Add BeaconUI.xaml back in (handles both light/dark via DynamicResource)
            appResources.Add(new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/it-beacon-common;component/Themes/BeaconUI.xaml")
            });
        }

        /// <summary>
        /// Watches for registry changes and reapplies BeaconUI automatically.
        /// </summary>
        public static void RegisterThemeChangeListener()
        {
            SystemEvents.UserPreferenceChanged += (s, e) =>
            {
                if (e.Category == UserPreferenceCategory.General)
                {
                    ApplyTheme();
                }
            };
        }
    }
}