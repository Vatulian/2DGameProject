using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    private void OnCollisionStay2D(Collision2D collision)
    {
        // Player tag'ine sahip objeyi kontrol et
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.position += gameObject.GetComponent<LandMovement>().PlatformSpeed;
        }
    }
}
