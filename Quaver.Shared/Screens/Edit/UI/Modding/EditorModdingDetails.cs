using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Quaver.Server.Client.Structures.Modding;
using Quaver.Shared.Assets;
using Quaver.Shared.Graphics;
using Quaver.Shared.Helpers;
using Quaver.Shared.Screens.Menu.UI.Jukebox;
using Wobble.Assets;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Buttons;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Graphics.UI.Form;
using Wobble.Input;
using Wobble.Managers;

namespace Quaver.Shared.Screens.Edit.UI.Modding
{
    internal enum EditorModdingDetailsMode
    {
        Empty,
        Discussion,
        NewMod,
        Reply
    }

    internal sealed class EditorModdingDetails : Container
    {
        private const int Padding = 16;

        private const int FooterHeight = 48;

        private EditorModdingPanel Panel { get; }

        private EditScreen Screen { get; }

        private Container Content { get; set; }

        private Textarea Composer { get; set; }

        private RoundedButton SubmitButton { get; set; }

        private RoundedButton CancelButton { get; set; }

        private RoundedButton AcceptButton { get; set; }

        private RoundedButton DenyButton { get; set; }

        private RoundedButton ReplyButton { get; set; }

        private SpriteTextPlus LocationValue { get; set; }

        private EditorModdingTypeDropdown TypeDropdown { get; set; }

        private string NewModSelection { get; set; }

        private bool Busy { get; set; }

        private EditorModdingDetailsMode Mode { get; set; }

        public bool HasDraft => (Mode == EditorModdingDetailsMode.NewMod || Mode == EditorModdingDetailsMode.Reply) &&
                                !string.IsNullOrWhiteSpace(Composer?.RawText);

        public EditorModdingDetails(EditorModdingPanel panel, EditScreen screen, ScalableVector2 size)
        {
            Panel = panel;
            Screen = screen;
            Size = size;

            var background = new Sprite
            {
                Parent = this,
                Size = Size,
                Tint = ColorHelper.HexToColor("#242424")
            };
            background.AddBorder(ColorHelper.HexToColor("#BEBEBE"), 2);
            background.Border.Alpha = 0.5f;
        }

        public void ShowEmpty()
        {
            Mode = EditorModdingDetailsMode.Empty;
            ResetContent();

            new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterSemiBold),
                LocalizationManager.Get("Screen_Editor_ModdingSelectMod"), 18)
            {
                Parent = Content,
                Alignment = Alignment.MidCenter,
                Tint = Color.Gray
            };
        }

        public void ShowDiscussion(MapModdingEntry mod, bool isMapCreator)
        {
            if (mod == null)
            {
                ShowEmpty();
                return;
            }

            Mode = EditorModdingDetailsMode.Discussion;
            ResetContent();

            var scroll = new ScrollContainer(new ScalableVector2(Width, Height - FooterHeight), new ScalableVector2(Width, Height - FooterHeight))
            {
                Parent = Content,
                Tint = ColorHelper.HexToColor("#242424"),
                InputEnabled = true,
                CapturesMouseWheelInput = true,
                AllowScrollbarDragging = true,
                ScrollSpeed = 150,
                EasingType = Easing.OutQuint,
                TimeToCompleteScroll = 200
            };
            scroll.Scrollbar.Tint = ColorHelper.HexToColor("#656565");
            scroll.Scrollbar.Width = 4;

            var y = (float)Padding;
            var status = CreateBadge(EditorModdingFormatting.GetStatusLabel(mod.Status), EditorModdingFormatting.GetStatusColor(mod.Status), 104);
            status.Position = new ScalableVector2(Padding, y);
            scroll.AddContainedDrawable(status);

            var type = CreateBadge(EditorModdingFormatting.GetTypeLabel(mod.Type), ColorHelper.HexToColor("#454545"), 112);
            type.Position = new ScalableVector2(status.X + status.Width + 8, y);
            scroll.AddContainedDrawable(type);

            var author = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterSemiBold), mod.Author?.Username ?? LocalizationManager.Get("Screen_Editor_ModdingUnknownAuthor"), 17)
            {
                Position = new ScalableVector2(type.X + type.Width + 12, y + 4),
                Tint = Color.White
            };
            scroll.AddContainedDrawable(author);

            var date = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterSemiBold), mod.Timestamp.ToLocalTime().ToString("g"), 13)
            {
                Alignment = Alignment.TopRight,
                Position = new ScalableVector2(-Padding - 8, y + 6),
                Tint = Color.Gray
            };
            scroll.AddContainedDrawable(date);

            y += 44;
            var comment = CreateWrappedText(mod.Comment, 17, Color.White, Width - Padding * 2 - 10);
            comment.Position = new ScalableVector2(Padding, y);
            scroll.AddContainedDrawable(comment);
            y += comment.Height + 18;

            if (mod.Replies != null)
            {
                foreach (var reply in mod.Replies)
                {
                    var divider = new Sprite
                    {
                        Position = new ScalableVector2(Padding, y),
                        Size = new ScalableVector2(Width - Padding * 2 - 10, 1),
                        Tint = ColorHelper.HexToColor("#BEBEBE"),
                        Alpha = 0.3f
                    };
                    scroll.AddContainedDrawable(divider);
                    y += 12;

                    var replyAuthor = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterSemiBold), reply.Author?.Username ?? LocalizationManager.Get("Screen_Editor_ModdingUnknownAuthor"), 15)
                    {
                        Position = new ScalableVector2(Padding, y),
                        Tint = ColorHelper.HexToColor("#45D6F5")
                    };
                    scroll.AddContainedDrawable(replyAuthor);

                    var replyDate = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterSemiBold), reply.Timestamp.ToLocalTime().ToString("g"), 12)
                    {
                        Alignment = Alignment.TopRight,
                        Position = new ScalableVector2(-Padding - 8, y + 2),
                        Tint = Color.Gray
                    };
                    scroll.AddContainedDrawable(replyDate);

                    y += replyAuthor.Height + 5;

                    var replyText = CreateWrappedText(reply.Comment, 15, Color.White, Width - Padding * 2 - 10);
                    replyText.Position = new ScalableVector2(Padding, y);
                    scroll.AddContainedDrawable(replyText);
                    y += replyText.Height + 14;
                }
            }

            scroll.ContentContainer.Height = Math.Max(scroll.Height, y + Padding);
            CreateDiscussionActions(mod, isMapCreator);
        }

        public void ShowNewModComposer(string selection)
        {
            Mode = EditorModdingDetailsMode.NewMod;
            NewModSelection = selection;
            ResetContent();

            CreateComposerTitle("Screen_Editor_ModdingNewMod");

            TypeDropdown = new EditorModdingTypeDropdown
            {
                Parent = Content,
                Position = new ScalableVector2(Padding, 48)
            };

            CreateLocationRow();
            CreateComposer(126, "Screen_Editor_ModdingDescribePlaceholder");
        }

        public void ShowReplyComposer(MapModdingEntry mod)
        {
            Mode = EditorModdingDetailsMode.Reply;
            ResetContent();

            CreateComposerTitle("Screen_Editor_ModdingReply");
            var replyingTo = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterSemiBold), LocalizationManager.Get("Screen_Editor_ModdingReplyingTo", mod.Author?.Username ?? "-"), 14)
            {
                Parent = Content,
                Position = new ScalableVector2(Padding, 46),
                Tint = Color.Gray
            };
            replyingTo.TruncateWithEllipsis((int)Width - Padding * 2);

            CreateComposer(76, "Screen_Editor_ModdingReplyPlaceholder");
        }

        public void CloseComposer()
        {
            if (Panel.SelectedMod != null)
                ShowDiscussion(Panel.SelectedMod, Panel.IsCurrentUserMapCreator);
            else
                ShowEmpty();
        }

        public void SetBusy(bool busy)
        {
            Busy = busy;
            SetButtonState(SubmitButton, !busy);
            SetButtonState(CancelButton, !busy);
            SetButtonState(AcceptButton, !busy);
            SetButtonState(DenyButton, !busy);
            SetButtonState(ReplyButton, !busy);
        }

        private void CreateDiscussionActions(MapModdingEntry mod, bool isMapCreator)
        {
            var footer = new FlexContainer
            {
                Parent = Content,
                Position = new ScalableVector2(Padding, Height - FooterHeight + 8),
                Size = new ScalableVector2(Width - Padding * 2, 32),
                Direction = FlexDirection.Row,
                AlignItems = FlexAlignItems.Center,
                ColumnGap = 8
            };

            if (isMapCreator && mod.Status == MapModdingStatus.Pending)
            {
                AcceptButton = CreateButton("Screen_Editor_ModdingAccept", 92, ColorHelper.HexToColor("#27B06E"),
                    (sender, args) => Panel.UpdateStatus(MapModdingStatus.Accepted));
                AcceptButton.Parent = footer;

                DenyButton = CreateButton("Screen_Editor_ModdingDeny", 82, ColorHelper.HexToColor("#F9645D"),
                    (sender, args) => Panel.UpdateStatus(MapModdingStatus.Denied));
                DenyButton.Parent = footer;
            }

            var spacer = new Container { Parent = footer, Size = new ScalableVector2(1, 1) };
            footer.SetItemOptions(spacer, new FlexItemOptions { Grow = 1 });

            ReplyButton = CreateButton("Screen_Editor_ModdingReply", 92, ColorHelper.HexToColor("#363636"), (sender, args) => Panel.ShowReplyComposer());
            ReplyButton.Parent = footer;
            footer.RefreshLayout();
        }

        private void CreateLocationRow()
        {
            var row = new FlexContainer
            {
                Parent = Content,
                Position = new ScalableVector2(Padding, 86),
                Size = new ScalableVector2(Width - Padding * 2, 32),
                Direction = FlexDirection.Row,
                AlignItems = FlexAlignItems.Center,
                ColumnGap = 8
            };

            new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterSemiBold), LocalizationManager.Get("Screen_Editor_ModdingLocationLabel"), 18)
            {
                Parent = row
            };

            LocationValue = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterSemiBold), "", 16)
            {
                Parent = row,
                Tint = ColorHelper.HexToColor("#45D6F5")
            };
            row.SetItemOptions(LocationValue, new FlexItemOptions { Grow = 1, Shrink = 1 });

            var capture = CreateButton("Screen_Editor_ModdingUseCurrentSelection", 178, ColorHelper.HexToColor("#363636"),
                (sender, args) =>
                {
                    NewModSelection = Screen.GetSelectedObjectTimestamps();
                    UpdateLocationValue();
                });
            capture.Parent = row;

            var clear = new RoundedButton((sender, args) =>
            {
                NewModSelection = null;
                UpdateLocationValue();
            })
            {
                Parent = row,
                Size = new ScalableVector2(30, 30),
                CornerRadius = 5,
                Tint = ColorHelper.HexToColor("#4A4A4A"),
                Depth = -1
            };
            clear.SetIcon(FontAwesome.Get(FontAwesomeIcon.fa_times), new Vector2(11, 11));

            row.RefreshLayout();
            UpdateLocationValue();
        }

        private void CreateComposer(float y, string placeholderKey)
        {
            var composerHeight = Math.Max(80, Height - y - FooterHeight - 8);
            Composer = new Textarea(new ScalableVector2(Width - Padding * 2, composerHeight), FontManager.GetWobbleFont(Fonts.InterSemiBold), 16, "", LocalizationManager.Get(placeholderKey))
            {
                Parent = Content,
                Position = new ScalableVector2(Padding, y),
                Tint = ColorHelper.HexToColor("#181818"),
                AllowNewLines = true,
                MaxCharacters = 5000,
                Focused = false
            };
            Composer.AddBorder(ColorHelper.HexToColor("#656565"), 1);
            Composer.Button.Depth = -1;

            var footer = new FlexContainer
            {
                Parent = Content,
                Position = new ScalableVector2(Padding, Height - FooterHeight + 8),
                Size = new ScalableVector2(Width - Padding * 2, 32),
                Direction = FlexDirection.Row,
                JustifyContent = FlexJustifyContent.FlexEnd,
                AlignItems = FlexAlignItems.Center,
                ColumnGap = 8
            };

            CancelButton = CreateButton("SkinEditor_Cancel", 92, ColorHelper.HexToColor("#4A4A4A"),
                (sender, args) => CloseComposer());
            CancelButton.Parent = footer;

            SubmitButton = CreateButton("Screen_Editor_ModdingSubmit", 94, ColorHelper.HexToColor("#27B06E"),
                (sender, args) => SubmitComposer());
            SubmitButton.Parent = footer;
            footer.RefreshLayout();
            SetBusy(Busy);
        }

        private void SubmitComposer()
        {
            if (Busy || string.IsNullOrWhiteSpace(Composer?.RawText))
                return;

            if (Mode == EditorModdingDetailsMode.NewMod)
            {
                var type = TypeDropdown.Dropdown.SelectedIndex == 1
                    ? MapModdingType.Suggestion
                    : MapModdingType.Issue;
                Panel.SubmitNewMod(type, NewModSelection, Composer.RawText.Trim());
            }
            else if (Mode == EditorModdingDetailsMode.Reply)
                Panel.SubmitReply(Composer.RawText.Trim());
        }

        private void CreateComposerTitle(string localizationKey)
        {
            new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterSemiBold), LocalizationManager.Get(localizationKey), 20)
            {
                Parent = Content,
                Position = new ScalableVector2(Padding, 14),
                Tint = Color.White
            };
        }

        private void UpdateLocationValue()
        {
            if (LocationValue == null)
                return;

            LocationValue.Text = string.IsNullOrWhiteSpace(NewModSelection)
                ? LocalizationManager.Get("Screen_Editor_ModdingGeneral")
                : EditorModdingFormatting.FormatDisplayTime(new MapModdingEntry
                {
                    MapTimestamp = NewModSelection
                });
            LocationValue.TruncateWithEllipsis(125);
        }

        private void ResetContent()
        {
            Content?.Destroy();
            Content = new Container
            {
                Parent = this,
                Size = Size
            };

            Composer = null;
            SubmitButton = null;
            CancelButton = null;
            AcceptButton = null;
            DenyButton = null;
            ReplyButton = null;
            LocationValue = null;
            TypeDropdown = null;
            Busy = false;
        }

        private static RoundedButton CreateBadge(string label, Color color, float width)
        {
            var badge = new RoundedButton
            {
                Size = new ScalableVector2(width, 28),
                CornerRadius = 5,
                Tint = color,
                IsInteractionEnabled = false
            };
            badge.SetLabel(FontManager.GetWobbleFont(Fonts.InterSemiBold), label, 14, Color.White);
            return badge;
        }

        private static RoundedButton CreateButton(string localizationKey, float width, Color color, EventHandler clicked)
        {
            var button = new RoundedButton(clicked)
            {
                Size = new ScalableVector2(width, 32),
                CornerRadius = 6,
                Tint = color,
                Depth = -1
            };
            button.SetLabel(FontManager.GetWobbleFont(Fonts.InterSemiBold),
                LocalizationManager.Get(localizationKey), 16, Color.White);
            return button;
        }

        private static SpriteTextPlus CreateWrappedText(string text, int fontSize, Color color, float maxWidth)
            => new(FontManager.GetWobbleFont(Fonts.InterSemiBold), text ?? "", fontSize)
            {
                MaxWidth = maxWidth,
                Tint = color
            };

        private static void SetButtonState(RoundedButton button, bool enabled)
        {
            if (button == null)
                return;

            button.IsClickable = enabled;
            button.Alpha = enabled ? 1 : 0.5f;
        }
    }
}