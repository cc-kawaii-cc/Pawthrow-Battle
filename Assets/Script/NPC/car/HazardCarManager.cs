using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

// สร้าง Class เพื่อเก็บข้อมูลของรถแต่ละคัน
[System.Serializable]
public class CarPathData
{
    [Header("Car Setup")]
    public Transform carTransform;
    public Transform[] waypoints; // จุด Waypoint เฉพาะของรถคันนี้
    
    [Header("Settings")]
    public float moveSpeed = 10f;
    public float turnSpeed = 5f;
    
    [HideInInspector] 
    public int currentWaypointIndex = 0;
}

public class HazardCarManager : NetworkBehaviour
{
    [Header("All Map Cars")]
    // ลิสต์รายชื่อรถทั้งหมดและเส้นทางของมัน
    public List<CarPathData> allTrafficCars = new List<CarPathData>();

    void Update()
    {
        // ให้เซิร์ฟเวอร์เป็นคนประมวลผลการเดินรถทั้งหมดในลูปเดียว
        if (!IsServer) return;

        foreach (var carData in allTrafficCars)
        {
            if (carData.carTransform == null || carData.waypoints.Length == 0) continue;

            Transform target = carData.waypoints[carData.currentWaypointIndex];
            Vector3 direction = (target.position - carData.carTransform.position).normalized;
            direction.y = 0; // ล็อคแกน Y
            
            // เคลื่อนที่
            carData.carTransform.position += direction * carData.moveSpeed * Time.deltaTime;
            
            // หมุนหน้ารถ
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                carData.carTransform.rotation = Quaternion.Slerp(carData.carTransform.rotation, lookRotation, carData.turnSpeed * Time.deltaTime);
            }

            // เช็คว่าถึง Waypoint หรือยัง
            if (Vector3.Distance(new Vector3(carData.carTransform.position.x, 0, carData.carTransform.position.z), 
                                 new Vector3(target.position.x, 0, target.position.z)) < 1f)
            {
                carData.currentWaypointIndex = (carData.currentWaypointIndex + 1) % carData.waypoints.Length;
            }
        }
    }
}