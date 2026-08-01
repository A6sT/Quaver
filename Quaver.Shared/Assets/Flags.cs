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
using Wobble;
using Wobble.Graphics.Sprites;

namespace Quaver.Shared.Assets
{
    /// <summary>
    ///     Loads and caches individual country flags from the global flag spritesheet.
    /// </summary>
    public static class Flags
    {
        private const int FlagSize = 72;
        private const int ColumnCount = 13;
        private const int RowCount = 20;
        private const int TileStride = 77;
        private const string FallbackCode = "XX";
        private const int ExpectedFlagCount = 259;

        private static Texture2D? Sheet { get; set; }

        private static Dictionary<string, Texture2D> Textures { get; } =
            new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

        private static IReadOnlyDictionary<string, Point> Coordinates { get; } = CreateCoordinates();

        /// <summary>
        ///     Gets the country codes in the same row-major order as the spritesheet.
        /// </summary>
        internal static IEnumerable<string> CountryCodes => Coordinates.Keys;

        /// <summary>
        ///     Gets a shared 72x72 texture for a country code.
        ///     Country codes are normalized case-insensitively and unknown codes use the unknown flag.
        ///     Callers must not dispose the returned texture.
        /// </summary>
        /// <param name="countryCode">The ISO-style country code to retrieve.</param>
        /// <returns>The globally owned country flag texture.</returns>
        /// <exception cref="InvalidOperationException">The flag spritesheet is unavailable.</exception>
        public static Texture2D Get(string? countryCode)
        {
            var sheet = Sheet;

            if (sheet == null || sheet.IsDisposed)
                throw new InvalidOperationException("The flag spritesheet has not been loaded.");

            var normalizedCode = NormalizeCode(countryCode);

            if (!Coordinates.ContainsKey(normalizedCode))
                normalizedCode = FallbackCode;

            if (Textures.TryGetValue(normalizedCode, out var cachedTexture) && !cachedTexture.IsDisposed)
                return cachedTexture;

            var sourceRectangle = CreateSourceRectangle(Coordinates[normalizedCode]);
            ValidateSourceRectangle(normalizedCode, sheet, sourceRectangle);

            var texture = Crop(sheet, sourceRectangle);
            Textures[normalizedCode] = texture;
            return texture;
        }

        /// <summary>
        ///     Gets a source rectangle in the global flag atlas for a country code.
        /// </summary>
        public static TextureRegion GetRegion(string? countryCode)
        {
            var sheet = Sheet;

            if (sheet == null || sheet.IsDisposed)
                throw new InvalidOperationException("The flag spritesheet has not been loaded.");

            var normalizedCode = NormalizeCode(countryCode);

            if (!Coordinates.ContainsKey(normalizedCode))
                normalizedCode = FallbackCode;

            var sourceRectangle = CreateSourceRectangle(Coordinates[normalizedCode]);
            ValidateSourceRectangle(normalizedCode, sheet, sourceRectangle);
            return new TextureRegion(sheet, sourceRectangle);
        }

        /// <summary>
        ///     Loads and validates the global flag spritesheet on the UI thread.
        /// </summary>
        internal static void Load()
        {
            if (Sheet != null && !Sheet.IsDisposed)
                return;

            DisposeTextures(Textures.Values);
            Textures.Clear();
            Sheet = null;

            var sheet = UserInterface.FlagsGrid;

            try
            {
                foreach (var mapping in Coordinates)
                {
                    var sourceRectangle = CreateSourceRectangle(mapping.Value);
                    ValidateSourceRectangle(mapping.Key, sheet, sourceRectangle);
                }

                Sheet = sheet;
            }
            catch
            {
                DisposeTextures(Textures.Values);
                Textures.Clear();
                Sheet = null;
                throw;
            }
        }

        /// <summary>
        ///     Disposes cropped flag textures created on demand. The source sheet is owned by
        ///     <c>TextureManager</c>.
        /// </summary>
        internal static void Dispose()
        {
            DisposeTextures(Textures.Values);
            Textures.Clear();
            Sheet = null;
        }

        private static Dictionary<string, Point> CreateCoordinates()
        {
            var rows = new[]
            {
                "AC AD AE AF AG AI AL AM AO AQ AR AS AT",
                "AU AW AX AZ BA BB BD BE BF BG BH BI BJ",
                "BL BM BN BO BQ BR BS BT BV BW BY BZ CA",
                "CC CD CF CG CH CI CK CL CM CN CO CP CR",
                "CU CV CW CX CY CZ DE DG DJ DK DM DO DZ",
                "EA EC EE EG EH ER ES ET EU FI FJ FK FM",
                "FO FR GA GB GD GE GF GG GH GI GL GM GN",
                "GP GQ GR GS GT GU GW GY HK HM HN HR HT",
                "HU IC ID IE IL IM IN IO IQ IR IS IT JE",
                "JM JO JP KE KG KH KI KM KN KP KR KW KY",
                "KZ LA LB LC LI LK LR LS LT LU LV LY MA",
                "MC MD ME MF MG MH MK ML MM MN MO MP MQ",
                "MR MS MT MU MV MW MX MY MZ NA NC NE NF",
                "NG NI NL NO NP NR NU NZ OM PA PE PF PG",
                "PH PK PL PM PN PR PS PT PW PY QA RE RO",
                "RS RU RW SA SB SC SD SE SG SH SI SJ SK",
                "SL SM SN SO SR SS ST SV SX SY SZ TA TC",
                "TD TF TG TH TJ TK TL TM TN TO TR TT TV",
                "TW TZ UA UG UM UN US UY UZ VA VC VE VG",
                "VI VN VU WF WS XK YE YT ZA ZM ZW XX"
            };

            if (rows.Length != RowCount)
                throw new InvalidOperationException($"The flag mapping must contain {RowCount} rows.");

            var coordinates = new Dictionary<string, Point>(StringComparer.OrdinalIgnoreCase);

            for (var row = 0; row < rows.Length; row++)
            {
                var codes = rows[row].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (codes.Length > ColumnCount)
                    throw new InvalidOperationException(
                        $"The flag mapping row {row + 1} contains more than {ColumnCount} codes.");

                for (var column = 0; column < codes.Length; column++)
                {
                    if (!coordinates.TryAdd(codes[column], new Point(column, row)))
                    {
                        throw new InvalidOperationException(
                            $"The flag mapping contains the duplicate country code {codes[column]}.");
                    }
                }
            }

            if (coordinates.Count != ExpectedFlagCount || !coordinates.ContainsKey(FallbackCode))
            {
                throw new InvalidOperationException(
                    $"The flag mapping must contain {ExpectedFlagCount} unique codes including {FallbackCode}, " +
                    $"but contained {coordinates.Count}.");
            }

            return coordinates;
        }

        private static string NormalizeCode(string? countryCode)
        {
            var normalizedCode = countryCode?.Trim().ToUpperInvariant();
            return string.IsNullOrEmpty(normalizedCode) ? FallbackCode : normalizedCode;
        }

        private static Rectangle CreateSourceRectangle(Point coordinate) => new Rectangle(
            coordinate.X * TileStride,
            coordinate.Y * TileStride,
            FlagSize,
            FlagSize);

        private static void ValidateSourceRectangle(string countryCode, Texture2D sheet, Rectangle sourceRectangle)
        {
            if (sourceRectangle.Left < 0 || sourceRectangle.Top < 0 ||
                sourceRectangle.Right > sheet.Width || sourceRectangle.Bottom > sheet.Height)
            {
                throw new InvalidOperationException(
                    $"The mapping for {countryCode} ({sourceRectangle}) is outside the " +
                    $"{sheet.Width}x{sheet.Height} flag spritesheet.");
            }
        }

        private static Texture2D Crop(Texture2D sheet, Rectangle sourceRectangle)
        {
            var pixels = new Color[sourceRectangle.Width * sourceRectangle.Height];
            sheet.GetData(0, sourceRectangle, pixels, 0, pixels.Length);

            var texture = new Texture2D(GameBase.Game.GraphicsDevice, sourceRectangle.Width, sourceRectangle.Height);
            texture.SetData(pixels);
            return texture;
        }

        private static void DisposeTextures(IEnumerable<Texture2D> textures)
        {
            foreach (var texture in textures)
                texture.Dispose();
        }

    }
}
