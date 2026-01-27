using UnityEngine;
using System;
using System.Collections;

public class BlockView : MonoBehaviour{
    
    private int x;
    private int y;

    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float maxSpeed = 50f;
    
    public Action<BlockView> OnBlastFinished; 
    public Action<BlockView> OnMoveFinished;
    public bool isNewSpawn = false;
    public bool IsMoving = false;
    private Coroutine moveCoroutine;

    

    private SpriteRenderer sr;


    private void Awake(){
        sr = GetComponent<SpriteRenderer>();
    }


    public void Init(int x, int y){
        this.x = x;
        this.y = y;
    }

    public void UpdateObjectCoords(int newX, int newY){
        this.x = newX;
        this.y = newY;
    }


    // getters
    public int getX => x;
    public int getY => y;

    // setters
    public void SetSprite(Sprite sprite){
        sr.sprite = sprite;
        sr.enabled = true;
    }

    public void SetVisible(bool value){
        sr.enabled = value;
    }

    // Scales down the block to zero before notifying the pool for collection.

    public void BlastView(){
        StartCoroutine(BlastRoutine());
    }

    private IEnumerator BlastRoutine(){
        Vector3 startScale = transform.localScale;
        float t = 0f;
        float duration = 0.15f;

        while (t < duration){
            t += Time.deltaTime;
            float k = 1f - (t / duration);
            transform.localScale = startScale * k;
            yield return null;
        }

        transform.localScale = startScale;
        OnBlastFinished?.Invoke(this);
    }


    // Initiates a smooth translation to the target grid position, handles falling and movement with simulated gravity

    public void MoveTo(Vector3 targetPos){
        if (moveCoroutine != null){
            StopCoroutine(moveCoroutine);
        }
        StartCoroutine(MoveRoutine(targetPos));
    }

    private IEnumerator MoveRoutine(Vector3 target){
        IsMoving = true;
        float speed = 0f;


        while (Vector3.Distance(transform.position, target) > 0.01f){
            
            speed = Mathf.MoveTowards(speed, maxSpeed, acceleration* Time.deltaTime);
            
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                speed * Time.deltaTime
            );
            yield return null;
        }

        transform.position = target;
        IsMoving = false;
        moveCoroutine = null;
        OnMoveFinished?.Invoke(this);
    }


    // Enlarges the block to indicate it has been modified by the deadlock solver.
    public void DeadlockAnimation(){
        StartCoroutine(DeadlockRoutine());
    }

    IEnumerator DeadlockRoutine(){
        Vector3 baseScale = transform.localScale;
        transform.localScale = baseScale * 1.1f;
        yield return new WaitForSeconds(0.3f);
        transform.localScale = baseScale;
    }

    
}
