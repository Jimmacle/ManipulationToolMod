using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Jimmacle.Manipulator
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class ManipulationComponent : MySessionComponentBase
    {
        private const ushort HANDLER_ID = 14268;

        private bool init = false;
        private bool grabbed = false;
        private Grabber grabber;
        private List<Grabber> otherGrabbers = new List<Grabber>();
        private VRage.ModAPI.IMyInput input;

        public override void UpdateBeforeSimulation()
        {
            if (MyAPIGateway.Session != null)
            {
                if (!init)
                {
                    MyAPIGateway.Multiplayer.RegisterMessageHandler(HANDLER_ID, MessageHandler);
                    input = MyAPIGateway.Input;
                    init = true;
                }

                if (input.IsNewPrimaryButtonPressed())
                {
                    if (MyAPIGateway.Session.ControlledObject != null)
                    {
                        if (grabber == null)
                        {
                            grabber = new Grabber(MyAPIGateway.Session.Player);
                        }

                        if (grabber.TryGrabNew(5))
                        {
                            List<byte> packet = new List<byte>();
                            packet.AddRange(BitConverter.GetBytes(true));
                            packet.AddRange(BitConverter.GetBytes(MyAPIGateway.Session.Player.IdentityId));

                            MyAPIGateway.Multiplayer.SendMessageToOthers(HANDLER_ID, packet.ToArray());

                            grabbed = true;
                        }
                    }
                }
                else if (input.IsNewPrimaryButtonReleased())
                {
                    if (grabbed)
                    {
                        if (MyAPIGateway.Session.ControlledObject != null)
                        {
                            List<byte> packet = new List<byte>();
                            packet.AddRange(BitConverter.GetBytes(false));
                            packet.AddRange(BitConverter.GetBytes(MyAPIGateway.Session.Player.IdentityId));

                            MyAPIGateway.Multiplayer.SendMessageToOthers(HANDLER_ID, packet.ToArray());

                            grabber.ReleaseGrabbed();
                            grabbed = false;
                        }
                    }
                }

                UpdateRemoteGrabbers();

                if (grabbed)
                {
                    grabber.MoveGrabbed();
                }
            }
        }

        private void UpdateRemoteGrabbers()
        {
            foreach (var grab in otherGrabbers)
            {
                grab.MoveGrabbed();
            }
        }

        public void MessageHandler(byte[] message)
        {
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Multiplayer.Players.GetPlayers(players);

            bool grabbed = BitConverter.ToBoolean(message, 0);
            long identity = BitConverter.ToInt64(message, 1);

            if (grabbed)
            {
                var player = players.Find(p => p.IdentityId == identity);
                if (player != null)
                {
                    var extGrabber = new Grabber(player);
                    extGrabber.TryGrabNew(5);

                    otherGrabbers.Add(extGrabber);
                }
            }
            else
            {
                var extGrabber = otherGrabbers.Find(g => g.Player.IdentityId == identity);
                if (extGrabber != null)
                {
                    extGrabber.ReleaseGrabbed();
                    otherGrabbers.Remove(extGrabber);
                }
            }
        }
    }

    internal class Grabber
    {
        private IMyPlayer player;

        private IMyEntity grabbedEntity;
        private IMyEntity grabbedParent;
        private MatrixD initialEntOrientation;
        private MatrixD initialCamOrientation;
        private IMyEntity character;

        public double TargetDistance { get; set; }
        public float MaxForce { get; set; }
        public IMyEntity Character { get { return character; } }
        public long LastShoot { get; set; }
        public bool Held { get; set; }
        public IMyPlayer Player { get { return player; } }

        private MatrixD HeadTransform
        {
            get
            {
                return (character as VRage.Game.ModAPI.Interfaces.IMyControllableEntity).GetHeadMatrix(true);
            }
        }

        public Grabber(IMyPlayer player)
        {
            this.player = player;
            TargetDistance = 2.5;
            Held = false;
            LastShoot = 0;
            MaxForce = 500;
        }

        /// <summary>
        /// Try to grab a new entity in front of the camera.
        /// </summary>
        /// <param name="range">Range in front of the camera.</param>
        /// <returns>Success</returns>
        public bool TryGrabNew(double range)
        {
            character = player.Controller.ControlledEntity.Entity;
            var builder = character.GetObjectBuilder() as MyObjectBuilder_Character;
            if (builder == null)
            {
                return false;
            }
            if (builder.HandWeapon != null)
            {
                return false;
            }

            //Calculate the start and end points of the ray.
            Vector3D rayStart = HeadTransform.Translation + HeadTransform.Forward;
            Vector3D rayEnd = rayStart + (HeadTransform.Forward * range);

            IMyEntity grabbedEntity;
            if (!TryRaycastEntity(rayStart, rayEnd, out grabbedEntity))
            {
                //No entity in range.
                return false;
            }

            if (!IsEntityGrabbable(grabbedEntity))
            {
                //Entity is not grabbable.
                return false;
            }

            //Calculate stuff about the grabbed entity.
            this.grabbedEntity = grabbedEntity;
            grabbedParent = grabbedEntity.GetTopMostParent();
            initialEntOrientation = grabbedParent.WorldMatrix.GetOrientation();
            initialCamOrientation = HeadTransform.GetOrientation();
            TargetDistance = (grabbedEntity.WorldMatrix.Translation - HeadTransform.Translation).Length();

            return true;
        }

        /// <summary>
        /// Move the grabbed entity to the current target position.
        /// </summary>
        public void MoveGrabbed()
        {
            if (grabbedEntity != null && !grabbedEntity.Closed && grabbedParent != null && !grabbedParent.Closed && character != null && !character.Closed)
            {
                //Calculate the rotation amount and new translation for the grabbed entity.
                MatrixD deltaOrientation = MatrixD.Transpose(initialCamOrientation) * HeadTransform.GetOrientation();
                Vector3D targetTranslation = HeadTransform.Translation + (HeadTransform.Forward * TargetDistance);

                //Get the translation offset between the grabbed entity and its parent.
                Vector3D translationOffset = grabbedParent.WorldMatrix.Translation - grabbedEntity.WorldMatrix.Translation;
                targetTranslation += translationOffset;

                //Combine the rotation and translation then set the new transform.
                MatrixD newTransform = initialEntOrientation * deltaOrientation;
                newTransform.Translation = targetTranslation;
                grabbedParent.SetWorldMatrix(newTransform);
                grabbedParent.SetPosition(newTransform.Translation);

                grabbedParent.Physics.LinearVelocity = (Character as IMyEntity).Physics.LinearVelocity;
                grabbedParent.Physics.AngularVelocity = (Character as IMyEntity).Physics.AngularVelocity;
            }
        }

        /// <summary>
        /// Move the grabbed entity using forces.
        /// </summary>
        public void DragGrabbed()
        {
            if (grabbedEntity != null && !grabbedEntity.Closed && Character != null)
            {
                Vector3D targetTranslation = HeadTransform.Translation + (HeadTransform.Forward * TargetDistance);
                Vector3D forceDirection = Vector3D.Normalize(targetTranslation - grabbedEntity.WorldMatrix.Translation);

                grabbedParent.Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, forceDirection * MaxForce, grabbedParent.WorldMatrix.Translation, null);
                //(character as MyEntity).Physics.AddForce(MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, -forceDirection * MaxForce, grabbedParent.WorldMatrix.Translation, null);
            }
        }

        /// <summary>
        /// Let go of the entity.
        /// </summary>
        public void ReleaseGrabbed()
        {
            grabbedEntity = null;
            grabbedParent = null;
            initialCamOrientation = MatrixD.Identity;
            initialEntOrientation = MatrixD.Identity;
        }

        /// <summary>
        /// Check if the entity can be grabbed.
        /// </summary>
        /// <param name="ent"></param>
        /// <returns></returns>
        public bool IsEntityGrabbable(IMyEntity ent)
        {
            if (ent == null)
            {
                return false;
            }
            if (ent is IMyVoxelMap)
            {
                return false;
            }
            if (ent is IMyCharacter)
            {
                return false;
            }
            if (ent.GetTopMostParent().Physics == null)
            {
                return false;
            }
            if (ent.GetTopMostParent().Physics.IsStatic)
            {
                return false;
            }
            if (ent.GetTopMostParent().Physics.Mass > 6000f)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Get the closest entity to the ray.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool TryRaycastEntity(Vector3D start, Vector3D end, out IMyEntity entity)
        {
            entity = null;
            LineD ray = new LineD(start, end);

            //Get entities near the ray rather than raycast all entities in the world.
            BoundingSphereD sphere = new BoundingSphereD(start, (end - start).Length());
            List<IMyEntity> entities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);

            //Find the entity with the intersection closest to the start point
            double closestDist = double.MaxValue;
            foreach (var ent in entities)
            {
                var intersect = ent.GetIntersectionWithLineAndBoundingSphere(ref ray, 1f);
                if (intersect != null)
                {
                    double thisDist = ((Vector3)intersect - start).Length();
                    if (entity != null)
                    {
                        if (thisDist < closestDist)
                        {
                            entity = ent;
                            closestDist = thisDist;
                        }
                    }
                    else
                    {
                        entity = ent;
                        closestDist = thisDist;
                    }
                }
            }

            return entity != null;
        }
    }
}
