using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Jimmacle.Manipulator
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallGatlingGun), "Force")]
    public class ForceGun : MyGameLogicComponent
    {
        private MyObjectBuilder_EntityBase ob;
        private IMySmallGatlingGun gun;
        private bool hitDebounce = false;
        private ILink link;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            ob = objectBuilder;
            gun = Entity as IMySmallGatlingGun;
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return copy ? ob.Clone() as MyObjectBuilder_EntityBase : ob;
        }

        public override void UpdateBeforeSimulation()
        {
            if (gun.IsShooting && !hitDebounce)
            {
                if (link == null)
                {
                    var source = new GrabInfoSlimBlock(gun.CubeGrid.GetCubeBlock(gun.Position), gun.GetPosition());

                    var startPos = gun.GetPosition() + gun.WorldMatrix.Forward;
                    var endPos = gun.GetPosition() + gun.WorldMatrix.Forward * 10f;

                    IGrabInfo info;
                    if (Extensions.RaycastDetailed(startPos, endPos, out info))
                    {
                        link = new Spring(source, info);
                        LinkHandler.Links.Add(link);
                    }
                }
                else
                {
                    LinkHandler.Links.Remove(link);
                    link = null;
                }

                hitDebounce = true;
            }
            else
            {
                hitDebounce = false;
            }
        }
    }
}
