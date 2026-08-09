using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Quaver.Server.Client.Objects.Twitch;
using Quaver.Shared.Skinning.V2;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;

namespace Quaver.Shared.Screens.V2.UI.OnlineHub.SongRequests
{
    internal readonly struct SongRequestFeedItem
    {
        internal SongRequest Request { get; }

        internal DateTimeOffset ReceivedAt { get; }

        internal bool IsPlayed { get; }

        internal SongRequestFeedItem(SongRequest request, DateTimeOffset receivedAt, bool isPlayed)
        {
            Request = request;
            ReceivedAt = receivedAt;
            IsPlayed = isPlayed;
        }
    }

    internal sealed class SongRequestFeed : ScrollContainer
    {
        private static OnlineHubDesign Design => OnlineHubDesign.Default;

        private OnlineHubFeedDesign Config { get; }

        private OnlineHubSongRequestRowDesign RowConfig { get; }

        private Action<SongRequest> ShowActions { get; }

        private List<SongRequestRow> Rows { get; } = new List<SongRequestRow>();

        private FlexContainer EmptyState { get; }

        private SpriteTextPlus EmptyStateTitle { get; }

        private SpriteTextPlus EmptyStateDescription { get; }

        private SongRequestFeedItem[] Items { get; set; } = Array.Empty<SongRequestFeedItem>();

        private int FirstVisibleIndex { get; set; } = -1;

        private bool IsActive { get; set; }

        internal SongRequestFeed(OnlineHubFeedDesign config, OnlineHubSongRequestRowDesign rowConfig,
            Action<SongRequest> showActions)
            : base(new ScalableVector2(1, 1), new ScalableVector2(1, 1))
        {
            Config = config;
            RowConfig = rowConfig;
            ShowActions = showActions;
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
            EmptyStateDescription = new SpriteTextPlus(
                FontManager.GetWobbleFont(Design.Style.SecondaryFont), "",
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
            SizeChanged -= OnSizeChanged;
            base.Destroy();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            RefreshVisibleRows(false);
        }

        internal void SetActive(bool active)
        {
            IsActive = active;
            InputEnabled = active;
            foreach (var row in Rows)
                row.SetSectionInteractionEnabled(active);
        }

        internal void SetItems(SongRequestFeedItem[] items, string emptyStateTitle, string emptyStateDescription)
        {
            Items = items ?? Array.Empty<SongRequestFeedItem>();
            EmptyStateTitle.Text = emptyStateTitle;
            EmptyStateDescription.Text = emptyStateDescription;
            EmptyState.Visible = Items.Length == 0;
            EmptyState.RefreshLayout();
            UpdateContentHeight();
            RefreshVisibleRows(true);
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
            RefreshVisibleRows(true);
        }

        private void EnsurePoolSize()
        {
            var required = Math.Max(1, (int) Math.Ceiling(Height / GetStride()) + Config.OverscanRows);
            while (Rows.Count < required)
            {
                var row = new SongRequestRow(RowConfig, ShowActions);
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

        private void RefreshVisibleRows(bool force)
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

                Rows[index].Bind(Items[itemIndex], width, itemIndex * GetStride());
            }
        }

        private float GetStride() => RowConfig.Height + Config.RowGap;
    }
}
