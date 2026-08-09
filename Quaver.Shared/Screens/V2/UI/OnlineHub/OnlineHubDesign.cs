using Quaver.Shared.Skinning.V2;

namespace Quaver.Shared.Screens.V2.UI.OnlineHub
{
    internal sealed class OnlineHubDesign
    {
        internal static OnlineHubDesign Default { get; } = new OnlineHubDesign();

        internal OnlineHubStyleDesign Style { get; } = new OnlineHubStyleDesign();

        internal OnlineHubToolbarDesign Toolbar { get; } = new OnlineHubToolbarDesign();

        internal OnlineHubFeedDesign Feed { get; } = new OnlineHubFeedDesign();

        internal OnlineHubTimestampDesign Timestamp { get; } = new OnlineHubTimestampDesign();

        internal OnlineHubWindowDesign Window { get; } = new OnlineHubWindowDesign();

        internal OnlineHubHeaderDesign Header { get; } = new OnlineHubHeaderDesign();

        internal float Padding { get; } = 8;

        internal float SectionGap { get; } = 8;

        internal OnlineHubNotificationsDesign Notifications { get; } = new OnlineHubNotificationsDesign();

        internal OnlineHubUsersDesign Users { get; } = new OnlineHubUsersDesign();

        internal OnlineHubSongRequestsDesign SongRequests { get; } = new OnlineHubSongRequestsDesign();
    }

    /// <summary>
    /// Global style settings shared
    /// FontSize is used by buttons and row titles
    /// SmallFontSize by map titles and status text
    /// </summary>
    internal sealed class OnlineHubStyleDesign
    {
        internal string Font { get; } = SkinV2FontWeightsConfig.Bold;

        internal string SecondaryFont { get; } = SkinV2FontWeightsConfig.SemiBold;

        internal int FontSize { get; } = SkinV2FontSizesConfig.TextLg;

        internal int SmallFontSize { get; } = SkinV2FontSizesConfig.TextBase;

        internal int DetailFontSize { get; } = SkinV2FontSizesConfig.TextSm;

        internal float CornerRadius { get; } = SkinV2BorderRadiusConfig.Normal;

        internal string TextColor { get; } = "#FFFFFFFF";

        internal string AccentColor { get; } = "#4889CFFF";

        internal string SurfaceColor { get; } = "#283038FF";

        internal string ControlColor { get; } = "#191E25FF";
    }

    /// <summary>
    /// Shared layout settings for the main tab buttons and the toolbar beneath them in all three sections.
    /// </summary>
    internal sealed class OnlineHubToolbarDesign
    {
        internal float Height { get; } = 53;

        internal float Padding { get; } = 8;

        internal float Gap { get; } = 8;

        internal float ControlHeight => Height - Padding * 2;

        internal float IconSize { get; } = SkinV2Spacing.Spacing3Xl;

        internal float ClearButtonWidth { get; } = 124;
    }

    /// <summary>
    /// Scrolling NotificationFeed, UserFeed and SongRequestFeed settings
    /// Song requests override row spacing and scrollbar width
    /// </summary>
    internal sealed class OnlineHubFeedDesign
    {
        internal float RowGap { get; }

        internal float ScrollbarWidth { get; }

        internal int ScrollSpeed { get; } = 220;

        internal int OverscanRows { get; } = 2;

        internal float EmptyStateGap { get; } = SkinV2MarginsConfig.Sm;

        internal int EmptyStateTitleFontSize { get; } = SkinV2FontSizesConfig.Text2Xl;

        internal int EmptyStateDescriptionFontSize { get; } = SkinV2FontSizesConfig.TextXl;

        internal OnlineHubFeedDesign(float rowGap = 8, float scrollbarWidth = 4)
        {
            RowGap = rowGap;
            ScrollbarWidth = scrollbarWidth;
        }
    }

    /// <summary>
    /// Timestamp settings
    /// </summary>
    internal sealed class OnlineHubTimestampDesign
    {
        internal float IconSize { get; } = SkinV2FontSizesConfig.TextSm;

        internal float Gap { get; } = SkinV2MarginsConfig.Sm;

        internal int FontSize { get; } = SkinV2FontSizesConfig.TextXs;

        internal string Format { get; } = "HH:mm";

        internal string Color { get; } = "#B8B8B8FF";
    }

    /// <summary>
    /// Opening animation settings
    /// </summary>
    internal sealed class OnlineHubWindowDesign
    {
        internal float Width { get; } = 736;

        internal int SlideDurationMilliseconds { get; } = 220;
    }

    /// <summary>
    /// Hub header used to cover the user's profile
    /// </summary>
    internal sealed class OnlineHubHeaderDesign
    {
        internal float ProfileWidth { get; } = 476;

        internal string BackgroundColor { get; } = "#171D24FF";
    }

    /// <summary>
    /// OnlineHubUsersSection groups the search/filter controls and player rows
    /// </summary>
    internal sealed class OnlineHubUsersDesign
    {
        internal OnlineHubUserControlsDesign Controls { get; } = new OnlineHubUserControlsDesign();

        internal OnlineHubUserRowDesign Row { get; } = new OnlineHubUserRowDesign();
    }

    /// <summary>
    /// User tab search and filter dropdown settings
    /// </summary>
    internal sealed class OnlineHubUserControlsDesign
    {
        internal float Gap { get; } = SkinV2MarginsConfig.Md;

        internal float FilterWidth { get; } = 232;

        internal float SearchMinimumWidth { get; } = 200;

        internal float SearchIconSize { get; } = SkinV2Spacing.SpacingLg;

        internal float HorizontalPadding { get; } = SkinV2MarginsConfig.Md;

        internal string PlaceholderColor { get; } = "#D0D0D0FF";
    }

    /// <summary>
    /// Users tab avatar, flag, name/status layout and online-status indicator settings
    /// </summary>
    internal sealed class OnlineHubUserRowDesign
    {
        internal float Height { get; } = 62;

        internal float AvatarSize { get; } = 62;

        internal float ContentGap { get; } = SkinV2MarginsConfig.Md;

        internal float FlagSize { get; } = SkinV2Spacing.Spacing3Xl;

        internal float IdentityGap { get; } = SkinV2MarginsConfig.Sm;

        internal float IdentityHeight { get; } = SkinV2Spacing.Spacing3Xl;

        internal float StatusHeight { get; } = SkinV2Spacing.SpacingXl;

        internal float OnlineStatusBorderSize { get; } = SkinV2Spacing.Spacing2Xl;

        internal float OnlineStatusSize { get; } = SkinV2Spacing.SpacingBase;

        internal string StatusTextColor { get; } = "#A7A7A7FF";

        internal string OnlineStatusBorderColor { get; } = "#222D35FF";

        internal string OnlineStatusColor { get; } = "#25F3AAFF";
    }

    /// <summary>
    /// Notifications section New/History segment width and card-specific settings
    /// </summary>
    internal sealed class OnlineHubNotificationsDesign
    {
        internal float SegmentedControlWidth { get; } = 476;

        internal OnlineHubNotificationRowDesign Row { get; } = new OnlineHubNotificationRowDesign();

        internal OnlineHubMultiplayerInviteDesign MultiplayerInvite { get; } = new OnlineHubMultiplayerInviteDesign();
    }

    /// <summary>
    /// Shared notification card settings
    /// </summary>
    internal sealed class OnlineHubNotificationRowDesign
    {
        internal float Height { get; } = 86;

        internal float Padding { get; } = SkinV2Spacing.SpacingSm;

        internal float IconSize { get; } = 34;

        internal float ContentGap { get; } = SkinV2Spacing.SpacingSm;

        internal float BorderThickness { get; } = 2;

        internal float AccentWidth { get; } = SkinV2Spacing.SpacingXs;

        internal float AccentGap { get; } = SkinV2Spacing.Spacing2Xs;

        internal string BackgroundColor { get; } = "#242424FF";

        internal string InfoColor { get; } = "#57BFFEFF";

        internal string ErrorColor { get; } = "#F9326EFF";

        internal string WarningColor { get; } = "#FBE23EFF";

        internal string SuccessColor { get; } = "#4DFFA0FF";
    }

    /// <summary>
    /// Multiplayer Invite Notification card settings
    /// </summary>
    internal sealed class OnlineHubMultiplayerInviteDesign
    {
        internal float Height { get; } = 122;

        internal float AvatarSize { get; } = 48;

        internal float TitleGap { get; } = SkinV2MarginsConfig.Sm;

        internal float DescriptionTop { get; } = 39;

        internal float ButtonTop { get; } = 67;

        internal float ButtonWidth { get; } = 204;

        internal float ButtonHeight { get; } = 40;

        internal float ButtonGap { get; } = SkinV2Spacing.Spacing2Xs;
    }

    /// <summary>
    /// Song Requests section settings
    /// </summary>
    internal sealed class OnlineHubSongRequestsDesign
    {
        internal float DisplayAlertWidth { get; } = 232;

        internal float ConnectButtonWidth { get; } = 206;

        internal float HeaderHorizontalPadding { get; } = SkinV2Spacing.SpacingXs;

        internal float AlertToggleWidth { get; } = 64;

        internal float AlertToggleHeight { get; } = SkinV2Spacing.Spacing3Xl;

        internal float AlertTogglePadding { get; } = 3;

        internal float AlertToggleStateWidth { get; } = 32;

        internal int AlertToggleFontSize { get; } = SkinV2FontSizesConfig.TextXs;

        internal string AlertToggleInactiveColor { get; } = "#343B45FF";

        internal string AlertToggleOffColor { get; } = "#F9326EFF";

        internal string AlertToggleOnColor { get; } = "#25F3AAFF";

        internal string AlertToggleStateColor { get; } = "#F2F2F2FF";

        internal string AlertToggleOffTextColor { get; } = "#C82C5AFF";

        internal string AlertToggleOnTextColor { get; } = "#179E70FF";

        internal float ActionMenuWidth { get; } = 200;

        internal float ActionMenuPadding { get; } = SkinV2MarginsConfig.Sm;

        internal float ActionMenuItemHeight { get; } = 40;

        internal float ActionMenuItemGap { get; } = 2;

        internal string ActionMenuColor { get; } = "#454545FF";

        internal string ActionMenuItemColor { get; } = "#555555FF";

        internal string ProfileActionColor { get; } = "#0787E3FF";

        internal string ListingActionColor { get; } = "#9B51E0FF";

        internal string DeleteActionColor { get; } = "#FF6868FF";

        internal OnlineHubFeedDesign Feed { get; } = new OnlineHubFeedDesign(SkinV2MarginsConfig.Md, SkinV2Spacing.SpacingXs);

        internal OnlineHubSongRequestRowDesign Row { get; } = new OnlineHubSongRequestRowDesign();
    }

    /// <summary>
    /// Song Request row settings
    /// </summary>
    internal sealed class OnlineHubSongRequestRowDesign
    {
        internal float Height { get; } = 121;

        internal float Padding { get; } = SkinV2Spacing.SpacingSm;

        internal float IconWidth { get; } = 48;

        internal float IconHeight { get; } = 36;

        internal float TextGap { get; } = SkinV2MarginsConfig.Sm;

        internal float ContentGap { get; } = 9;

        internal float RequestTop { get; } = 11;

        internal float MapTop { get; } = 34;

        internal float CreatorTop { get; } = 57;

        internal float LabelsTop { get; } = 80;

        internal float LabelHeight { get; } = 27;

        internal float DifficultyLabelWidth { get; } = 326;

        internal float ModeLabelWidth { get; } = 80;

        internal float LabelGap { get; } = 8;

        internal float LabelHorizontalPadding { get; } = SkinV2MarginsConfig.Md;

        internal float DifficultyIconWidth { get; } = SkinV2Spacing.Spacing3Xl;

        internal float DifficultyIconHeight { get; } = SkinV2Spacing.SpacingLg;

        internal string HoverColor { get; } = "#222A33FF";

        internal float PlayedContentAlpha { get; } = 0.7f;
    }
}
