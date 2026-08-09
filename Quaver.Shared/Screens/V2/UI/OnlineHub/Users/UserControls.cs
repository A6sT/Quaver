using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Quaver.Shared.Assets;
using Quaver.Shared.Graphics.Form.Dropdowns;
using Quaver.Shared.Graphics.Overlays.Hub.OnlineUsers;
using Quaver.Shared.Skinning.V2;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Shaders;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.UI.Form;
using Wobble.Managers;

namespace Quaver.Shared.Screens.V2.UI.OnlineHub.Users
{
    internal sealed class UserControls : FlexContainer
    {
        private static OnlineHubDesign Design => OnlineHubDesign.Default;

        private OnlineHubUserControlsDesign Config { get; }

        private Container OverlayHost { get; }

        private Textbox SearchBox { get; }

        private Dropdown FilterDropdown { get; }

        private Container FilterSlot { get; }

        private Color TextColor { get; }

        private Color PlaceholderColor { get; }

        private bool SearchWasEmpty { get; set; }

        internal event EventHandler QueryChanged;

        internal event EventHandler FilterChanged;

        internal string SearchQuery { get; private set; } = "";

        internal bool DropdownOpened => FilterDropdown.Opened;

        internal UserControls(Container overlayHost, OnlineUserListFilter initialFilter,
            OnlineHubUserControlsDesign config)
        {
            Config = config;
            OverlayHost = overlayHost;
            TextColor = SkinV2Color.Parse(Design.Style.TextColor);
            PlaceholderColor = SkinV2Color.Parse(Config.PlaceholderColor);
            Direction = FlexDirection.Row;
            AlignItems = FlexAlignItems.Stretch;
            ColumnGap = Config.Gap;
            Height = Design.Toolbar.ControlHeight;

            var font = FontManager.GetWobbleFont(Design.Style.Font);
            SearchBox = new Textbox(new ScalableVector2(Config.SearchMinimumWidth, Design.Toolbar.ControlHeight), font,
                Design.Style.FontSize, SearchQuery, LocalizationManager.Get("Screen_OnlineHub_SearchUsers"))
            {
                Parent = this,
                Tint = SkinV2Color.Parse(Design.Style.ControlColor),
                StoppedTypingActionCalltime = 250
            };
            SearchBox.Cursor.Tint = SkinV2Color.Parse(Design.Style.TextColor);
            SearchBox.InputText.X = Config.HorizontalPadding * 2 + Config.SearchIconSize;
            SearchBox.Scrollbar.Visible = false;
            SearchBox.OnStoppedTyping += OnQueryChanged;
            SearchBox.SizeChanged += OnSearchBoxSizeChanged;
            _ = new Sprite
            {
                Parent = SearchBox,
                Alignment = Alignment.MidLeft,
                X = Config.HorizontalPadding,
                Image = FontAwesome.Get(FontAwesomeIcon.fa_magnifying_glass),
                Size = new ScalableVector2(Config.SearchIconSize, Config.SearchIconSize),
                Tint = PlaceholderColor,
                UsePreviousSpriteBatchOptions = true
            };
            SetItemOptions(SearchBox, new FlexItemOptions
            {
                Basis = Config.SearchMinimumWidth,
                Grow = 1,
                Shrink = 1
            });

            var options = new List<string>
            {
                GetFilterLabel(OnlineUserListFilter.All),
                GetFilterLabel(OnlineUserListFilter.Friends),
                GetFilterLabel(OnlineUserListFilter.Country)
            };
            FilterSlot = new Container { Parent = this };
            SetItemOptions(FilterSlot, new FlexItemOptions { Basis = Config.FilterWidth, Shrink = 0 });
            FilterDropdown = new Dropdown(options, new ScalableVector2(Config.FilterWidth, Design.Toolbar.ControlHeight),
                Design.Style.FontSize, TextColor, (int) initialFilter)
            {
                Parent = OverlayHost,
                Alignment = Alignment.TopLeft,
                Tint = SkinV2Color.Parse(Design.Style.ControlColor),
                HighlightAlpha = 0.2f,
                IsInteractionEnabled = false
            };
            FilterDropdown.SelectedText.X = Config.HorizontalPadding;
            FilterDropdown.Chevron.X = -Config.HorizontalPadding;
            FilterDropdown.ItemSelected += OnFilterSelected;

            ApplySearchBoxSize();
            ApplySearchTextColor(true);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            ApplySearchTextColor(false);
            LayoutFilterDropdown();
        }

        public override void Destroy()
        {
            SearchBox.OnStoppedTyping -= OnQueryChanged;
            SearchBox.SizeChanged -= OnSearchBoxSizeChanged;
            FilterDropdown.ItemSelected -= OnFilterSelected;
            if (!FilterDropdown.IsDisposed)
                FilterDropdown.Destroy();
            base.Destroy();
        }

        internal OnlineUserListFilter GetSelectedFilter() => (OnlineUserListFilter) FilterDropdown.SelectedIndex;

        internal void SetSelectedFilter(OnlineUserListFilter filter)
        {
            if (FilterDropdown.SelectedIndex == (int) filter)
                return;

            FilterDropdown.SelectItem(FilterDropdown.Items[(int) filter], false);
            FilterChanged?.Invoke(this, EventArgs.Empty);
        }

        internal void SetInteractionEnabled(bool enabled)
        {
            SearchBox.InputEnabled = enabled;
            SearchBox.Button.IsInteractionEnabled = enabled;
            FilterDropdown.IsInteractionEnabled = enabled;
            FilterDropdown.IsClickable = enabled;
            if (enabled)
                return;

            SearchBox.Focused = false;
            CloseDropdown();
        }

        internal void CloseDropdown() => FilterDropdown.Close(0);

        private void OnQueryChanged(string query)
        {
            query ??= "";
            if (SearchQuery == query)
                return;

            SearchQuery = query;
            QueryChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnFilterSelected(object sender, DropdownClickedEventArgs args) =>
            FilterChanged?.Invoke(this, EventArgs.Empty);

        private void ApplySearchBoxSize()
        {
            if (SearchBox.Width <= 0 || SearchBox.Height <= 0)
                return;

            var texture = RoundedRectTextureCache.Get(SearchBox.Width, SearchBox.Height, Design.Style.CornerRadius);
            if (SearchBox.Image != texture)
                SearchBox.Image = texture;
            SearchBox.Button.Size = SearchBox.Size;
            SearchBox.ContentContainer.Size = SearchBox.Size;
        }

        private void LayoutFilterDropdown()
        {
            FilterDropdown.Position = new ScalableVector2(
                FilterSlot.ScreenRectangle.X - OverlayHost.ScreenRectangle.X,
                FilterSlot.ScreenRectangle.Y - OverlayHost.ScreenRectangle.Y);
        }

        private void ApplySearchTextColor(bool force)
        {
            var empty = string.IsNullOrEmpty(SearchBox.RawText);
            if (!force && SearchWasEmpty == empty)
                return;

            SearchWasEmpty = empty;
            SearchBox.InputText.Tint = empty ? PlaceholderColor : TextColor;
            SearchBox.InputText.Alpha = 1;
        }

        private void OnSearchBoxSizeChanged(object sender, ScalableVector2 size) => ApplySearchBoxSize();

        private static string GetFilterLabel(OnlineUserListFilter filter)
        {
            switch (filter)
            {
                case OnlineUserListFilter.Friends:
                    return LocalizationManager.Get("Screen_Selection_Friends");
                case OnlineUserListFilter.Country:
                    return LocalizationManager.Get("Screen_Selection_Country");
                default:
                    return LocalizationManager.Get("Screen_Selection_All");
            }
        }
    }
}
