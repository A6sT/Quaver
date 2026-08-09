using Microsoft.Xna.Framework;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Sprites.Text;

namespace Quaver.Shared.Screens.V2.UI.OnlineHub.Shared
{
    internal sealed class OnlineHubMarqueeLabel : Container
    {
        private WobbleFontStore Font { get; }

        private int FontSize { get; }

        private SpriteTextPlus StaticText { get; }

        private MarqueeSpriteText Marquee { get; set; }

        private string CurrentText { get; set; } = "";

        private Color CurrentTint { get; set; } = Color.White;

        private float CurrentAlpha { get; set; } = 1;

        internal OnlineHubMarqueeLabel(WobbleFontStore font, int fontSize)
        {
            Font = font;
            FontSize = fontSize;
            StaticText = new SpriteTextPlus(font, "", fontSize)
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                UsePreviousSpriteBatchOptions = true
            };
            SizeChanged += OnSizeChanged;
        }

        public override void Destroy()
        {
            SizeChanged -= OnSizeChanged;
            base.Destroy();
        }

        internal void SetText(string text, Color tint)
        {
            text ??= "";
            if (CurrentText == text && CurrentTint == tint)
                return;

            CurrentText = text;
            CurrentTint = tint;
            StaticText.Text = CurrentText;
            StaticText.Tint = tint;
            StaticText.Alpha = CurrentAlpha;
            RefreshMode();
        }

        internal void SetAlpha(float alpha)
        {
            CurrentAlpha = alpha;
            StaticText.Alpha = alpha;
            if (Marquee != null)
                Marquee.TextSprite.Alpha = alpha;
        }

        internal void SetMarqueeActive(bool active)
        {
            if (Marquee != null)
                Marquee.IsActive = active;
        }

        private void OnSizeChanged(object sender, ScalableVector2 size) => RefreshMode();

        private void RefreshMode()
        {
            if (Width <= 0 || Height <= 0)
                return;

            var overflow = StaticText.Width > Width;
            StaticText.Visible = !overflow;
            if (!overflow)
            {
                if (Marquee != null)
                {
                    Marquee.Visible = false;
                    Marquee.IsActive = false;
                    Marquee.ResetPosition();
                }

                return;
            }

            if (Marquee == null)
            {
                Marquee = new MarqueeSpriteText(Font, CurrentText, FontSize, Width)
                {
                    Parent = this,
                    Alignment = Alignment.MidLeft
                };
            }

            Marquee.TextSprite.Text = CurrentText;
            Marquee.TextSprite.Tint = CurrentTint;
            Marquee.TextSprite.Alpha = CurrentAlpha;
            Marquee.Size = new ScalableVector2(Width, Height);
            Marquee.Visible = true;
            Marquee.ResetPosition();
        }
    }
}
