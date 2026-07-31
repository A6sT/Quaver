using System;
using Quaver.API.Maps.Structures;

namespace Quaver.Shared.Screens.Edit.Actions.Keysounds
{
    public class EditorCustomAudioSamplesChangedEventArgs : EventArgs
    {
        public CustomAudioSampleInfo Sample { get; }

        public int Index { get; }

        public EditorCustomAudioSamplesChangedEventArgs(CustomAudioSampleInfo sample, int index)
        {
            Sample = sample;
            Index = index;
        }
    }
}
