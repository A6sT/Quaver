using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.API.Helpers;
using Quaver.Shared.Assets;
using Quaver.Shared.Screens.Downloading;
using Quaver.Shared.Screens.V2.UI;
using Quaver.Shared.Skinning.V2;
using Wobble;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Buttons;
using Wobble.Graphics.Shaders;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Buttons;
using Wobble.Managers;

namespace Quaver.Shared.Screens.V2.Downloading.UI
{
    /// <summary>
    ///     Search and filter header for the first V2 Download screen slice.
    /// </summary>
    internal sealed class DownloadingSearchPanel : Sprite
    {
        private DownloadingSearchState State { get; }

        private SkinV2DownloadingConfig Config { get; }

        private WobbleFontStore FieldFont { get; }

        private WobbleFontStore ButtonFont { get; }

        private WobbleFontStore DropdownFont { get; }

        private SkinV2DropdownConfig DropdownConfig { get; }

        private Container OverlayHost { get; }

        private Container LayoutRoot { get; set; }

        private FlexContainer TopRow { get; set; }

        private FlexContainer ExtraRow { get; set; }

        private RoundedButton ExpandButton { get; set; }

        private RoundedButton SortOrderButton { get; set; }

        private bool LayoutDirty { get; set; }

        private bool IsNarrow { get; set; }

        private float ExpansionProgress { get; set; }

        private float LastLayoutWidth { get; set; } = -1;

        private List<float> TopRowItemBases { get; } = new List<float>();

        private List<float> ExtraRowItemBases { get; } = new List<float>();

        private FlexItemOptions SearchItemOptions { get; set; }

        private int TopRowLineCount { get; set; } = 1;

        private int ExtraRowLineCount { get; set; } = 1;

        public DownloadingSearchPanel(float width, DownloadingSearchState state,
            SkinV2DownloadingConfig config, Container overlayHost,
            SkinV2DropdownConfig dropdownConfig)
        {
            State = state;
            Config = config;
            OverlayHost = overlayHost;
            DropdownConfig = dropdownConfig;
            FieldFont = FontManager.GetWobbleFont(config.Field.Font);
            ButtonFont = FontManager.GetWobbleFont(config.Button.Font);
            DropdownFont = FontManager.GetWobbleFont(dropdownConfig.Font);
            Size = new ScalableVector2(width, config.SearchArea.CompactHeight);
            Tint = SkinV2Color.Parse(config.SearchArea.BackgroundColor);
            ExpansionProgress = state.MapsetsExpanded.Value ? 1 : 0;

            State.ActiveTab.ValueChanged += OnTabChanged;
            State.MapsetsExpanded.ValueChanged += OnExpansionChanged;
            State.ShowOwnedMapsets.ValueChanged += OnOwnedChanged;
            State.ShowOwnedPlaylists.ValueChanged += OnOwnedChanged;
            State.ReverseSort.ValueChanged += OnSortOrderChanged;

            RebuildLayout();
        }

        public override void Update(GameTime gameTime)
        {
            if (LayoutDirty)
                RebuildLayout();

            UpdateResponsiveLayout();
            UpdateExpansion(gameTime);
            if (ExtraRow != null)
                ApplyAlpha(ExtraRow, ExpansionProgress);

            base.Update(gameTime);
        }

        public override void Destroy()
        {
            State.ActiveTab.ValueChanged -= OnTabChanged;
            State.MapsetsExpanded.ValueChanged -= OnExpansionChanged;
            State.ShowOwnedMapsets.ValueChanged -= OnOwnedChanged;
            State.ShowOwnedPlaylists.ValueChanged -= OnOwnedChanged;
            State.ReverseSort.ValueChanged -= OnSortOrderChanged;
            base.Destroy();
        }

        protected override void OnRectangleRecalculated()
        {
            base.OnRectangleRecalculated();
            if (Config != null)
            {
                var texture = RoundedRectTextureCache.Get(Width, Height,
                    Config.SearchArea.CornerRadius);
                if (Image != texture)
                    Image = texture;
            }
        }

        private void RebuildLayout()
        {
            LayoutDirty = false;
            LayoutRoot?.Destroy();
            TopRowItemBases.Clear();
            ExtraRowItemBases.Clear();
            SearchItemOptions = null;
            LayoutRoot = new Container
            {
                Parent = this,
                Size = Size
            };

            TopRow = CreateFlexRow(LayoutRoot);
            if (State.ActiveTab.Value == DownloadSearchTab.Mapsets)
            {
                BuildMapsetTopRow();
                ExtraRow = CreateFlexRow(LayoutRoot);
                ExtraRow.DrawOrder = 0;
                BuildMapsetExtraRow();
            }
            else
            {
                BuildPlaylistTopRow();
                ExtraRow = null;
            }

            TopRow.DrawOrder = 10;
            LastLayoutWidth = -1;
            UpdateResponsiveLayout(true);
            UpdateExpandIcon();
        }

        private FlexContainer CreateFlexRow(Drawable parent) => new FlexContainer
        {
            Parent = parent,
            Direction = FlexDirection.Row,
            Wrap = FlexWrap.Wrap,
            JustifyContent = FlexJustifyContent.FlexStart,
            AlignItems = FlexAlignItems.Center,
            AlignContent = FlexAlignContent.FlexStart,
            RowGap = Config.SearchArea.RowGap,
            ColumnGap = Config.SearchArea.ColumnGap
        };

        private void BuildMapsetTopRow()
        {
            AddSearchBox(TopRow, State.MapsetQuery, "Screen_Download_SearchMaps");
            AddFixed(TopRow, CreateDifficultyRange(), GetDifficultyRangeWidth());
            AddFixed(TopRow, CreateToggle(
                "Screen_Download_OwnedMaps", Config.Button.OwnedMapsetsWidth,
                State.ShowOwnedMapsets.Value,
                () => State.ShowOwnedMapsets.Value = !State.ShowOwnedMapsets.Value));
            AddFixed(TopRow, CreateTabs());
            AddFixed(TopRow, CreateKeymodeDropdown(), Config.Button.KeymodeWidth);
            AddFixed(TopRow, CreateRankedDropdown(), Config.Button.RankedWidth);

            ExpandButton = CreateButton(string.Empty, Config.Button.ExpandWidth, false,
                () => State.MapsetsExpanded.Value = !State.MapsetsExpanded.Value,
                GlobalIcons.Get(State.MapsetsExpanded.Value
                    ? GlobalIcon.LessOptions
                    : GlobalIcon.MoreOptions), Config.Button.ExpandIconSize);
            AddFixed(TopRow, ExpandButton, Config.Button.ExpandWidth);
        }

        private void BuildMapsetExtraRow()
        {
            AddFixed(ExtraRow, CreateNumericPair("Screen_Download_LN",
                    State.MinimumLongNotePercentage, State.MaximumLongNotePercentage,
                    "Screen_Download_MinPercent", "Screen_Download_MaxPercent"),
                184);
            AddFixed(ExtraRow, CreateNumericPair("Screen_Download_NPS",
                    State.MinimumNotesPerSecond, State.MaximumNotesPerSecond,
                    "Screen_Download_Min", "Screen_Download_Max"),
                184);
            AddFixed(ExtraRow, CreateNumericPair("Screen_Download_BPM",
                    State.MinimumBpm, State.MaximumBpm,
                    "Screen_Download_Min", "Screen_Download_Max"),
                184);

            var spacer = new Container
            {
                Parent = ExtraRow,
                Size = new ScalableVector2(1, Config.Button.Height)
            };
            ExtraRow.SetItemOptions(spacer, new FlexItemOptions { Basis = 1, Grow = 1, Shrink = 1 });
            ExtraRowItemBases.Add(1);

            AddFixed(ExtraRow, CreateLengthDropdown(), Config.Button.StaticSelectorWidth);
            AddFixed(ExtraRow, CreateComboDropdown(), Config.Button.StaticSelectorWidth);
            SortOrderButton = CreateSortOrderButton();
            AddFixed(ExtraRow, SortOrderButton, Config.Button.ExpandWidth);
            AddFixed(ExtraRow, CreateSortDropdown(), Config.Button.SortWidth);
        }

        private void BuildPlaylistTopRow()
        {
            AddSearchBox(TopRow, State.PlaylistQuery, "Screen_Download_SearchPlaylists");
            AddFixed(TopRow, CreateKeymodeDropdown(), Config.Button.KeymodeWidth);
            AddFixed(TopRow, CreateRankedDropdown(), Config.Button.RankedWidth);
            AddFixed(TopRow, CreateToggle(
                "Screen_Download_OwnedPlaylists", Config.Button.OwnedPlaylistsWidth,
                State.ShowOwnedPlaylists.Value,
                () => State.ShowOwnedPlaylists.Value = !State.ShowOwnedPlaylists.Value));
            AddFixed(TopRow, CreateTabs());
            AddFixed(TopRow, CreateStaticSelector("Screen_Download_AnyMapCount",
                Config.Button.StaticSelectorWidth));
            SortOrderButton = CreateSortOrderButton();
            AddFixed(TopRow, SortOrderButton, Config.Button.ExpandWidth);
            AddFixed(TopRow, CreateSortDropdown(), Config.Button.SortWidth);
        }

        private void AddSearchBox(FlexContainer parent, Bindable<string> query, string placeholderKey)
        {
            var textbox = new DownloadingSearchQueryTextbox(query,
                LocalizationManager.Get(placeholderKey), FieldFont, Config.Field)
            {
                Parent = parent
            };
            SearchItemOptions = new FlexItemOptions
            {
                Basis = Config.Field.SearchWidth,
                Grow = 1,
                Shrink = 1
            };
            parent.SetItemOptions(textbox, SearchItemOptions);
            TopRowItemBases.Add(Config.Field.SearchWidth);
        }

        private FlexContainer CreateDifficultyRange()
        {
            var group = new FlexContainer
            {
                Size = new ScalableVector2(GetDifficultyRangeWidth(), Config.Field.Height),
                Direction = FlexDirection.Row,
                AlignItems = FlexAlignItems.Center,
                ColumnGap = Config.SearchArea.ColumnGap
            };
            var minimum = new DownloadingNumericTextbox(State.MinimumDifficulty,
                string.Empty, FieldFont, Config.Field, Config.Field.NumericWidth,
                "00.00", true, value => Math.Min(value, State.MaximumDifficulty.Value),
                value => value <= State.MinimumDifficulty.MinValue)
            {
                Parent = group
            };
            AddFixed(group, minimum, Config.Field.NumericWidth);

            var slider = new DownloadingRangeSlider(State.MinimumDifficulty,
                State.MaximumDifficulty, Config.Range)
            {
                Parent = group
            };
            AddFixed(group, slider, Config.Range.Width);

            var maximum = new DownloadingNumericTextbox(State.MaximumDifficulty,
                string.Empty, FieldFont, Config.Field, Config.Field.NumericWidth,
                "00.00", true, value => Math.Max(value, State.MinimumDifficulty.Value),
                value => value >= State.MaximumDifficulty.MaxValue)
            {
                Parent = group
            };
            AddFixed(group, maximum, Config.Field.NumericWidth);
            return group;
        }

        private FlexContainer CreateNumericPair(string labelKey, BindableFloat minimum,
            BindableFloat maximum, string minimumPlaceholderKey, string maximumPlaceholderKey)
        {
            var group = new FlexContainer
            {
                Size = new ScalableVector2(184, Config.Field.Height),
                Direction = FlexDirection.Row,
                AlignItems = FlexAlignItems.Center,
                ColumnGap = Config.SearchArea.ColumnGap
            };
            var label = new SpriteTextPlus(ButtonFont, LocalizationManager.Get(labelKey),
                Config.Button.FontSize)
            {
                Parent = group,
                Tint = SkinV2Color.Parse(Config.Button.TextColor)
            };
            group.SetItemOptions(label, new FlexItemOptions { Shrink = 0 });

            var minimumTextbox = new DownloadingNumericTextbox(minimum,
                LocalizationManager.Get(minimumPlaceholderKey), FieldFont, Config.Field,
                Config.Field.NumericCompactWidth, normalize: value => Math.Min(value, maximum.Value))
            {
                Parent = group
            };
            AddFixed(group, minimumTextbox, Config.Field.NumericCompactWidth);

            var maximumTextbox = new DownloadingNumericTextbox(maximum,
                LocalizationManager.Get(maximumPlaceholderKey), FieldFont, Config.Field,
                Config.Field.NumericCompactWidth, normalize: value => Math.Max(value, minimum.Value))
            {
                Parent = group
            };
            AddFixed(group, maximumTextbox, Config.Field.NumericCompactWidth);
            return group;
        }

        private FlexContainer CreateTabs()
        {
            var mapsets = CreateButton("Screen_Download_Mapsets", Config.Button.MapsetsTabWidth,
                State.ActiveTab.Value == DownloadSearchTab.Mapsets,
                () => State.ActiveTab.Value = DownloadSearchTab.Mapsets,
                tabCorners: TabCorners.Left);
            var playlists = CreateButton("Screen_Selection_Playlists",
                Config.Button.PlaylistsTabWidth,
                State.ActiveTab.Value == DownloadSearchTab.Playlists,
                () => State.ActiveTab.Value = DownloadSearchTab.Playlists,
                tabCorners: TabCorners.Right);
            var tabsWidth = mapsets.Width + playlists.Width;

            var tabs = new FlexContainer
            {
                Size = new ScalableVector2(tabsWidth, Config.Button.Height),
                Direction = FlexDirection.Row,
                AlignItems = FlexAlignItems.Center,
                ColumnGap = 0
            };
            mapsets.Parent = tabs;
            AddFixed(tabs, mapsets);

            playlists.Parent = tabs;
            AddFixed(tabs, playlists);

            return tabs;
        }

        private V2Dropdown<int> CreateKeymodeDropdown() =>
            CreateDropdown(Config.Button.KeymodeWidth, State.Keymode, GetKeymodeOptions());

        private V2Dropdown<DownloadSearchRankedStatus> CreateRankedDropdown() =>
            CreateDropdown(Config.Button.RankedWidth, State.RankedStatus, GetRankedOptions());

        private V2Dropdown<DownloadSearchLengthFilter> CreateLengthDropdown() =>
            CreateDropdown(Config.Button.StaticSelectorWidth, State.LengthFilter, GetLengthOptions());

        private V2Dropdown<DownloadSearchComboFilter> CreateComboDropdown() =>
            CreateDropdown(Config.Button.StaticSelectorWidth, State.ComboFilter, GetComboOptions());

        private V2Dropdown<DownloadSearchSortBy> CreateSortDropdown() =>
            CreateDropdown(Config.Button.SortWidth, State.SortBy, GetSortOptions());

        private V2Dropdown<T> CreateDropdown<T>(float width, Bindable<T> value,
            IReadOnlyList<KeyValuePair<T, string>> options)
        {
            var entries = new List<DropdownEntry<T>>();
            foreach (var option in options)
                entries.Add(new DropdownOption<T>(option.Key, option.Value));

            return new V2Dropdown<T>(width, value, entries, DropdownFont, DropdownConfig, OverlayHost);
        }

        private RoundedButton CreateSortOrderButton() =>
            CreateButton(string.Empty, Config.Button.ExpandWidth, false,
                () => State.ReverseSort.Value = !State.ReverseSort.Value,
                GlobalIcons.Get(State.ReverseSort.Value
                    ? GlobalIcon.ReverseSortAscending
                    : GlobalIcon.ReverseSortDescending));

        private RoundedButton CreateToggle(string localizationKey, float width, bool active,
            Action clicked) => CreateButton(localizationKey, width, active, clicked);

        private RoundedButton CreateStaticSelector(string localizationKey, float width,
            TextureRegion? icon = null) =>
            CreateButton(localizationKey, width, false, null, icon);

        private RoundedButton CreateButton(string localizationKey, float width, bool active,
            Action clicked, TextureRegion? icon = null, float? iconSize = null,
            TabCorners? tabCorners = null)
        {
            var clickAction = clicked == null
                ? null
                : (EventHandler) ((sender, args) => clicked());
            var button = tabCorners.HasValue
                ? (RoundedButton) new SegmentedTabButton(tabCorners.Value, clickAction)
                : new DownloadingSearchButton(clickAction);

            button.Size = new ScalableVector2(width, Config.Button.Height);
            button.CornerRadius = Config.Button.CornerRadius;
            button.Tint = SkinV2Color.Parse(active
                    ? Config.Button.ActiveColor
                    : Config.Button.BackgroundColor);
            button.PerformHoverFade = true;

            if (icon.HasValue)
            {
                var size = iconSize ?? Config.Button.IconSize;
                button.SetIcon(icon.Value, new Vector2(size, size));
            }
            if (!string.IsNullOrEmpty(localizationKey))
                button.SetLabel(ButtonFont, LocalizationManager.Get(localizationKey),
                    Config.Button.FontSize, SkinV2Color.Parse(active
                        ? Config.Button.ActiveTextColor
                        : Config.Button.TextColor));

            if (button.Label != null)
            {
                var contentWidth = button.Label.Width + Config.Button.HorizontalPadding * 2;
                if (button.Icon != null)
                    contentWidth += button.Icon.Width + 8;
                button.Width = Math.Max(button.Width, contentWidth);
            }

            return button;
        }

        [Flags]
        private enum TabCorners
        {
            TopLeft = 1,
            TopRight = 2,
            BottomLeft = 4,
            BottomRight = 8,
            Left = TopLeft | BottomLeft,
            Right = TopRight | BottomRight
        }

        /// <summary>
        ///     Keeps the standard RoundedButton interaction and hover behavior while only rounding
        ///     the outside corners of a segmented control. Wobble does not clip child drawables to
        ///     a rounded parent, so a single rounded group background cannot produce this result.
        /// </summary>
        private sealed class SegmentedTabButton : RoundedButton
        {
            private TabCorners Corners { get; }

            private Texture2D BackgroundTexture { get; set; }

            private bool UpdatingBackground { get; set; }

            public SegmentedTabButton(TabCorners corners, EventHandler clickAction = null)
                : base(clickAction)
            {
                Corners = corners;
            }

            protected override void OnRectangleRecalculated()
            {
                if (UpdatingBackground)
                    return;

                UpdatingBackground = true;
                try
                {
                    base.OnRectangleRecalculated();

                    if (Width <= 0 || Height <= 0)
                        return;

                    var width = Math.Max(1, (int) Math.Ceiling(Width));
                    var height = Math.Max(1, (int) Math.Ceiling(Height));
                    var radius = Math.Min(CornerRadius ?? Height / 2f,
                        Math.Min(Width, Height) / 2f);
                    var texture = CreateBackgroundTexture(width, height, radius);
                    BackgroundTexture?.Dispose();
                    BackgroundTexture = texture;
                    Image = texture;
                }
                finally
                {
                    UpdatingBackground = false;
                }
            }

            public override void Destroy()
            {
                var texture = BackgroundTexture;
                BackgroundTexture = null;
                base.Destroy();
                texture?.Dispose();
            }

            private Texture2D CreateBackgroundTexture(int width, int height, float radius)
            {
                var texture = new Texture2D(GameBase.Game.GraphicsDevice, width, height,
                    false, SurfaceFormat.Color);
                var pixels = new Color[width * height];
                var halfWidth = width / 2f;
                var halfHeight = height / 2f;

                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var corner = GetCorner(x, y, halfWidth, halfHeight);
                        var cornerRadius = Corners.HasFlag(corner) ? radius : 0;
                        var qx = Math.Abs(x + 0.5f - halfWidth) - (halfWidth - cornerRadius);
                        var qy = Math.Abs(y + 0.5f - halfHeight) - (halfHeight - cornerRadius);
                        var outsideDistance = (float) Math.Sqrt(Math.Max(qx, 0) * Math.Max(qx, 0) +
                                                                Math.Max(qy, 0) * Math.Max(qy, 0));
                        var distance = outsideDistance + Math.Min(Math.Max(qx, qy), 0) - cornerRadius;
                        var coverage = 1 - SmoothStep(-1, 0, distance);
                        pixels[y * width + x] = new Color((byte) 255, (byte) 255, (byte) 255,
                            (byte) (Microsoft.Xna.Framework.MathHelper.Clamp(coverage, 0, 1) * 255));
                    }
                }

                texture.SetData(pixels);
                return texture;
            }

            private static TabCorners GetCorner(int x, int y, float halfWidth, float halfHeight)
            {
                var isLeft = x < halfWidth;
                var isTop = y < halfHeight;

                if (isTop)
                    return isLeft ? TabCorners.TopLeft : TabCorners.TopRight;

                return isLeft ? TabCorners.BottomLeft : TabCorners.BottomRight;
            }

            private static float SmoothStep(float min, float max, float value)
            {
                var amount = Microsoft.Xna.Framework.MathHelper.Clamp((value - min) / (max - min), 0, 1);
                return amount * amount * (3 - 2 * amount);
            }
        }

        private void UpdateResponsiveLayout(bool force = false)
        {
            if (!force && Math.Abs(Width - LastLayoutWidth) < 0.001f)
                return;

            LastLayoutWidth = Width;
            IsNarrow = Width < Config.Layout.ReflowBreakpoint;
            var padding = Config.SearchArea.Padding;
            var contentWidth = Math.Max(1, Width - padding * 2);
            var searchBasis = IsNarrow
                ? Config.Field.SearchMinimumWidth
                : Config.Field.SearchWidth;
            if (SearchItemOptions != null)
                SearchItemOptions.Basis = searchBasis;
            if (TopRowItemBases.Count > 0)
                TopRowItemBases[0] = searchBasis;

            TopRowLineCount = CountWrappedLines(TopRowItemBases, contentWidth);
            ExtraRowLineCount = CountWrappedLines(ExtraRowItemBases, contentWidth);
            var compactHeight = GetCompactHeight();
            var expandedHeight = GetExpandedHeight();
            var topHeight = Math.Max(1, compactHeight - padding * 2);

            LayoutRoot.Size = new ScalableVector2(Width, Height);
            TopRow.Position = new ScalableVector2(padding, padding);
            TopRow.Size = new ScalableVector2(contentWidth, topHeight);
            TopRow.RefreshLayout();

            if (ExtraRow != null)
            {
                ExtraRow.Position = new ScalableVector2(padding,
                    padding + topHeight + Config.SearchArea.RowGap);
                ExtraRow.Size = new ScalableVector2(contentWidth,
                    Math.Max(1, expandedHeight - compactHeight - Config.SearchArea.RowGap));
                ExtraRow.RefreshLayout();
            }

            UpdatePanelHeight();
        }

        private void UpdateExpansion(GameTime gameTime)
        {
            if (State.ActiveTab.Value != DownloadSearchTab.Mapsets)
            {
                ExpansionProgress = 0;
                UpdatePanelHeight();
                return;
            }

            var target = State.MapsetsExpanded.Value ? 1f : 0f;
            if (Math.Abs(target - ExpansionProgress) > 0.001f)
            {
                var change = (float) (gameTime.ElapsedGameTime.TotalMilliseconds /
                                      Math.Max(1, Config.SearchArea.ExpansionDurationMilliseconds));
                ExpansionProgress = target > ExpansionProgress
                    ? Math.Min(target, ExpansionProgress + change)
                    : Math.Max(target, ExpansionProgress - change);
            }
            else
                ExpansionProgress = target;

            if (ExtraRow != null)
                ExtraRow.Visible = State.MapsetsExpanded.Value && ExpansionProgress > 0.001f;
            UpdatePanelHeight();
        }

        private void UpdatePanelHeight()
        {
            var compact = GetCompactHeight();
            // Keep the result grid out of the expansion animation. Interpolating this height
            // moves every mapset card every frame, which recursively recalculates all of the
            // card descendants while the filters are opening or closing.
            var targetHeight = State.ActiveTab.Value == DownloadSearchTab.Playlists ||
                               !State.MapsetsExpanded.Value
                ? compact
                : GetExpandedHeight();
            if (Math.Abs(Height - targetHeight) > 0.001f)
            {
                Height = targetHeight;
                LayoutRoot.Height = targetHeight;
            }
        }

        private float GetCompactHeight()
        {
            var baseHeight = State.ActiveTab.Value == DownloadSearchTab.Playlists
                ? Config.SearchArea.PlaylistHeight
                : Config.SearchArea.CompactHeight;
            return baseHeight + Math.Max(0, TopRowLineCount - 1) *
                (Config.Button.Height + Config.SearchArea.RowGap);
        }

        private float GetExpandedHeight() =>
            Config.SearchArea.ExpandedHeight +
            Math.Max(0, TopRowLineCount - 1) *
            (Config.Button.Height + Config.SearchArea.RowGap) +
            Math.Max(0, ExtraRowLineCount - 1) *
            (Config.Button.Height + Config.SearchArea.RowGap);

        private int CountWrappedLines(IReadOnlyList<float> itemBases, float availableWidth)
        {
            if (itemBases.Count == 0)
                return 1;

            var lineCount = 1;
            var occupiedWidth = 0f;
            foreach (var itemBasis in itemBases)
            {
                var requiredWidth = occupiedWidth +
                    (occupiedWidth > 0 ? Config.SearchArea.ColumnGap : 0) + itemBasis;
                if (occupiedWidth > 0 && requiredWidth > availableWidth)
                {
                    lineCount++;
                    occupiedWidth = itemBasis;
                    continue;
                }

                occupiedWidth = requiredWidth;
            }

            return lineCount;
        }

        private float GetDifficultyRangeWidth() =>
            Config.Field.NumericWidth * 2 + Config.Range.Width +
            Config.SearchArea.ColumnGap * 2;

        private void UpdateExpandIcon()
        {
            if (ExpandButton == null)
                return;

            ExpandButton.SetIcon(GlobalIcons.Get(State.MapsetsExpanded.Value
                    ? GlobalIcon.LessOptions
                    : GlobalIcon.MoreOptions),
                new Vector2(Config.Button.ExpandIconSize, Config.Button.ExpandIconSize));
        }

        private void OnTabChanged(object sender, BindableValueChangedEventArgs<DownloadSearchTab> args) =>
            LayoutDirty = true;

        private void OnExpansionChanged(object sender, BindableValueChangedEventArgs<bool> args) =>
            UpdateExpandIcon();

        private void OnOwnedChanged(object sender, BindableValueChangedEventArgs<bool> args) =>
            LayoutDirty = true;

        private void OnSortOrderChanged(object sender, BindableValueChangedEventArgs<bool> args) =>
            UpdateSortOrderIcon();

        private void UpdateSortOrderIcon()
        {
            if (SortOrderButton == null)
                return;

            SortOrderButton.SetIcon(GlobalIcons.Get(State.ReverseSort.Value
                    ? GlobalIcon.ReverseSortAscending
                    : GlobalIcon.ReverseSortDescending),
                new Vector2(Config.Button.IconSize, Config.Button.IconSize));
        }

        private void AddFixed(FlexContainer parent, Drawable child, float basis)
        {
            if (child.Parent == null)
                child.Parent = parent;
            parent.SetItemOptions(child, new FlexItemOptions
            {
                Basis = basis,
                Grow = 0,
                Shrink = 0
            });

            if (parent == TopRow)
                TopRowItemBases.Add(basis);
            else if (parent == ExtraRow)
                ExtraRowItemBases.Add(basis);
        }

        private void AddFixed(FlexContainer parent, Drawable child) =>
            AddFixed(parent, child, child.Width);

        private static IReadOnlyList<KeyValuePair<int, string>> GetKeymodeOptions()
        {
            var options = new List<KeyValuePair<int, string>>
            {
                new KeyValuePair<int, string>(0,
                    LocalizationManager.Get("Screen_Download_AllKeymodes"))
            };
            options.AddRange(ModeHelper.AllModes.Select(mode =>
                new KeyValuePair<int, string>((int) mode,
                    LocalizationManager.Get("Screen_Download_" +
                                            ModeHelper.ToLongHand(mode).Replace(" ", string.Empty)))));
            return options;
        }

        private static IReadOnlyList<KeyValuePair<DownloadSearchRankedStatus, string>>
            GetRankedOptions() => new[]
        {
            new KeyValuePair<DownloadSearchRankedStatus, string>(
                DownloadSearchRankedStatus.All,
                LocalizationManager.Get("Screen_Download_All")),
            new KeyValuePair<DownloadSearchRankedStatus, string>(
                DownloadSearchRankedStatus.Unranked,
                LocalizationManager.Get("Screen_Download_Unranked")),
            new KeyValuePair<DownloadSearchRankedStatus, string>(
                DownloadSearchRankedStatus.Ranked,
                LocalizationManager.Get("Screen_Download_Ranked")),
            new KeyValuePair<DownloadSearchRankedStatus, string>(
                DownloadSearchRankedStatus.ClanRanked,
                LocalizationManager.Get("Screen_Download_ClanRanked"))
        };

        private static IReadOnlyList<KeyValuePair<DownloadSearchLengthFilter, string>>
            GetLengthOptions() => new[]
        {
            new KeyValuePair<DownloadSearchLengthFilter, string>(
                DownloadSearchLengthFilter.Any,
                LocalizationManager.Get("Screen_Download_AnyLength")),
            new KeyValuePair<DownloadSearchLengthFilter, string>(
                DownloadSearchLengthFilter.LessThan30Seconds,
                LocalizationManager.Get("Screen_Download_LengthUnder30Seconds")),
            new KeyValuePair<DownloadSearchLengthFilter, string>(
                DownloadSearchLengthFilter.From30To90Seconds,
                LocalizationManager.Get("Screen_Download_Length30To90Seconds")),
            new KeyValuePair<DownloadSearchLengthFilter, string>(
                DownloadSearchLengthFilter.From90To150Seconds,
                LocalizationManager.Get("Screen_Download_Length90To150Seconds")),
            new KeyValuePair<DownloadSearchLengthFilter, string>(
                DownloadSearchLengthFilter.From150To210Seconds,
                LocalizationManager.Get("Screen_Download_Length150To210Seconds")),
            new KeyValuePair<DownloadSearchLengthFilter, string>(
                DownloadSearchLengthFilter.From210To300Seconds,
                LocalizationManager.Get("Screen_Download_Length210To300Seconds")),
            new KeyValuePair<DownloadSearchLengthFilter, string>(
                DownloadSearchLengthFilter.From300To600Seconds,
                LocalizationManager.Get("Screen_Download_Length300To600Seconds")),
            new KeyValuePair<DownloadSearchLengthFilter, string>(
                DownloadSearchLengthFilter.GreaterThan600Seconds,
                LocalizationManager.Get("Screen_Download_LengthOver600Seconds"))
        };

        private static IReadOnlyList<KeyValuePair<DownloadSearchComboFilter, string>>
            GetComboOptions() => new[]
        {
            new KeyValuePair<DownloadSearchComboFilter, string>(
                DownloadSearchComboFilter.Any,
                LocalizationManager.Get("Screen_Download_AnyCombo")),
            new KeyValuePair<DownloadSearchComboFilter, string>(
                DownloadSearchComboFilter.LessThan150,
                LocalizationManager.Get("Screen_Download_ComboUnder150")),
            new KeyValuePair<DownloadSearchComboFilter, string>(
                DownloadSearchComboFilter.From151To250,
                LocalizationManager.Get("Screen_Download_Combo151To250")),
            new KeyValuePair<DownloadSearchComboFilter, string>(
                DownloadSearchComboFilter.From251To500,
                LocalizationManager.Get("Screen_Download_Combo251To500")),
            new KeyValuePair<DownloadSearchComboFilter, string>(
                DownloadSearchComboFilter.From501To1000,
                LocalizationManager.Get("Screen_Download_Combo501To1000")),
            new KeyValuePair<DownloadSearchComboFilter, string>(
                DownloadSearchComboFilter.From1001To1500,
                LocalizationManager.Get("Screen_Download_Combo1001To1500")),
            new KeyValuePair<DownloadSearchComboFilter, string>(
                DownloadSearchComboFilter.From1501To2500,
                LocalizationManager.Get("Screen_Download_Combo1501To2500")),
            new KeyValuePair<DownloadSearchComboFilter, string>(
                DownloadSearchComboFilter.GreaterThan2501,
                LocalizationManager.Get("Screen_Download_ComboOver2501"))
        };

        private static IReadOnlyList<KeyValuePair<DownloadSearchSortBy, string>>
            GetSortOptions() => new[]
        {
            new KeyValuePair<DownloadSearchSortBy, string>(
                DownloadSearchSortBy.Newest,
                DownloadLocalization.Get("Newest")),
            new KeyValuePair<DownloadSearchSortBy, string>(
                DownloadSearchSortBy.DateSubmitted,
                DownloadLocalization.Get("Date Submitted")),
            new KeyValuePair<DownloadSearchSortBy, string>(
                DownloadSearchSortBy.Length,
                DownloadLocalization.Get("Length")),
            new KeyValuePair<DownloadSearchSortBy, string>(
                DownloadSearchSortBy.Difficulty,
                DownloadLocalization.Get("Difficulty")),
            new KeyValuePair<DownloadSearchSortBy, string>(
                DownloadSearchSortBy.MaxCombo,
                DownloadLocalization.Get("Max Combo")),
            new KeyValuePair<DownloadSearchSortBy, string>(
                DownloadSearchSortBy.Bpm,
                DownloadLocalization.Get("BPM")),
            new KeyValuePair<DownloadSearchSortBy, string>(
                DownloadSearchSortBy.LongNotePercentage,
                DownloadLocalization.Get("LN %")),
            new KeyValuePair<DownloadSearchSortBy, string>(
                DownloadSearchSortBy.PlayCount,
                DownloadLocalization.Get("Play Count"))
        };

        private static void ApplyAlpha(Drawable drawable, float alpha)
        {
            if (drawable is V2DropdownBase dropdown)
            {
                dropdown.SetExternalAlphaInternal(alpha);
                return;
            }

            if (drawable is Button button)
            {
                button.IsInteractionEnabled = alpha > 0.001f;

                if (button is DownloadingSearchButton downloadingButton)
                {
                    downloadingButton.SetExpansionAlpha(alpha);
                    return;
                }
            }

            if (drawable is DownloadingSearchTextbox textbox)
            {
                textbox.Alpha = alpha;
                textbox.InputText.Alpha = alpha;
                return;
            }

            if (drawable is Sprite sprite)
            {
                sprite.Alpha = alpha;
                return;
            }

            foreach (var child in drawable.Children)
                ApplyAlpha(child, alpha);
        }
    }
}
