using VRage.Game.ModAPI;
using VRageMath;
using VRage.ModAPI;
using Sandbox.ModAPI;

namespace Jimmacle.Manipulator
{
    public class GrabInfo : IGrabInfo
    {
        private bool dirty;
        public Vector3D LocalPosition { get; } = Vector3D.Zero;

        private Vector3D worldPosition;
        public Vector3D WorldPosition
        {
            get
            {
                if (dirty)
                {
                    RecalculateWorldPosition();
                }
                return worldPosition;
            }
        }

        public bool IsValid => PhysicsEntity?.Physics != null && !PhysicsEntity.Closed;

        public IMyEntity PhysicsEntity { get; }
        public object GrabbedObject => PhysicsEntity;

        public GrabInfo(IMyEntity entity, Vector3D worldHitPos)
        {
            PhysicsEntity = entity.GetTopMostParent();
            LocalPosition = Vector3D.Transform(worldHitPos, PhysicsEntity.WorldMatrix);
            worldPosition = worldHitPos;

            PhysicsEntity.PositionComp.OnPositionChanged += x => dirty = true;
        }

        private void RecalculateWorldPosition()
        {
            worldPosition = Vector3D.Transform(LocalPosition, PhysicsEntity.WorldMatrixNormalizedInv);
            dirty = false;
        }

        public GrabInfo(IHitInfo info)
        {
            PhysicsEntity = info.HitEntity;
            worldPosition = info.Position;
        }
    }
}
