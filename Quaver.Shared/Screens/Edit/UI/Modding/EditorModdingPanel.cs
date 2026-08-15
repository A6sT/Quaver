using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Quaver.Server.Client.Structures.Modding;
using Quaver.Shared.Assets;
using Quaver.Shared.Graphics;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Helpers;
using Quaver.Shared.Online;
using Quaver.Shared.Screens.Menu.UI.Jukebox;
using Wobble.Assets;
using Wobble.Graphics;
using Wobble.Graphics.Buttons;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Buttons;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Managers;

namespace Quaver.Shared.Screens.Edit.UI.Modding
{
    public sealed class EditorModdingPanel : DraggableButton
    {
        private const int OuterPadding = 16;

        private const int ToolbarHeight = 38;

        private const int ColumnHeaderHeight = 26;

        private const int TimelineHeight = 250;

        private EditScreen Screen { get; }

        public EditorModdingPanelContainer Container { get; }

        public MapModdingEntry SelectedMod { get; private set; }

        private List<MapModdingEntry> Mods { get; set; } = [];

        private EditorModdingTimeline Timeline { get; set; }

        private EditorModdingDetails Details { get; set; }

        private SpriteTextPlus RefreshStatus { get; set; }

        private RoundedButton RefreshButton { get; set; }

        private RoundedButton NewModButton { get; set; }

        private EditorModdingStatusDropdown StatusDropdown { get; set; }

        private bool IsLoading { get; set; }

        private bool IsMutating { get; set; }

        private bool HasLoaded { get; set; }

        private string RefreshError { get; set; }

        private int? MapCreatorId { get; set; }

        private bool SelectNewestAfterRefresh { get; set; }

        internal bool IsCurrentUserMapCreator => MapCreatorId.HasValue &&
                                                 OnlineManager.Self?.OnlineUser?.Id == MapCreatorId.Value;

        public EditorModdingPanel(EditScreen screen, EditorModdingPanelContainer container)
            : base(UserInterface.AutoModPanel)
        {
            Screen = screen;
            Container = container;
            Size = new ScalableVector2(Image.Width, Image.Height);

            new EditorModdingHeader(this) { Parent = this };
            CreateToolbar();
            CreateColumnHeader();
            CreateTimeline();
            CreateDetails();
        }

        public void EnsureLoaded()
        {
            if (!HasLoaded && !IsLoading)
                Refresh();
        }

        public void Refresh()
        {
            if (IsLoading || IsMutating || Screen.WorkingMap.MapId == -1)
                return;

            var client = OnlineManager.Client;
            if (client == null)
            {
                RefreshError = LocalizationManager.Get("Screen_Editor_ModdingUnavailable");
                UpdateRefreshStatus();
                return;
            }

            IsLoading = true;
            RefreshError = null;
            UpdateInteractionState();
            UpdateRefreshStatus();

            _ = LoadModsAsync(client, Screen.WorkingMap.MapId);
        }

        public void SelectMod(MapModdingEntry mod)
        {
            if (IsMutating || mod == null || SelectedMod?.Id == mod.Id)
                return;

            if (Details.HasDraft)
            {
                DialogManager.Show(new YesNoDialog(
                    LocalizationManager.Get("Screen_Editor_ModdingDiscardDraftTitle"),
                    LocalizationManager.Get("Screen_Editor_ModdingDiscardDraftMessage"),
                    () => SelectModImmediately(mod)));
                return;
            }

            SelectModImmediately(mod);
        }

        public void GoToSelection(MapModdingEntry mod)
        {
            if (!string.IsNullOrWhiteSpace(mod?.MapTimestamp))
                Screen.GoToObjects(mod.MapTimestamp);
        }

        internal void ShowNewModComposer()
        {
            if (IsMutating || !CanPost())
                return;

            Details.ShowNewModComposer(Screen.GetSelectedObjectTimestamps());
        }

        internal void ShowReplyComposer()
        {
            if (IsMutating || !CanPost() || SelectedMod == null)
                return;

            Details.ShowReplyComposer(SelectedMod);
        }

        internal void SubmitNewMod(MapModdingType type, string selection, string comment)
        {
            if (string.IsNullOrWhiteSpace(comment) || IsLoading || IsMutating)
                return;

            IsMutating = true;
            Details.SetBusy(true);
            UpdateInteractionState();
            _ = RunMutationAsync(
                () => OnlineManager.Client.SubmitMapModAsync(Screen.WorkingMap.MapId, type, selection, comment),
                "Screen_Editor_ModdingSubmitted", true);
        }

        internal void SubmitReply(string comment)
        {
            if (SelectedMod == null || string.IsNullOrWhiteSpace(comment) || IsLoading || IsMutating)
                return;

            IsMutating = true;
            Details.SetBusy(true);
            UpdateInteractionState();
            _ = RunMutationAsync(
                () => OnlineManager.Client.SubmitMapModCommentAsync(Screen.WorkingMap.MapId, SelectedMod.Id, comment),
                "Screen_Editor_ModdingReplySubmitted");
        }

        internal void UpdateStatus(MapModdingStatus status)
        {
            if (!IsCurrentUserMapCreator || SelectedMod == null || IsLoading || IsMutating)
                return;

            IsMutating = true;
            Details.SetBusy(true);
            UpdateInteractionState();
            _ = RunMutationAsync(
                () => OnlineManager.Client.UpdateMapModStatusAsync(Screen.WorkingMap.MapId, SelectedMod.Id, status),
                "Screen_Editor_ModdingStatusUpdated");
        }

        private async Task LoadModsAsync(Quaver.Server.Client.OnlineClient client, int mapId)
        {
            try
            {
                var modsTask = client.RetrieveMapModsAsync(mapId);
                var mapTask = Task.Run(() => client.RetrieveMapInfo(mapId));
                await Task.WhenAll(modsTask, mapTask).ConfigureAwait(false);

                var response = await modsTask.ConfigureAwait(false);
                var mapResponse = await mapTask.ConfigureAwait(false);

                AddScheduledUpdate(() =>
                {
                    if (IsDisposed)
                        return;

                    Mods = response?.Mods ?? [];
                    MapCreatorId = mapResponse?.Map?.CreatorId;
                    IsLoading = false;
                    HasLoaded = true;
                    RefreshError = null;
                    ApplyFilter();
                    UpdateInteractionState();
                    UpdateRefreshStatus();
                });
            }
            catch (Exception exception)
            {
                AddScheduledUpdate(() =>
                {
                    if (IsDisposed)
                        return;

                    IsLoading = false;
                    RefreshError = exception.Message;
                    Details.SetBusy(false);
                    UpdateInteractionState();
                    UpdateRefreshStatus();
                });
            }
        }

        private async Task RunMutationAsync(Func<Task> mutation, string successKey, bool selectNewest = false)
        {
            try
            {
                await mutation().ConfigureAwait(false);
                AddScheduledUpdate(() =>
                {
                    if (IsDisposed)
                        return;

                    SelectNewestAfterRefresh = selectNewest;
                    IsMutating = false;
                    Details.SetBusy(false);
                    Details.CloseComposer();
                    NotificationManager.Show(NotificationLevel.Success, LocalizationManager.Get(successKey));
                    Refresh();
                });
            }
            catch (Exception exception)
            {
                AddScheduledUpdate(() =>
                {
                    if (IsDisposed)
                        return;

                    IsMutating = false;
                    Details.SetBusy(false);
                    UpdateInteractionState();
                    NotificationManager.Show(NotificationLevel.Error, exception.Message);
                });
            }
        }

        private void CreateToolbar()
        {
            var headerHeight = UserInterface.AutoModPanelHeader.Height;
            var toolbar = new FlexContainer
            {
                Parent = this,
                Position = new ScalableVector2(OuterPadding, headerHeight + 10),
                Size = new ScalableVector2(Width - OuterPadding * 2, ToolbarHeight),
                Direction = FlexDirection.Row,
                AlignItems = FlexAlignItems.Center,
                ColumnGap = 10
            };

            RefreshButton = CreateButton("SkinEditor_Refresh", 105, (sender, args) => Refresh());
            RefreshButton.Parent = toolbar;

            RefreshStatus = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterSemiBold), "", 14)
            {
                Parent = toolbar,
                Tint = Color.LightGray
            };
            toolbar.SetItemOptions(RefreshStatus, new FlexItemOptions { Grow = 1, Shrink = 1 });

            StatusDropdown = new EditorModdingStatusDropdown { Parent = toolbar };
            StatusDropdown.Dropdown.ItemSelected += (sender, args) => ApplyFilter();

            NewModButton = CreateButton("Screen_Editor_ModdingNewMod", 116, (sender, args) => ShowNewModComposer());
            NewModButton.Parent = toolbar;
            toolbar.RefreshLayout();
        }

        private void CreateColumnHeader()
        {
            var y = UserInterface.AutoModPanelHeader.Height + 10 + ToolbarHeight + 8;
            var header = new Container
            {
                Parent = this,
                Position = new ScalableVector2(OuterPadding, y),
                Size = new ScalableVector2(Width - OuterPadding * 2, ColumnHeaderHeight)
            };

            CreateColumnLabel(header, "Screen_Editor_Time", 12);
            CreateColumnLabel(header, "Screen_Editor_StatusLabel", 122);
            CreateColumnLabel(header, "Screen_Editor_ModdingTypeLabel", 220);
            CreateColumnLabel(header, "Screen_Editor_ModdingAuthor", 328);
            CreateColumnLabel(header, "Screen_Editor_ModdingPreview", 458);
        }

        private void CreateTimeline()
        {
            var y = UserInterface.AutoModPanelHeader.Height + 10 + ToolbarHeight + 8 + ColumnHeaderHeight;
            Timeline = new EditorModdingTimeline(this, new ScalableVector2(Width - OuterPadding * 2, TimelineHeight))
            {
                Parent = this,
                Position = new ScalableVector2(OuterPadding, y)
            };
        }

        private void CreateDetails()
        {
            const int spacing = 12;
            var y = Timeline.Y + Timeline.Height + spacing;
            Details = new EditorModdingDetails(this, Screen, new ScalableVector2(Width - OuterPadding * 2, Height - y - OuterPadding))
            {
                Parent = this,
                Position = new ScalableVector2(OuterPadding, y)
            };
            Details.ShowEmpty();
        }

        private void ApplyFilter()
        {
            var statusIndex = StatusDropdown?.Dropdown.SelectedIndex ?? 0;
            var filtered = Mods
                .Where(x => statusIndex == 0 || (int)x.Status == statusIndex - 1)
                .OrderBy(x => EditorModdingFormatting.GetDisplayTime(x).HasValue ? 1 : 0)
                .ThenBy(x => EditorModdingFormatting.GetDisplayTime(x) ?? 0)
                .ThenBy(x => x.Timestamp)
                .ToList();

            Timeline.SetItems(filtered);

            if (SelectNewestAfterRefresh && filtered.Count > 0)
            {
                SelectNewestAfterRefresh = false;
                SelectModImmediately(filtered.OrderByDescending(x => x.Id).First());
                return;
            }

            var refreshedSelection = filtered.FirstOrDefault(x => x.Id == SelectedMod?.Id);
            if (refreshedSelection != null && !Details.HasDraft)
                SelectModImmediately(refreshedSelection, false);
            else if (filtered.Count > 0 && !Details.HasDraft)
                SelectModImmediately(filtered[0]);
            else if (filtered.Count == 0 && !Details.HasDraft)
            {
                SelectedMod = null;
                Details.ShowEmpty();
            }
        }

        private void SelectModImmediately(MapModdingEntry mod, bool refreshTimeline = true)
        {
            SelectedMod = mod;
            Details.ShowDiscussion(mod, IsCurrentUserMapCreator);

            if (refreshTimeline)
                Timeline.RefreshSelection();
        }

        private bool CanPost()
        {
            if (OnlineManager.Connected && OnlineManager.Self != null)
                return true;

            NotificationManager.Show(NotificationLevel.Warning, LocalizationManager.Get("Screen_Editor_ModdingMustBeLoggedIn"));
            return false;
        }

        private void UpdateInteractionState()
        {
            var isBusy = IsLoading || IsMutating;
            RefreshButton.IsClickable = !isBusy;
            RefreshButton.Alpha = isBusy ? 0.5f : 1;

            var canPost = !isBusy && OnlineManager.Connected && OnlineManager.Self != null;
            NewModButton.IsClickable = canPost;
            NewModButton.Alpha = canPost ? 1 : 0.5f;
        }

        private void UpdateRefreshStatus()
        {
            if (RefreshStatus == null)
                return;

            if (IsLoading)
            {
                RefreshStatus.Text = LocalizationManager.Get("Screen_Editor_ModdingRefreshing");
                RefreshStatus.Tint = ColorHelper.HexToColor("#45D6F5");
                return;
            }

            if (!string.IsNullOrWhiteSpace(RefreshError))
            {
                RefreshStatus.Text = RefreshError;
                RefreshStatus.Tint = ColorHelper.HexToColor("#F9645D");
                RefreshStatus.TruncateWithEllipsis(180);
                return;
            }

            RefreshStatus.Text = "";
            RefreshStatus.Tint = Color.LightGray;
        }

        private static RoundedButton CreateButton(string localizationKey, float width, EventHandler clicked)
        {
            var button = new RoundedButton(clicked)
            {
                Size = new ScalableVector2(width, 32),
                CornerRadius = 6,
                Tint = ColorHelper.HexToColor("#363636"),
                Depth = -1
            };
            button.SetLabel(FontManager.GetWobbleFont(Fonts.InterSemiBold), LocalizationManager.Get(localizationKey), 16, Color.White);
            return button;
        }

        private static void CreateColumnLabel(Container parent, string localizationKey, float x)
        {
            new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterSemiBold), LocalizationManager.Get(localizationKey).TrimEnd(':'), 14)
            {
                Parent = parent,
                Alignment = Alignment.MidLeft,
                X = x,
                Tint = Color.Gray
            };
        }
    }
}