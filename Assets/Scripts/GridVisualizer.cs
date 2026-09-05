using UnityEngine;
using System.Collections.Generic;
using System.Collections;


/* FUNCTIONS
    Init(Grid grid, int m, int n, int a, int b, int c) 
    CreatePrefabs()                                  
    UpdateAllSprites()                                  
    BlastVisual(List<Vector2Int> group)                
    CollapseVisual(HashSet<int> columns)              
    SpawnVisual(HashSet<int> columns)                   
    UpdateGroupIfReady(int x, int y)                  
    ApplyIcon(int x, int y)                             
    AdjustCamera()                                      
    GridToWorld(int x, int y)                           
*/



public class GridVisualizer : MonoBehaviour{

    public GameObject blockPrefab; // The base template for all blocks
    public BlockData[] blockDatas; // ScriptableObject array containing sprites for colors and group icons

    private Grid grid;
    private int M, N, A, B, C;
    private Vector2 blockSize; // Extracted sprite size for precise positioning
    private BlockView[,] views; // 2D Array to track the visual representation of blocks

    private int lastUpdateFrame = -1;
    private HashSet<Vector2Int> processedThisFrame = new HashSet<Vector2Int>();

    [SerializeField] private BlockPool blockPool; // Reference to the object pooling system

    // Counters to synchronize asynchronous animations
    private int movingCollapseCount;
    private int movingSpawnCount;





    /* Initializes the visualizer with core logic and game thresholds.
    Sets up the view array and determines block dimensions. */
    
    public void Init(Grid grid, int m, int n, int a, int b, int c){
        this.grid = grid;
        M = m;
        N = n;
        A = a;
        B = b;
        C = c;

        views = new BlockView[N, M];
        blockSize = blockDatas[1].def.bounds.size; //x = 2.25 and y = 2.56, from sprite
    }





   /* Spawns initial block game objects and the view array.
    This is typically called once during the initialization phase.*/

    public void CreatePrefabs(){
        for (int x = 0; x < N; x++){
            for (int y = 0; y < M; y++){
                GameObject go = Instantiate(blockPrefab, transform);
            
                BlockView view = go.GetComponent<BlockView>();
                view.Init(x, y); 

                go.transform.position = GridToWorld(x, y);
                views[x, y] = view;
            }
        }
    }



    /* Synchronizes the entire visual grid with the current logic state.
    Evaluates group sizes to assign appropriate sprites. */
    
    public void UpdateAllSprites(){
        for (int x = 0; x < N; x++){
            for (int y = 0; y < M; y++){

                Block block = grid.GetBlock(x, y);
                BlockView view = views[x, y];
                if (view == null) continue;

                if (block.isEmpty){
                    view.SetVisible(false);
                    continue;
                }

                BlockData data = blockDatas[block.colorID];

                if (block.groupSize > C)
                    view.SetSprite(data.C);
                else if (block.groupSize > B)
                    view.SetSprite(data.B);
                else if (block.groupSize > A)
                    view.SetSprite(data.A);
                else
                    view.SetSprite(data.def);
            }
        }
    }


    /* Handles the visual destruction of a group.
    Disconnects the views from the grid array and returns objects to the pool.*/

    public void BlastVisual(List<Vector2Int> group){

        foreach (Vector2Int pos in group){

            BlockView view = views[pos.x, pos.y];
            if (view != null){

                // blast that position
                views[pos.x, pos.y] = null; 

                view.BlastView();
                view.OnBlastFinished = HandleBlastFinished;
            }
        }
    }

    // Callback triggered when a block's blast animation is complete.

    private void HandleBlastFinished(BlockView view){
        view.OnBlastFinished = null; 
        blockPool.releaseBlock(view.gameObject);
    }





    /* Visualizes the downward movement of blocks after a blast.
    /// Uses a Read/Write index logic to ensure smooth transitions within columns. */

    public IEnumerator CollapseVisual(HashSet<int> groupColumns){

        movingCollapseCount = 0; 

        foreach (int x in groupColumns){
            int writeIndex = 0;

            for (int readIndex = 0; readIndex < M; readIndex++){
                BlockView view = views[x, readIndex];

                if (view != null){
                    if (readIndex!= writeIndex){

                        // Update the reference in the 2D array to the new logical position
                        views[x, writeIndex] = view; 
                        views[x, readIndex] = null; 

                        Vector3 targetPos = GridToWorld(x, writeIndex); 

                        movingCollapseCount++; 

                        view.OnMoveFinished = HandleMoveFinished;

                        // Start the translation animation
                        view.MoveTo(targetPos);
                        view.UpdateObjectCoords(x,writeIndex);
                        
                    }

                    writeIndex++;
                }    
            }

        }
        // Wait until all falling animations in this batch are completed
        while (movingCollapseCount > 0){
            yield return null;
        }

    }



    /* Spawns new blocks from above the grid with a staggered entrance.
    Uses spawnOffset to prevent overlapping and create a natural fall. */

    public IEnumerator SpawnVisual(HashSet<int> columns){

        movingSpawnCount = 0;

        foreach (int x in columns){
            int spawnOffset = 0; 

            for (int y = 0; y < M; y++){
                Block block = grid.GetBlock(x, y);

                // Find empty visual slots that have a logical block assigned
                if (!block.isEmpty && views[x, y] == null){

                    GameObject go = blockPool.getBlock();
                    go.transform.SetParent(transform);
                    BlockView view = go.GetComponent<BlockView>();
                    view.isNewSpawn = true;
                    view.UpdateObjectCoords(x, y);

                    // Offset the starting height so blocks arrive in a satisfying sequence
                    Vector3 spawnPos = GridToWorld(x, M + spawnOffset);
                    go.transform.position = spawnPos;

                    // Set default sprite before the animation begins

                    BlockData data = blockDatas[block.colorID];
                    view.SetSprite(data.def);

                    Vector3 targetPos = GridToWorld(x, y);
                    movingSpawnCount++;

                    view.OnMoveFinished = HandleMoveFinished;
                    view.MoveTo(targetPos);

                    views[x, y] = view;

                    spawnOffset++; 
                }
            }
        }
        // Wait until all falling animations in this batch are completed
        while (movingSpawnCount > 0){
            yield return null;
        }
        
    }


    /* Event handler triggered when a block's downward movement (collapse or spawn) is complete.
    Manages animation synchronization counters and triggers group icon updates for stability */

    private void HandleMoveFinished(BlockView view){
        view.OnMoveFinished = null;

        view.isNewSpawn = false;
        if (movingCollapseCount > 0) movingCollapseCount--;
        if (movingSpawnCount > 0) movingSpawnCount--;
        
        UpdateGroupIfReady(view.getX, view.getY);
        UpdateGroupIfReady(view.getX - 1, view.getY);
        UpdateGroupIfReady(view.getX + 1, view.getY);
    }



    // Attempts to update the group icons only if all members of the group are physically settled. 

    private void UpdateGroupIfReady(int x, int y){

        // Boundary check
        if (x < 0 || x >= N || y < 0 || y >= M) return;

        if (Time.frameCount != lastUpdateFrame){
            processedThisFrame.Clear();
            lastUpdateFrame = Time.frameCount;
        }

        //Skip if this cell was already processed as part of another group search this frame
        Vector2Int key = new Vector2Int(x, y);
        if (processedThisFrame.Contains(key)) return;

        // BFS 
        List<Vector2Int> currentGroup = grid.FindGroup(x, y);

        //If any block in the group is still animating, return
        foreach (var pos in currentGroup){
            BlockView v = views[pos.x, pos.y];
            if (v == null || v.IsMoving || v.isNewSpawn)
                return;
        }

        // Apply correct icons and mark all members as processed
        foreach (var pos in currentGroup){
            ApplyIcon(pos.x, pos.y);
            processedThisFrame.Add(pos);
        }
    }


    // Updates the visual icon of a given block based on its logical group size

    private void ApplyIcon(int x, int y){
        Block block = grid.GetBlock(x, y);
        BlockView view = views[x, y];
        
        if (view == null || block.isEmpty) return;
        
        BlockData data = blockDatas[block.colorID];

        if (block.groupSize > C)
            view.SetSprite(data.C);
         else if (block.groupSize > B) 
            view.SetSprite(data.B);
        else if (block.groupSize > A) 
            view.SetSprite(data.A);
        else 
            view.SetSprite(data.def);
    }
    


    public void DeadlockVisual(Vector2Int pos){
        BlockView view = views[pos.x, pos.y];
        if (view != null)
            view.DeadlockAnimation();
    }
    

    public void AdjustCamera(){
        Camera cam = Camera.main; // get the main camera
        cam.orthographic = true; // guaranteeing the camera position

        float gridWidth = N * blockSize.x;
        float gridHeight = M * blockSize.y;

        float screenAspect = (float)Screen.width / Screen.height;

        // (height=ortographicsize*2) * screenAspect = gridWidth
        
        float minimumCameraHeight = gridWidth / (screenAspect * 2f);
        float minimumCameraWidth = gridHeight / 2f;
        
        cam.orthographicSize = Mathf.Max(minimumCameraHeight,minimumCameraWidth) + (minimumCameraHeight / 4);

        //sprite plane is on 0, preventing the default
        cam.transform.position = new Vector3(0, 0, -10);
    }


    // centerize the objects
    private Vector3 GridToWorld(int x, int y){
        
        float offsetX = (N - 1) * blockSize.x / 2f;
        float offsetY = (M - 1) * blockSize.y / 2f;

        return new Vector3(
            (x * blockSize.x) - offsetX,
            (y * blockSize.y) - offsetY,
            0f
        );
    }

}