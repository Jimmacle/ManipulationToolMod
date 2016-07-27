using System;
using VRage.Game;
using VRage.Game.Components;
using VRageMath;

namespace Jimmacle.Manipulator
{
    public class Spring : ILink
    {
        private int direction;
        private float breakForce;
        private float springConstant;
        private float targetLength;
        private IGrabInfo leftHit;
        private IGrabInfo rightHit;

        public bool IsBroken { get; private set; }

        public Spring(IGrabInfo left, IGrabInfo right, float? targetLength = null, float springConstant = 0, float breakForce = float.MaxValue)
        {
            leftHit = left;
            rightHit = right;

            if (targetLength.HasValue)
            {
                this.targetLength = targetLength.Value;
            }
            else
            {
                this.targetLength = (float)(left.WorldPosition - right.WorldPosition).Length();
            }

            this.springConstant = springConstant;
            this.breakForce = breakForce;

            IsBroken = false;
        }

        public void Break()
        {
            IsBroken = true;
        }

        public void Update()
        {
            if (!leftHit.IsValid || !rightHit.IsValid || IsBroken)
                return;

            if (leftHit.GrabbedObject == rightHit.GrabbedObject)
            {
                Break();
                return;
            }

            var displacement = leftHit.WorldPosition - rightHit.WorldPosition;
            var forceMag = (displacement.Length() - targetLength) * springConstant;

            if (forceMag > breakForce)
            {
                Break();
                return;
            }

            if (leftHit.PhysicsEntity != rightHit.PhysicsEntity)
            {
                var force = Vector3D.Normalize(displacement) * forceMag;

                if (direction <= 0)
                {
                    leftHit.PhysicsEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, -force, leftHit.WorldPosition, null);
                }
                if (direction >= 0)
                {
                    rightHit.PhysicsEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, force, rightHit.WorldPosition, null);
                }
            }

            if (Extensions.HasGui())
            {
                var lineColor = Vector4.Lerp(Color.LimeGreen, Color.Red, (float)(forceMag / breakForce));
                lineColor.W = Settings.Instance.Opacity;
                var leftMatrix = MatrixD.CreateFromTransformScale(Quaternion.Identity, leftHit.WorldPosition, Vector3D.One);
                var rightMatrix = MatrixD.CreateFromTransformScale(Quaternion.Identity, rightHit.WorldPosition, Vector3D.One);
                var sphereColor = (Color)lineColor;
                MySimpleObjectDraw.DrawTransparentSphere(ref leftMatrix, 0.2f, ref sphereColor, MySimpleObjectRasterizer.Solid, 20);
                MySimpleObjectDraw.DrawTransparentSphere(ref rightMatrix, 0.2f, ref sphereColor, MySimpleObjectRasterizer.Solid, 20);
                MySimpleObjectDraw.DrawLine(leftHit.WorldPosition, rightHit.WorldPosition, "SquareFullColor", ref lineColor, 0.2f);
            }
        }
    }
}