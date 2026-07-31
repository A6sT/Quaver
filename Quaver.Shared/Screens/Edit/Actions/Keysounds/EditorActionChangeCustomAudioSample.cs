using MoonSharp.Interpreter;
using Quaver.API.Maps.Structures;

namespace Quaver.Shared.Screens.Edit.Actions.Keysounds
{
    [MoonSharpUserData]
    public class EditorActionChangeCustomAudioSample : IEditorAction
    {
        public EditorActionType Type { get; } = EditorActionType.ChangeCustomAudioSample;

        private EditorActionManager ActionManager { get; }

        public CustomAudioSampleInfo Sample { get; }

        public int Index { get; }

        public bool UnaffectedByRate { get; }

        private bool PreviousUnaffectedByRate { get; }

        public EditorActionChangeCustomAudioSample(EditorActionManager actionManager, CustomAudioSampleInfo sample,
            int index, bool unaffectedByRate)
        {
            ActionManager = actionManager;
            Sample = sample;
            Index = index;
            UnaffectedByRate = unaffectedByRate;
            PreviousUnaffectedByRate = sample.UnaffectedByRate;
        }

        public void Perform()
        {
            Sample.UnaffectedByRate = UnaffectedByRate;
            TriggerEvent();
        }

        public void Undo()
        {
            Sample.UnaffectedByRate = PreviousUnaffectedByRate;
            TriggerEvent();
        }

        private void TriggerEvent() => ActionManager.TriggerEvent(Type,
            new EditorCustomAudioSamplesChangedEventArgs(Sample, Index));
    }
}
