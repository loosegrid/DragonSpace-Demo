namespace QTExperiments.Quadtrees
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using QTExperiments.Lists;
    using QTExperiments.QtStructs;

    /// <summary>
    /// A rectangular point-region quadtree (I think) that inserts elements into every leaf node
    /// they overlap. Better for static data.
    /// </summary>
    public class QuadTree<T> : QuadTreeBase<T> where T : class
    {
        /// <summary>
        /// Creates a new tree and automatically determines the max depth based on the average
        /// element size to minimize elements being inserted into too many leaves. 
        /// The optimal settings may be different
        /// </summary>
        /// <param name="width">The width of the tree</param>
        /// <param name="height">The height of the tree</param>
        /// <param name="avgEltSize">The approximate average size of your elements largest dimension</param>
        /// <returns>A new tree, set to 11 max elements and the computed max depth</returns>
        public static QuadTree<T> NewTree(int width, int height, int avgEltSize)
        {
            if (width < 0 || height < 0)
            {
                throw new ArgumentException("width and height must be greater than zero");
            }

            int nodeMinSize = Math.Min(width, height);
            int i = 0;
            while (nodeMinSize > avgEltSize)
            {
                ++i;
                nodeMinSize /= 2;
            }
            return new QuadTree<T>(width, height, 11, i-1);
        }

        /// <summary>
        /// Creates a quadtree with the requested extents, maximum elements per leaf, and maximum tree depth.
        /// </summary>
        /// <param name="width">Width of the root node</param>
        /// <param name="height">Height of the root node</param>
        /// <param name="maxElts">Maximum elements per leaf node</param>
        /// <param name="maxDepth">Maximum tree depth</param>
        public QuadTree(int width, int height, int maxElts, int maxDepth)
        {
            if (width < 0 || height < 0)
            {
                throw new ArgumentException("width and height must be greater than zero");
            }

            MaxElements = maxElts;
            MaxDepth = maxDepth;

            // Insert the root node to the qt.
            _nodes.Insert(QtNode.Empty);

            // Set the extents of the root node.
            _rootNode = new QtNodeData
            {
                idx = 0,
                mx = width / 2,
                my = height / 2
            };
            _rootNode.sx = _rootNode.mx;
            _rootNode.sy = _rootNode.my;
        }

        public override T this[int index] => _elements[index].obj;

        /// <summary>
        /// Inserts a new element with a bounding box to the tree and returns the index to it
        /// </summary>
        /// <param name="thing">The object to insert</param>
        /// <param name="lft">The left edge of the element</param>
        /// <param name="top">The TOP edge of the element</param>
        /// <param name="rgt">The right edge of the element</param>
        /// <param name="btm">The BOTTOM edge of the element</param>
        /// <returns>index of the element in the tree</returns>
        public override int InsertRect(T thing, int lft, int top, int rgt, int btm)
        {
            if (rgt <= lft || top <= btm)
            {
                throw new ArgumentException("width and height must be greater than zero");
            }

            int newElement = _elements.Insert(new QtElement<T>(thing, lft, top, rgt, btm));

            // Insert the element to the appropriate leaf node(s).
            NodeInsert(_rootNode, newElement);
            return newElement;
        }

        /// <summary>
        /// Inserts a new element with a point and half size to the tree and returns the index to it
        /// </summary>
        /// <param name="thing">The object to insert</param>
        /// <param name="x">The x coordinate of the element's bottom-left corner</param>
        /// <param name="y">The y coordinate of the element's bottom-left corner</param>
        /// <param name="width">The width of the element</param>
        /// <param name="height">The height of the element</param>
        /// <returns>index of the element in the tree's list</returns>
        public override int InsertPoint(T thing, int x, int y, int width, int height)
        {
            if (width < 0 || height < 0)
            {
                throw new ArgumentException("width and height must be greater than zero");
            }

            return InsertRect(thing, x, y + height, x + width, y);
        }

        /// <summary>
        /// Removes the specified element from the tree.
        /// </summary>
        /// <param name="eltIndex">The index of the element</param>
        protected override void RemoveIndex(int eltIndex, bool fromList = true)
        {
            // Find the leaves that contain the element
            ref QtElement<T> elt = ref _elements[eltIndex];
            List<QtNodeData> leaves = _listPool.RentList();
            FindLeaves(in leaves, _rootNode, in elt);

            // For each leaf node, remove the element node.
            for (int i = leaves.Count - 1; i >= 0; i--)
            {
                int nodeIdx = leaves[i].idx;

                // Walk the list of element nodes until we find the one with the element
                int enodeIdx = _nodes[nodeIdx].firstChild;
                int prevIndex = -1;
                while (enodeIdx != -1 && _enodes[enodeIdx].elt != eltIndex)
                {
                    prevIndex = enodeIdx;
                    enodeIdx = _enodes[enodeIdx].nextEnode;
                }

                if (enodeIdx != -1)
                {
                    // Remove the element node.
                    int nextIndex = _enodes[enodeIdx].nextEnode;
                    if (prevIndex == -1)
                        _nodes[nodeIdx].firstChild = nextIndex;
                    else
                        _enodes[prevIndex].nextEnode = nextIndex;
                    _enodes.RemoveAt(enodeIdx);

                    // Decrement the leaf element count.
                    _nodes[nodeIdx].childCount--;
                }
            }

            _listPool.ReturnList(leaves);
            // Remove the element.
            if (fromList)
                _elements.RemoveAt(eltIndex);
        }

        /// <summary>
        /// Moves the specified element the specified distance
        /// </summary>
        /// <param name="eltIndex">The index of the element</param>
        /// <param name="moveY">The distance to move the element</param>
        public override void MoveIndex(int eltIndex, int moveX, int moveY)
        {
            RemoveIndex(eltIndex, false);

            ref QtElement<T> elt = ref _elements[eltIndex];
            elt.lft += moveX;
            elt.rgt += moveX;
            elt.btm += moveY;
            elt.top += moveY;
            
            NodeInsert(_rootNode, eltIndex);
        }

        /// <summary>
        /// Moves the specified element to a new point
        /// </summary>
        /// <param name="eltIndex">The index of the element</param>
        /// <param name="newX">The new x position for the element's bottom-left corner</param>
        /// <param name="newY">The new y position for the element's bottom-left corner</param>
        public override void MoveIndexToPoint(int eltIndex, int newX, int newY)
        {
            RemoveIndex(eltIndex, false);

            ref QtElement<T> elt = ref _elements[eltIndex];

            int w = elt.Width;
            int h = elt.Height;

            elt.lft = newX;
            elt.rgt = newX + w;
            elt.btm = newY;
            elt.top = newY + h;

            NodeInsert(_rootNode, eltIndex);
        }

        /// <summary>Stores the indexes of the nodes for <see cref="Cleanup"/></summary>
        private Stack<int> _toClean = new Stack<int>(64);
        /// <summary>
        /// Cleans up the tree, removing empty leaves. Call at the end of a frame
        /// </summary>
        public override void Cleanup()
        {
            // Only process the root if it's not a leaf.
            if (_nodes[0].childCount == -1)
            {
                // Push the root index to the stack.
                _toClean.Push(0);
            }

            while (_toClean.Count > 0)
            {
                // Pop a node from the stack.
                int node = _toClean.Pop();
                int fc = _nodes[node].firstChild;
                int emptyLeaves = 0;

                // Loop through the children.
                for (int j = 0; j < 4; ++j)
                {
                    int child = fc + j;

                    // Increment empty leaf count if the child is an empty 
                    // leaf. Otherwise if the child is a branch, add it to
                    // the stack to be processed in the next iteration. (of this while loop)
                    if (_nodes[child].childCount == 0)
                        ++emptyLeaves;
                    else if (_nodes[child].childCount == -1)
                    {
                        // Push the child index to the stack.
                        _toClean.Push(child);
                    }
                }

                // If all the children were empty leaves, remove them and 
                // make this node the new empty leaf.
                if (emptyLeaves == 4)
                {
                    // Remove all 4 children in reverse order so that they 
                    // can be reclaimed on subsequent insertions in proper
                    // order. (because it's a free list)
                    _nodes.Remove(fc + 3);
                    _nodes.Remove(fc + 2);
                    _nodes.Remove(fc + 1);
                    _nodes.Remove(fc + 0);

                    // Make this node the new empty leaf.
                    _nodes[node] = QtNode.Empty;
                }
            }
        }

        /// <summary>
        /// Returns a list of element indexes found in the specified rectangle
        /// </summary>
        /// <param name="qlft">left edge of the rectangle to query</param>
        /// <param name="qtop">top edge of the rectangle to query</param>
        /// <param name="qrgt">right edge of the rectangle to query</param>
        /// <param name="qbtm">bottom edge of the rectangle to query</param>
        /// <returns>A Stack<int> of element indexes</returns>
        public override List<T> Query(int qlft, int qtop, int qrgt, int qbtm)
        {
            return Query(qlft, qtop, qrgt, qbtm, -1);
        }

        private readonly List<T> _queryResults = new List<T>(16);
        /// <summary>
        /// Returns a list of element indexes found in the specified rectangle excluding the
        /// specified element to omit.
        /// </summary>
        /// <param name="qlft">left edge of the rectangle to query</param>
        /// <param name="qtop">top edge of the rectangle to query</param>
        /// <param name="qrgt">right edge of the rectangle to query</param>
        /// <param name="qbtm">bottom edge of the rectangle to query</param>
        /// <param name="omitElement">Element index to omit</param>
        /// <returns>A Stack<int> of element indexes</returns>
        public override List<T> Query(int qlft, int qtop, int qrgt, int qbtm, int omitElement)
        {
            _queryResults.Clear();

            // Find the leaves that intersect the specified query rectangle.
            QtElement<T> query = new QtElement<T>(null, qlft, qtop, qrgt, qbtm);

            List<QtNodeData> leavesData = _listPool.RentList();
            FindLeaves(in leavesData, in _rootNode, query);

            if (_temp.Count < _elements.Count)
            { _temp.Length = _elements.Count; }

            // For each leaf node, look for elements that intersect.
            for (int i = leavesData.Count - 1; i >= 0; --i)
            {
                int index = leavesData[i].idx;

                // Walk the list and add elements that intersect.
                int eltNodeIndex = _nodes[index].firstChild;
                while (eltNodeIndex != -1)
                {
                    int eltIdx = _enodes[eltNodeIndex].elt;

                    if (!_temp[eltIdx] && eltIdx != omitElement && RectOverlap(query, _elements[eltIdx]))
                    {
                        //add a new element to the list and mark the element 
                        _queryResults.Add(_elements[eltIdx].obj);
                        _temp[eltIdx] = true;
                    }
                    eltNodeIndex = _enodes[eltNodeIndex].nextEnode;
                }
            }
            _listPool.ReturnList(leavesData);
            // Unmark the elements that were inserted.
            _temp.SetAll(false);
            return _queryResults;
        }

        private Stack<QtNodeData> _toTraverse = new Stack<QtNodeData>(64);
        /// <summary>
        /// Traverses all the nodes in the tree, calling 'branch' for branch nodes and 'leaf' for leaf nodes.
        /// </summary>
        /// <param name="visitor"></param>
        public override void Traverse(IQtVisitor visitor)
        {
            _toTraverse.Push(_rootNode);

            while (_toTraverse.Count > 0)
            {
                QtNodeData node = _toTraverse.Pop();
                int fc = _nodes[node.idx].firstChild;

                if (_nodes[node.idx].childCount == -1)
                {
                    // Push the children of the branch to the stack.
                    int hx = node.sx >> 1, hy = node.sy >> 1;  //half the half size
                    int l = node.mx - hx, t = node.my + hy, r = node.mx + hx, b = node.my - hy;
                    _toTraverse.Push(new QtNodeData(fc + 0, node.depth + 1, l, t, hx, hy));
                    _toTraverse.Push(new QtNodeData(fc + 1, node.depth + 1, r, t, hx, hy));
                    _toTraverse.Push(new QtNodeData(fc + 2, node.depth + 1, l, b, hx, hy));
                    _toTraverse.Push(new QtNodeData(fc + 3, node.depth + 1, r, b, hx, hy));
                    visitor.Branch(node.idx, node.depth, node.mx, node.my, node.sx, node.sy);
                }
                else
                    visitor.Leaf(_nodes[node.idx].childCount, node.idx, node.depth, node.mx, node.my, node.sx, node.sy);
            }
        }
        #region Private Methods       

        /// <summary>
        /// Finds all leaf nodes overlapping a rectangle.
        /// </summary>
        /// <param name="results">A list to fill with the overlapping leaves</param>
        /// <param name="node">The node to search from</param>
        /// <param name="elt">The element or query area to find</param>
        protected void FindLeaves(in List<QtNodeData> results, in QtNodeData node, in QtElement<T> elt)
        {
            FindLeavesRecursive(in results, node.idx, node.depth, node.mx, node.my, node.sx, node.sy, in elt);
        }

        /// <summary>
        /// Finds all leaf nodes overlapping a rectangle. 
        /// Should only be called from <see cref="FindLeaves(in List{QtNodeData}, in QtNodeData, in QtElement)"/>
        /// </summary>
        /// <param name="results">The list to fill with results</param>
        /// <param name="idx">The index of the node to search</param>
        /// <param name="depth">The depth of the node to search</param>
        /// <param name="mx">The center x of the node to search</param>
        /// <param name="my">The center y of the node to search</param>
        /// <param name="sx">The half width of the node</param>
        /// <param name="sy">The half height of the node</param>
        /// <param name="elt">The rectangle to compare. ID is not used</param>
        protected void FindLeavesRecursive(in List<QtNodeData> results, 
            int idx, int depth, int mx, int my, int sx, int sy, in QtElement<T> elt)
        {
            ref QtNode node = ref _nodes[idx];
            // If this node is a leaf, add it to the results
            if (node.childCount != -1)
            { results.Add(new QtNodeData(idx, depth, mx, my, sx, sy)); }
            else
            {
                // Otherwise push the child nodes that intersect the rectangle.
                int fc = node.firstChild;
                int hx = sx >> 1, hy = sy >> 1;  //hx = half x aka 1/4 of the node
                int l = mx - hx, t = my + hy, r = mx + hx, b = my - hy;

                if (elt.top >= my)
                {
                    if (elt.lft <= mx)
                        FindLeavesRecursive(in results, fc + 0, depth + 1, l, t, hx, hy, in elt);
                    if (elt.rgt > mx)
                        FindLeavesRecursive(in results, fc + 1, depth + 1, r, t, hx, hy, in elt);
                }
                if (elt.btm < my)
                {
                    if (elt.lft <= mx)
                        FindLeavesRecursive(in results, fc + 2, depth + 1, l, b, hx, hy, in elt);
                    if (elt.rgt > mx)
                        FindLeavesRecursive(in results, fc + 3, depth + 1, r, b, hx, hy, in elt);
                }
            }
        }

        /// <summary>
        /// inserts an element into all the leaves of a node that it overlaps
        /// </summary>
        /// <param name="nData">The data of the node to insert to</param>
        /// <param name="eltIdx">The index of the element to insert</param>
        /// <returns></returns>
        protected virtual void NodeInsert(in QtNodeData nData, int eltIdx)
        {
            List<QtNodeData> leavesData = _listPool.RentList();
            FindLeaves(in leavesData, in nData, in _elements[eltIdx]);

            // insert the element to all the leaves found.
            for (int i = leavesData.Count - 1; i >= 0; i--)
            {
                QtNodeData next = leavesData[i];
                LeafInsert(next, eltIdx);
            }
            _listPool.ReturnList(leavesData);
        }

        /// <summary>
        /// Stores the indexes of elements being moved from a leaf that's splitting.
        /// </summary>
        protected Stack<int> _toSplit = new Stack<int>(16);
        /// <summary>
        /// inserts an element to a leaf and splits it if necessary. Note "inserting" means 
        /// adding a new element node to the leaf node's linked list
        /// </summary>
        /// <param name="nData">The leaf to insert to</param>
        /// <param name="element">The element index to insert</param>
        protected virtual void LeafInsert(in QtNodeData nData, int element)
        {
            // Insert the element node to the leaf.
            int fc = _nodes[nData.idx].firstChild;
            _nodes[nData.idx].firstChild = _enodes.Insert(new EltNode(fc, element));

            // If the leaf is full, split it.
            if (_nodes[nData.idx].childCount++ == MaxElements && nData.depth < MaxDepth)
            {
                // Transfer elements from the leaf node to a temporary list of elements.
                while (_nodes[nData.idx].firstChild != -1)
                {
                    int index = _nodes[nData.idx].firstChild;
                    int nextIndex = _enodes[index].nextEnode;
                    int eltIdx = _enodes[index].elt;

                    // Pop off the element node from the leaf and remove it from the qt.
                    _nodes[nData.idx].firstChild = nextIndex;
                    _enodes.RemoveAt(index);

                    // Insert element to the list.
                    _toSplit.Push(eltIdx);
                }
                // Allocate 4 child nodes.  
                _nodes[nData.idx].firstChild = _nodes.Insert(QtNode.Empty);
                _nodes.Insert(QtNode.Empty);
                _nodes.Insert(QtNode.Empty);
                _nodes.Insert(QtNode.Empty);
                // Transfer the elements in the former leaf node to its new children.
                _nodes[nData.idx].childCount = -1;
                do
                {
                    NodeInsert(nData, _toSplit.Pop());
                } while (_toSplit.Count > 0);
                    
            }
        }
        #endregion


        // ----------------------------------------------------------------------------------------
        // Quadtree Members
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Temporary buffer used for queries.
        /// </summary>
        protected BitArray _temp = new BitArray(1);

        protected ListPool<QtNodeData> _listPool = new ListPool<QtNodeData>(2,4);
        protected Stack<QtNodeData> _foundNodes = new Stack<QtNodeData>(16);

        /// <summary>
        /// The bounds of the root node, which is the entire quadtree
        /// </summary>
        protected QtNodeData _rootNode;

        /// <summary>
        /// Maximum allowed elements in a leaf before the leaf is subdivided/split 
        /// unless the leaf is at the maximum allowed tree depth.
        /// </summary>
        public int MaxElements { get; protected set; }

        /// <summary>
        /// Stores the maximum depth allowed for the quadtree.
        /// </summary>
        public int MaxDepth { get; protected set; }

        // ----------------------------------------------------------------------------------------
        // Element node fields:
        // An element node is part of a singly-linked list, holding the ID of an element,
        // and a reference (int ID) to the next element node
        // ----------------------------------------------------------------------------------------
        protected FreeList<EltNode> _enodes = new FreeList<EltNode>(16);
                
        // ----------------------------------------------------------------------------------------
        // Element fields:
        // An element is an int ID and four ints for the AABB containing it.
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Stores all the elements in the quadtree. Each element has 5 int fields: 
        /// an ID, and four coordinates of a rectangle
        /// </summary>
        private FreeList<QtElement<T>> _elements = new FreeList<QtElement<T>>(128);

        // ----------------------------------------------------------------------------------------
        // Node fields:
        // a node is just two fields indicating the location in the tree structure,
        // the rest of the spatial data is computed as needed from the parent nodes and root
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Stores all the nodes in the quadtree. The first node in this
        /// sequence is always the root.
        /// </summary>
        protected NodeList _nodes = new NodeList();
    }
}