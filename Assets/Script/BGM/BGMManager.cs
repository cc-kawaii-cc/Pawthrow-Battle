using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    [Header("Playlist Settings")]
    public AudioClip[] playlist; // ลากเพลงมาใส่ตรงนี้
    public bool shuffle = true;  // อยากให้สุ่มเพลงไหม? (ถ้าไม่ติ๊กจะเล่นเรียงตามลำดับ)

    [Header("Volume")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    private AudioSource audioSource;
    private int currentTrackIndex = 0;

    void Awake()
    {
        // ระบบ Singleton เพื่อให้มี BGMManager แค่ตัวเดียวในเกม และไม่ถูกทำลายตอนเปลี่ยนฉาก
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // ถ้าโหลดฉากใหม่แล้วเจอตัวซ้ำ ให้ลบตัวใหม่ทิ้ง
            return;
        }

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false; // ปิด Loop เพราะเราจะให้มันเล่นเพลงถัดไปตอนเพลงจบ
        audioSource.volume = musicVolume;
    }

    void Start()
    {
        if (shuffle)
        {
            currentTrackIndex = Random.Range(0, playlist.Length);
        }
        PlayNextTrack();
    }

    void Update()
    {
        // เช็คว่าถ้าเพลงหยุดเล่น (เล่นจบ) ให้เปลี่ยนไปเล่นเพลงถัดไป
        if (!audioSource.isPlaying && playlist.Length > 0)
        {
            PlayNextTrack();
        }
    }

    void PlayNextTrack()
    {
        if (playlist.Length == 0) return;

        audioSource.clip = playlist[currentTrackIndex];
        audioSource.Play();

        if (shuffle)
        {
            // สุ่มเพลงถัดไป
            currentTrackIndex = Random.Range(0, playlist.Length);
        }
        else
        {
            // เลื่อนไปเพลงถัดไป ถ้าถึงเพลงสุดท้ายแล้วให้วนกลับมาเพลงแรก (index 0)
            currentTrackIndex = (currentTrackIndex + 1) % playlist.Length;
        }
    }
}