using System;
using System.Linq;
using Basis.Scripts.Device_Management.Devices;
using Basis.Scripts.TransformBinders.BoneControl;

public abstract partial class InteractableObject
{
    public struct InputSources {
        public BasisInputWrapper desktopCenterEye, leftHand, rightHand;
        public BasisInputWrapper[] extras;
        
        public InputSources(uint extrasCount)
        {
            desktopCenterEye = default;
            leftHand = default;
            rightHand = default;
            extras = new BasisInputWrapper[(int)extrasCount];
        }

        public readonly bool AnyInteracting(bool skipExtras = true)
        {
            bool interacting = leftHand.Source != null && leftHand.IsInteracting || 
                            rightHand.Source != null && rightHand.IsInteracting || 
                            desktopCenterEye.Source != null && desktopCenterEye.IsInteracting;
            if (!skipExtras)
            {
                interacting |= extras.Any(x => x.Source != null && x.IsInteracting);
            }
            return interacting;
        }

        public readonly BasisInputWrapper? Find(BasisInput input)
        {
            if (input == null)
                return null;
            string inUDI = input.UniqueDeviceIdentifier;
            var found = Array.Find(ToArray(), x => x.Source != null && x.Source.UniqueDeviceIdentifier == inUDI);
            // not found
            if (found.Source == null)
                return null;
            return found;
        }
        public readonly bool Contains(BasisInput input, bool skipExtras = true)
        {
            string inUDI = input != null ? input.UniqueDeviceIdentifier : "";

            bool contains = leftHand.Source != null && leftHand.Source.UniqueDeviceIdentifier == inUDI || 
                            rightHand.Source != null && rightHand.Source.UniqueDeviceIdentifier == inUDI || 
                            desktopCenterEye.Source != null && desktopCenterEye.Source.UniqueDeviceIdentifier == inUDI;

            if (!skipExtras)
            {
                contains |= extras.Any(x => x.Source != null && x.Source.UniqueDeviceIdentifier == inUDI);
            }
            return contains;
        }

        public readonly BasisInputWrapper[] ToArray()
        {
            BasisInputWrapper[] primary = new BasisInputWrapper[] {
                desktopCenterEye,
                leftHand,
                rightHand,
            };
            if (extras.Length != 0)
                return primary.Concat(extras).ToArray();
            return primary;
        }

        public bool AddInputByRole(BasisInput input, bool isInteracting)
        {
            if (input.TryGetRole(out BasisBoneTrackedRole role))
            {
                switch (role)
                {
                    case BasisBoneTrackedRole.CenterEye:
                        desktopCenterEye = new BasisInputWrapper(input, isInteracting);
                        return true;
                    case BasisBoneTrackedRole.LeftHand:
                        leftHand = new BasisInputWrapper(input, isInteracting);
                        return true;
                    case BasisBoneTrackedRole.RightHand:
                        rightHand = new BasisInputWrapper(input, isInteracting);
                        return true;
                    default:
                        return false;
                }
            }
            return false;

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
