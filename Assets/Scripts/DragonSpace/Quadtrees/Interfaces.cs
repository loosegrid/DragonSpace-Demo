namespace DragonSpace.Quadtrees
{
    public interface IQtVisitor
    {
        #region Generic interface
        /// <summary>
        /// Called when traversing a branch node in normal trees
        /// </summary>
        /// <param name="qt">The quadtree to visit</param>
        /// <param name="node">The index of the node in the nodes list</param>
        /// <param name="depth">The depth of the node</param>
        /// <param name="mx">x coordinate of the center of the node's AABB.</param>
        /// <param name="my">y coordinate of the center of the node's AABB.</param>
        /// <param name="sx">the half-size of the node's AABB x axis.</param>
        /// <param name="sy">the half-size of the node's AABB y axis.</param>
        void Branch(int node, int depth, int mx, int my, int sx, int sy);

        /// <summary>
        /// Called when traversing a leaf node in normal trees
        /// </summary>
        /// <param name="count">The number of elements in the leaf</param>
        /// <param name="node">The index of the node in the nodes list</param>
        /// <param name="depth">The depth of the node</param>
        /// <param name="mx">x coordinate of the center of the node's AABB.</param>
        /// <param name="my">y coordinate of the center of the node's AABB.</param>
        /// <param name="sx">the half-size of the node's AABB x axis.</param>
        /// <param name="sy">the half-size of the node's AABB y axis.</param>
        void Leaf(int count, int node, int depth, int mx, int my, int sx, int sy);

        /// <summary>
        /// Called for each branch node in loose trees
        /// </summary>
        /// <param name="qt">The quadtree to visit</param>
        /// <param name="lft">The left edge of the node</param>
        /// <param name="top">The top edge of the node</param>
        /// <param name="rgt">The right edge of the node</param>
        /// <param name="btm">The left edge of the node</param>
        void Branch(int lft, int top, int rgt, int btm);

        /// <summary>
        /// Called for each leaf node in loose trees
        /// </summary>
        /// <param name="count">The number of elements in the leaf</param>
        /// <param name="lft">The left edge of the node</param>
        /// <param name="top">The top edge of the node</param>
        /// <param name="rgt">The right edge of the node</param>
        /// <param name="btm">The left edge of the node</param>
        void Leaf(int count, int lft, int top, int rgt, int btm);
        #endregion
    }

    public interface IQtInsertable  //TODO: actually use this???
    {
        int X { get; }
        int Y { get; }
        int Lft { get; }
        int Top { get; }
        int Rgt { get; }
        int Btm { get; }
        int Width { get; }
        int Height { get; }
    }
}
