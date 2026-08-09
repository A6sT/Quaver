using System;
using Microsoft.Xna.Framework;
using Quaver.Shared.Assets;
using Quaver.Shared.Graphics;
using Quaver.Shared.Helpers;
using Quaver.Shared.Online;
using Quaver.Shared.Screens.V2.UI.OnlineHub;
using Quaver.Shared.Screens.V2.UI.OnlineHub.Shared;
using Quaver.Shared.Skinning.V2;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Buttons;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;

namespace Quaver.Shared.Graphics.Notifications.V2
{
    internal sealed class MultiplayerInviteNotificationCard : NotificationCard
    {
        private OnlineHubMultiplayerInviteDesign InviteConfig { get; }

        private OnlineHubAvatar Avatar { get; }

        private SpriteTextPlus QuickTitle { get; }

        private SpriteTextPlus Clan { get; }

        private SpriteTextPlus Username { get; }

        private SpriteTextPlus Description { get; }

        private RoundedButton JoinButton { get; }

        private RoundedButton DeclineButton { get; }

        private ulong AvatarSteamId { get; set; }

        private bool IsAvatarSubscribed { get; set; }

        private bool IsAvatarReady { get; set; }

        protected override bool CanActivateFromCard => Display == NotificationCardDisplay.Quick;

        internal override bool IsReadyToDisplay => Display != NotificationCardDisplay.Quick || IsAvatarReady;

        internal MultiplayerInviteNotificationCard(OnlineHubNotificationRowDesign config,
            OnlineHubMultiplayerInviteDesign inviteConfig, NotificationTextures textures,
            NotificationCardDisplay display) : base(config, textures, display)
        {
            InviteConfig = inviteConfig;
            var avatarSize = Display == NotificationCardDisplay.Hub ? InviteConfig.AvatarSize : Config.IconSize;
            Avatar = new OnlineHubAvatar(avatarSize, Design.Style.CornerRadius, UserInterface.UnknownAvatar)
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                UsePreviousSpriteBatchOptions = true
            };

            if (Display == NotificationCardDisplay.Quick)
            {
                QuickTitle = new SpriteTextPlus(FontManager.GetWobbleFont(Design.Style.Font), "",
                    Design.Style.FontSize)
                {
                    Parent = this,
                    Alignment = Alignment.TopLeft,
                    UsePreviousSpriteBatchOptions = true
                };
                return;
            }

            Clan = new SpriteTextPlus(FontManager.GetWobbleFont(Design.Style.Font), "", Design.Style.FontSize)
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                UsePreviousSpriteBatchOptions = true
            };
            Username = new SpriteTextPlus(FontManager.GetWobbleFont(Design.Style.Font), "", Design.Style.FontSize)
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                UsePreviousSpriteBatchOptions = true
            };
            Description = new SpriteTextPlus(FontManager.GetWobbleFont(Design.Style.SecondaryFont),
                LocalizationManager.Get("Screen_OnlineHub_MultiplayerInviteDescription"), Design.Style.DetailFontSize)
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                Tint = TextColor,
                UsePreviousSpriteBatchOptions = true
            };
            JoinButton = CreateButton("Screen_OnlineHub_Join", OnJoinClicked);
            DeclineButton = CreateButton("Screen_OnlineHub_Decline", OnDeclineClicked);
        }

        internal override bool Supports(NotificationInfo notification) => notification is MultiplayerInviteNotificationInfo;

        internal override void ClearContentBinding()
        {
            AvatarSteamId = 0;
            IsAvatarReady = false;
            Avatar.ClearAnimations();
            UnsubscribeAvatarUpdates();
        }

        protected override float GetHeight()
        {
            if (Display == NotificationCardDisplay.Hub)
                return InviteConfig.Height;

            return Config.Height;
        }

        protected override void ApplyContent(NotificationInfo notification, Color color)
        {
            var invite = (MultiplayerInviteNotificationInfo) notification;
            if (SetAvatar(invite.SenderSteamId))
                UnsubscribeAvatarUpdates();
            else
            {
                SubscribeAvatarUpdates();
                SteamManager.SendAvatarRetrievalRequest(invite.SenderSteamId);
            }

            if (Display == NotificationCardDisplay.Quick)
            {
                QuickTitle.Text = $"{invite.SenderName} invited you to a multiplayer game.";
                QuickTitle.Tint = Colors.GetUserChatColor(invite.SenderGroups);
                return;
            }

            Clan.Text = string.IsNullOrEmpty(invite.SenderClanTag) ? "" : $"[{invite.SenderClanTag}]";
            Clan.Tint = GetClanColor(invite);
            Clan.Visible = Clan.Text.Length > 0;
            Username.Text = invite.SenderName;
            Username.Tint = Colors.GetUserChatColor(invite.SenderGroups);
        }

        protected override void LayoutContent(float bodyX)
        {
            if (Display == NotificationCardDisplay.Quick)
            {
                LayoutQuick(bodyX);
                return;
            }

            LayoutHub(bodyX);
        }

        protected override void SetContentInteractionEnabled(bool enabled)
        {
            if (JoinButton == null)
                return;

            JoinButton.Visible = enabled;
            JoinButton.IsInteractionEnabled = enabled;
            DeclineButton.Visible = enabled;
            DeclineButton.IsInteractionEnabled = enabled;
            if (enabled)
                return;

            JoinButton.ResetInteractionState();
            DeclineButton.ResetInteractionState();
        }

        private void LayoutQuick(float bodyX)
        {
            var mediaX = bodyX + Config.Padding;
            Avatar.Alignment = Alignment.MidLeft;
            Avatar.Position = new ScalableVector2(mediaX, 0);
            var contentX = mediaX + Config.IconSize + Config.ContentGap;
            var contentRight = Math.Max(contentX, Width - Config.Padding);
            QuickTitle.MaxWidth = Math.Max(0, contentRight - contentX);
            QuickTitle.Position = new ScalableVector2(contentX, Math.Max(0, (Height - QuickTitle.Height) / 2));
        }

        private void LayoutHub(float bodyX)
        {
            var mediaX = bodyX + Config.Padding;
            var contentX = mediaX + InviteConfig.AvatarSize + Config.ContentGap;
            var contentRight = Math.Max(contentX, Width - Config.Padding);
            Avatar.Alignment = Alignment.TopLeft;
            Avatar.Position = new ScalableVector2(mediaX, Config.Padding);

            var timestampWidth = Design.Timestamp.IconSize + Design.Timestamp.Gap * 2 + Timestamp.Width;
            Clan.Position = new ScalableVector2(contentX, Config.Padding);
            Clan.MaxWidth = Math.Max(0, contentRight - contentX - timestampWidth);
            var clanWidth = GetFirstLineWidth(Clan);
            var usernameX = Clan.Visible ? Clan.X + clanWidth + InviteConfig.TitleGap : contentX;
            Username.Position = new ScalableVector2(usernameX, Config.Padding);
            Username.MaxWidth = Math.Max(0, contentRight - usernameX - timestampWidth);

            var usernameWidth = GetFirstLineWidth(Username);
            LayoutTimestamp(usernameX + usernameWidth + Design.Timestamp.Gap, Username);
            Description.Position = new ScalableVector2(contentX, InviteConfig.DescriptionTop);
            Description.MaxWidth = Math.Max(0, contentRight - contentX);

            var availableButtonWidth = Math.Max(0, contentRight - contentX);
            var buttonWidth = Math.Min(InviteConfig.ButtonWidth, Math.Max(0, (availableButtonWidth - InviteConfig.ButtonGap) / 2));
            SetButtonSize(JoinButton, buttonWidth);
            SetButtonSize(DeclineButton, buttonWidth);
            JoinButton.Position = new ScalableVector2(contentX, InviteConfig.ButtonTop);
            DeclineButton.Position = new ScalableVector2(contentX + buttonWidth + InviteConfig.ButtonGap, InviteConfig.ButtonTop);
        }

        private RoundedButton CreateButton(string localizationKey, EventHandler clickAction)
        {
            var button = new RoundedButton(clickAction)
            {
                Parent = this,
                Size = new ScalableVector2(InviteConfig.ButtonWidth, InviteConfig.ButtonHeight),
                CornerRadius = Design.Style.CornerRadius,
                Tint = SkinV2Color.Parse(Design.Style.SurfaceColor),
                PerformHoverFade = true,
                Visible = false,
                IsInteractionEnabled = false,
                UsePreviousSpriteBatchOptions = true
            };
            button.SetLabel(FontManager.GetWobbleFont(Design.Style.Font),
                LocalizationManager.Get(localizationKey), Design.Style.FontSize,
                SkinV2Color.Parse(Design.Style.TextColor));
            return button;
        }

        private void SetButtonSize(RoundedButton button, float width)
        {
            if (Math.Abs(button.Width - width) <= 0.001f &&
                Math.Abs(button.Height - InviteConfig.ButtonHeight) <= 0.001f)
                return;

            button.Size = new ScalableVector2(width, InviteConfig.ButtonHeight);
        }

        private bool SetAvatar(ulong steamId)
        {
            AvatarSteamId = steamId;
            IsAvatarReady = false;
            Avatar.ClearAnimations();
            Avatar.Alpha = 1;
            Avatar.SetSource(UserInterface.UnknownAvatar);
            if (steamId == 0 || SteamManager.UserAvatars == null)
            {
                IsAvatarReady = true;
                return true;
            }

            if (SteamManager.UserAvatars.TryGetValue(steamId, out var avatar) && avatar != null &&
                !avatar.IsDisposed)
            {
                Avatar.SetSource(avatar);
                IsAvatarReady = true;
                return true;
            }

            return false;
        }

        private static Color GetClanColor(MultiplayerInviteNotificationInfo invite)
        {
            var fallback = Colors.GetUserChatColor(invite.SenderGroups);
            if (string.IsNullOrEmpty(invite.SenderClanAccentColor))
                return fallback;

            try
            {
                return ColorHelper.HexToColor(invite.SenderClanAccentColor);
            }
            catch
            {
                return fallback;
            }
        }

        private void SubscribeAvatarUpdates()
        {
            if (IsAvatarSubscribed)
                return;

            SteamManager.SteamUserAvatarLoaded += OnSteamAvatarLoaded;
            IsAvatarSubscribed = true;
        }

        private void UnsubscribeAvatarUpdates()
        {
            if (!IsAvatarSubscribed)
                return;

            SteamManager.SteamUserAvatarLoaded -= OnSteamAvatarLoaded;
            IsAvatarSubscribed = false;
        }

        private void OnSteamAvatarLoaded(object sender, SteamAvatarLoadedEventArgs args)
        {
            if (args.SteamId != AvatarSteamId || args.Texture == null || args.Texture.IsDisposed)
                return;

            IsAvatarReady = true;
            Avatar.ClearAnimations();
            Avatar.SetSource(args.Texture);
            if (Display == NotificationCardDisplay.Quick)
                Avatar.Alpha = 1;
            else
            {
                Avatar.Alpha = 0;
                Avatar.FadeTo(1, Easing.Linear, 200);
            }

            UnsubscribeAvatarUpdates();
        }

        private void OnJoinClicked(object sender, EventArgs args) => RaisePrimaryAction(args);

        private void OnDeclineClicked(object sender, EventArgs args) => RaiseSecondaryAction(args);
    }
}
