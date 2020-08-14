namespace DragonSpace.Quadtrees
{
    using System.Collections.Generic;
    using DragonSpace.QtStructs;

    /// <summary>
    /// Base class for quadtree implementations
    /// </summary>
    public abstract class QuadTreeBase<T> where T : class
    {
        public abstract T this[int index] { get; }

        /// <summary>
        /// Inserts a new element with a bounding box to the tree and returns the index to it
        /// </summary>
        /// <param name="thing">The object</param>
        /// <param name="lft">The left edge of the element</param>
        /// <param name="top">The TOP edge of the element</param>
        /// <param name="rgt">The right edge of the element</param>
        /// <param name="btm">The BOTTOM edge of the element</param>
        /// <returns>index of the element in the tree</returns>
        public abstract int InsertRect(T thing, int lft, int top, int rgt, int btm);

        /// <summary>
        /// Inserts a new element with a center and half size to the tree and returns the index to it
        /// </summary>
        /// <param name="thing">The object</param>
        /// <param name="x">The x coordinate of the element's bottom-left corner</param>
        /// <param name="y">The y coordinate of the element's bottom-left corner</param>
        /// <param name="width">The width of the element</param>
        /// <param name="height">The height of the element</param>
        /// <returns>index of the element in the tree's list</returns>
        public abstract int InsertPoint(T thing, int x, int y, int width, int height);

        /// <summary>
        /// Removes the specified element from the tree.
        /// </summary>
        /// <param name="eltIndex">The index of the element</param>
        public virtual void RemoveIndex(int eltIndex)
        {
            RemoveIndex(eltIndex, true);
        }

        /// <summary>
        /// Removes the specified element from the tree.
        /// </summary>
        /// <param name="eltIndex">The index of the element</param>
        /// <param name="fromList">Should the element be removed from the element list? 
        /// used for moving elements within the tree</param>
        protected abstract void RemoveIndex(int eltIndex, bool fromList = true);

        /// <summary>
        /// Moves the specified element.
        /// </summary>
        /// <param name="eltIndex">The index of the element</param>
        /// <param name="moveY">The distance to move the element</param>
        public abstract void MoveIndex(int eltIndex, int moveX, int moveY);

        /// <summary>
        /// Moves the specified element to a new point
        /// </summary>
        /// <param name="eltIndex">The index of the element</param>
        /// <param name="newX">The new x position for the element's bottom-left corner</param>
        /// <param name="newY">The new y position for the element's bottom-left corner</param>
        public abstract void MoveIndexToPoint(int eltIndex, int newX, int newY);

        /// <summary>
        /// Cleans up the tree, removing empty leaves. Contracts nodes in the loose version 
        /// Call at the end of a frame
        /// </summary>
        public abstract void Cleanup();

        /// <summary>
        /// Returns a list of element indexes found in the specified rectangle
        /// </summary>
        /// <param name="qlft">left edge of the rectangle to query</param>
        /// <param name="qtop">top edge of the rectangle to query</param>
        /// <param name="qrgt">right edge of the rectangle to query</param>
        /// <param name="qbtm">bottom edge of the rectangle to query</param>
        /// <returns>A Stack<int> of element indexes</returns>
        public abstract List<T> Query(int qlft, int qtop, int qrgt, int qbtm);

        /// <summary>
        /// Returns a list of element indexes found in the specified rectangle excluding the
        /// specified element to omit.
        /// </summary>
        /// <param name="qlft">left edge of the rectangle to query</param>
        /// <param name="qtop">top edge of the rectangle to query</param>
        /// <param name="qrgt">right edge of the rectangle to query</param>
        /// <param name="qbtm">bottom edge of the rectangle to query</param>
        /// <param name="omitElement">element index to omit</param>
        /// <returns>A Stack<int> of element indexes</returns>
        public abstract List<T> Query(int qlft, int qtop, int qrgt, int qbtm, int omitElement);

        /// <summary>
        /// Traverses all the nodes in the tree, calling 'branch' for branch nodes and 'leaf' for leaf nodes.
        /// </summary>
        /// <param name="visitor"></param>
        public abstract void Traverse(IQtVisitor visitor);

        protected static bool RectOverlap(in QtElement<T> a, in QtElement<T> b)
        {
            return RectOverlap(a.lft, a.top, a.rgt, a.btm, b.lft, b.top, b.rgt, b.btm);
        }

        protected static bool RectOverlap(int l1, int t1, int r1, int b1,
                                         int l2, int t2, int r2, int b2)
        {
            return l2 <= r1 && r2 >= l1 && t2 >= b1 && b2 <= t1;
        }        
    }
}