using System;
using Microsoft.Xna.Framework;
using Quaver.Shared.Assets;
using Quaver.Shared.Screens.V2.UI.OnlineHub;
using Quaver.Shared.Skinning.V2;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Buttons;
using Wobble.Graphics.Shaders;
using Wobble.Managers;

namespace Quaver.Shared.Screens.V2.UI.OnlineHub.Notifications
{
    internal enum NotificationView
    {
        New,
        History
    }

    internal sealed class NotificationHeader : Container
    {
        private static OnlineHubDesign Design => OnlineHubDesign.Default;

        private OnlineHubNotificationsDesign Config { get; }

        private FlexContainer Layout { get; }

        private RoundedButton NewButton { get; }

        private RoundedButton HistoryButton { get; }

        private RoundedButton ClearButton { get; }

        private Container Spacer { get; }

        private Color ButtonColor { get; }

        private Color SelectedButtonColor { get; }

        private Color TextColor { get; }

        internal NotificationView SelectedView { get; private set; }

        internal event EventHandler SelectedViewChanged;

        internal NotificationHeader(OnlineHubNotificationsDesign config, Action clear)
        {
            Config = config;
            ButtonColor = SkinV2Color.Parse(Design.Style.ControlColor);
            SelectedButtonColor = SkinV2Color.Parse(Design.Style.AccentColor);
            TextColor = SkinV2Color.Parse(Design.Style.TextColor);
            Height = Design.Toolbar.Height;

            Layout = new FlexContainer
            {
                Parent = this,
                Direction = FlexDirection.Row,
                AlignItems = FlexAlignItems.Stretch
            };
            NewButton = CreateSegmentButton("Screen_OnlineHub_New", SelectNew,
                new RoundedRectCornerRadii(Design.Style.CornerRadius, 0, 0, Design.Style.CornerRadius));
            HistoryButton = CreateSegmentButton("Screen_Multi_History", SelectHistory,
                new RoundedRectCornerRadii(0, Design.Style.CornerRadius, Design.Style.CornerRadius, 0));
            Spacer = new Container { Parent = Layout };
            ClearButton = CreateClearButton(clear);

            var segmentWidth = Config.SegmentedControlWidth / 2;
            Layout.SetItemOptions(NewButton, new FlexItemOptions { Basis = segmentWidth, Shrink = 1 });
            Layout.SetItemOptions(HistoryButton, new FlexItemOptions { Basis = segmentWidth, Shrink = 1 });
            Layout.SetItemOptions(Spacer, new FlexItemOptions { Basis = 0, Grow = 1, Shrink = 0 });
            Layout.SetItemOptions(ClearButton, new FlexItemOptions { Basis = Design.Toolbar.ClearButtonWidth, Shrink = 1 });

            SizeChanged += OnSizeChanged;
            UpdateButtonStyles();
        }

        public override void Destroy()
        {
            SizeChanged -= OnSizeChanged;
            base.Destroy();
        }

        private RoundedButton CreateSegmentButton(string localizationKey, Action action,
            RoundedRectCornerRadii cornerRadii)
        {
            var button = new RoundedButton((sender, args) => action())
            {
                Parent = Layout,
                Size = new ScalableVector2(1, 1),
                CornerRadii = cornerRadii,
                PerformHoverFade = true
            };
            button.SetLabel(FontManager.GetWobbleFont(Design.Style.Font), LocalizationManager.Get(localizationKey),
                Design.Style.FontSize, TextColor);
            return button;
        }

        private RoundedButton CreateClearButton(Action clear)
        {
            var button = new RoundedButton((sender, args) => clear())
            {
                Parent = Layout,
                Size = new ScalableVector2(1, 1),
                CornerRadius = Design.Style.CornerRadius,
                PerformHoverFade = true
            };
            button.SetIcon(UserInterface.ClearAllIcon, new Vector2(Design.Toolbar.IconSize, Design.Toolbar.IconSize));
            button.SetLabel(FontManager.GetWobbleFont(Design.Style.Font),
                LocalizationManager.Get("Screen_OnlineHub_ClearAll"), Design.Style.FontSize, TextColor);
            return button;
        }

        private void SelectNew() => Select(NotificationView.New);

        private void SelectHistory() => Select(NotificationView.History);

        private void Select(NotificationView view)
        {
            if (SelectedView == view)
                return;

            SelectedView = view;
            UpdateButtonStyles();
            SelectedViewChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateButtonStyles()
        {
            ApplyButtonStyle(NewButton, SelectedView == NotificationView.New);
            ApplyButtonStyle(HistoryButton, SelectedView == NotificationView.History);
            ApplyButtonStyle(ClearButton, false);
        }

        private void ApplyButtonStyle(RoundedButton button, bool selected)
        {
            button.Tint = selected ? SelectedButtonColor : ButtonColor;
            if (button.Icon != null)
                button.Icon.Tint = TextColor;
            button.Label.Tint = TextColor;
        }

        private void OnSizeChanged(object sender, ScalableVector2 size)
        {
            Layout.Position = new ScalableVector2(0, Design.Toolbar.Padding);
            Layout.Size = new ScalableVector2(Width, Math.Max(0, Height - Design.Toolbar.Padding * 2));
            Layout.RefreshLayout();
        }
    }
}
