using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using Quaver.Server.Client.Structures.Modding;
using Quaver.Shared.Assets;
using Quaver.Shared.Graphics.Containers;
using Quaver.Shared.Helpers;
using Quaver.Shared.Screens.Menu.UI.Jukebox;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Buttons;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Input;
using Wobble.Managers;
using ColorHelper = Quaver.Shared.Helpers.ColorHelper;

namespace Quaver.Shared.Screens.Edit.UI.Modding
{
    internal sealed class EditorModdingTimeline : PoolableScrollContainer<MapModdingEntry>
    {
        public EditorModdingPanel Panel { get; }

        public EditorModdingTimeline(EditorModdingPanel panel, ScalableVector2 size)
            : base([], 7, 0, size, size)
        {
            Panel = panel;
            Tint = ColorHelper.HexToColor("#242424");
            AddBorder(ColorHelper.HexToColor("#BEBEBE"), 2);
            Border.Alpha = 0.5f;

            EasingType = Easing.OutQuint;
            TimeToCompleteScroll = 250;
            ScrollSpeed = 180;
            CapturesMouseWheelInput = true;
            AllowScrollbarDragging = true;
            Scrollbar.Tint = ColorHelper.HexToColor("#656565");
            Scrollbar.Width = 4;
            Scrollbar.X = 8;

            CreatePool();
        }

        public override void Update(GameTime gameTime)
        {
            InputEnabled = GraphicsHelper.RectangleContains(ScreenRectangle, MouseManager.CurrentState.Position)
                           && DialogManager.Dialogs.Count == 0
                           && !KeyboardManager.CurrentState.IsKeyDown(Keys.LeftAlt)
                           && !KeyboardManager.CurrentState.IsKeyDown(Keys.RightAlt);

            base.Update(gameTime);
        }

        public void SetItems(List<MapModdingEntry> mods)
        {
            Pool?.ForEach(x => x.Destroy());
            Pool = [];
            AvailableItems = mods ?? [];
            PoolStartingIndex = 0;
            ContentContainer.Y = 0;
            TargetY = 0;
            PreviousTargetY = 0;
            PreviousContentContainerY = 0;
            CreatePool();
            RecalculateContainerHeight();
        }

        public void RefreshSelection()
        {
            if (Pool == null)
                return;

            foreach (var row in Pool)
                row.UpdateContent(row.Item, row.Index);
        }

        public bool IsSelected(MapModdingEntry mod) => Panel.SelectedMod?.Id == mod?.Id;

        public void Select(MapModdingEntry mod) => Panel.SelectMod(mod);

        public void GoToSelection(MapModdingEntry mod) => Panel.GoToSelection(mod);

        protected override PoolableSprite<MapModdingEntry> CreateObject(MapModdingEntry item, int index)
            => new DrawableEditorModdingTimelineRow(this, item, index);
    }

    internal sealed class DrawableEditorModdingTimelineRow : PoolableSprite<MapModdingEntry>
    {
        public sealed override int HEIGHT { get; } = 48;

        private EditorModdingTimeline Timeline => (EditorModdingTimeline)Container;

        private ContainedRoundedButton RowButton { get; }

        private ContainedRoundedButton TimeButton { get; }

        private SpriteTextPlus Status { get; }

        private SpriteTextPlus Type { get; }

        private SpriteTextPlus Author { get; }

        private SpriteTextPlus Preview { get; }

        public DrawableEditorModdingTimelineRow(EditorModdingTimeline container, MapModdingEntry item, int index) : base(container, item, index)
        {
            Size = new ScalableVector2(container.Width, HEIGHT);

            RowButton = new ContainedRoundedButton(container, (sender, args) => Timeline.Select(Item))
            {
                Parent = this,
                Size = Size,
                CornerRadius = 0,
                Tint = ColorHelper.HexToColor("#242424"),
                Depth = -1
            };

            TimeButton = new ContainedRoundedButton(container, (sender, args) =>
            {
                Timeline.Select(Item);
                Timeline.GoToSelection(Item);
            })
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = 8,
                Size = new ScalableVector2(102, 30),
                CornerRadius = 5,
                Tint = ColorHelper.HexToColor("#353535"),
                Depth = -2
            };
            TimeButton.SetLabel(FontManager.GetWobbleFont(Fonts.InterSemiBold), "", 16, ColorHelper.HexToColor("#45D6F5"));

            Status = CreateColumnText(122, 88);
            Type = CreateColumnText(220, 98);
            Author = CreateColumnText(328, 120);
            Preview = CreateColumnText(458, Width - 474);

            new Sprite
            {
                Parent = this,
                Alignment = Alignment.BotLeft,
                Size = new ScalableVector2(Width, 1),
                Tint = ColorHelper.HexToColor("#BEBEBE"),
                Alpha = 0.35f
            };

            UpdateContent(item, index);
        }

        public override void UpdateContent(MapModdingEntry item, int index)
        {
            Item = item;
            Index = index;

            var hasTimestamp = EditorModdingFormatting.GetDisplayTime(item).HasValue;
            TimeButton.SetLabel(FontManager.GetWobbleFont(Fonts.InterSemiBold), EditorModdingFormatting.FormatDisplayTime(item), 16, hasTimestamp ? ColorHelper.HexToColor("#45D6F5") : Color.Gray);
            TimeButton.IsClickable = hasTimestamp;

            Status.Text = EditorModdingFormatting.GetStatusLabel(item.Status);
            Status.Tint = EditorModdingFormatting.GetStatusColor(item.Status);
            Status.TruncateWithEllipsis(82);

            Type.Text = EditorModdingFormatting.GetTypeLabel(item.Type);
            Type.TruncateWithEllipsis(92);

            Author.Text = item.Author?.Username ?? LocalizationManager.Get("Screen_Editor_ModdingUnknownAuthor");
            Author.TruncateWithEllipsis(114);

            Preview.Text = EditorModdingFormatting.GetPreview(item.Comment);
            Preview.TruncateWithEllipsis((int)Math.Max(30, Width - Preview.X - 18));

            RowButton.Tint = Timeline.IsSelected(item)
                ? ColorHelper.HexToColor("#35444A")
                : ColorHelper.HexToColor("#242424");
        }

        private SpriteTextPlus CreateColumnText(float x, float maxWidth)
        {
            var text = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterSemiBold), "", 16)
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = x,
                MaxWidth = maxWidth
            };
            return text;
        }
    }

    internal sealed class ContainedRoundedButton(Drawable container, EventHandler clickAction = null) : RoundedButton(clickAction)
    {
        private Drawable Container { get; } = container;

        protected override bool IsMouseInClickArea()
        {
            if (Container == null)
                return base.IsMouseInClickArea();

            var clickArea = RectangleF.Intersection(ScreenRectangle, Container.ScreenRectangle);
            return GraphicsHelper.RectangleContains(clickArea, MouseManager.CurrentState.Position);
        }
    }
}