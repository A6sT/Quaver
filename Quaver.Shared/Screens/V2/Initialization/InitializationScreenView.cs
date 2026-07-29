namespace Quaver.Shared.Screens.V2.Initialization
{
    internal sealed class InitializationScreenView : PlaceholderScreenView
    {
        public InitializationScreenView(InitializationScreen screen) : base(screen,
            config => config.Screens.Initialization, "Screens.Initialization",
            "Screen_V2_InitializationScreen")
        {
        }
    }
}
