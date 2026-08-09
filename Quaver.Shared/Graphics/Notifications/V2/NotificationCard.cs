using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Shared.Assets;
using Quaver.Shared.Screens.V2.UI.OnlineHub;
using Quaver.Shared.Skinning.V2;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Buttons;
using Wobble.Graphics.Shaders;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;

namespace Quaver.Shared.Graphics.Notifications.V2
{
    internal enum NotificationCardDisplay
    {
        Hub,
        Quick
    }

    internal sealed class NotificationTextures
    {
        internal static NotificationTextures Shared { get; } = new NotificationTextures();

        private Texture2D Info { get; }

        private Texture2D Error { get; }

        private Texture2D Warning { get; }

        private Texture2D Success { get; }

        internal Texture2D Timestamp { get; }

        private NotificationTextures()
        {
            Info = UserInterface.NotificationInfo;
            Error = UserInterface.NotificationError;
            Warning = UserInterface.NotificationWarning;
            Success = UserInterface.NotificationSuccess;
            Timestamp = UserInterface.Clock;
        }

        internal Texture2D GetIcon(NotificationLevel level)
        {
            switch (level)
            {
                case NotificationLevel.Error:
                    return Error;
                case NotificationLevel.Warning:
                    return Warning;
                case NotificationLevel.Success:
                    return Success;
                default:
                    return Info;
            }
        }
    }

    internal abstract class NotificationCard : RoundedButton
    {
        protected static OnlineHubDesign Design => OnlineHubDesign.Default;

        protected OnlineHubNotificationRowDesign Config { get; }

        protected NotificationCardDisplay Display { get; }

        protected Color TextColor { get; }

        protected Sprite TimestampIcon { get; }

        protected SpriteTextPlus Timestamp { get; }

        private Sprite Accent { get; }

        private Sprite Background { get; }

        private Color InfoColor { get; }

        private Color ErrorColor { get; }

        private Color WarningColor { get; }

        private Color SuccessColor { get; }

        protected abstract bool CanActivateFromCard { get; }

        internal virtual bool IsReadyToDisplay => true;

        internal event EventHandler PrimaryAction;

        internal event EventHandler SecondaryAction;

        protected NotificationCard(OnlineHubNotificationRowDesign config, NotificationTextures textures,
            NotificationCardDisplay display)
        {
            Config = config;
            Display = display;
            TextColor = SkinV2Color.Parse(Design.Style.TextColor);
            InfoColor = SkinV2Color.Parse(Config.InfoColor);
            ErrorColor = SkinV2Color.Parse(Config.ErrorColor);
            WarningColor = SkinV2Color.Parse(Config.WarningColor);
            SuccessColor = SkinV2Color.Parse(Config.SuccessColor);
            Height = Config.Height;
            CornerRadius = Design.Style.CornerRadius;
            PerformHoverFade = true;

            if (Display == NotificationCardDisplay.Hub)
            {
                Accent = new Sprite
                {
                    Parent = this,
                    Alignment = Alignment.TopLeft,
                    UsePreviousSpriteBatchOptions = true
                };
            }

            Background = new Sprite
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                Tint = SkinV2Color.Parse(Config.BackgroundColor),
                UsePreviousSpriteBatchOptions = true
            };

            if (Display == NotificationCardDisplay.Hub)
            {
                var timestampColor = SkinV2Color.Parse(Design.Timestamp.Color);
                TimestampIcon = new Sprite
                {
                    Parent = this,
                    Alignment = Alignment.TopLeft,
                    Image = textures.Timestamp,
                    Size = new ScalableVector2(Design.Timestamp.IconSize, Design.Timestamp.IconSize),
                    Tint = timestampColor,
                    UsePreviousSpriteBatchOptions = true
                };
                Timestamp = new SpriteTextPlus(FontManager.GetWobbleFont(Design.Style.SecondaryFont), "",
                    Design.Timestamp.FontSize)
                {
                    Parent = this,
                    Alignment = Alignment.TopLeft,
                    Tint = timestampColor,
                    UsePreviousSpriteBatchOptions = true
                };
            }

            Clicked += OnClicked;
            SizeChanged += OnSizeChanged;
        }

        public override void Destroy()
        {
            Clicked -= OnClicked;
            SizeChanged -= OnSizeChanged;
            ClearContentBinding();
            base.Destroy();
        }

        public override void DrawToSpriteBatch()
        {
            if (Display == NotificationCardDisplay.Hub)
                return;

            base.DrawToSpriteBatch();
        }

        internal static NotificationCard Create(OnlineHubNotificationRowDesign config,
            OnlineHubMultiplayerInviteDesign inviteConfig, NotificationTextures textures,
            NotificationCardDisplay display, NotificationInfo notification)
        {
            if (notification is MultiplayerInviteNotificationInfo)
                return new MultiplayerInviteNotificationCard(config, inviteConfig, textures, display);

            return new StandardNotificationCard(config, textures, display);
        }

        internal void SetContent(NotificationInfo notification, DateTimeOffset receivedAt)
        {
            if (!Supports(notification))
                throw new ArgumentException("The notification is not supported by this card.", nameof(notification));

            var height = GetHeight();
            if (Math.Abs(Height - height) > 0.001f)
                Height = height;

            var color = GetLevelColor(notification.Level);
            Tint = Display == NotificationCardDisplay.Quick ? color : Color.Transparent;
            if (Accent != null)
                Accent.Tint = color;
            if (Timestamp != null)
                Timestamp.Text = receivedAt.ToLocalTime().ToString(Design.Timestamp.Format);

            ApplyContent(notification, color);
            IsClickable = IsInteractionEnabled && CanActivateFromCard;
            LayoutCard();
        }

        internal void SetInteractionEnabled(bool enabled)
        {
            IsInteractionEnabled = enabled;
            IsClickable = enabled && CanActivateFromCard;
            SetContentInteractionEnabled(enabled);
            if (!enabled)
                ResetInteractionState();
        }

        internal abstract bool Supports(NotificationInfo notification);

        internal virtual void ClearContentBinding() { }

        protected abstract float GetHeight();

        protected abstract void ApplyContent(NotificationInfo notification, Color color);

        protected abstract void LayoutContent(float bodyX);

        protected virtual void SetContentInteractionEnabled(bool enabled) { }

        protected void RaisePrimaryAction(EventArgs args) => PrimaryAction?.Invoke(this, args);

        protected void RaiseSecondaryAction(EventArgs args) => SecondaryAction?.Invoke(this, args);

        protected void LayoutTimestamp(float iconX, SpriteTextPlus firstLine)
        {
            var centerY = firstLine.Y + firstLine.CapTopOffset + firstLine.CapHeight / 2;
            TimestampIcon.Position = new ScalableVector2(iconX, centerY - Design.Timestamp.IconSize / 2);
            Timestamp.Position = new ScalableVector2(iconX + Design.Timestamp.IconSize + Design.Timestamp.Gap,
                centerY - Timestamp.CapTopOffset - Timestamp.CapHeight / 2);
        }

        protected static float GetFirstLineWidth(SpriteTextPlus text)
        {
            if (text.Children.Count > 0 && text.Children[0] is SpriteTextPlusLine firstLine)
                return firstLine.LayoutWidth;

            return 0;
        }

        private void LayoutCard()
        {
            var bodyX = LayoutBackground();
            LayoutContent(bodyX);
        }

        private float LayoutBackground()
        {
            if (Display == NotificationCardDisplay.Quick)
            {
                var innerWidth = Math.Max(0, Width - Config.BorderThickness * 2);
                var innerHeight = Math.Max(0, Height - Config.BorderThickness * 2);
                var radius = Math.Max(0, Design.Style.CornerRadius - Config.BorderThickness);
                SetRoundedSpriteSize(Background, innerWidth, innerHeight, radius);
                Background.Position = new ScalableVector2(Config.BorderThickness, Config.BorderThickness);
                return 0;
            }

            var bodyX = Config.AccentWidth + Config.AccentGap;
            SetRoundedSpriteSize(Accent, Config.AccentWidth, Height, Design.Style.CornerRadius);
            Accent.Position = new ScalableVector2(0, 0);
            SetRoundedSpriteSize(Background, Math.Max(0, Width - bodyX), Height, Design.Style.CornerRadius);
            Background.Position = new ScalableVector2(bodyX, 0);
            return bodyX;
        }

        private static void SetRoundedSpriteSize(Sprite sprite, float width, float height, float cornerRadius)
        {
            if (Math.Abs(sprite.Width - width) <= 0.001f && Math.Abs(sprite.Height - height) <= 0.001f)
                return;

            sprite.Size = new ScalableVector2(width, height);
            if (width > 0 && height > 0)
                sprite.Image = RoundedRectTextureCache.Get(width, height, cornerRadius);
        }

        private Color GetLevelColor(NotificationLevel level)
        {
            switch (level)
            {
                case NotificationLevel.Error:
                    return ErrorColor;
                case NotificationLevel.Warning:
                    return WarningColor;
                case NotificationLevel.Success:
                    return SuccessColor;
                default:
                    return InfoColor;
            }
        }

        private void OnClicked(object sender, EventArgs args)
        {
            if (CanActivateFromCard)
                RaisePrimaryAction(args);
        }

        private void OnSizeChanged(object sender, ScalableVector2 size) => LayoutCard();
    }
}
