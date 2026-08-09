using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Assets;
using Quaver.Shared.Skinning.V2;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Buttons;
using Wobble.Graphics.Sprites;
using Wobble.Managers;

namespace Quaver.Shared.Screens.V2.UI.OnlineHub
{
    internal sealed class OnlineHubTabNavigation : Container
    {
        private const int TabCount = 3;

        private static OnlineHubDesign Design => OnlineHubDesign.Default;

        private OnlineHubToolbarDesign Config { get; }

        private Sprite Background { get; }

        private FlexContainer ButtonLayout { get; }

        private RoundedButton[] Buttons { get; } = new RoundedButton[TabCount];

        private Color ButtonColor { get; }

        private Color SelectedButtonColor { get; }

        private Color TextColor { get; }

        internal OnlineHubTab SelectedTab { get; private set; } = OnlineHubTab.Notifications;

        internal event EventHandler SelectedTabChanged;

        internal OnlineHubTabNavigation(OnlineHubToolbarDesign config)
        {
            Config = config;
            ButtonColor = SkinV2Color.Parse(Design.Style.SurfaceColor);
            SelectedButtonColor = SkinV2Color.Parse(Design.Style.AccentColor);
            TextColor = SkinV2Color.Parse(Design.Style.TextColor);
            Size = new ScalableVector2(1, Config.Height);

            Background = new Sprite
            {
                Parent = this,
                Image = UserInterface.BlankBox,
                Tint = SkinV2Color.Parse(Design.Header.BackgroundColor),
                UsePreviousSpriteBatchOptions = true
            };
            ButtonLayout = new FlexContainer
            {
                Parent = this,
                Direction = FlexDirection.Row,
                AlignItems = FlexAlignItems.Stretch,
                ColumnGap = Config.Gap
            };

            CreateButton(OnlineHubTab.Notifications, UserInterface.HubNotifications, "Screen_Options_Notifications");
            CreateButton(OnlineHubTab.Users, UserInterface.HubOnlineUsers, "Screen_OnlineHub_Users");
            CreateButton(OnlineHubTab.SongRequests, UserInterface.HubSongRequests, "Screen_OnlineHub_SongRequests");

            SizeChanged += OnSizeChanged;
            ResizeChildren();
            UpdateButtonStyles();
        }

        public override void Destroy()
        {
            SizeChanged -= OnSizeChanged;
            base.Destroy();
        }

        private void CreateButton(OnlineHubTab tab, Texture2D icon, string localizationKey)
        {
            var button = new RoundedButton((sender, args) => Select(tab))
            {
                Parent = ButtonLayout,
                Size = new ScalableVector2(1, 1),
                CornerRadius = Design.Style.CornerRadius,
                PerformHoverFade = true
            };
            button.SetIcon(icon, new Vector2(Config.IconSize, Config.IconSize));
            button.SetLabel(FontManager.GetWobbleFont(Design.Style.Font), LocalizationManager.Get(localizationKey), Design.Style.FontSize, TextColor);

            Buttons[(int) tab] = button;
            ButtonLayout.SetItemOptions(button, new FlexItemOptions { Basis = 0, Grow = 1, Shrink = 1 });
        }

        internal void Select(OnlineHubTab tab)
        {
            if (SelectedTab == tab)
                return;

            SelectedTab = tab;
            UpdateButtonStyles();
            SelectedTabChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateButtonStyles()
        {
            for (var i = 0; i < Buttons.Length; i++)
            {
                var selected = i == (int) SelectedTab;
                Buttons[i].Tint = selected ? SelectedButtonColor : ButtonColor;
                Buttons[i].Icon.Tint = TextColor;
                Buttons[i].Label.Tint = TextColor;
            }
        }

        private void OnSizeChanged(object sender, ScalableVector2 size) => ResizeChildren();

        private void ResizeChildren()
        {
            Background.Size = Size;
            ButtonLayout.Position = new ScalableVector2(Config.Padding, Config.Padding);
            ButtonLayout.Size = new ScalableVector2(Math.Max(0, Width - Config.Padding * 2), Math.Max(0, Height - Config.Padding * 2));
            ButtonLayout.RefreshLayout();
        }
    }
}
