using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Shaders;
using Wobble.Graphics.Sprites;

namespace Quaver.Shared.Screens.V2.UI.OnlineHub.Shared
{
    internal sealed class OnlineHubAvatar : Sprite
    {
        private SpriteBatchOptions PlainScissorOptions { get; } =
            RoundedRectShader.CreateScissorSafeOptions();

        internal OnlineHubAvatar(float size, float cornerRadius, Texture2D image)
        {
            Size = new ScalableVector2(size, size);
            var shader = RoundedRectShader.Create(cornerRadius);
            RoundedRectShader.UpdateSize(shader, new Vector2(size, size));
            SpriteBatchOptions = new SpriteBatchOptions
            {
                RasterizerState = RoundedRectShader.ScissorSafeRasterizerState,
                Shader = shader
            };
            SetSource(image);
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Alpha <= 0)
                return;

            base.Draw(gameTime);
            PlainScissorOptions.Begin();
        }

        internal void SetSource(Texture2D image)
        {
            if (image != null && !image.IsDisposed)
                Image = image;
        }
    }
}
