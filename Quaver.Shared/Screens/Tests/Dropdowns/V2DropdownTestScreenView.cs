using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Helpers;
using Quaver.Shared.Screens.V2.UI;
using Quaver.Shared.Skinning;
using Quaver.Shared.Skinning.V2;
using Wobble;
using Wobble.Assets;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Buttons;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Managers;
using Wobble.Screens;
using Wobble.Window;

namespace Quaver.Shared.Screens.Tests.Dropdowns
{
    /// <summary>
    ///     Visual gallery for the shared V2 dropdown behavior. The legacy Dropdown test remains
    ///     available beside this surface for regression comparison.
    /// </summary>
    public sealed class V2DropdownTestScreenView : ScreenView
    {
        private SkinStoreV2Lease Skin { get; set; }

        private SkinV2DropdownConfig Config { get; }

        private WobbleFontStore Font { get; }

        private Container OverlayRoot { get; }

        private V2Dropdown<int> SingleDropdown { get; }

        private V2Dropdown<int> MultipleDropdown { get; }

        private V2Dropdown<int> BottomDropdown { get; }

        private SpriteTextPlus SelectionStatus { get; }

        private float LastWidth { get; set; } = -1;

        private float LastHeight { get; set; } = -1;

        public V2DropdownTestScreenView(Screen screen) : base(screen)
        {
            // The visual-test hot-loader can reach this screen before the normal V2
            // initialization screen has loaded ConfigManager's skin directory. Use the
            // shared defaults in that narrow case; production V2 screens still acquire
            // their active skin through SkinManager as usual.
            if (SkinManager.SkinV2 != null || ConfigManager.SkinDirectory?.Value != null)
            {
                Skin = SkinManager.AcquireV2();
                Config = Skin.Config.Shared.Dropdown;
            }
            else
                Config = new SkinV2DropdownConfig();

            Font = FontManager.GetWobbleFont(Config.Font);

            Container.Size = new ScalableVector2(WindowManager.Width, WindowManager.Height);
            OverlayRoot = new Container
            {
                Parent = Container,
                Size = Container.Size,
                DrawOrder = 1000
            };

            CreateBackground();
            CreateTitle();

            var singleValue = new Bindable<int>(1);
            SingleDropdown = new V2Dropdown<int>(240, singleValue,
                CreateSingleEntries(), Font, Config, OverlayRoot)
            {
                Parent = Container,
                X = 72,
                Y = 138,
                MaxVisibleItems = 6
            };
            SingleDropdown.OptionSelected += OnSingleOptionSelected;

            MultipleDropdown = new V2Dropdown<int>(240, CreateMultipleEntries(),
                new[] { 1, 3 }, Font, Config, OverlayRoot)
            {
                Parent = Container,
                X = 360,
                Y = 138,
                MaxVisibleItems = 4,
                EmptySelectionText = "Nothing selected"
            };
            MultipleDropdown.SelectionChanged += OnMultipleSelectionChanged;

            BottomDropdown = new V2Dropdown<int>(240, singleValue, CreateBottomEntries(),
                Font, Config, OverlayRoot)
            {
                Parent = Container,
                Y = -Config.Height - 30
            };

            SelectionStatus = new SpriteTextPlus(Font, "Selected: One", Config.FontSize)
            {
                Parent = Container,
                X = 360,
                Y = 212,
                Tint = SkinV2Color.Parse(Config.TextColor)
            };

            CreateDialogButton();
            CreateLabels();
            UpdateResponsiveLayout(true);
        }

        public override void Update(GameTime gameTime)
        {
            UpdateResponsiveLayout();
            Container.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            GameBase.Game.GraphicsDevice.Clear(ColorHelper.HexToColor("#081119"));
            Container.Draw(gameTime);
        }

        public override void Destroy()
        {
            SingleDropdown.CloseImmediately();
            MultipleDropdown.CloseImmediately();
            BottomDropdown.CloseImmediately();
            Container.Destroy();
            Skin?.Dispose();
        }

        private void CreateBackground()
        {
            new Sprite
            {
                Parent = Container,
                Size = Container.Size,
                Image = WobbleAssets.WhiteBox,
                Tint = SkinV2Color.Parse(Config.TriggerColor),
                Alpha = 0.35f,
                DrawOrder = -100
            };
        }

        private void CreateTitle()
        {
            new SpriteTextPlus(Font, "Shared V2 Dropdown Gallery", 24)
            {
                Parent = Container,
                X = 72,
                Y = 48,
                Tint = SkinV2Color.Parse(Config.TextColor)
            };

            new SpriteTextPlus(Font,
                "Single select, multi-select, dividers, viewport placement, scrolling and dialog cleanup",
                Config.FontSize)
            {
                Parent = Container,
                X = 72,
                Y = 82,
                Tint = SkinV2Color.Parse(Config.DividerColor)
            };
        }

        private void CreateLabels()
        {
            CreateLabel("Single selection with icons and dividers", 72, 112);
            CreateLabel("Multiple selection with short-label summary", 360, 112);
            CreateLabel("Bottom-edge placement", 0, WindowManager.Height - 82);
        }

        private void CreateDialogButton()
        {
            var button = new RoundedButton((sender, args) =>
                DialogManager.Show(new Quaver.Shared.Graphics.YesNoDialog("Dropdown dialog test",
                    "The dropdown registry should close any open menu before this dialog appears.")))
            {
                Parent = Container,
                X = 72,
                Y = 250,
                Size = new ScalableVector2(240, Config.Height),
                CornerRadius = Config.CornerRadius,
                Tint = SkinV2Color.Parse(Config.TriggerColor),
                PerformHoverFade = true
            };
            button.SetLabel(Font, "Open dialog while testing", Config.FontSize,
                SkinV2Color.Parse(Config.TextColor));
        }

        private void OnSingleOptionSelected(object sender, DropdownOptionEventArgs<int> args)
        {
            SelectionStatus.Text = $"Selected: {args.Option.Label}";
            if (args.Option.Value == 4)
                DialogManager.Show(new Quaver.Shared.Graphics.YesNoDialog("Dropdown dialog test",
                    "Single-select menus close before their callback opens a dialog."));
        }

        private void OnMultipleSelectionChanged(object sender, EventArgs args)
        {
            var selected = MultipleDropdown.SelectedItems;
            SelectionStatus.Text = selected.Count == 0
                ? "Selected: nothing"
                : $"Selected values: {string.Join(", ", selected)}";
        }

        private void UpdateResponsiveLayout(bool force = false)
        {
            if (!force && Math.Abs(LastWidth - WindowManager.Width) < 0.5f &&
                Math.Abs(LastHeight - WindowManager.Height) < 0.5f)
                return;

            LastWidth = WindowManager.Width;
            LastHeight = WindowManager.Height;
            Container.Size = new ScalableVector2(LastWidth, LastHeight);
            OverlayRoot.Size = Container.Size;

            BottomDropdown.X = Math.Max(24, LastWidth - BottomDropdown.Width - 72);
            BottomDropdown.Y = Math.Max(Config.Height + 40, LastHeight - Config.Height - 54);
        }

        private void CreateLabel(string text, float x, float y)
        {
            new SpriteTextPlus(Font, text, Config.FontSize)
            {
                Parent = Container,
                X = x,
                Y = y,
                Tint = SkinV2Color.Parse(Config.TextColor)
            };
        }

        private static TextureRegion Icon(GlobalIcon icon) =>
            Quaver.Shared.Assets.GlobalIcons.Get(icon);

        private static IReadOnlyList<DropdownEntry<int>> CreateSingleEntries() =>
            new List<DropdownEntry<int>>
            {
                new DropdownOption<int>(1, "One", "1", Icon(GlobalIcon.Music)),
                new DropdownOption<int>(2, "Two", "2", Icon(GlobalIcon.Options)),
                new DropdownDivider<int>(),
                new DropdownOption<int>(3, "Three", "3", Icon(GlobalIcon.ViewMap)),
                new DropdownOption<int>(4,
                    "Open dialog from callback - long localized option label", "Dialog",
                    Icon(GlobalIcon.QuestionMark)),
                new DropdownDivider<int>(),
                new DropdownOption<int>(5, "Five", "5", Icon(GlobalIcon.Ready))
            };

        private static IReadOnlyList<DropdownEntry<int>> CreateMultipleEntries() =>
            new List<DropdownEntry<int>>
            {
                new DropdownOption<int>(1, "First option", "First", Icon(GlobalIcon.SinglePlayer)),
                new DropdownOption<int>(2, "Second option", "Second", Icon(GlobalIcon.Multiplayer)),
                new DropdownDivider<int>(),
                new DropdownOption<int>(3, "Third option", "Third", Icon(GlobalIcon.ViewMap)),
                new DropdownOption<int>(4, "Fourth option", "Fourth", Icon(GlobalIcon.Ready)),
                new DropdownOption<int>(5, "Fifth option", "Fifth", Icon(GlobalIcon.Add)),
                new DropdownOption<int>(6, "Sixth option", "Sixth", Icon(GlobalIcon.Download))
            };

        private static IReadOnlyList<DropdownEntry<int>> CreateBottomEntries() =>
            new List<DropdownEntry<int>>
            {
                new DropdownOption<int>(1, "Above one", icon: Icon(GlobalIcon.LessOptions)),
                new DropdownOption<int>(2, "Above two", icon: Icon(GlobalIcon.LessOptions)),
                new DropdownOption<int>(3, "Above three", icon: Icon(GlobalIcon.LessOptions)),
                new DropdownOption<int>(4, "Above four", icon: Icon(GlobalIcon.LessOptions)),
                new DropdownOption<int>(5, "Above five", icon: Icon(GlobalIcon.LessOptions))
            };
    }
}
