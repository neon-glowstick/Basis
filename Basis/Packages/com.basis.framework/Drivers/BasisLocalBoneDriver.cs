using Basis.Scripts.Device_Management;
using Basis.Scripts.TransformBinders.BoneControl;
using UnityEngine;

namespace Basis.Scripts.Drivers
{
    public class BasisLocalBoneDriver : BaseBoneDriver
    {
        public void Start()
        {
            BasisDeviceManagement.Instance.OnBootModeChanged += OnBootModeChanged;
            OnBootModeChanged(BasisDeviceManagement.Instance.CurrentMode);
        }

        private void OnBootModeChanged(string mode)
        {
            for (int Index = 0; Index < ControlsLength; Index++)
            {
                BasisBoneControl Control = Controls[Index];
                BasisBoneTrackedRole role = trackedRoles[Index];
                if (BasisBoneTrackedRoleCommonCheck.CheckIfLeftHand(role) || BasisBoneTrackedRoleCommonCheck.CheckIfRightHand(role))
                {
                    Control.Cullable = mode == BasisDeviceManagement.Desktop;
                }
                else
                {
                    Control.Cullable = false;
                }
            }
        }
        public float DeltaTime;
        public void Simulate()
        {
            DeltaTime = Time.deltaTime;
            if (float.IsNaN(DeltaTime))
            {
                return;
            }

            SimulateAndApply(DeltaTime);
        }
        public void OnDestroy()
        {
            BasisDeviceManagement.Instance.OnBootModeChanged -= OnBootModeChanged;
        }
    }
}
