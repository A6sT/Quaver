using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Quaver.Server.Client;
using Quaver.Server.Client.Handlers;
using Quaver.Server.Client.Structures;
using Quaver.Shared.Config;
using Quaver.Shared.Graphics.Form.Dropdowns;
using Quaver.Shared.Graphics.Overlays.Hub.OnlineUsers;
using Quaver.Shared.Graphics.Overlays.Hub.OnlineUsers.Scrolling;
using Quaver.Shared.Online;
using Quaver.Shared.Screens.V2.UI.OnlineHub;
using Wobble;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Input;
using Wobble.Managers;

namespace Quaver.Shared.Screens.V2.UI.OnlineHub.Users
{
    internal sealed class OnlineHubUsersSection : Container, IOnlineHubSection
    {
        private static OnlineHubDesign Design => OnlineHubDesign.Default;

        private OnlineHubUsersDesign Config { get; }

        private FlexContainer Layout { get; }

        private Container ControlsHost { get; }

        private UserControls Controls { get; }

        private UserFeed Feed { get; }

        private OnlineClient SubscribedClient { get; set; }

        private DrawableOnlineUserRightClickOptions ActiveUserMenu { get; set; }

        private bool IsActive { get; set; }

        private bool WasFilterDropdownOpen { get; set; }

        private int RefreshScheduled;

        private int StatusRefreshScheduled;

        internal OnlineHubUsersSection(OnlineHubUsersDesign config)
        {
            Config = config;
            DestroyIfParentIsNull = false;
            var initialFilter = ConfigManager.OnlineUserListFilterType?.Value ?? OnlineUserListFilter.All;
            Layout = new FlexContainer
            {
                Parent = this,
                Direction = FlexDirection.Column,
                AlignItems = FlexAlignItems.Stretch,
                RowGap = Design.SectionGap
            };
            ControlsHost = new Container { Parent = Layout };
            Controls = new UserControls(this, initialFilter, Config.Controls)
            {
                Parent = ControlsHost,
                Position = new ScalableVector2(0, Design.Toolbar.Padding)
            };
            Layout.SetItemOptions(ControlsHost, new FlexItemOptions { Basis = Design.Toolbar.Height, Shrink = 0 });
            Feed = new UserFeed(Design.Feed, Config.Row, OpenUserMenu) { Parent = Layout };
            Layout.SetItemOptions(Feed, new FlexItemOptions { Basis = 0, Grow = 1, Shrink = 1 });

            Controls.QueryChanged += OnQueryChanged;
            Controls.FilterChanged += OnFilterChanged;
            SizeChanged += OnSizeChanged;
            Controls.SetInteractionEnabled(false);
        }

        public override void Update(GameTime gameTime)
        {
            if (ActiveUserMenu != null &&
                (MouseManager.IsUniqueClick(MouseButton.Left) || MouseManager.IsUniqueClick(MouseButton.Right)) &&
                !Contains(ActiveUserMenu.ItemContainer.ScreenRectangle, MouseManager.CurrentState.Position))
                DismissUserMenu();

            base.Update(gameTime);
            if (ActiveUserMenu != null && !ActiveUserMenu.Opened)
                DismissUserMenu();

            var dropdownOpened = Controls.DropdownOpened;
            if (WasFilterDropdownOpen == dropdownOpened)
                return;

            WasFilterDropdownOpen = dropdownOpened;
            Feed.SetActive(IsActive && !dropdownOpened);
        }

        public override void Destroy()
        {
            Deactivate();
            Controls.QueryChanged -= OnQueryChanged;
            Controls.FilterChanged -= OnFilterChanged;
            SizeChanged -= OnSizeChanged;
            base.Destroy();
        }

        public void Activate()
        {
            if (IsActive)
                return;

            IsActive = true;
            Controls.SetInteractionEnabled(true);
            Feed.SetActive(true);
            OnlineManager.Status.ValueChanged += OnConnectionStatusChanged;
            OnlineManager.FriendsListUserChanged += OnFriendsListUserChanged;
            if (ConfigManager.OnlineUserListFilterType != null)
                ConfigManager.OnlineUserListFilterType.ValueChanged += OnConfiguredFilterChanged;
            SubscribeClient();
            RefreshUsers(true);
        }

        public void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;
            Controls.SetInteractionEnabled(false);
            Controls.CloseDropdown();
            WasFilterDropdownOpen = false;
            Feed.SetActive(false);
            OnlineManager.Status.ValueChanged -= OnConnectionStatusChanged;
            OnlineManager.FriendsListUserChanged -= OnFriendsListUserChanged;
            if (ConfigManager.OnlineUserListFilterType != null)
                ConfigManager.OnlineUserListFilterType.ValueChanged -= OnConfiguredFilterChanged;
            UnsubscribeClient();
            DismissUserMenu();
            OnlineHubPanel.ResetInteractionState(this);
        }

        private void RefreshUsers(bool preserveAnchor)
        {
            if (!IsActive)
                return;

            var connected = OnlineManager.Connected && OnlineManager.OnlineUsers != null;
            var users = new List<User>();
            if (connected)
            {
                try
                {
                    users = OnlineManager.OnlineUsers.Values.ToList();
                }
                catch (InvalidOperationException)
                {
                    ScheduleUserRefresh();
                    return;
                }
            }

            if (connected)
                FilterUsers(users);

            users.Sort(CompareUsers);
            var titleKey = connected ? "Screen_OnlineHub_NoUsersTitle" : "Screen_OnlineHub_UsersOfflineTitle";
            var descriptionKey = "Screen_OnlineHub_UsersOfflineDescription";
            if (connected)
                descriptionKey = "Screen_OnlineHub_NoUsersDescription";
            Feed.SetItems(users.ToArray(), preserveAnchor, LocalizationManager.Get(titleKey),
                LocalizationManager.Get(descriptionKey));
        }

        private void FilterUsers(List<User> users)
        {
            var filter = Controls.GetSelectedFilter();
            if (filter == OnlineUserListFilter.Friends)
            {
                HashSet<int> friends;
                var friendsList = OnlineManager.FriendsList;
                if (friendsList == null)
                    friends = new HashSet<int>();
                else
                {
                    lock (friendsList)
                        friends = new HashSet<int>(friendsList);
                }
                users.RemoveAll(user => user?.OnlineUser == null || !friends.Contains(user.OnlineUser.Id));
            }
            else if (filter == OnlineUserListFilter.Country)
            {
                var country = OnlineManager.Self?.OnlineUser?.CountryFlag;
                users.RemoveAll(user => user?.OnlineUser?.CountryFlag != country);
            }

            var query = Controls.SearchQuery;
            if (!string.IsNullOrWhiteSpace(query))
            {
                users.RemoveAll(user => user?.OnlineUser?.Username == null ||
                                        user.OnlineUser.Username.IndexOf(query,
                                            StringComparison.OrdinalIgnoreCase) < 0);
            }
        }

        private void ScheduleUserRefresh()
        {
            if (Interlocked.Exchange(ref RefreshScheduled, 1) != 0)
                return;

            ScheduleUpdate(() =>
            {
                Interlocked.Exchange(ref RefreshScheduled, 0);
                RefreshUsers(true);
            });
        }

        private void ScheduleStatusRefresh()
        {
            if (Interlocked.Exchange(ref StatusRefreshScheduled, 1) != 0)
                return;

            ScheduleUpdate(() =>
            {
                Interlocked.Exchange(ref StatusRefreshScheduled, 0);
                if (IsActive)
                    Feed.RefreshVisibleStatuses();
            });
        }

        private void SubscribeClient()
        {
            var client = OnlineManager.Client;
            if (client == null || ReferenceEquals(SubscribedClient, client))
                return;

            UnsubscribeClient();
            SubscribedClient = client;
            SubscribedClient.OnUsersOnline += OnUsersOnline;
            SubscribedClient.OnUserConnected += OnUserConnected;
            SubscribedClient.OnUserDisconnected += OnUserDisconnected;
            SubscribedClient.OnUserFriendsListReceived += OnFriendsListReceived;
            SubscribedClient.OnUserInfoReceived += OnUserInfoReceived;
            SubscribedClient.OnUserStatusReceived += OnUserStatusReceived;
        }

        private void UnsubscribeClient()
        {
            if (SubscribedClient == null)
                return;

            SubscribedClient.OnUsersOnline -= OnUsersOnline;
            SubscribedClient.OnUserConnected -= OnUserConnected;
            SubscribedClient.OnUserDisconnected -= OnUserDisconnected;
            SubscribedClient.OnUserFriendsListReceived -= OnFriendsListReceived;
            SubscribedClient.OnUserInfoReceived -= OnUserInfoReceived;
            SubscribedClient.OnUserStatusReceived -= OnUserStatusReceived;
            SubscribedClient = null;
        }

        private void OpenUserMenu(User user)
        {
            if (!IsActive || user?.OnlineUser == null)
                return;

            Controls.CloseDropdown();
            DismissUserMenu();
            ActiveUserMenu = new DrawableOnlineUserRightClickOptions(user)
            {
                Parent = this,
                Visible = true
            };
            ActiveUserMenu.ItemSelected += OnUserMenuItemSelected;
            ActiveUserMenu.ItemContainer.Height = 0;

            var x = MouseManager.CurrentState.X - ScreenRectangle.X - ActiveUserMenu.Width;
            var y = MouseManager.CurrentState.Y - ScreenRectangle.Y;
            x = MathHelper.Clamp(x, 0, Math.Max(0, Width - ActiveUserMenu.Width));
            y = MathHelper.Clamp(y, 0, Math.Max(0, Height - ActiveUserMenu.OpenHeight));
            ActiveUserMenu.Position = new ScalableVector2(x, y);
            ActiveUserMenu.Open(200);
        }

        private void DismissUserMenu()
        {
            if (ActiveUserMenu == null)
                return;

            ActiveUserMenu.ItemSelected -= OnUserMenuItemSelected;
            ActiveUserMenu.Destroy();
            ActiveUserMenu = null;
        }

        private void OnSizeChanged(object sender, ScalableVector2 size)
        {
            Layout.Size = size;
            Layout.RefreshLayout();
            var controlsWidth = Math.Max(0, ControlsHost.Width - Design.Feed.ScrollbarWidth - Design.Feed.RowGap);
            Controls.Size = new ScalableVector2(controlsWidth, Design.Toolbar.ControlHeight);
        }

        private void OnQueryChanged(object sender, EventArgs args) => RefreshUsers(false);

        private void OnFilterChanged(object sender, EventArgs args)
        {
            var filter = Controls.GetSelectedFilter();
            if (ConfigManager.OnlineUserListFilterType != null &&
                ConfigManager.OnlineUserListFilterType.Value != filter)
                ConfigManager.OnlineUserListFilterType.Value = filter;
            RefreshUsers(false);
        }

        private void OnConfiguredFilterChanged(object sender,
            BindableValueChangedEventArgs<OnlineUserListFilter> args)
        {
            if (Controls.GetSelectedFilter() != args.Value)
                Controls.SetSelectedFilter(args.Value);
        }

        private void OnConnectionStatusChanged(object sender,
            BindableValueChangedEventArgs<ConnectionStatus> args)
        {
            SubscribeClient();
            ScheduleUserRefresh();
        }

        private void OnUsersOnline(object sender, UsersOnlineEventArgs args) => ScheduleUserRefresh();

        private void OnUserConnected(object sender, UserConnectedEventArgs args) => ScheduleUserRefresh();

        private void OnUserDisconnected(object sender, UserDisconnectedEventArgs args) => ScheduleUserRefresh();

        private void OnFriendsListReceived(object sender, UserFriendsListEventArgs args) => ScheduleUserRefresh();

        private void OnFriendsListUserChanged(object sender, FriendsListUserChangedEventArgs args) =>
            ScheduleUserRefresh();

        private void OnUserInfoReceived(object sender, UserInfoEventArgs args) => ScheduleUserRefresh();

        private void OnUserStatusReceived(object sender, UserStatusEventArgs args) => ScheduleStatusRefresh();

        private void OnUserMenuItemSelected(object sender, DropdownClickedEventArgs args) =>
            ScheduleUpdate(DismissUserMenu);

        private static int CompareUsers(User left, User right)
        {
            var leftName = left?.OnlineUser?.Username;
            var rightName = right?.OnlineUser?.Username;
            if (leftName == null && rightName != null)
                return 1;
            if (leftName != null && rightName == null)
                return -1;

            var result = StringComparer.OrdinalIgnoreCase.Compare(leftName, rightName);
            if (result != 0)
                return result;

            return (left?.OnlineUser?.Id ?? -1).CompareTo(right?.OnlineUser?.Id ?? -1);
        }

        private static bool Contains(MonoGame.Extended.RectangleF rectangle, Vector2 point) =>
            point.X >= rectangle.Left && point.X <= rectangle.Right &&
            point.Y >= rectangle.Top && point.Y <= rectangle.Bottom;
    }
}
