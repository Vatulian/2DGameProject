using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 0.25f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject[] fireballs;   // object pool
    [SerializeField] private AudioClip fireballSound;

    private Animator anim;
    private PlayerMovement playerMovement;
    private float cooldownTimer = 0f;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;

        // Tek tıklama ile ateş (basılı tutup otomatik ateş istersen altta opsiyon var)
        if (Input.GetMouseButtonDown(0) && cooldownTimer <= 0f && playerMovement != null && playerMovement.canAttack())
        {
            FireOnce();
        }

        /*  // *** OPSİYONEL: basılı tutarak auto-fire ***
        if (Input.GetMouseButton(0) && cooldownTimer <= 0f && playerMovement != null && playerMovement.canAttack())
        {
            FireOnce();
        }
        */
    }

    private void FireOnce()
    {
        int idx = FindInactiveFireball();
        if (idx < 0) return;

        cooldownTimer = attackCooldown;

        if (anim) anim.SetTrigger("Attack");
        if (SoundManager.instance && fireballSound) SoundManager.instance.PlaySound(fireballSound);

        GameObject go = fireballs[idx];
        go.transform.position = firePoint.position;

        float dir = Mathf.Sign(transform.localScale.x);
        var proj = go.GetComponent<Projectile>();
        if (proj != null)
            proj.Fire(transform, dir);      // owner gönder → self-hit yok, spawn patlaması yok
        else
            go.GetComponent<Projectile>()?.SetDirection(dir); // geriye uyumluluk, gerekmezse silebilirsin
    }

    private int FindInactiveFireball()
    {
        for (int i = 0; i < fireballs.Length; i++)
            if (!fireballs[i].activeInHierarchy)
                return i;
        return -1;
    }
}
