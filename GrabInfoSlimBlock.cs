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
        private Vector3D worldPosition;
        private IMyCubeGrid grid;
        public Vector3D LocalPosition { get; }
        public Vector3D GridOffset { get; private set; }
        public IMySlimBlock Block { get; }
        public IMyEntity PhysicsEntity => CurrentGrid;

        public Vector3D WorldPosition
        {
            get
            {
                UpdateWorldPosition();
                return worldPosition;
            }
        }

        public IMyCubeGrid CurrentGrid
        {
            get
            {
                UpdateGridOffset();
                return grid;
            }
        }


        object IGrabInfo.GrabbedObject => Block;
        bool IGrabInfo.IsValid => CurrentGrid?.Physics != null && !CurrentGrid.Closed && CurrentGrid.GetCubeBlock(Block.Position) != null;

        public GrabInfoSlimBlock(IMySlimBlock block, Vector3D worldGrabPos)
        {
            Block = block;
            grid = block.CubeGrid;
            LocalPosition = Vector3D.Transform(worldGrabPos, MatrixD.Invert(GetBlockWorldMatrix()));
            worldPosition = worldGrabPos;

            UpdateGridOffset();
            UpdateWorldPosition();
        }

        private void OnPositionChanged(MyPositionComponentBase obj)
        {
            dirty = true;
        }

        private void UpdateWorldPosition()
        {
            if (dirty)
            {
                worldPosition = Vector3D.Transform(GridOffset, CurrentGrid.WorldMatrix);
                dirty = false;
            }
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

        private void UpdateGridOffset()
        {
            if (gridChanged)
            {
                grid = Block.CubeGrid;
                grid.OnBlockRemoved += OnBlockRemoved;
                grid.PositionComp.OnPositionChanged += OnPositionChanged;
                var currentWorldPos = Vector3D.Transform(LocalPosition, GetBlockWorldMatrix());
                GridOffset = Vector3D.Transform(currentWorldPos, grid.WorldMatrixNormalizedInv);
                MyAPIGateway.Utilities.ShowMessage("", GridOffset.ToString());
                gridChanged = false;
            }
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
