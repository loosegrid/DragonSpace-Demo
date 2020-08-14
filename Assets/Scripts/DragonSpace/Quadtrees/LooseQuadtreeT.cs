namespace DragonSpace.Quadtrees
{
    using System;
    using System.Collections.Generic;
    using DragonSpace.Structs;
    using DragonSpace.QtStructs;

    /// <summary>
    /// A loose quadtree that stores elements as a point and size rather than a bounding box.
    /// Nodes have a bounding box that expands to contain their children, and elements are only
    /// inserted into one leaf, making this faster for insertion and removal.
    /// </summary>
    public class LooseQuadTree<T> : QuadTreeBase<T> where T : class
    {
        /// <summary>
        /// Creates a new tree and automatically determines the max depth based on the average
        /// element size - the optimal settings may be slightly different
        /// </summary>
        /// <param name="width">The width of the tree</param>
        /// <param name="height">The height of the tree</param>
        /// <param name="avgEltSize">The approximate average size of your elements largest dimension</param>
        /// <returns>A new tree, set to 9 max elements and the computed max depth</returns>
        public static LooseQuadTree<T> NewTree(int width, int height, int avgEltSize)
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
            return new LooseQuadTree<T>(width, height, 9, i - 1);
        }

        /// <summary>
        /// Creates an empty tree with width and height of 1000
        /// </summary>
        public LooseQuadTree()
        {
            MaxElements = 8;
            MaxDepth = 6;

            _HalfWidth = _HalfHeight = 500;
            _centerX = _HalfWidth;
            _centerY = _HalfHeight;

            //we don't use element 0, so we need a placeholder
            _elements.Insert(new LQtElement<T>(
                null, int.MinValue, int.MinValue, 0, 0));
            // Insert the root node to the qt.
            _nodes.Insert(new QtLooseNode());
        }

        /// <summary>
        /// Creates a quadtree with the requested extents, maximum elements per leaf, and maximum tree depth.
        /// </summary>
        /// <param name="width">Width of the tree</param>
        /// <param name="height">Height of the tree</param>
        /// <param name="maxElts">Maximum elements per leaf node</param>
        /// <param name="maxDepth">Maximum tree depth</param>
        public LooseQuadTree(int width, int height, int maxElts, int maxDepth)
        {
            if (width < 0 || height < 0)
            {
                throw new ArgumentException("width and height must be greater than zero");
            }

            MaxElements = maxElts;
            MaxDepth = maxDepth;

            _HalfWidth = width / 2;
            _HalfHeight = height / 2;
            _centerX = _HalfWidth;
            _centerY = _HalfHeight;

            //we don't use element 0, so we need a placeholder
            _elements.Insert(new LQtElement<T>(
                default, int.MinValue, int.MinValue, 0, 0));
            // Insert the root node to the qt.
            _nodes.Insert(new QtLooseNode());
        }

        public override T this[int index]
        {
            get => _elements[index].obj;
        }

        /// <summary>
        /// Inserts a new element with a bounding box to the tree and returns the index to it
        /// </summary>
        /// <param name="thing">The element to insert</param>
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

            int w = rgt - lft;
            int h = top - btm;
            int x = lft;
            int y = btm;
            return InsertPoint(thing, x, y, w, h);
        }

        /// <summary>
        /// Inserts a new element to the tree and returns the index to it
        /// </summary>
        /// <param name="obj">The element to insert</param>
        /// <param name="x">The x coordinate of the element's bottom-left corner</param>
        /// <param name="y">The y coordinate of the element's bottom-left corner</param>
        /// <param name="width">The width of the element</param>
        /// <param name="height">The height of the element</param>
        /// <returns>index of the element in the tree's list</returns>
        public override int InsertPoint(T obj, int x, int y, int width, int height)
        {
            if (width < 0 || height < 0)
            {
                throw new ArgumentException("width and height must be greater than zero");
            }

            int newElement = _elements.Insert(new LQtElement<T>(obj, x, y, width, height));

            // Insert the element to the appropriate leaf node(s).
            int leafIdx = FindLeafAndExpand(in _elements[newElement], out int d);
            LeafInsert(leafIdx, d, newElement);

            return newElement;
        }

        /// <summary>
        /// Inserts a new element to the tree without expanding any bounding boxes, 
        /// then returns the index to it. Use to bulk insert a large number of elements at once 
        /// for example, loading saved state. Call <see cref="Cleanup"/> after to set the 
        /// bounding boxes.
        /// </summary>
        /// <param name="obj">The element to insert</param>
        /// <param name="x">The x coordinate of the element's bottom-left corner</param>
        /// <param name="y">The y coordinate of the element's bottom-left corner</param>
        /// <param name="width">The width of the element</param>
        /// <param name="height">The height of the element</param>
        /// <returns>index of the element in the tree's list</returns>
        public int BulkInsertPoint(T obj, int x, int y, int width, int height)
        {
            int newElement = _elements.Insert(new LQtElement<T>(obj, x, y, width, height));

            // Insert the element to the appropriate leaf node(s).
            int leafIdx = FindLeaf(in _elements[newElement], out int d);
            BulkLeafInsert(leafIdx, d, newElement);

            return newElement;
        }

        /// <summary>
        /// Removes the specified element from the tree.
        /// </summary>
        /// <param name="eltIndex">The index of the element</param>
        /// <param name="fromList">Should the element be removed from the element list? 
        /// used for moving elements within the tree</param>
        protected override void RemoveIndex(int eltIndex, bool fromList = true)
        {
            // Find the leaf that contains the element
            ref LQtElement<T> elt = ref _elements[eltIndex];
            int leafIdx = FindLeaf(in elt, out int d);

            //walk the list until we find it
            int nextIndex = _nodes[leafIdx].fc;
            int prevIndex = -1;
            while (nextIndex != eltIndex)
            {
                prevIndex = nextIndex;
                nextIndex = _elements[nextIndex].next;
            }
            //and remove it
            if (prevIndex > 0)
                _elements[prevIndex].next = elt.next;
            else
                _nodes[leafIdx].fc = elt.next;
            _nodes[leafIdx].childCount--;

            // Remove the element from the elements list
            if (fromList)
                _elements.Remove(eltIndex);
        }

        /// <summary>
        /// Moves the specified element by the specified distance
        /// </summary>
        /// <param name="eltIndex">The index of the element</param>
        /// <param name="moveX">The horizontal distance to move the element</param>
        /// <param name="moveY">The vertical distance to move the element</param>
        public override void MoveIndex(int eltIndex, int moveX, int moveY)
        {
            if (eltIndex < 1 || eltIndex > _elements.Count)
            {
                throw new IndexOutOfRangeException(
                    "Trying to move invalid index. Note elements begin at index 1, not 0");
            }

            //find the leaf that has the element
            ref LQtElement<T> elt = ref _elements[eltIndex];

            RemoveIndex(eltIndex, false);

            //update the element's position
            elt.x += moveX;
            elt.y += moveY;

            int leafIdx = FindLeafAndExpand(in elt, out int d);
            LeafInsert(leafIdx, d, eltIndex);
        }

        /// <summary>
        /// Moves the specified element to a new point
        /// </summary>
        /// <param name="eltIndex">The index of the element</param>
        /// <param name="newX">The new x position for the element's bottom-left corner</param>
        /// <param name="newY">The new y position for the element's bottom-left corner</param>
        public override void MoveIndexToPoint(int eltIndex, int newX, int newY)
        {
            if (eltIndex < 1 || eltIndex > _elements.Count)
            {
                throw new IndexOutOfRangeException(
                    "Trying to move invalid index. Note elements begin at index 1, not 0");
            }

            //find the leaf that has the element
            ref LQtElement<T> elt = ref _elements[eltIndex];

            RemoveIndex(eltIndex, false);

            //update the element's position
            elt.x = newX;
            elt.y = newY;

            int leafIdx = FindLeafAndExpand(in elt, out int d);
            LeafInsert(leafIdx, d, eltIndex);
        }

        private Stack<int> _toClean = new Stack<int>(16);
        /// <summary>
        /// Cleans up the tree, removing empty leaves, then contracting nodes. 
        /// Call at the end of a frame
        /// </summary>
        public override void Cleanup()
        {
            // Only process the root if it's not a leaf.
            if (_nodes[0].fc < 0)
            {
                // Push the root index to the stack.
                _toClean.Push(0);
            }

            while (_toClean.Count > 0)
            {
                // Pop a branch node from the stack and negate the index
                // remember all branch nodes are negative
                int nodeIdx = _toClean.Pop();

                //we already know this node is a branch so its fc will be negative
                int fc = -_nodes[nodeIdx].fc;

                int emptyLeaves = 0;

                // Loop through the children.
                for (int j = 0; j < 4; ++j)
                {
                    int child = fc + j;

                    // Increment empty leaf count if the child is an empty 
                    // leaf. Otherwise if the child is a branch, add it to
                    // the stack to be processed in the next iteration. (of this while loop)
                    if (_nodes[child].fc == 0)
                    {
                        ++emptyLeaves;
                        _nodes[child] = new QtLooseNode();  //simpler than resizing
                                                            //theoretically this can cause unnecessary constructors and be slow
                                                            //if a leaf is just going to be removed, but I can't imagine it
                                                            //would be a significant performance impact in any reasonable use case
                    }
                    else if (_nodes[child].fc < 0)
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
                    _nodes[nodeIdx].fc = 0;
                }
            }
            ResizeBranches(0);
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
        /// <param name="omitElement">element index to omit</param>
        /// <returns>A <see cref="List{Int32}"/> of element indexes</returns>
        public override List<T> Query(int qlft, int qtop, int qrgt, int qbtm, int omitElement)
        {
            // Find the leaves that intersect the specified query rectangle.
            AABB query = new AABB(qlft, qtop, qrgt, qbtm);

            _queryResults.Clear();
            _usedLeaves.Clear();
            FindUsedLeaves(0, query);

            // For each leaf node, look for elements that intersect.
            for (int i = _usedLeaves.Count - 1; i >= 0; --i)
            {
                int index = _usedLeaves[i];

                // Walk the list and add elements that intersect.
                int eltIdx = _nodes[index].fc;
                while (eltIdx != -1)
                {
                    ref LQtElement<T> elt = ref _elements[eltIdx];
                    if (eltIdx != omitElement && RectOverlap(elt, query))
                    {
                        //add the element to the list
                        _queryResults.Add(elt.obj);
                    }
                    eltIdx = elt.next;
                }
            }
            return _queryResults;
        }

        private Stack<QtLooseNode> _toTraverse = new Stack<QtLooseNode>();
        /// <summary>
        /// Traverses all the nodes in the tree, calling 'branch' for branch nodes and 'leaf' for leaf nodes.
        /// </summary>
        /// <param name="visitor">The interface implementing object we'll be calling functions on</param>
        public override void Traverse(IQtVisitor visitor)
        {
            _toTraverse.Push(_nodes[0]);

            while (_toTraverse.Count > 0)
            {
                QtLooseNode node = _toTraverse.Pop();
                int fc = node.fc;

                if (fc < 0)
                {
                    fc = -fc;
                    // Push the children of the branch to the stack.
                    _toTraverse.Push(_nodes[fc + 0]);
                    _toTraverse.Push(_nodes[fc + 1]);
                    _toTraverse.Push(_nodes[fc + 2]);
                    _toTraverse.Push(_nodes[fc + 3]);
                    visitor.Branch(node.lft, node.top, node.rgt, node.btm);
                }
                else
                    visitor.Leaf(node.childCount, node.lft, node.top, node.rgt, node.btm);
            }
        }

        #region Private Methods

        /// <summary>
        /// For the results of <see cref="FindUsedLeaves(int, in AABB)"/> in <see cref="Query(int, int, int, int)"/>.
        /// Clear before using!
        /// </summary>
        private List<int> _usedLeaves = new List<int>(16);
        /// <summary>
        /// Finds all non-empty leaves in a query area and puts their indexes 
        /// in the <see cref="_usedLeaves"/> list. Make sure to clear it before use!
        /// FindOccupiedLeaves was too much to type.
        /// </summary>
        /// <param name="nodeIdx">the index of the node to start in (usually 0)</param>
        /// <param name="query">The query area</param>
        protected void FindUsedLeaves(int nodeIdx, in AABB query)
        {
            ref QtLooseNode node = ref _nodes[nodeIdx];

            if (RectOverlap(in node, in query))
            {
                if (node.IsLeaf)  //if the node is a leaf, we're done
                {
                    if (node.fc > 0)
                        _usedLeaves.Add(nodeIdx);
                    return;
                }
                else               //if not, search all the branches
                {
                    int fc = -node.fc;
                    FindUsedLeaves(fc + 0, in query);
                    FindUsedLeaves(fc + 1, in query);
                    FindUsedLeaves(fc + 2, in query);
                    FindUsedLeaves(fc + 3, in query);
                }
            }
        }

        protected static bool RectOverlap(in QtLooseNode a, in AABB b)
        {
            return RectOverlap(a.lft, a.top, a.rgt, a.btm,
                b.lft, b.top, b.rgt, b.btm);
        }

        protected static bool RectOverlap(in LQtElement<T> a, in AABB b)
        {
            return RectOverlap(a.Lft, a.Top, a.Rgt, a.Btm,
                b.lft, b.top, b.rgt, b.btm);
        }

        protected static bool RectOverlap(in AABB a, in AABB b)
        {
            return RectOverlap(a.lft, a.top, a.rgt, a.btm,
                b.lft, b.top, b.rgt, b.btm);
        }

        /// <summary>
        /// Finds the leaf where an element is, starting from the root.
        /// </summary>
        /// <param name="elt">The element you're trying to locate</param>
        /// <returns>The index of the leaf</returns>
        protected virtual int FindLeaf(in LQtElement<T> elt, out int depth)
        {
            depth = 0;
            return FindLeafRecursive(0, ref depth, in elt,
                _centerX, _centerY, _HalfWidth, _HalfHeight);
        }

        /// <summary>
        /// Finds the leaf where an element belongs, *and expands branches along the way*. 
        /// For inserting new elements. Starts from the root. 
        /// </summary>
        /// <param name="elt">The element you're inserting</param>
        /// <param name="x">The x coordinate of the point to find</param>
        /// <param name="y">The y coordinate of the point to find</param>
        /// <param name="depth">Returns the depth of the leaf found</param>
        /// <returns>The index of the leaf</returns>
        protected virtual int FindLeafAndExpand(in LQtElement<T> elt, out int depth)
        {
            depth = 0;
            return FindLeafRecursive(0, ref depth, in elt,
                _centerX, _centerY, _HalfWidth, _HalfHeight, true);
        }

        /// <summary>
        /// Finds the leaf where a given point belongs. 
        /// Should only be called from the root with <see cref="FindLeaf(in LQtElement)"/> or an overload,
        /// because the quad coordinates and sizes must be calculated in relation to the root.
        /// </summary>
        /// <param name="nodeIdx">The index of the node to start searching from</param>
        /// <param name="depth">The depth of the node to start searching. Use to get depth of
        /// the resulting leaf</param>
        /// <param name="x">The x value of the point</param>
        /// <param name="y">The y value of the point</param>
        /// <param name="nodeX">The x value of the quad's center point</param>
        /// <param name="nodeY">The y value of the quad's center point</param>
        /// <param name="nHalfX">The half size of the quad's x axis</param>
        /// <param name="nHalfY">The half size of the quad's y axis</param>
        /// <param name="expand">use to expand branch nodes as the tree is traversed, 
        /// for element insertion</param>
        /// <returns>The index of the leaf in the <see cref="_nodes"/> list</returns>
        protected virtual int FindLeafRecursive(int nodeIdx, ref int depth, in LQtElement<T> elt,
            int nodeX, int nodeY, int nHalfX, int nHalfY, bool expand = false)
        {
            int fc = _nodes[nodeIdx].fc;

            if (fc >= 0)  //if the node is a leaf, we're done
            {
                return nodeIdx;
            }
            else               //if not, figure out which quad the point is in and search that
            {
                fc = -fc;
                depth++;

                if (expand)
                    ExpandBranchNode(nodeIdx, in elt);

                //calculate the quarter size of the node, i.e. the half size of the node's quads
                int qxs = nHalfX >> 1, qys = nHalfY >> 1;
                //calculate the midpoints of the quads
                int lftX = nodeX - qxs, topY = nodeY + qys, rgtX = nodeX + qxs, btmY = nodeY - qys;

                //if the point is above the horizontal center line of the node
                if (elt.y >= nodeY)
                {
                    //if the elt is in the left half of the node's quadrant
                    if (elt.x <= nodeX)
                    {
                        return FindLeafRecursive(fc + 0, ref depth, in elt, lftX, topY, qxs, qys, expand);
                    }
                    else
                    {
                        return FindLeafRecursive(fc + 1, ref depth, in elt, rgtX, topY, qxs, qys, expand);
                    }
                }
                else
                {
                    if (elt.x <= nodeX)
                    {
                        return FindLeafRecursive(fc + 2, ref depth, in elt, lftX, btmY, qxs, qys, expand);
                    }
                    else
                    {
                        return FindLeafRecursive(fc + 3, ref depth, in elt, rgtX, btmY, qxs, qys, expand);
                    }
                }
            }
        }

        /// <summary>
        /// Stores the indexes of elements being moved from a leaf that's splitting.
        /// </summary>
        protected Stack<int> _toSplit = new Stack<int>(16);
        /// <summary>
        /// inserts an element to a leaf and splits it if necessary. Note "inserting" means 
        /// adding the element to the linked list of elements starting from the node's 
        /// <see cref="QtLooseNode.fc"/> index in the element list
        /// </summary>
        /// <param name="leafIdx">The index of the leaf to insert to</param>
        /// <param name="depth">The depth of the leaf</param>
        /// <param name="eltIdx">The index of the element to insert</param>
        protected void LeafInsert(int leafIdx, int depth, int eltIdx)
        {
            ref LQtElement<T> elt = ref _elements[eltIdx];
            ref QtLooseNode node = ref _nodes[leafIdx];

            //insert the element to the top of the node's linked list
            elt.next = node.fc;
            node.fc = eltIdx;
            //expand the leaf, initializing it if this is the first element inserted
            ExpandLeaf(ref node, in elt);

            //if the leaf is full, split it.
            if (node.childCount > MaxElements && depth < MaxDepth)
            {
                // Transfer elements from the leaf node to a temporary list of elements.
                int fc = node.fc;
                while (fc > 0)
                {
                    _toSplit.Push(fc);
                    fc = _elements[fc].next;
                }
                node.childCount = 0;

                // Allocate 4 contiguous child nodes in the node list
                // and set this node's firstChild reference to the first one (top left quad)
                // note we're negating the index first
                node.fc = -_nodes.Insert(new QtLooseNode());
                _nodes.Insert(new QtLooseNode());
                _nodes.Insert(new QtLooseNode());
                _nodes.Insert(new QtLooseNode());

                do
                {
                    int newLeaf =
                        FindLeafAndExpand(in _elements[_toSplit.Peek()], out int d);
                    LeafInsert(newLeaf, d, _toSplit.Pop());
                } while (_toSplit.Count > 0);
            }
            else
            {
                node.childCount++;
            }
        }

        /// <summary>
        /// inserts an element to a leaf and splits it if necessary. 
        /// Does not change the leaf's bounding box. Note "inserting" means 
        /// adding the element to the linked list of elements starting from the node's 
        /// <see cref="QtLooseNode.fc"/> index in the element list
        /// </summary>
        /// <param name="leafIdx">The index of the leaf to insert to</param>
        /// <param name="depth">The depth of the leaf</param>
        /// <param name="eltIdx">The index of the element to insert</param>
        protected void BulkLeafInsert(int leafIdx, int depth, int eltIdx)
        {
            ref LQtElement<T> elt = ref _elements[eltIdx];
            ref QtLooseNode node = ref _nodes[leafIdx];

            //insert the element to the top of the node's linked list
            elt.next = node.fc;
            node.fc = eltIdx;

            //if the leaf is full, split it.
            if (node.childCount > MaxElements && depth < MaxDepth)
            {
                // Transfer elements from the leaf node to a temporary list of elements.
                int fc = node.fc;
                while (fc > 0)
                {
                    _toSplit.Push(fc);
                    fc = _elements[fc].next;
                }
                node.childCount = 0;

                // Allocate 4 contiguous child nodes in the node list
                // and set this node's firstChild reference to the first one (top left quad)
                // note we're negating the index first
                node.fc = -_nodes.Insert(new QtLooseNode());
                _nodes.Insert(new QtLooseNode());
                _nodes.Insert(new QtLooseNode());
                _nodes.Insert(new QtLooseNode());

                do
                {
                    int newLeaf =
                        FindLeaf(in _elements[_toSplit.Peek()], out int d);
                    BulkLeafInsert(newLeaf, d, _toSplit.Pop());
                } while (_toSplit.Count > 0);
            }
            else
            {
                node.childCount++;
            }
        }

        /// <summary>Expands a leaf node's bounding box to fit an element inside</summary>
        /// <param name="node">The leaf node to expand</param>
        /// <param name="elt">The element being inserted</param>
        private void ExpandLeaf(ref QtLooseNode node, in LQtElement<T> elt)
        {

            if (node.childCount == 0)
            {
                //if there are no elements, the node doesn't have an AABB yet
                node.lft = elt.Lft;
                node.top = elt.Top;
                node.rgt = elt.Rgt;
                node.btm = elt.Btm;
            }
            else
            {
                //otherwise, just expand the leaf
                node.lft = Math.Min(node.lft, elt.Lft);
                node.top = Math.Max(node.top, elt.Top);
                node.rgt = Math.Max(node.rgt, elt.Rgt);
                node.btm = Math.Min(node.btm, elt.Btm);
            }
        }

        public static System.Diagnostics.Stopwatch s = new System.Diagnostics.Stopwatch();
        public static int calls;
        /// <summary>
        /// Expands a branch node's bounding box to fit a new element.
        /// Branches should always already have a size and child nodes. 
        /// </summary>
        /// <param name="nodeIdx">The node to expand</param>
        /// <param name="elt">The element being inserted</param>
        private void ExpandBranchNode(int nodeIdx, in LQtElement<T> elt)
        {
            ref QtLooseNode node = ref _nodes[nodeIdx];

            s.Start();
            node.lft = Math.Min(node.lft, elt.Lft);
            node.top = Math.Max(node.top, elt.Top);
            node.rgt = Math.Max(node.rgt, elt.Rgt);
            node.btm = Math.Min(node.btm, elt.Btm);

            s.Stop();
            ++calls;
        }

        /// <summary>
        /// Contracts a node to the smallest size that contains its children
        /// </summary>
        /// <param name="nodeIdx">The index of the node to contract</param>
        private void ContractNode(int nodeIdx)
        {
            ref QtLooseNode node = ref _nodes[nodeIdx];
            int fc = node.fc;
            AABB maxBounds = new AABB(int.MaxValue, int.MinValue, int.MinValue, int.MaxValue);

            //figure out the maximum extents of the node's children
            if (fc >= 0)
            {
                //for leaves, check all the elements in the leaf
                while (fc > 0)
                {
                    ref LQtElement<T> e = ref _elements[fc];

                    maxBounds.lft = Math.Min(maxBounds.lft, e.Lft);
                    maxBounds.top = Math.Max(maxBounds.top, e.Top);
                    maxBounds.rgt = Math.Max(maxBounds.rgt, e.Rgt);
                    maxBounds.btm = Math.Min(maxBounds.btm, e.Btm);
                    fc = e.next;
                }
            }
            else
            {
                //for branches, we just check the child nodes
                for (int i = 0; i < 4; ++i)
                {
                    ref QtLooseNode l = ref _nodes[-fc + i];

                    maxBounds.lft = Math.Min(maxBounds.lft, l.lft);
                    maxBounds.top = Math.Max(maxBounds.top, l.top);
                    maxBounds.rgt = Math.Max(maxBounds.rgt, l.rgt);
                    maxBounds.btm = Math.Min(maxBounds.btm, l.btm);
                }
            }
            //then, set the node to whatever the max bounds were
            node.lft = maxBounds.lft;
            node.top = maxBounds.top;
            node.rgt = maxBounds.rgt;
            node.btm = maxBounds.btm;
        }

        /// <summary>
        /// Contracts all the nodes of the tree from the bottom up 
        /// (which will also expand branches that haven't been expanded)
        /// </summary>
        /// <param name="nodeIdx">The node to start from</param>
        private void ResizeBranches(int nodeIdx)
        {
            int fc = _nodes[nodeIdx].fc;

            if (fc >= 0)
            {
                ContractNode(nodeIdx);
                return;
            }
            else
            {
                fc = -fc;
                ResizeBranches(fc + 0);
                ResizeBranches(fc + 1);
                ResizeBranches(fc + 2);
                ResizeBranches(fc + 3);
            }

            ContractNode(nodeIdx);
        }
        #endregion


        // ----------------------------------------------------------------------------------------
        // Quadtree Members
        // ----------------------------------------------------------------------------------------
        private readonly int _HalfWidth, _HalfHeight;
        private readonly int _centerX, _centerY;

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
        // Element fields:
        // An element is a reference to an object and four ints for the AABB containing it.
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Stores all the elements in the quadtree. Each element has 6 int fields: 
        /// an object reference, center and half sizes, and a reference to the next element in whatever leaf
        /// it's currently in
        /// </summary>
        protected LooseEltList<T> _elements = new LooseEltList<T>(1280);

        // ----------------------------------------------------------------------------------------
        // Node fields:
        // a node is just two fields indicating the location in the tree structure,
        // the rest of the spatial data is computed as needed from the parent nodes and root
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Stores all the nodes in the quadtree. The first node in this
        /// sequence is always the root.
        /// </summary>
        protected LooseNodeList _nodes = new LooseNodeList();
    }
}