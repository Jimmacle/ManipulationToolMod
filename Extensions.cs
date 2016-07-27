using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Gui;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using SpaceEngineers.Game.ModAPI;

namespace Jimmacle.Manipulator
{
    public static class Extensions
    {
        public static bool HasGui()
        {
            return MyAPIGateway.Session?.Player != null;
        }

        public static bool IsInMenu()
        {
            return MyGuiScreenTerminal.GetCurrentScreen() == MyTerminalPageEnum.None &&
                   MyGuiScreenGamePlay.ActiveGameplayScreen == null;
        }

        public static bool RaycastDetailed(Vector3D start, Vector3D end, out IGrabInfo info)
        {
            IHitInfo hit;
            if (MyAPIGateway.Physics.CastRay(start, end, out hit))
            {
                if (hit.HitEntity is IMyCubeGrid || hit.HitEntity is IMySpaceBall)
                {
                    var grid = (IMyCubeGrid)hit.HitEntity.GetTopMostParent();
                    var hitPos = grid.RayCastBlocks(start, end);
                    if (hitPos.HasValue)
                    {
                        var block = grid.GetCubeBlock(hitPos.Value);
                        if (block != null)
                        {
                            info = new GrabInfoSlimBlock(block, hit.Position);
                            return true;
                        }
                    }
                }
                else
                {
                    info = new GrabInfo(hit);
                    return true;
                }
            }

            info = null;
            return false;
        }
    }
}
