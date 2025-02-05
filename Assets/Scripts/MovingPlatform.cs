using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    private void OnCollisionStay2D(Collision2D collision)
    {
        // Player tag object control
        if (collision.gameObject.CompareTag("Player"))
        {
            Transform playerTransform = collision.transform;

            // Integrate platform speed with player
            Vector3 platformSpeed = gameObject.GetComponent<LandMovement>().PlatformSpeed;

            // If platform moves y-axis, implement the tolerance
            if (Mathf.Abs(platformSpeed.y) > 0.01f) // Y-Axis control
            {
                // Check the difference between the player's Y position and the platform's Y position
                float verticalDifference = Mathf.Abs(playerTransform.position.y - transform.position.y);

                // If the player is close to the platform within a certain tolerance
                if (verticalDifference <= 0.1f) // We set the tolerance as 0.1
                {
                    playerTransform.position += platformSpeed;
                }
            }
            else
            {
                // If there is no vertical movement, apply platform speed directly.
                playerTransform.position += platformSpeed;
            }
        }
    }

}
