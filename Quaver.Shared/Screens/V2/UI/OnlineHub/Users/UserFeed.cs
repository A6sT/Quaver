using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Server.Client.Structures;
using Quaver.Shared.Online;
using Quaver.Shared.Screens.V2.UI.OnlineHub;
using Quaver.Shared.Skinning.V2;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;

namespace Quaver.Shared.Screens.V2.UI.OnlineHub.Users
{
    internal sealed class UserFeed : ScrollContainer
    {
        private const double StatusRequestInterval = 2000;

        private static OnlineHubDesign Design => OnlineHubDesign.Default;

        private OnlineHubFeedDesign Config { get; }

        private OnlineHubUserRowDesign RowConfig { get; }

        private Action<User> UserSelected { get; }

        private List<UserRow> Rows { get; } = new List<UserRow>();

        private HashSet<int> RequestedUserInfo { get; } = new HashSet<int>();

        private List<int> VisibleUserIds { get; } = new List<int>();

        private Dictionary<ulong, Texture2D> PendingAvatars { get; } = new Dictionary<ulong, Texture2D>();

        private FlexContainer EmptyState { get; }

        private SpriteTextPlus EmptyStateTitle { get; }

        private SpriteTextPlus EmptyStateDescription { get; }

        private User[] Items { get; set; } = Array.Empty<User>();

        private int FirstVisibleIndex { get; set; } = -1;

        private bool IsActive { get; set; }

        private double TimeSinceStatusRequest { get; set; }

        private bool AvatarRefreshScheduled { get; set; }

        internal UserFeed(OnlineHubFeedDesign config,
            OnlineHubUserRowDesign rowConfig, Action<User> userSelected)
            : base(new ScalableVector2(1, 1), new ScalableVector2(1, 1))
        {
            Config = config;
            RowConfig = rowConfig;
            UserSelected = userSelected;
            Tint = Color.Transparent;
            InputEnabled = false;
            CapturesMouseWheelInput = true;
            AllowScrollbarDragging = true;
            ScrollSpeed = Config.ScrollSpeed;
            EasingType = Easing.OutQuint;
            TimeToCompleteScroll = 250;
            Scrollbar.Width = Config.ScrollbarWidth;
            Scrollbar.Tint = SkinV2Color.Parse(Design.Style.TextColor);

            EmptyState = new FlexContainer
            {
                Parent = this,
                Direction = FlexDirection.Column,
                JustifyContent = FlexJustifyContent.Center,
                AlignItems = FlexAlignItems.Center,
                RowGap = Config.EmptyStateGap,
                UsePreviousSpriteBatchOptions = true
            };
            EmptyStateTitle = new SpriteTextPlus(FontManager.GetWobbleFont(Design.Style.Font), "",
                Config.EmptyStateTitleFontSize)
            {
                Parent = EmptyState,
                TextAlignment = TextAlignment.Center,
                Tint = SkinV2Color.Parse(Design.Style.TextColor),
                UsePreviousSpriteBatchOptions = true
            };
            EmptyStateDescription = new SpriteTextPlus(FontManager.GetWobbleFont(Design.Style.SecondaryFont), "",
                Config.EmptyStateDescriptionFontSize)
            {
                Parent = EmptyState,
                TextAlignment = TextAlignment.Center,
                Tint = SkinV2Color.Parse(Design.Style.TextColor),
                UsePreviousSpriteBatchOptions = true
            };
            SizeChanged += OnSizeChanged;
        }

        public override void Destroy()
        {
            SetActive(false);
            SizeChanged -= OnSizeChanged;
            foreach (var row in Rows)
                row.Selected -= UserSelected;
            base.Destroy();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            RefreshVisibleRows(false, false);
            if (!IsActive || Items.Length == 0)
                return;

            TimeSinceStatusRequest += gameTime.ElapsedGameTime.TotalMilliseconds;
            if (TimeSinceStatusRequest < StatusRequestInterval)
                return;

            TimeSinceStatusRequest = 0;
            RequestVisibleStatuses();
        }

        internal void SetActive(bool active)
        {
            if (IsActive == active)
                return;

            IsActive = active;
            InputEnabled = active;
            foreach (var row in Rows)
                row.SetSectionInteractionEnabled(active);

            if (active)
            {
                SteamManager.SteamUserAvatarLoaded += OnSteamAvatarLoaded;
                RefreshVisibleRows(true, false);
                return;
            }

            SteamManager.SteamUserAvatarLoaded -= OnSteamAvatarLoaded;
            RequestedUserInfo.Clear();
            VisibleUserIds.Clear();
            lock (PendingAvatars)
            {
                PendingAvatars.Clear();
                AvatarRefreshScheduled = false;
            }
            TimeSinceStatusRequest = 0;
        }

        internal void SetItems(User[] users, bool preserveAnchor, string emptyStateTitle,
            string emptyStateDescription)
        {
            var anchor = preserveAnchor ? GetScrollAnchor() : null;
            Items = users ?? Array.Empty<User>();
            EmptyStateTitle.Text = emptyStateTitle;
            EmptyStateDescription.Text = emptyStateDescription;
            EmptyState.Visible = Items.Length == 0;
            EmptyState.RefreshLayout();
            UpdateContentHeight();

            if (!preserveAnchor)
                SetScrollPosition(0);
            else if (anchor.HasValue)
                RestoreScrollAnchor(anchor.Value);

            RefreshVisibleRows(true, true);
        }

        internal void RefreshVisibleStatuses()
        {
            foreach (var row in Rows)
                row.RefreshStatus();
        }

        private void OnSizeChanged(object sender, ScalableVector2 size)
        {
            ContentContainer.Width = size.X.Value;
            EmptyState.Size = size;
            var emptyStateWidth = Math.Max(0, size.X.Value - SkinV2Spacing.SpacingBase * 2);
            EmptyStateTitle.MaxWidth = emptyStateWidth;
            EmptyStateDescription.MaxWidth = emptyStateWidth;
            EmptyState.RefreshLayout();
            EnsurePoolSize();
            UpdateContentHeight();
            RefreshVisibleRows(true, false);
        }

        private void EnsurePoolSize()
        {
            var required = Math.Max(1, (int) Math.Ceiling(Height / GetStride()) + Config.OverscanRows);
            while (Rows.Count < required)
            {
                var row = new UserRow(this, RowConfig);
                row.Selected += UserSelected;
                row.SetSectionInteractionEnabled(IsActive);
                AddContainedDrawable(row);
                Rows.Add(row);
            }
        }

        private void UpdateContentHeight()
        {
            var itemsHeight = Items.Length == 0 ? 0 : Items.Length * GetStride() - Config.RowGap;
            ContentContainer.Height = Math.Max(Height, itemsHeight);
            var minimumY = Math.Min(0, Height - ContentContainer.Height);
            TargetY = MathHelper.Clamp(TargetY, minimumY, 0);
            PreviousTargetY = TargetY;
            ContentContainer.Y = MathHelper.Clamp(ContentContainer.Y, minimumY, 0);
            FirstVisibleIndex = -1;
        }

        private void RefreshVisibleRows(bool force, bool refreshContent)
        {
            if (Rows.Count == 0)
                return;

            var first = Math.Max(0, (int) Math.Floor(-ContentContainer.Y / GetStride()) -
                                    Config.OverscanRows / 2);
            if (!force && FirstVisibleIndex == first)
                return;

            FirstVisibleIndex = first;
            var width = Math.Max(0, Width - Config.ScrollbarWidth - Config.RowGap);
            for (var index = 0; index < Rows.Count; index++)
            {
                var itemIndex = first + index;
                if (itemIndex >= Items.Length)
                {
                    Rows[index].ClearBinding();
                    continue;
                }

                Rows[index].Bind(Items[itemIndex], width, itemIndex * GetStride(), refreshContent);
            }

            RequestVisibleUserInfo();
        }

        private void RequestVisibleUserInfo()
        {
            VisibleUserIds.Clear();
            foreach (var row in Rows)
            {
                var userId = row.GetUserId();
                if (userId >= 0 && row.NeedsUserInfo() && RequestedUserInfo.Add(userId))
                    VisibleUserIds.Add(userId);
            }

            if (VisibleUserIds.Count > 0)
                OnlineManager.Client?.RequestUserInfo(VisibleUserIds);
        }

        private void RequestVisibleStatuses()
        {
            VisibleUserIds.Clear();
            foreach (var row in Rows)
            {
                var userId = row.GetUserId();
                if (userId >= 0 && !row.NeedsUserInfo())
                    VisibleUserIds.Add(userId);
            }

            if (VisibleUserIds.Count > 0)
                OnlineManager.Client?.RequestUserStatuses(VisibleUserIds);
        }

        private (int UserId, float Offset)? GetScrollAnchor()
        {
            if (Items.Length == 0)
                return null;

            var viewportTop = Math.Max(0, -ContentContainer.Y);
            var index = Math.Min(Items.Length - 1, (int) Math.Floor(viewportTop / GetStride()));
            var userId = Items[index]?.OnlineUser?.Id ?? -1;
            if (userId < 0)
                return null;

            return (userId, viewportTop - index * GetStride());
        }

        private void RestoreScrollAnchor((int UserId, float Offset) anchor)
        {
            for (var index = 0; index < Items.Length; index++)
            {
                if (Items[index]?.OnlineUser?.Id != anchor.UserId)
                    continue;

                SetScrollPosition(-(index * GetStride() + anchor.Offset));
                return;
            }
        }

        private void SetScrollPosition(float y)
        {
            var minimumY = Math.Min(0, Height - ContentContainer.Height);
            y = MathHelper.Clamp(y, minimumY, 0);
            TargetY = y;
            PreviousTargetY = y;
            ContentContainer.Y = y;
            FirstVisibleIndex = -1;
        }

        private float GetStride() => RowConfig.Height + Config.RowGap;

        private void OnSteamAvatarLoaded(object sender, SteamAvatarLoadedEventArgs args)
        {
            if (args.SteamId == 0 || args.Texture == null || args.Texture.IsDisposed)
                return;

            var scheduleRefresh = false;
            lock (PendingAvatars)
            {
                PendingAvatars[args.SteamId] = args.Texture;
                if (!AvatarRefreshScheduled)
                {
                    AvatarRefreshScheduled = true;
                    scheduleRefresh = true;
                }
            }

            if (scheduleRefresh)
                AddScheduledUpdate(ApplyPendingAvatars);
        }

        private void ApplyPendingAvatars()
        {
            lock (PendingAvatars)
            {
                if (!IsActive)
                {
                    PendingAvatars.Clear();
                    AvatarRefreshScheduled = false;
                    return;
                }

                foreach (var row in Rows)
                {
                    var steamId = row.GetAvatarSteamId();
                    if (PendingAvatars.TryGetValue(steamId, out var texture))
                        row.ApplyAvatar(steamId, texture);
                }

                PendingAvatars.Clear();
                AvatarRefreshScheduled = false;
            }
        }
    }
}
