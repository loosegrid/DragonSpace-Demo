using System;
using System.Collections.Generic;

namespace QTExperiments.Lists
{
    /// <summary>
    /// Creates a pool of generic lists of type T to avoid GC allocations. Lists must be returned
    /// by reference. A memory leak will result if the reference is lost (using new List()) or not returned.
    /// </summary>
    /// <typeparam name="T">The type to use for the lists</typeparam>
    public class ListPool<T>
    {
        private readonly Stack<List<T>> _pool;
        private int Capacity { get; set; }

        public ListPool()
        {
            _pool = new Stack<List<T>>(4);  //create the pool
            _pool.Push(new List<T>());      //start with just one list
        }

        /// <param name="capacity">How many lists to start with</param>
        public ListPool(int capacity) : this()
        {
            Init(capacity, 8);
        }

        /// <param name="capacity">How many lists to start with</param>
        /// <param name="listLength">How long each list should start</param>
        public ListPool(int capacity, int listLength) : this()
        {
            Init(capacity, listLength);
        }

        private void Init(int cap, int listLength)
        {
            ExpandPool(cap, listLength);  // TODO: this seems redundant huh
        }

        private void ExpandPool(int newSize)
        {
            ExpandPool(newSize, 8);
        }

        private void ExpandPool(int newSize, int listLength)
        {
            if (newSize < _pool.Count) { return; }
            Capacity = newSize;
            while (_pool.Count < newSize)
            {
                _pool.Push(new List<T>(listLength));
            }
        }

        /// <summary>
        /// Returns a <see cref="List{T}"/> from a pool. Be a good citizen and
        /// return with <see cref="ReturnList(List{T})"/>
        /// </summary>
        /// <returns>An empty <see cref="List{T}"/></returns>
        public List<T> RentList()
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }
            else
            {
                return new List<T>();
            }
        }

        /// <summary>
        /// Returns the list to the pool and clears its data
        /// </summary>
        /// <param name="li">The list rented with <see cref="RentList"/></param>
        public void ReturnList(List<T> li)
        {
            if (_pool.Count < Capacity)
            {
                li.Clear();
                _pool.Push(li);
            }
            //just let it get garbage collected if we have more than we need
        }
    }

    /// <summary>
    /// An indexed free list, will expand as needed
    /// </summary>
    /// <typeparam name="T">The struct or class type to list</typeparam>
    public class FreeList<T>
    {
        private FreeElement[] _data;
        private int _freeElement;    // Index of the first free slot or -1 if there are
                                     // no free slots before the end of the list

        public int Count { get; private set; } = 0;
        public int Capacity
        {
            get { return _data.Length; }
            private set
            {
                if (value > _data.Length)
                {
                    ExpandCapacity(value);
                }
            }  //TODO: handle reducing capacity?
        }

        /// <summary>
        /// Creates a new list of elements with 4 free spaces
        /// </summary>
        public FreeList()
        {
            _data = new FreeElement[4];
            Count = 0;
            _freeElement = -1;
        }

        /// <summary>
        /// Creates a new list of elements
        /// </summary>
        /// <param name="capacity">initial capacity of the list</param>
        public FreeList(int capacity)
        {
            _data = new FreeElement[capacity];
            Count = 0;
            _freeElement = -1;
        }

        private void ExpandCapacity(int cap)
        {
            FreeElement[] new_array = new FreeElement[cap];
            Array.Copy(_data, 0, new_array, 0, Capacity);
            _data = new_array;
        }

        //TODO: ReduceCapacity()

        #region List Interface
        public ref T this[int index]
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
            //If it needs to be cleared, the list can just be resized with new
            Count = 0;
            _freeElement = -1;
        }
        #endregion

        /// <summary>
        /// Inserts an element to the back of the list and returns an index to it.
        /// Expands the array if necessary.
        /// </summary>
        /// <returns>The index where the new element was inserted</returns>
        private int PushBack(T elt)
        {
            // Check if the array is full
            if (Count == Capacity)
            {
                // Use double the size for the new capacity.
                ExpandCapacity(Count * 2);
            }
            _data[Count].element = elt;
            return Count++;
        }

        #region Free List Interface
        /// <summary>
        /// Inserts an element to a vacant position in the list and returns an index to it.
        /// </summary>
        /// <returns>index of the slot where the element was inserted</returns>
        public int Insert(T elt)
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
        public void RemoveAt(int n)
        {
            // Add the slot to the free list
            _data[n].element = default;
            _data[n].next = _freeElement;  //Set the value of the slot to the next free slot
            _freeElement = n;              //Make this slot the first free slot
        }
        #endregion

        // Doesn't work with generic types :(
        ///// <summary>
        ///// C++ union style struct. Both fields overlap so only one will have valid data.
        ///// If an element is removed, write over the data and treat it as an int index for the array.
        ///// </summary>
        //[StructLayout(LayoutKind.Explicit)]
        //struct FreeElement
        //{
        //    [FieldOffset(0)] internal int next;
        //    [FieldOffset(0)] internal T element;
        //}

        /// <summary>
        /// Either the element or a reference to the next free slot
        /// </summary>
        private struct FreeElement
        {
            internal int next;
            internal T element;
        }
    }

    /// <summary>
    /// A singly-linked list in an array managed as a free list. 
    /// Indexed by position in the free list array.
    /// </summary>
    /// <typeparam name="T">The class type to list</typeparam>
    public class FreeLinkedList<T>
    {
        private FreeElement[] _data;
        private int _head;         // the index of the first element in the linked list
        private int _freeSlot;    // Index of the first free slot or -1 if there
                                  // are no free slots before the end of the list
                                  /// <summary>The number of elements in the linked list</summary>
        public int Count { get; private set; } = 0;
        public int Capacity
        {
            get { return _data.Length; }
            private set
            {
                if (value > _data.Length)
                {
                    ExpandCapacity(value);
                }
            }  //TODO: handle reducing capacity?
        }
        public FreeElement FirstItem
        {
            get => this[_head];
        }

        /// <summary> Creates a new list of elements with 4 free spaces </summary>
        public FreeLinkedList()
        {
            _data = new FreeElement[4];
            _data[0].next = -1;         //not sure if I need to do this but can't hurt
            Count = 0;
            _head = 0;
            _freeSlot = -1;
        }

        /// <summary>Creates a new list of elements</summary>
        /// <param name="capacity">initial capacity of the list</param>
        public FreeLinkedList(int capacity)
        {
            _data = new FreeElement[capacity];
            _data[0].next = -1;         //not sure if I need to do this but can't hurt
            Count = 0;
            _head = 0;
            _freeSlot = -1;
        }

        private void ExpandCapacity(int cap)
        {
            FreeElement[] new_array = new FreeElement[cap];
            Array.Copy(_data, 0, new_array, 0, Capacity);
            _data = new_array;
        }

        //TODO: ReduceCapacity()

        #region List Interface
        public ref FreeElement this[int index]   //use by ref at your own risk!!
        {
            get { return ref _data[index]; }
        }

        /// <summary>Clears the list</summary>
        public void Clear()
        {
            //note that the data is still in the underlying array
            Count = 0;
            _head = 0;
            _freeSlot = -1;
        }
        #endregion

        #region Free list interface
        /// <summary>
        /// Inserts an element to the back of the list and returns an index to it.
        /// Expands the array if necessary.
        /// </summary>
        /// <returns>The index where the new element was inserted</returns>
        private int FreePush(T elt)
        {
            // Check if the array is full
            if (Count == Capacity)
            {
                // Use double the size for the new capacity.
                ExpandCapacity(Count * 2);
            }
            _data[Count].element = elt;  //insert this to the back of the array
            return Count++;
        }

        /// <summary>
        /// Inserts an element to a vacant position in the list and returns an index to it.
        /// </summary>
        /// <returns>index of the slot where the element was inserted</returns>
        private int FreeInsert(T elt)
        {
            //if there's an open slot in the free list, pop that and use it
            if (_freeSlot != -1)
            {
                int index = _freeSlot;

                //set the free index to the next open index in the free list
                _freeSlot = _data[index].next;

                //actually insert the element
                _data[index].element = elt;
                _data[index].next = _head;
                _head = index;
                //return the index where the element was inserted
                return index;
            }
            // Otherwise insert to the back of the array.
            return FreePush(elt);
        }
        #endregion

        #region Linked List Interface

        /// <summary>
        /// Inserts a new element at the front of the linked list
        /// </summary>
        /// <param name="obj">The element to insert</param>
        /// <returns>The index where the element was inserted</returns>
        public int InsertFirst(T obj)
        {
            int idx = FreeInsert(obj);      //put it into the array
            _data[idx].next = _head;        //put it on top of the linked list
            _head = idx;                    //keep track of the start of the list
            return idx;
        }

        /// <summary>
        /// Inserts a new element after a specified element in the linked list
        /// </summary>
        /// <param name="obj">The element to insert</param>
        /// <param name="idx">The index of the element to insert after</param>
        /// <returns>The index where the element was inserted 
        /// (this is the same regardless of where it is in the linked list)</returns>
        public int InsertAfter(T obj, int idx)
        {
            int newIdx = FreeInsert(obj);
            _data[newIdx].next = _data[idx].next;
            _data[idx].next = newIdx;
            return newIdx;
        }

        /// <summary>
        /// Inserts a new element before a specified element in the linked list. 
        /// Takes O(n) where n is the position in the linked list (not the underlying array)
        /// </summary>
        /// <param name="obj">The element to insert</param>
        /// <param name="idx">The array index of the element to insert before</param>
        /// <returns>The index where the element was inserted 
        /// (this is the same regardless of where it is in the linked list)</returns>
        public int InsertBefore(T obj, int idx)
        {
            int newIdx = FreeInsert(obj);   //put the element in the array

            //if the index is the head, lucky us
            if (idx == _head)
            {
                _data[newIdx].next = _head;        //put it on top of the linked list
                _head = newIdx;                    //keep track of the start of the list
                return newIdx;
            }

            //otherwise, walk the list unti we find the insertion point
            int prev = _head;
            int next = _data[prev].next;
            while (next != idx)                //find the previous index to the insertion point
            {
                if (next < 0)
                { throw new IndexOutOfRangeException("The index was not found in the linked list"); }
                prev = next;
                next = _data[prev].next;
            }
            _data[newIdx].next = idx;       //link the element between the index and previous elt
            _data[prev].next = newIdx;
            return newIdx;
        }

        /// <summary>
        /// Removes the nth element in the array, which could break the linked list
        /// </summary>
        /// <param name="n">The array index to erase</param>
        public void Erase(int n)
        {
            // Add the slot to the free list
            _data[n].element = default;
            _data[n].next = _freeSlot;  //Set the value of the slot to the next free slot
            _freeSlot = n;              //Make this slot the first free slot
        }

        /// <summary>
        /// Removes the nth element in the linked list
        /// </summary>
        /// <param name="n">The array index of the preceding element in the linked list</param>
        public void RemoveAfter(int n)
        {
            int idx = _data[n].next;
            _data[n].next = _data[idx].next;
            Erase(idx);
        }

        /// <summary>
        /// Removes the nth element in the linked list. 
        /// O(n) time where n is the element's position in the list (not the array)
        /// </summary>
        /// <param name="n">The array index of the element to remove</param>
        public void Remove(int n)
        {
            //if the index is the head, lucky us
            if (n == _head)
            {
                _head = _data[n].next;        //put it on top of the linked list
                return;
            }

            //otherwise, walk the list until we find the element before n
            int prev = _head;
            int next = _data[prev].next;
            while (next != n)                //find the index before n in the linked list
            {
                if (next < 0)
                { throw new IndexOutOfRangeException("The index was not found in the linked list"); }
                prev = next;
                next = _data[prev].next;
            }
            _data[prev].next = _data[n].next;
            Erase(n);
        }
        #endregion

        /// <summary>
        /// Either the element and the next element in the linked list,
        /// or null and a reference to the next free slot in the free list
        /// </summary>
        public struct FreeElement
        {
            //we're gonna use this like a union, so it's either the next free slot
            //or the next element in the linked list
            internal int next;
            internal T element;
        }
    }
}