using System;
using Wobble.Bindables;

namespace Quaver.Shared.Screens.V2.Downloading
{
    internal enum DownloadSearchTab
    {
        Mapsets,
        Playlists
    }

    internal enum DownloadSearchRankedStatus
    {
        All,
        Unranked,
        Ranked,
        ClanRanked
    }

    internal enum DownloadSearchLengthFilter
    {
        Any,
        LessThan30Seconds,
        From30To90Seconds,
        From90To150Seconds,
        From150To210Seconds,
        From210To300Seconds,
        From300To600Seconds,
        GreaterThan600Seconds
    }

    internal enum DownloadSearchComboFilter
    {
        Any,
        LessThan150,
        From151To250,
        From251To500,
        From501To1000,
        From1001To1500,
        From1501To2500,
        GreaterThan2501
    }

    internal enum DownloadSearchSortBy
    {
        Newest,
        DateSubmitted,
        Length,
        Difficulty,
        MaxCombo,
        Bpm,
        LongNotePercentage,
        PlayCount
    }

    /// <summary>
    ///     Local-only state for the first V2 Download search-header slice.
    /// </summary>
    internal sealed class DownloadingSearchState : IDisposable
    {
        public event EventHandler MapsetFiltersChanged;

        public Bindable<DownloadSearchTab> ActiveTab { get; } =
            new Bindable<DownloadSearchTab>(DownloadSearchTab.Mapsets);

        public Bindable<bool> MapsetsExpanded { get; } = new Bindable<bool>(false);

        public Bindable<string> MapsetQuery { get; } = new Bindable<string>(string.Empty);

        public Bindable<string> PlaylistQuery { get; } = new Bindable<string>(string.Empty);

        public Bindable<bool> ShowOwnedMapsets { get; } = new Bindable<bool>(true);

        public Bindable<bool> ShowOwnedPlaylists { get; } = new Bindable<bool>(false);

        /// <summary>
        ///     Zero represents all keymodes; positive values use the underlying GameMode value.
        /// </summary>
        public Bindable<int> Keymode { get; } = new Bindable<int>(0);

        public Bindable<DownloadSearchRankedStatus> RankedStatus { get; } =
            new Bindable<DownloadSearchRankedStatus>(DownloadSearchRankedStatus.Ranked);

        public Bindable<DownloadSearchLengthFilter> LengthFilter { get; } =
            new Bindable<DownloadSearchLengthFilter>(DownloadSearchLengthFilter.Any);

        public Bindable<DownloadSearchComboFilter> ComboFilter { get; } =
            new Bindable<DownloadSearchComboFilter>(DownloadSearchComboFilter.Any);

        public Bindable<DownloadSearchSortBy> SortBy { get; } =
            new Bindable<DownloadSearchSortBy>(DownloadSearchSortBy.Newest);

        public Bindable<bool> ReverseSort { get; } = new Bindable<bool>(false);

        public BindableFloat MinimumDifficulty { get; } = new BindableFloat(0, 0, 99.99f);

        public BindableFloat MaximumDifficulty { get; } = new BindableFloat(99.99f, 0, 99.99f);

        public BindableFloat MinimumLongNotePercentage { get; } = new BindableFloat(0, 0, 100);

        public BindableFloat MaximumLongNotePercentage { get; } = new BindableFloat(100, 0, 100);

        public BindableFloat MinimumNotesPerSecond { get; } = new BindableFloat(0, 0, 9999);

        public BindableFloat MaximumNotesPerSecond { get; } = new BindableFloat(9999, 0, 9999);

        public BindableFloat MinimumBpm { get; } = new BindableFloat(0, 0, 9999);

        public BindableFloat MaximumBpm { get; } = new BindableFloat(9999, 0, 9999);

        public DownloadingSearchState()
        {
            MapsetQuery.ValueChanged += OnStringFilterChanged;
            ShowOwnedMapsets.ValueChanged += OnBoolFilterChanged;
            Keymode.ValueChanged += OnIntFilterChanged;
            RankedStatus.ValueChanged += OnRankedStatusChanged;
            LengthFilter.ValueChanged += OnLengthFilterChanged;
            ComboFilter.ValueChanged += OnComboFilterChanged;
            SortBy.ValueChanged += OnSortByChanged;
            ReverseSort.ValueChanged += OnBoolFilterChanged;
            MinimumDifficulty.ValueChanged += OnFloatFilterChanged;
            MaximumDifficulty.ValueChanged += OnFloatFilterChanged;
            MinimumLongNotePercentage.ValueChanged += OnFloatFilterChanged;
            MaximumLongNotePercentage.ValueChanged += OnFloatFilterChanged;
            MinimumNotesPerSecond.ValueChanged += OnFloatFilterChanged;
            MaximumNotesPerSecond.ValueChanged += OnFloatFilterChanged;
            MinimumBpm.ValueChanged += OnFloatFilterChanged;
            MaximumBpm.ValueChanged += OnFloatFilterChanged;
        }

        public void Dispose()
        {
            MapsetQuery.ValueChanged -= OnStringFilterChanged;
            ShowOwnedMapsets.ValueChanged -= OnBoolFilterChanged;
            Keymode.ValueChanged -= OnIntFilterChanged;
            RankedStatus.ValueChanged -= OnRankedStatusChanged;
            LengthFilter.ValueChanged -= OnLengthFilterChanged;
            ComboFilter.ValueChanged -= OnComboFilterChanged;
            SortBy.ValueChanged -= OnSortByChanged;
            ReverseSort.ValueChanged -= OnBoolFilterChanged;
            MinimumDifficulty.ValueChanged -= OnFloatFilterChanged;
            MaximumDifficulty.ValueChanged -= OnFloatFilterChanged;
            MinimumLongNotePercentage.ValueChanged -= OnFloatFilterChanged;
            MaximumLongNotePercentage.ValueChanged -= OnFloatFilterChanged;
            MinimumNotesPerSecond.ValueChanged -= OnFloatFilterChanged;
            MaximumNotesPerSecond.ValueChanged -= OnFloatFilterChanged;
            MinimumBpm.ValueChanged -= OnFloatFilterChanged;
            MaximumBpm.ValueChanged -= OnFloatFilterChanged;
            MapsetFiltersChanged = null;

            ActiveTab.Dispose();
            MapsetsExpanded.Dispose();
            MapsetQuery.Dispose();
            PlaylistQuery.Dispose();
            ShowOwnedMapsets.Dispose();
            ShowOwnedPlaylists.Dispose();
            Keymode.Dispose();
            RankedStatus.Dispose();
            LengthFilter.Dispose();
            ComboFilter.Dispose();
            SortBy.Dispose();
            ReverseSort.Dispose();
            MinimumDifficulty.Dispose();
            MaximumDifficulty.Dispose();
            MinimumLongNotePercentage.Dispose();
            MaximumLongNotePercentage.Dispose();
            MinimumNotesPerSecond.Dispose();
            MaximumNotesPerSecond.Dispose();
            MinimumBpm.Dispose();
            MaximumBpm.Dispose();
        }

        private void OnStringFilterChanged(object sender, BindableValueChangedEventArgs<string> args) =>
            NotifyMapsetFiltersChanged();

        private void OnBoolFilterChanged(object sender, BindableValueChangedEventArgs<bool> args) =>
            NotifyMapsetFiltersChanged();

        private void OnIntFilterChanged(object sender, BindableValueChangedEventArgs<int> args) =>
            NotifyMapsetFiltersChanged();

        private void OnFloatFilterChanged(object sender, BindableValueChangedEventArgs<float> args) =>
            NotifyMapsetFiltersChanged();

        private void OnRankedStatusChanged(object sender,
            BindableValueChangedEventArgs<DownloadSearchRankedStatus> args) => NotifyMapsetFiltersChanged();

        private void OnLengthFilterChanged(object sender,
            BindableValueChangedEventArgs<DownloadSearchLengthFilter> args) => NotifyMapsetFiltersChanged();

        private void OnComboFilterChanged(object sender,
            BindableValueChangedEventArgs<DownloadSearchComboFilter> args) => NotifyMapsetFiltersChanged();

        private void OnSortByChanged(object sender,
            BindableValueChangedEventArgs<DownloadSearchSortBy> args) => NotifyMapsetFiltersChanged();

        private void NotifyMapsetFiltersChanged() =>
            MapsetFiltersChanged?.Invoke(this, EventArgs.Empty);
    }
}
