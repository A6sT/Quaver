using Quaver.Server.Client.Enums;
using Quaver.Server.Client.Objects;

namespace Quaver.Shared.Screens.V2.Downloading
{
    internal sealed class DownloadingScreen : PlaceholderScreen
    {
        public override QuaverScreenType Type { get; } = QuaverScreenType.Download;

        public DownloadingScreen(QuaverScreenType previousScreen = QuaverScreenType.Menu) =>
            View = new DownloadingScreenView(this);

        public override UserClientStatus GetClientStatus() =>
            new UserClientStatus(ClientStatus.InMenus, -1, "", 1, "", 0);
    }
}
