/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * Copyright (c) Swan & The Quaver Team <support@quavergame.com>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Graphics.Transitions;
using Quaver.Shared.Online;
using Quaver.Shared.Scheduling;
using Quaver.Shared.Screens.V2;
using Wobble;
using Wobble.Graphics.UI.Buttons;
using Wobble.Logging;
using Wobble.Scheduling;
using Wobble.Screens;

namespace Quaver.Shared.Screens
{
    public static class QuaverScreenManager
    {
        private const int TransitionWaitTimeout = 5000;

        /// <summary>
        ///     The previous screen that the user was on.
        /// </summary>
        public static QuaverScreenType LastScreen { get; private set; } = QuaverScreenType.None;

        /// <summary>
        ///     Task that's ran when fetching for leaderboard scores
        /// </summary>
        private static TaskHandler<ScreenChangeRequest, QuaverScreen> ScreenLoadTask { get; set; }

        public static void Initialize()
        {
            QuaverScreenFactory.Initialize(ConfigManager.UseNewScreens.Value);
            ScreenLoadTask = new TaskHandler<ScreenChangeRequest, QuaverScreen>(LoadScreen);
            ScreenLoadTask.OnCompleted += OnCompleted;
        }

        /// <summary>
        ///     Schedules a new screen to be loaded.
        /// </summary>
        /// <param name="newScreen"></param>
        /// <param name="switchImmediately"></param>
        /// <param name="delay"></param>
        /// <param name="transitionMode"></param>
        public static void ScheduleScreenChange(Func<QuaverScreen> newScreen, bool switchImmediately = false,
            int delay = 0, ScreenTransitionMode transitionMode = ScreenTransitionMode.Auto)
        {
            Logger.Important($"Scheduled Screen Change", LogType.Runtime);

            var game = (QuaverGame)GameBase.Game;

            if (game.CurrentScreen != null)
                LastScreen = game.CurrentScreen.Type;

            if (LastScreen == QuaverScreenType.None || switchImmediately)
            {
                var screen = newScreen();
                var retainedElements = GetRetainedElements(game.CurrentScreen, screen);
                Transitioner.SetForegroundElements(GetTransitionForegroundElements(transitionMode,
                    retainedElements));
                ChangeScreen(screen, retainedElements, true);
                return;
            }

            ScreenLoadTask.Run(new ScreenChangeRequest(newScreen, transitionMode), delay);
        }

        /// <summary>
        ///     Loads the new screen in a task.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private static QuaverScreen LoadScreen(ScreenChangeRequest request, CancellationToken token)
        {
            var screen = request.NewScreen();
            token.ThrowIfCancellationRequested();

            Logger.Important($"Screen `{screen.Type}` has been loaded proceeding to switch.", LogType.Runtime);
            return screen;
        }

        /// <summary>
        ///     Called when the screen has finished loading.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="screen"></param>
        private static void OnCompleted(object sender, TaskCompleteEventArgs<ScreenChangeRequest, QuaverScreen> e)
        {
            var game = (QuaverGame)GameBase.Game;
            var retainedElements = GetRetainedElements(game.CurrentScreen, e.Result);
            var foregroundElements = GetTransitionForegroundElements(e.Input.TransitionMode, retainedElements);

            Transitioner.FadeIn(foregroundElements);

            // Wait for the transitioner to fully fade to black.
            var waitStarted = Environment.TickCount64;
            while (Transitioner.IsAnimating &&
                   Environment.TickCount64 - waitStarted < TransitionWaitTimeout)
                Thread.Sleep(16);

            if (Transitioner.IsAnimating)
                Logger.Warning("Screen transition fade-in timed out; forcing the screen switch and recovery fade.",
                    LogType.Runtime);

            // Run this on the next game loop on the main thread.
            game.ScheduleRenderTargetDraw(() => ChangeScreen(e.Result, retainedElements, false));
        }

        private static IReadOnlyCollection<string> GetRetainedElements(QuaverScreen currentScreen,
            QuaverScreen nextScreen)
        {
            if (currentScreen is IPersistentScreen currentPersistent &&
                nextScreen is IPersistentScreen nextPersistent)
                return currentPersistent.PersistentElementKeys
                    .Intersect(nextPersistent.PersistentElementKeys, StringComparer.Ordinal)
                    .ToArray();

            return Array.Empty<string>();
        }

        private static IReadOnlyCollection<string> GetTransitionForegroundElements(
            ScreenTransitionMode transitionMode, IReadOnlyCollection<string> retainedElements) =>
            transitionMode == ScreenTransitionMode.FullScreen
                ? Array.Empty<string>()
                : retainedElements;

        private static void ChangeScreen(QuaverScreen screen, IReadOnlyCollection<string> retainedElements,
            bool switchImmediately)
        {
            var game = (QuaverGame)GameBase.Game;
            try
            {
                ScreenManager.ChangeScreen(screen, retainedElements, switchImmediately);
                game.CurrentScreen = screen;
                game.RefreshFpsCounterVisibility();

                // Update client status on the server.
                var status = screen.GetClientStatus();

                if (status != null)
                    OnlineManager.Client?.UpdateClientStatus(status);

                OtherGameMapDatabaseCache.RunThread();

                Logger.Important($"Screen has been switched to type: `{screen.Type}`", LogType.Runtime);
            }
            finally
            {
                // The screen is fully covered at this point, so starting the fade immediately
                // still preserves the black transition frame. Keeping this in the finally block
                // prevents an exception or a lost follow-up draw callback from orphaning blackness.
                Transitioner.FadeOut();
                Button.IsGloballyClickable = true;
            }
        }

        private sealed class ScreenChangeRequest
        {
            public Func<QuaverScreen> NewScreen { get; }

            public ScreenTransitionMode TransitionMode { get; }

            public ScreenChangeRequest(Func<QuaverScreen> newScreen, ScreenTransitionMode transitionMode)
            {
                NewScreen = newScreen;
                TransitionMode = transitionMode;
            }
        }
    }
}
