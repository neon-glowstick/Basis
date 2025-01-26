using System;
using System.Linq;
using Basis.Scripts.Device_Management.Devices;
using Basis.Scripts.TransformBinders.BoneControl;

public abstract partial class InteractableObject
{
    public struct InputSources {
        public BasisInputWrapper desktopCenterEye, leftHand, rightHand;

        private BasisInputWrapper[] primary; // scratch array to avoid alloc on ToArray
        public BasisInputWrapper[] extras;
        
        public InputSources(uint extrasCount)
        {
            desktopCenterEye = default;
            leftHand = default;
            rightHand = default;
            extras = new BasisInputWrapper[extrasCount];
            primary = new BasisInputWrapper[3];
        }

        private static bool IsInfluencing(InteractInputState state)
        {
            return state == InteractInputState.Hovering || state == InteractInputState.Interacting;
        }

        public readonly bool AnyInteracting(bool skipExtras = true)
        {
            bool influencing = IsInfluencing(desktopCenterEye.State) ||
                            IsInfluencing(leftHand.State) ||
                            IsInfluencing(rightHand.State);
            if (!skipExtras)
            {
                influencing |= extras.Any(x => IsInfluencing(x.State));
            }
            return influencing;
        }

        public readonly BasisInputWrapper? FindExcludeExtras(BasisInput input)
        {
            if (input == null)
                return null;
            // done this way to avoid the array GC alloc
            var inUDI = input.UniqueDeviceIdentifier;
            if (desktopCenterEye.State != InteractInputState.NotAdded && desktopCenterEye.Source.UniqueDeviceIdentifier == inUDI)
            {
                return desktopCenterEye;
            }
            else if (leftHand.State != InteractInputState.NotAdded && leftHand.Source.UniqueDeviceIdentifier == inUDI)
            {
                return leftHand;
            }
            else if (rightHand.State != InteractInputState.NotAdded && rightHand.Source.UniqueDeviceIdentifier == inUDI)
            {
                return rightHand;
            }

            return null;
        }


        public readonly bool Contains(BasisInput input, bool skipExtras = true)
        {
            string inUDI = input != null ? input.UniqueDeviceIdentifier : "";

            bool contains = leftHand.State != InteractInputState.NotAdded && leftHand.Source.UniqueDeviceIdentifier == inUDI || 
                            rightHand.State != InteractInputState.NotAdded && rightHand.Source.UniqueDeviceIdentifier == inUDI || 
                            desktopCenterEye.State != InteractInputState.NotAdded && desktopCenterEye.Source.UniqueDeviceIdentifier == inUDI;

            if (!skipExtras)
            {
                contains |= extras.Any(x => x.State != InteractInputState.NotAdded && x.Source.UniqueDeviceIdentifier == inUDI);
            }
            return contains;
        }

        public readonly BasisInputWrapper[] ToArray()
        {
            primary[0] = desktopCenterEye;
            primary[1] = leftHand;
            primary[2] = rightHand;

            if (extras.Length != 0)
                return primary.Concat(extras).ToArray();
            return primary;
        }

        public bool AddInput(BasisInput input, InteractInputState state)
        {
            var created = BasisInputWrapper.TryNewTracking(input, state, out BasisInputWrapper wrapper);
            if (!created)
                return false;

            switch (wrapper.Role)
            {    
                case BasisBoneTrackedRole.CenterEye:
                    desktopCenterEye = wrapper;
                    return true;
                case BasisBoneTrackedRole.LeftHand:
                    leftHand = wrapper;
                    return true;
                case BasisBoneTrackedRole.RightHand:
                    rightHand = wrapper;
                    return true;
                default:
                    return false;
            }
        }
        public readonly bool TryGetByRole(BasisBoneTrackedRole role, out BasisInputWrapper input)
        {
            input = default;
            switch (role)
            {
                case BasisBoneTrackedRole.CenterEye:
                    input = desktopCenterEye;
                    return true;
                case BasisBoneTrackedRole.LeftHand:
                    input = leftHand;
                    return true;
                case BasisBoneTrackedRole.RightHand:
                    input = rightHand;
                    return true;
                default:
                    return false;
            }
        }

        public bool ChangeStateByRole(BasisBoneTrackedRole role, InteractInputState newState)
        {
            switch (role)
            {
                case BasisBoneTrackedRole.CenterEye:
                    desktopCenterEye.State = newState;
                    return true;
                case BasisBoneTrackedRole.LeftHand:
                    leftHand.State = newState;
                    return true;
                case BasisBoneTrackedRole.RightHand:
                    rightHand.State = newState;
                    return true;
                default:
                    return false;
            }
        }

        public bool RemoveByRole(BasisBoneTrackedRole role)
        {
            switch (role)
            {
                case BasisBoneTrackedRole.CenterEye:
                    desktopCenterEye = default;
                    return true;
                case BasisBoneTrackedRole.LeftHand:
                    leftHand = default;
                    return true;
                case BasisBoneTrackedRole.RightHand:
                    rightHand = default;
                    return true;
                default:
                    return false;
            }
        }
    }
}
