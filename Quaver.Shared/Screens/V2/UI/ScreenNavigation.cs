using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Quaver.Server.Client;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Graphics;
using Quaver.Shared.Graphics.Menu.Border.Components.Users;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Graphics.Overlays.Hub;
using Quaver.Shared.Helpers;
using Quaver.Shared.Online;
using Quaver.Shared.Screens.Main.UI;
using Quaver.Shared.Screens.Options;
using Quaver.Shared.Screens.V2.SkinEditor;
using Quaver.Shared.Skinning;
using Quaver.Shared.Skinning.V2;
using Steamworks;
using Wobble;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Buttons;
using Wobble.Graphics.Shaders;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Graphics.UI.Navigation;
using Wobble.Graphics.UI.Tooltips;
using Wobble.Input;
using Wobble.Managers;
using Wobble.Screens;
using Wobble.Window;

namespace Quaver.Shared.Screens.V2.UI
{
    /// <summary>
    ///     Shared top and bottom chrome for replacement screens.
    /// </summary>
    internal sealed class ScreenNavigation : Container
    {
        public const string ElementKey = "quaver-screen-navigation";

        private SkinStoreV2Lease Skin { get; }

        private SkinV2NavigationConfig Config { get; }

        private NavigationBar TopBar { get; }

        private NavigationBar BottomBar { get; }

        private ProfileControl ProfileButton { get; }

        private RoundedButton DonateButton { get; }

        private RoundedButton HubButton { get; }

        private Texture2D HubListIcon { get; }

        private Texture2D HubMenuIcon { get; }

        private OnlineHub SubscribedOnlineHub { get; set; }

        private List<Drawable> NavigationButtons { get; } = new List<Drawable>();

        private List<Drawable> FooterButtons { get; } = new List<Drawable>();

        private List<Drawable> TopLayoutButtons { get; } = new List<Drawable>();

        private FooterLayout? CurrentFooterLayout { get; set; }

        private TopLayout? CurrentTopLayout { get; set; }

        private QuaverScreenType CurrentActiveScreen { get; set; } = QuaverScreenType.None;

        private ScreenNavigation(SkinV2Config previewConfig = null)
        {
            Skin = SkinManager.AcquireV2();
            Config = (previewConfig ?? Skin.Config).Shared.Navigation;
            Size = new ScalableVector2(WindowManager.Width, WindowManager.Height);

            TopBar = CreateBar(Alignment.TopLeft, Config.Bar);
            BottomBar = CreateBar(Alignment.BotLeft, Config.Footer);

            ProfileButton = new ProfileControl(Config.Profile,
                SkinV2Color.Parse(Config.Button.BackgroundColor),
                UserInterface.OfflineAvatar,
                Config.Button.Size);
            HubListIcon = GlobalIcons.Get(GlobalIcon.BurgerRedDot);
            HubMenuIcon = GlobalIcons.Get(GlobalIcon.Burger);
            DonateButton = AddIconButton(TopBar, NavigationBarRegion.Right,
                GlobalIcons.Get(GlobalIcon.Heart),
                LocalizationManager.Get("Screen_Main_Menu_Donate"), ShowDonateMessage,
                TooltipAnchor.BottomCenter);
            HubButton = AddIconButton(TopBar, NavigationBarRegion.Right, HubMenuIcon,
                "Online Hub", ToggleOnlineHub, TooltipAnchor.BottomCenter);

            ShowMainTopBar();
            ShowDefaultFooter();
        }

        public void ShowMainTopBar()
        {
            if (CurrentTopLayout == TopLayout.Main)
                return;

            ClearTopLayout();

            AddTopLayoutIconButton(GlobalIcon.Jukebox,
                LocalizationManager.Get("Screen_Main_Menu_Jukebox"), OpenMusicPlayer);
            AddTopLayoutIconButton(GlobalIcon.Chat,
                LocalizationManager.Get("Screen_Options_ToggleChatOverlay"), ToggleChat);
            AddSharedRightControls();

            CurrentTopLayout = TopLayout.Main;
            CurrentActiveScreen = QuaverScreenType.Menu;
        }

        public void ShowApplicationTopBar(QuaverScreenType activeScreen)
        {
            if (CurrentTopLayout == TopLayout.Application && CurrentActiveScreen == activeScreen)
                return;

            ClearTopLayout();

            AddApplicationButton(GlobalIcon.Home, "Screen_Main_Menu_Home", NavigateHome,
                activeScreen == QuaverScreenType.Menu);
            AddApplicationButton(GlobalIcon.SinglePlayer, "Screen_Main_SinglePlayer",
                NavigateSinglePlayer, activeScreen == QuaverScreenType.Select);
            AddApplicationButton(GlobalIcon.Multiplayer, "Screen_Main_Multiplayer",
                NavigateMultiplayer, activeScreen == QuaverScreenType.Lobby ||
                                     activeScreen == QuaverScreenType.Multiplayer);
            AddApplicationButton(GlobalIcon.Download, "Screen_Download_Download",
                NavigateDownload, activeScreen == QuaverScreenType.Download);
            AddApplicationButton(GlobalIcon.Chat, "Screen_Main_Menu_Chat", ToggleChat, false);
            AddApplicationButton(GlobalIcon.Jukebox, "Screen_Overlay_VolumeControl_Music",
                OpenMusicPlayer, activeScreen == QuaverScreenType.Music);
            AddSharedRightControls();

            CurrentTopLayout = TopLayout.Application;
            CurrentActiveScreen = activeScreen;
        }

        public void ShowDefaultFooter()
        {
            if (CurrentFooterLayout == FooterLayout.Default)
                return;

            ClearFooter();

            AddIconButton(BottomBar, NavigationBarRegion.Left, GlobalIcons.Get(GlobalIcon.Website),
                LocalizationManager.Get("Screen_Main_Menu_Website"),
                () => BrowserHelper.OpenURL("https://quavergame.com"), TooltipAnchor.TopCenter);
            AddIconButton(BottomBar, NavigationBarRegion.Left, GlobalIcons.Get(GlobalIcon.Discord),
                LocalizationManager.Get("Screen_Main_Menu_Discord"),
                () => BrowserHelper.OpenURL("https://discord.gg/quaver", true), TooltipAnchor.TopCenter);
            AddIconButton(BottomBar, NavigationBarRegion.Left, GlobalIcons.Get(GlobalIcon.GitHub),
                LocalizationManager.Get("Screen_Main_Menu_GitHub"),
                () => BrowserHelper.OpenURL("https://github.com/Quaver"), TooltipAnchor.TopCenter);

            AddIconButton(BottomBar, NavigationBarRegion.Right, GlobalIcons.Get(GlobalIcon.Volume),
                LocalizationManager.Get("Screen_Options_Volume"), ShowVolume, TooltipAnchor.TopCenter);
            AddIconButton(BottomBar, NavigationBarRegion.Right, GlobalIcons.Get(GlobalIcon.Options),
                LocalizationManager.Get("Screen_Main_Options"),
                () => DialogManager.Show(new OptionsDialog()), TooltipAnchor.TopCenter);
            AddIconButton(BottomBar, NavigationBarRegion.Right, GlobalIcons.Get(GlobalIcon.Quit),
                LocalizationManager.Get("Screen_Main_QuitGame"),
                () => DialogManager.Show(new QuitDialog()), TooltipAnchor.TopCenter);

            CurrentFooterLayout = FooterLayout.Default;
        }

        public void ShowSelectionFooter()
        {
            if (CurrentFooterLayout == FooterLayout.Selection)
                return;

            ClearFooter();

            var button = BottomBar.AddRoundedButton(NavigationBarRegion.Right,
                new NavigationBarButtonOptions
                {
                    Icon = GlobalIcons.Get(GlobalIcon.Play),
                    IconSize = new Vector2(Config.Button.IconSize, Config.Button.IconSize),
                    Text = LocalizationManager.Get("Screen_Selection_Play"),
                    Font = FontManager.GetWobbleFont(Fonts.InterBold),
                    FontSize = SkinV2FontSizesConfig.TextBase,
                    WidthMode = ButtonSizeMode.Auto,
                    Height = Config.Button.Size,
                    AutoSizePadding = new Vector2(Config.EdgePadding, 0),
                    CornerRadius = Config.Button.CornerRadius,
                    BackgroundColor = SkinV2Color.Parse(Config.Button.BackgroundColor),
                    ForegroundColor = SkinV2Color.Parse(Config.Button.ForegroundColor),
                    AlwaysShowLabel = true,
                    ClickAction = (sender, args) => ShowSelectionPlayUnavailable()
                });

            NavigationButtons.Add(button);
            FooterButtons.Add(button);
            CurrentFooterLayout = FooterLayout.Selection;
        }

        public static ScreenNavigation EnsureAttached(Container parent, SkinV2Config previewConfig = null)
        {
            if (ScreenManager.TryGetElement<ScreenNavigation>(ElementKey, out var navigation))
            {
                if (previewConfig == null &&
                    navigation.Skin.Generation == SkinManager.SkinV2?.Generation)
                {
                    if (navigation.Parent != parent)
                        navigation.Parent = parent;
                    navigation.ResetTransientState();
                    navigation.ResizeToWindow();
                    return navigation;
                }

                ScreenManager.RemoveElement(ElementKey);
            }

            navigation = new ScreenNavigation(previewConfig) { Parent = parent };
            ScreenManager.RegisterElement(ElementKey, navigation);
            return navigation;
        }

        public static ScreenNavigation ReplaceAttached(Container parent, SkinV2Config previewConfig)
        {
            ScreenManager.RemoveElement(ElementKey);
            return EnsureAttached(parent, previewConfig);
        }

        public IReadOnlyList<SkinEditorTarget> GetSkinEditorTargets() => new[]
        {
            new SkinEditorTarget("navigation-top",
                LocalizationManager.Get("SkinEditor_Component_TopNavigation"),
                "Shared.Navigation.Bar", TopBar),
            new SkinEditorTarget("navigation-bottom",
                LocalizationManager.Get("SkinEditor_Component_BottomNavigation"),
                "Shared.Navigation.Footer", BottomBar),
            new SkinEditorTarget("navigation-buttons",
                LocalizationManager.Get("SkinEditor_Component_NavigationButtons"),
                "Shared.Navigation.Button", NavigationButtons.ToArray()),
            new SkinEditorTarget("navigation-profile",
                LocalizationManager.Get("SkinEditor_Component_Profile"),
                "Shared.Navigation.Profile", ProfileButton)
        };

        public override void Update(GameTime gameTime)
        {
            ResizeToWindow();
            EnsureOnlineHubSubscription();
            base.Update(gameTime);
        }

        public override void Destroy()
        {
            if (SubscribedOnlineHub != null)
                SubscribedOnlineHub.UnreadStateChanged -= OnHubUnreadStateChanged;

            base.Destroy();

            Skin.Dispose();
        }

        private NavigationBar CreateBar(Alignment alignment, SkinV2NavigationBarConfig config) => new NavigationBar(
            WindowManager.Width, Config.Button.Size + Config.EdgePadding * 2, Color.Transparent)
        {
            Parent = this,
            Alignment = alignment,
            EdgePadding = Config.EdgePadding,
            ItemSpacing = Config.ItemSpacing,
            Background = SkinV2Background.Create(Skin, config.Background)
        };

        private RoundedButton AddIconButton(NavigationBar bar, NavigationBarRegion region,
            Texture2D icon, string tooltip, Action action, TooltipAnchor tooltipAnchor)
        {
            var button = bar.AddRoundedButton(region, new NavigationBarButtonOptions
            {
                Icon = icon,
                IconSize = new Vector2(Config.Button.IconSize, Config.Button.IconSize),
                Width = Config.Button.Size,
                Height = Config.Button.Size,
                CornerRadius = Config.Button.CornerRadius,
                BackgroundColor = SkinV2Color.Parse(Config.Button.BackgroundColor),
                ForegroundColor = SkinV2Color.Parse(Config.Button.ForegroundColor),
                ClickAction = (sender, args) => action()
            });

            button.AddTooltip(new TooltipOptions(tooltip)
            {
                Anchor = tooltipAnchor,
                MaximumWidth = 240
            });

            NavigationButtons.Add(button);
            if (bar == BottomBar)
                FooterButtons.Add(button);
            return button;
        }

        private void AddTopLayoutIconButton(GlobalIcon icon, string tooltip, Action action)
        {
            var button = AddIconButton(TopBar, NavigationBarRegion.Left, GlobalIcons.Get(icon),
                tooltip, action, TooltipAnchor.BottomCenter);
            TopLayoutButtons.Add(button);
        }

        private void AddApplicationButton(GlobalIcon icon, string localizationKey, Action action,
            bool active)
        {
            var button = TopBar.AddRoundedButton(NavigationBarRegion.Left,
                new NavigationBarButtonOptions
                {
                    Icon = GlobalIcons.Get(icon),
                    IconSize = new Vector2(Config.Button.IconSize, Config.Button.IconSize),
                    Text = LocalizationManager.Get(localizationKey),
                    Font = FontManager.GetWobbleFont(Fonts.InterBold),
                    FontSize = SkinV2FontSizesConfig.TextBase,
                    Width = Config.Button.Size,
                    Height = Config.Button.Size,
                    AutoSizePadding = new Vector2(Config.EdgePadding, 0),
                    CornerRadius = Config.Button.CornerRadius,
                    BackgroundColor = SkinV2Color.Parse(Config.Button.BackgroundColor),
                    ForegroundColor = SkinV2Color.Parse(Config.Button.ForegroundColor),
                    ExpandLabelOnHover = true,
                    AlwaysShowLabel = active,
                    ClickAction = (sender, args) => action()
                });

            NavigationButtons.Add(button);
            TopLayoutButtons.Add(button);
        }

        private void AddSharedRightControls()
        {
            TopBar.Add(NavigationBarRegion.Right, ProfileButton);
            TopBar.Add(NavigationBarRegion.Right, DonateButton);
            TopBar.Add(NavigationBarRegion.Right, HubButton);
        }

        private void ClearTopLayout()
        {
            TopBar.Clear(destroy: false);

            foreach (var button in TopLayoutButtons)
            {
                NavigationButtons.Remove(button);
                button.Destroy();
            }

            TopLayoutButtons.Clear();
        }

        private void ClearFooter()
        {
            foreach (var button in FooterButtons)
                NavigationButtons.Remove(button);

            FooterButtons.Clear();
            BottomBar.Clear(destroy: true);
        }

        private void EnsureOnlineHubSubscription()
        {
            var onlineHub = (GameBase.Game as QuaverGame)?.OnlineHub;
            if (ReferenceEquals(SubscribedOnlineHub, onlineHub))
                return;

            if (SubscribedOnlineHub != null)
                SubscribedOnlineHub.UnreadStateChanged -= OnHubUnreadStateChanged;

            SubscribedOnlineHub = onlineHub;
            if (SubscribedOnlineHub != null)
                SubscribedOnlineHub.UnreadStateChanged += OnHubUnreadStateChanged;

            UpdateHubIcon();
        }

        private void OnHubUnreadStateChanged(object sender, EventArgs args) => UpdateHubIcon();

        private void UpdateHubIcon()
        {
            var icon = SubscribedOnlineHub?.HasUnreadSections == true ? HubListIcon : HubMenuIcon;
            if (HubButton.Icon.Image != icon)
                HubButton.Icon.Image = icon;
        }

        private void ResizeToWindow()
        {
            if (Math.Abs(Width - WindowManager.Width) > 0.001f ||
                Math.Abs(Height - WindowManager.Height) > 0.001f)
                Size = new ScalableVector2(WindowManager.Width, WindowManager.Height);

            if (Math.Abs(TopBar.Width - WindowManager.Width) > 0.001f)
            {
                TopBar.Width = WindowManager.Width;
                BottomBar.Width = WindowManager.Width;
            }
        }

        private void ResetTransientState()
        {
            ProfileButton.ResetTransientState();
        }

        private static void OpenMusicPlayer()
        {
            if (GameBase.Game is QuaverGame game)
                game.CurrentScreen?.Exit(() => QuaverScreenFactory.CreateMusicPlayer());
        }

        private static void NavigateHome()
        {
            if (GameBase.Game is QuaverGame game &&
                game.CurrentScreen?.Type != QuaverScreenType.Menu)
                game.CurrentScreen?.Exit(() => QuaverScreenFactory.CreateMainMenu());
        }

        private static void NavigateSinglePlayer()
        {
            if (GameBase.Game is QuaverGame game &&
                game.CurrentScreen?.Type != QuaverScreenType.Select)
                game.CurrentScreen?.Exit(() => QuaverScreenFactory.CreateSelection());
        }

        private static void NavigateMultiplayer()
        {
            if (!(GameBase.Game is QuaverGame game) ||
                game.CurrentScreen?.Type == QuaverScreenType.Lobby ||
                game.CurrentScreen?.Type == QuaverScreenType.Multiplayer)
                return;

            if (!OnlineManager.Connected)
            {
                NotificationManager.Show(NotificationLevel.Error,
                    LocalizationManager.Get("Screen_Main_MultiplayerLoginRequired"));
                return;
            }

            if (MapManager.Mapsets.Count == 0)
            {
                if (OnlineManager.Status.Value == ConnectionStatus.Connected)
                {
                    game.CurrentScreen?.Exit(() => QuaverScreenFactory.CreateDownloading());
                    return;
                }

                NotificationManager.Show(NotificationLevel.Error,
                    LocalizationManager.Get("Screen_Main_NoMapsLoaded"));
                return;
            }

            game.CurrentScreen?.Exit(() => QuaverScreenFactory.CreateMultiplayerLobby());
        }

        private static void NavigateDownload()
        {
            if (!(GameBase.Game is QuaverGame game) ||
                game.CurrentScreen?.Type == QuaverScreenType.Download)
                return;

            var previousScreen = game.CurrentScreen?.Type ?? QuaverScreenType.Menu;
            game.CurrentScreen?.Exit(() => QuaverScreenFactory.CreateDownloading(previousScreen));
        }

        private static void ToggleChat()
        {
            if (!(GameBase.Game is QuaverGame game) || game.OnlineChat == null)
                return;

            if (game.OnlineChat.IsOpen)
                game.OnlineChat.Close();
            else
                game.OnlineChat.Open();
        }

        private static void ShowDonateMessage() => NotificationManager.Show(NotificationLevel.Info,
            "Donating is currently unavailable from in-game and can only be done on the website.\n\n" +
            "We are working on adding this back soon.");

        private static void ShowVolume()
        {
            if (GameBase.Game is QuaverGame game)
                game.VolumeController?.Show();
        }

        private static void ShowSelectionPlayUnavailable() =>
            NotificationManager.Show(NotificationLevel.Warning,
                LocalizationManager.Get("Screen_Main_NotImplemented"));

        private static void ToggleOnlineHub()
        {
            if (DialogManager.Dialogs.Count == 0)
            {
                DialogManager.Show(new OnlineHubDialog());
                return;
            }

            var topDialog = DialogManager.Dialogs[DialogManager.Dialogs.Count - 1];
            if (topDialog is OnlineHubDialog dialog)
                dialog.Close();
        }

        private enum FooterLayout
        {
            Default,
            Selection
        }

        private enum TopLayout
        {
            Main,
            Application
        }

        /// <summary>
        ///     Replacement-screen account control. This deliberately avoids the legacy menu-border drawable,
        ///     whose transparent root sprite is styled by the legacy header.
        /// </summary>
        private sealed class ProfileControl : RoundedButton
        {
            private SkinV2ProfileConfig Config { get; }

            private Texture2D OfflineAvatar { get; }

            private RoundedAvatar Avatar { get; }

            private Sprite Flag { get; }

            private ClanTag Clan { get; }

            private SpriteTextPlus Username { get; }

            private RoundedButton StatusBorder { get; }

            private RoundedButton StatusDot { get; }

            private bool IsOpen { get; set; }

            private bool LastConnected { get; set; }

            private object LastUser { get; set; }

            private string LastUsername { get; set; }

            public ProfileControl(SkinV2ProfileConfig config, Color backgroundColor, Texture2D offlineAvatar,
                float buttonSize)
            {
                Config = config;
                OfflineAvatar = offlineAvatar;
                Size = new ScalableVector2(Config.Width, buttonSize);
                Tint = backgroundColor;
                CornerRadius = Config.CornerRadius;
                PerformHoverFade = true;

                Avatar = new RoundedAvatar(buttonSize, Config.CornerRadius, GetAvatar())
                {
                    Parent = this,
                    Alignment = Alignment.MidLeft,
                    X = 0
                };

                StatusBorder = new RoundedButton
                {
                    Parent = Avatar,
                    Alignment = Alignment.BotRight,
                    Position = new ScalableVector2(0, 0),
                    Size = new ScalableVector2(Config.StatusBorderSize, Config.StatusBorderSize),
                    Tint = backgroundColor,
                    IsClickable = false,
                    PerformHoverFade = false
                };

                StatusDot = new RoundedButton
                {
                    Parent = StatusBorder,
                    Alignment = Alignment.MidCenter,
                    Size = new ScalableVector2(Config.StatusDotSize, Config.StatusDotSize),
                    IsClickable = false,
                    PerformHoverFade = false
                };

                Flag = new Sprite
                {
                    Parent = this,
                    Alignment = Alignment.MidLeft,
                    X = Config.FlagX,
                    Size = new ScalableVector2(Config.FlagSize, Config.FlagSize),
                    Image = Flags.Get("XX"),
                    Visible = false
                };

                Clan = new ClanTag(Config.UsernameFontSize)
                {
                    Parent = this,
                    Alignment = Alignment.MidLeft,
                    X = Flag.X + Flag.Width + Config.TextSpacing
                };

                Username = new SpriteTextPlus(FontManager.GetWobbleFont(Config.UsernameFont), string.Empty,
                    Config.UsernameFontSize)
                {
                    Parent = this,
                    Alignment = Alignment.MidLeft,
                    Tint = SkinV2Color.Parse(Config.TextColor)
                };

                Clicked += (sender, args) => ToggleAccountDropdown();
                ConfigManager.Username.ValueChanged += OnUsernameChanged;
                OnlineManager.Status.ValueChanged += OnOnlineStatusChanged;
                SteamManager.SteamUserAvatarLoaded += OnSteamAvatarLoaded;

                UpdateProfile();
            }

            public override void Update(GameTime gameTime)
            {
                var connected = OnlineManager.Connected;
                var username = GetDisplayUsername(connected);
                if (connected != LastConnected || !ReferenceEquals(LastUser, OnlineManager.Self) ||
                    LastUsername != username)
                    UpdateProfile();

                if (IsOpen && MouseManager.IsUniqueClick(MouseButton.Left) && !IsHovered &&
                    GameBase.Game is QuaverGame game &&
                    game.CurrentScreen?.ActiveLoggedInUserDropdown?.IsHovered() != true)
                    ToggleAccountDropdown();

                if (DialogManager.Dialogs.Count == 0 && KeyboardManager.IsUniqueKeyPress(Keys.F10))
                    ToggleAccountDropdown();

                base.Update(gameTime);
            }

            public override void Destroy()
            {
                ConfigManager.Username.ValueChanged -= OnUsernameChanged;
                OnlineManager.Status.ValueChanged -= OnOnlineStatusChanged;
                SteamManager.SteamUserAvatarLoaded -= OnSteamAvatarLoaded;
                base.Destroy();
            }

            public void ResetTransientState() => IsOpen = false;

            private void ToggleAccountDropdown()
            {
                if (!(GameBase.Game is QuaverGame game) || game.CurrentScreen == null)
                    return;

                IsOpen = !IsOpen;

                if (!IsOpen)
                {
                    game.CurrentScreen.ActiveLoggedInUserDropdown?.Close();
                    return;
                }

                if (game.CurrentScreen.ActiveLoggedInUserDropdown == null)
                {
                    game.CurrentScreen.ActivateLoggedInUserDropdown(new LoggedInUserDropdown(),
                        new ScalableVector2(
                            AbsolutePosition.X + AbsoluteSize.X - LoggedInUserDropdown.ContainerSize.X.Value,
                            AbsolutePosition.Y + AbsoluteSize.Y + Config.DropdownGap));
                    return;
                }

                game.CurrentScreen.ActiveLoggedInUserDropdown.Open();
            }

            private void UpdateProfile()
            {
                var connected = OnlineManager.Connected;
                var user = OnlineManager.Self;
                var username = GetDisplayUsername(connected);

                Avatar.AvatarSprite.Image = GetAvatar();
                StatusDot.Tint = connected
                    ? Color.White
                    : SkinV2Color.Parse(Config.OfflineStatusColor);

                if (connected)
                {
                    Flag.Image = Flags.Get(user?.OnlineUser?.CountryFlag ?? "XX");
                    Flag.Visible = true;
                    Clan.UpdateFromUser(user?.OnlineUser, SkinV2Color.Parse(Config.TextColor));
                }
                else
                {
                    Flag.Visible = false;
                    Clan.Clear();
                }

                Clan.X = Flag.Visible ? Flag.X + Flag.Width + Config.TextSpacing : Config.FlagX - 1;
                var usernameX = Clan.Visible ? Clan.X + Clan.Width + Config.TextSpacing - 1 : Clan.X;
                Username.X = usernameX;
                Username.Text = username;
                Username.TruncateWithEllipsis((int) Math.Max(40,
                    Config.Width - usernameX - Config.UsernameRightPadding));

                LastConnected = connected;
                LastUser = user;
                LastUsername = username;
            }

            private static string GetDisplayUsername(bool connected) => connected
                ? OnlineManager.Self?.OnlineUser?.Username ?? ConfigManager.Username.Value ?? "Player"
                : "Login";

            private Texture2D GetAvatar()
            {
                var image = OfflineAvatar;

                if (OnlineManager.Status.Value == ConnectionStatus.Connected && SteamManager.UserAvatars != null)
                {
                    var id = SteamUser.GetSteamID().m_SteamID;
                    if (SteamManager.UserAvatars.TryGetValue(id, out var avatar))
                        image = avatar;
                }

                return image;
            }

            private void OnUsernameChanged(object sender, BindableValueChangedEventArgs<string> args) =>
                UpdateProfile();

            private void OnOnlineStatusChanged(object sender, BindableValueChangedEventArgs<ConnectionStatus> args) =>
                UpdateProfile();

            private void OnSteamAvatarLoaded(object sender, SteamAvatarLoadedEventArgs args)
            {
                if (SteamUser.GetSteamID().m_SteamID == args.SteamId)
                    Avatar.AvatarSprite.Image = args.Texture;
            }
        }

        private sealed class RoundedAvatar : SpriteMaskContainer
        {
            public Sprite AvatarSprite { get; }

            public RoundedAvatar(float size, float cornerRadius, Texture2D image)
            {
                Size = new ScalableVector2(size, size);
                Image = RoundedRectTextureCache.Get(size, size, cornerRadius);

                AvatarSprite = new Sprite
                {
                    Alignment = Alignment.TopLeft,
                    Size = Size,
                    Image = image
                };

                AddContainedSprite(AvatarSprite);
            }
        }
    }
}
