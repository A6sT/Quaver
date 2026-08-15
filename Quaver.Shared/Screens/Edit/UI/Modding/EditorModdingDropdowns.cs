using System.Collections.Generic;
using Quaver.Shared.Graphics.Form.Dropdowns;
using Quaver.Shared.Graphics.Form.Dropdowns.Custom;
using Quaver.Shared.Helpers;
using Wobble.Graphics;
using Wobble.Managers;

namespace Quaver.Shared.Screens.Edit.UI.Modding
{
    internal sealed class EditorModdingStatusDropdown : LabelledDropdown
    {
        public EditorModdingStatusDropdown() : base(LocalizationManager.Get("Screen_Editor_StatusLabel"), 18, new Dropdown(GetOptions(), new ScalableVector2(132, 30), 16, ColorHelper.HexToColor("#45D6F5")))
        {
            Dropdown.Depth = -3;
            Dropdown.Items.ForEach(x => x.Depth = -3);
        }

        private static List<string> GetOptions() => new List<string>
        {
            LocalizationManager.Get("Screen_Editor_All"),
            LocalizationManager.Get("Screen_Editor_ModdingPending"),
            LocalizationManager.Get("Screen_Editor_ModdingAccepted"),
            LocalizationManager.Get("Screen_Editor_ModdingDenied"),
            LocalizationManager.Get("Screen_Editor_ModdingIgnored")
        };
    }

    internal sealed class EditorModdingTypeDropdown : LabelledDropdown
    {
        public EditorModdingTypeDropdown() : base(LocalizationManager.Get("Screen_Editor_ModdingTypeLabel"), 18, new Dropdown(GetOptions(), new ScalableVector2(132, 30), 16, ColorHelper.HexToColor("#45D6F5")))
        {
            Dropdown.Depth = -3;
            Dropdown.Items.ForEach(x => x.Depth = -3);
        }

        private static List<string> GetOptions() => new List<string>
        {
            LocalizationManager.Get("Screen_Editor_ModdingIssue"),
            LocalizationManager.Get("Screen_Editor_ModdingSuggestion")
        };
    }
}