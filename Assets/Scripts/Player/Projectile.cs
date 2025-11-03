using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float speed = 10f;

    [Header("Collision (layer-based)")]
    [SerializeField] private LayerMask damageLayers;      // Enemy, Boss vb.
    [SerializeField] private LayerMask passThroughLayers; // Player, PlayerTriggers vb.
    [SerializeField] private float minLifetimeBeforeHit = 0.05f; // spawn sonrası kısa bağışıklık

    private float direction = 1f;
    private bool hit;
    private float lifetime;
    private float spawnTime;

    private Animator anim;
    private Collider2D myCollider;

    // Sahip (self-hit engellemek için)
    private Transform owner;
    private Collider2D[] ownerColliders;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        hit = false;
        lifetime = 0f;
        spawnTime = Time.time;
        if (myCollider) myCollider.enabled = true;
    }

    private void Update()
    {
        if (hit) return;

        float movement = speed * Time.deltaTime * direction;
        transform.Translate(movement, 0f, 0f);

        lifetime += Time.deltaTime;
        if (lifetime > 5f)
            Deactivate();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hit) return;

        // Spawn anındaki çakışmaları yok say
        if (Time.time - spawnTime < minLifetimeBeforeHit) return;

        // Sahibime/çocuklarına asla vurma
        if (owner && (collision.transform == owner || collision.transform.IsChildOf(owner))) return;

        int colMask = 1 << collision.gameObject.layer;

        // Pass-through layer'ları tamamen yok say (Player, PlayerTriggers vb.)
        if ((passThroughLayers.value & colMask) != 0) return;

        // Patlama animini tetikle
        hit = true;
        if (myCollider) myCollider.enabled = false;
        if (anim) anim.SetTrigger("Explode");

        // Çarpışma noktası (VFX için iyi görünür)
        Vector3 hitPoint = collision.bounds.ClosestPoint(transform.position);

        // Sadece damageLayers içindeyse hasar uygula
        bool canDamage = (damageLayers.value & colMask) != 0;
        if (canDamage)
        {
            // Önce Health (normal düşmanlar)
            var hp = collision.GetComponent<Health>() ?? collision.GetComponentInParent<Health>();
            if (hp != null)
            {
                hp.TakeDamage(1);
                return;
            }

            // Sonra Boss (vuruş noktasıyla)
            var boss = collision.GetComponent<Boss>() ?? collision.GetComponentInParent<Boss>();
            if (boss != null)
            {
                boss.TakeDamageAt(1, hitPoint);
                return;
            }
        }

        // Hasar yoksa sadece patlar (anim event yoksa alttakini aç)
        // Deactivate();
    }

    /// <summary>
    /// Sahibi ve yönüyle ateşle (önerilen).
    /// </summary>
    public void Fire(Transform shooter, float dir)
    {
        owner = shooter;
        ownerColliders = shooter ? shooter.GetComponentsInChildren<Collider2D>() : null;

        // Sahibimle çarpışmayı kapat
        if (myCollider && ownerColliders != null)
        {
            foreach (var oc in ownerColliders)
                if (oc) Physics2D.IgnoreCollision(myCollider, oc, true);
        }

        direction = Mathf.Sign(dir) == 0 ? 1f : Mathf.Sign(dir);
        gameObject.SetActive(true);
        hit = false;
        lifetime = 0f;
        spawnTime = Time.time;
        if (myCollider) myCollider.enabled = true;

        // Görsel flip
        var s = transform.localScale;
        if (Mathf.Sign(s.x) != direction) s.x = -s.x;
        transform.localScale = s;
    }

    /// <summary>
    /// Geriye uyumluluk; sahibi göndermez (self-hit koruması zayıflar).
    /// </summary>
    public void SetDirection(float dir) => Fire(null, dir);

    private void Deactivate()
    {
        // Sahibimle ignore'u geri aç
        if (myCollider && ownerColliders != null)
        {
            foreach (var oc in ownerColliders)
                if (oc) Physics2D.IgnoreCollision(myCollider, oc, false);
        }

        gameObject.SetActive(false);
        owner = null;
        ownerColliders = null;
    }
}
