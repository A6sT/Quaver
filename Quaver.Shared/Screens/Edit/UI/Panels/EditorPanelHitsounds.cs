using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Quaver.API.Enums;
using Quaver.API.Maps.Structures;
using Quaver.Shared.Assets;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Helpers;
using Quaver.Shared.Screens.Edit.Actions;
using Quaver.Shared.Screens.Edit.Actions.Hitsounds.Add;
using Quaver.Shared.Screens.Edit.Actions.Hitsounds.Remove;
using Quaver.Shared.Screens.Edit.Plugins;
using Wobble.Assets;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Buttons;
using Wobble.Graphics.UI.Tooltips;
using Wobble.Managers;

namespace Quaver.Shared.Screens.Edit.UI.Panels
{
    public class EditorPanelHitsounds : EditorPanel
    {
        /// <summary>
        /// </summary>
        private BindableList<HitObjectInfo> SelectedHitObjects { get; }

        /// <summary>
        /// </summary>
        private EditorActionManager ActionManager { get; }

        /// <summary>
        /// </summary>
        private List<Button> SoundButtons { get; }

        /// <summary>
        /// </summary>
        private Bindable<HitSounds> SelectedHitsounds { get; } = new Bindable<HitSounds>(0);

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="selectedHitObjects"></param>
        /// <param name="manager"></param>
        public EditorPanelHitsounds(BindableList<HitObjectInfo> selectedHitObjects, EditorActionManager manager)
            : base(LocalizationManager.Get("Screen_Editor_Hitsounds"))
        {
            ActionManager = manager;
            SelectedHitObjects = selectedHitObjects;

            Depth = 1;

            SoundButtons = new List<Button>
            {
                new DrawableEditorHitsound(HitSounds.Whistle, SelectedHitsounds, SelectedHitObjects, ActionManager),
                new DrawableEditorHitsound(HitSounds.Finish, SelectedHitsounds, SelectedHitObjects, ActionManager),
                new DrawableEditorHitsound(HitSounds.Clap, SelectedHitsounds, SelectedHitObjects, ActionManager),
                new DrawableEditorKeysoundButton(ActionManager.EditScreen)
            };

            AlignSounds();
            SetSelectedHitsounds();

            SelectedHitObjects.ItemAdded += OnItemAdded;
            SelectedHitObjects.ItemRemoved += OnItemRemoved;
            SelectedHitObjects.MultipleItemsAdded += OnMultipleItemsAdded;
            SelectedHitObjects.ListCleared += OnListCleared;
            ActionManager.HitsoundAdded += OnHitsoundAdded;
            ActionManager.HitsoundRemoved += OnHitsoundRemoved;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public override void Destroy()
        {
            SelectedHitsounds.Dispose();

            // ReSharper disable twice DelegateSubtraction
            SelectedHitObjects.ItemAdded -= OnItemAdded;
            SelectedHitObjects.ItemRemoved -= OnItemRemoved;
            SelectedHitObjects.MultipleItemsAdded -= OnMultipleItemsAdded;
            SelectedHitObjects.ListCleared -= OnListCleared;
            ActionManager.HitsoundAdded -= OnHitsoundAdded;
            ActionManager.HitsoundRemoved -= OnHitsoundRemoved;

            base.Destroy();
        }

        /// <summary>
        /// </summary>
        private void AlignSounds()
        {
            for (var i = 0; i < SoundButtons.Count; i++)
            {
                var sound = SoundButtons[i];

                sound.Parent = Content;
                sound.Size = new ScalableVector2(Content.Width, Content.Height / SoundButtons.Count - 3);
                sound.X = 4;
                sound.Y = sound.Height * i;
            }
        }

        /// <summary>
        /// </summary>
        private void SetSelectedHitsounds()
        {
            if (SelectedHitObjects.Value.Count == 0)
            {
                SelectedHitsounds.Value = 0;
                return;
            }

            var hitsoundList = Enum.GetValues(typeof(HitSounds));

            // Start w/ a combination of all sounds, so ones that aren't active can get removed.
            var activeHitsounds = HitSounds.Clap | HitSounds.Finish | HitSounds.Whistle;

            foreach (var h in SelectedHitObjects.Value)
            {
                foreach (HitSounds hitsound in hitsoundList)
                {
                    // The object doesn't contain this sound, so remove it.
                    if (!h.HitSound.HasFlag(hitsound) && activeHitsounds.HasFlag(hitsound))
                        activeHitsounds -= hitsound;
                }

                if (activeHitsounds == 0)
                    break;
            }

            SelectedHitsounds.Value = activeHitsounds;
        }

        private void OnItemAdded(object sender, BindableListItemAddedEventArgs<HitObjectInfo> e) => SetSelectedHitsounds();

        private void OnItemRemoved(object sender, BindableListItemRemovedEventArgs<HitObjectInfo> e) => SetSelectedHitsounds();

        private void OnMultipleItemsAdded(object sender, BindableListMultipleItemsAddedEventArgs<HitObjectInfo> e) => SetSelectedHitsounds();

        private void OnListCleared(object sender, BindableListClearedEventArgs e) => SetSelectedHitsounds();

        private void OnHitsoundAdded(object sender, EditorHitsoundAddedEventArgs e) => SetSelectedHitsounds();

        private void OnHitsoundRemoved(object sender, EditorHitSoundRemovedEventArgs e) => SetSelectedHitsounds();
    }

    public class DrawableEditorKeysoundButton : ImageButton
    {
        private EditScreen Screen { get; }

        private IDisposable TooltipRegistration { get; }

        private Sprite Icon { get; }

        private SpriteTextPlus Name { get; }

        private Sprite BorderLine { get; }

        public DrawableEditorKeysoundButton(EditScreen screen)
            : base(UserInterface.OptionsSidebarButtonBackground)
        {
            Screen = screen;
            Alpha = 0;

            Icon = new Sprite
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = 17,
                Size = new ScalableVector2(20, 20),
                Image = FontAwesome.Get(FontAwesomeIcon.fa_music_note_black_symbol)
            };

            Name = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold),
                LocalizationManager.Get("Screen_Editor_CustomKeysounds"), 18)
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = Icon.X + Icon.Width + 14
            };

            BorderLine = new Sprite
            {
                Parent = this,
                Size = new ScalableVector2(4, 0),
                Alpha = 0
            };

            TooltipRegistration = this.AddTooltip(new TooltipOptions(
                LocalizationManager.Get("Screen_Editor_CustomKeysoundsTooltip")));
            Clicked += OnClicked;
        }

        private void OnClicked(object sender, EventArgs e)
        {
            if (Screen.Map.Game != MapGame.Quaver)
            {
                NotificationManager.Show(NotificationLevel.Warning,
                    LocalizationManager.Get("Screen_Editor_CannotEditKeysoundsForImportedMap"));
                return;
            }

            Screen.ToggleBuiltinPlugin(EditorBuiltInPlugin.KeysoundEditor);
        }

        public override void Update(GameTime gameTime)
        {
            var isActive = Screen.BuiltInPlugins[EditorBuiltInPlugin.KeysoundEditor].IsActive;

            if (isActive)
            {
                Alpha = 1;
                Tint = ColorHelper.HexToColor("#45D6F5");
                BorderLine.Alpha = 1;
            }
            else if (IsHovered)
            {
                Alpha = 0.45f;
                Tint = Color.White;
                BorderLine.Alpha = Alpha;
            }
            else
            {
                Alpha = 0;
                Tint = Color.White;
                BorderLine.Alpha = 0;
            }

            Icon.Tint = Tint;
            Name.Tint = Tint;
            BorderLine.Height = Height;
            BorderLine.Tint = Tint;
            base.Update(gameTime);
        }

        public override void Destroy()
        {
            Clicked -= OnClicked;
            TooltipRegistration.Dispose();
            base.Destroy();
        }
    }

    public class DrawableEditorHitsound : ImageButton
    {
        /// <summary>
        /// </summary>
        private HitSounds Sound { get; }

        /// <summary>
        /// </summary>
        private Bindable<HitSounds> SelectedHitsounds { get; }

        /// <summary>
        /// </summary>
        private BindableList<HitObjectInfo> SelectedHitObjects { get; }

        /// <summary>
        /// </summary>
        private EditorActionManager ActionManager { get; }

        /// <summary>
        /// </summary>
        private Sprite Icon { get; set; }

        /// <summary>
        /// </summary>
        private SpriteTextPlus Name { get; set; }

        /// <summary>
        /// </summary>
        private Sprite BorderLine { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="selectedHitsounds"></param>
        /// <param name="selectedHitObjects"></param>
        /// <param name="manager"></param>
        public DrawableEditorHitsound(HitSounds sound, Bindable<HitSounds> selectedHitsounds,
            BindableList<HitObjectInfo> selectedHitObjects, EditorActionManager manager) : base(UserInterface.OptionsSidebarButtonBackground)
        {
            Sound = sound;
            SelectedHitsounds = selectedHitsounds;
            SelectedHitObjects = selectedHitObjects;
            ActionManager = manager;

            CreateIcon();
            CreateName();
            CreateLine();

            Alpha = 0;
            BorderLine.Alpha = 0;

            Clicked += OnClicked;
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClicked(object sender, EventArgs e)
        {
            if (SelectedHitObjects.Value.Count == 0)
            {
                NotificationManager.Show(NotificationLevel.Warning,
                    LocalizationManager.Get("Screen_Editor_SelectObjectsBeforeChangingHitsounds"));
                return;
            }

            if (SelectedHitsounds.Value.HasFlag(Sound))
            {
                ActionManager.Perform(new EditorActionRemoveHitsound(ActionManager, new List<HitObjectInfo>(SelectedHitObjects.Value), Sound));
                return;
            }

            ActionManager.Perform(new EditorActionAddHitsound(ActionManager, new List<HitObjectInfo>(SelectedHitObjects.Value), Sound));
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            if (SelectedHitsounds.Value.HasFlag(Sound))
            {
                Alpha = 1;
                Tint = ColorHelper.HexToColor("#45D6F5");
                BorderLine.Alpha = 1;
            }
            else if (IsHovered)
            {
                Alpha = 0.45f;
                Tint = Color.White;
                BorderLine.Alpha = Alpha;
            }
            else
            {
                Tint = Color.White;
                Alpha = 0;
                BorderLine.Alpha = Alpha;
            }

            Name.Tint = Tint;
            Icon.Tint = Tint;

            BorderLine.Height = Height;
            BorderLine.Tint = Tint;

            base.Update(gameTime);
        }

        /// <summary>
        /// </summary>
        private void CreateIcon()
        {
            var icon = GetTexture();

            Icon = new Sprite
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = 17,
                Size = new ScalableVector2(20, 20f * icon.Height / icon.Width),
                Image = icon
            };
        }

        /// <summary>
        /// </summary>
        private void CreateName()
        {
            Name = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold),
                LocalizationManager.Get("Screen_Editor_Hitsound_" + Sound), 18)
            {
                Parent = this,
                Alignment = Alignment.MidLeft,
                X = Icon.X + Icon.Width + 14
            };
        }

        /// <summary>
        /// </summary>
        private void CreateLine() => BorderLine = new Sprite
        {
            Parent = this,
            Size = new ScalableVector2(4, 0)
        };

        /// <summary>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private Texture2D GetTexture()
        {
            switch (Sound)
            {
                case HitSounds.Whistle:
                    return UserInterface.EditorIconWhistle;
                case HitSounds.Finish:
                    return UserInterface.EditorIconFinish;
                case HitSounds.Clap:
                    return UserInterface.EditorIconClap;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
