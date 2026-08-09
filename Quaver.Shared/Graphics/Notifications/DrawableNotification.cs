using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Assets;
using Quaver.Shared.Graphics.Containers;
using Quaver.Shared.Graphics.Overlays.Hub;
using Quaver.Shared.Graphics.Overlays.Hub.Notifications;
using Quaver.Shared.Graphics.Notifications.V2;
using Quaver.Shared.Helpers;
using Quaver.Shared.Screens.V2;
using Quaver.Shared.Screens.V2.UI.OnlineHub;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Buttons;
using Wobble.Logging;
using Wobble.Managers;
using Wobble.Window;

namespace Quaver.Shared.Graphics.Notifications
{
    public class DrawableNotification : PoolableSprite<NotificationInfo>
    {
        /// <summary>
        ///     Horizontal padding between the components of a notification.
        /// </summary>
        private const int PADDING = 14;

        /// <summary>
        /// </summary>
        public override int HEIGHT { get; } = 0;

        /// <summary>
        /// </summary>
        private ImageButton Button { get; set; }

        /// <summary>
        /// </summary>
        private Sprite Icon { get; set; }

        /// <summary>
        /// </summary>
        private SpriteTextPlus Text { get; set; }

        private NotificationCard Card { get; set; }

        private float LastNotificationWindowWidth { get; set; } = -1;

        internal bool UsesOnlineHubStyle { get; }

        internal bool IsReadyToDisplay => !UsesOnlineHubStyle || Card.IsReadyToDisplay;

        internal void PrepareForDisplay() => RunScheduledUpdates();

        /// <summary>
        ///     The amount of time the notification has been inactive (not hovered)
        /// </summary>
        private double TimeInactive { get; set; }

        /// <summary>
        ///     If the notification is currently sliding out
        /// </summary>
        public bool IsSlidingOut { get; private set; }

        /// <summary>
        /// </summary>
        public bool HasSlidOut { get; private set; }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="container"></param>
        /// <param name="item"></param>
        /// <param name="index"></param>
        public DrawableNotification(PoolableScrollContainer<NotificationInfo> container, NotificationInfo item, int index) : base(container, item, index)
        {
            UsesOnlineHubStyle = container == null && GameBase.Game is QuaverGame game && game.CurrentScreen is SkinV2Screen;

            if (UsesOnlineHubStyle)
                CreateNotificationCard(item);
            else
                CreateLegacyContent();

            // ReSharper disable once VirtualMemberCallInConstructor
            UpdateContent(Item, Index);
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            if (UsesOnlineHubStyle)
                ResizeNotificationCard();
            else
            {
                Button.Size = new ScalableVector2(Width - Border.Thickness * 2, Height - Border.Thickness * 2);
                Button.Alpha = Button.IsHovered ? 0.35f : 0;
            }

            var game = (QuaverGame)GameBase.Game;

            if (Container != null)
                Button.IsClickable = game.OnlineHub.SelectedSection == game.OnlineHub.Sections[OnlineHubSectionType.Notifications];

            var isHovered = UsesOnlineHubStyle ? Card.IsHovered : Button.IsHovered;
            if (isHovered)
                TimeInactive = 0;
            else
                TimeInactive += gameTime.ElapsedGameTime.TotalMilliseconds;

            // Automatically slide out the notification after a few seconds
            if (Item.AutomaticallySlide && !IsSlidingOut && TimeInactive >= 5000)
                SlideOut();

            // Mark the notification as having slid out
            if (Item.AutomaticallySlide && !HasSlidOut && IsSlidingOut && Animations.Count == 0)
                HasSlidOut = true;

            base.Update(gameTime);
        }

        public override void DrawToSpriteBatch()
        {
            if (UsesOnlineHubStyle)
                return;

            base.DrawToSpriteBatch();
        }

        public override void Destroy()
        {
            if (Card != null)
                Card.PrimaryAction -= OnNotificationCardPrimaryAction;

            base.Destroy();
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        public override void UpdateContent(NotificationInfo item, int index)
        {
            Item = item;
            Index = index;

            ScheduleUpdate(() =>
            {
                if (Item.AutomaticallySlide)
                    SlideIn();

                ApplyContent();
            });
        }

        /// <summary>
        ///     Refreshes the notification content and restarts its visible lifetime.
        /// </summary>
        /// <param name="item"></param>
        internal void Refresh(NotificationInfo item)
        {
            Item = item;
            TimeInactive = 0;
            IsSlidingOut = false;
            HasSlidOut = false;
            ClearAnimations();

            ScheduleUpdate(() =>
            {
                ApplyContent();

                if (Item.AutomaticallySlide)
                    MoveToX(-30, Easing.OutQuint, 450);
            });
        }

        internal void DismissWithoutAction() => Item.WasClicked = true;

        /// <summary>
        /// </summary>
        private void ApplyContent()
        {
            if (UsesOnlineHubStyle)
            {
                if (!Card.Supports(Item))
                    ReplaceNotificationCard(Item);
                Card.SetContent(Item, DateTimeOffset.Now);
                return;
            }

            Border.Tint = GetColor();
            Icon.Image = GetIconTexture();

            Text.Text = Item.Text;

            const int padding = 30;
            Height = Math.Max(Icon.Height + padding, Text.Height + padding);
        }

        /// <summary>
        /// </summary>
        private void CreateButton()
        {
            Button = new ImageButton(UserInterface.BlankBox, (sender, args) =>
            {
                // Make the notification not clickable if it's currently sliding out
                if (IsSlidingOut)
                    return;

                Item.ClickAction?.Invoke(sender, args);
                Item.WasClicked = true;

                if (Container != null)
                {
                    var container = (NotificationScrollContainer)Container;
                    container.Remove(Item, false);
                }
            })
            {
                Parent = this,
                Alignment = Alignment.MidCenter,
                Alpha = 0,
                AllowInputWhenDialogOpen = true,
                UsePreviousSpriteBatchOptions = true
            };
        }

        /// <summary>
        /// </summary>
        public void SlideIn()
        {
            X = Width + 10;
            MoveToX(-30, Easing.OutQuint, 450);
        }

        /// <summary>
        /// </summary>
        public void SlideOut()
        {
            IsSlidingOut = true;
            MoveToX(Width + 10, Easing.OutQuint, 450);
        }

        private void CreateLegacyContent()
        {
            Size = new ScalableVector2(408, 86);
            Tint = ColorHelper.HexToColor("#242424");
            AddBorder(Color.White, 2);
            CreateButton();
            CreateIcon();
            CreateText();
        }

        private void CreateNotificationCard(NotificationInfo notification)
        {
            var config = OnlineHubDesign.Default.Notifications;
            Card = NotificationCard.Create(config.Row, config.MultiplayerInvite, NotificationTextures.Shared,
                NotificationCardDisplay.Quick, notification);
            Card.Parent = this;
            Card.Alignment = Alignment.TopLeft;
            Card.AllowInputWhenDialogOpen = true;
            Card.UsePreviousSpriteBatchOptions = true;
            Card.PrimaryAction += OnNotificationCardPrimaryAction;
            Alignment = Alignment.TopRight;
            Tint = Color.Transparent;
            ResizeNotificationCard(true);
        }

        private void ReplaceNotificationCard(NotificationInfo notification)
        {
            Card.PrimaryAction -= OnNotificationCardPrimaryAction;
            Card.Destroy();
            CreateNotificationCard(notification);
        }

        private void ResizeNotificationCard(bool force = false)
        {
            if (!force && Math.Abs(LastNotificationWindowWidth - WindowManager.Width) <= 0.001f)
                return;

            LastNotificationWindowWidth = WindowManager.Width;
            var onlineHub = OnlineHubDesign.Default;
            var panelWidth = Math.Min(onlineHub.Window.Width, WindowManager.Width);
            var notifications = onlineHub.Notifications;
            var width = panelWidth - onlineHub.Padding * 2 - onlineHub.Feed.ScrollbarWidth -
                        onlineHub.Feed.RowGap;
            Size = new ScalableVector2(Math.Max(0, width), notifications.Row.Height);
            Card.Size = Size;
        }

        private void OnNotificationCardPrimaryAction(object sender, EventArgs args)
        {
            if (IsSlidingOut)
                return;

            Item.ClickAction?.Invoke(sender, args);
            Item.WasClicked = true;
        }

        /// <summary>
        /// </summary>
        private void CreateIcon()
        {
            Icon = new Sprite
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                Size = new ScalableVector2(34, 34),
                X = PADDING,
                UsePreviousSpriteBatchOptions = true
            };
        }

        /// <summary>
        /// </summary>
        private void CreateText()
        {
            Text = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterSemiBold), "", 18)
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = PADDING + Icon.Width + PADDING,
                MaxWidth = Width - PADDING - PADDING - Icon.Width - PADDING,
                UsePreviousSpriteBatchOptions = true
            };
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private Color GetColor()
        {
            switch (Item.Level)
            {
                case NotificationLevel.Info:
                    return ColorHelper.HexToColor("#0FBAE5");
                case NotificationLevel.Error:
                    return ColorHelper.HexToColor("#F9645D");
                case NotificationLevel.Warning:
                    return ColorHelper.HexToColor("#E9B736");
                case NotificationLevel.Success:
                    return ColorHelper.HexToColor("#27B06E");
                default:
                    return ColorHelper.HexToColor("#0FBAE5");
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private Texture2D GetIconTexture()
        {
            switch (Item.Level)
            {
                case NotificationLevel.Info:
                    return UserInterface.NotificationInfo;
                case NotificationLevel.Error:
                    return UserInterface.NotificationError;
                case NotificationLevel.Warning:
                    return UserInterface.NotificationWarning;
                case NotificationLevel.Success:
                    return UserInterface.NotificationSuccess;
                default:
                    return UserInterface.NotificationInfo;
            }
        }
    }
}
