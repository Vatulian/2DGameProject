using UnityEngine;

public class CameraController : MonoBehaviour
{
    //Follow player
    [SerializeField] private Transform player;
    [SerializeField] private float aheadDistance;
    [SerializeField] private float cameraSpeed;
    private float lookAhead;
    private float verticalOffset; // offset for Y-axis

    private void Update()
    {
        // Follow player
        transform.position = new Vector3(player.position.x + lookAhead, transform.position.y, transform.position.z);

        // Update for lookAhead
        lookAhead = Mathf.Lerp(lookAhead, (aheadDistance * player.localScale.x), Time.deltaTime * cameraSpeed);

        // update verticalOffset for the Y-axis
        verticalOffset = Mathf.Lerp(transform.position.y, player.position.y, Time.deltaTime * cameraSpeed);

        // Determine new camera position
        transform.position = new Vector3(transform.position.x, verticalOffset, transform.position.z);
    }
}
