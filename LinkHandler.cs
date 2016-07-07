using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Jimmacle.Manipulator
{
    public static class LinkHandler
    {
        public static List<ILink> Links = new List<ILink>();

        private static IGrabInfo lastHit;

        public static void Update()
        {
            foreach (var link in Links)
            {
                link.Update();
            }
        }

        public static bool TryMake()
        {
            var playerCam = MyAPIGatewayShortcuts.GetMainCamera.Invoke();

            var startPos = playerCam.Position + playerCam.WorldMatrix.Forward;
            var endPos = playerCam.Position + playerCam.WorldMatrix.Forward * 5f;

            IGrabInfo info;
            Extensions.RaycastDetailed(startPos, endPos, out info);

            if (info == null)
            {
                MyAPIGateway.Utilities.ShowMessage("", "no hit");
                return false;
            }

            if (lastHit == null)
            {
                MyAPIGateway.Utilities.ShowMessage("", "first hit");
                lastHit = info;
                return false;
            }

            MyAPIGateway.Utilities.ShowMessage("", "making link");
            Links.Add(new Spring(lastHit, info, null, 1000, 100000));
            lastHit = null;
            return true;
        }
    }
}
