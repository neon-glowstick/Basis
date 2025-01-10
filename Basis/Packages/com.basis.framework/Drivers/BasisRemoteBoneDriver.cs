using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.TransformBinders.BoneControl;
using UnityEngine;

namespace Basis.Scripts.Drivers
{
    public class BasisRemoteBoneDriver : BaseBoneDriver
    {
        public BasisRemotePlayer RemotePlayer;
        public Transform HeadAvatar;
        public Transform HipsAvatar;
        public BasisBoneControl Head;
        public BasisBoneControl Hips;
        public bool HasEvent = false;
        public void Initialize()
        {
            FindBone(out Head, BasisBoneTrackedRole.Head);
            FindBone(out Hips, BasisBoneTrackedRole.Hips);
            if (Head != null)
            {
                Head.HasTracked = BasisHasTracked.HasTracker;
            }
            if (Hips != null)
            {
                Hips.HasTracked = BasisHasTracked.HasTracker;
            }
            if (HasEvent == false)
            {
                OnSimulate += CalculateHeadBoneData;
                HasEvent = true;
            }
        }
        public void OnDestroy()
        {
            if (HasEvent)
            {
                OnSimulate -= CalculateHeadBoneData;
                HasEvent = false;
            }
            DeInitalzeGizmos();
        }
        public void CalculateHeadBoneData()
        {
            if (Head.HasBone && HasHead)
            {
                HeadAvatar.GetPositionAndRotation(out Vector3 Position, out Quaternion Rotation);
                Head.IncomingData.position = Position - RemotePlayer.RemoteBoneDriver.transform.position;
                Head.IncomingData.rotation = Rotation;
            }
            if (Hips.HasBone && HasHips)
            {
                HipsAvatar.GetPositionAndRotation(out Vector3 Position, out Quaternion Rotation);
                Hips.IncomingData.position = Position - RemotePlayer.RemoteBoneDriver.transform.position;
                Hips.IncomingData.rotation = Rotation;
            }
        }
        public bool HasHead;
        public bool HasHips;
        public void OnCalibration(BasisRemotePlayer remotePlayer)
        {
            HeadAvatar = RemotePlayer.BasisAvatar.Animator.GetBoneTransform(HumanBodyBones.Head);
            HasHead = HeadAvatar != null;
            HipsAvatar = RemotePlayer.BasisAvatar.Animator.GetBoneTransform(HumanBodyBones.Hips);
            HasHips = HipsAvatar != null;
            this.RemotePlayer = remotePlayer;
        }
    }
}
