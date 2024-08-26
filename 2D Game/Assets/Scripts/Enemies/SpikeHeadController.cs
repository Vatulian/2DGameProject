using UnityEngine;
using System.Collections;

public class Spikehead : EnemyDamage
{
    [Header("SpikeHead Attributes")]
    [SerializeField] private float speed;
    [SerializeField] private float range;
    [SerializeField] private float checkDelay;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float returnSpeed = 1.0f;
    private Vector3[] directions = new Vector3[4];
    private Vector3 destination;
    private Vector3 originalPosition; // SpikeHead original position
    private float checkTimer;
    private bool attacking;
    private bool returning;

    private void OnEnable()
    {
        Stop();
        originalPosition = transform.position; // Keep original position
    }

    private void Update()
    {
        if (attacking)
        {
            // If attack mode on, attack!
            transform.Translate(destination * Time.deltaTime * speed);
        }
        else if (returning)
        {
            
        }
        else
        {
            checkTimer += Time.deltaTime;
            if (checkTimer > checkDelay)
                CheckForPlayer();
        }
    }

    private void CheckForPlayer()
    {
        // If we are returning, don't raycast!
        if (returning)
            return;

        CalculateDirections();

        // SpikeHead check directions for x and y axis
        for (int i = 0; i < directions.Length; i++)
        {
            Debug.DrawRay(transform.position, directions[i], Color.red);
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directions[i], range, playerLayer);

            if (hit.collider != null && !attacking)
            {
                attacking = true;
                destination = directions[i];
                checkTimer = 0;
            }
        }
    }

    private void CalculateDirections()
    {
       // directions[2] = transform.right * range; // Right direction
       // directions[3] = -transform.right * range; // Left direction
        directions[0] = transform.up * range; // Up direction
        directions[1] = -transform.up * range; // Down direction
    }

    private void Stop()
    {
        destination = transform.position; // Set target to current position to stop moving
        attacking = false;
        returning = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
        Stop(); // Spikehead will stop when collide something
        returning = true; // Retrun after collide
        StartCoroutine(SmoothLerp(returnSpeed)); // Start coroutine
    }

    private IEnumerator SmoothLerp(float time)
    {
        Vector3 startingPos = transform.position;
        Vector3 finalPos = originalPosition;

        float elapsedTime = 0;

        while (elapsedTime < time)
        {
            transform.position = Vector3.Lerp(startingPos, finalPos, (elapsedTime / time));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = finalPos; // Set to original position slowly
        returning = false; // Stop when returning is done
    }
}
