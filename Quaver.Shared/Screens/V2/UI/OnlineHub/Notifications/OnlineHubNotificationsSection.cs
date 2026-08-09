using System;
using System.Threading;
using Quaver.Shared.Graphics.Notifications;
using Wobble;
using Wobble.Graphics;
using Wobble.Managers;

namespace Quaver.Shared.Screens.V2.UI.OnlineHub.Notifications
{
    internal sealed class OnlineHubNotificationsSection : Container, IOnlineHubSection
    {
        private static OnlineHubDesign Design => OnlineHubDesign.Default;

        private OnlineHubNotificationsDesign Config { get; }

        private NotificationHistoryStore Store => NotificationManager.History;

        private FlexContainer Layout { get; }

        private NotificationHeader Header { get; }

        private NotificationFeed Feed { get; }

        private bool IsActive { get; set; }

        private int RefreshScheduled;

        internal OnlineHubNotificationsSection(OnlineHubNotificationsDesign config)
        {
            Config = config;
            DestroyIfParentIsNull = false;
            Layout = new FlexContainer
            {
                Parent = this,
                Direction = FlexDirection.Column,
                AlignItems = FlexAlignItems.Stretch,
                RowGap = Design.SectionGap
            };
            Header = new NotificationHeader(Config, Store.Clear) { Parent = Layout };
            Layout.SetItemOptions(Header, new FlexItemOptions { Basis = Design.Toolbar.Height, Shrink = 0 });

            Feed = new NotificationFeed(Design.Feed, Config.Row, Config.MultiplayerInvite, Store.Remove)
            {
                Parent = Layout,
                InputEnabled = false
            };
            Layout.SetItemOptions(Feed, new FlexItemOptions { Basis = 0, Grow = 1, Shrink = 1 });

            Header.SelectedViewChanged += OnSelectedViewChanged;
            SizeChanged += OnSizeChanged;
        }

        public override void Destroy()
        {
            Deactivate();
            Header.SelectedViewChanged -= OnSelectedViewChanged;
            SizeChanged -= OnSizeChanged;
            base.Destroy();
        }

        public void Activate()
        {
            if (IsActive)
                return;

            IsActive = true;
            Feed.InputEnabled = true;
            NotificationManager.SetOnlineHubNotificationSectionOpen(true);
            Store.SetHistoryViewOpen(Header.SelectedView == NotificationView.History);
            Store.Changed += OnStoreChanged;
            RefreshFeed();
        }

        public void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;
            Feed.InputEnabled = false;
            NotificationManager.SetOnlineHubNotificationSectionOpen(false);
            Store.Changed -= OnStoreChanged;
            Store.MarkAllSeen();
            Store.SetHistoryViewOpen(false);
            OnlineHubPanel.ResetInteractionState(this);
        }

        private void RefreshFeed()
        {
            if (!IsActive)
                return;

            var items = Header.SelectedView == NotificationView.New ? Store.GetNew() : Store.GetHistory();
            Feed.SetItems(items, LocalizationManager.Get("Screen_OnlineHub_NoNotificationsTitle"),
                LocalizationManager.Get("Screen_OnlineHub_NoNotificationsDescription"));
        }

        private void ScheduleRefresh()
        {
            if (Interlocked.Exchange(ref RefreshScheduled, 1) != 0)
                return;

            ScheduleUpdate(() =>
            {
                Interlocked.Exchange(ref RefreshScheduled, 0);
                RefreshFeed();
            });
        }

        private void OnSelectedViewChanged(object sender, EventArgs args)
        {
            Store.SetHistoryViewOpen(Header.SelectedView == NotificationView.History);
            ScheduleRefresh();
        }

        private void OnStoreChanged(object sender, EventArgs args) => ScheduleRefresh();

        private void OnSizeChanged(object sender, ScalableVector2 size)
        {
            Layout.Size = size;
            Layout.RefreshLayout();
        }

    }
}
