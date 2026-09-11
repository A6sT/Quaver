using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using Quaver.API.Enums;
using Quaver.API.Helpers;
using Quaver.Shared.Assets;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Online.API.MapsetSearch;
using Quaver.Shared.Screens.Download;
using Quaver.Shared.Screens.Downloading;
using Quaver.Shared.Skinning.V2;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Buttons;
using Wobble.Graphics.Shaders;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;
using Wobble.Window;

namespace Quaver.Shared.Screens.V2.Downloading.UI
{
    /// <summary>
    ///     Flex-driven mapset card used by the first V2 Download results slice.
    /// </summary>
    internal sealed class DownloadingMapsetContainer : Sprite
    {
        private DownloadableMapset Mapset { get; }

        private SkinV2DownloadingMapsetConfig Config { get; }

        private Func<RectangleF> VisibilityViewportProvider { get; }

        private FlexContainer Layout { get; }

        private FlexContainer BannerLayout { get; set; }

        private FlexContainer BannerContentLayout { get; set; }

        private FlexContainer MetadataRow { get; set; }

        private FlexContainer InfoLayout { get; set; }

        private FlexContainer StatisticsRow { get; }

        private RoundedBanner Banner { get; }

        private SpriteTextPlus Title { get; }

        private SpriteTextPlus Artist { get; }

        private SpriteTextPlus Creator { get; }

        private bool IsRefreshingSurface { get; set; }

        private float LastSurfaceWidth { get; set; } = -1;

        private float LastSurfaceHeight { get; set; } = -1;

        public DownloadingMapsetContainer(DownloadableMapset mapset,
            SkinV2DownloadingMapsetConfig config, Func<RectangleF> visibilityViewportProvider = null)
        {
            Mapset = mapset ?? throw new ArgumentNullException(nameof(mapset));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            VisibilityViewportProvider = visibilityViewportProvider;

            Size = new ScalableVector2(1, Config.Height);
            Tint = SkinV2Color.Parse(Config.CardColor);
            UsePreviousSpriteBatchOptions = true;

            Layout = new FlexContainer
            {
                Parent = this,
                Size = Size,
                Direction = FlexDirection.Column,
                AlignItems = FlexAlignItems.Stretch,
                UpdateWhenInvisible = false,
                UsePreviousSpriteBatchOptions = true
            };
            SetChildrenVisibility = true;

            var topSpacer = CreateSpacer(Layout, 1, Config.Padding);
            Layout.SetItemOptions(topSpacer, FixedBasis(Config.Padding));

            var contentRow = new FlexContainer
            {
                Parent = Layout,
                Direction = FlexDirection.Row,
                AlignItems = FlexAlignItems.Stretch,
                UsePreviousSpriteBatchOptions = true
            };
            Layout.SetItemOptions(contentRow, new FlexItemOptions
            {
                Basis = 1,
                Grow = 1,
                Shrink = 1
            });

            var leftSpacer = CreateSpacer(contentRow, Config.Padding, 1);
            contentRow.SetItemOptions(leftSpacer, FixedBasis(Config.Padding));

            var mainColumn = new FlexContainer
            {
                Parent = contentRow,
                Direction = FlexDirection.Column,
                AlignItems = FlexAlignItems.Stretch,
                RowGap = Config.ContentGap,
                UsePreviousSpriteBatchOptions = true
            };
            contentRow.SetItemOptions(mainColumn, new FlexItemOptions
            {
                Basis = 1,
                Grow = 1,
                Shrink = 1
            });

            var rightSpacer = CreateSpacer(contentRow, Config.Padding, 1);
            contentRow.SetItemOptions(rightSpacer, FixedBasis(Config.Padding));

            Banner = new RoundedBanner(Config.BannerHeight, Config.CornerRadius,
                SkinV2Color.Parse(Config.BannerOverlayColor), UserInterface.DefaultBanner)
            {
                Parent = mainColumn,
                UsePreviousSpriteBatchOptions = true
            };
            mainColumn.SetItemOptions(Banner, FixedBasis(Config.BannerHeight));

            StatisticsRow = new FlexContainer
            {
                Parent = mainColumn,
                Direction = FlexDirection.Row,
                AlignItems = FlexAlignItems.Center,
                ColumnGap = Config.PillGap,
                UsePreviousSpriteBatchOptions = true
            };
            mainColumn.SetItemOptions(StatisticsRow, FixedBasis(Config.PillHeight));

            var bottomSpacer = CreateSpacer(Layout, 1, Config.Padding);
            Layout.SetItemOptions(bottomSpacer, FixedBasis(Config.Padding));

            BannerLayout = Banner.Layout;
            BuildBannerLayout();

            Title = CreateText(InfoLayout, Mapset.Title, Config.TitleFont, Config.TitleFontSize,
                SkinV2Color.Parse(Config.PrimaryTextColor));
            Artist = CreateText(InfoLayout, Mapset.Artist, Config.MetadataFont,
                Config.MetadataFontSize, SkinV2Color.Parse(Config.PrimaryTextColor));
            Creator = CreateText(InfoLayout, Mapset.CreatorUsername, Config.MetadataFont,
                Config.MetadataFontSize, SkinV2Color.Parse(Config.SecondaryTextColor));

            BuildMetadata(Mapset);
            BuildStatistics(Mapset);
            RefreshSurface();
        }

        public override void Update(GameTime gameTime)
        {
            var viewport = VisibilityViewportProvider?.Invoke() ??
                           new RectangleF(0, 0, WindowManager.Width, WindowManager.Height);
            var shouldRender = viewport.Width > 0 && viewport.Height > 0 &&
                               RectangleF.Intersects(ScreenMinimumBoundingRectangle, viewport);
            if (Visible != shouldRender)
                Visible = shouldRender;

            base.Update(gameTime);
        }

        protected override void OnRectangleRecalculated()
        {
            if (IsDisposed || IsRefreshingSurface)
                return;

            base.OnRectangleRecalculated();

            if (IsDisposed || Width <= 0 || Height <= 0 || Layout == null)
                return;

            if (NearlyEqual(Width, LastSurfaceWidth) && NearlyEqual(Height, LastSurfaceHeight))
                return;

            RefreshSurface();
        }

        private void RefreshSurface()
        {
            if (IsDisposed || IsRefreshingSurface || Width <= 0 || Height <= 0 || Layout == null)
                return;

            IsRefreshingSurface = true;

            try
            {
                LastSurfaceWidth = Width;
                LastSurfaceHeight = Height;

                var texture = RoundedRectTextureCache.Get(Width, Height, Config.CornerRadius);
                if (Image != texture)
                    Image = texture;

                Layout.Size = Size;
                Layout.RefreshLayout();
            }
            finally
            {
                IsRefreshingSurface = false;
            }
        }

        private void BuildBannerLayout()
        {
            var topSpacer = CreateSpacer(BannerLayout, 1, Config.BannerPadding);
            BannerLayout.SetItemOptions(topSpacer, FixedBasis(Config.BannerPadding));

            var contentRow = new FlexContainer
            {
                Parent = BannerLayout,
                Direction = FlexDirection.Row,
                AlignItems = FlexAlignItems.Stretch,
                UsePreviousSpriteBatchOptions = true
            };
            BannerLayout.SetItemOptions(contentRow, new FlexItemOptions
            {
                Basis = 1,
                Grow = 1,
                Shrink = 1
            });

            var leftSpacer = CreateSpacer(contentRow, Config.BannerPadding, 1);
            contentRow.SetItemOptions(leftSpacer, FixedBasis(Config.BannerPadding));

            BannerContentLayout = new FlexContainer
            {
                Parent = contentRow,
                Direction = FlexDirection.Column,
                AlignItems = FlexAlignItems.Stretch,
                RowGap = Config.PillGap,
                UsePreviousSpriteBatchOptions = true
            };
            contentRow.SetItemOptions(BannerContentLayout, new FlexItemOptions
            {
                Basis = 1,
                Grow = 1,
                Shrink = 1
            });

            var rightSpacer = CreateSpacer(contentRow, Config.BannerPadding, 1);
            contentRow.SetItemOptions(rightSpacer, FixedBasis(Config.BannerPadding));

            MetadataRow = new FlexContainer
            {
                Parent = BannerContentLayout,
                Direction = FlexDirection.Row,
                Wrap = FlexWrap.NoWrap,
                AlignItems = FlexAlignItems.Center,
                ColumnGap = Config.PillGap,
                UsePreviousSpriteBatchOptions = true
            };
            BannerContentLayout.SetItemOptions(MetadataRow, FixedBasis(Config.PillHeight));

            var infoSpacer = CreateSpacer(BannerContentLayout, 1, 1);
            BannerContentLayout.SetItemOptions(infoSpacer, new FlexItemOptions
            {
                Basis = 1,
                Grow = 1,
                Shrink = 1
            });

            InfoLayout = new FlexContainer
            {
                Parent = BannerContentLayout,
                Direction = FlexDirection.Column,
                JustifyContent = FlexJustifyContent.FlexEnd,
                AlignItems = FlexAlignItems.Stretch,
                RowGap = Config.PillGap,
                UsePreviousSpriteBatchOptions = true
            };
            BannerContentLayout.SetItemOptions(InfoLayout, new FlexItemOptions
            {
                Basis = Config.TitleFontSize + Config.MetadataFontSize * 2 + Config.PillGap * 2,
                Shrink = 0
            });

            var bottomSpacer = CreateSpacer(BannerContentLayout, 1, Config.BannerPadding);
            BannerContentLayout.SetItemOptions(bottomSpacer, FixedBasis(Config.BannerPadding));
        }

        private void BuildMetadata(DownloadableMapset mapset)
        {
            AddPill(MetadataRow, CapsuleIcon.Music, mapset.Maps.Count.ToString(CultureInfo.InvariantCulture));
            AddPill(MetadataRow, CapsuleIcon.BarChart, FormatDifficulty(mapset));
            AddPill(MetadataRow, CapsuleIcon.Clock, FormatLength(mapset));

            var modeSpacer = CreateSpacer(MetadataRow, 1, Config.PillHeight);
            MetadataRow.SetItemOptions(modeSpacer, new FlexItemOptions
            {
                Basis = 1,
                Grow = 1,
                Shrink = 1
            });

            AddPill(MetadataRow, null, FormatModes(mapset));
            AddPill(MetadataRow, null, DownloadLocalization.Get("Other Game"));
        }

        private void BuildStatistics(DownloadableMapset mapset)
        {
            AddPill(StatisticsRow, CapsuleIcon.CandlestickChart, FormatLongNotePercentage(mapset),
                Config.StatisticsIconSize);
            AddPill(StatisticsRow, CapsuleIcon.Equals, FormatNps(mapset));
            AddPill(StatisticsRow, CapsuleIcon.Gauge, FormatBpm(mapset));

            var spacer = CreateSpacer(StatisticsRow, 1, Config.PillHeight);
            StatisticsRow.SetItemOptions(spacer, new FlexItemOptions
            {
                Basis = 1,
                Grow = 1,
                Shrink = 1
            });

            var downloadButton = new RoundedButton((sender, args) => DownloadMapset())
            {
                Parent = StatisticsRow,
                Size = new ScalableVector2(Config.DownloadButtonSize, Config.DownloadButtonSize),
                CornerRadius = Config.CornerRadius,
                Tint = SkinV2Color.Parse(Config.ButtonColor),
                PerformHoverFade = true,
                UsePreviousSpriteBatchOptions = true
            };
            downloadButton.SetIcon(CapsuleIcons.Get(CapsuleIcon.Download),
                new Vector2(Config.DownloadIconSize, Config.DownloadIconSize));
            StatisticsRow.SetItemOptions(downloadButton, FixedBasis(Config.DownloadButtonSize));
        }

        private void DownloadMapset()
        {
            if (MapsetDownloadManager.IsMapsetInQueue(Mapset.Id))
            {
                NotificationManager.Show(NotificationLevel.Warning,
                    DownloadLocalization.Get("This mapset is already downloading!"));
                return;
            }

            MapsetDownloadManager.Download(Mapset.Id, Mapset.Artist, Mapset.Title);
        }

        private void AddPill(FlexContainer parent, CapsuleIcon? icon, string text,
            float? iconSize = null)
        {
            var pill = new RoundedButton
            {
                Parent = parent,
                Height = Config.PillHeight,
                WidthMode = ButtonSizeMode.Auto,
                HeightMode = ButtonSizeMode.Fixed,
                AutoSizePadding = new Vector2(Config.PillHorizontalPadding * 2, 0),
                CornerRadius = Config.PillCornerRadius,
                Tint = SkinV2Color.Parse(Config.PillColor),
                IsClickable = false,
                PerformHoverFade = false,
                UsePreviousSpriteBatchOptions = true
            };

            if (icon.HasValue)
            {
                var iconHeight = iconSize ?? Config.IconSize;
                var displaySize = CapsuleIcons.GetDisplaySize(icon.Value);
                var iconWidth = iconHeight * displaySize.X / displaySize.Y;
                pill.SetIcon(CapsuleIcons.Get(icon.Value), new Vector2(iconWidth, iconHeight));
            }

            pill.SetLabel(FontManager.GetWobbleFont(Config.PillFont), text,
                Config.PillFontSize, SkinV2Color.Parse(Config.PrimaryTextColor));
            parent.SetItemOptions(pill, new FlexItemOptions
            {
                Basis = Math.Max(1, pill.Width),
                Grow = 0,
                Shrink = 1
            });
        }

        private static SpriteTextPlus CreateText(FlexContainer parent, string text, string font,
            int fontSize, Color color) => new SpriteTextPlus(FontManager.GetWobbleFont(font), text,
            fontSize)
        {
            Parent = parent,
            Tint = color,
            UsePreviousSpriteBatchOptions = true
        };

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

        private static bool NearlyEqual(float first, float second) => Math.Abs(first - second) < 0.001f;

        private static string FormatDifficulty(DownloadableMapset mapset)
        {
            if (mapset.Maps == null || mapset.Maps.Count == 0)
                return "0.00 - 0.00";

            return $"{mapset.Maps.Min(x => x.DifficultyRating):0.00} - " +
                   $"{mapset.Maps.Max(x => x.DifficultyRating):0.00}";
        }

        private static string FormatLength(DownloadableMapset mapset)
        {
            var milliseconds = mapset.Maps == null || mapset.Maps.Count == 0
                ? 0
                : mapset.Maps.Max(x => x.Length);
            var duration = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
            return duration.TotalHours >= 1
                ? duration.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
                : duration.ToString(@"m\:ss", CultureInfo.InvariantCulture);
        }

        private static string FormatLongNotePercentage(DownloadableMapset mapset)
        {
            if (mapset.Maps == null || mapset.Maps.Count == 0)
                return "0 - 0%";

            return $"{mapset.Maps.Min(x => x.LongNotePercentage):0.#} - " +
                   $"{mapset.Maps.Max(x => x.LongNotePercentage):0.#}%";
        }

        private static string FormatNps(DownloadableMapset mapset)
        {
            var nps = mapset.Maps == null || mapset.Maps.Count == 0
                ? 0
                : mapset.Maps.Max(GetNps);
            return $"{nps:0.#} {DownloadLocalization.Get("NPS")}";
        }

        private static double GetNps(DownloadableMap map)
        {
            if (map == null || map.Length <= 0)
                return 0;

            return (map.CountHitObjectNormal + map.CountHitObjectLong) * 1000d / map.Length;
        }

        private static string FormatBpm(DownloadableMapset mapset)
        {
            var bpm = mapset.Maps == null || mapset.Maps.Count == 0
                ? 0
                : mapset.Maps.Max(x => x.Bpm);
            return $"{bpm:0.#} {DownloadLocalization.Get("BPM")}";
        }

        private static string FormatModes(DownloadableMapset mapset)
        {
            if (mapset.Maps == null || mapset.Maps.Count == 0)
                return string.Empty;

            var modes = mapset.Maps.Select(x => x.GameMode).Distinct()
                .OrderBy(x => ModeHelper.ToKeyCount(x))
                .Select(x => ModeHelper.ToKeyCount(x))
                .Select(x => $"{x}K");
            return string.Join("/", modes);
        }

        private sealed class RoundedBanner : Container
        {
            private float CornerRadius { get; }

            private Color OverlayColor { get; }

            private Texture2D SourceImage { get; }

            private Sprite Content { get; }

            public FlexContainer Layout { get; }

            private RenderTarget2D RenderTarget { get; set; } = null!;

            private bool NeedsTextureRefresh { get; set; }

            private bool IsRefreshingTexture { get; set; }

            private float LastSurfaceWidth { get; set; } = -1;

            private float LastSurfaceHeight { get; set; } = -1;

            private static BlendState MaskBlendState { get; } = new BlendState
            {
                ColorSourceBlend = Blend.Zero,
                ColorDestinationBlend = Blend.One,
                AlphaSourceBlend = Blend.One,
                AlphaDestinationBlend = Blend.Zero
            };

            private static BlendState ImageMaskBlendState { get; } = new BlendState
            {
                ColorSourceBlend = Blend.One,
                ColorDestinationBlend = Blend.Zero,
                AlphaSourceBlend = Blend.DestinationAlpha,
                AlphaDestinationBlend = Blend.Zero
            };

            private bool IsRefreshingSurface { get; set; }

            public RoundedBanner(float height, float cornerRadius, Color overlayColor, Texture2D image)
            {
                CornerRadius = cornerRadius;
                OverlayColor = overlayColor;
                SourceImage = image ?? throw new ArgumentNullException(nameof(image));
                Size = new ScalableVector2(1, height);

                Content = new Sprite
                {
                    Parent = this,
                    Size = Size,
                    Image = SourceImage,
                    UsePreviousSpriteBatchOptions = true
                };

                Layout = new FlexContainer
                {
                    Parent = this,
                    Size = Size,
                    Direction = FlexDirection.Column,
                    AlignItems = FlexAlignItems.Stretch,
                    UsePreviousSpriteBatchOptions = true
                };

                NeedsTextureRefresh = true;
                RefreshSurface();
            }

            public override void Draw(GameTime gameTime)
            {
                if (NeedsTextureRefresh && !IsDisposed)
                    RefreshTexture();

                base.Draw(gameTime);
            }

            protected override void OnRectangleRecalculated()
            {
                if (IsDisposed || IsRefreshingSurface || IsRefreshingTexture)
                    return;

                base.OnRectangleRecalculated();

                if (IsDisposed || Width <= 0 || Height <= 0 || Content == null || Layout == null)
                    return;

                if (NearlyEqual(Width, LastSurfaceWidth) && NearlyEqual(Height, LastSurfaceHeight))
                    return;

                RefreshSurface();
            }

            private void RefreshSurface()
            {
                if (IsDisposed || IsRefreshingSurface || IsRefreshingTexture || Width <= 0 || Height <= 0 ||
                    Content == null || Layout == null)
                    return;

                IsRefreshingSurface = true;

                try
                {
                    LastSurfaceWidth = Width;
                    LastSurfaceHeight = Height;
                    NeedsTextureRefresh = true;
                    Content.Size = Size;
                    Layout.Size = Size;
                    Layout.RefreshLayout();
                }
                finally
                {
                    IsRefreshingSurface = false;
                }
            }

            private void RefreshTexture()
            {
                if (IsDisposed || IsRefreshingTexture || Width <= 0 || Height <= 0)
                    return;

                IsRefreshingTexture = true;

                try
                {
                    var scale = Math.Max(WindowManager.ScreenScale.X, WindowManager.ScreenScale.Y);
                    var targetWidth = Math.Max(1, (int) Math.Ceiling(Width * scale));
                    var targetHeight = Math.Max(1, (int) Math.Ceiling(Height * scale));

                    if (RenderTarget == null || RenderTarget.IsDisposed || RenderTarget.IsContentLost ||
                        RenderTarget.GraphicsDevice != GameBase.Game.GraphicsDevice ||
                        RenderTarget.Width != targetWidth || RenderTarget.Height != targetHeight)
                    {
                        RenderTarget?.Dispose();
                        RenderTarget = new RenderTarget2D(GameBase.Game.GraphicsDevice, targetWidth, targetHeight,
                            false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                    }

                    var mask = RoundedRectTextureCache.Get(targetWidth, targetHeight, CornerRadius * scale, false);
                    var destination = new Rectangle(0, 0, targetWidth, targetHeight);
                    var graphicsDevice = GameBase.Game.GraphicsDevice;
                    var previousRenderTargets = graphicsDevice.GetRenderTargets();

                    try
                    {
                        graphicsDevice.SetRenderTarget(RenderTarget);
                        graphicsDevice.Clear(Color.Transparent);

                        DrawRenderTargetPass(MaskBlendState, mask, destination, Color.White);
                        DrawRenderTargetPass(ImageMaskBlendState, SourceImage, destination, Color.White);
                        DrawRenderTargetPass(BlendState.NonPremultiplied, mask, destination, OverlayColor);

                        _ = GameBase.Game.TryEndBatch();
                    }
                    finally
                    {
                        graphicsDevice.SetRenderTargets(previousRenderTargets);
                    }

                    Content.Image = RenderTarget;
                    NeedsTextureRefresh = false;
                }
                finally
                {
                    IsRefreshingTexture = false;
                }
            }

            private static void DrawRenderTargetPass(BlendState blendState, Texture2D texture,
                Rectangle destination, Color tint)
            {
                _ = GameBase.Game.TryEndBatch();
                try
                {
                    GameBase.Game.SpriteBatch.Begin(SpriteSortMode.Deferred, blendState, SamplerState.LinearClamp,
                        null, RasterizerState.CullNone, null, null);
                    GameBase.Game.SpriteBatch.Draw(texture, destination, tint);
                }
                finally
                {
                    _ = GameBase.Game.TryEndBatch();
                }
            }

            private static bool NearlyEqual(float first, float second) => Math.Abs(first - second) < 0.001f;

            public override void Destroy()
            {
                if (RenderTarget != null && !RenderTarget.IsDisposed)
                    RenderTarget.Dispose();

                base.Destroy();
            }
        }
    }
}
