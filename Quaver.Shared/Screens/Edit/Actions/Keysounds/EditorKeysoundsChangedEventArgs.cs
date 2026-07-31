using System;
using System.Collections.Generic;
using Quaver.API.Maps.Structures;

namespace Quaver.Shared.Screens.Edit.Actions.Keysounds
{
    public class EditorKeysoundsChangedEventArgs : EventArgs
    {
        public List<HitObjectInfo> HitObjects { get; }

        public EditorKeysoundChangeMode Mode { get; }

        public EditorKeysoundsChangedEventArgs(List<HitObjectInfo> hitObjects, EditorKeysoundChangeMode mode)
        {
            HitObjects = hitObjects;
            Mode = mode;
        }
    }
}
