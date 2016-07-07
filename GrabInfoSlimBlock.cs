using VRage.Game.ModAPI;
using VRageMath;
using VRage.ModAPI;
using VRage.Game.Components;
using Sandbox.ModAPI;

namespace Jimmacle.Manipulator
{
    public class GrabInfoSlimBlock : IGrabInfo
    {
        private bool dirty = false;
        public Vector3D LocalPosition { get; private set; } = Vector3D.Zero;
        public Vector3D GridOffset { get; private set; }
        public Vector3D WorldPosition { get; private set; }
        public IMySlimBlock Block { get; private set; }
        public IMyCubeGrid Grid { get; private set; }

        public IMyEntity PhysicsEntity
        {
            get
            {
                return Grid;
            }
        }

        object IGrabInfo.GrabbedObject { get { return Block; } }

        bool IGrabInfo.IsValid
        {
            get
            {
                return Grid?.Physics != null && !Grid.Closed && Grid.GetCubeBlock(Block.Position) != null;
            }
        }

        public GrabInfoSlimBlock(IMySlimBlock block, Vector3D worldGrabPos)
        {
            MyAPIGateway.Utilities.ShowMessage("link", "slimblock");
            Block = block;
            Grid = block.CubeGrid;

            block.CubeGrid.OnBlockRemoved += OnBlockRemoved;
            block.CubeGrid.PositionComp.OnPositionChanged += OnPositionChanged;

            LocalPosition = LocalPosition = Vector3D.Transform(worldGrabPos, MatrixD.Invert(GetBlockWorldMatrix()));
            RecalculateGridOffset();
            WorldPosition = worldGrabPos;
        }

        private void OnPositionChanged(MyPositionComponentBase obj)
        {
            WorldPosition = Vector3D.Transform(GridOffset, Grid.WorldMatrix);
        }

        private void OnBlockRemoved(IMySlimBlock obj)
        {
            if (obj == Block)
            {
                Grid.OnBlockRemoved -= OnBlockRemoved;
                Grid.PositionComp.OnPositionChanged -= OnPositionChanged;
                dirty = true;
            }
        }

        public void Update()
        {
            if (dirty)
            {
                Grid = Block.CubeGrid;
                Grid.OnBlockRemoved += OnBlockRemoved;
                Grid.PositionComp.OnPositionChanged += OnPositionChanged;
                RecalculateGridOffset();
                dirty = false;
            }
        }

        public void RecalculateGridOffset()
        {
            var currentWorldPos = Vector3D.Transform(LocalPosition, GetBlockWorldMatrix());

            GridOffset = Vector3D.Transform(currentWorldPos, Grid.WorldMatrixNormalizedInv);
        }

        public MatrixD GetBlockWorldMatrix()
        {
            var position = Grid.GridIntegerToWorld(Block.Position);
            Matrix blockMatrix;
            Block.Orientation.GetMatrix(out blockMatrix);

            var transform = blockMatrix * Grid.WorldMatrix;
            transform.Translation = position;

            return transform;
        }
    }
}
