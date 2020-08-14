using UnityEngine;
using DragonSpace.Structs;

namespace DragonSpace.Grids
{
    class LooseGridGizmo : ILooseGridVisitor
    {
        public static LooseGridGizmo Draw { get { return new LooseGridGizmo(); } }

        public void CoarseGrid(float width, float height, float cellWidth, float cellHeight)
        {
            //recalculating stuff just to have fewer parameters
            //There's probably a better way to handle this
            int numRows = (int)(height / cellHeight) + 1;
            int numCols = (int)(width / cellWidth) + 1;
            width = cellWidth * numCols;
            height = cellHeight * numRows;

            //draw grid outline
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(
                new Vector3(width / 2, -1, height / 2),
                new Vector3(width, 1, height));

            //draw grid lines
            for (int i = 1; i < numRows; i++)
            {
                Gizmos.DrawLine(
                    new Vector3(0, -1, i * cellHeight),
                    new Vector3(width, -1, i * cellHeight));
            }
            for (int i = 1; i < numCols; i++)
            {
                Gizmos.DrawLine(
                    new Vector3(i * cellWidth, -1, 0),
                    new Vector3(i * cellWidth, -1, height));
            }
        }

        public void CoarseCell(int count, int x, int y, float cWidth, float cHeight)
        {
            if (count < 1)
                return;

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(
                    new Vector3((x * cWidth) + (cWidth / 2), 0, (y * cHeight) + (cHeight / 2)),
                    new Vector3(cWidth, 1, cHeight));
        }

        public void LooseGrid(float width, float height, float cellWidth, float cellHeight)
        {
            //recalculating stuff just to have fewer parameters
            //There's probably a better way to handle this
            int numRows = (int)(height / cellHeight) + 1;
            int numCols = (int)(width / cellWidth) + 1;
            width = cellWidth * numCols;
            height = cellHeight * numRows;

            //draw grid outline
            Gizmos.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
            Gizmos.DrawWireCube(
                new Vector3(width / 2, -2, height / 2),
                new Vector3(width, 1, height));

            //draw grid lines
            for (int i = 1; i < numRows; i++)
            {
                Gizmos.DrawLine(
                    new Vector3(0, -2, i * cellHeight),
                    new Vector3(width, -2, i * cellHeight));
            }
            for (int i = 1; i < numCols; i++)
            {
                Gizmos.DrawLine(
                    new Vector3(i * cellWidth, -2, 0),
                    new Vector3(i * cellWidth, -2, height));
            }
        }

        public void LooseCell(IGridElt firstElt, AABB bounds)
        {
            GizmoHelp.DrawAABB(Color.yellow, bounds);
        }
    }

    //TODO: won't work with grids not at (0,0). Same for all the other gizmos
    class UGridGizmo : IUniformGridVisitor
    {
        public static UGridGizmo Draw { get { return new UGridGizmo(); } }

        public void Grid(float width, float height, float cellWidth, float cellHeight)
        {
            //recalculating stuff just to have fewer parameters
            //There's probably a better way to handle this
            int numRows = (int)(height / cellHeight) + 1;
            int numCols = (int)(width / cellWidth) + 1;
            width = cellWidth * numCols;
            height = cellHeight * numRows;

            //draw grid outline
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(
                new Vector3(width / 2, 0, height / 2),
                new Vector3(width, 0, height));

            //draw grid lines
            for (int i = 1; i < numRows; i++)
            {
                Gizmos.DrawLine(
                    new Vector3(0, 0, i * cellHeight),
                    new Vector3(width, 0, i * cellHeight));
            }
            for (int i = 1; i < numCols; i++)
            {
                Gizmos.DrawLine(
                    new Vector3(i * cellWidth, 0, 0),
                    new Vector3(i * cellWidth, 0, height));
            }
        }

        public void Cell(IUGridElt firstElt, int x, int y, float cellWidth, float cellHeight)
        {
            float halfWidth = cellWidth / 2;
            float halfHeight = cellHeight / 2;
            Gizmos.color = new Color(0, 0, 1, 0.5f);
            Gizmos.DrawCube(
                new Vector3((x * cellWidth) + halfWidth, 0, (y * cellHeight) + halfHeight),
                new Vector3(cellWidth, 1, cellHeight));
        }
    }
}
