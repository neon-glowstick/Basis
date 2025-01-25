using Basis.Scripts.Device_Management.Devices;

public abstract partial class InteractableObject
{
    public struct BasisInputWrapper
    {
        public BasisInputWrapper(BasisInput source, bool isInteracting)
        {
            Source = source;
            IsInteracting = isInteracting;
        }

        public BasisInput Source { get; set; }

        /// <summary>
        /// - true: source interacting with object
        /// - false: source hovering
        /// If not either, this source should not be in the list!
        /// </summary>
        public bool IsInteracting { get; set; }
    }
}
