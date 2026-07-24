using System;
using System.Collections.Generic;
using System.Linq;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Scheduling;
using Quaver.Shared.Screens.Gameplay;
using Wobble.Bindables;
using Wobble.Input;

namespace Quaver.Shared.Input.Global;

public class GlobalInputHandler : IInputHandler<GlobalKeybindActions>
{

    /// <summary>
    /// </summary>
    private const string VISUAL_OFFSET_NOTIFICATION_KEY = "gameplay-visual-offset";

    /// <summary>
    /// </summary>
    private const string LOCAL_MAP_OFFSET_NOTIFICATION_KEY = "gameplay-local-map-offset";

    private readonly Dictionary<GlobalKeybindActions, Bindable<bool>> _invertScrollingActions = [];

    private static readonly HashSet<GlobalKeybindActions> HoldRepeatActions = [];

    private static readonly HashSet<GlobalKeybindActions> HoldAndReleaseActionsSet =
    [
        GlobalKeybindActions.GameplayPause,
        GlobalKeybindActions.GameplayRetry
    ];

    /// <inheritdoc />
    public bool? InvertedScrolling(GlobalKeybindActions action)
    {
        if (!_invertScrollingActions.TryGetValue(action, out var bindable))
            return null;
        return bindable.Value;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void HandleAction(GlobalKeybindActions action, bool isKeyPress = true,
        bool isRelease = false)
    {
        var scopes = GlobalInputManager.ScopeTokens.AsEnumerable().Reverse().ToList();
        foreach (var scope in scopes)
        {
            var shouldBreak = false;
            switch (scope.Handle(action, isKeyPress, isRelease))
            {
                case GlobalInputHandleResult.Consumed:
                    shouldBreak = true;
                    break;
                case GlobalInputHandleResult.Pass:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (shouldBreak)
                break;
        }
    }

    /// <inheritdoc />
    public void HandleCustomActions(GenericKeyState keyState, GenericKeyState previousKeyState,
        HashSet<Keybind> uniqueKeyPresses)
    {
    }

    /// <inheritdoc />
    public void HandleActionCombination(Dictionary<Keybind, HashSet<GlobalKeybindActions>> actions,
        HashSet<Keybind> uniqueKeyPresses)
    {
    }

    /// <inheritdoc />
    public bool IsHoldRepeat(GlobalKeybindActions action) => HoldRepeatActions.Contains(action);

    /// <inheritdoc />
    public bool IsHoldAndRelease(GlobalKeybindActions action) =>
        HoldAndReleaseActionsSet.Contains(action);

    /// <inheritdoc />
    public IEnumerable<GlobalKeybindActions> HoldAndReleaseActions => HoldAndReleaseActionsSet;

    /// <inheritdoc />
    public bool IsKeybindBlocked(GenericKey key) => false;

    /// <inheritdoc />
    public bool InFocus => false;

    public static void HandleOffsetAction(GlobalKeybindActions action)
    {
        var change = action.HasFlag(GlobalKeybindActions.Small) ? 1 : 5;
        if (action.HasFlag(GlobalKeybindActions.Reverse))
            change *= -1;
                
        if ((action & GlobalKeybindActions.BaseActionMask) == GlobalKeybindActions.ResetOffset)
        {
            if (action.HasFlag(GlobalKeybindActions.Visual))
            {
                ConfigManager.VisualOffset.Value = 0;
                NotificationManager.ShowOrUpdate(VISUAL_OFFSET_NOTIFICATION_KEY, NotificationLevel.Success,
                    $"Visual offset has been reset to: {ConfigManager.VisualOffset.Value} ms", null, true);
            }
            else
            {
                MapManager.Selected.Value.LocalOffset = 0;
                NotificationManager.ShowOrUpdate(LOCAL_MAP_OFFSET_NOTIFICATION_KEY, NotificationLevel.Success,
                    $"Local map audio offset has been reset to: {MapManager.Selected.Value.LocalOffset} ms", null, true);

                ThreadScheduler.Run(() => MapDatabaseCache.UpdateMap(MapManager.Selected.Value));
            }
        }

        if ((action & GlobalKeybindActions.BaseActionMask) == GlobalKeybindActions.IncreaseOffset)
        {
            if (action.HasFlag(GlobalKeybindActions.Visual))
            {
                ConfigManager.VisualOffset.Value += change;
                NotificationManager.ShowOrUpdate(VISUAL_OFFSET_NOTIFICATION_KEY, NotificationLevel.Success,
                    $"Visual offset has been changed to: {ConfigManager.VisualOffset.Value} ms", null, true);
            }
            else
            {
                MapManager.Selected.Value.LocalOffset += change;
                NotificationManager.ShowOrUpdate(LOCAL_MAP_OFFSET_NOTIFICATION_KEY, NotificationLevel.Success,
                    $"Local map audio offset is now: {MapManager.Selected.Value.LocalOffset} ms", null, true);

                ThreadScheduler.Run(() => MapDatabaseCache.UpdateMap(MapManager.Selected.Value));
            }
        }
    }
}
