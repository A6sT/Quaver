using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Quaver.Shared.Assets;
using Quaver.Shared.Skinning.V2;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Buttons;
using Wobble.Graphics.Shaders;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Form;
using Wobble.Input;

namespace Quaver.Shared.Screens.V2.Downloading.UI
{
    internal abstract class DownloadingSearchTextbox : Textbox
    {
        private SkinV2DownloadingFieldConfig Config { get; }

        private Color TextColor { get; }

        private Color PlaceholderColor { get; }

        protected DownloadingSearchTextbox(ScalableVector2 size, WobbleFontStore font,
            SkinV2DownloadingFieldConfig config, string initialText, string placeholder)
            : base(size, font, config.FontSize, initialText, placeholder)
        {
            Config = config;
            TextColor = SkinV2Color.Parse(config.TextColor);
            PlaceholderColor = SkinV2Color.Parse(config.PlaceholderColor);
            Tint = SkinV2Color.Parse(config.BackgroundColor);
            Cursor.Tint = SkinV2Color.Parse(config.CursorColor);
            Scrollbar.Visible = false;
            InputEnabled = false;
            StoppedTypingActionCalltime = 250;
            ApplySize();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            InputText.Tint = string.IsNullOrEmpty(RawText) ? PlaceholderColor : TextColor;
            InputText.Alpha = 1;
        }

        protected override void OnRectangleRecalculated()
        {
            base.OnRectangleRecalculated();
            if (Config != null)
                ApplySize();
        }

        private void ApplySize()
        {
            Button.Size = Size;
            ContentContainer.Size = Size;

            // FlexContainer can briefly report an empty rectangle while the window is
            // being resized. Avoid generating a rounded texture for that transient state.
            if (Width <= 0 || Height <= 0 || float.IsNaN(Width) || float.IsNaN(Height) ||
                float.IsInfinity(Width) || float.IsInfinity(Height))
                return;

            var texture = RoundedRectTextureCache.Get(Width, Height, Config.CornerRadius);
            if (Image != texture)
                Image = texture;
        }
    }

    internal sealed class DownloadingSearchQueryTextbox : DownloadingSearchTextbox
    {
        private Bindable<string> Query { get; }

        private Sprite SearchIcon { get; }

        public DownloadingSearchQueryTextbox(Bindable<string> query, string placeholder,
            WobbleFontStore font, SkinV2DownloadingFieldConfig config)
            : base(new ScalableVector2(config.SearchWidth, config.Height), font, config,
                query.Value, placeholder)
        {
            Query = query;
            InputText.X = config.SearchIconInset * 2 + config.SearchIconSize;
            SearchIcon = new Sprite
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = config.SearchIconInset,
                Image = FontAwesome.Get(FontAwesomeIcon.fa_magnifying_glass),
                Size = new ScalableVector2(config.SearchIconSize, config.SearchIconSize),
                Tint = SkinV2Color.Parse(config.PlaceholderColor),
                UsePreviousSpriteBatchOptions = true
            };

            OnStoppedTyping += OnQueryChanged;
            Query.ValueChanged += OnBoundQueryChanged;
        }

        public override void Destroy()
        {
            OnStoppedTyping -= OnQueryChanged;
            Query.ValueChanged -= OnBoundQueryChanged;
            base.Destroy();
        }

        private void OnQueryChanged(string value)
        {
            if (Query.Value != value)
                Query.Value = value;
        }

        private void OnBoundQueryChanged(object sender, BindableValueChangedEventArgs<string> args)
        {
            var value = args.Value ?? string.Empty;
            if (RawText != value)
                RawText = value;
        }
    }

    internal sealed class DownloadingNumericTextbox : DownloadingSearchTextbox
    {
        private const string InfinitySymbol = "∞";

        private static readonly Regex NumericCharacters =
            new Regex(@"^(?!.*\..*\.)[.\d]*$", RegexOptions.Compiled);

        private BindableFloat Value { get; }

        private Func<float, float> Normalize { get; }

        private string Format { get; }

        private Func<float, bool> ShowInfinity { get; }

        private bool HasValue { get; set; }

        private bool WasFocused { get; set; }

        public DownloadingNumericTextbox(BindableFloat value, string placeholder,
            WobbleFontStore font, SkinV2DownloadingFieldConfig config, float width,
            string format = "0.##", bool showInitialValue = false,
            Func<float, float> normalize = null, Func<float, bool> showInfinity = null)
            : base(new ScalableVector2(width, config.Height), font, config,
                showInitialValue ? FormatValue(value.Value, format, showInfinity) : string.Empty,
                placeholder)
        {
            Value = value;
            Normalize = normalize;
            Format = format;
            ShowInfinity = showInfinity;
            HasValue = showInitialValue;
            AllowedCharacters = NumericCharacters;
            MaxCharacters = 8;

            OnStoppedTyping += OnTextChanged;
            Value.ValueChanged += OnBoundValueChanged;
        }

        public override void Update(GameTime gameTime)
        {
            // Infinity is a display-only value. Clear it once the user focuses the field so
            // normal numeric input can replace it without requiring an explicit selection.
            if (Focused && !WasFocused && ShowInfinity?.Invoke(Value.Value) == true &&
                RawText == InfinitySymbol)
            {
                RawText = string.Empty;
                HasValue = false;
            }
            else if (!Focused && WasFocused && ShowInfinity != null &&
                     (string.IsNullOrEmpty(RawText) || RawText == "."))
            {
                HasValue = true;
                SetFormattedText(Value.Value);
            }

            WasFocused = Focused;
            base.Update(gameTime);
        }

        public override void Destroy()
        {
            OnStoppedTyping -= OnTextChanged;
            Value.ValueChanged -= OnBoundValueChanged;
            base.Destroy();
        }

        private void OnTextChanged(string text)
        {
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                return;

            parsed = Normalize?.Invoke(parsed) ?? parsed;
            Value.Value = parsed;
            HasValue = true;
            SetFormattedText(Value.Value);
        }

        private void OnBoundValueChanged(object sender, BindableValueChangedEventArgs<float> args)
        {
            if (HasValue || ShowInfinity != null)
                SetFormattedText(args.Value);
        }

        private void SetFormattedText(float value)
        {
            var formatted = FormatValue(value, Format, ShowInfinity);
            if (RawText != formatted)
                RawText = formatted;
        }

        private static string FormatValue(float value, string format, Func<float, bool> showInfinity) =>
            showInfinity?.Invoke(value) == true
                ? InfinitySymbol
                : value.ToString(format, CultureInfo.InvariantCulture);
    }

    internal sealed class DownloadingRangeSlider : Container
    {
        private BindableFloat Minimum { get; }

        private BindableFloat Maximum { get; }

        private SkinV2DownloadingRangeConfig Config { get; }

        private Sprite Track { get; }

        private Sprite SelectedTrack { get; }

        private RangeThumb MinimumThumb { get; }

        private RangeThumb MaximumThumb { get; }

        public DownloadingRangeSlider(BindableFloat minimum, BindableFloat maximum,
            SkinV2DownloadingRangeConfig config)
        {
            Minimum = minimum;
            Maximum = maximum;
            Config = config;
            Size = new ScalableVector2(config.Width, config.ThumbHeight);

            Track = new Sprite
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                Size = new ScalableVector2(config.Width, config.TrackHeight),
                Image = RoundedRectTextureCache.Get(config.Width, config.TrackHeight,
                    config.TrackCornerRadius),
                Tint = SkinV2Color.Parse(config.TrackColor)
            };
            SelectedTrack = new Sprite
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                Height = config.TrackHeight,
                Image = RoundedRectTextureCache.Get(config.Width, config.TrackHeight,
                    config.TrackCornerRadius),
                Tint = SkinV2Color.Parse(config.SelectedTrackColor)
            };

            MinimumThumb = CreateThumb(value => SetMinimum(value));
            MaximumThumb = CreateThumb(value => SetMaximum(value));
            Minimum.ValueChanged += OnValueChanged;
            Maximum.ValueChanged += OnValueChanged;
            RefreshPositions();
        }

        public override void Destroy()
        {
            Minimum.ValueChanged -= OnValueChanged;
            Maximum.ValueChanged -= OnValueChanged;
            base.Destroy();
        }

        protected override void OnRectangleRecalculated()
        {
            base.OnRectangleRecalculated();
            if (Track != null)
                RefreshPositions();
        }

        private RangeThumb CreateThumb(Action<float> dragged) => new RangeThumb(dragged)
        {
            Parent = this,
            Alignment = Alignment.MidLeft,
            Size = new ScalableVector2(Config.ThumbWidth, Config.ThumbHeight),
            CornerRadius = Config.ThumbCornerRadius,
            Tint = SkinV2Color.Parse(Config.ThumbColor),
            PerformHoverFade = true,
            Depth = 50
        };

        private void SetMinimum(float normalized)
        {
            var value = Minimum.MinValue + normalized * (Minimum.MaxValue - Minimum.MinValue);
            Minimum.Value = Math.Min(value, Maximum.Value);
        }

        private void SetMaximum(float normalized)
        {
            var value = Maximum.MinValue + normalized * (Maximum.MaxValue - Maximum.MinValue);
            Maximum.Value = Math.Max(value, Minimum.Value);
        }

        private void OnValueChanged(object sender, BindableValueChangedEventArgs<float> args) =>
            RefreshPositions();

        private void RefreshPositions()
        {
            if (Track == null || MinimumThumb == null || MaximumThumb == null)
                return;

            Track.Width = Width;
            Track.Image = RoundedRectTextureCache.Get(Width, Config.TrackHeight,
                Config.TrackCornerRadius);

            var usableWidth = Math.Max(1, Width - Config.ThumbWidth);
            var minimumPosition = Normalize(Minimum.Value, Minimum.MinValue, Minimum.MaxValue) * usableWidth;
            var maximumPosition = Normalize(Maximum.Value, Maximum.MinValue, Maximum.MaxValue) * usableWidth;
            MinimumThumb.X = minimumPosition;
            MaximumThumb.X = maximumPosition;
            SelectedTrack.X = minimumPosition + Config.ThumbWidth / 2f;
            SelectedTrack.Width = Math.Max(1, maximumPosition - minimumPosition);
            SelectedTrack.Image = RoundedRectTextureCache.Get(SelectedTrack.Width,
                Config.TrackHeight, Config.TrackCornerRadius);
        }

        private static float Normalize(float value, float minimum, float maximum) =>
            maximum <= minimum ? 0 : MathHelper.Clamp((value - minimum) / (maximum - minimum), 0, 1);

        private sealed class RangeThumb : RoundedButton
        {
            private Action<float> Dragged { get; }

            public RangeThumb(Action<float> dragged) => Dragged = dragged;

            protected override void OnHeld(GameTime gameTime)
            {
                base.OnHeld(gameTime);
                var parent = Parent;
                if (parent == null)
                    return;

                var usableWidth = Math.Max(1, parent.Width - Width);
                var localX = MouseManager.CurrentState.X - parent.ScreenRectangle.X - Width / 2f;
                Dragged(MathHelper.Clamp(localX / usableWidth, 0, 1));
            }
        }
    }

    /// <summary>
    ///     Keeps the dropdown's expansion fade separate from RoundedButton's hover fade.
    /// </summary>
    internal sealed class DownloadingSearchButton : RoundedButton
    {
        private float HoverAlpha { get; set; } = 1;

        private float ExpansionAlpha { get; set; } = 1;

        public DownloadingSearchButton(EventHandler clickAction = null) : base(clickAction)
        {
        }

        public void SetExpansionAlpha(float alpha)
        {
            ExpansionAlpha = MathHelper.Clamp(alpha, 0, 1);
            ApplyCombinedAlpha();
        }

        public override void Update(GameTime gameTime)
        {
            // RoundedButton uses Alpha as its hover animation state. Restore that state before
            // the base update, then apply the expansion fade after the hover interpolation.
            Alpha = HoverAlpha;
            base.Update(gameTime);
            HoverAlpha = Alpha;
            ApplyCombinedAlpha();
        }

        private void ApplyCombinedAlpha() => Alpha = HoverAlpha * ExpansionAlpha;
    }

}
