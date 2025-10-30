using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    private float direction;
    private bool hit;
    private float lifetime;

    private Animator anim;
    private BoxCollider2D boxCollider;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        if (hit) return;

        float movementSpeed = speed * Time.deltaTime * direction;
        transform.Translate(movementSpeed, 0f, 0f);

        lifetime += Time.deltaTime;
        if (lifetime > 5f)
            Deactivate();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hit) return;
        hit = true;

        if (boxCollider) boxCollider.enabled = false;
        if (anim) anim.SetTrigger("Explode");

        // 1) Önce Health ara (düşmanlar)
        Health hp = collision.GetComponent<Health>() ?? collision.GetComponentInParent<Health>();
        if (hp != null)
        {
            hp.TakeDamage(1);
            Debug.Log($"Projectile hit an Enemy with Health: {hp.name}");
            return;
        }

        // 2) Boss kontrolü
        Boss boss = collision.GetComponent<Boss>() ?? collision.GetComponentInParent<Boss>();
        if (boss != null)
        {
            boss.TakeDamage(1);
            Debug.Log($"Projectile hit the Boss! Remaining HP: {boss.health}");
            return;
        }

        // 3) Ne Health ne Boss bulunamadıysa
        Debug.Log($"Projectile hit something else: {collision.name} (tag: {collision.tag})");
        // Deactivate(); // anim event yoksa aç
    }

    public void SetDirection(float _direction)
    {
        lifetime = 0f;
        direction = _direction;
        gameObject.SetActive(true);
        hit = false;
        if (boxCollider) boxCollider.enabled = true;

        Vector3 scale = transform.localScale;
        if (Mathf.Sign(scale.x) != _direction)
            scale.x = -scale.x;
        transform.localScale = scale;
    }

    private void Deactivate()
    {
        gameObject.SetActive(false);
    }
}
