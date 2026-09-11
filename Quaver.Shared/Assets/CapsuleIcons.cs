/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * Copyright (c) Swan & The Quaver Team <support@quavergame.com>.
*/

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Wobble.Graphics.Sprites;

namespace Quaver.Shared.Assets
{
    /// <summary>
    ///     Icons available in the global capsule icon spritesheet.
    /// </summary>
    public enum CapsuleIcon
    {
        BarChart,
        AreaChart,
        LineChart,
        CandlestickChart,
        Download,
        Music,
        Users,
        Refresh,
        Headset,
        Award,
        Clock,
        Equals,
        Gauge,
        Trophy
    }

    /// <summary>
    ///     Loads and caches individual textures from the global capsule icon spritesheet.
    /// </summary>
    public static class CapsuleIcons
    {
        public const int IconHeight = 36;

        private const int LogicalSheetWidth = 740;
        private const int LogicalSheetHeight = IconHeight;

        private static Texture2D? Sheet { get; set; }

        private static Dictionary<CapsuleIcon, TextureRegion> Textures { get; } =
            new Dictionary<CapsuleIcon, TextureRegion>();

        // These rectangles stop at the visible artwork. The source atlas has additional transparent pixels
        // between some icons, but those pixels are intentionally not exposed by the returned regions.
        private static IReadOnlyDictionary<CapsuleIcon, Rectangle> Mappings { get; } =
            new Dictionary<CapsuleIcon, Rectangle>
            {
                { CapsuleIcon.BarChart, new Rectangle(0, 0, 48, IconHeight) },
                { CapsuleIcon.AreaChart, new Rectangle(60, 0, 48, IconHeight) },
                { CapsuleIcon.LineChart, new Rectangle(120, 0, 48, IconHeight) },
                { CapsuleIcon.CandlestickChart, new Rectangle(180, 0, 36, IconHeight) },
                { CapsuleIcon.Download, new Rectangle(228, 0, 36, IconHeight) },
                { CapsuleIcon.Music, new Rectangle(276, 0, 36, IconHeight) },
                { CapsuleIcon.Users, new Rectangle(324, 0, 52, IconHeight) },
                { CapsuleIcon.Refresh, new Rectangle(388, 0, 36, IconHeight) },
                { CapsuleIcon.Headset, new Rectangle(436, 0, 36, IconHeight) },
                { CapsuleIcon.Award, new Rectangle(484, 0, 46, IconHeight) },
                { CapsuleIcon.Clock, new Rectangle(542, 0, 36, IconHeight) },
                { CapsuleIcon.Equals, new Rectangle(590, 0, 40, IconHeight) },
                { CapsuleIcon.Gauge, new Rectangle(642, 0, 46, IconHeight) },
                { CapsuleIcon.Trophy, new Rectangle(700, 0, 40, IconHeight) }
            };

        /// <summary>
        ///     Gets a shared capsule icon atlas region.
        /// </summary>
        /// <param name="icon">The icon to retrieve.</param>
        /// <returns>The globally owned capsule icon atlas region.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The icon is not mapped.</exception>
        /// <exception cref="InvalidOperationException">The capsule icon spritesheet is unavailable.</exception>
        public static TextureRegion Get(CapsuleIcon icon)
        {
            if (!Mappings.ContainsKey(icon))
                throw new ArgumentOutOfRangeException(nameof(icon), icon, "The capsule icon is not mapped.");

            if (Textures.TryGetValue(icon, out var cachedTexture))
                return cachedTexture;

            if (Sheet == null || Sheet.IsDisposed)
                throw new InvalidOperationException("The capsule icon spritesheet has not been loaded.");

            throw new InvalidOperationException($"The capsule icon {icon} was not loaded.");
        }

        /// <summary>
        ///     Gets the logical on-screen size of a capsule icon atlas cell.
        /// </summary>
        public static Point GetDisplaySize(CapsuleIcon icon)
        {
            if (!Mappings.TryGetValue(icon, out var mapping))
                throw new ArgumentOutOfRangeException(nameof(icon), icon, "The capsule icon is not mapped.");

            return new Point(mapping.Width, mapping.Height);
        }

        /// <summary>
        ///     Loads and validates every capsule icon on the UI thread.
        /// </summary>
        internal static void Load()
        {
            if (Sheet != null && !Sheet.IsDisposed && Textures.Count == Mappings.Count)
                return;

            Textures.Clear();
            Sheet = null;

            var sheet = UserInterface.CapsuleIcons;

            if (sheet.Width != LogicalSheetWidth || sheet.Height != LogicalSheetHeight)
            {
                throw new InvalidOperationException(
                    $"The capsule icon spritesheet must be {LogicalSheetWidth}x{LogicalSheetHeight}, " +
                    $"but was {sheet.Width}x{sheet.Height}.");
            }

            try
            {
                foreach (var mapping in Mappings)
                {
                    ValidateSourceRectangle(mapping.Key, sheet, mapping.Value);
                    Textures.Add(mapping.Key, new TextureRegion(sheet, mapping.Value));
                }

                Sheet = sheet;
            }
            catch
            {
                Textures.Clear();
                Sheet = null;
                throw;
            }
        }

        /// <summary>
        ///     Clears all capsule icon atlas regions. The source sheet is owned by <c>TextureManager</c>.
        /// </summary>
        internal static void Dispose()
        {
            Textures.Clear();
            Sheet = null;
        }

        private static void ValidateSourceRectangle(CapsuleIcon icon, Texture2D sheet,
            Rectangle sourceRectangle)
        {
            if (sourceRectangle.Left < 0 || sourceRectangle.Top < 0 ||
                sourceRectangle.Right > sheet.Width || sourceRectangle.Bottom > sheet.Height)
            {
                throw new InvalidOperationException(
                    $"The mapping for {icon} ({sourceRectangle}) is outside the " +
                    $"{sheet.Width}x{sheet.Height} capsule icon spritesheet.");
            }
        }
    }
}
