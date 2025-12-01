using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Follow Player")]
    [SerializeField] private Transform player;
    [SerializeField] private float aheadDistance = 2f;
    [SerializeField] private float followLerpSpeed = 5f;   // normal takip hızı

    [Header("Zoom")]
    [SerializeField] private float normalSize = 5f;   // normal oyun boyutu
    [SerializeField] private float bossSize = 7f;     // boss arenası boyutu
    [SerializeField] private float zoomLerpSpeed = 3f;

    [Header("Lock Settings")]
    [SerializeField] private bool isLocked = false;       // şu anda boss lock modunda mı
    [SerializeField] private Vector3 lockedPosition;      // boss arenanın merkezi
    [SerializeField] private bool useBossZoom = false;    // lock sırasında boss zoom mu kullanılsın
    [SerializeField] private float lockLerpSpeed = 2f;    // boss’a kilitlenirken hız

    [Header("Release From Lock")]
    [SerializeField] private float releaseDuration = 1.0f; // boss’tan player’a dönüş süresi (sn)
    private bool isReleasingFromLock;
    private float releaseTimer;
    private Vector3 releaseStartPos;

    private Camera cam;
    private float lookAhead;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (cam == null) return;

        // 1) KAMERA BOSS’A KİLİTLİYKEN
        if (isLocked)
        {
            // Pozisyonu lockedPosition’a doğru yumuşak taşır
            Vector3 targetPos = new Vector3(lockedPosition.x, lockedPosition.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, targetPos, lockLerpSpeed * Time.deltaTime);

            // Zoom’u bossSize’a doğru taşır (useBossZoom true ise)
            float targetSize = useBossZoom ? bossSize : normalSize;
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, zoomLerpSpeed * Time.deltaTime);
            return;
        }

        // 2) LOCK’TAN YUMUŞAK ÇIKIŞ MODU
        if (isReleasingFromLock)
        {
            if (player == null)
            {
                isReleasingFromLock = false;
                return;
            }

            releaseTimer += Time.deltaTime;
            float t = Mathf.Clamp01(releaseTimer / releaseDuration);

            // Player’a göre hedef pozisyonu hesapla (normal follow mantığı)
            float desiredLookAhead = aheadDistance * Mathf.Sign(player.localScale.x);
            lookAhead = Mathf.Lerp(lookAhead, desiredLookAhead, followLerpSpeed * Time.deltaTime);

            float targetX = player.position.x + lookAhead;
            float targetY = Mathf.Lerp(transform.position.y, player.position.y, followLerpSpeed * Time.deltaTime);
            Vector3 followPos = new Vector3(targetX, targetY, transform.position.z);

            // Kamera pozisyonunu releaseStartPos -> followPos arasında karıştır
            transform.position = Vector3.Lerp(releaseStartPos, followPos, t);

            // Zoom’u normalSize’a doğru getir
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, normalSize, zoomLerpSpeed * Time.deltaTime);

            // Geçiş bittiğinde normal moda dön
            if (t >= 0.999f)
            {
                isReleasingFromLock = false;
            }

            return;
        }

        // 3) NORMAL TAKİP MODU
        if (player == null) return;

        float targetAhead = aheadDistance * Mathf.Sign(player.localScale.x);
        lookAhead = Mathf.Lerp(lookAhead, targetAhead, followLerpSpeed * Time.deltaTime);

        float x = player.position.x + lookAhead;
        float y = Mathf.Lerp(transform.position.y, player.position.y, followLerpSpeed * Time.deltaTime);

        transform.position = new Vector3(x, y, transform.position.z);

        // Zoom’u normal’e yumuşat
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, normalSize, zoomLerpSpeed * Time.deltaTime);
    }

    // 🔒 Boss arenasına giriş (lock + isteğe bağlı boss zoom)
    public void LockToPosition(Vector3 worldPos, bool zoomToBoss = false)
    {
        lockedPosition = worldPos;
        isLocked = true;
        useBossZoom = zoomToBoss;

        // Lock’a girerken devam eden release var ise iptal et
        isReleasingFromLock = false;
    }

    // 🔓 Lock’tan çıkış – default: smooth
    public void Unlock(bool smooth = true)
    {
        // Artık locked değiliz
        isLocked = false;
        useBossZoom = false;

        if (smooth && player != null)
        {
            isReleasingFromLock = true;
            releaseTimer = 0f;
            releaseStartPos = transform.position;
        }
        else
        {
            isReleasingFromLock = false;
            // Hard snap istersen burada direkt player’a alabilirsin, ama şimdilik gerek yok
        }
    }

    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;
    }
}
