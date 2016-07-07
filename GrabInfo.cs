using VRage.Game.ModAPI;
using VRageMath;
using VRage.ModAPI;
using Sandbox.ModAPI;

namespace Jimmacle.Manipulator
{
    public class GrabInfo : IGrabInfo
    {
        public Vector3D LocalPosition { get; private set; } = Vector3D.Zero;


        public Vector3D WorldPosition
        {
            get
            {
                return Vector3D.Transform(LocalPosition, PhysicsEntity.WorldMatrix);
            }
            set
            {
                LocalPosition = Vector3D.Transform(value, PhysicsEntity.WorldMatrixNormalizedInv);
            }
        }

        public void Update()
        {
            //This is only necessary in GrabInfoBlock.
        }

        public bool IsValid
        {
            get
            {
                return PhysicsEntity != null && !PhysicsEntity.Closed;
            }
        }

        public IMyEntity PhysicsEntity { get; private set; }
        public object GrabbedObject { get { return PhysicsEntity; } }

        public GrabInfo(IMyEntity entity, Vector3D worldHitPos)
        {
            PhysicsEntity = entity.GetTopMostParent();
            WorldPosition = worldHitPos;
        }

        public GrabInfo(IHitInfo info)
        {
            MyAPIGateway.Utilities.ShowMessage("link", info.HitEntity.GetType().ToString());
            PhysicsEntity = info.HitEntity;
            WorldPosition = info.Position;
        }
    }
}
