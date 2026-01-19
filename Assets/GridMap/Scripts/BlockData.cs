using UnityEngine;

[CreateAssetMenu(fileName = "BlockData", menuName = "Scriptable Objects/BlockData")]
public class BlockData : ScriptableObject
{
    public int id; // ids showen with integers in grid array
    public Sprite def;
    public Sprite A;
    public Sprite B;
    public Sprite C;
    
}
