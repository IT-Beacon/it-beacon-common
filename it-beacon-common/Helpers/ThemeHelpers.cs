using Microsoft.Win32;
using System;
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
        /// Applies the appropriate light or dark theme brushes to the application's resources.
        /// </summary>
        /// <summary>
        /// Applies the appropriate light or dark theme brushes to the application's resources.
        /// </summary>
        public static void ApplyTheme()
        {
            var resources = Application.Current.Resources;
            bool useLightTheme = IsLightTheme;

            // Set brushes for light or dark theme
            if (useLightTheme)
            {
                resources["PopupBackgroundBrush"] = new SolidColorBrush(Colors.White);
                resources["PopupForegroundBrush"] = new SolidColorBrush(Colors.Black);
                //resources["PopupBorderBrush"] = new SolidColorBrush(Color.FromRgb(220, 220, 220));
                // Added a new brush for the footer row in light mode
                resources["FooterBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            }
            else
            {
                resources["PopupBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(32, 32, 32));
                resources["PopupForegroundBrush"] = new SolidColorBrush(Colors.White);
                //resources["PopupBorderBrush"] = new SolidColorBrush(Color.FromRgb(64, 64, 64));
                // Added a new brush for the footer row in dark mode
                resources["FooterBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
            }

            // Handle Windows Accent Color for buttons and highlights
            var accent = SystemParameters.WindowGlassColor;
            resources["AccentBrush"] = new SolidColorBrush(accent);
            resources["AccentHoverBrush"] = new SolidColorBrush(Lighten(accent, 0.25));
            resources["AccentPressedBrush"] = new SolidColorBrush(Darken(accent, 0.2));
        }

        /// <summary>
        /// Watches for registry changes and reapplies the theme automatically.
        /// </summary>
        public static void RegisterThemeChangeListener()
        {
            SystemEvents.UserPreferenceChanged += (s, e) =>
            {
                if (e.Category == UserPreferenceCategory.General || e.Category == UserPreferenceCategory.Color)
                {
                    Application.Current.Dispatcher.Invoke(ApplyTheme);
                }
            };
        }

        private static Color Lighten(Color color, double factor) => Color.FromRgb(
            (byte)Math.Min(255, color.R + (255 - color.R) * factor),
            (byte)Math.Min(255, color.G + (255 - color.G) * factor),
            (byte)Math.Min(255, color.B + (255 - color.B) * factor)
        );

        private static Color Darken(Color color, double factor) => Color.FromRgb(
            (byte)(color.R * (1 - factor)),
            (byte)(color.G * (1 - factor)),
            (byte)(color.B * (1 - factor))
        );
    }
}