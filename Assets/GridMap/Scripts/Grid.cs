using UnityEngine;
using System.Collections.Generic;
using System;



// grid class for instantiating a 2d array with different id's showing its color id. 
// just for math calculations, does not include any visualization 


/* BOARD STATE MANAGEMENT 
	• FindGroup
	• UpdateGroupSizes (for all grid at the start)
    • solveDeadlock
    • BlastGroup
	• CollapseColumns
	• SpawnNewBlocks
*/

// should look at: deadlock ; rather than visited as group size = 0 should implement bool[,] visited

/* 2. Küçük Bir İpucu: UpdateGroupSizes Performansı
Şu anki UpdateGroupSizes metodunda her yeni grup bulduğunda FindGroup çağırıyorsun. FindGroup kendi içinde her seferinde bool[,] visited dizisi oluşturuyor.

Mühendislik Önerisi: Eğer performans sorunu yaşarsan (çok büyük gridlerde),
 visited dizisini UpdateGroupSizes başında bir kez oluşturup FindGroup metoduna parametre olarak gönderebilirsin. 
 Böylece her hücre için sürekli yeni bellek tahsis edilmemiş olur.*/





//block structure 

public struct Block {
    public byte colorID;
    public int groupSize; // 
    public bool isEmpty; //
}




public class Grid{

    private int rowCount; // height (row) 2-10
    private int columnCount; // width (column) 2-10
    private int colors; // number of colors 1-6
    private bool hasBlastableGroup = false; // for solving the deadlock
    private HashSet<int> affectedColumns; // for collapsing just affected columns , x axis
    private byte[] activeColors;
    private Block[,] gridArray;
    private List<Vector2Int> group;
    private bool[,] tempVisited; 

    //private float cellsize = 1f; //gonna change this for fitting every mobile phone later

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


        // ALLOCATIONS

        activeColors = new byte[colors];
        gridArray = new Block[columnCount, rowCount]; // x , y
        affectedColumns = new HashSet<int>();
        group = new List<Vector2Int>();
        tempVisited = new bool[columnCount, rowCount]; // [1.1], if visited = true

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
                //Debug.Log($"Hücre oluşturuldu: {x}, {y} - Renk ID: {gridArray[x,y].colorID}");
            }
        }

    }




    // FUNCTIONS 


    // first we should find the neighbour groups by iterating through grid and update their group size
    // return type = group array 2d vector list
    // parameters : startX and startY for 

    public List<Vector2Int> FindGroup(int startX, int startY){

        group.Clear();
        Array.Clear(tempVisited, 0, tempVisited.Length);

        // boolean array for storing visited information
        //bool[,] visited = new bool[columnCount, rowCount]; // [1.1], if visited = true

        Block startBlock = gridArray[startX, startY];

        // empty group
        if (startBlock.isEmpty)return group;

        byte startingColor = startBlock.colorID;
        Queue<Vector2Int> BFSQueue = new Queue<Vector2Int>();

        //starting the BFS
        BFSQueue.Enqueue(new Vector2Int(startX, startY));
        tempVisited[startX,startY] = true;

        while(BFSQueue.Count>0){

            Vector2Int current = BFSQueue.Dequeue();
            group.Add(current);

            Vector2Int[] neighbors = {
                new Vector2Int(current.x + 1, current.y),
                new Vector2Int(current.x - 1, current.y),
                new Vector2Int(current.x, current.y + 1),
                new Vector2Int(current.x, current.y - 1)
            };

            foreach (Vector2Int neighbor in neighbors){

                if (neighbor.x < 0 || neighbor.x >= columnCount || neighbor.y < 0 || neighbor.y >= rowCount)continue; //out of bounds

                Block neighborBlock = gridArray[neighbor.x, neighbor.y];

                if(neighborBlock.colorID == startingColor
                && !tempVisited[neighbor.x, neighbor.y] 
                && !neighborBlock.isEmpty){

                    tempVisited[neighbor.x, neighbor.y] = true;
                    BFSQueue.Enqueue(neighbor);

                }
            }

        }

        return group;
    }



    public void UpdateGroupSizes(){

        hasBlastableGroup = false;
        ResetGroupSize();

        for (int x = 0; x < columnCount; x++){

            for (int y = 0; y < rowCount; y++){

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

        if (hasBlastableGroup == false){
            SolveDeadlock();
            
        }
    }



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



    // In-Place algorithm, just on grid, just on affected columns by clicking, 2 pointers for writing and reading
    public void CollapseColumns(){

        foreach (int x in affectedColumns){

            int writeIndex = 0;
            for(int readIndex = 0; readIndex < rowCount; readIndex++){

                // read index should be full and write index should be at the empty block
                if (!gridArray[x, readIndex].isEmpty){ 
                    if (readIndex != writeIndex){
                        gridArray[x, writeIndex] = gridArray[x, readIndex]; //copy the information to
                    }
                    writeIndex++;
                }
            }

            // make the above columns empty
            for (int y = writeIndex; y < rowCount; y++)
            {
                gridArray[x, y].colorID = 0;
                gridArray[x, y].isEmpty = true;
                gridArray[x, y].groupSize = 0; // defaut groupsize
            }
        }
    }



    // fill the empty blocks with random colors

    public void SpawnNewBlocks()
    {
        foreach (int x in affectedColumns)
        {
            // from to the bottom
            for (int y = rowCount - 1; y >= 0; y--)
            {
                Block b = gridArray[x, y];

                // finish after encountering at the first not empty block
                if (!b.isEmpty)
                    break;

                // produce new block
                b.colorID = activeColors[UnityEngine.Random.Range(0, activeColors.Length)];
                b.isEmpty = false; // now filled
                b.groupSize = 0; // will change after grids last situation

                gridArray[x, y] = b;
            }
        }
    }




    // Gonna look here later

    private void SolveDeadlock(){
        Debug.Log("there is a deadlock");
        
    }




    // HELPER FUNCTIONS


    private void ResetGroupSize(){
         for (int x = 0; x < columnCount; x++){
            for (int y = 0; y < rowCount; y++){
                gridArray[x, y].groupSize = 0;
            }
        }
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
