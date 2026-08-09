using System;
using Microsoft.Xna.Framework;
using Quaver.API.Enums;
using Quaver.Server.Client.Objects.Twitch;
using Quaver.Shared.Assets;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Helpers;
using Quaver.Shared.Screens.V2.UI.OnlineHub.Shared;
using Quaver.Shared.Skinning.V2;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Buttons;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;

namespace Quaver.Shared.Screens.V2.UI.OnlineHub.SongRequests
{
    internal sealed class SongRequestRow : RoundedButton
    {
        private static OnlineHubDesign Design => OnlineHubDesign.Default;

        private OnlineHubSongRequestRowDesign Config { get; }

        private Action<SongRequest> ShowActions { get; }

        private Sprite RequestIcon { get; }

        private SpriteTextPlus RequestPrefix { get; }

        private OnlineHubMarqueeLabel Requester { get; }

        private OnlineHubMarqueeLabel MapTitle { get; }

        private OnlineHubMarqueeLabel Creator { get; }

        private Sprite TimestampIcon { get; }

        private SpriteTextPlus Timestamp { get; }

        private RoundedButton DifficultyLabel { get; }

        private Sprite DifficultyIcon { get; }

        private OnlineHubMarqueeLabel DifficultyText { get; }

        private RoundedButton ModeLabel { get; }

        private Color TextColor { get; }

        private Color RequesterColor { get; }

        private Color LabelColor { get; }

        private Color BackgroundColor { get; }

        private Color HoverColor { get; }

        private SongRequest Request { get; set; }

        private DateTimeOffset ReceivedAt { get; set; }

        private bool IsPlayed { get; set; }

        private bool SectionInteractionEnabled { get; set; }

        private bool MarqueeActive { get; set; }

        internal SongRequestRow(OnlineHubSongRequestRowDesign config, Action<SongRequest> showActions)
        {
            Config = config;
            ShowActions = showActions;
            TextColor = SkinV2Color.Parse(Design.Style.TextColor);
            RequesterColor = SkinV2Color.Parse(Design.Style.AccentColor);
            LabelColor = SkinV2Color.Parse(Design.Style.SurfaceColor);
            BackgroundColor = SkinV2Color.Parse(Design.Style.ControlColor);
            HoverColor = SkinV2Color.Parse(Config.HoverColor);
            Tint = BackgroundColor;
            CornerRadius = Design.Style.CornerRadius;
            PerformHoverFade = false;
            Visible = false;
            IsInteractionEnabled = false;

            RequestIcon = new Sprite
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                Image = UserInterface.SongRequestIcon,
                Size = new ScalableVector2(Config.IconWidth, Config.IconHeight),
                Tint = TextColor,
                UsePreviousSpriteBatchOptions = true
            };
            RequestPrefix = new SpriteTextPlus(FontManager.GetWobbleFont(Design.Style.Font),
                LocalizationManager.Get("Screen_OnlineHub_RequestFrom"), Design.Style.FontSize)
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                Tint = TextColor,
                UsePreviousSpriteBatchOptions = true
            };
            Requester = new OnlineHubMarqueeLabel(FontManager.GetWobbleFont(Design.Style.Font),
                Design.Style.FontSize)
            {
                Parent = this,
                Alignment = Alignment.TopLeft
            };
            MapTitle = new OnlineHubMarqueeLabel(FontManager.GetWobbleFont(Design.Style.Font), Design.Style.SmallFontSize)
            {
                Parent = this,
                Alignment = Alignment.TopLeft
            };
            Creator = new OnlineHubMarqueeLabel(FontManager.GetWobbleFont(Design.Style.SecondaryFont), Design.Style.DetailFontSize)
            {
                Parent = this,
                Alignment = Alignment.TopLeft
            };
            var timestampColor = SkinV2Color.Parse(Design.Timestamp.Color);
            TimestampIcon = new Sprite
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                Image = UserInterface.Clock,
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
            DifficultyLabel = new RoundedButton
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                Size = new ScalableVector2(Config.DifficultyLabelWidth, Config.LabelHeight),
                CornerRadius = Config.LabelHeight / 2,
                Tint = LabelColor,
                PerformHoverFade = false,
                IsClickable = false,
                IsInteractionEnabled = false,
                UsePreviousSpriteBatchOptions = true
            };
            DifficultyIcon = new Sprite
            {
                Parent = DifficultyLabel,
                Alignment = Alignment.MidLeft,
                Image = UserInterface.SongRequestDifficulty,
                Size = new ScalableVector2(Config.DifficultyIconWidth, Config.DifficultyIconHeight),
                Tint = TextColor,
                UsePreviousSpriteBatchOptions = true
            };
            DifficultyText = new OnlineHubMarqueeLabel(FontManager.GetWobbleFont(Design.Style.Font),
                Design.Style.DetailFontSize)
            {
                Parent = DifficultyLabel,
                Alignment = Alignment.TopLeft
            };
            ModeLabel = new RoundedButton
            {
                Parent = this,
                Alignment = Alignment.TopLeft,
                Size = new ScalableVector2(Config.ModeLabelWidth, Config.LabelHeight),
                CornerRadius = Config.LabelHeight / 2,
                Tint = LabelColor,
                PerformHoverFade = false,
                IsClickable = false,
                IsInteractionEnabled = false,
                UsePreviousSpriteBatchOptions = true
            };
            ModeLabel.SetLabel(FontManager.GetWobbleFont(Design.Style.Font), "", Design.Style.DetailFontSize, TextColor);

            Clicked += OnClicked;
            RightClicked += OnClicked;
            SizeChanged += OnSizeChanged;
            Size = new ScalableVector2(1, Config.Height);
        }

        public override void Destroy()
        {
            Clicked -= OnClicked;
            RightClicked -= OnClicked;
            SizeChanged -= OnSizeChanged;
            base.Destroy();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            Tint = IsHovered ? HoverColor : BackgroundColor;
            SetMarqueeActive(IsHovered);
        }

        internal void Bind(SongRequestFeedItem item, float width, float y)
        {
            if (Math.Abs(Width - width) > 0.001f)
                Width = width;

            Y = y;
            Visible = true;
            UpdateInteraction();
            if (ReferenceEquals(Request, item.Request) && ReceivedAt == item.ReceivedAt && IsPlayed == item.IsPlayed)
                return;

            Request = item.Request;
            ReceivedAt = item.ReceivedAt;
            IsPlayed = item.IsPlayed;
            Requester.SetText(Request?.TwitchUsername ?? "", RequesterColor);
            MapTitle.SetText(GetMapTitle(Request), TextColor);
            Creator.SetText(Request?.Creator ?? "", TextColor);
            Timestamp.Text = ReceivedAt.ToLocalTime().ToString(Design.Timestamp.Format);
            var difficultyColor = GetDifficultyColor(Request);
            DifficultyLabel.Tint = LabelColor;
            DifficultyIcon.Tint = difficultyColor;
            DifficultyText.SetText(GetDifficultyText(Request), difficultyColor);
            ModeLabel.SetLabel(FontManager.GetWobbleFont(Design.Style.Font), GetModeText(Request), Design.Style.DetailFontSize,
                TextColor);
            ApplyPlayedState();
            LayoutContent();
        }

        internal void ClearBinding()
        {
            Request = null;
            ReceivedAt = default;
            IsPlayed = false;
            Visible = false;
            UpdateInteraction();
            ResetInteractionState();
            SetMarqueeActive(false);
        }

        internal void SetSectionInteractionEnabled(bool enabled)
        {
            SectionInteractionEnabled = enabled;
            UpdateInteraction();
            if (!IsInteractionEnabled)
            {
                ResetInteractionState();
                SetMarqueeActive(false);
            }
        }

        private void UpdateInteraction()
        {
            IsInteractionEnabled = SectionInteractionEnabled && Visible;
        }

        private void SetMarqueeActive(bool active)
        {
            if (MarqueeActive == active)
                return;

            MarqueeActive = active;
            Requester.SetMarqueeActive(active);
            MapTitle.SetMarqueeActive(active);
            Creator.SetMarqueeActive(active);
            DifficultyText.SetMarqueeActive(active);
        }

        private void ApplyPlayedState()
        {
            var alpha = IsPlayed ? Config.PlayedContentAlpha : 1;
            RequestIcon.Alpha = alpha;
            RequestPrefix.Alpha = alpha;
            Requester.SetAlpha(alpha);
            MapTitle.SetAlpha(alpha);
            Creator.SetAlpha(alpha);
            TimestampIcon.Alpha = alpha;
            Timestamp.Alpha = alpha;
            DifficultyLabel.Alpha = alpha;
            ModeLabel.Alpha = alpha;
        }

        private void LayoutContent()
        {
            RequestIcon.Position = new ScalableVector2(Config.Padding, (Height - Config.IconHeight) / 2);
            var contentX = Config.Padding + Config.IconWidth + Config.ContentGap;
            var contentRight = Math.Max(contentX, Width - Config.Padding);

            var timestampCenterY = Config.RequestTop + RequestPrefix.CapTopOffset + RequestPrefix.CapHeight / 2;
            Timestamp.Position = new ScalableVector2(contentRight - Timestamp.Width,
                timestampCenterY - Timestamp.CapTopOffset - Timestamp.CapHeight / 2);
            TimestampIcon.Position = new ScalableVector2(Timestamp.X - Design.Timestamp.Gap - Design.Timestamp.IconSize,
                timestampCenterY - Design.Timestamp.IconSize / 2);

            RequestPrefix.Position = new ScalableVector2(contentX, Config.RequestTop);
            var requesterX = RequestPrefix.X + RequestPrefix.Width + Config.TextGap;
            var requesterWidth = Math.Max(0, TimestampIcon.X - Design.Timestamp.Gap - requesterX);
            Requester.Position = new ScalableVector2(requesterX, Config.RequestTop);
            Requester.Size = new ScalableVector2(requesterWidth, RequestPrefix.Height);

            var contentWidth = Math.Max(0, contentRight - contentX);
            MapTitle.Position = new ScalableVector2(contentX, Config.MapTop);
            MapTitle.Size = new ScalableVector2(contentWidth, Design.Style.SmallFontSize + Config.TextGap);
            Creator.Position = new ScalableVector2(contentX, Config.CreatorTop);
            Creator.Size = new ScalableVector2(contentWidth, Design.Style.DetailFontSize + Config.TextGap);

            var labelsWidth = Math.Max(0, contentRight - contentX);
            var difficultyWidth = Math.Min(Config.DifficultyLabelWidth,
                Math.Max(0, labelsWidth - Config.ModeLabelWidth - Config.LabelGap));
            DifficultyLabel.Position = new ScalableVector2(contentX, Config.LabelsTop);
            DifficultyLabel.Size = new ScalableVector2(difficultyWidth, Config.LabelHeight);
            ModeLabel.Position = new ScalableVector2(contentX + difficultyWidth + Config.LabelGap, Config.LabelsTop);

            DifficultyIcon.X = Config.LabelHorizontalPadding;
            var difficultyTextX = Config.LabelHorizontalPadding + Config.DifficultyIconWidth + Config.TextGap;
            DifficultyText.Position = new ScalableVector2(difficultyTextX, 0);
            DifficultyText.Size = new ScalableVector2(
                Math.Max(0, difficultyWidth - difficultyTextX - Config.LabelHorizontalPadding), Config.LabelHeight);
        }

        private void OnSizeChanged(object sender, ScalableVector2 size) => LayoutContent();

        private void OnClicked(object sender, EventArgs args)
        {
            if (Request != null)
                ShowActions(Request);
        }

        private static string GetMapTitle(SongRequest request)
        {
            var artist = request?.Artist ?? "";
            var title = request?.Title ?? "";
            if (artist.Length == 0)
                return title;
            if (title.Length == 0)
                return artist;

            return $"{artist} - {title}";
        }

        private static string GetDifficultyText(SongRequest request)
        {
            if (request == null)
                return "";

            var rating = StringHelper.RatingToString(request.DifficultyRating);
            if ((MapGame) request.Game == MapGame.Osu)
                rating += "*";

            if (string.IsNullOrWhiteSpace(request.DifficultyName))
                return rating;

            return $"{rating} - {request.DifficultyName}";
        }

        private Color GetDifficultyColor(SongRequest request)
        {
            if (request == null)
                return TextColor;

            if ((MapGame) request.Game == MapGame.Osu)
                return ColorHelper.OsuStarRatingToColor((float) request.DifficultyRating);

            return ColorHelper.DifficultyToColor((float) request.DifficultyRating);
        }

        private static string GetModeText(SongRequest request)
        {
            if (request == null)
                return "";

            if (!string.IsNullOrEmpty(request.MapMd5))
            {
                var map = MapManager.FindMapFromMd5(request.MapMd5);
                if (map != null)
                    return map.Mode == GameMode.Keys7 ? "7K" : "4K";
            }

            switch ((MapGame) request.Game)
            {
                case MapGame.Osu:
                    return "osu!";
                case MapGame.Etterna:
                    return "4K";
                default:
                    return "4K/7K";
            }
        }
    }
}
