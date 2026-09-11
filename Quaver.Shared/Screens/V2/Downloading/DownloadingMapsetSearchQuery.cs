using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Quaver.API.Enums;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Online.API.MapsetSearch;
using Quaver.Shared.Screens.Downloading.UI.Search;

namespace Quaver.Shared.Screens.V2.Downloading
{
    /// <summary>
    ///     Immutable snapshot of the V2 Download mapset filters. Keeping the network request
    ///     separate from the controls prevents a late response from reading newer UI values.
    /// </summary>
    internal sealed class DownloadingMapsetSearchQuery
    {
        private const int ApiPageSize = 50;

        private const int ResultLimit = 50;

        public int Generation { get; }

        private string Search { get; }

        private GameMode Mode { get; }

        private DownloadFilterRankedStatus RankedStatus { get; }

        private float MinimumDifficulty { get; }

        private float MaximumDifficulty { get; }

        private float MinimumLongNotePercentage { get; }

        private float MaximumLongNotePercentage { get; }

        private float MinimumNotesPerSecond { get; }

        private float MaximumNotesPerSecond { get; }

        private float MinimumBpm { get; }

        private float MaximumBpm { get; }

        private int MinimumLength { get; }

        private int MaximumLength { get; }

        private int MinimumCombo { get; }

        private int MaximumCombo { get; }

        private bool ShowOwnedMapsets { get; }

        private bool ShowExplicitMapsets { get; }

        private bool ReverseSort { get; }

        private DownloadSortBy SortBy { get; }

        public DownloadingMapsetSearchQuery(DownloadingSearchState state, int generation,
            bool showExplicitMapsets)
        {
            Generation = generation;
            Search = state.MapsetQuery.Value ?? string.Empty;
            Mode = (GameMode) state.Keymode.Value;
            RankedStatus = (DownloadFilterRankedStatus) state.RankedStatus.Value;
            MinimumDifficulty = state.MinimumDifficulty.Value;
            MaximumDifficulty = state.MaximumDifficulty.Value;
            MinimumLongNotePercentage = state.MinimumLongNotePercentage.Value;
            MaximumLongNotePercentage = state.MaximumLongNotePercentage.Value;
            MinimumNotesPerSecond = state.MinimumNotesPerSecond.Value;
            MaximumNotesPerSecond = state.MaximumNotesPerSecond.Value;
            MinimumBpm = state.MinimumBpm.Value;
            MaximumBpm = state.MaximumBpm.Value;
            (MinimumLength, MaximumLength) = GetLengthRange(state.LengthFilter.Value);
            (MinimumCombo, MaximumCombo) = GetComboRange(state.ComboFilter.Value);
            ShowOwnedMapsets = state.ShowOwnedMapsets.Value;
            ShowExplicitMapsets = showExplicitMapsets;
            ReverseSort = state.ReverseSort.Value;
            SortBy = (DownloadSortBy) state.SortBy.Value;
        }

        public IReadOnlyList<DownloadableMapset> Execute(CancellationToken token)
        {
            var mapsets = new List<DownloadableMapset>();
            var page = 0;

            while (mapsets.Count < ResultLimit)
            {
                token.ThrowIfCancellationRequested();
                var response = CreateRequest(page).ExecuteRequest();
                var pageMapsets = response?.Mapsets ?? new List<DownloadableMapset>();

                foreach (var mapset in pageMapsets)
                {
                    token.ThrowIfCancellationRequested();
                    mapset.IsOwned = MapDatabaseCache.FindSet(mapset.Id) != null;

                    if (!ShowOwnedMapsets && mapset.IsOwned)
                        continue;

                    if (!FilterByNotesPerSecond(mapset))
                        continue;

                    mapsets.Add(mapset);
                    if (mapsets.Count == ResultLimit)
                        break;
                }

                var fetchedAllResults = pageMapsets.Count < ApiPageSize ||
                                        response?.Total > 0 && (page + 1) * ApiPageSize >= response.Total;
                if (fetchedAllResults)
                    break;

                page++;
            }

            return mapsets;
        }

        private APIRequestMapsetSearch CreateRequest(int page) => new APIRequestMapsetSearch(
            Search, Mode, RankedStatus, MinimumDifficulty, MaximumDifficulty,
            MinimumBpm, MaximumBpm, MinimumLength, MaximumLength,
            (int) MinimumLongNotePercentage, (int) MaximumLongNotePercentage,
            0, int.MaxValue, "01-01-1970", "12-31-9999", "01-01-1970", "12-31-9999",
            MinimumCombo, MaximumCombo, ReverseSort, SortBy, page, ShowExplicitMapsets);

        private bool FilterByNotesPerSecond(DownloadableMapset mapset)
        {
            if (mapset.Maps == null)
                return false;

            mapset.Maps = mapset.Maps.Where(map =>
            {
                var notesPerSecond = map.Length <= 0
                    ? 0
                    : (map.CountHitObjectNormal + map.CountHitObjectLong) * 1000d / map.Length;
                return notesPerSecond >= MinimumNotesPerSecond &&
                       notesPerSecond <= MaximumNotesPerSecond;
            }).ToList();

            return mapset.Maps.Count != 0;
        }

        private static (int Minimum, int Maximum) GetLengthRange(DownloadSearchLengthFilter filter) =>
            filter switch
            {
                DownloadSearchLengthFilter.LessThan30Seconds => (0, 29999),
                DownloadSearchLengthFilter.From30To90Seconds => (30000, 90000),
                DownloadSearchLengthFilter.From90To150Seconds => (90000, 150000),
                DownloadSearchLengthFilter.From150To210Seconds => (150000, 210000),
                DownloadSearchLengthFilter.From210To300Seconds => (210000, 300000),
                DownloadSearchLengthFilter.From300To600Seconds => (300000, 600000),
                DownloadSearchLengthFilter.GreaterThan600Seconds => (600001, int.MaxValue),
                _ => (0, int.MaxValue)
            };

        private static (int Minimum, int Maximum) GetComboRange(DownloadSearchComboFilter filter) =>
            filter switch
            {
                DownloadSearchComboFilter.LessThan150 => (0, 149),
                DownloadSearchComboFilter.From151To250 => (151, 250),
                DownloadSearchComboFilter.From251To500 => (251, 500),
                DownloadSearchComboFilter.From501To1000 => (501, 1000),
                DownloadSearchComboFilter.From1001To1500 => (1001, 1500),
                DownloadSearchComboFilter.From1501To2500 => (1501, 2500),
                DownloadSearchComboFilter.GreaterThan2501 => (2502, int.MaxValue),
                _ => (0, int.MaxValue)
            };
    }
}
