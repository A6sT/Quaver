using System;
using Quaver.Shared.Assets;
using Quaver.Shared.Screens.V2.UI.OnlineHub.Notifications;
using Quaver.Shared.Screens.V2.UI.OnlineHub.SongRequests;
using Quaver.Shared.Screens.V2.UI.OnlineHub.Users;
using Quaver.Shared.Skinning.V2;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.UI.Buttons;

namespace Quaver.Shared.Screens.V2.UI.OnlineHub
{
    internal enum OnlineHubTab
    {
        Notifications,
        Users,
        SongRequests
    }

    internal interface IOnlineHubSection
    {
        void Activate();

        void Deactivate();
    }

    internal sealed class OnlineHubPanel : Container
    {
        private const int SectionCount = 3;

        private OnlineHubDesign Config { get; } = OnlineHubDesign.Default;

        private Sprite Background { get; }

        private FlexContainer Layout { get; }

        private OnlineHubTabNavigation TabNavigation { get; }

        private Container[] Sections { get; } = new Container[SectionCount];

        private Container ActiveSection { get; set; }

        private Container Content { get; }

        internal OnlineHubTab SelectedTab => TabNavigation.SelectedTab;

        internal OnlineHubPanel(OnlineHubTab initialTab = OnlineHubTab.Notifications)
        {
            Background = new Sprite
            {
                Parent = this,
                Image = UserInterface.BlankBox,
                Tint = SkinV2Color.Parse(Config.Style.SurfaceColor)
            };
            Layout = new FlexContainer
            {
                Parent = this,
                Direction = FlexDirection.Column,
                AlignItems = FlexAlignItems.Stretch,
                RowGap = Config.SectionGap
            };
            TabNavigation = new OnlineHubTabNavigation(Config.Toolbar) { Parent = Layout };
            Layout.SetItemOptions(TabNavigation,
                new FlexItemOptions { Basis = Config.Toolbar.Height, Shrink = 0 });

            Content = new Container { Parent = Layout };
            Layout.SetItemOptions(Content, new FlexItemOptions
            {
                Basis = 0,
                Grow = 1,
                Shrink = 1,
                AlignSelf = FlexAlignSelf.Center
            });

            TabNavigation.Select(initialTab);
            TabNavigation.SelectedTabChanged += OnSelectedTabChanged;
            Content.SizeChanged += OnContentSizeChanged;
            SelectSection(TabNavigation.SelectedTab);
        }

        public override void Destroy()
        {
            TabNavigation.SelectedTabChanged -= OnSelectedTabChanged;
            Content.SizeChanged -= OnContentSizeChanged;
            if (ActiveSection is IOnlineHubSection activeSection)
                activeSection.Deactivate();

            foreach (var section in Sections)
                section?.Destroy();

            ActiveSection = null;
            base.Destroy();
        }

        internal void Resize(float width, float height)
        {
            if (Math.Abs(Width - width) <= 0.001f && Math.Abs(Height - height) <= 0.001f)
                return;

            Size = new ScalableVector2(width, height);
            Background.Size = Size;
            Layout.Size = new ScalableVector2(width, Math.Max(0, height - Config.Padding));
            Content.Width = Math.Max(0, width - Config.Padding * 2);
            Layout.RefreshLayout();
        }

        internal void SelectTab(OnlineHubTab tab) => TabNavigation.Select(tab);

        private void SelectSection(OnlineHubTab tab)
        {
            var section = Sections[(int) tab];
            if (section != null && ReferenceEquals(ActiveSection, section))
                return;

            if (ActiveSection != null)
            {
                if (ActiveSection is IOnlineHubSection activeSection)
                    activeSection.Deactivate();
                ActiveSection.Parent = null;
            }

            section ??= Sections[(int) tab] = CreateSection(tab);
            section.Parent = Content;
            section.Size = Content.Size;
            if (section is IOnlineHubSection newSection)
                newSection.Activate();
            ActiveSection = section;
        }

        private Container CreateSection(OnlineHubTab tab)
        {
            if (tab == OnlineHubTab.Notifications)
                return new OnlineHubNotificationsSection(Config.Notifications);

            if (tab == OnlineHubTab.Users)
                return new OnlineHubUsersSection(Config.Users);

            return new OnlineHubSongRequestsSection(Config.SongRequests);
        }

        private void OnSelectedTabChanged(object sender, EventArgs args) => SelectSection(TabNavigation.SelectedTab);

        private void OnContentSizeChanged(object sender, ScalableVector2 size)
        {
            if (ActiveSection != null)
                ActiveSection.Size = size;
        }

        internal static void ResetInteractionState(Drawable drawable)
        {
            if (drawable is Button button)
                button.ResetInteractionState();

            foreach (var child in drawable.Children)
                ResetInteractionState(child);
        }
    }
}
