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
        private bool gridChanged = false;
        public Vector3D LocalPosition { get; private set; } = Vector3D.Zero;
        public Vector3D GridOffset { get; private set; }
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
        public IMySlimBlock Block { get; private set; }

        private IMyCubeGrid grid;
        public IMyCubeGrid Grid
        {
            get
            {
                if (gridChanged)
                {
                    CalculateGridOffset();
                }
                return grid;
            }
        }

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
            Block = block;
            grid = block.CubeGrid;
            LocalPosition = Vector3D.Transform(worldGrabPos, MatrixD.Invert(GetBlockWorldMatrix()));
            worldPosition = worldGrabPos;

            CalculateGridOffset();
            RecalculateWorldPosition();
        }

        private void OnPositionChanged(MyPositionComponentBase obj)
        {
            dirty = true;
        }

        private void RecalculateWorldPosition()
        {
            worldPosition = Vector3D.Transform(GridOffset, Grid.WorldMatrix);
            dirty = false;
        }

        private void OnBlockRemoved(IMySlimBlock obj)
        {
            if (obj == Block)
            {
                grid.OnBlockRemoved -= OnBlockRemoved;
                grid.PositionComp.OnPositionChanged -= OnPositionChanged;
                grid = null;
                gridChanged = true;
            }
        }

        private void CalculateGridOffset()
        {
            grid = Block.CubeGrid;
            grid.OnBlockRemoved += OnBlockRemoved;
            grid.PositionComp.OnPositionChanged += OnPositionChanged;
            var currentWorldPos = Vector3D.Transform(LocalPosition, GetBlockWorldMatrix());
            GridOffset = Vector3D.Transform(currentWorldPos, grid.WorldMatrixNormalizedInv);
            MyAPIGateway.Utilities.ShowMessage("", GridOffset.ToString());
            gridChanged = false;
        }

        public MatrixD GetBlockWorldMatrix()
        {
            var position = grid.GridIntegerToWorld(Block.Position);
            Matrix blockMatrix;
            Block.Orientation.GetMatrix(out blockMatrix);

            var transform = blockMatrix * grid.WorldMatrix;
            transform.Translation = position;

            return transform;
        }
    }
}
