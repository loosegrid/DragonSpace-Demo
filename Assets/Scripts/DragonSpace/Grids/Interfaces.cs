using DragonSpace.Structs;

namespace DragonSpace.Grids
{
    public interface IGridElt
    {
        int ID { get; set; }
        IGridElt NextElt { get; set; }
        /// <summary>
        /// The bottom-left position of the uniformly-sized element
        /// </summary>
        float Xf { get; }
        /// <summary>
        /// The bottom-left position of the uniformly-sized element
        /// </summary>
        float Yf { get; }
        int Width { get; }
        int Height { get; }

        //TODO: destroying an element without removing it from the tree will break the list!
        //      same for the loose quadtree! Is that acceptable or am I laying a trap for myself??
    }

    public interface IUGridElt
    {
        /// <summary>
        /// The next object in the linked list of whatever cell this is in.
        /// Note destroying the object without removing it from the grid 
        /// will break the linked list so don't do that
        /// </summary>
        IUGridElt NextElt { get; set; }

        /// <summary>
        /// The bottom-left position of the uniformly-sized element
        /// </summary>
        float Xf { get; }
        /// <summary>
        /// The bottom-left position of the uniformly-sized element
        /// </summary>
        float Yf { get; }
        /// <summary>
        /// A unique id to identify the object.
        /// Only needs to be unique within the grid it's inserted into
        /// </summary>
        int ID { get; set; }
    }

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
