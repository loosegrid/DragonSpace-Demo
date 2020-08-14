using DragonSpace.Structs;

namespace DragonSpace.Grids
{
    public interface ILooseGridVisitor
    {
        void CoarseGrid(float width, float height, float cellWidth, float cellHeight);

        void CoarseCell(int count, int x, int y, float cWidth, float cHeight);

        void LooseGrid(float width, float height, float cellWidth, float cellHeight);

        void LooseCell(IGridElt firstElt, AABB bounds);
    }

    public interface IUniformGridVisitor
    {
        void Grid(float width, float height, float cellWidth, float cellHeight);

        void Cell(IUGridElt firstElt, int x, int y, float cellWidth, float cellHeight);
    }
}
