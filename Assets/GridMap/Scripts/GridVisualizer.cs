using UnityEngine;
using System.Collections.Generic;


public class GridVisualizer : MonoBehaviour{

    public GameObject blockPrefab;
    public BlockData[] blockDatas;

    private Grid grid;
    private int M, N, A, B, C;
    private Vector2 blockSize;
    private BlockView[,] views;
    

    public void Init(Grid grid, int m, int n, int a, int b, int c)
    {
        this.grid = grid;
        M = m;
        N = n;
        A = a;
        B = b;
        C = c;

        views = new BlockView[N, M];
        blockSize = blockDatas[1].def.bounds.size; //x = 2.25 and y = 2.56, from sprite
    }



    //JUST PUT PREFABS, ŞEFFAF

    public void CreatePrefabs(){

        
        for (int x = 0; x < N; x++){
            for (int y = 0; y < M; y++){
                GameObject go = Instantiate(blockPrefab, transform);
            
                // that prefabs info for clicking
                BlockView view = go.GetComponent<BlockView>();
                view.Init(x, y); 

                go.transform.position = GridToWorld(x, y);
                views[x, y] = view;
            }
        }
    }



    // updates 
    public void UpdateAllSprites()
    {
        for (int x = 0; x < N; x++){
            for (int y = 0; y < M; y++)
            {
                Block block = grid.GetBlock(x, y);

                BlockView view = views[x, y];
                SpriteRenderer sr = view.GetComponent<SpriteRenderer>();

                if (block.isEmpty){
                    sr.enabled = false;
                    continue;
                }

                sr.enabled = true;
                BlockData data = blockDatas[block.colorID];

                //look at the grid's block, comparee groupsizes, choose correct icon
                if(block.groupSize > C){
                    sr.sprite = data.C;
                }
                else if(block.groupSize > B){
                    sr.sprite = data.B;
                }
                else if(block.groupSize > A){
                    sr.sprite = data.A;
                }
                else{
                    sr.sprite = data.def; 
                }
            }
        }
    }


    public void AdjustCamera(){
        Camera cam = Camera.main; // get the main camera
        cam.orthographic = true; // guaranteeing the camera position

        float gridWidth = N * blockSize.x;

        float screenAspect = (float)Screen.width / Screen.height;

        // (height=ortographicsize*2) * screenAspect = gridWidth
        
        float minimumCameraHeight = gridWidth / (screenAspect * 2f);
        
        cam.orthographicSize = minimumCameraHeight + (minimumCameraHeight / 4);

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