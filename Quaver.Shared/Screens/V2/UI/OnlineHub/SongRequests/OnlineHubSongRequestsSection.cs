using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.Server.Client;
using Quaver.Server.Client.Handlers;
using Quaver.Server.Client.Helpers;
using Quaver.Server.Client.Objects.Twitch;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Graphics;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Graphics.Overlays.Hub;
using Quaver.Shared.Helpers;
using Quaver.Shared.Online;
using Quaver.Shared.Screens;
using Quaver.Shared.Screens.Download;
using Quaver.Shared.Screens.Importing;
using Quaver.Shared.Skinning.V2;
using Wobble;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Buttons;
using Wobble.Graphics.Shaders;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Input;
using Wobble.Managers;

namespace Quaver.Shared.Screens.V2.UI.OnlineHub.SongRequests
{
    internal sealed class OnlineHubSongRequestsSection : Container, IOnlineHubSection
    {
        private const int ActionMenuDepth = -100;

        private const int ActionMenuItemDepth = ActionMenuDepth - 1;

        private static OnlineHubDesign Design => OnlineHubDesign.Default;

        private OnlineHubSongRequestsDesign Config { get; }

        private FlexContainer Layout { get; }

        private Container Header { get; }

        private FlexContainer HeaderLayout { get; }

        private RoundedButton DisplayAlertButton { get; }

        private Sprite AlertToggleTrack { get; }

        private Sprite AlertToggleState { get; }

        private SpriteTextPlus AlertToggleLabel { get; }

        private Texture2D AlertToggleOffTexture { get; }

        private Texture2D AlertToggleOnTexture { get; }

        private RoundedButton ConnectButton { get; }

        private Container HeaderSpacer { get; }

        private RoundedButton ClearButton { get; }

        private SongRequestFeed Feed { get; }

        private HashSet<SongRequest> PlayedRequests { get; } = new HashSet<SongRequest>();

        private RoundedButton ActionMenu { get; set; }

        private FlexContainer ActionMenuLayout { get; set; }

        private SongRequest ActionRequest { get; set; }

        private bool ActionMenuOpenedThisFrame { get; set; }

        private OnlineClient SubscribedClient { get; set; }

        private bool IsActive { get; set; }

        private int RefreshScheduled;

        internal OnlineHubSongRequestsSection(OnlineHubSongRequestsDesign config)
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
            Header = new Container { Parent = Layout };
            HeaderLayout = new FlexContainer
            {
                Parent = Header,
                Direction = FlexDirection.Row,
                AlignItems = FlexAlignItems.Stretch,
                ColumnGap = Design.Toolbar.Gap
            };

            DisplayAlertButton = new RoundedButton((sender, args) => ToggleDisplayAlerts())
            {
                Parent = HeaderLayout,
                Size = new ScalableVector2(1, 1),
                CornerRadius = Design.Style.CornerRadius,
                Tint = SkinV2Color.Parse(Design.Style.ControlColor),
                PerformHoverFade = true,
                IsInteractionEnabled = false
            };
            DisplayAlertButton.SetLabel(FontManager.GetWobbleFont(Design.Style.Font),
                LocalizationManager.Get("Screen_OnlineHub_DisplayAlert"), Design.Style.FontSize,
                SkinV2Color.Parse(Design.Style.TextColor));
            DisplayAlertButton.Label.Alignment = Alignment.MidLeft;
            DisplayAlertButton.Label.X = Config.HeaderHorizontalPadding;

            AlertToggleOffTexture = CreateAlertToggleTexture(SkinV2Color.Parse(Config.AlertToggleOffColor),
                SkinV2Color.Parse(Config.AlertToggleInactiveColor));
            AlertToggleOnTexture = CreateAlertToggleTexture(SkinV2Color.Parse(Config.AlertToggleInactiveColor),
                SkinV2Color.Parse(Config.AlertToggleOnColor));
            AlertToggleTrack = new Sprite
            {
                Parent = DisplayAlertButton,
                Alignment = Alignment.MidRight,
                X = -Config.HeaderHorizontalPadding,
                Size = new ScalableVector2(Config.AlertToggleWidth, Config.AlertToggleHeight),
                Image = AlertToggleOffTexture,
                SetChildrenAlpha = true,
                UsePreviousSpriteBatchOptions = true
            };
            var stateHeight = Config.AlertToggleHeight - Config.AlertTogglePadding * 2;
            AlertToggleState = new Sprite
            {
                Parent = AlertToggleTrack,
                Size = new ScalableVector2(Config.AlertToggleStateWidth, stateHeight),
                Image = RoundedRectTextureCache.Get(Config.AlertToggleStateWidth, stateHeight, stateHeight / 2),
                Tint = SkinV2Color.Parse(Config.AlertToggleStateColor),
                SetChildrenAlpha = true,
                UsePreviousSpriteBatchOptions = true
            };
            AlertToggleLabel = new SpriteTextPlus(FontManager.GetWobbleFont(Design.Style.Font), "", Config.AlertToggleFontSize)
            {
                Parent = AlertToggleState,
                Alignment = Alignment.MidCenter,
                UsePreviousSpriteBatchOptions = true
            };

            ConnectButton = new RoundedButton((sender, args) => HandleTwitchConnection())
            {
                Parent = HeaderLayout,
                Size = new ScalableVector2(1, 1),
                CornerRadius = Design.Style.CornerRadius,
                Tint = SkinV2Color.Parse(Design.Style.ControlColor),
                PerformHoverFade = true,
                IsInteractionEnabled = false
            };
            ConnectButton.SetIcon(UserInterface.TwitchIconWhite, new Vector2(Design.Toolbar.IconSize, Design.Toolbar.IconSize));

            HeaderSpacer = new Container { Parent = HeaderLayout };
            ClearButton = new RoundedButton((sender, args) => ClearRequests())
            {
                Parent = HeaderLayout,
                Size = new ScalableVector2(1, 1),
                CornerRadius = Design.Style.CornerRadius,
                Tint = SkinV2Color.Parse(Design.Style.ControlColor),
                PerformHoverFade = true,
                IsInteractionEnabled = false
            };
            ClearButton.SetIcon(UserInterface.ClearAllIcon, new Vector2(Design.Toolbar.IconSize, Design.Toolbar.IconSize));
            ClearButton.SetLabel(FontManager.GetWobbleFont(Design.Style.Font),
                LocalizationManager.Get("Screen_OnlineHub_ClearAll"), Design.Style.FontSize,
                SkinV2Color.Parse(Design.Style.TextColor));
            HeaderLayout.SetItemOptions(DisplayAlertButton,
                new FlexItemOptions { Basis = Config.DisplayAlertWidth, Shrink = 1 });
            HeaderLayout.SetItemOptions(ConnectButton,
                new FlexItemOptions { Basis = Config.ConnectButtonWidth, Shrink = 1 });
            HeaderLayout.SetItemOptions(HeaderSpacer, new FlexItemOptions { Basis = 0, Grow = 1, Shrink = 0 });
            HeaderLayout.SetItemOptions(ClearButton, new FlexItemOptions { Basis = Design.Toolbar.ClearButtonWidth, Shrink = 1 });
            Layout.SetItemOptions(Header, new FlexItemOptions { Basis = Design.Toolbar.Height, Shrink = 0 });

            Feed = new SongRequestFeed(Config.Feed, Config.Row, ShowActionMenu) { Parent = Layout };
            Layout.SetItemOptions(Feed, new FlexItemOptions { Basis = 0, Grow = 1, Shrink = 1 });
            CreateActionMenu();

            Header.SizeChanged += OnHeaderSizeChanged;
            SizeChanged += OnSizeChanged;
            UpdateAlertToggle();
            UpdateConnectButton();
        }

        public override void Destroy()
        {
            Deactivate();
            Header.SizeChanged -= OnHeaderSizeChanged;
            SizeChanged -= OnSizeChanged;
            base.Destroy();
            AlertToggleOffTexture.Dispose();
            AlertToggleOnTexture.Dispose();
        }

        public override void Update(GameTime gameTime)
        {
            var restoreFeedInteraction = false;
            if (ActionMenu.Visible && ActionMenuOpenedThisFrame)
            {
                ActionMenuOpenedThisFrame = false;
            }
            else if (ActionMenu.Visible)
            {
                var clicked = MouseManager.IsUniqueClick(MouseButton.Left) ||
                              MouseManager.IsUniqueClick(MouseButton.Right);
                if (clicked && !ActionMenu.IsHovered())
                {
                    CloseActionMenu(false);
                    restoreFeedInteraction = true;
                }
            }

            base.Update(gameTime);
            if (restoreFeedInteraction && IsActive)
                Feed.SetActive(true);
        }

        public void Activate()
        {
            if (IsActive)
                return;

            IsActive = true;
            Feed.SetActive(true);
            OnlineManager.Status.ValueChanged += OnConnectionStatusChanged;
            if (ConfigManager.DisplaySongRequestNotifications != null)
                ConfigManager.DisplaySongRequestNotifications.ValueChanged += OnDisplayAlertChanged;

            SubscribeClient();
            RefreshRequests();
            UpdateAlertToggle();
            UpdateConnectButton();
            if (GameBase.Game is QuaverGame game)
                game.OnlineHub.Sections[OnlineHubSectionType.SongRequests].MarkAsRead();
        }

        public void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;
            Feed.SetActive(false);
            DisplayAlertButton.IsInteractionEnabled = false;
            ConnectButton.IsInteractionEnabled = false;
            ClearButton.IsInteractionEnabled = false;
            OnlineManager.Status.ValueChanged -= OnConnectionStatusChanged;
            if (ConfigManager.DisplaySongRequestNotifications != null)
                ConfigManager.DisplaySongRequestNotifications.ValueChanged -= OnDisplayAlertChanged;

            CloseActionMenu();
            UnsubscribeClient();
            OnlineHubPanel.ResetInteractionState(this);
        }

        private void RefreshRequests()
        {
            if (!IsActive)
                return;

            var requests = OnlineManager.GetSongRequestsSnapshot();

            var connected = OnlineManager.Connected;
            var titleKey = "Screen_OnlineHub_NoSongRequestsTitle";
            var descriptionKey = "Screen_OnlineHub_NoSongRequestsDescription";
            if (!connected)
            {
                titleKey = "Screen_OnlineHub_SongRequestsOfflineTitle";
                descriptionKey = "Screen_OnlineHub_SongRequestsOfflineDescription";
            }

            var items = new SongRequestFeedItem[requests.Length];
            for (var index = 0; index < requests.Length; index++)
                items[index] = new SongRequestFeedItem(requests[index].Request, requests[index].ReceivedAt,
                    PlayedRequests.Contains(requests[index].Request));

            Feed.SetItems(items, LocalizationManager.Get(titleKey), LocalizationManager.Get(descriptionKey));
            DisplayAlertButton.IsInteractionEnabled = IsActive;
            ConnectButton.IsInteractionEnabled = IsActive;
            ClearButton.IsInteractionEnabled = IsActive;
        }

        private void ClearRequests()
        {
            OnlineManager.ClearSongRequests();
            PlayedRequests.Clear();
            CloseActionMenu();
            RefreshRequests();
        }

        private void CreateActionMenu()
        {
            const int itemCount = 4;
            var height = Config.ActionMenuPadding * 2 + Config.ActionMenuItemHeight * itemCount +
                         Config.ActionMenuItemGap * (itemCount - 1);
            ActionMenu = new RoundedButton
            {
                Parent = this,
                Size = new ScalableVector2(Config.ActionMenuWidth, height),
                CornerRadius = Design.Style.CornerRadius,
                Tint = SkinV2Color.Parse(Config.ActionMenuColor),
                PerformHoverFade = false,
                IsClickable = false,
                IsInteractionEnabled = false,
                Visible = false,
                UpdateWhenInvisible = false,
                Depth = ActionMenuDepth
            };
            ActionMenuLayout = new FlexContainer
            {
                Parent = ActionMenu,
                Position = new ScalableVector2(Config.ActionMenuPadding, Config.ActionMenuPadding),
                Size = new ScalableVector2(Config.ActionMenuWidth - Config.ActionMenuPadding * 2,
                    height - Config.ActionMenuPadding * 2),
                Direction = FlexDirection.Column,
                AlignItems = FlexAlignItems.Stretch,
                RowGap = Config.ActionMenuItemGap,
                UsePreviousSpriteBatchOptions = true
            };

            CreateActionButton("Screen_Selection_Play", Design.Style.TextColor, PlayRequest);
            CreateActionButton("Screen_OnlineHub_RequesterProfile", Config.ProfileActionColor, VisitRequesterProfile);
            CreateActionButton("Screen_Selection_OnlineListing", Config.ListingActionColor, VisitOnlineListing);
            CreateActionButton("Screen_Editor_Delete", Config.DeleteActionColor, DeleteRequest);
            ActionMenuLayout.RefreshLayout();
        }

        private void CreateActionButton(string localizationKey, string textColor, Action<SongRequest> action)
        {
            var button = new RoundedButton((sender, args) => RunAction(action))
            {
                Parent = ActionMenuLayout,
                Size = new ScalableVector2(1, 1),
                CornerRadius = Design.Style.CornerRadius,
                Tint = SkinV2Color.Parse(Config.ActionMenuItemColor),
                PerformHoverFade = true,
                UsePreviousSpriteBatchOptions = true,
                Depth = ActionMenuItemDepth
            };
            button.SetLabel(FontManager.GetWobbleFont(Design.Style.Font), LocalizationManager.Get(localizationKey),
                Design.Style.SmallFontSize, SkinV2Color.Parse(textColor));
            ActionMenuLayout.SetItemOptions(button, new FlexItemOptions { Basis = Config.ActionMenuItemHeight, Shrink = 0 });
        }

        private void ShowActionMenu(SongRequest request)
        {
            ActionRequest = request;
            ActionMenuOpenedThisFrame = true;
            ActionMenu.Visible = true;
            ActionMenu.IsInteractionEnabled = true;
            Feed.SetActive(false);
            ActionMenu.X = MathHelper.Clamp(MouseManager.CurrentState.X - AbsolutePosition.X - ActionMenu.Width, 0,
                Math.Max(0, Width - ActionMenu.Width));
            ActionMenu.Y = MathHelper.Clamp(MouseManager.CurrentState.Y - AbsolutePosition.Y, 0,
                Math.Max(0, Height - ActionMenu.Height));
        }

        private void CloseActionMenu(bool restoreFeedInteraction = true)
        {
            if (ActionMenu == null)
                return;

            ActionRequest = null;
            ActionMenuOpenedThisFrame = false;
            ActionMenu.Visible = false;
            ActionMenu.IsInteractionEnabled = false;
            OnlineHubPanel.ResetInteractionState(ActionMenu);
            if (restoreFeedInteraction && IsActive)
                Feed.SetActive(true);
        }

        private void RunAction(Action<SongRequest> action)
        {
            var request = ActionRequest;
            CloseActionMenu();
            if (request != null)
                action(request);
        }

        private static void VisitRequesterProfile(SongRequest request) => BrowserHelper.OpenURL($"https://twitch.tv/{request.TwitchUsername}");

        private static void VisitOnlineListing(SongRequest request)
        {
            switch ((MapGame) request.Game)
            {
                case MapGame.Quaver:
                    BrowserHelper.OpenURL($"https://quavergame.com/mapsets/{request.MapsetId}");
                    break;
                case MapGame.Osu:
                    BrowserHelper.OpenURL($"https://osu.ppy.sh/beatmapsets/{request.MapsetId}");
                    break;
            }
        }

        private void DeleteRequest(SongRequest request)
        {
            OnlineManager.RemoveSongRequest(request);
            PlayedRequests.Remove(request);
            RefreshRequests();
        }

        private void PlayRequest(SongRequest request)
        {
            if (GameBase.Game is not QuaverGame game || game.CurrentScreen.Type != QuaverScreenType.Select)
            {
                NotificationManager.Show(NotificationLevel.Warning,
                    LocalizationManager.Get("Screen_OnlineHub_SongRequestRequiresSelection"));
                return;
            }

            PlayedRequests.Add(request);
            RefreshRequests();
            if (!string.IsNullOrEmpty(request.MapMd5))
            {
                var map = MapManager.FindMapFromMd5(request.MapMd5);
                if (map != null)
                {
                    MapManager.PlaySongRequest(request, map);
                    return;
                }
            }

            switch ((MapGame) request.Game)
            {
                case MapGame.Quaver:
                    PlayOrDownloadQuaverRequest(game, request);
                    break;
                case MapGame.Osu:
                    BrowserHelper.OpenURL($"https://osu.ppy.sh/beatmapsets/{request.MapsetId}", true);
                    break;
            }
        }

        private static void PlayOrDownloadQuaverRequest(QuaverGame game, SongRequest request)
        {
            if (MapManager.Mapsets.Count != 0)
            {
                var mapset = MapManager.Mapsets.Find(x => x.Maps.Count > 0 && x.Maps[0].Game == MapGame.Quaver &&
                                                          x.Maps[0].MapSetId == request.MapsetId);
                if (mapset != null)
                {
                    MapManager.PlaySongRequest(request, mapset.Maps[0]);
                    return;
                }
            }

            if (MapsetDownloadManager.IsMapsetInQueue(request.MapsetId))
                return;

            var download = MapsetDownloadManager.Download(request.MapsetId, request.Artist, request.Title);
            download.Status.ValueChanged += (sender, args) =>
            {
                if (args.Value.Status != FileDownloaderStatus.Complete ||
                    game.CurrentScreen.Type != QuaverScreenType.Select)
                    return;

                game.CurrentScreen.Exit(() => QuaverScreenFactory.CreateImporting(null, true));
                var dialog = DialogManager.Dialogs.Find(x => x is OnlineHubOverlay) as OnlineHubOverlay;
                dialog?.Close();
            };
        }

        private void ToggleDisplayAlerts()
        {
            var setting = ConfigManager.DisplaySongRequestNotifications;
            if (setting != null)
                setting.Value = !setting.Value;
        }

        private void UpdateAlertToggle()
        {
            var enabled = ConfigManager.DisplaySongRequestNotifications?.Value ?? false;
            AlertToggleTrack.Image = enabled ? AlertToggleOnTexture : AlertToggleOffTexture;
            AlertToggleState.Alignment = enabled ? Alignment.MidRight : Alignment.MidLeft;
            AlertToggleState.X = enabled ? -Config.AlertTogglePadding : Config.AlertTogglePadding;
            var stateKey = enabled ? "SkinEditor_On" : "SkinEditor_Off";
            var textColor = enabled ? Config.AlertToggleOnTextColor : Config.AlertToggleOffTextColor;
            AlertToggleLabel.Text = LocalizationManager.Get(stateKey).ToUpperInvariant();
            AlertToggleLabel.Tint = SkinV2Color.Parse(textColor);
        }

        private Texture2D CreateAlertToggleTexture(Color leftColor, Color rightColor)
        {
            var width = Math.Max(1, (int) Config.AlertToggleWidth);
            var height = Math.Max(1, (int) Config.AlertToggleHeight);
            var radius = height / 2f;
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var sampleX = MathHelper.Clamp(x + 0.5f, radius, width - radius);
                    var distanceX = x + 0.5f - sampleX;
                    var distanceY = y + 0.5f - radius;
                    var coverage = MathHelper.Clamp(radius + 0.5f - MathF.Sqrt(distanceX * distanceX +
                        distanceY * distanceY), 0, 1);
                    var progress = width == 1 ? 0 : x / (float) (width - 1);
                    pixels[y * width + x] = Color.Lerp(leftColor, rightColor, progress) * coverage;
                }
            }

            var texture = new Texture2D(GameBase.Game.GraphicsDevice, width, height, false, SurfaceFormat.Color);
            texture.SetData(pixels);
            return texture;
        }

        private void HandleTwitchConnection()
        {
            if (string.IsNullOrEmpty(OnlineManager.TwitchUsername))
            {
                BrowserHelper.OpenURL(OnlineClient.CONNECT_TWITCH_URL, true);
                return;
            }

            if (!OnlineManager.Connected)
            {
                NotificationManager.Show(NotificationLevel.Error,
                    LocalizationManager.Get("Screen_OnlineHub_TwitchUnlinkRequiresLogin"));
                return;
            }

            var title = LocalizationManager.Get("Screen_OnlineHub_TwitchUnlinkTitle");
            var description = string.Format(LocalizationManager.Get("Screen_OnlineHub_TwitchUnlinkConfirmation"),
                OnlineManager.TwitchUsername);
            DialogManager.Show(new YesNoDialog(title, description, () => OnlineManager.Client?.UnlinkTwitchAccount()));
        }

        private void UpdateConnectButton()
        {
            var key = "Screen_OnlineHub_Connect";
            if (!string.IsNullOrEmpty(OnlineManager.TwitchUsername))
                key = "Screen_OnlineHub_Unlink";

            ConnectButton.SetLabel(FontManager.GetWobbleFont(Design.Style.Font), LocalizationManager.Get(key),
                Design.Style.FontSize, SkinV2Color.Parse(Design.Style.TextColor));
        }

        private void ScheduleRefresh()
        {
            if (Interlocked.Exchange(ref RefreshScheduled, 1) != 0)
                return;

            ScheduleUpdate(() =>
            {
                Interlocked.Exchange(ref RefreshScheduled, 0);
                RefreshRequests();
            });
        }

        private void SubscribeClient()
        {
            var client = OnlineManager.Client;
            if (client == null || ReferenceEquals(SubscribedClient, client))
                return;

            UnsubscribeClient();
            SubscribedClient = client;
            SubscribedClient.OnSongRequestReceived += OnSongRequestReceived;
            SubscribedClient.OnTwitchConnectionReceived += OnTwitchConnectionReceived;
        }

        private void UnsubscribeClient()
        {
            if (SubscribedClient == null)
                return;

            SubscribedClient.OnSongRequestReceived -= OnSongRequestReceived;
            SubscribedClient.OnTwitchConnectionReceived -= OnTwitchConnectionReceived;
            SubscribedClient = null;
        }

        private void OnHeaderSizeChanged(object sender, ScalableVector2 size)
        {
            HeaderLayout.Position = new ScalableVector2(0, Design.Toolbar.Padding);
            HeaderLayout.Size = new ScalableVector2(size.X.Value,
                Math.Max(0, size.Y.Value - Design.Toolbar.Padding * 2));
            HeaderLayout.RefreshLayout();
        }

        private void OnSizeChanged(object sender, ScalableVector2 size)
        {
            Layout.Size = size;
            Layout.RefreshLayout();
        }

        private void OnSongRequestReceived(object sender, SongRequestEventArgs args) => ScheduleRefresh();

        private void OnTwitchConnectionReceived(object sender, TwitchConnectionEventArgs args) => ScheduleUpdate(UpdateConnectButton);

        private void OnDisplayAlertChanged(object sender, BindableValueChangedEventArgs<bool> args) => ScheduleUpdate(UpdateAlertToggle);

        private void OnConnectionStatusChanged(object sender, BindableValueChangedEventArgs<ConnectionStatus> args)
        {
            SubscribeClient();
            ScheduleRefresh();
        }

    }
}
