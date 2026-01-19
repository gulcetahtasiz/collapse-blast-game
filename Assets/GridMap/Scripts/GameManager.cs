using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    
    [Header("Grid Settings")]
    public int M; // rows
    public int N; // columnssu
    public int K; // color count
    public int A; // first condition
    public int B; // second condition
    public int C; // third condition


    public GridVisualizer visualizer;

    private Grid grid;
    


    private void Start(){
        grid = new Grid(M, N, K); // input from player

        grid.UpdateGroupSizes();
        visualizer.Init(grid, M, N, A, B, C);
        visualizer.CreatePrefabs(); //just once
        visualizer.AdjustCamera();
        visualizer.UpdateAllSprites();
    }


    private void Update(){

        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }

    }


    private void HandleClick(){

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // are there any t2d collider at that ray?
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

        if (hit.collider == null) return;

        BlockView bl = hit.collider.GetComponent<BlockView>();

        if (bl==null) return;

        //Debug.Log($"Clicked block at {bl.getX}, {bl.getY}");
        List<Vector2Int> clickedGroup = grid.FindGroup(bl.getX,bl.getY);

        //if group is not blastable do nothing 
        if (clickedGroup.Count < 2) return;


        grid.BlastGroup(clickedGroup);

        grid.CollapseColumns();
    
        grid.SpawnNewBlocks();

        grid.UpdateGroupSizes();

        visualizer.UpdateAllSprites();

    } 
}
