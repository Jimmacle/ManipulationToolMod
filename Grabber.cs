﻿using Sandbox.ModAPI;
using System;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Jimmacle.Manipulator
{
    public class Grabber
    {
        private MatrixD iCamOrient;
        private IMyEntity character;
        private IMyEntity grabbedEntity;
        private Vector3D grabVector;
        private Vector3D grabPos;
        private Vector3D localGrabPos;
        private float dampAng;

        public IMyPlayer Player { get; }

        private MatrixD HeadTransform
        {
            get
            {
                var controller = character as VRage.Game.ModAPI.Interfaces.IMyControllableEntity;
                return controller?.GetHeadMatrix(true) ?? MatrixD.Identity;
            }
        }

        public Grabber(IMyPlayer player)
        {
            Player = player;
        }

        /// <summary>
        /// Try to grab a new entity in front of the camera.
        /// </summary>
        /// <param name="range">Range in front of the camera.</param>
        public bool TryGrabNew(double range)
        {
            character = Player?.Controller?.ControlledEntity?.Entity;
            var builder = character?.GetObjectBuilder() as MyObjectBuilder_Character;
            if (builder == null || builder.HandWeapon != null)
            {
                return false;
            }

            var rayStart = HeadTransform.Translation + HeadTransform.Forward;
            var rayEnd = rayStart + (HeadTransform.Forward * range);

            IHitInfo hitInfo;

            if (!MyAPIGateway.Physics.CastRay(rayStart, rayEnd, out hitInfo))
            {
                return false;
            }

            if (hitInfo.HitEntity is IMyCharacter)
            {
                if (Extensions.HasGui())
                {
                    MyAPIGateway.Utilities.ShowNotification("Can't grab other players.", 1000, MyFontEnum.Red);
                }
                return false;
            }

            if (hitInfo.HitEntity.Physics?.IsStatic ?? true)
            {
                if (Extensions.HasGui())
                {
                    MyAPIGateway.Utilities.ShowNotification("Can't grab a static object.", 1000, MyFontEnum.Red);
                }
                return false;
            }

            grabPos = hitInfo.Position;
            grabbedEntity = hitInfo.HitEntity;
            grabVector = hitInfo.Position - HeadTransform.Translation;
            iCamOrient = HeadTransform.GetOrientation();

            dampAng = grabbedEntity.Physics.AngularDamping;
            grabbedEntity.Physics.AngularDamping = 0.5f;

            localGrabPos = Vector3D.Transform(grabPos, grabbedEntity.WorldMatrixNormalizedInv);

            return true;
        }

        /// <summary>
        /// Move the grabbed entity to the current target position.
        /// </summary>
        public void MoveGrabbed()
        {
            if (!(grabbedEntity?.Closed ?? true) && !(character?.Closed ?? true))
            {
                //Calculate the rotation amount and new translation for the grabbed entity.
                MatrixD deltaOrientation = MatrixD.Transpose(iCamOrient) * HeadTransform.GetOrientation();
                Vector3D targetTranslation = HeadTransform.Translation + Vector3D.Transform(grabVector, deltaOrientation);

                var worldGrabPos = Vector3D.Transform(localGrabPos, grabbedEntity.WorldMatrix);

                //Calculate target force direction and magnitude
                var posError = targetTranslation - worldGrabPos;
                var distanceRatio = posError.LengthSquared() / 625f;

                if (distanceRatio > 1f)
                {
                    ReleaseGrabbed();
                    return;
                }

                var velError = character.Physics.LinearVelocity - grabbedEntity.Physics.LinearVelocity;

                var totalError = posError + velError;
                var forceMag = (float)Math.Pow(totalError.Length() * grabbedEntity.Physics.Mass, Settings.Instance.ForceMult);

                if (forceMag > 100000)
                {
                    forceMag = 100000;
                }

                var force = Vector3D.Normalize(posError) * forceMag;

                grabbedEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, force, worldGrabPos, null);

                //Graphics stuff
                if (!Extensions.HasGui())
                {
                    var lineColor = Vector4.Lerp(Color.LimeGreen, Color.Red, (float)distanceRatio);
                    lineColor.W = Settings.Instance.Opacity;
                    var grabMatrix = MatrixD.CreateFromTransformScale(Quaternion.Identity, worldGrabPos, Vector3D.One);
                    var targetMatrix = MatrixD.CreateFromTransformScale(Quaternion.Identity, targetTranslation, Vector3D.One);
                    var sphereColor = (Color)lineColor;
                    MySimpleObjectDraw.DrawTransparentSphere(ref grabMatrix, 0.2f, ref sphereColor, MySimpleObjectRasterizer.Solid, 20);
                    MySimpleObjectDraw.DrawTransparentSphere(ref targetMatrix, 0.2f, ref sphereColor, MySimpleObjectRasterizer.Solid, 20);
                    MySimpleObjectDraw.DrawLine(targetTranslation, worldGrabPos, "SquareFullColor", ref lineColor, 0.2f * (forceMag / 100000f));
                }
            }
        }

        /// <summary>
        /// Let go of the entity.
        /// </summary>
        public void ReleaseGrabbed()
        {
            if (grabbedEntity?.Physics?.AngularDamping != null)
            {
                grabbedEntity.Physics.AngularDamping = dampAng;
            }
            grabbedEntity = null;
            iCamOrient = MatrixD.Identity;
        }
    }
}
