using Microsoft.Xna.Framework;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.UI.Dialogs;

namespace Quaver.Shared.Screens.Edit.UI.Modding
{
    public sealed class EditorModdingPanelContainer : Container
    {
        public Bindable<bool> IsActive { get; } = new Bindable<bool>(false);

        public EditorModdingPanel Panel { get; }

        private ScalableVector2 PanelPosition { get; set; }

        private bool DialogsOpen { get; set; }

        public EditorModdingPanelContainer(EditScreen screen)
        {
            Panel = new EditorModdingPanel(screen, this)
            {
                Parent = this,
                Alignment = Alignment.MidRight
            };

            ChangePanelPosition();
            IsActive.ValueChanged += OnActiveChanged;
        }

        public override void Update(GameTime gameTime)
        {
            if (IsActive.Value && Panel.Position.X.Value > 0)
                PanelPosition = Panel.Position;

            if (DialogManager.Dialogs.Count > 0 != DialogsOpen)
                ChangePanelPosition();

            DialogsOpen = DialogManager.Dialogs.Count > 0;
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            if (IsActive.Value)
                base.Draw(gameTime);
        }

        public override void Dispose()
        {
            IsActive.ValueChanged -= OnActiveChanged;
            IsActive.Dispose();
            base.Dispose();
        }

        private void OnActiveChanged(object sender, BindableValueChangedEventArgs<bool> args)
        {
            ChangePanelPosition();

            if (args.Value)
                Panel.EnsureLoaded();
        }

        private void ChangePanelPosition()
        {
            Panel.Position = !IsActive.Value || DialogManager.Dialogs.Count > 0
                ? new ScalableVector2(-10000, 0)
                : PanelPosition;
        }
    }
}