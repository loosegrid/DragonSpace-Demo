using System;
using System.Runtime.InteropServices;

namespace QTExperiments.Structs
{
    /// <summary>
    /// Just four ints for a rectangle. Float constructor rounds up or down to make sure 
    /// the points given are always in the bounding box
    /// </summary>
    public struct AABB
    {
        public int lft;
        public int top;
        public int rgt;
        public int btm;

        public AABB(int l, int t, int r, int b)
        {
            lft = l;
            top = t;
            rgt = r;
            btm = b;
        }

        public AABB(float l, float t, float r, float b)
        {
            lft = (int)l;
            top = (int)Math.Ceiling(t);
            rgt = (int)Math.Ceiling(r);
            btm = (int)b;
        }
    }
}

namespace QTExperiments.QtStructs
{
    [Flags]
    public enum Quads
    {
        TLft = 1 << 0,
        TRgt = 1 << 1,
        BLft = 1 << 2,
        BRgt = 1 << 3,
        TopHalf = TLft | TRgt,
        BtmHalf = BLft | BRgt,
        LftHalf = TLft | BLft,
        RgtHalf = TRgt | BRgt
    }

    // ----------------------------------------------------------------------------------------
    // Quadtree structs:
    // The "normal" quadtree uses a very tiny memory representation for nodes.
    // The position and size are calculated as needed.
    // Elements store a bounding box
    // ----------------------------------------------------------------------------------------
    #region Qt structs

    /// <summary>
    /// DON'T USE THIS UNINITIALIZED, USE QtNode.Empty!!
    /// </summary>
    public struct QtNode
    {
        /// <summary>
        /// Index of the first child node if this node is a branch or index of the first element
        /// if this node is a leaf. Child nodes are accessed by offset from the first child.
        /// </summary>
        public int firstChild;

        /// <summary>
        /// Stores the number of elements in the node or -1 if it is not a leaf.
        /// </summary>
        public int childCount;

        /// <summary>
        /// Initialized node (firstChild = -1)
        /// </summary>
        public static QtNode Empty
        {
            get
            {
                QtNode n = new QtNode
                {
                    firstChild = -1
                };
                return n;
            }
        }
    }

    /// <summary>
    /// Node for the linked list of what elements are in a node. 
    /// Elements can be in multiple nodes
    /// </summary>
    public struct EltNode
    {
        /// <summary>
        /// The next item in the linked list, or -1 if this is the end of the list
        /// </summary>
        public int nextEnode;
        /// <summary>
        /// The index of the element in the _elements list
        /// </summary>
        public int elt;

        public EltNode(int next, int thisElt)
        {
            nextEnode = next;
            elt = thisElt;
        }
    }

    /// <summary>
    /// The data describing the location and size of a qt node. Calculated as needed in
    /// relation to the root.
    /// </summary>
    public struct QtNodeData
    {
        /// <summary>the index of the node in the list, aka its ID</summary>
        public int idx;
        /// <summary>the depth of the node in the tree (root = 0)</summary>
        public int depth;
        /// <summary>x coordinate of the center of the node's AABB.</summary>
        public int mx;
        /// <summary>y coordinate of the center of the node's AABB.</summary>
        public int my;
        /// <summary>half the width of the node</summary>
        public int sx;
        /// <summary>half the height of the node</summary>
        public int sy;

        /// <summary>
        /// Create a new node data object with given data
        /// </summary>
        /// <param name="id">The node being referenced</param>
        /// <param name="d">the depth of the node</param>
        /// <param name="midX">center x coordinate</param>
        /// <param name="midY">center y coordinate</param>
        /// <param name="halfWidth">half width</param>
        /// <param name="halfHeight">half height</param>
        public QtNodeData(int id, int d, int midX, int midY, int halfWidth, int halfHeight)
        {
            idx = id;
            depth = d;
            mx = midX;
            my = midY;
            sx = halfWidth;
            sy = halfHeight;
        }
    }

    /// <summary>
    /// An element with an ID and a bounding box. The ID is used to refer to external data
    /// </summary>
    public struct QtElement
    {
        /// <summary>the ID or index to find external data of the element</summary>
        public int id;
        /// <summary>the left edge of the element's bounding box</summary>
        public int lft;
        /// <summary>the top edge of the element's bounding box</summary>
        public int top;
        /// <summary>the right edge of the element's bounding box</summary>
        public int rgt;
        /// <summary>the btm edge of the element's bounding box</summary>
        public int btm;

        /// <summary>X coordinate of the element's bottom-left corner</summary>
        public int X { get => lft; }
        /// <summary>Y coordinate of the element's bottom-left corner</summary>
        public int Y { get => btm; }
        /// <summary>Width of the element's bounding box</summary>
        public int Width { get => rgt - lft; }
        /// <summary>Height of the element's bounding box</summary>
        public int Height { get => top - btm; }

        public QtElement(int i, int l, int t, int r, int b)
        {
            id = i;
            lft = l;
            top = t;
            rgt = r;
            btm = b;
        }
    }

    /// <summary>
    /// An element with a bounding box
    /// </summary>
    public struct QtElement<T>
    {
        /// <summary>the object being stored</summary>
        public T obj;
        /// <summary>the left edge of the element's bounding box</summary>
        public int lft;
        /// <summary>the top edge of the element's bounding box</summary>
        public int top;
        /// <summary>the right edge of the element's bounding box</summary>
        public int rgt;
        /// <summary>the btm edge of the element's bounding box</summary>
        public int btm;

        /// <summary>X coordinate of the element's bottom-left corner</summary>
        public int X { get => lft; }
        /// <summary>Y coordinate of the element's bottom-left corner</summary>
        public int Y { get => btm; }
        /// <summary>Width of the element's bounding box</summary>
        public int Width { get => rgt - lft; }
        /// <summary>Height of the element's bounding box</summary>
        public int Height { get => top - btm; }

        public QtElement(T o, int l, int t, int r, int b)
        {
            obj = o;
            lft = l;
            top = t;
            rgt = r;
            btm = b;
        }
    }

    #endregion
    // ----------------------------------------------------------------------------------------
    // Loose quadtree structs:
    // The loose quadtree uses a bounding box for nodes instead of elements.
    // Elements use a position + size representation
    // ----------------------------------------------------------------------------------------
    #region Loose structs
    /// <summary>
    /// The information for each element in the tree. The ID is used to refer to external data
    /// </summary>
    public struct LQtElement
    {
        /// <summary>The ID or index to find external data of the element</summary>
        public int id;
        /// <summary>X coordinate of the element's bottom-left corner</summary>
        public int x;
        /// <summary>Y coordinate of the element's bottom-left corner</summary>
        public int y;
        /// <summary>Width of the element's bounding box</summary>
        public int width;
        /// <summary>Height of the element's bounding box</summary>
        public int height;
        /// <summary>The next element in whatever leaf we're in, or -1 if this is last</summary>
        public int next;

        /// <summary>
        /// Creates a new element
        /// </summary>
        /// <param name="i">The element's ID for external data</param>
        /// <param name="xpos">X coordinate of the element's bottom-left corner</param>
        /// <param name="ypos">Y coordinate of the element's bottom-left corner</param>
        /// <param name="xhs">Width of the element's bounding box</param>
        /// <param name="h">Height of the element's bounding box</param>
        public LQtElement(int i, int xpos, int ypos, int w, int h)
        {
            id = i;
            x = xpos;
            y = ypos;
            width = w;
            height = h;
            next = -1;
        }

        /// <summary>The left edge of the element's bounding box</summary>
        public int Lft { get => x; }
        /// <summary>The top edge of the element's bounding box</summary>
        public int Top { get => y + height; }
        /// <summary>The right edge of the element's bounding box</summary>
        public int Rgt { get => x + width; }
        /// <summary>The bottom edge of the element's bounding box</summary>
        public int Btm { get => y; }
    }

    /// <summary>
    /// The information for each element in the tree. The ID is used to refer to external data
    /// </summary>
    public struct LQtElement<T>
    {
        /// <summary>The object the element represents</summary>
        public T obj;
        /// <summary>X coordinate of the element's bottom-left corner</summary>
        public int x;
        /// <summary>Y coordinate of the element's bottom-left corner</summary>
        public int y;
        /// <summary>Width of the element's bounding box</summary>
        public int width;
        /// <summary>Height of the element's bounding box</summary>
        public int height;
        /// <summary>The next element in whatever leaf we're in, or -1 if this is last</summary>
        public int next;

        /// <summary>
        /// Creates a new element
        /// </summary>
        /// <param name="o">The object the element represents</param>
        /// <param name="xpos">X coordinate of the element's bottom-left corner</param>
        /// <param name="ypos">Y coordinate of the element's bottom-left corner</param>
        /// <param name="xhs">Width of the element's bounding box</param>
        /// <param name="h">Height of the element's bounding box</param>
        public LQtElement(T o, int xpos, int ypos, int w, int h)
        {
            obj = o;
            x = xpos;
            y = ypos;
            width = w;
            height = h;
            next = -1;
        }

        /// <summary>X coordinate of the element's center point</summary>
        public int MidX { get => x + (width / 2); }
        /// <summary>X coordinate of the element's ccenter point</summary>
        public int MidY { get => x + (width / 2); }
        /// <summary>The left edge of the element's bounding box</summary>
        public int Lft { get => x; }
        /// <summary>The top edge of the element's bounding box</summary>
        public int Top { get => y + height; }
        /// <summary>The right edge of the element's bounding box</summary>
        public int Rgt { get => x + width; }
        /// <summary>The bottom edge of the element's bounding box</summary>
        public int Btm { get => y; }
    }

    /// <summary>
    /// A loose quadtree node, with an AABB, child count and reference to the first child.
    /// </summary>
    public struct QtLooseNode
    {
        /// <summary>
        /// The edges of the node's bounding box
        /// </summary>
        public int lft, top, rgt, btm;
        /// <summary>
        /// Stores the negative index to the first child node for branches 
        /// or the positive index to the first element for leaves. Empty leaves fc == 0
        /// </summary>
        public int fc;
        /// <summary>
        /// How many elements are in the leaf
        /// </summary>
        public int childCount;

        /// <summary>
        /// The horizontal midpoint of the node, representing a line splitting the left and right
        /// </summary>
        public int MidX
        {
            get => lft + ((rgt - lft) >> 1);
        }

        /// <summary>
        /// The vertical midpoint of the node, representing a line splitting the top and bottom
        /// </summary>
        public int MidY
        {
            get => btm + ((top - btm) >> 1);
        }

        /// <summary>
        /// Returns true if this node is a leaf with or without elements
        /// </summary>
        public bool IsLeaf
        {
            get => fc >= 0;
        }

        /// <summary>
        /// Returns true if this is a leaf node with no elements in it
        /// </summary>
        public bool EmptyLeaf
        {
            get => fc == 0;
        }

        /// <summary>
        /// Sets all the bounds of the node to the same value. Doesn't change the
        /// first child
        /// </summary>
        public void ResetBounds()
        {
            lft = top = rgt = btm = int.MinValue;
        }
    }
    #endregion
    // ----------------------------------------------------------------------------------------
    // Node and element lists:
    // These are just implementations of FreeList<T> with an explicit layout struct for the
    // elements, which saves 4 bytes of space per element.
    // ----------------------------------------------------------------------------------------
    #region Explicit Lists
    /// <summary>
    /// An indexed free list, will expand as needed. Just a <see cref="FreeList{T}"/> with
    /// an explicit layout struct to save space
    /// </summary>
    public class LooseNodeList
    {
        private FreeElement[] _data;
        private int _freeElement;    // Index of the first free slot
                                     // or -1 if the list is empty.
        public int Count { get; private set; } = 0;
        public int Capacity
        {
            get { return _data.Length; }
            private set { SetCapacity(value); }
        }

        /// <summary>
        /// Creates a new list of elements with 16 free spaces
        /// </summary>
        public LooseNodeList()
        {
            // By default reserves space for 16 elements
            _data = new FreeElement[16];
            Count = 0;
            _freeElement = -1;
        }

        /// <summary>
        /// Creates a new list of elements
        /// </summary>
        /// <param name="capacity">initial capacity of the list</param>
        public LooseNodeList(int capacity)
        {
            _data = new FreeElement[capacity];
            Count = 0;
            _freeElement = -1;
        }

        private void SetCapacity(int cap)
        {
            FreeElement[] new_array = new FreeElement[cap];
            Array.Copy(_data, 0, new_array, 0, Capacity);
            _data = new_array;
        }

        #region List Interface
        public ref QtLooseNode this[int index]
        {
            get { return ref _data[index].element; }
        }

        /// <summary>
        /// Clears the list
        /// </summary>
        public void Clear()
        {
            //note that the data is still in the underlying array
            //so memory is still reserved and the GC won't collect it. 
            //If it needs to be cleared, the list can just be set to null
            Count = 0;
            _freeElement = -1;
        }
        #endregion

        /// <summary>
        /// Inserts an element to the back of the list and returns an index to it.
        /// </summary>
        /// <returns>The index where the new element was inserted</returns>
        private int PushBack(QtLooseNode elt)
        {
            // Check if the array is full
            if (Count == Capacity)
            {
                // Use double the size for the new capacity.
                SetCapacity(Count * 2);
            }
            _data[Count].element = elt;
            return Count++;
        }

        #region Free List Interface (do not mix with stack usage; use one or the other)
        /// <summary>
        /// Inserts an element to a vacant position in the list and returns an index to it.
        /// </summary>
        /// <returns>index of the slot where the element was inserted</returns>
        public int Insert(QtLooseNode elt)
        {
            //if there's an open slot in the free list, pop that and use it
            if (_freeElement != -1)
            {
                int index = _freeElement;

                //set the free index to the next open index in the free list
                _freeElement = _data[index].next;

                //actually insert the element
                _data[index].element = elt;
                //return the index where the element was inserted
                return index;
            }
            // Otherwise insert to the back of the array.
            return PushBack(elt);
        }

        /// <summary>
        /// Removes the nth element in the list.
        /// </summary>
        /// <param name="n">The index to remove from</param>
        public void Remove(int n)
        {
            // Add the slot to the free list
            _data[n].next = _freeElement;  //Set the value of the slot to the next free element
            _freeElement = n;              //Make this slot the first free element
        }
        #endregion

        // Doesn't work with generic types :(
        /// <summary>
        /// C++ union style struct. Both fields overlap so only one will have valid data.
        /// If an element is removed, write over the data and treat it as an int index for the array.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        struct FreeElement
        {
            [FieldOffset(0)] internal int next;
            [FieldOffset(0)] internal QtLooseNode element;
        }
    }

    /// <summary>
    /// An indexed free list, will expand as needed. Just a <see cref="FreeList{T}"/> with
    /// an explicit layout struct to save space
    /// </summary>
    public class LooseEltList
    {
        private FreeElement[] _data;
        private int _freeElement;    // Index of the first free slot
                                     // or -1 if the list is empty.
        public int Count { get; private set; } = 0;
        public int Capacity
        {
            get { return _data.Length; }
            private set { SetCapacity(value); }
        }

        /// <summary>
        /// Creates a new list of elements with 16 free spaces
        /// </summary>
        public LooseEltList()
        {
            // By default reserves space for 16 elements
            _data = new FreeElement[16];
            Count = 0;
            _freeElement = -1;
        }

        /// <summary>
        /// Creates a new list of elements
        /// </summary>
        /// <param name="capacity">initial capacity of the list</param>
        public LooseEltList(int capacity)
        {
            _data = new FreeElement[capacity];
            Count = 0;
            _freeElement = -1;
        }

        private void SetCapacity(int cap)
        {
            FreeElement[] new_array = new FreeElement[cap];
            Array.Copy(_data, 0, new_array, 0, Capacity);
            _data = new_array;
        }

        #region List Interface
        public ref LQtElement this[int index]
        {
            get { return ref _data[index].element; }
        }

        /// <summary>
        /// Clears the list
        /// </summary>
        public void Clear()
        {
            //note that the data is still in the underlying array
            //so memory is still reserved and the GC won't collect it. 
            //If it needs to be cleared, the list can just be set to null
            Count = 0;
            _freeElement = -1;
        }
        #endregion

        /// <summary>
        /// Inserts an element to the back of the list and returns an index to it.
        /// </summary>
        /// <returns>The index where the new element was inserted</returns>
        private int PushBack(LQtElement elt)
        {
            // Check if the array is full
            if (Count == Capacity)
            {
                // Use double the size for the new capacity.
                SetCapacity(Count * 2);
            }
            _data[Count].element = elt;
            return Count++;
        }

        #region Free List Interface (do not mix with stack usage; use one or the other)
        /// <summary>
        /// Inserts an element to a vacant position in the list and returns an index to it.
        /// </summary>
        /// <returns>index of the slot where the element was inserted</returns>
        public int Insert(LQtElement elt)
        {
            //if there's an open slot in the free list, pop that and use it
            if (_freeElement != -1)
            {
                int index = _freeElement;

                //set the free index to the next open index in the free list
                _freeElement = _data[index].next;

                //actually insert the element
                _data[index].element = elt;
                //return the index where the element was inserted
                return index;
            }
            // Otherwise insert to the back of the array.
            return PushBack(elt);
        }

        /// <summary>
        /// Removes the nth element in the list.
        /// </summary>
        /// <param name="n">The index to remove from</param>
        public void Remove(int n)
        {
            // Add the slot to the free list
            _data[n].next = _freeElement;  //Set the value of the slot to the next free element
            _freeElement = n;              //Make this slot the first free element
        }
        #endregion

        // Doesn't work with generic types :(
        /// <summary>
        /// C++ union style struct. Both fields overlap so only one will have valid data.
        /// If an element is removed, write over the data and treat it as an int index for the array.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        struct FreeElement
        {
            [FieldOffset(0)] internal int next;
            [FieldOffset(0)] internal LQtElement element;
        }
    }

    /// <summary>
    /// An indexed free list, will expand as needed. Just a <see cref="FreeList{T}"/> with
    /// an explicit layout struct to save space
    /// </summary>
    public class LooseEltList<T>
    {
        private FreeElement[] _data;
        private int _freeElement;    // Index of the first free slot
                                     // or -1 if the list is empty.
        public int Count { get; private set; } = 0;
        public int Capacity
        {
            get { return _data.Length; }
            private set { SetCapacity(value); }
        }

        /// <summary>
        /// Creates a new list of elements with 16 free spaces
        /// </summary>
        public LooseEltList()
        {
            // By default reserves space for 16 elements
            _data = new FreeElement[16];
            Count = 0;
            _freeElement = -1;
        }

        /// <summary>
        /// Creates a new list of elements
        /// </summary>
        /// <param name="capacity">initial capacity of the list</param>
        public LooseEltList(int capacity)
        {
            _data = new FreeElement[capacity];
            Count = 0;
            _freeElement = -1;
        }

        private void SetCapacity(int cap)
        {
            FreeElement[] new_array = new FreeElement[cap];
            Array.Copy(_data, 0, new_array, 0, Capacity);
            _data = new_array;
        }

        #region List Interface
        public ref LQtElement<T> this[int index]
        {
            get { return ref _data[index].element; }
        }

        /// <summary>
        /// Clears the list
        /// </summary>
        public void Clear()
        {
            //note that the data is still in the underlying array
            //so memory is still reserved and the GC won't collect it. 
            //If it needs to be cleared, the list can just be set to null
            Count = 0;
            _freeElement = -1;
        }
        #endregion

        /// <summary>
        /// Inserts an element to the back of the list and returns an index to it.
        /// </summary>
        /// <returns>The index where the new element was inserted</returns>
        private int PushBack(LQtElement<T> elt)
        {
            // Check if the array is full
            if (Count == Capacity)
            {
                // Use double the size for the new capacity.
                SetCapacity(Count * 2);
            }
            _data[Count].element = elt;
            return Count++;
        }

        #region Free List Interface (do not mix with stack usage; use one or the other)
        /// <summary>
        /// Inserts an element to a vacant position in the list and returns an index to it.
        /// </summary>
        /// <returns>index of the slot where the element was inserted</returns>
        public int Insert(LQtElement<T> elt)
        {
            //if there's an open slot in the free list, pop that and use it
            if (_freeElement != -1)
            {
                int index = _freeElement;

                //set the free index to the next open index in the free list
                _freeElement = _data[index].next;

                //actually insert the element
                _data[index].element = elt;
                //return the index where the element was inserted
                return index;
            }
            // Otherwise insert to the back of the array.
            return PushBack(elt);
        }

        /// <summary>
        /// Removes the nth element in the list.
        /// </summary>
        /// <param name="n">The index to remove from</param>
        public void Remove(int n)
        {
            // Add the slot to the free list
            _data[n].next = _freeElement;  //Set the value of the slot to the next free element
            _freeElement = n;              //Make this slot the first free element
        }
        #endregion

        // Doesn't work with generic types :(
        /// <summary>
        /// C++ union style struct. Both fields overlap so only one will have valid data.
        /// If an element is removed, write over the data and treat it as an int index for the array.
        /// </summary>
        struct FreeElement
        {
            internal int next;
            internal LQtElement<T> element;
        }
    }

    /// <summary>
    /// An indexed free list, will expand as needed
    /// </summary>
    public class NodeList
    {
        private FreeElement[] _data;
        private int _freeElement;    // Index of the first free slot
                                     // or -1 if the list is empty.
        public int Count { get; private set; } = 0;
        public int Capacity
        {
            get { return _data.Length; }
            private set { SetCapacity(value); }
        }

        /// <summary>
        /// Creates a new list of elements with 16 free spaces
        /// </summary>
        public NodeList()
        {
            // By default reserves space for 16 elements
            _data = new FreeElement[16];
            Count = 0;
            _freeElement = -1;
        }

        /// <summary>
        /// Creates a new list of elements
        /// </summary>
        /// <param name="capacity">initial capacity of the list</param>
        public NodeList(int capacity)
        {
            _data = new FreeElement[capacity];
            Count = 0;
            _freeElement = -1;
        }

        private void SetCapacity(int cap)
        {
            FreeElement[] new_array = new FreeElement[cap];
            Array.Copy(_data, 0, new_array, 0, Capacity);
            _data = new_array;
        }

        #region List Interface
        public ref QtNode this[int index]
        {
            get { return ref _data[index].element; }
        }

        /// <summary>
        /// Clears the list
        /// </summary>
        public void Clear()
        {
            //note that the data is still in the underlying array
            //so memory is still reserved and the GC won't collect it. 
            //If it needs to be cleared, the list can just be set to null
            Count = 0;
            _freeElement = -1;
        }
        #endregion

        /// <summary>
        /// Inserts an element to the back of the list and returns an index to it.
        /// </summary>
        /// <returns>The index where the new element was inserted</returns>
        private int PushBack(QtNode elt)
        {
            // Check if the array is full
            if (Count == Capacity)
            {
                // Use double the size for the new capacity.
                SetCapacity(Count * 2);
            }
            _data[Count].element = elt;
            return Count++;
        }

        #region Free List Interface (do not mix with stack usage; use one or the other)
        /// <summary>
        /// Inserts an element to a vacant position in the list and returns an index to it.
        /// </summary>
        /// <returns>index of the slot where the element was inserted</returns>
        public int Insert(QtNode elt)
        {
            //if there's an open slot in the free list, pop that and use it
            if (_freeElement != -1)
            {
                int index = _freeElement;

                //set the free index to the next open index in the free list
                _freeElement = _data[index].next;

                //actually insert the element
                _data[index].element = elt;
                //return the index where the element was inserted
                return index;
            }
            // Otherwise insert to the back of the array.
            return PushBack(elt);
        }

        /// <summary>
        /// Removes the nth element in the list.
        /// </summary>
        /// <param name="n">The index to remove from</param>
        public void Remove(int n)
        {
            // Add the slot to the free list
            _data[n].next = _freeElement;  //Set the value of the slot to the next free element
            _freeElement = n;              //Make this slot the first free element
        }
        #endregion

        // Doesn't work with generic types :(
        /// <summary>
        /// C++ union style struct. Both fields overlap so only one will have valid data.
        /// If an element is removed, write over the data and treat it as an int index for the array.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        struct FreeElement
        {
            [FieldOffset(0)] internal int next;
            [FieldOffset(0)] internal QtNode element;
        }
    }
    #endregion
}
