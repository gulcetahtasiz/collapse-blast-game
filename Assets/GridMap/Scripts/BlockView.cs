using UnityEngine;
using System.Collections;

public class BlockView : MonoBehaviour{
    
    private int x;
    private int y;

    public void Init(int x, int y){
        this.x = x;
        this.y = y;
    }

    // getters
    public int getX => x;
    public int getY => y;

    
}
