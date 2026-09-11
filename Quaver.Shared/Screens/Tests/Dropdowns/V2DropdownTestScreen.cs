using Wobble.Screens;

namespace Quaver.Shared.Screens.Tests.Dropdowns
{
    public sealed class V2DropdownTestScreen : Screen
    {
        public override ScreenView View { get; protected set; }

        public V2DropdownTestScreen() => View = new V2DropdownTestScreenView(this);
    }
}
