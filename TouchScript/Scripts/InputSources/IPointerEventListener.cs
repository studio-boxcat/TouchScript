using JetBrains.Annotations;
using TouchScript.Pointers;

namespace TouchScript.InputSources
{
    public interface IPointerEventListener
    {
        void AddPointer([NotNull] Pointer pointer);
        void UpdatePointer([NotNull] Pointer pointer);
        void PressPointer([NotNull] Pointer pointer);
        void ReleasePointer([NotNull] Pointer pointer);
        void RemovePointer([NotNull] Pointer pointer);
        void CancelPointer([NotNull] Pointer pointer);
    }
}