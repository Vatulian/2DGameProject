using System.Collections;
using UnityEngine;

public class DropThroughPlatform : MonoBehaviour
{
    private Collider2D playerCollider;

    private void Awake()
    {
        playerCollider = GetComponent<Collider2D>();
    }

    private void OnTriggerStay2D(Collider2D col)
    {
        if (col.CompareTag("OneWayPlatform"))
        {
            // Aşağı yön tuşuna + Jump’a basılıyorsa
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                StartCoroutine(DisableCollision(col));
            }
        }
    }

    private IEnumerator DisableCollision(Collider2D platform)
    {
        Physics2D.IgnoreCollision(playerCollider, platform, true);
        yield return new WaitForSeconds(0.25f); // kısa süre
        Physics2D.IgnoreCollision(playerCollider, platform, false);
    }
}
