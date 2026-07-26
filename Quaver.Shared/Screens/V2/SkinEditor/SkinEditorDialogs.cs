using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Quaver.Shared.Assets;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Buttons;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Graphics.UI.Form;
using Wobble.Input;
using Wobble.Managers;
using Wobble.Window;

namespace Quaver.Shared.Screens.V2.SkinEditor
{
    internal sealed class SkinEditorChoiceDialog : DialogScreen
    {
        private readonly string title;
        private readonly string message;
        private readonly string primaryLabel;
        private readonly Action primaryAction;
        private readonly string secondaryLabel;
        private readonly Action secondaryAction;
        private readonly string cancelLabel;
        private readonly Action cancelAction;

        public SkinEditorChoiceDialog(string title, string message,
            string primaryLabel, Action primaryAction,
            string secondaryLabel, Action secondaryAction,
            string cancelLabel, Action cancelAction) : base(0.75f)
        {
            this.title = title;
            this.message = message;
            this.primaryLabel = primaryLabel;
            this.primaryAction = primaryAction;
            this.secondaryLabel = secondaryLabel;
            this.secondaryAction = secondaryAction;
            this.cancelLabel = cancelLabel;
            this.cancelAction = cancelAction;
            CreateContent();
        }

        public override void CreateContent()
        {
            var panel = new Sprite
            {
                Parent = Container,
                Alignment = Alignment.MidCenter,
                Size = new ScalableVector2(720, 230),
                Tint = new Color(19, 27, 38)
            };

            new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), title, 24)
            {
                Parent = panel,
                Alignment = Alignment.TopCenter,
                Y = 26,
                Tint = Color.White
            };

            new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterMedium), message, 16)
            {
                Parent = panel,
                Alignment = Alignment.TopCenter,
                Y = 72,
                Tint = new Color(210, 220, 232),
                TextAlignment = TextAlignment.Center
            };

            CreateButton(panel, primaryLabel, 80, primaryAction, new Color(39, 176, 110));
            CreateButton(panel, secondaryLabel, 270, secondaryAction, new Color(31, 136, 255));
            CreateButton(panel, cancelLabel, 460, cancelAction, new Color(249, 100, 93));
        }

        public override void HandleInput(GameTime gameTime)
        {
            if (KeyboardManager.IsUniqueKeyPress(Keys.Escape))
            {
                cancelAction?.Invoke();
                DialogManager.Dismiss(this);
            }
        }

        private void CreateButton(Drawable parent, string label, float x, Action action, Color color)
        {
            var button = new RoundedButton((sender, args) =>
            {
                action?.Invoke();
                DialogManager.Dismiss(this);
            })
            {
                Parent = parent,
                Alignment = Alignment.BotLeft,
                Position = new ScalableVector2(x, -30),
                Size = new ScalableVector2(180, 42),
                Tint = color,
                CornerRadius = 6
            };
            button.SetLabel(FontManager.GetWobbleFont(Fonts.InterBold), label, 16, Color.White);
        }
    }

    internal sealed class SkinEditorTextPromptDialog : DialogScreen
    {
        private readonly string title;
        private readonly string initialValue;
        private readonly Action<string> action;
        private Textbox textbox;

        public SkinEditorTextPromptDialog(string title, string initialValue, Action<string> action) : base(0.75f)
        {
            this.title = title;
            this.initialValue = initialValue;
            this.action = action;
            CreateContent();
        }

        public override void CreateContent()
        {
            var panel = new Sprite
            {
                Parent = Container,
                Alignment = Alignment.MidCenter,
                Size = new ScalableVector2(620, 210),
                Tint = new Color(19, 27, 38)
            };

            new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), title, 24)
            {
                Parent = panel,
                Alignment = Alignment.TopCenter,
                Y = 25,
                Tint = Color.White
            };

            textbox = new Textbox(new ScalableVector2(520, 42),
                FontManager.GetWobbleFont(Fonts.InterMedium), 16, initialValue)
            {
                Parent = panel,
                Alignment = Alignment.TopCenter,
                Y = 72,
                Tint = new Color(35, 47, 62)
            };

            CreateButton(panel, LocalizationManager.Get("SkinEditor_Copy"), -105, () =>
            {
                if (string.IsNullOrWhiteSpace(textbox.RawText))
                    return;
                action(textbox.RawText);
                DialogManager.Dismiss(this);
            });
            CreateButton(panel, LocalizationManager.Get("SkinEditor_Cancel"), 105,
                () => DialogManager.Dismiss(this));
        }

        public override void HandleInput(GameTime gameTime)
        {
            if (KeyboardManager.IsUniqueKeyPress(Keys.Escape))
                DialogManager.Dismiss(this);
        }

        private static void CreateButton(Drawable parent, string label, float x, Action action)
        {
            var button = new RoundedButton((sender, args) => action())
            {
                Parent = parent,
                Alignment = Alignment.BotCenter,
                X = x,
                Y = -25,
                Size = new ScalableVector2(180, 40),
                Tint = new Color(31, 136, 255),
                CornerRadius = 6
            };
            button.SetLabel(FontManager.GetWobbleFont(Fonts.InterBold), label, 16, Color.White);
        }
    }
}
