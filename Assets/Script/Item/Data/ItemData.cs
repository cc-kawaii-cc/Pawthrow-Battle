using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Pawthrow/ItemData")]
public class ItemData : ScriptableObject
{
    public float damage = 10f; 
    public float knockbackForce = 15f;

    [Header("Stun Effect")]
    public bool isHeavyItem = false;
    public float stunDuration = 2f;
    
    [Header("Destroy Settings")]
    public bool destroyOnHitPlayer = true;  
    public bool destroyOnHitGround = false; 
    public float destroyDelay = 0.1f;       
    
    [Header("─── Consumable Settings ───")]
    public bool isConsumable = false;
    [Tooltip("เพิ่ม HP (ไก่ทอด)")]
    public float healAmount = 0f;
    [Tooltip("เพิ่ม moveSpeed ชั่วคราว (Energy Drink)")]
    public float speedBoostAmount = 0f;
    public float speedBoostDuration = 0f;
    [Tooltip("เพิ่ม throwForce ชั่วคราว (เหล้า/เบียร์)")]
    public float throwBoostAmount = 0f;
    public float throwBoostDuration = 0f;
    
    // === NEW: DAMAGE BOOST (x2 Damage Item) ===
    [Tooltip("ตัวคูณดาเมจชั่วคราว (เช่น 2 = x2 Damage)")]
    public float damageBoostMultiplier = 1f; // Default is 1 (normal damage)
    public float damageBoostDuration = 0f;

    // === NEW: TRAP & WALK-OVER SETTINGS ===
    [Header("─── Trap Settings (Walk-over Pickups) ───")]
    [Tooltip("ถ้าติ๊กถูก ไอเทมนี้จะทำงานทันทีที่เดินเหยียบ (เป็นกับดัก หรือบัฟเดินชน)")]
    public bool triggerOnWalk = false;
    [Tooltip("ถ้าเดินชน/ใช้งาน จะทำลายไอเทมที่อยู่ในมือผู้เล่นทันที")]
    public bool destroysHeldItem = false;
    [Tooltip("สตันผู้เล่นทันทีที่เดินชน (ใช้ร่วมกับ triggerOnWalk)")]
    public bool isStunTrap = false;

    [Header("─── Explosion Settings ───")]
    public bool isExplosive = false;
    public float explosionRadius = 5f;
    public float explosionDamage = 25f;
    public float explosionKnockback = 35f;
    public float explosionStunDuration = 1.2f;
    public GameObject explosionVFXPrefab; 

    [Header("─── Fire Item Settings ───")]
    public bool isFireItem = false;
    public GameObject fireZonePrefab; 
}