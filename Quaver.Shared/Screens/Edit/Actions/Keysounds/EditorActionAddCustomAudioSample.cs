using MoonSharp.Interpreter;
using Quaver.API.Maps;
using Quaver.API.Maps.Structures;

namespace Quaver.Shared.Screens.Edit.Actions.Keysounds
{
    [MoonSharpUserData]
    public class EditorActionAddCustomAudioSample : IEditorAction
    {
        public EditorActionType Type { get; } = EditorActionType.AddCustomAudioSample;

        private EditorActionManager ActionManager { get; }

        private Qua WorkingMap { get; }

        public CustomAudioSampleInfo Sample { get; }

        public int Index { get; }

        public EditorActionAddCustomAudioSample(EditorActionManager actionManager, Qua workingMap,
            CustomAudioSampleInfo sample)
        {
            ActionManager = actionManager;
            WorkingMap = workingMap;
            Sample = sample;
            Index = workingMap.CustomAudioSamples.Count;
        }

        public void Perform()
        {
            WorkingMap.CustomAudioSamples.Insert(Index, Sample);
            TriggerEvent();
        }

        public void Undo()
        {
            WorkingMap.CustomAudioSamples.RemoveAt(Index);
            TriggerEvent();
        }

        private void TriggerEvent() => ActionManager.TriggerEvent(Type,
            new EditorCustomAudioSamplesChangedEventArgs(Sample, Index));
    }
}
