using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using Quaver.Server.Client.Enums;
using Quaver.Server.Client.Structures;
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
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Input;
using Wobble.Managers;

namespace Quaver.Shared.Screens.V2.UI.OnlineHub.Users
{
    internal sealed class UserRow : RoundedButton
    {
        private static OnlineHubDesign Design => OnlineHubDesign.Default;

        private OnlineHubUserRowDesign Config { get; }

        private Drawable ClickableArea { get; }

        private OnlineHubAvatar Avatar { get; }

        private RoundedButton OnlineStatusBorder { get; }

        private RoundedButton OnlineStatus { get; }

        private FlexContainer InfoLayout { get; }

        private Container IdentityHost { get; }

        private Sprite Flag { get; }

        private SpriteTextPlus Clan { get; }

        private OnlineHubMarqueeLabel Username { get; }

        private OnlineHubMarqueeLabel Status { get; }

        private User User { get; set; }

        private ulong AvatarSteamId { get; set; }

        private bool SectionInteractionEnabled { get; set; }

        private bool MarqueeActive { get; set; }

        private Color LoadingTextColor { get; }

        private Color StatusTextColor { get; }

        internal event Action<User> Selected;

        internal UserRow(Drawable clickableArea, OnlineHubUserRowDesign config)
        {
            ClickableArea = clickableArea;
            Config = config;
            LoadingTextColor = SkinV2Color.Parse(Design.Style.TextColor);
            StatusTextColor = SkinV2Color.Parse(Config.StatusTextColor);
            Tint = SkinV2Color.Parse(Design.Style.ControlColor);
            CornerRadius = Design.Style.CornerRadius;
            PerformHoverFade = true;
            SetChildrenAlpha = false;
            Visible = false;
            IsInteractionEnabled = false;

            Avatar = new OnlineHubAvatar(Config.AvatarSize, Design.Style.CornerRadius, UserInterface.UnknownAvatar)
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                Alpha = 0,
                UsePreviousSpriteBatchOptions = true
            };
            InfoLayout = new FlexContainer
            {
                Parent = this,
                Direction = FlexDirection.Column,
                JustifyContent = FlexJustifyContent.Center,
                AlignItems = FlexAlignItems.Stretch
            };
            IdentityHost = new Container { Parent = InfoLayout };
            InfoLayout.SetItemOptions(IdentityHost, new FlexItemOptions
            {
                Basis = Config.IdentityHeight,
                Shrink = 0
            });
            Flag = new Sprite
            {
                Parent = IdentityHost,
                Alignment = Alignment.MidLeft,
                Size = new ScalableVector2(Config.FlagSize, Config.FlagSize),
                Region = Flags.GetRegion("XX"),
                UsePreviousSpriteBatchOptions = true
            };
            Clan = new SpriteTextPlus(FontManager.GetWobbleFont(Design.Style.Font), "",
                Design.Style.FontSize)
            {
                Parent = IdentityHost,
                Alignment = Alignment.MidLeft,
                Visible = false,
                UsePreviousSpriteBatchOptions = true
            };
            Username = new OnlineHubMarqueeLabel(FontManager.GetWobbleFont(Design.Style.Font),
                Design.Style.FontSize)
            {
                Parent = IdentityHost,
                Alignment = Alignment.MidLeft
            };
            Status = new OnlineHubMarqueeLabel(FontManager.GetWobbleFont(Design.Style.SecondaryFont), Design.Style.SmallFontSize)
            {
                Parent = InfoLayout
            };
            InfoLayout.SetItemOptions(Status, new FlexItemOptions { Basis = Config.StatusHeight, Shrink = 0 });

            OnlineStatusBorder = CreateStatusDot(Config.OnlineStatusBorderSize,
                SkinV2Color.Parse(Config.OnlineStatusBorderColor));
            OnlineStatus = CreateStatusDot(Config.OnlineStatusSize,
                SkinV2Color.Parse(Config.OnlineStatusColor));

            Clicked += OnSelected;
            RightClicked += OnSelected;
            SizeChanged += OnSizeChanged;
            Size = new ScalableVector2(1, Config.Height);
        }

        public override void Destroy()
        {
            Clicked -= OnSelected;
            RightClicked -= OnSelected;
            SizeChanged -= OnSizeChanged;
            base.Destroy();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            SetMarqueeActive(IsHovered);
        }

        protected override bool IsMouseInClickArea()
        {
            var visibleArea = RectangleF.Intersection(ScreenRectangle, ClickableArea.ScreenRectangle);
            return GraphicsHelper.RectangleContains(visibleArea, MouseManager.CurrentState.Position);
        }

        internal void Bind(User user, float width, float y, bool force)
        {
            if (Math.Abs(Width - width) > 0.001f)
                Width = width;

            Y = y;
            Visible = true;
            IsInteractionEnabled = SectionInteractionEnabled;
            var sameUser = User?.OnlineUser?.Id == user?.OnlineUser?.Id;
            User = user;
            if (!force && sameUser)
            {
                SetAvatar((ulong) (User?.OnlineUser?.SteamId ?? 0));
                return;
            }

            RefreshContent();
        }

        internal void ClearBinding()
        {
            User = null;
            AvatarSteamId = 0;
            Alpha = 1;
            Visible = false;
            IsInteractionEnabled = false;
            ResetInteractionState();
            SetMarqueeActive(false);
        }

        internal void SetSectionInteractionEnabled(bool enabled)
        {
            SectionInteractionEnabled = enabled;
            IsInteractionEnabled = enabled && Visible;
            if (!IsInteractionEnabled)
            {
                ResetInteractionState();
                SetMarqueeActive(false);
            }
        }

        internal int GetUserId() => User?.OnlineUser?.Id ?? -1;

        internal ulong GetAvatarSteamId() => AvatarSteamId;

        internal bool NeedsUserInfo() => User?.OnlineUser != null && !User.HasUserInfo;

        internal void RefreshStatus()
        {
            if (User != null)
                Status.SetText(GetStatusText(User), StatusTextColor);
        }

        internal void ApplyAvatar(ulong steamId, Microsoft.Xna.Framework.Graphics.Texture2D texture)
        {
            if (steamId == 0 || steamId != AvatarSteamId || texture == null || texture.IsDisposed)
                return;

            Avatar.ClearAnimations();
            Avatar.Alpha = 0;
            Avatar.SetSource(texture);
            Avatar.FadeTo(1, Easing.Linear, 200);
        }

        private void SetMarqueeActive(bool active)
        {
            if (MarqueeActive == active)
                return;

            MarqueeActive = active;
            Username.SetMarqueeActive(active);
            Status.SetMarqueeActive(active);
        }

        private void RefreshContent()
        {
            var onlineUser = User?.OnlineUser;
            if (onlineUser == null)
            {
                ClearBinding();
                return;
            }

            if (!User.HasUserInfo)
            {
                Flag.Region = Flags.GetRegion("XX");
                Clan.Text = "";
                Clan.Visible = false;
                Username.SetText(LocalizationManager.Get("Screen_OnlineHub_UserLoading", onlineUser.Id),
                    LoadingTextColor);
            }
            else
            {
                var usernameColor = Colors.GetUserChatColor(onlineUser.UserGroups);
                Flag.Region = Flags.GetRegion(onlineUser.CountryFlag ?? "XX");
                Clan.Text = string.IsNullOrEmpty(onlineUser.ClanTag) ? "" : $"[{onlineUser.ClanTag}]";
                Clan.Visible = Clan.Text.Length > 0;
                Clan.Tint = GetClanColor(User, usernameColor);
                Username.SetText(onlineUser.Username ?? "", usernameColor);
            }

            RefreshStatus();
            SetAvatar((ulong) onlineUser.SteamId);
            LayoutContent();
        }

        private void SetAvatar(ulong steamId)
        {
            AvatarSteamId = steamId;
            Avatar.ClearAnimations();
            if (steamId != 0 && SteamManager.UserAvatars != null &&
                SteamManager.UserAvatars.TryGetValue(steamId, out var texture) && texture != null &&
                !texture.IsDisposed)
            {
                Avatar.SetSource(texture);
                Avatar.Alpha = 1;
                return;
            }

            Avatar.SetSource(UserInterface.UnknownAvatar);
            Avatar.Alpha = 0;
            if (steamId != 0)
                SteamManager.SendAvatarRetrievalRequest(steamId);
        }

        private RoundedButton CreateStatusDot(float size, Color color) => new RoundedButton
        {
            Parent = this,
            Size = new ScalableVector2(size, size),
            Tint = color,
            IsClickable = false,
            IsInteractionEnabled = false,
            UsePreviousSpriteBatchOptions = true
        };

        private void LayoutContent()
        {
            var infoX = Config.AvatarSize + Config.ContentGap;
            InfoLayout.Position = new ScalableVector2(infoX, 0);
            InfoLayout.Size = new ScalableVector2(Math.Max(0, Width - infoX - Config.ContentGap), Height);
            InfoLayout.RefreshLayout();

            Flag.X = 0;
            var identityX = Flag.Width + Config.IdentityGap;
            Clan.X = identityX;
            if (Clan.Visible)
                identityX += Clan.Width + Config.IdentityGap;

            Username.X = identityX;
            Username.Size = new ScalableVector2(Math.Max(0, IdentityHost.Width - identityX),
                Config.IdentityHeight);

            var borderX = Math.Max(0, Config.AvatarSize - Config.OnlineStatusBorderSize);
            var borderY = Math.Max(0, Config.Height - Config.OnlineStatusBorderSize);
            OnlineStatusBorder.Position = new ScalableVector2(borderX, borderY);
            OnlineStatus.Position = new ScalableVector2(
                borderX + (Config.OnlineStatusBorderSize - Config.OnlineStatusSize) / 2,
                borderY + (Config.OnlineStatusBorderSize - Config.OnlineStatusSize) / 2);
        }

        private void OnSelected(object sender, EventArgs args)
        {
            if (User != null)
                Selected?.Invoke(User);
        }

        private void OnSizeChanged(object sender, ScalableVector2 size) => LayoutContent();

        private static Color GetClanColor(User user, Color fallback)
        {
            var accent = user.OnlineUser.ClanAccentColor;
            if (string.IsNullOrEmpty(accent))
                return fallback;

            try
            {
                return Quaver.Shared.Helpers.ColorHelper.HexToColor(accent);
            }
            catch
            {
                return fallback;
            }
        }

        private static string GetStatusText(User user)
        {
            var status = user.CurrentStatus;
            if (status == null)
                return LocalizationManager.Get("Screen_OnlineHub_UserStatusIdle");

            switch (status.Status)
            {
                case ClientStatus.Selecting:
                    return LocalizationManager.Get("Screen_OnlineHub_UserStatusSelecting");
                case ClientStatus.Playing:
                    return LocalizationManager.Get("Screen_OnlineHub_UserStatusPlaying", status.Content);
                case ClientStatus.Paused:
                    return LocalizationManager.Get("Screen_OnlineHub_UserStatusPaused");
                case ClientStatus.Watching:
                    return LocalizationManager.Get("Screen_OnlineHub_UserStatusWatching", status.Content);
                case ClientStatus.Editing:
                    return LocalizationManager.Get("Screen_OnlineHub_UserStatusEditing", status.Content);
                case ClientStatus.InLobby:
                    return LocalizationManager.Get("Screen_OnlineHub_UserStatusInLobby");
                case ClientStatus.Multiplayer:
                    return LocalizationManager.Get("Screen_OnlineHub_UserStatusMultiplayer");
                case ClientStatus.Listening:
                    return LocalizationManager.Get("Screen_OnlineHub_UserStatusListening", status.Content);
                default:
                    return LocalizationManager.Get("Screen_OnlineHub_UserStatusIdle");
            }
        }
    }
}
