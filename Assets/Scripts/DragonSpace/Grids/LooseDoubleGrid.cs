namespace DragonSpace.Grids
{
    using DragonSpace.Lists;
    using System;
    using System.Collections.Generic;
    using DragonSpace.Structs;

    public class LooseDoubleGrid
    {
        private CoarseGrid grid;

        private readonly LooseGridRow[] rows;

        /// <summary>
        /// the size of the grid
        /// </summary>
        private readonly int _width, _height;

        // Stores the number of columns and rows in the grid.
        private readonly int _numRows, _numCols;

        private readonly int _coarseRows, _coarseCols;

        /// <summary>
        /// stores the size of a cell
        /// </summary>
        private readonly float _invCellWidth, _invCellHeight;

        /// <summary>
        /// The size of all elements stored in the grid.
        /// </summary>
        private readonly float _invCoarseWidth, _invCoarseHeight;

        public LooseDoubleGrid(float cellWidth, float cellHeight, float coarseWidth, float coarseHeight,
            int gridWidth, int gridHeight)
        {
            if (cellWidth > coarseWidth || cellHeight > coarseHeight)
            {
                throw new ArgumentException("Coarse cells must be (significantly) larger than cell size");
            }

            _width = gridWidth;
            _height = gridHeight;
            _numRows = (int)(gridHeight / cellHeight) + 1;
            _numCols = (int)(gridWidth / cellWidth) + 1;
            _invCellWidth = 1 / cellWidth;
            _invCellHeight = 1 / cellHeight;
            _coarseRows = (int)(gridHeight / coarseHeight) + 1;
            _coarseCols = (int)(gridWidth / coarseWidth) + 1;
            _invCoarseWidth = 1 / coarseWidth;
            _invCoarseHeight = 1 / coarseHeight;

            //init rows
            rows = new LooseGridRow[_numRows];
            //init columns
            for (int i = 0; i < _numRows; i++)
            {
                rows[i] = new LooseGridRow(_numCols);
                for (int j = 0; j < rows[i].cells.Length; j++)
                {
                    rows[i].cells[j] = new LooseCell();
                }
            }

            grid = new CoarseGrid(_coarseCols, _coarseRows);
        }

        #region Public methods
        /// <summary>
        /// Inserts an object into the grid, expanding the cell to fit the element
        /// </summary>
        /// <param name="obj">The object to insert</param>
        public void Insert(IGridElt obj)
        {
            int xIdx = GridLocalToCellCol(obj.Xf);
            int yIdx = GridLocalToCellRow(obj.Yf);
            InsertToCell(obj, xIdx, yIdx);
        }

        /// <summary>
        /// Inserts an object into a grid cell at the given index
        /// </summary>
        /// <param name="elt">The object to insert</param>
        /// <param name="xIdx">The column index of the cell</param>
        /// <param name="yIdx">The row index of the cell</param>
        private void InsertToCell(IGridElt elt, int xIdx, int yIdx)
        {
            LooseCell cell = rows[yIdx].cells[xIdx];

            //if the cell is empty, initialize the bounds to match the element
            if (cell.FirstElt == null)
            {
                cell.Push(elt);
                cell.lft = (int)elt.Xf;
                cell.btm = (int)elt.Yf;
                cell.rgt = cell.lft + elt.Width;
                cell.top = cell.btm + elt.Height;

                //insert into the tight cells it overlaps
                InsertToCoarseGrid(cell);
            }
            else    //otherwise, see if the bounds need to change to fit the element
            {
                cell.Push(elt);
                ExpandCell(cell, elt);
            }
        }

        /// <summary>
        /// Removes an element from the grid. The <see cref="IUGridElt"/> of the object must give
        /// the same position where the element is currently in the grid
        /// </summary>
        /// <param name="obj">The object to remove</param>
        public void Remove(IGridElt obj)
        {
            int xIdx = GridLocalToCellCol(obj.Xf);
            int yIdx = GridLocalToCellRow(obj.Yf);
            RemoveFromCell(obj, xIdx, yIdx);
        }

        private void RemoveFromCell(IGridElt obj, int xIdx, int yIdx)
        {            
            LooseCell cell = rows[yIdx].cells[xIdx];

            IGridElt elt = cell.FirstElt;
            IGridElt prevElt = null;

            while (elt.ID != obj.ID)
            {
                prevElt = elt;
                elt = elt.NextElt;
            }

            if (prevElt == null)
                cell.Pop();
            else
                prevElt.NextElt = elt.NextElt;
        }

        /// <summary>
        /// Moves an element in the grid from the former position to the new one.
        /// </summary>
        /// <param name="obj">The object to move</param>
        /// <param name="fromX">The current position of the object in the grid</param>
        /// <param name="fromY">The current position of the object in the grid</param>
        /// <param name="toX">The new position of the object in the grid</param>
        /// <param name="toY">The new position of the object in the grid</param>
        public void Move(IGridElt obj, float fromX, float fromY, float toX, float toY)
        {
            int oldCol = GridLocalToCellCol(fromX);
            int oldRow = GridLocalToCellRow(fromY);
            int newCol = GridLocalToCellCol(toX);
            int newRow = GridLocalToCellRow(toY);

            ref LooseGridRow row = ref rows[oldRow];

            if (oldCol != newCol || oldRow != newRow)
            {
                RemoveFromCell(obj, oldCol, oldRow);
                InsertToCell(obj, newCol, newRow);
            }
            else
            {
                //just expand the cell if necessary, we can contract it later
                ExpandCell(rows[oldRow].cells[oldCol], obj);
            }
        }

        private readonly List<IGridElt> _queryResults = new List<IGridElt>(16);
        /// <summary>
        /// Returns all the elements that intersect the specified rectangle excluding 
        /// elements with the specified ID to omit.
        /// </summary>
        /// <param name="omitEltID">The ID of the element from the <see cref="IGridElt"/> interface to omit from results</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="IGridElt"/>s</returns>
        public List<IGridElt> Query(float lft, float btm, float rgt, float top, int omitEltID = int.MinValue)
        {
            // Find the coarse cells that intersect the search query.
            int minX = GridLocalToCoarseCol(lft);
            int minY = GridLocalToCoarseRow(btm);
            int maxX = GridLocalToCoarseCol(rgt);
            int maxY = GridLocalToCoarseRow(top);

            AABB query = new AABB(lft, top, rgt, btm);

            _queryResults.Clear();

            for (int y = minY; y <= maxY; ++y)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    FreeLinkedList<LooseCell> cells = grid.cells[y][x].looseCells;
                    if (cells.Count == 0)
                        continue;

                    //go through the linked list of loose cells
                    FreeLinkedList<LooseCell>.FreeElement cNode = cells.FirstItem;
                    for (int i = cells.Count - 1; i >= 0; --i)
                    {
                        LooseCell cell = cNode.element;
                        if (RectOverlap(in query, in cell))
                        {
                            // check elements in cell
                            IGridElt elt = cell.FirstElt;
                            while (elt != null)
                            {
                                if (RectOverlap(in query, in elt) && elt.ID != omitEltID)
                                    _queryResults.Add(elt);
                                elt = elt.NextElt;
                            }
                        }
                        cNode = cells[cNode.next];
                    }
                }
            }
            return _queryResults;
        }

        /// <summary>
        /// Contracts all the loose cell boundaries and removes them from coarse cells.
        /// Run at the end of every frame or update
        /// </summary>
        public void TightenUp()
        {
            //remove all loose cells from the coarse grid
            for (int y = 0; y < _coarseRows; ++y)
            {
                for (int x = 0; x < _coarseCols; ++x)
                {
                    grid.cells[y][x].looseCells.Clear();
                }
            }

            //contract all loose cells, then add back to the coarse grid
            for (int i = rows.Length - 1; i >= 0; i--)
            {
                LooseCell[] row = rows[i].cells;
                for (int j = row.Length - 1; j >= 0; j--)
                {
                    LooseCell cell = row[j];
                    ContractCell(cell);
                    InsertToCoarseGrid(cell);
                }
            }
        }

        /// <summary>
        /// Calls <see cref="ILooseGridVisitor.CoarseGrid(float, float, float, float)"/> with the grid data
        /// Then traverses the whole grid and calls <see cref="IUniformGridVisitor.Cell(int, int)"/>
        /// on any non-empty cells.
        /// </summary>
        public void Traverse(ILooseGridVisitor visitor)
        {
            visitor.CoarseGrid(_width, _height, 1f / _invCoarseWidth, 1f / _invCoarseHeight);

            visitor.LooseGrid(_width, _height, 1f / _invCellWidth, 1f / _invCellHeight);

            //go through the grid
            for (int y = 0; y < _coarseRows; ++y)
            {
                for (int x = 0; x < _coarseCols; ++x)
                {
                    visitor.CoarseCell(grid.cells[y][x].looseCells.Count, 
                        x, y, 1f/ _invCoarseWidth, 1f / _invCoarseHeight);
                }
            }

            for (int i = rows.Length - 1; i >= 0; i--)
            {
                LooseCell[] row = rows[i].cells;
                for (int j = row.Length - 1; j >= 0; j--)
                {
                    LooseCell cell = row[j];
                    visitor.LooseCell(
                        cell.FirstElt, new AABB(cell.lft, cell.top, cell.rgt, cell.btm));
                }
            }
        }
        #endregion

        #region Private methods
        private void InsertToCoarseGrid(LooseCell cell)
        {
            int minX = GridLocalToCoarseCol(cell.lft);
            int minY = GridLocalToCoarseRow(cell.btm);
            int maxX = GridLocalToCoarseCol(cell.rgt);
            int maxY = GridLocalToCoarseRow(cell.top);

            for (int y = minY; y <= maxY; ++y)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    grid.cells[y][x].looseCells.InsertFirst(cell);
                }
            }
        }

        private void ExpandCell(LooseCell cell, IGridElt elt)
        {
            int xMin1 = GridLocalToCoarseCol(cell.lft);
            int yMin1 = GridLocalToCoarseRow(cell.btm);
            int xMax1 = GridLocalToCoarseCol(cell.rgt);
            int yMax1 = GridLocalToCoarseRow(cell.top);

            int eLft = (int)elt.Xf;
            int eBtm = (int)elt.Yf;

            cell.lft = Math.Min(cell.lft, eLft);
            cell.btm = Math.Min(cell.btm, eBtm);
            cell.rgt = Math.Max(cell.rgt, eLft + elt.Width);
            cell.top = Math.Max(cell.top, eBtm + elt.Height);

            int xMax2 = GridLocalToCoarseCol(cell.rgt);
            int yMax2 = GridLocalToCoarseRow(cell.top);

            //insert into new coarse cells
            int xdiff = (xMax2 > xMax1) ? 1 : 0;
            if (xMax1 != xMax2 || yMax1 != yMax2)
            {
                for (int y = yMin1; y <= yMax2; ++y)
                {
                    //if in an old row, only do the new columns,
                    //otherwise do the whole row
                    int x = (y > yMax1) ? xMin1 : xMax1 + xdiff;
                    for (; x <= xMax2; ++x)
                    {
                        grid.cells[y][x].looseCells.InsertFirst(cell);
                    }
                }
            }
        }

        private void ContractCell(LooseCell cell)
        {
            cell.lft = cell.btm = int.MaxValue;
            cell.rgt = cell.top = int.MinValue;
            IGridElt elt = cell.FirstElt;
            if (elt != null)
            {
                cell.lft = (int)elt.Xf;
                cell.btm = (int)elt.Yf;
                cell.rgt = cell.lft + elt.Width;
                cell.top = cell.btm + elt.Height;

                elt = elt.NextElt;
                while (elt != null)
                {
                    int eLft = (int)elt.Xf;
                    int eBtm = (int)elt.Yf;

                    cell.lft = Math.Min(cell.lft, eLft);
                    cell.btm = Math.Min(cell.btm, eBtm);
                    cell.rgt = Math.Max(cell.rgt, eLft + elt.Width);
                    cell.top = Math.Max(cell.top, eBtm + elt.Height);

                    elt = elt.NextElt;
                }
            }
        }

        private int GridLocalToCoarseRow(float y)
        {
            if (y <= 0) { return 0; }
            return Math.Min((int)(y * _invCoarseHeight), _coarseRows - 1);
        }

        private int GridLocalToCoarseCol(float x)
        {
            if (x <= 0) { return 0; }
            return Math.Min((int)(x * _invCoarseWidth), _coarseCols - 1);
        }

        // Returns the grid cell Y index for the specified position.
        private int GridLocalToCellRow(float y)
        {
            if (y <= 0) { return 0; }
            return Math.Min((int)(y * _invCellHeight), _numRows - 1);
        }

        // Returns the grid cell X index for the specified position.
        private int GridLocalToCellCol(float x)
        {
            if (x <= 0) { return 0; }
            return Math.Min((int)(x * _invCellWidth), _numCols - 1);
        }

        //TODO: move somewhere more useful

        private static bool RectOverlap(in AABB a, in IGridElt b)
        {
            int bLft = (int)b.Xf;
            int bBtm = (int)b.Yf;
            return RectOverlap(a.lft, a.top, a.rgt, a.btm, 
                bLft, bBtm + b.Height, bLft + b.Width, bBtm);
        }

        private static bool RectOverlap(in AABB a, in LooseCell b)
        {
            return RectOverlap(a.lft, a.top, a.rgt, a.btm, b.lft, b.top, b.rgt, b.btm);
        }

        private static bool RectOverlap(int l1, int t1, int r1, int b1,
                                         int l2, int t2, int r2, int b2)
        {
            return l2 <= r1 && r2 >= l1 && t2 >= b1 && b2 <= t1;
        }

        private static bool RectOverlap(float l1, float t1, float r1, float b1,
                                         float l2, float t2, float r2, float b2)
        {
            return l2 <= r1 && r2 >= l1 && t2 >= b1 && b2 <= t1;
        }
        #endregion

        //=============================================
        #region child classes

        private class CoarseGrid
        {
            public readonly CoarseCell[][] cells;

            public CoarseGrid(int cellsWide, int cellsHigh)
            {
                int cellsPerCell = cellsWide * cellsHigh;
                cells = new CoarseCell[cellsHigh][];
                for (int y = cellsHigh - 1; y >= 0; --y)
                {
                    cells[y] = new CoarseCell[cellsWide];
                    for (int x = cellsWide - 1; x >= 0; --x)
                    {
                        cells[y][x] = new CoarseCell(cellsPerCell);
                    }
                }
            }
        }

        private class CoarseCell
        {
            /// <summary>
            /// A singly linked list of all the loose cells in this tight cell
            /// </summary>
            public readonly FreeLinkedList<LooseCell> looseCells;

            public CoarseCell()
            {
                looseCells = new FreeLinkedList<LooseCell>();
            }

            /// <param name="cap">How much space to allocate. Should be the number of loose cells that the 
            /// coarse cell spans, or slightly more.</param>
            public CoarseCell(int cap)
            {
                looseCells = new FreeLinkedList<LooseCell>(cap);
            }
        }

        //TODO: This would be faster as a struct b/c data locality
        //      but doesn't work with the way I'm inserting them into coarse cells
        //      not worth addressing now, this way works and that's good enough.
        //      maybe try out the one big list method since that would reduce copies?
        //      then query overlapping cells and put them in a stack 
        //      before checking for overlapping elements
        private class LooseCell
        {
            /// <summary>
            /// The first element in the linked list for this cell, 
            /// the rest are accessed by <see cref="IGridElt.NextElt"/>
            /// </summary>
            public IGridElt FirstElt { get; set; }

            /// <summary>X coordinate of the cell's bottom-left corner</summary>
            public int lft;
            /// <summary>Y coordinate of the cell's bottom-left corner</summary>
            public int btm;
            /// <summary>Width of the cell's bounding box</summary>
            public int rgt;
            /// <summary>Height of the cell's bounding box</summary>
            public int top;

            public bool NoSize
            {
                get => lft == int.MaxValue && btm == int.MaxValue &&
                    rgt == int.MinValue && top == int.MinValue;
            }

            public void Push(IGridElt elt)
            {
                elt.NextElt = FirstElt;
                FirstElt = elt;
            }

            public void Pop()
            {
                FirstElt = FirstElt.NextElt;
            }
        }

        private struct LooseGridRow
        {
            // Stores all the loose cells in the row. 
            // Each cell stores the first element in that cell, 
            // which points to the next in the elts list.
            public LooseCell[] cells;

            public LooseGridRow(int cellsWide)
            {
                cells = new LooseCell[cellsWide];
            }
        }
    }
    #endregion
}