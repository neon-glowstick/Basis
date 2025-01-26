using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Device_Management.Devices;
using Basis.Scripts.TransformBinders.BoneControl;

public abstract partial class InteractableObject
{
    public enum InteractInputState
    {
        /// <summary>
        /// Input in scene/initialized, BasisInput & Bone Control is null
        /// </summary>
        NotAdded,
        /// <summary>
        /// Input in scene, not affecting this interactable
        /// </summary>
        Ignored,
        /// <summary>
        /// Input is hovering
        /// </summary>
        Hovering,
        /// <summary>
        /// Input is interacting
        /// </summary>
        Interacting,
    }
    
    public struct BasisInputWrapper
    {
        public static bool TryNewTracking(BasisInput source, InteractInputState state, out BasisInputWrapper wrapper)
        {
            wrapper = default;
            if (source != null && 
                source.TryGetRole(out BasisBoneTrackedRole role) &&
                BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out BasisBoneControl control, role)
            ) {
                wrapper.Source = source;
                wrapper.BoneControl = control;
                wrapper.State = state;
                wrapper.Role = role;
                return true;
            }
            return false;
        }

        public BasisInput Source { get; set; }

        public BasisBoneControl BoneControl { get; set; }
        public BasisBoneTrackedRole Role { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public InteractInputState State { get; set; }
    }
}
