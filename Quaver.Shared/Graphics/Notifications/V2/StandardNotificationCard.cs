using System;
using Microsoft.Xna.Framework;
using Quaver.Shared.Screens.V2.UI.OnlineHub;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;

namespace Quaver.Shared.Graphics.Notifications.V2
{
    internal sealed class StandardNotificationCard : NotificationCard
    {
        private NotificationTextures Textures { get; }

        private Sprite NotificationIcon { get; }

        private SpriteTextPlus Title { get; }

        private SpriteTextPlus Description { get; }

        protected override bool CanActivateFromCard => true;

        internal StandardNotificationCard(OnlineHubNotificationRowDesign config,
            NotificationTextures textures, NotificationCardDisplay display) : base(config, textures, display)
        {
            Textures = textures;
            NotificationIcon = new Sprite
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                Size = new ScalableVector2(Config.IconSize, Config.IconSize),
                UsePreviousSpriteBatchOptions = true
            };
            Title = new SpriteTextPlus(FontManager.GetWobbleFont(Design.Style.Font), "", Design.Style.FontSize)
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                Tint = TextColor,
                UsePreviousSpriteBatchOptions = true
            };

            if (Display == NotificationCardDisplay.Hub)
            {
                Description = new SpriteTextPlus(FontManager.GetWobbleFont(Design.Style.SecondaryFont), "",
                    Design.Style.DetailFontSize)
                {
                    Parent = this,
                    Alignment = Alignment.TopLeft,
                    Tint = TextColor,
                    Visible = false,
                    UsePreviousSpriteBatchOptions = true
                };
            }
        }

        internal override bool Supports(NotificationInfo notification) => !(notification is MultiplayerInviteNotificationInfo);

        protected override float GetHeight() => Config.Height;

        protected override void ApplyContent(NotificationInfo notification, Color color)
        {
            NotificationIcon.Image = Textures.GetIcon(notification.Level);
            NotificationIcon.Tint = color;
            SetText(notification.Text);
        }

        protected override void LayoutContent(float bodyX)
        {
            var mediaX = bodyX + Config.Padding;
            NotificationIcon.Position = new ScalableVector2(mediaX, 0);
            var contentX = mediaX + Config.IconSize + Config.ContentGap;
            var contentRight = Math.Max(contentX, Width - Config.Padding);
            var timestampWidth = 0f;
            if (Timestamp != null)
                timestampWidth = Design.Timestamp.IconSize + Design.Timestamp.Gap * 2 + Timestamp.Width;

            Title.MaxWidth = Math.Max(0, contentRight - contentX - timestampWidth);
            var titleY = Config.Padding;
            if (Display == NotificationCardDisplay.Quick)
                titleY = Math.Max(0, (Height - Title.Height) / 2);
            Title.Position = new ScalableVector2(contentX, titleY);

            if (Timestamp == null)
                return;

            var firstLineWidth = GetFirstLineWidth(Title);
            LayoutTimestamp(contentX + firstLineWidth + Design.Timestamp.Gap, Title);
            Description.Position = new ScalableVector2(contentX, Title.Y + Title.Height);
            Description.MaxWidth = Math.Max(0, contentRight - contentX);
        }

        private void SetText(string text)
        {
            text ??= "";
            var lineBreak = text.IndexOf('\n');
            Title.Text = lineBreak < 0 ? text : text.Substring(0, lineBreak).TrimEnd('\r');
            if (Description == null)
                return;

            Description.Text = lineBreak < 0 ? "" : text.Substring(lineBreak + 1).TrimStart('\r', '\n');
            Description.Visible = Description.Text.Length > 0;
        }
    }
}
