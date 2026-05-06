using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InkBlindEffect : MonoBehaviour
{
    public static InkBlindEffect Instance;
    
    [SerializeField] private Image inkOverlay; // ลาก Image สีดำ ใน Canvas มาใส่
    private Coroutine activeRoutine;

    private void Awake() 
    { 
        Instance = this; 
        if (inkOverlay != null)
        {
            // ซ่อนไว้ก่อนตั้งแต่ตอนเริ่ม
            inkOverlay.color = new Color(0, 0, 0, 0); 
        }
    }

    public void ApplyBlind(float duration)
    {
        // ถ้าโดนหมึกซ้ำตอนที่ยังไม่หายตาบอด ให้เริ่มนับใหม่เลย
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(BlindRoutine(duration));
    }

    private IEnumerator BlindRoutine(float duration)
    {
        // จังหวะหมึกสาดใส่ตา: Fade in ความทึบ 0 -> 0.85 (ใช้เวลา 0.3 วินาที)
        float t = 0f;
        while (t < 0.3f) 
        {
            t += Time.deltaTime;
            inkOverlay.color = new Color(0, 0, 0, Mathf.Lerp(0f, 0.85f, t / 0.3f));
            yield return null;
        }
        
        // Hold: ค้างจอดำไว้ตามระยะเวลา duration ของสกิล
        yield return new WaitForSeconds(duration);
        
        // จังหวะตาเริ่มมองเห็น: Fade out ความทึบ 0.85 -> 0 (ใช้เวลา 0.5 วินาที)
        t = 0f;
        while (t < 0.5f) 
        {
            t += Time.deltaTime;
            inkOverlay.color = new Color(0, 0, 0, Mathf.Lerp(0.85f, 0f, t / 0.5f));
            yield return null;
        }
        
        // เซ็ตให้ใส 100% เคลียร์ชัวร์ๆ
        inkOverlay.color = new Color(0, 0, 0, 0);
        activeRoutine = null;
    }
}