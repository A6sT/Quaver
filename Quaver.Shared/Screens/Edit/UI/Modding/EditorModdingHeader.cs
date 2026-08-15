using Microsoft.Xna.Framework;
using Quaver.Shared.Assets;
using Quaver.Shared.Helpers;
using Quaver.Shared.Screens.Menu.UI.Jukebox;
using Wobble.Assets;
using Wobble.Graphics;
using Wobble.Graphics.Buttons;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;

namespace Quaver.Shared.Screens.Edit.UI.Modding
{
    internal sealed class EditorModdingHeader : Sprite
    {
        public EditorModdingHeader(EditorModdingPanel panel)
        {
            Image = UserInterface.AutoModPanelHeader;
            Size = new ScalableVector2(panel.Width, Image.Height);

            var icon = new Sprite
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = Height / 2,
                Size = new ScalableVector2(Height / 2, Height / 2),
                Image = UserInterface.AutoModHeaderGear
            };

            new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterSemiBold), LocalizationManager.Get("Screen_Editor_ModdingDiscussion"), 20)
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = icon.X + icon.Width + 10
            };

            var close = new RoundedButton((sender, args) => panel.Container.IsActive.Value = false)
            {
                Parent = this,
                Alignment = Alignment.MidRight,
                X = -8,
                Size = new ScalableVector2(24, 24),
                CornerRadius = 5,
                Tint = ColorHelper.HexToColor("#F9645D"),
                Depth = -1
            };
            close.SetIcon(FontAwesome.Get(FontAwesomeIcon.fa_times), new Vector2(11, 11));
        }
    }
}