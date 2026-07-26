using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Quaver.Shared.Config;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Skinning;
using Quaver.Shared.Skinning.V2;
using Wobble;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Managers;

namespace Quaver.Shared.Screens.V2.SkinEditor
{
    internal sealed class SkinEditorController
    {
        internal const float LeftPanelWidth = 220;
        internal const float RightPanelWidth = 360;
        internal const float AssetPanelHeight = 230;

        private readonly ISkinV2EditorHost host;
        private SkinEditorSession session;
        private SkinEditorOverlay overlay;
        private SkinEditorFileFingerprint fingerprint;
        private bool watcherWasActive;

        public bool IsOpen => overlay != null;

        public static bool ReopenAfterSkinReload { get; set; }

        public SkinEditorController(ISkinV2EditorHost host) =>
            this.host = host ?? throw new ArgumentNullException(nameof(host));

        public void Open()
        {
            if (IsOpen || SkinManager.SkinV2 == null)
                return;

            watcherWasActive = SkinManager.Watcher != null;
            if (watcherWasActive)
                SkinManager.StopWatching();

            try
            {
                session = new SkinEditorSession(SkinManager.SkinV2);
                fingerprint = SkinEditorFileFingerprint.Capture(SkinManager.SkinV2.ConfigPath);
                host.SetSkinEditorLayout(true, LeftPanelWidth, RightPanelWidth, AssetPanelHeight);
                overlay = new SkinEditorOverlay(host, session, Save, RequestClose, CopyWorkshopSkin,
                    ConfigManager.UseSteamWorkshopSkin.Value)
                {
                    Parent = host.EditorRoot
                };
            }
            catch (Exception e)
            {
                overlay?.Destroy();
                overlay = null;
                session = null;
                host.SetSkinEditorLayout(false);
                ResumeWatcher();
                NotificationManager.Show(NotificationLevel.Error,
                    LocalizationManager.Get("SkinEditor_OpenFailed", e.Message));
            }
        }

        public void RequestClose()
        {
            if (!IsOpen)
                return;

            if (!session.IsDirty && !session.HasInvalidInput)
            {
                Close(false);
                return;
            }

            DialogManager.Show(new SkinEditorChoiceDialog(
                LocalizationManager.Get("SkinEditor_UnsavedTitle"),
                LocalizationManager.Get("SkinEditor_UnsavedMessage"),
                LocalizationManager.Get("SkinEditor_Save"), () =>
                {
                    if (Save())
                        Close(false);
                },
                LocalizationManager.Get("SkinEditor_Discard"), () => Close(true),
                LocalizationManager.Get("SkinEditor_Cancel"), null));
        }

        public void Destroy()
        {
            if (!IsOpen)
                return;

            overlay.Destroy();
            overlay = null;
            session = null;
            watcherWasActive = false;
        }

        private bool Save()
        {
            if (!IsOpen || session.HasInvalidInput ||
                ConfigManager.UseSteamWorkshopSkin.Value)
                return false;

            var configPath = SkinManager.SkinV2.ConfigPath;
            bool matches;
            try
            {
                matches = fingerprint.Matches(configPath);
            }
            catch (Exception e)
            {
                NotificationManager.Show(NotificationLevel.Error,
                    LocalizationManager.Get("SkinEditor_FileCheckFailed", e.Message));
                return false;
            }

            if (!matches)
            {
                DialogManager.Show(new SkinEditorChoiceDialog(
                    LocalizationManager.Get("SkinEditor_ConflictTitle"),
                    LocalizationManager.Get("SkinEditor_ConflictMessage"),
                    LocalizationManager.Get("SkinEditor_Reload"), ReloadFromDisk,
                    LocalizationManager.Get("SkinEditor_Overwrite"), () => SaveIgnoringConflict(),
                    LocalizationManager.Get("SkinEditor_Cancel"), null));
                return false;
            }

            return SaveIgnoringConflict();
        }

        private bool SaveIgnoringConflict()
        {
            if (!SkinManager.TryPublishV2Config(session.Working, out var errors))
            {
                ShowErrors(errors);
                return false;
            }

            session.AcceptWorkingAsBaseline();
            fingerprint = SkinEditorFileFingerprint.Capture(SkinManager.SkinV2.ConfigPath);
            overlay.RefreshSaveState();
            NotificationManager.Show(NotificationLevel.Success,
                LocalizationManager.Get("SkinEditor_SaveSuccess"));
            return true;
        }

        private void ReloadFromDisk()
        {
            try
            {
                var old = SkinManager.SkinV2;
                using var replacement = new SkinStoreV2(old.RootDirectory);
                session = new SkinEditorSession(replacement);
                fingerprint = SkinEditorFileFingerprint.Capture(old.ConfigPath);
                Close(true);
                ReopenAfterSkinReload = true;
                SkinManager.TimeSkinReloadRequested = GameBase.Game.TimeRunning;
            }
            catch (Exception e)
            {
                NotificationManager.Show(NotificationLevel.Error,
                    LocalizationManager.Get("SkinEditor_ReloadFailed", e.Message));
            }
        }

        private void Close(bool restoreInitial)
        {
            if (!IsOpen)
                return;

            if (restoreInitial)
                host.ApplySkinEditorPreview(session.Initial);

            overlay.Destroy();
            overlay = null;
            host.SetSkinEditorLayout(false);
            ResumeWatcher();
        }

        private void ResumeWatcher()
        {
            if (watcherWasActive)
                SkinManager.StartWatching();
            watcherWasActive = false;
        }

        private void CopyWorkshopSkin()
        {
            var defaultName = SanitizeName(ConfigManager.Skin.Value + " Local");
            DialogManager.Show(new SkinEditorTextPromptDialog(
                LocalizationManager.Get("SkinEditor_CopyTitle"), defaultName, requestedName =>
                {
                    try
                    {
                        var source = SkinManager.SkinV2.RootDirectory;
                        var name = GetUniqueLocalName(SanitizeName(requestedName));
                        var destination = Path.Combine(ConfigManager.SkinDirectory.Value, name);
                        var staging = Path.Combine(ConfigManager.SkinDirectory.Value,
                            ".skin-editor-copy-" + Guid.NewGuid().ToString("N"));
                        try
                        {
                            CopyDirectory(source, staging);
                            Directory.Move(staging, destination);
                        }
                        catch
                        {
                            if (Directory.Exists(staging))
                                Directory.Delete(staging, true);
                            throw;
                        }

                        ConfigManager.UseSteamWorkshopSkin.Value = false;
                        ConfigManager.Skin.Value = name;
                        ReopenAfterSkinReload = true;
                        Close(false);
                        SkinManager.TimeSkinReloadRequested = GameBase.Game.TimeRunning;
                    }
                    catch (Exception e)
                    {
                        NotificationManager.Show(NotificationLevel.Error,
                            LocalizationManager.Get("SkinEditor_CopyFailed", e.Message));
                    }
                }));
        }

        private static string GetUniqueLocalName(string requested)
        {
            var baseName = string.IsNullOrWhiteSpace(requested) ? "Workshop Skin Local" : requested;
            var candidate = baseName;
            var suffix = 2;
            while (Directory.Exists(Path.Combine(ConfigManager.SkinDirectory.Value, candidate)) ||
                   File.Exists(Path.Combine(ConfigManager.SkinDirectory.Value, candidate)))
                candidate = baseName + " " + suffix++;
            return candidate;
        }

        private static string SanitizeName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string((value ?? string.Empty).Where(x => !invalid.Contains(x)).ToArray()).Trim();
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.EnumerateFiles(source))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), false);

            foreach (var directory in Directory.EnumerateDirectories(source))
            {
                var info = new DirectoryInfo(directory);
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
                    continue;
                CopyDirectory(directory, Path.Combine(destination, info.Name));
            }
        }

        private static void ShowErrors(IReadOnlyList<string> errors)
        {
            var message = errors == null || errors.Count == 0
                ? LocalizationManager.Get("SkinEditor_SaveFailed")
                : string.Join(" ", errors.Take(3));
            NotificationManager.Show(NotificationLevel.Error, message);
        }
    }
}
