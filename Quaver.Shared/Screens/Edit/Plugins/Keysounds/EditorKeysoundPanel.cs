using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ImGuiNET;
using Quaver.API.Enums;
using Quaver.API.Maps.Structures;
using Quaver.Shared.Audio;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Helpers;
using Quaver.Shared.Screens.Edit.Actions.Keysounds;
using Quaver.Shared.Screens.Edit.Plugins.Timing;
using Wobble;
using Wobble.Audio.Samples;
using Wobble.Graphics.ImGUI;
using Wobble.Logging;
using Wobble.Managers;
using Color = Microsoft.Xna.Framework.Color;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace Quaver.Shared.Screens.Edit.Plugins.Keysounds;

public class EditorKeysoundPanel : SpriteImGui, IEditorPlugin, IColoredImGuiTitle
{
    private sealed class SelectionAssignment
    {
        public int Sample { get; init; }

        public int NoteCount { get; set; }

        public int Volume { get; set; }

        public bool HasMixedVolumes { get; set; }
    }

    private static readonly Vector4 Accent = new(0.27f, 0.78f, 0.96f, 1f);

    private static readonly Vector4 MutedText = new(0.62f, 0.65f, 0.72f, 1f);

    private static readonly Vector4 PanelBackground = new(0.075f, 0.08f, 0.105f, 0.94f);

    private static readonly Vector4 PositiveButton = new(0.10f, 0.48f, 0.68f, 1f);

    private static readonly Vector4 DestructiveButton = new(0.62f, 0.18f, 0.24f, 1f);

    private EditScreen Screen { get; }

    public string Name { get; } = LocalizationManager.Get("Screen_Editor_Keysounds");

    public string Author { get; } = "The Quaver Team";

    public string Description { get; set; } = "";

    public bool IsBuiltIn { get; set; } = true;

    public string Directory { get; set; }

    public bool IsWorkshop { get; set; }

    public bool IsActive { get; set; }

    public bool IsWindowHovered { get; private set; }

    public Color TitleColor => ColorHelper.HexToColor("#166A91");

    private string SampleSearch { get; set; } = "";

    private int SelectedSampleIndex { get; set; } = -1;

    private int Volume { get; set; } = 100;

    private int PendingAssignmentVolumeSample { get; set; } = -1;

    private int PendingAssignmentVolume { get; set; }

    private int[] UsageCounts { get; set; } = Array.Empty<int>();

    public EditorKeysoundPanel(EditScreen screen)
        : base(false, EditorImGuiOptions.GetOptions(), screen.ImGuiScale)
    {
        Screen = screen;
        GameBase.Game.Window.FileDropped += OnFileDropped;
    }

    public void Initialize()
    {
        ClampSelectedSample();

        if (SelectedSampleIndex >= 0)
            SelectSample(SelectedSampleIndex);
    }

    protected override void RenderImguiLayout()
    {
        IsWindowHovered = false;
        ClampSelectedSample();
        UpdateUsageCounts();

        ImGui.SetNextWindowSize(new Vector2(900, 600), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(new Vector2(720, 480), new Vector2(1400, 1000));
        ImGui.PushFont(Options.Fonts.First().Context);
        ((IColoredImGuiTitle)this).ImGuiPushTitleColors();

        var open = IsActive;
        var visible = ImGui.Begin(Name, ref open, ImGuiWindowFlags.NoCollapse);

        if (visible)
        {
            DrawHeader();

            if (Screen.Map.Game != MapGame.Quaver)
            {
                ImGui.Dummy(new Vector2(0, 12));
                ImGui.TextColored(new Vector4(1f, 0.55f, 0.35f, 1f),
                    LocalizationManager.Get("Screen_Editor_CannotEditKeysoundsForImportedMap"));
            }
            else
            {
                DrawWorkspace();
            }
        }

        IsWindowHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows) ||
                          ImGui.IsAnyItemFocused();
        ImGui.End();
        ((IColoredImGuiTitle)this).ImGuiPopTitleColors();
        ImGui.PopFont();
        IsActive = open;
    }

    private void DrawHeader()
    {
        ImGui.TextWrapped(LocalizationManager.Get("Screen_Editor_KeysoundEditorMessage"));
        ImGui.Separator();
    }

    private void DrawWorkspace()
    {
        ImGui.Dummy(new Vector2(0, 6));

        var available = ImGui.GetContentRegionAvail();
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var libraryWidth = Math.Max(350, available.X * 0.54f);
        var inspectorWidth = Math.Max(300, available.X - libraryWidth - spacing);

        ImGui.PushStyleColor(ImGuiCol.ChildBg, PanelBackground);

        ImGui.BeginChild("##KeysoundLibraryPane", new Vector2(libraryWidth, 0), ImGuiChildFlags.Border);
        DrawSampleLibrary();
        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("##KeysoundInspectorPane", new Vector2(inspectorWidth, 0), ImGuiChildFlags.Border);
        DrawInspectorTabs();
        ImGui.EndChild();

        ImGui.PopStyleColor();
    }

    private void DrawInspectorTabs()
    {
        if (!ImGui.BeginTabBar("##KeysoundInspectorTabs", ImGuiTabBarFlags.FittingPolicyResizeDown))
            return;

        if (ImGui.BeginTabItem(LocalizationManager.Get("Screen_Editor_KeysoundSelectedNotes")))
        {
            DrawSelectedNotesTab();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem(LocalizationManager.Get("Screen_Editor_KeysoundSampleUsageTab")))
        {
            DrawSampleUsage();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    private void DrawSampleLibrary()
    {
        DrawSectionTitle(LocalizationManager.Get("Screen_Editor_KeysoundSampleLibrary"));

        ImGui.SetNextItemWidth(-1);
        var sampleSearch = SampleSearch;
        if (ImGui.InputTextWithHint("##KeysoundSampleSearch",
                LocalizationManager.Get("Screen_Editor_KeysoundSearchSamples"), ref sampleSearch, 256))
            SampleSearch = sampleSearch;

        var sampleCount = Screen.WorkingMap.CustomAudioSamples.Count;
        var visibleCount = CountVisibleSamples();
        ImGui.TextColored(MutedText, $"{visibleCount}/{sampleCount} " +
                                     LocalizationManager.Get("Screen_Editor_KeysoundSamples"));

        ImGui.Dummy(new Vector2(0, 3));
        DrawDropTargetHint();
        ImGui.Dummy(new Vector2(0, 6));

        if (sampleCount == 0)
        {
            ImGui.TextColored(MutedText,
                LocalizationManager.Get("Screen_Editor_KeysoundNoSamples"));
            return;
        }

        var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH |
                    ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable |
                    ImGuiTableFlags.SizingStretchProp;

        if (!ImGui.BeginTable("##KeysoundSampleTable", 4, flags, new Vector2(0, -1)))
            return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn(LocalizationManager.Get("Screen_Editor_Name"),
            ImGuiTableColumnFlags.WidthStretch, 1);
        ImGui.TableSetupColumn(LocalizationManager.Get("Screen_Editor_KeysoundUses"),
            ImGuiTableColumnFlags.WidthFixed, 56);
        ImGui.TableSetupColumn(LocalizationManager.Get("Screen_Editor_Preview"),
            ImGuiTableColumnFlags.WidthFixed, 64);
        ImGui.TableHeadersRow();

        for (var i = 0; i < sampleCount; i++)
        {
            var sample = Screen.WorkingMap.CustomAudioSamples[i];
            if (!SampleMatchesSearch(sample))
                continue;

            ImGui.PushID(i);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextColored(i == SelectedSampleIndex ? Accent : MutedText, $"{i + 1}");

            ImGui.TableNextColumn();
            var sampleName = Path.GetFileName(sample.Path);
            if (ImGui.Selectable(sampleName, i == SelectedSampleIndex))
                SelectSample(i);

            if (ImGui.IsItemHovered())
                DrawSampleTooltip(i);

            ImGui.TableNextColumn();
            ImGui.Text($"{UsageCounts[i]}");

            ImGui.TableNextColumn();
            if (ImGui.SmallButton(LocalizationManager.Get("Screen_Editor_KeysoundPlay")))
                CustomAudioSampleCache.Play(i, Volume);

            ImGui.PopID();
        }

        ImGui.EndTable();
    }

    private void DrawDropTargetHint()
    {
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.11f, 0.13f, 0.17f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.14f, 0.17f, 0.22f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Text, MutedText);
        ImGui.BeginDisabled();
        ImGui.Button(LocalizationManager.Get("Screen_Editor_DropAudioSamplesHere"),
            new Vector2(-1, 34));
        ImGui.EndDisabled();
        ImGui.PopStyleColor(3);
    }

    private void DrawSelectedNotesTab()
    {
        var selected = Screen.SelectedHitObjects.Value;
        var assignments = GetSelectionAssignments(selected);
        var hasSample = SelectedSampleIndex >= 0;
        var sampleNumber = SelectedSampleIndex + 1;
        var assigned = hasSample
            ? selected.Count(x => x.KeySounds.Any(y => y.Sample == sampleNumber))
            : 0;

        ImGui.Dummy(new Vector2(0, 6));
        DrawSelectionSummary(selected);

        ImGui.Dummy(new Vector2(0, 5));
        DrawAddKeysoundButton(selected);
        DrawRemovalButtons(selected, assigned);

        ImGui.Dummy(new Vector2(0, 10));
        DrawSubsectionTitle(LocalizationManager.Get("Screen_Editor_KeysoundAssignments"));
        DrawSelectionAssignments(selected, assignments);

        ImGui.Dummy(new Vector2(0, 10));
        DrawSubsectionTitle(LocalizationManager.Get("Screen_Editor_Preview"));
        DrawPreviewButtons(assignments);

        if (hasSample)
        {
            var sample = Screen.WorkingMap.CustomAudioSamples[SelectedSampleIndex];
            var unaffectedByRate = sample.UnaffectedByRate;
            if (ImGui.Checkbox(LocalizationManager.Get("Screen_Editor_KeysoundUnaffectedByRate"),
                    ref unaffectedByRate))
            {
                Screen.ActionManager.Perform(new EditorActionChangeCustomAudioSample(
                    Screen.ActionManager, sample, SelectedSampleIndex, unaffectedByRate));
            }
        }
    }

    private static void DrawSelectionSummary(List<HitObjectInfo> selected)
    {
        if (selected.Count == 0)
        {
            ImGui.TextColored(new Vector4(1f, 0.67f, 0.30f, 1f),
                LocalizationManager.Get("Screen_Editor_NoNotesSelected"));
            ImGui.TextWrapped(LocalizationManager.Get("Screen_Editor_SelectObjectsBeforeChangingKeysounds"));
            return;
        }

        var keysounded = selected.Count(x => x.KeySounds.Count != 0);
        ImGui.Text(LocalizationManager.Get("Screen_Editor_SelectedNoteCount", selected.Count));
        ImGui.SameLine();
        ImGui.TextColored(MutedText,
            LocalizationManager.Get("Screen_Editor_KeysoundKeysoundedNotes", keysounded, selected.Count));
    }

    private unsafe void DrawSampleUsage()
    {
        ImGui.Dummy(new Vector2(0, 6));

        if (SelectedSampleIndex < 0)
        {
            ImGui.TextColored(MutedText,
                LocalizationManager.Get("Screen_Editor_KeysoundNoSamples"));
            return;
        }

        var sampleNumber = SelectedSampleIndex + 1;
        var sample = Screen.WorkingMap.CustomAudioSamples[SelectedSampleIndex];
        var usages = Screen.WorkingMap.HitObjects
            .SelectMany(hitObject => hitObject.KeySounds
                .Where(keysound => keysound.Sample == sampleNumber)
                .Take(1)
                .Select(keysound => (HitObject: hitObject, Keysound: keysound)))
            .OrderBy(x => x.HitObject.StartTime)
            .ThenBy(x => x.HitObject.Lane)
            .ToList();

        ImGui.TextWrapped(Path.GetFileName(sample.Path));
        ImGui.TextColored(MutedText,
            LocalizationManager.Get("Screen_Editor_KeysoundSampleUsage", usages.Count));
        ImGui.Dummy(new Vector2(0, 6));

        if (usages.Count == 0)
        {
            ImGui.TextColored(MutedText,
                LocalizationManager.Get("Screen_Editor_KeysoundSampleNotUsed"));
            return;
        }

        var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH |
                    ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp;

        if (!ImGui.BeginTable("##KeysoundSampleUsageTable", 2, flags, new Vector2(0, -1)))
            return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn(LocalizationManager.Get("Screen_Editor_Time"),
            ImGuiTableColumnFlags.WidthStretch, 1);
        ImGui.TableSetupColumn(LocalizationManager.Get("Screen_Editor_Volume"),
            ImGuiTableColumnFlags.WidthFixed, 72);
        ImGui.TableHeadersRow();

        var clipperRaw = new ImGuiListClipper();
        var clipper = new ImGuiListClipperPtr(&clipperRaw);
        clipper.Begin(usages.Count);

        while (clipper.Step())
        {
            for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
            {
                var usage = usages[i];

                ImGui.PushID(i);
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                var goTo = $"{usage.HitObject.StartTime}|{usage.HitObject.Lane}";
                if (ImGui.Button(goTo))
                    Screen.GoToObjects(goTo);

                ImGui.TableNextColumn();
                ImGui.Text($"{usage.Keysound.Volume}%");

                ImGui.PopID();
            }
        }

        ImGui.EndTable();
    }

    private void DrawSelectionAssignments(List<HitObjectInfo> selected,
        List<SelectionAssignment> assignments)
    {
        if (assignments.Count == 0)
        {
            ImGui.TextColored(MutedText,
                LocalizationManager.Get("Screen_Editor_KeysoundNoAssignments"));
            return;
        }

        var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH |
                    ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp;

        if (!ImGui.BeginTable("##SelectedKeysoundAssignments", 3, flags, new Vector2(0, 150)))
            return;

        ImGui.TableSetupColumn(LocalizationManager.Get("Screen_Editor_Name"),
            ImGuiTableColumnFlags.WidthStretch, 1);
        ImGui.TableSetupColumn(LocalizationManager.Get("Screen_Editor_Volume"),
            ImGuiTableColumnFlags.WidthFixed, 92);
        ImGui.TableSetupColumn(LocalizationManager.Get("Screen_Editor_KeysoundCoverage"),
            ImGuiTableColumnFlags.WidthFixed, 68);
        ImGui.TableHeadersRow();

        foreach (var assignment in assignments)
        {
            ImGui.PushID(assignment.Sample);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            var index = assignment.Sample - 1;
            var isValid = index >= 0 && index < Screen.WorkingMap.CustomAudioSamples.Count;
            var name = isValid
                ? Path.GetFileName(Screen.WorkingMap.CustomAudioSamples[index].Path)
                : $"#{assignment.Sample}";

            if (ImGui.Selectable(name, isValid && index == SelectedSampleIndex) && isValid)
            {
                SelectedSampleIndex = index;
                Volume = assignment.Volume;
            }

            ImGui.TableNextColumn();
            DrawAssignmentVolume(assignment);

            ImGui.TableNextColumn();
            ImGui.Text($"{assignment.NoteCount}/{selected.Count}");

            ImGui.PopID();
        }

        ImGui.EndTable();
    }

    private void DrawAssignmentVolume(SelectionAssignment assignment)
    {
        var isPending = PendingAssignmentVolumeSample == assignment.Sample;
        var volume = isPending ? PendingAssignmentVolume : assignment.Volume;
        var format = assignment.HasMixedVolumes && !isPending ? "%d%%*" : "%d%%";

        ImGui.SetNextItemWidth(-1);
        if (ImGui.DragInt("##AssignmentVolume", ref volume, 1, 1, 100, format))
        {
            PendingAssignmentVolumeSample = assignment.Sample;
            PendingAssignmentVolume = Math.Clamp(volume, 1, 100);
            isPending = true;
        }

        if (assignment.HasMixedVolumes && ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(LocalizationManager.Get("Screen_Editor_KeysoundMixed"));
            ImGui.EndTooltip();
        }

        if (!isPending || !ImGui.IsItemDeactivatedAfterEdit())
            return;

        ApplyKeysoundVolume(assignment.Sample, PendingAssignmentVolume);
        PendingAssignmentVolumeSample = -1;
    }

    private void DrawPreviewButtons(List<SelectionAssignment> assignments)
    {
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var buttonWidth = (ImGui.GetContentRegionAvail().X - spacing) / 2f;
        var buttonSize = new Vector2(buttonWidth, 34);
        var hasSample = SelectedSampleIndex >= 0;
        var sampleCount = Screen.WorkingMap.CustomAudioSamples.Count;
        var canPreviewAll = assignments.Any(x => x.Sample >= 1 && x.Sample <= sampleCount);

        ImGui.Dummy(new Vector2(0, 8));
        ImGui.BeginDisabled(!hasSample);
        if (ImGui.Button(LocalizationManager.Get("Screen_Editor_KeysoundPreviewSelected"),
                buttonSize))
            CustomAudioSampleCache.Play(SelectedSampleIndex, Volume);
        ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.BeginDisabled(!canPreviewAll);
        if (ImGui.Button(LocalizationManager.Get("Screen_Editor_KeysoundPreviewAll"),
                buttonSize))
            PreviewAll(assignments);
        ImGui.EndDisabled();
    }

    private void DrawAddKeysoundButton(List<HitObjectInfo> selected)
    {
        var hasSelection = selected.Count != 0;
        var hasSample = SelectedSampleIndex >= 0;

        ImGui.Dummy(new Vector2(0, 8));
        ImGui.BeginDisabled(!hasSelection || !hasSample);
        if (DrawColoredButton(LocalizationManager.Get("Screen_Editor_KeysoundAddToSelected"),
                new Vector2(-1, 34), PositiveButton))
            ApplyKeysoundChange(EditorKeysoundChangeMode.Add);
        ImGui.EndDisabled();
    }

    private void DrawRemovalButtons(List<HitObjectInfo> selected, int assigned)
    {
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var buttonWidth = (ImGui.GetContentRegionAvail().X - spacing) / 2f;
        var buttonSize = new Vector2(buttonWidth, 34);
        var hasSelection = selected.Count != 0;
        var hasSample = SelectedSampleIndex >= 0;

        ImGui.Dummy(new Vector2(0, 8));
        ImGui.BeginDisabled(!hasSelection || !hasSample || assigned == 0);
        if (DrawColoredButton(LocalizationManager.Get("Screen_Editor_RemoveSampleFromSelected"),
                buttonSize, DestructiveButton))
            ApplyKeysoundChange(EditorKeysoundChangeMode.Remove);
        ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.BeginDisabled(!hasSelection || selected.All(x => x.KeySounds.Count == 0));
        if (DrawColoredButton(LocalizationManager.Get("Screen_Editor_ClearKeysounds"),
                buttonSize, DestructiveButton))
            ApplyKeysoundChange(EditorKeysoundChangeMode.Clear);
        ImGui.EndDisabled();
    }

    private static bool DrawColoredButton(string label, Vector2 size, Vector4 color)
    {
        var hovered = new Vector4(
            Math.Min(1, color.X + 0.08f),
            Math.Min(1, color.Y + 0.08f),
            Math.Min(1, color.Z + 0.08f),
            color.W);
        var active = new Vector4(color.X * 0.85f, color.Y * 0.85f, color.Z * 0.85f, color.W);

        ImGui.PushStyleColor(ImGuiCol.Button, color);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, active);
        var clicked = ImGui.Button(label, size);
        ImGui.PopStyleColor(3);
        return clicked;
    }

    private static void DrawSectionTitle(string text)
    {
        ImGui.TextColored(Accent, text);
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, 3));
    }

    private static void DrawSubsectionTitle(string text)
    {
        ImGui.TextColored(MutedText, text);
        ImGui.Separator();
    }

    private void DrawSampleTooltip(int index)
    {
        var sample = Screen.WorkingMap.CustomAudioSamples[index];

        ImGui.BeginTooltip();
        ImGui.TextWrapped(sample.Path);
        ImGui.TextColored(MutedText,
            LocalizationManager.Get("Screen_Editor_KeysoundSampleUsage", UsageCounts[index]));
        ImGui.TextColored(MutedText,
            LocalizationManager.Get(sample.UnaffectedByRate
                ? "Screen_Editor_KeysoundUnaffectedByRate"
                : "Screen_Editor_KeysoundFollowsRate"));
        ImGui.EndTooltip();
    }

    private List<SelectionAssignment> GetSelectionAssignments(List<HitObjectInfo> selected)
    {
        var assignments = new Dictionary<int, SelectionAssignment>();

        foreach (var hitObject in selected)
        {
            foreach (var keysound in hitObject.KeySounds)
            {
                if (!assignments.TryGetValue(keysound.Sample, out var assignment))
                {
                    assignment = new SelectionAssignment
                    {
                        Sample = keysound.Sample,
                        Volume = keysound.Volume
                    };
                    assignments.Add(keysound.Sample, assignment);
                }

                assignment.NoteCount++;
                assignment.HasMixedVolumes |= assignment.Volume != keysound.Volume;
            }
        }

        return assignments.Values.OrderBy(x => x.Sample).ToList();
    }

    private void ApplyKeysoundChange(EditorKeysoundChangeMode mode)
    {
        if (Screen.SelectedHitObjects.Value.Count == 0)
            return;

        if (mode != EditorKeysoundChangeMode.Clear && SelectedSampleIndex < 0)
            return;

        var action = new EditorActionChangeKeysounds(Screen.ActionManager,
            new List<HitObjectInfo>(Screen.SelectedHitObjects.Value), mode,
            SelectedSampleIndex + 1, Volume);

        if (action.HasChanges)
            Screen.ActionManager.Perform(action);
    }

    private void ApplyKeysoundVolume(int sample, int volume)
    {
        var action = new EditorActionChangeKeysounds(Screen.ActionManager,
            new List<HitObjectInfo>(Screen.SelectedHitObjects.Value),
            EditorKeysoundChangeMode.ChangeVolume, sample, volume);

        if (action.HasChanges)
            Screen.ActionManager.Perform(action);

        if (SelectedSampleIndex == sample - 1)
            Volume = volume;
    }

    private void PreviewAll(List<SelectionAssignment> assignments)
    {
        var sampleCount = Screen.WorkingMap.CustomAudioSamples.Count;

        foreach (var assignment in assignments.Where(x => x.Sample >= 1 && x.Sample <= sampleCount))
            CustomAudioSampleCache.Play(assignment.Sample - 1, assignment.Volume);
    }

    private void SelectSample(int index)
    {
        SelectedSampleIndex = index;

        var assignment = Screen.SelectedHitObjects.Value
            .SelectMany(x => x.KeySounds)
            .FirstOrDefault(x => x.Sample == index + 1);

        Volume = assignment?.Volume ?? 100;
    }

    private void ClampSelectedSample()
    {
        var sampleCount = Screen.WorkingMap.CustomAudioSamples.Count;

        if (sampleCount == 0)
        {
            SelectedSampleIndex = -1;
            return;
        }

        if (SelectedSampleIndex < 0)
            SelectedSampleIndex = 0;

        if (SelectedSampleIndex >= sampleCount)
            SelectedSampleIndex = sampleCount - 1;
    }

    private void UpdateUsageCounts()
    {
        var sampleCount = Screen.WorkingMap.CustomAudioSamples.Count;
        if (UsageCounts.Length != sampleCount)
            UsageCounts = new int[sampleCount];
        else
            Array.Clear(UsageCounts);

        foreach (var hitObject in Screen.WorkingMap.HitObjects)
        {
            foreach (var keysound in hitObject.KeySounds)
            {
                var index = keysound.Sample - 1;
                if (index >= 0 && index < UsageCounts.Length)
                    UsageCounts[index]++;
            }
        }
    }

    private int CountVisibleSamples()
    {
        var count = 0;

        foreach (var sample in Screen.WorkingMap.CustomAudioSamples)
        {
            if (SampleMatchesSearch(sample))
                count++;
        }

        return count;
    }

    private bool SampleMatchesSearch(CustomAudioSampleInfo sample) =>
        string.IsNullOrWhiteSpace(SampleSearch) ||
        sample.Path.Contains(SampleSearch, StringComparison.OrdinalIgnoreCase);

    private void OnFileDropped(object sender, string file)
    {
        if (!IsActive || Screen.Map.Game != MapGame.Quaver || !IsSupportedAudioFile(file))
            return;

        ImportSample(file);
    }

    private void ImportSample(string file)
    {
        try
        {
            using (var sample = new AudioSample(file))
            {
                // Loading the sample validates it before it is copied into the mapset.
            }

            var mapDirectory = Path.GetFullPath(Path.Combine(ConfigManager.SongDirectory.Value,
                Screen.Map.Directory));
            var sourcePath = Path.GetFullPath(file);
            var relativePath = Path.GetRelativePath(mapDirectory, sourcePath);
            var sourceIsInMapDirectory = !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}") &&
                                         relativePath != ".." &&
                                         !Path.IsPathRooted(relativePath);

            if (!sourceIsInMapDirectory)
            {
                relativePath = Path.GetFileName(sourcePath);
                var existingPath = Path.Combine(mapDirectory, relativePath);
                var existingFileIndex = Screen.WorkingMap.CustomAudioSamples.FindIndex(x =>
                    string.Equals(x.Path, relativePath, StringComparison.OrdinalIgnoreCase));

                if (existingFileIndex >= 0 && File.Exists(existingPath) &&
                    FilesHaveSameContent(sourcePath, existingPath))
                {
                    SelectSample(existingFileIndex);
                    NotificationManager.Show(NotificationLevel.Info,
                        LocalizationManager.Get("Screen_Editor_KeysoundAlreadyImported"));
                    return;
                }

                relativePath = GetAvailableFileName(mapDirectory, relativePath);
                File.Copy(sourcePath, Path.Combine(mapDirectory, relativePath));
            }

            relativePath = relativePath.Replace(Path.DirectorySeparatorChar, '/');
            var existingIndex = Screen.WorkingMap.CustomAudioSamples.FindIndex(x =>
                string.Equals(x.Path, relativePath, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                SelectSample(existingIndex);
                NotificationManager.Show(NotificationLevel.Info,
                    LocalizationManager.Get("Screen_Editor_KeysoundAlreadyImported"));
                return;
            }

            var info = new CustomAudioSampleInfo { Path = relativePath };
            Screen.ActionManager.Perform(new EditorActionAddCustomAudioSample(Screen.ActionManager,
                Screen.WorkingMap, info));
            SelectSample(Screen.WorkingMap.CustomAudioSamples.Count - 1);

            NotificationManager.Show(NotificationLevel.Success,
                LocalizationManager.Get("Screen_Editor_KeysoundImported", relativePath));
        }
        catch (Exception e)
        {
            Logger.Error(e, LogType.Runtime);
            NotificationManager.Show(NotificationLevel.Error,
                LocalizationManager.Get("Screen_Editor_KeysoundImportFailed"));
        }
    }

    private static string GetAvailableFileName(string directory, string fileName)
    {
        var result = fileName;
        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var suffix = 2;

        while (File.Exists(Path.Combine(directory, result)))
            result = $"{name}_{suffix++}{extension}";

        return result;
    }

    private static bool FilesHaveSameContent(string first, string second)
    {
        var firstInfo = new FileInfo(first);
        var secondInfo = new FileInfo(second);

        if (firstInfo.Length != secondInfo.Length)
            return false;

        using var algorithm = SHA256.Create();
        using var firstStream = File.OpenRead(first);
        var firstHash = algorithm.ComputeHash(firstStream);
        using var secondStream = File.OpenRead(second);
        var secondHash = algorithm.ComputeHash(secondStream);
        return firstHash.SequenceEqual(secondHash);
    }

    private static bool IsSupportedAudioFile(string file)
    {
        var extension = Path.GetExtension(file);
        return extension.Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase);
    }

    public override void Destroy()
    {
        GameBase.Game.Window.FileDropped -= OnFileDropped;
        base.Destroy();
    }
}
