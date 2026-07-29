namespace Quaver.Shared.Screens.V2.Downloading
{
    internal sealed class DownloadingScreenView : PlaceholderScreenView
    {
        public DownloadingScreenView(DownloadingScreen screen) : base(screen,
            config => config.Screens.Downloading, "Screens.Downloading",
            "Screen_V2_DownloadingScreen")
        {
        }
    }
}
