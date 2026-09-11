using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Quaver.Server.Client.Enums;
using Quaver.Server.Client.Objects;
using Quaver.Shared.Config;
using Quaver.Shared.Graphics;
using Quaver.Shared.Online;
using Quaver.Shared.Screens.Downloading;
using Quaver.Shared.Screens.V2.SkinEditor;
using Quaver.Shared.Skinning;
using Wobble;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Input;

namespace Quaver.Shared.Screens.V2.Downloading
{
    internal sealed class DownloadingScreen : SkinV2Screen
    {
        public override QuaverScreenType Type { get; } = QuaverScreenType.Download;

        internal DownloadingSearchState SearchState { get; } = new DownloadingSearchState();

        private QuaverScreenType PreviousScreen { get; }

        protected override ISkinV2EditorHost SkinEditorHost => (DownloadingScreenView) View;

        public DownloadingScreen(QuaverScreenType previousScreen = QuaverScreenType.Menu)
        {
            PreviousScreen = previousScreen;
            View = new DownloadingScreenView(this);
        }

        public override void OnFirstUpdate()
        {
            GameBase.Game.GlobalUserInterface.Cursor.Show(1);
            GameBase.Game.GlobalUserInterface.Cursor.Alpha = 1;
            SkinManager.StartWatching();
            ScreenExiting += OnScreenExiting;
            base.OnFirstUpdate();
        }

        public override void Update(GameTime gameTime)
        {
            if (!Exiting && !IsSkinEditorActive && DialogManager.Dialogs.Count == 0 &&
                KeyboardManager.IsUniqueKeyPress(Keys.Escape))
                ExitToPreviousScreen();

            base.Update(gameTime);
        }

        public override void Destroy()
        {
            ScreenExiting -= OnScreenExiting;
            base.Destroy();
            SearchState.Dispose();
        }

        public override UserClientStatus GetClientStatus() =>
            new UserClientStatus(ClientStatus.InMenus, -1, "", 1, "", 0);

        internal void ExitToPreviousScreen()
        {
            switch (PreviousScreen)
            {
                case QuaverScreenType.Select:
                    Exit(() => QuaverScreenFactory.CreateSelection());
                    break;
                case QuaverScreenType.Lobby:
                    Exit(() => OnlineManager.Connected
                        ? QuaverScreenFactory.CreateMultiplayerLobby()
                        : QuaverScreenFactory.CreateMainMenu());
                    break;
                case QuaverScreenType.Multiplayer:
                    Exit(() => OnlineManager.CurrentGame != null
                        ? QuaverScreenFactory.CreateMultiplayerGame()
                        : QuaverScreenFactory.CreateMainMenu());
                    break;
                case QuaverScreenType.Music:
                    Exit(() => QuaverScreenFactory.CreateMusicPlayer());
                    break;
                case QuaverScreenType.Theatre:
                    Exit(() => QuaverScreenFactory.CreateTheater());
                    break;
                default:
                    Exit(() => QuaverScreenFactory.CreateMainMenu());
                    break;
            }
        }

        internal void ShowRecommendedDifficultyDialog()
        {
            DialogManager.Show(new YesNoDialog(
                DownloadLocalization.Get("Recommended difficulty"),
                DownloadLocalization.Get("Recommend Difficulty Description"), () =>
                {
                    if (!OnlineManager.Connected || OnlineManager.Self == null)
                    {
                        SearchState.MapsetQuery.Value = "Easy";
                        return;
                    }

                    var rating = OnlineManager.Self.Stats[ConfigManager.SelectedGameMode.Value]
                        .OverallPerformanceRating;
                    var approximateLevel = rating / 20f;

                    if (rating == 0 || approximateLevel < 5)
                        SearchState.MapsetQuery.Value = "Easy";
                    else if (approximateLevel < 10)
                        SearchState.MapsetQuery.Value = "Normal";
                    else if (approximateLevel < 20)
                        SearchState.MapsetQuery.Value = "Hard";
                    else if (approximateLevel < 28)
                        SearchState.MapsetQuery.Value = "Insane";
                    else
                        SearchState.MapsetQuery.Value = string.Empty;
                }));
        }

        private static void OnScreenExiting(object sender, ScreenExitingEventArgs args) =>
            SkinManager.StopWatching();
    }
}
