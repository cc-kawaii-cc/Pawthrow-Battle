using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject 
{
    public float damage = 10f; // ดาเมจของชิ้นนี้
    public float knockbackForce = 15f; // แรงเด้งเวลาโดนปาใส่
}