using UnityEngine;
using System.Collections.Generic;

public class BlockPool : MonoBehaviour{

    [SerializeField] private GameObject blockPrefab;

    private Queue<GameObject> poolQueue = new Queue<GameObject>();

    // get block from pool
    public GameObject getBlock(){

        // if there is a block in the queue
        if (poolQueue.Count > 0){
            GameObject block = poolQueue.Dequeue();

            block.SetActive(true);
            return block;
        }

        else{
            return Instantiate(blockPrefab);
        }
    }

    public void releaseBlock(GameObject block){
        // put block to the queue with its first visual
        block.transform.localScale = Vector3.one; 

        block.SetActive(false);
        poolQueue.Enqueue(block);
    }

}
