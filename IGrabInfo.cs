using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.ModAPI;
using VRageMath;

namespace Jimmacle.Manipulator
{
    public interface IGrabInfo
    {
        object GrabbedObject { get; }
        IMyEntity PhysicsEntity { get; }
        bool IsValid { get; }
        Vector3D WorldPosition { get; }
        void Update();
    }
}
