using System;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Quaver.Server.Client.Structures.Modding;
using Quaver.Shared.Helpers;
using Wobble.Managers;

namespace Quaver.Shared.Screens.Edit.UI.Modding
{
    internal static class EditorModdingFormatting
    {
        public static int? GetDisplayTime(MapModdingEntry mod)
        {
            if (string.IsNullOrWhiteSpace(mod?.MapTimestamp))
                return null;

            var firstSelection = mod.MapTimestamp.Split(',')[0];
            var timestamp = firstSelection.Split('|')[0];
            return int.TryParse(timestamp, out var value) ? value : (int?)null;
        }

        public static string FormatDisplayTime(MapModdingEntry mod)
        {
            var milliseconds = GetDisplayTime(mod);
            if (!milliseconds.HasValue)
                return LocalizationManager.Get("Screen_Editor_ModdingGeneral");

            var time = TimeSpan.FromMilliseconds(milliseconds.Value);
            return time.TotalHours >= 1
                ? time.ToString(@"h\:mm\:ss\.fff")
                : time.ToString(@"mm\:ss\.fff");
        }

        public static string GetStatusLabel(MapModdingStatus status)
        {
            switch (status)
            {
                case MapModdingStatus.Accepted:
                    return LocalizationManager.Get("Screen_Editor_ModdingAccepted");
                case MapModdingStatus.Denied:
                    return LocalizationManager.Get("Screen_Editor_ModdingDenied");
                case MapModdingStatus.Ignored:
                    return LocalizationManager.Get("Screen_Editor_ModdingIgnored");
                default:
                    return LocalizationManager.Get("Screen_Editor_ModdingPending");
            }
        }

        public static string GetTypeLabel(MapModdingType type) => type == MapModdingType.Suggestion
            ? LocalizationManager.Get("Screen_Editor_ModdingSuggestion")
            : LocalizationManager.Get("Screen_Editor_ModdingIssue");

        public static string GetPreview(string text)
        {
            var value = Regex.Replace(text ?? "", @"\s+", " ").Trim();
            return string.IsNullOrEmpty(value) ? "-" : value;
        }

        public static Color GetStatusColor(MapModdingStatus status)
        {
            switch (status)
            {
                case MapModdingStatus.Accepted:
                    return ColorHelper.HexToColor("#5EFF75");
                case MapModdingStatus.Denied:
                    return ColorHelper.HexToColor("#F9645D");
                case MapModdingStatus.Ignored:
                    return ColorHelper.HexToColor("#E9B736");
                default:
                    return ColorHelper.HexToColor("#45D6F5");
            }
        }
    }
}