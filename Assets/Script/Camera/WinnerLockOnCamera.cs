using UnityEngine;

public class WinnerLockOnCamera : MonoBehaviour
{
    private Transform target; 
    public Vector3 offset = new Vector3(0, 5, -10); 
    public float lookAtSpeed = 5.0f;
    public float followSpeed = 3.0f;

    public void SetTarget(Transform winnerTransform)
    {
        target = winnerTransform;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

        Vector3 direction = target.position - transform.position;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, lookAtSpeed * Time.deltaTime);
    }
}