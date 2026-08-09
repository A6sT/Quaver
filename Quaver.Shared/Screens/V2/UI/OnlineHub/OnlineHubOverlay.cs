using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Quaver.Shared.Skinning;
using Quaver.Shared.Skinning.V2;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Input;
using Wobble.Window;

namespace Quaver.Shared.Screens.V2.UI.OnlineHub
{
    internal sealed class OnlineHubOverlay : DialogScreen
    {
        private SkinStoreV2Lease Skin { get; }

        private OnlineHubWindowDesign Config { get; }

        private OnlineHubTab InitialTab { get; }

        private OnlineHubPanel Panel { get; set; }

        private bool IsClosing { get; set; }

        private double CloseTimeRemaining { get; set; }

        private float LastWindowWidth { get; set; } = -1;

        private float LastWindowHeight { get; set; } = -1;

        internal OnlineHubTab SelectedTab => Panel.SelectedTab;

        internal OnlineHubOverlay(OnlineHubTab initialTab = OnlineHubTab.Notifications) : base(0)
        {
            Skin = SkinManager.AcquireV2();
            Config = OnlineHubDesign.Default.Window;
            InitialTab = initialTab;
            AutoResizeForResolutions = false;
            IsInteractionEnabled = false;
            CreateContent();
        }

        public override void CreateContent()
        {
            Alignment = Alignment.TopLeft;
            Size = new ScalableVector2(WindowManager.Width, WindowManager.Height);

            Container.Size = Size;
            Panel = new OnlineHubPanel(InitialTab)
            {
                Parent = Container,
                Alignment = Alignment.TopRight
            };

            ResizeToWindow(true);
            Panel.ClearAnimations();
            Panel.X = Panel.Width;
            Panel.MoveToX(0, Easing.OutCubic, Config.SlideDurationMilliseconds);
            ScreenNavigation.SetOnlineHubHeaderPosition(Panel.X, Panel.Width);
        }

        public override void Update(GameTime gameTime)
        {
            ResizeToWindow();

            if (!IsClosing && GameBase.Game is QuaverGame game && game.CurrentScreen is not SkinV2Screen)
                Close();

            base.Update(gameTime);
            ScreenNavigation.SetOnlineHubHeaderPosition(Panel.X, Panel.Width);

            if (!IsClosing)
                return;

            CloseTimeRemaining -= gameTime.ElapsedGameTime.TotalMilliseconds;
            if (CloseTimeRemaining <= 0)
                DialogManager.Dismiss(this);
        }

        public override void HandleInput(GameTime gameTime)
        {
            if (KeyboardManager.IsUniqueKeyPress(Keys.Escape))
            {
                Close();
                return;
            }

            if (MouseManager.IsUniqueClick(MouseButton.Left) && !Panel.IsHovered())
                Close();
        }

        public override void DrawToSpriteBatch() { }

        public override void Destroy()
        {
            ScreenNavigation.SetOnlineHubHeaderPosition(Panel.Width, Panel.Width);
            base.Destroy();
            Skin.Dispose();
        }

        internal void Close()
        {
            if (IsClosing)
                return;

            IsClosing = true;
            CloseTimeRemaining = Config.SlideDurationMilliseconds;
            Panel.ClearAnimations();
            Panel.MoveToX(Panel.Width, Easing.InCubic, Config.SlideDurationMilliseconds);
        }

        internal void Open(OnlineHubTab tab)
        {
            Panel.SelectTab(tab);
            if (!IsClosing)
                return;

            IsClosing = false;
            CloseTimeRemaining = 0;
            Panel.ClearAnimations();
            Panel.MoveToX(0, Easing.OutCubic, Config.SlideDurationMilliseconds);
        }

        private void ResizeToWindow(bool force = false)
        {
            var windowWidth = WindowManager.Width;
            var windowHeight = WindowManager.Height;
            if (!force && Math.Abs(LastWindowWidth - windowWidth) <= 0.001f &&
                Math.Abs(LastWindowHeight - windowHeight) <= 0.001f)
                return;

            LastWindowWidth = windowWidth;
            LastWindowHeight = windowHeight;
            Size = new ScalableVector2(windowWidth, windowHeight);
            Container.Size = Size;

            var navigation = Skin.Config.Shared.Navigation;
            var navigationHeight = navigation.Button.Size + navigation.EdgePadding * 2;
            var panelWidth = Math.Min(Config.Width, windowWidth);
            var panelHeight = Math.Max(0, windowHeight - navigationHeight * 2);

            var widthChanged = Math.Abs(Panel.Width - panelWidth) > 0.001f;
            Panel.Resize(panelWidth, panelHeight);
            Panel.Y = navigationHeight;

            if (!widthChanged)
                return;

            Panel.ClearAnimations();
            if (IsClosing)
            {
                CloseTimeRemaining = Config.SlideDurationMilliseconds;
                Panel.MoveToX(Panel.Width, Easing.InCubic, Config.SlideDurationMilliseconds);
            }
            else
            {
                Panel.MoveToX(0, Easing.OutCubic, Config.SlideDurationMilliseconds);
            }
        }
    }
}
