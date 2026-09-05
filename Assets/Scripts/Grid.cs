using UnityEngine;
using System.Collections.Generic;
using System;



/* FUNCTIONS
	FindGroup(int startX, int startY)
	UpdateGroupSizes()
    UpdateLocalGroupSizes()
    BlastGroup(List<Vector2Int> group)
	CollapseColumns()
	SpawnNewBlocks()
*/



//block structure 
public struct Block {
    public byte colorID;
    public int groupSize; 
    public bool isEmpty; 
}



public class Grid{

    private int rowCount; // height (row) 2-10
    private int columnCount; // width (column) 2-10
    private int colors; // number of colors 1-6
    private bool hasBlastableGroup = false; // for solving the deadlock
    public HashSet<int> affectedColumns; // for collapsing just affected columns , x axis
    private byte[] activeColors;
    private Block[,] gridArray;
    private List<Vector2Int> group;
    private bool[,] tempVisited; 
    private Queue<Vector2Int> BFSQueue;


    static readonly Vector2Int[] Directions = 
    {new Vector2Int(1, 0),new Vector2Int(-1, 0),
    new Vector2Int(0, 1),new Vector2Int(0, -1)};



    // constructor for creating the table with random colors
    public Grid(int m, int n, int k){
        this.rowCount = m;
        this.columnCount = n;
        this.colors = k;

        if (k < 1 || k > 6) throw new System.ArgumentException(" Wrong color count");
        if (m < 2 || m > 10) throw new System.ArgumentException(" Wrong row count");
        if (n < 2 || n > 10) throw new System.ArgumentException(" Wrong column count");


        // red=1
        // blue=2
        // pink=3 
        // purple=4 
        // green=5 
        // yellow=6


        // randomizing colors for diversity 
        byte[] colorPool = {1,2,3,4,5,6};

        for (int i = 0; i < colorPool.Length; i++) {
            int r = UnityEngine.Random.Range(i, colorPool.Length);
            (colorPool[i], colorPool[r]) = (colorPool[r], colorPool[i]);
        }

 
        // PRE - ALLOCATIONS

        activeColors = new byte[colors];
        gridArray = new Block[columnCount, rowCount]; // x , y
        affectedColumns = new HashSet<int>();
        group = new List<Vector2Int>();
        tempVisited = new bool[columnCount, rowCount]; // [1.1], if visited = true
        BFSQueue = new Queue<Vector2Int>();

        //choose random k colors at the start and continue with it when spawning
        for (int i = 0; i < colors; i++)
        {
            activeColors[i] = colorPool[i];
        }


        for (int x = 0; x < gridArray.GetLength(0); x++) {
            for (int y = 0; y < gridArray.GetLength(1); y++) {

                gridArray[x, y] = new Block {
                    colorID =activeColors[UnityEngine.Random.Range(0, activeColors.Length)],
                    groupSize = 0, // will use it to know if it is visited or not
                    isEmpty = false
                };
            }
        }


    }




    // FUNCTIONS 

    /* Removes all blocks in the given group from the grid.
    Marks those cells as empty and records which columns are affected,
    so collapse and spawn operations can be applied only to those columns. */

    public void BlastGroup(List<Vector2Int> group){

        affectedColumns.Clear();

        foreach (Vector2Int pos in group){

            affectedColumns.Add(pos.x); 
            Block b = GetBlock(pos.x, pos.y);

            b.isEmpty = true;
            b.colorID = 0;
            b.groupSize = 0;

            SetBlock(pos.x, pos.y, b);
        }
    }



    /* Collapses blocks downward in affected columns only.
    Uses an in-place two-pointer approach to move non-empty blocks down and leave empty cells on top. */

    public void CollapseColumns(){

        foreach (int x in affectedColumns){

            int writeIndex = 0;
            for(int readIndex = 0; readIndex < rowCount; readIndex++){

                // read index should be full and write index should be at the empty block
                if (!gridArray[x, readIndex].isEmpty){ 
                    if (readIndex != writeIndex){
                        gridArray[x, writeIndex] = gridArray[x, readIndex]; 
                    }
                    writeIndex++;
                }
            }

            // make the above columns empty
            for (int y = writeIndex; y < rowCount; y++){
                gridArray[x, y].colorID = 0;
                gridArray[x, y].isEmpty = true;
                gridArray[x, y].groupSize = 0; // defaut groupsize
            }
        }
    }



    /* Fills empty cells in affected columns by spawning new blocks (random color) from top to the bottom */

    public void SpawnNewBlocks(){
        foreach (int x in affectedColumns)
        {
            // from top to the bottom
            for (int y = rowCount - 1; y >= 0; y--)
            {
                Block bl = gridArray[x, y];

                // finish after encountering at the first not empty block
                if (!bl.isEmpty)
                    break;

                // produce new block
                bl.colorID = activeColors[UnityEngine.Random.Range(0, activeColors.Length)];
                bl.isEmpty = false; // now filled
                bl.groupSize = 0; // will change after grids last situation

                gridArray[x, y] = bl;
            }
        }
    }


    /* Finds all connected blocks that have the same color as the starting block.
     Uses BFS to search up, down, left and right from (startX, startY).
     Returns the Vector2Int list of positions that belongs to the same group.  */

    public List<Vector2Int> FindGroup(int startX, int startY){

        group.Clear();
        Array.Clear(tempVisited, 0, tempVisited.Length);
        BFSQueue.Clear();

        Block startBlock = gridArray[startX, startY];

        // empty group
        if (startBlock.isEmpty)return group;

        byte startingColor = startBlock.colorID;

        //starting the BFS
        BFSQueue.Enqueue(new Vector2Int(startX, startY));
        tempVisited[startX,startY] = true;

        while(BFSQueue.Count>0){

            Vector2Int current = BFSQueue.Dequeue();
            group.Add(current);

            foreach (var dir in Directions){
                int nx = current.x + dir.x;
                int ny = current.y + dir.y;

                if (nx < 0 || nx >= columnCount || ny < 0 || ny >= rowCount)
                    continue;

                Block neighborBlock = gridArray[nx, ny];

                if (!neighborBlock.isEmpty && neighborBlock.colorID == startingColor &&!tempVisited[nx, ny]){
                    tempVisited[nx, ny] = true;
                    BFSQueue.Enqueue(new Vector2Int(nx, ny));
                }
            }

        }
        return group;
    }



    /* Updates the group size of each block on the grid.
     For every unvisited and non-empty block, it finds its connected group and assigns the group size to all blocks in that group.
     Also checks if there is at least one blastable group (size >= 2).
    Returns true if a blastable group exists, false otherwise for deadlock detection. */

    public bool UpdateGroupSizes(){

        hasBlastableGroup = false;

        //reset all blocks
        for (int x = 0; x < columnCount; x++){
            for (int y = 0; y < rowCount; y++){
                gridArray[x, y].groupSize = 0;
            }
        }

        for (int x = 0; x < columnCount; x++){
            for (int y = 0; y < rowCount; y++){

                gridArray[x, y].groupSize = 0;

                Block currentBlock = gridArray[x, y];

                if (currentBlock.groupSize == 0 && !currentBlock.isEmpty) // if not visited and full
                {
                    List<Vector2Int> currentGroup = FindGroup(x, y);

                    int currentSize = currentGroup.Count;

                    // detecting deadlock, if hasBlastableGroup = false, there is a deadlock
                    if(currentSize >= 2){
                            hasBlastableGroup = true;
                    }

                    foreach (Vector2Int bl in currentGroup)
                    {   
                        // update all elements groupsize
                        gridArray[bl.x, bl.y].groupSize = currentSize;
                    }
                }

            }
        }

        return hasBlastableGroup;
    }



    /* Recomputes group sizes only for columns affected by the last move.
    Instead of scanning the whole grid, it updates the changed columns and their immediate neighbors to improve performance. */

    public bool UpdateLocalGroupSizes(){

        hasBlastableGroup = false;

        HashSet<int> dirtyColumns = new HashSet<int>();

        foreach (int x in affectedColumns){
            dirtyColumns.Add(x);
            if (x > 0) dirtyColumns.Add(x - 1);
            if (x < columnCount - 1) dirtyColumns.Add(x + 1);
        }

        //Reset only dirty columns
        foreach (int x in dirtyColumns){
            for (int y = 0; y < rowCount; y++){
                gridArray[x, y].groupSize = 0;
            }
        }

        // Recompute groups only in dirty area
        foreach (int x in dirtyColumns){
            for (int y = 0; y < rowCount; y++){
                Block bl = gridArray[x, y];

                if (bl.isEmpty || bl.groupSize != 0)
                    continue;

                List<Vector2Int> group = FindGroup(x, y);
                int size = group.Count;

                if (size >= 2)
                    hasBlastableGroup = true;

                foreach (var pos in group){
                    gridArray[pos.x, pos.y].groupSize = size;
                }
            }
        }

        return hasBlastableGroup;
    }



    /* Tries to resolve a deadlock by randomly selecting a block and 
     matching its color with one of its neighbors to force a valid group.
     Returns the changed cell position if successful, otherwise returns null. */

    public Vector2Int? SolveDeadlock(){
        for (int attempt = 0; attempt < 50; attempt++){
            int x = UnityEngine.Random.Range(0, columnCount);
            int y = UnityEngine.Random.Range(0, rowCount);

            Block baseBlock = gridArray[x, y]; // random point

            if (baseBlock.isEmpty) continue;

            // Randomize the order to add variation in neighboor selection
            int[] order = { 0, 1, 2, 3 };
            for (int i = 0; i < order.Length; i++){
                int r = UnityEngine.Random.Range(i, order.Length);
                (order[i], order[r]) = (order[r], order[i]);
            }

            foreach (int idx in order){
                Vector2Int dir = Directions[idx];

                //choose the block
                int nx = x + dir.x;
                int ny = y + dir.y;

                if (nx < 0 || nx >= columnCount || ny < 0 || ny >= rowCount)
                    continue;

                Block neighbor = gridArray[nx, ny];
                if (neighbor.isEmpty)continue;

                baseBlock.colorID = neighbor.colorID;
                gridArray[x, y] = baseBlock;

                // for using updateLocalGroupSizes function
                affectedColumns.Clear();
                affectedColumns.Add(x);
                affectedColumns.Add(nx);

                return new Vector2Int(x, y); //
            }
        }
        return null; // if deadlock cannot be solved
    }


    //getters
    public int ColumnCount => columnCount;
    public int RowCount => rowCount;

    public Block GetBlock(int x, int y) {
        if (x >= 0 && x < columnCount && y >= 0 && y < rowCount) {
            return gridArray[x, y];
        }
        return default; 
    }

    public void SetBlock(int x, int y, Block newBlock) {
        if (x >= 0 && x < columnCount && y >= 0 && y < rowCount) {
            gridArray[x, y] = newBlock;
        }
    }


}
