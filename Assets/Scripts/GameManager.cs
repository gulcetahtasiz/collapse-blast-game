using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GameManager : MonoBehaviour
{
    
    [Header("Grid Settings")]
    public int M; // rows
    public int N; // columns
    public int K; // color count
    public int A; // first condition
    public int B; // second condition
    public int C; // third condition


    private Grid grid;
    [SerializeField] private GridVisualizer visualizer;

    private bool isBoardBusy = false;
    


    public void StartGame(int m, int n, int k, int a, int b, int c){

        M = Mathf.Clamp(m, 2, 10);
        N = Mathf.Clamp(n, 2, 10);
        K = Mathf.Clamp(k, 1, 6);

        A = a;
        B = b;
        C = c;

        // Initialize logical grid
        grid = new Grid(M, N, K); 

        // Compute initial group sizes and detect deadlock
        bool hasDeadlock = grid.UpdateGroupSizes();

        // Initialize visual system
        visualizer.Init(grid, M, N, A, B, C);
        visualizer.CreatePrefabs(); //just once
        visualizer.AdjustCamera();

        // Resolve deadlock if no blastable group exists at start
        if (!hasDeadlock){
            var changedBlock = grid.SolveDeadlock();
            if (changedBlock.HasValue){
                grid.UpdateLocalGroupSizes();
                visualizer.DeadlockVisual(changedBlock.Value);
            }
        }

        // Sync visuals with initial logic state
        visualizer.UpdateAllSprites();
    }


    private void Update(){
         // Handle player input
        if (Input.GetMouseButtonDown(0)){
            HandleClick();
        }

    }


    private void HandleClick(){

        // Ignore input while board is animating
        if (isBoardBusy) return;

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Raycast to detect clicked block
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
        if (hit.collider == null) return;

        BlockView view = hit.collider.GetComponent<BlockView>();
        if (view == null) return;

        // Find the group of the clicked block
        List<Vector2Int> group = grid.FindGroup(view.getX, view.getY);
        if (group.Count < 2) return;

        StartCoroutine(clickRoutine(group));
    }




    private IEnumerator clickRoutine(List<Vector2Int> group){
            
        isBoardBusy = true;

        // blast
        grid.BlastGroup(group);
        visualizer.BlastVisual(group);

        // collapse and spawn
        grid.CollapseColumns();
        grid.SpawnNewBlocks();

        // Recalculate group sizes only in affected area
        bool hasMove = grid.UpdateLocalGroupSizes();

        // visuals
        yield return visualizer.CollapseVisual(grid.affectedColumns);
        yield return visualizer.SpawnVisual(grid.affectedColumns);

        //deadlock check
        if (!hasMove){
            var changed = grid.SolveDeadlock();
            if (changed.HasValue){
                grid.UpdateLocalGroupSizes();
                visualizer.DeadlockVisual(changed.Value);
                visualizer.UpdateAllSprites();
            }
        }
        isBoardBusy = false;
    }
}