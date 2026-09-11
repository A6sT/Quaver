using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Quaver.Shared.Online.API.MapsetSearch;
using Quaver.Shared.Screens.V2.Downloading.UI;
using Quaver.Shared.Screens.V2.SkinEditor;
using Quaver.Shared.Screens.V2.UI;
using Quaver.Shared.Skinning;
using Quaver.Shared.Skinning.V2;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.UI.Navigation;
using Wobble.Managers;
using Wobble.Screens;
using Wobble.Window;

namespace Quaver.Shared.Screens.V2.Downloading
{
    internal sealed class DownloadingScreenView : ScreenView, ISkinV2EditorHost
    {
        private SkinStoreV2Lease Skin { get; }

        private SkinV2Config RootConfig { get; set; }

        private SkinV2DownloadingConfig Config => RootConfig.Screens.Downloading;

        private SkinV2NavigationConfig NavigationConfig => RootConfig.Shared.Navigation;

        private Color BackgroundClearColor { get; set; }

        private NavigationBar Background { get; set; }

        private DownloadingSearchPanel SearchPanel { get; set; }

        private Container ContentRoot { get; set; }

        private FlexContainer ContentLayout { get; set; }

        private FlexContainer BodyLayout { get; set; }

        private ScrollContainer MapsetScrollContainer { get; set; }

        private FlexContainer MapsetGrid { get; set; }

        private List<FlexContainer> MapsetRows { get; set; }

        private List<DownloadingMapsetContainer> MapsetContainers { get; set; }

        private float LastMapsetViewportWidth { get; set; } = -1;

        private float LastMapsetViewportHeight { get; set; } = -1;

        private float LastWindowWidth { get; set; } = -1;

        private float LastWindowHeight { get; set; } = -1;

        private bool EditorLayoutActive { get; set; }

        private float EditorLeftWidth { get; set; }

        private float EditorRightWidth { get; set; }

        private float EditorBottomHeight { get; set; }

        private List<SkinEditorTarget> editorTargets = new List<SkinEditorTarget>();

        private float NavigationBarHeight =>
            NavigationConfig.Button.Size + NavigationConfig.EdgePadding * 2;

        public Container PreviewRoot { get; }

        public Container EditorRoot { get; }

        public string EditorGroupLabel =>
            LocalizationManager.Get("SkinEditor_Group_Download");

        public IReadOnlyList<SkinEditorTarget> EditorTargets => editorTargets;

        public DownloadingScreenView(DownloadingScreen screen) : base(screen)
        {
            Skin = SkinManager.AcquireV2();
            RootConfig = Skin.Config;

            Container.Size = new ScalableVector2(WindowManager.Width, WindowManager.Height);
            PreviewRoot = new Container
            {
                Parent = Container,
                Size = new ScalableVector2(WindowManager.Width, WindowManager.Height),
                Pivot = Vector2.Zero
            };
            EditorRoot = new Container
            {
                Parent = Container,
                Size = new ScalableVector2(WindowManager.Width, WindowManager.Height),
                Visible = false
            };

            BuildContent();
        }

        public override void Update(GameTime gameTime)
        {
            UpdateResponsiveLayout();
            if (MapsetScrollContainer != null)
                MapsetScrollContainer.InputEnabled = MapsetScrollContainer.IsHovered();

            Container.Update(gameTime);
            RefreshMapsetViewportLayout();
        }

        public override void Draw(GameTime gameTime)
        {
            GameBase.Game.GraphicsDevice.Clear(BackgroundClearColor);
            Container.Draw(gameTime);
        }

        public override void Destroy()
        {
            Container.Destroy();
            Skin.Dispose();
        }

        public void EnsureNavigation()
        {
            var navigation = ScreenNavigation.EnsureAttached(PreviewRoot);
            ConfigureNavigation(navigation);
            AddNavigationTargets(navigation);
        }

        public void ApplySkinEditorPreview(SkinV2Config config)
        {
            RootConfig = config;
            BuildContent();
            var navigation = ScreenNavigation.ReplaceAttached(PreviewRoot, RootConfig);
            ConfigureNavigation(navigation);
            AddNavigationTargets(navigation);
            UpdateEditorLayout();
        }

        public void SetSkinEditorLayout(bool active, float leftPanelWidth = 0,
            float rightPanelWidth = 0, float assetPanelHeight = 0)
        {
            EditorLayoutActive = active;
            EditorLeftWidth = leftPanelWidth;
            EditorRightWidth = rightPanelWidth;
            EditorBottomHeight = assetPanelHeight;
            EditorRoot.Visible = active;
            UpdateEditorLayout();
        }

        private void BuildContent()
        {
            ContentRoot?.Destroy();
            ContentRoot = new Container
            {
                Parent = PreviewRoot,
                Size = new ScalableVector2(WindowManager.Width, WindowManager.Height)
            };

            BackgroundClearColor = SkinV2Color.Parse(Config.Background.SolidColor);
            Background = new NavigationBar(WindowManager.Width, WindowManager.Height)
            {
                Parent = ContentRoot,
                Background = SkinV2Background.Create(Skin, Config.Background)
            };

            ContentLayout = new FlexContainer
            {
                Parent = ContentRoot,
                Size = new ScalableVector2(WindowManager.Width, WindowManager.Height),
                Direction = FlexDirection.Column,
                AlignItems = FlexAlignItems.Stretch,
                UsePreviousSpriteBatchOptions = true
            };

            var navigationSpacer = CreateSpacer(ContentLayout, 1,
                NavigationBarHeight + Config.Layout.TopPadding);
            ContentLayout.SetItemOptions(navigationSpacer, FixedBasis(
                NavigationBarHeight + Config.Layout.TopPadding));

            var contentRow = new FlexContainer
            {
                Parent = ContentLayout,
                Direction = FlexDirection.Row,
                AlignItems = FlexAlignItems.Stretch,
                UsePreviousSpriteBatchOptions = true
            };
            ContentLayout.SetItemOptions(contentRow, new FlexItemOptions
            {
                Basis = 1,
                Grow = 1,
                Shrink = 1
            });

            var leftSpacer = CreateSpacer(contentRow, Config.Layout.HorizontalPadding, 1);
            contentRow.SetItemOptions(leftSpacer, FixedBasis(Config.Layout.HorizontalPadding));

            BodyLayout = new FlexContainer
            {
                Parent = contentRow,
                Direction = FlexDirection.Column,
                AlignItems = FlexAlignItems.Stretch,
                RowGap = Config.Layout.ContentGap,
                UsePreviousSpriteBatchOptions = true
            };
            contentRow.SetItemOptions(BodyLayout, new FlexItemOptions
            {
                Basis = 1,
                Grow = 1,
                Shrink = 1
            });

            var rightSpacer = CreateSpacer(contentRow, Config.Layout.HorizontalPadding, 1);
            contentRow.SetItemOptions(rightSpacer, FixedBasis(Config.Layout.HorizontalPadding));

            SearchPanel = new DownloadingSearchPanel(1,
                ((DownloadingScreen) Screen).SearchState, Config, PreviewRoot,
                RootConfig.Shared.Dropdown)
            {
                Parent = BodyLayout
            };
            BodyLayout.SetItemOptions(SearchPanel, new FlexItemOptions
            {
                Grow = 0,
                Shrink = 0
            });

            MapsetScrollContainer = new ScrollContainer(new ScalableVector2(1, 1),
                new ScalableVector2(1, 1))
            {
                Parent = BodyLayout,
                // ScrollContainer derives from Sprite and draws its fallback texture when it has no image.
                // Keep the viewport itself transparent; only its mapset children and scrollbar should render.
                Tint = Color.Transparent,
                InputEnabled = true,
                AllowScrollbarDragging = true,
                ScrollSpeed = Config.Mapset.ScrollSpeed,
                UsePreviousSpriteBatchOptions = true
            };
            MapsetScrollContainer.Scrollbar.Width = Config.Mapset.ScrollbarWidth;
            MapsetScrollContainer.Scrollbar.Tint = SkinV2Color.Parse(Config.Mapset.ScrollbarColor);
            BodyLayout.SetItemOptions(MapsetScrollContainer, new FlexItemOptions
            {
                Basis = 1,
                Grow = 1,
                Shrink = 1
            });

            MapsetGrid = new FlexContainer
            {
                Parent = MapsetScrollContainer.ContentContainer,
                Direction = FlexDirection.Column,
                AlignItems = FlexAlignItems.Stretch,
                AlignContent = FlexAlignContent.FlexStart,
                RowGap = Config.Mapset.GridRowGap,
                UsePreviousSpriteBatchOptions = true
            };

            BuildMapsetCards(((DownloadingScreen) Screen).Mapsets);

            // The dropdown menus are children of SearchPanel. Keep the panel first in the
            // flex layout, but move it after the mapset container in drawable child order so open menus
            // render above mapsets instead of being covered by them.
            SearchPanel.Parent = BodyLayout;
            BodyLayout.SetItemOptions(SearchPanel, new FlexItemOptions
            {
                Order = -1,
                Grow = 0,
                Shrink = 0
            });

            editorTargets = new List<SkinEditorTarget>
            {
                new SkinEditorTarget("downloading-background",
                    LocalizationManager.Get("SkinEditor_Component_Background"),
                    "Screens.Downloading.Background", Background),
                new SkinEditorTarget("downloading-search",
                    LocalizationManager.Get("SkinEditor_Component_SearchArea"),
                    "Screens.Downloading.SearchArea", SearchPanel),
                new SkinEditorTarget("downloading-search-fields",
                    LocalizationManager.Get("SkinEditor_Component_SearchFields"),
                    "Screens.Downloading.Field", SearchPanel),
                new SkinEditorTarget("downloading-search-buttons",
                    LocalizationManager.Get("SkinEditor_Component_SearchButtons"),
                    "Screens.Downloading.Button", SearchPanel),
                new SkinEditorTarget("downloading-search-dropdowns",
                    LocalizationManager.Get("SkinEditor_Component_SearchDropdowns"),
                    "Shared.Dropdown", SearchPanel),
                new SkinEditorTarget("downloading-search-sliders",
                    LocalizationManager.Get("SkinEditor_Component_SearchSliders"),
                    "Screens.Downloading.Range", SearchPanel),
                new SkinEditorTarget("downloading-mapset",
                    LocalizationManager.Get("SkinEditor_Component_Mapset"),
                    "Screens.Downloading.Mapset", MapsetGrid)
            };

            LastWindowWidth = -1;
            LastWindowHeight = -1;
            LastMapsetViewportWidth = -1;
            LastMapsetViewportHeight = -1;
            UpdateResponsiveLayout(true);
        }

        private void ConfigureNavigation(ScreenNavigation navigation)
        {
            var screen = (DownloadingScreen) Screen;
            navigation.ShowApplicationTopBar(QuaverScreenType.Download);
            navigation.ShowDownloadingFooter(screen.ExitToPreviousScreen,
                screen.ShowRecommendedDifficultyDialog);
        }

        private void AddNavigationTargets(ScreenNavigation navigation) =>
            editorTargets.AddRange(navigation.GetSkinEditorTargets());

        private void UpdateResponsiveLayout(bool force = false)
        {
            var width = WindowManager.Width;
            var height = WindowManager.Height;
            if (!force && Math.Abs(width - LastWindowWidth) < 0.001f &&
                Math.Abs(height - LastWindowHeight) < 0.001f)
                return;

            LastWindowWidth = width;
            LastWindowHeight = height;
            Container.Size = new ScalableVector2(width, height);
            PreviewRoot.Size = new ScalableVector2(width, height);
            ContentRoot.Size = new ScalableVector2(width, height);
            Background.Size = new ScalableVector2(width, height);
            ContentLayout.Size = new ScalableVector2(width, height);
            ContentLayout.RefreshLayout();
            BodyLayout.RefreshLayout();
            RefreshMapsetViewportLayout(true);
            UpdateEditorLayout();
        }

        private void RefreshMapsetViewportLayout(bool force = false)
        {
            if (MapsetScrollContainer == null || MapsetGrid == null || MapsetRows == null ||
                MapsetScrollContainer.Width <= 0 || MapsetScrollContainer.Height <= 0)
                return;

            var width = MapsetScrollContainer.Width;
            var height = MapsetScrollContainer.Height;
            if (!force && Math.Abs(width - LastMapsetViewportWidth) < 0.001f &&
                Math.Abs(height - LastMapsetViewportHeight) < 0.001f)
                return;

            LastMapsetViewportWidth = width;
            LastMapsetViewportHeight = height;
            var rowCount = MapsetRows.Count;
            var contentHeight = rowCount * Config.Mapset.Height +
                                Math.Max(0, rowCount - 1) * Config.Mapset.GridRowGap;
            var contentSize = new ScalableVector2(width, contentHeight);
            MapsetScrollContainer.ContentContainer.Size = contentSize;
            MapsetGrid.Size = contentSize;
            MapsetGrid.RefreshLayout();
            foreach (var mapsetRow in MapsetRows)
                mapsetRow.RefreshLayout();
        }

        private static Container CreateSpacer(Drawable parent, float width, float height) => new Container
        {
            Parent = parent,
            Size = new ScalableVector2(width, height),
            UsePreviousSpriteBatchOptions = true
        };

        private static FlexItemOptions FixedBasis(float basis) => new FlexItemOptions
        {
            Basis = basis,
            Grow = 0,
            Shrink = 0
        };

        internal void SetMapsets(IReadOnlyList<DownloadableMapset> mapsets)
        {
            BuildMapsetCards(mapsets ?? Array.Empty<DownloadableMapset>());
            MapsetScrollContainer.TargetY = 0;
            MapsetScrollContainer.PreviousTargetY = 0;
            MapsetScrollContainer.ContentContainer.Y = 0;
            LastMapsetViewportWidth = -1;
            LastMapsetViewportHeight = -1;
            RefreshMapsetViewportLayout(true);
        }

        private void BuildMapsetCards(IReadOnlyList<DownloadableMapset> mapsets)
        {
            if (MapsetRows != null)
            {
                foreach (var row in MapsetRows)
                    row.Destroy();
            }

            MapsetRows = new List<FlexContainer>();
            MapsetContainers = new List<DownloadingMapsetContainer>();

            for (var i = 0; i < mapsets.Count; i++)
            {
                if (i % Config.Mapset.GridColumns == 0)
                {
                    var mapsetRow = new FlexContainer
                    {
                        Parent = MapsetGrid,
                        Direction = FlexDirection.Row,
                        AlignItems = FlexAlignItems.Stretch,
                        ColumnGap = Config.Mapset.GridColumnGap,
                        UsePreviousSpriteBatchOptions = true
                    };
                    MapsetRows.Add(mapsetRow);
                    MapsetGrid.SetItemOptions(mapsetRow, FixedBasis(Config.Mapset.Height));
                }

                var mapsetContainer = new DownloadingMapsetContainer(mapsets[i], Config.Mapset,
                    () => MapsetScrollContainer.ScreenRectangle)
                {
                    Parent = MapsetRows[MapsetRows.Count - 1]
                };
                MapsetContainers.Add(mapsetContainer);
                MapsetRows[MapsetRows.Count - 1].SetItemOptions(mapsetContainer,
                    new FlexItemOptions
                    {
                        Basis = 1,
                        Grow = 1,
                        Shrink = 1
                    });
            }

            MapsetGrid.RefreshLayout();
        }

        private void UpdateEditorLayout()
        {
            Container.Size = new ScalableVector2(WindowManager.Width, WindowManager.Height);
            EditorRoot.Size = Container.Size;

            if (!EditorLayoutActive)
            {
                PreviewRoot.Position = new ScalableVector2(0, 0);
                PreviewRoot.Scale = Vector2.One;
                return;
            }

            const float margin = 16;
            var availableWidth = Math.Max(1,
                WindowManager.Width - EditorLeftWidth - EditorRightWidth - margin * 2);
            var availableHeight = Math.Max(1,
                WindowManager.Height - EditorBottomHeight - margin * 2);
            var scale = Math.Min(availableWidth / WindowManager.Width,
                availableHeight / WindowManager.Height);
            PreviewRoot.Scale = new Vector2(scale);
            PreviewRoot.Position = new ScalableVector2(
                EditorLeftWidth + margin + (availableWidth - WindowManager.Width * scale) / 2f,
                margin + (availableHeight - WindowManager.Height * scale) / 2f);
        }
    }
}
