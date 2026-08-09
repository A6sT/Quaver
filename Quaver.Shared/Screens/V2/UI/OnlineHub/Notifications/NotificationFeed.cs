using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Graphics.Notifications.V2;
using Quaver.Shared.Screens.V2.UI.OnlineHub;
using Quaver.Shared.Skinning;
using Quaver.Shared.Skinning.V2;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;

namespace Quaver.Shared.Screens.V2.UI.OnlineHub.Notifications
{
    internal sealed class NotificationFeed : ScrollContainer
    {
        private static OnlineHubDesign Design => OnlineHubDesign.Default;

        private OnlineHubFeedDesign Config { get; }

        private OnlineHubNotificationRowDesign RowConfig { get; }

        private OnlineHubMultiplayerInviteDesign InviteConfig { get; }

        private Action<long> Remove { get; }

        private List<NotificationCard> Cards { get; } = new List<NotificationCard>();

        private List<NotificationCard> VisibleCards { get; } = new List<NotificationCard>();

        private Stack<StandardNotificationCard> StandardPool { get; } =
            new Stack<StandardNotificationCard>();

        private Stack<MultiplayerInviteNotificationCard> InvitePool { get; } =
            new Stack<MultiplayerInviteNotificationCard>();

        private Dictionary<NotificationCard, NotificationHistoryEntry> BoundEntries { get; } =
            new Dictionary<NotificationCard, NotificationHistoryEntry>();

        private FlexContainer EmptyState { get; }

        private SpriteTextPlus EmptyStateTitle { get; }

        private SpriteTextPlus EmptyStateDescription { get; }

        private NotificationHistoryEntry[] Items { get; set; } = Array.Empty<NotificationHistoryEntry>();

        private float[] ItemOffsets { get; set; } = Array.Empty<float>();

        private float ItemsHeight { get; set; }

        private int FirstVisibleIndex { get; set; } = -1;

        private int VisibleCapacity { get; set; }

        internal NotificationFeed(OnlineHubFeedDesign config,
            OnlineHubNotificationRowDesign rowConfig,
            OnlineHubMultiplayerInviteDesign inviteConfig, Action<long> remove)
            : base(new ScalableVector2(1, 1), new ScalableVector2(1, 1))
        {
            Config = config;
            RowConfig = rowConfig;
            InviteConfig = inviteConfig;
            Remove = remove;
            Tint = Color.Transparent;
            InputEnabled = true;
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
            foreach (var card in Cards)
            {
                card.PrimaryAction -= OnPrimaryAction;
                card.SecondaryAction -= OnSecondaryAction;
            }

            base.Destroy();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            RefreshVisibleRows(false);
        }

        internal void SetItems(NotificationHistoryEntry[] items, string emptyStateTitle,
            string emptyStateDescription)
        {
            Items = items ?? Array.Empty<NotificationHistoryEntry>();
            RebuildItemOffsets();
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
            UpdateVisibleCapacity();
            UpdateContentHeight();
            RefreshVisibleRows(true);
        }

        private void UpdateVisibleCapacity()
        {
            var stride = Math.Min(RowConfig.Height, InviteConfig.Height) + Config.RowGap;
            VisibleCapacity = Math.Max(1, (int) Math.Ceiling(Height / stride) + Config.OverscanRows);
        }

        private void UpdateContentHeight()
        {
            ContentContainer.Height = Math.Max(Height, ItemsHeight);
            var minimumY = Math.Min(0, Height - ContentContainer.Height);
            TargetY = MathHelper.Clamp(TargetY, minimumY, 0);
            PreviousTargetY = TargetY;
            ContentContainer.Y = MathHelper.Clamp(ContentContainer.Y, minimumY, 0);
            FirstVisibleIndex = -1;
        }

        private void RefreshVisibleRows(bool force)
        {
            var first = Math.Max(0, FindFirstVisibleIndex(-ContentContainer.Y) - Config.OverscanRows / 2);
            if (!force && FirstVisibleIndex == first)
                return;

            FirstVisibleIndex = first;
            ReleaseVisibleCards();
            var width = Math.Max(0, Width - Config.ScrollbarWidth - Config.RowGap);
            for (var i = 0; i < VisibleCapacity; i++)
            {
                var itemIndex = first + i;
                if (itemIndex >= Items.Length)
                    break;

                var entry = Items[itemIndex];
                var card = AcquireCard(entry.Notification);
                if (Math.Abs(card.Width - width) > 0.001f)
                    card.Width = width;
                card.Y = ItemOffsets[itemIndex];
                card.SetContent(entry.Notification, entry.ReceivedAt);
                card.Visible = true;
                card.Alpha = 1;
                BoundEntries.Add(card, entry);
                VisibleCards.Add(card);
                card.SetInteractionEnabled(true);
            }
        }

        private NotificationCard AcquireCard(NotificationInfo notification)
        {
            if (notification is MultiplayerInviteNotificationInfo)
            {
                if (InvitePool.Count > 0)
                    return InvitePool.Pop();

                return CreateCard(notification);
            }

            if (StandardPool.Count > 0)
                return StandardPool.Pop();

            return CreateCard(notification);
        }

        private NotificationCard CreateCard(NotificationInfo notification)
        {
            var card = NotificationCard.Create(RowConfig, InviteConfig, NotificationTextures.Shared,
                NotificationCardDisplay.Hub, notification);
            card.Visible = false;
            card.SetInteractionEnabled(false);
            card.PrimaryAction += OnPrimaryAction;
            card.SecondaryAction += OnSecondaryAction;
            AddContainedDrawable(card);
            Cards.Add(card);
            return card;
        }

        private void ReleaseVisibleCards()
        {
            foreach (var card in VisibleCards)
            {
                BoundEntries.Remove(card);
                card.SetInteractionEnabled(false);
                card.ClearContentBinding();
                card.Visible = false;
                if (card is MultiplayerInviteNotificationCard invite)
                    InvitePool.Push(invite);
                else
                    StandardPool.Push((StandardNotificationCard) card);
            }

            VisibleCards.Clear();
        }

        private void RebuildItemOffsets()
        {
            ItemOffsets = new float[Items.Length];
            var y = 0f;
            for (var i = 0; i < Items.Length; i++)
            {
                ItemOffsets[i] = y;
                y += GetItemHeight(Items[i]);
                if (i < Items.Length - 1)
                    y += Config.RowGap;
            }

            ItemsHeight = y;
        }

        private int FindFirstVisibleIndex(float viewportTop)
        {
            var low = 0;
            var high = Items.Length;
            while (low < high)
            {
                var middle = low + (high - low) / 2;
                var itemBottom = ItemOffsets[middle] + GetItemHeight(Items[middle]);
                if (itemBottom < viewportTop)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low;
        }

        private float GetItemHeight(NotificationHistoryEntry entry)
        {
            if (entry.Notification is MultiplayerInviteNotificationInfo)
                return InviteConfig.Height;

            return RowConfig.Height;
        }

        private void OnPrimaryAction(object sender, EventArgs args)
        {
            var card = (NotificationCard) sender;
            if (!BoundEntries.TryGetValue(card, out var entry))
                return;

            entry.ClickAction?.Invoke(sender, args);
            Remove(entry.Id);
        }

        private void OnSecondaryAction(object sender, EventArgs args)
        {
            var card = (NotificationCard) sender;
            if (!BoundEntries.TryGetValue(card, out var entry) ||
                !(entry.Notification is MultiplayerInviteNotificationInfo invite))
                return;

            invite.DeclineAction?.Invoke(sender, args);
            Remove(entry.Id);
        }
    }
}
