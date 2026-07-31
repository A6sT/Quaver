using System;
using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;
using Quaver.API.Maps.Structures;

namespace Quaver.Shared.Screens.Edit.Actions.Keysounds
{
    [MoonSharpUserData]
    public class EditorActionChangeKeysounds : IEditorAction
    {
        private sealed class HitObjectKeysoundChange
        {
            public HitObjectInfo HitObject { get; }

            public List<KeySoundInfo> Before { get; }

            public List<KeySoundInfo> After { get; }

            public HitObjectKeysoundChange(HitObjectInfo hitObject, List<KeySoundInfo> before,
                List<KeySoundInfo> after)
            {
                HitObject = hitObject;
                Before = before;
                After = after;
            }
        }

        public EditorActionType Type { get; } = EditorActionType.ChangeKeysounds;

        private EditorActionManager ActionManager { get; }

        private List<HitObjectKeysoundChange> Changes { get; }

        public List<HitObjectInfo> HitObjects { get; }

        public EditorKeysoundChangeMode Mode { get; }

        public bool HasChanges => Changes.Any(x =>
            !x.Before.SequenceEqual(x.After, KeySoundInfo.ByValueComparer));

        public EditorActionChangeKeysounds(EditorActionManager actionManager, List<HitObjectInfo> hitObjects,
            EditorKeysoundChangeMode mode, int sample = 0, int volume = 100)
        {
            if (mode != EditorKeysoundChangeMode.Clear && sample < 1)
                throw new ArgumentOutOfRangeException(nameof(sample));

            if (volume < 1 || volume > 100)
                throw new ArgumentOutOfRangeException(nameof(volume));

            ActionManager = actionManager;
            HitObjects = hitObjects;
            Mode = mode;
            Changes = hitObjects.Select(hitObject =>
            {
                var before = Clone(hitObject.KeySounds);
                var after = BuildAfter(before, mode, sample, volume);
                return new HitObjectKeysoundChange(hitObject, before, after);
            }).ToList();
        }

        public void Perform()
        {
            foreach (var change in Changes)
                change.HitObject.KeySounds = Clone(change.After);

            TriggerEvent();
        }

        public void Undo()
        {
            foreach (var change in Changes)
                change.HitObject.KeySounds = Clone(change.Before);

            TriggerEvent();
        }

        private void TriggerEvent() => ActionManager.TriggerEvent(Type,
            new EditorKeysoundsChangedEventArgs(HitObjects, Mode));

        private static List<KeySoundInfo> BuildAfter(List<KeySoundInfo> before, EditorKeysoundChangeMode mode,
            int sample, int volume)
        {
            switch (mode)
            {
                case EditorKeysoundChangeMode.Add:
                {
                    var result = before.Where(x => x.Sample != sample).ToList();
                    result.Add(new KeySoundInfo { Sample = sample, Volume = volume });
                    return result;
                }
                case EditorKeysoundChangeMode.Replace:
                    return new List<KeySoundInfo>
                    {
                        new KeySoundInfo { Sample = sample, Volume = volume }
                    };
                case EditorKeysoundChangeMode.Remove:
                    return before.Where(x => x.Sample != sample).ToList();
                case EditorKeysoundChangeMode.Clear:
                    return new List<KeySoundInfo>();
                case EditorKeysoundChangeMode.ChangeVolume:
                    return before.Select(x => new KeySoundInfo
                    {
                        Sample = x.Sample,
                        Volume = x.Sample == sample ? volume : x.Volume
                    }).ToList();
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        private static List<KeySoundInfo> Clone(IEnumerable<KeySoundInfo> keysounds) => keysounds
            .Select(x => new KeySoundInfo { Sample = x.Sample, Volume = x.Volume })
            .ToList();
    }
}
