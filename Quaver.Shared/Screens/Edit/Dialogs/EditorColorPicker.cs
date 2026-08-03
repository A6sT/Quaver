using System;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Quaver.Shared.Config;
using Wobble.Graphics.ImGUI;
using Wobble.Window;
using NumericsVector2 = System.Numerics.Vector2;
using NumericsVector3 = System.Numerics.Vector3;

namespace Quaver.Shared.Screens.Edit.Dialogs
{
    internal sealed class EditorColorPicker : SpriteImGui
    {
        private const ImGuiColorEditFlags PickerFlags =
            ImGuiColorEditFlags.DisplayRGB |
            ImGuiColorEditFlags.DisplayHex |
            ImGuiColorEditFlags.InputRGB |
            ImGuiColorEditFlags.Uint8;

        private readonly string title;
        private readonly Action<Color> changed;
        private readonly Action closeRequested;

        private NumericsVector3 color;
        private Color? pendingColor;
        private bool positionWindow = true;
        private bool outsideClickReady;

        public bool IsOpen { get; private set; } = true;

        public EditorColorPicker(string title, Color initialColor,
            Action<Color> onChanged, Action onCloseRequested)
            : base(true, EditorImGuiOptions.GetOptions(),
                ConfigManager.EditorImGuiScalePercentage.Value / 100f)
        {
            this.title = title;
            changed = onChanged;
            closeRequested = onCloseRequested;
            SetColor(initialColor);
        }

        public void SetColor(Color value)
        {
            color = new NumericsVector3(
                value.R / (float)byte.MaxValue,
                value.G / (float)byte.MaxValue,
                value.B / (float)byte.MaxValue);
            pendingColor = null;
        }

        public void Close()
        {
            CommitPendingColor();
            IsOpen = false;
        }

        protected override void RenderImguiLayout()
        {
            if (!IsOpen)
                return;

            if (positionWindow)
            {
                ImGui.SetNextWindowPos(
                    new NumericsVector2(WindowManager.Width / 2f, WindowManager.Height / 2f),
                    ImGuiCond.Appearing, new NumericsVector2(0.5f, 0.5f));
                positionWindow = false;
            }

            var open = IsOpen;
            ImGui.SetNextWindowSizeConstraints(
                new NumericsVector2(300, 0),
                new NumericsVector2(float.MaxValue, float.MaxValue));

            var contentsVisible = ImGui.Begin(title + "###EditorColorPicker", ref open,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings);
            var windowHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);

            if (contentsVisible)
            {
                if (ImGui.ColorPicker3("##EditorColor", ref color, PickerFlags))
                    pendingColor = ToColor(color);

                if (ImGui.IsItemDeactivatedAfterEdit() ||
                    pendingColor.HasValue &&
                    !ImGui.IsMouseDown(ImGuiMouseButton.Left) &&
                    !ImGui.IsAnyItemActive())
                    CommitPendingColor();
            }

            ImGui.End();

            if (!open)
            {
                closeRequested?.Invoke();
                return;
            }

            if (!outsideClickReady)
            {
                outsideClickReady = !ImGui.IsMouseDown(ImGuiMouseButton.Left);
                return;
            }

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !windowHovered)
                closeRequested?.Invoke();
        }

        private void CommitPendingColor()
        {
            if (!pendingColor.HasValue)
                return;

            changed?.Invoke(pendingColor.Value);
            pendingColor = null;
        }

        private static Color ToColor(NumericsVector3 value) => new Color(
            ToByte(value.X),
            ToByte(value.Y),
            ToByte(value.Z));

        private static byte ToByte(float value) =>
            (byte)Math.Round(Math.Clamp(value, 0, 1) * byte.MaxValue,
                MidpointRounding.AwayFromZero);
    }
}
