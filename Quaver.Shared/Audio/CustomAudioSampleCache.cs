/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * Copyright (c) Swan & The Quaver Team <support@quavergame.com>.
*/

using System.Collections.Generic;
using System.IO;
using Quaver.API.Maps;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Wobble.Audio.Samples;

namespace Quaver.Shared.Audio
{
    public static class CustomAudioSampleCache
    {
        /// <summary>
        ///     Identity of the map state where the sound samples are from.
        /// </summary>
        private static string CacheKey { get; set; }

        /// <summary>
        ///     The cached audio samples.
        /// </summary>
        private static List<GameplayAudioSample> Samples { get; set; } = new List<GameplayAudioSample>();

        /// <summary>
        ///     Currently playing channels.
        /// </summary>
        private static List<AudioSampleChannel> Channels { get; set; } = new List<AudioSampleChannel>();

        /// <summary>
        ///     Loads audio samples for the specified map into the cache.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="md5"></param>
        public static void LoadSamples(Map map, string md5)
        {
            LoadSamples(map, map?.Qua, string.IsNullOrEmpty(md5) ? map?.Md5Checksum : md5);
        }

        /// <summary>
        ///     Loads audio samples from a specific map state. The editor uses this overload for its working copy.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="qua"></param>
        /// <param name="cacheKey"></param>
        /// <param name="force"></param>
        public static void LoadSamples(Map map, Qua qua, string cacheKey, bool force = false)
        {
            if (map == null || qua == null)
                return;

            if (string.IsNullOrEmpty(cacheKey))
                cacheKey = map.Md5Checksum;

            // Always clean up the left-over channels.
            StopAll();

            // If the map state is the same, no need to re-load the samples.
            if (!force && CacheKey != null && CacheKey == cacheKey)
                return;

            CacheKey = cacheKey;

            foreach (var sample in Samples)
                sample.Dispose();

            Samples = new List<GameplayAudioSample>();
            foreach (var info in qua.CustomAudioSamples)
            {
                // If the path is missing an extension or the file doesn't exist, we need to try some other extensions
                // for compatibility with osu!.
                var pathWithoutExt = info.Path;
                var extensions = new List<string> { "wav", "ogg", "mp3" };

                var dotIndex = info.Path.LastIndexOf('.');
                if (dotIndex > 0)
                {
                    pathWithoutExt = info.Path.Substring(0, dotIndex);
                    extensions.Insert(0, info.Path.Substring(dotIndex + 1));
                }
                else
                {
                    extensions.Insert(0, "");
                }

                var found = false;
                foreach (var ext in extensions)
                {
                    try
                    {
                        Samples.Add(new GameplayAudioSample(new AudioSample(MapManager.GetCustomAudioSamplePath(
                            map, pathWithoutExt + '.' + ext)), info.UnaffectedByRate));
                        found = true;
                        break;
                    }
                    catch (FileNotFoundException)
                    {
                        // Ignored.
                    }
                }

                // If none of the filenames worked, create a silent sample.
                if (!found)
                {
                    // Applying rate to an empty sample results in a crash.
                    Samples.Add(new GameplayAudioSample(new AudioSample(), true));
                }
            }
        }

        /// <summary>
        ///     Plays the sample for the given index.
        /// </summary>
        /// <param name="index">Index of a sample to play, same as into the Qua.CustomAudioSamples array.</param>
        /// <param name="volume">Volume between 0 and 100.</param>
        public static void Play(int index, int volume = 100)
        {
            if (index < 0 || index >= Samples.Count)
                return;

            var sample = Samples[index];
            var channel = sample.Sample.CreateChannel(
                ConfigManager.Pitched.Value, sample.UnaffectedByRate ? 1f : AudioEngine.Track.Rate);
            channel.Volume *= volume / 100f;
            channel.Play();

            Channels.Add(channel);
        }

        /// <summary>
        ///     Pauses all playing samples.
        /// </summary>
        public static void PauseAll()
        {
            for (var i = Channels.Count - 1; i >= 0; i--)
            {
                var channel = Channels[i];

                channel.Pause();

                // Remove channels that have finished playing.
                if (channel.IsStopped)
                    Channels.RemoveAt(i);
            }
        }

        /// <summary>
        ///     Resumes all samples.
        /// </summary>
        public static void ResumeAll()
        {
            foreach (var channel in Channels)
                channel.Play();
        }

        /// <summary>
        ///     Stops and frees all playing samples without the ability to resume them.
        /// </summary>
        public static void StopAll()
        {
            Channels.ForEach(x => x.Stop());
            Channels.Clear();
        }

        public static void Dispose()
        {
            StopAll();
            Samples.ForEach(x => x.Dispose());
            Samples.Clear();
            CacheKey = null;
        }
    }
}
