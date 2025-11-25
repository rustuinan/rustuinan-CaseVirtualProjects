using UnityEngine;
using DG.Tweening;

public class GodModeCameraController : MonoBehaviour
{
    [Header("Hareket")]
    public float panSpeed = 15f;
    public float zoomSpeed = 20f;
    public float minHeight = 10f;
    public float maxHeight = 40f;

    [Header("Açı")]
    public float tiltAngle = 60f;

    [Header("Lightning Skill")]
    public float lightningRadius = 5f;
    public float lightningDamage = 150f;
    public float lightningCooldown = 3f;

    [Header("Lightning VFX")]
    public GameObject lightningVfxPrefab;
    public float lightningVfxLifetime = 2f;

    [Header("Lightning SFX")]
    public AudioClip lightningSfx;
    [Range(0f, 1f)]
    public float lightningSfxVolume = 0.8f;

    public LayerMask groundLayer;
    public LayerMask unitLayer;

    [Header("Aim Göstergesi")]
    public Transform indicator;
    public float indicatorHeightOffset = 0.3f;

    private float currentCooldown;
    private Camera cam;

    private static Collider[] strikeResults = new Collider[128];

    private AudioSource audioSource;

    private void OnEnable()
    {
        if (indicator != null)
            indicator.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (indicator != null)
            indicator.gameObject.SetActive(false);
    }

    private void Start()
    {
        cam = GetComponent<Camera>();

        Vector3 euler = transform.eulerAngles;
        euler.x = tiltAngle;
        transform.eulerAngles = euler;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void Update()
    {
        HandleMovement();
        HandleZoom();
        HandleIndicatorAndLightning();

        if (currentCooldown > 0f)
            currentCooldown -= Time.deltaTime;
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 right = Vector3.right;
        Vector3 forward = Vector3.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 move = (forward * v + right * h).normalized;
        transform.position += move * panSpeed * Time.deltaTime;
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.0001f)
            return;

        Vector3 pos = transform.position;
        pos.y -= scroll * zoomSpeed;
        pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);
        transform.position = pos;
    }

    private void HandleIndicatorAndLightning()
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        bool hitGround = Physics.Raycast(ray, out hit, 500f, groundLayer);

        if (hitGround)
        {
            Vector3 targetPos = hit.point;

            if (indicator != null)
            {
                Vector3 indPos = targetPos;
                indPos.y = targetPos.y + indicatorHeightOffset;
                indicator.position = indPos;

                if (!indicator.gameObject.activeSelf)
                    indicator.gameObject.SetActive(true);
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (currentCooldown > 0f)
                {
                    Debug.Log($"[GodMode] Lightning click but on COOLDOWN: {currentCooldown:F2}s left");
                }
                else
                {
                    Debug.Log($"[GodMode] Ray HIT ground at {hit.point}, casting lightning...");
                    CastLightning(targetPos);
                }
            }
        }
        else
        {
            if (indicator != null && indicator.gameObject.activeSelf)
            {
                indicator.gameObject.SetActive(false);
            }

            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log("[GodMode] Mouse clicked but RAY did NOT hit groundLayer.");
            }
        }
    }

    private void CastLightning(Vector3 center)
    {
        currentCooldown = lightningCooldown;

        Debug.Log($"[GodMode] LIGHTNING STRIKE at {center}, radius={lightningRadius}");

        if (lightningVfxPrefab != null)
        {
            Vector3 vfxPos = center + Vector3.up * 0.1f;
            GameObject vfx = Instantiate(lightningVfxPrefab, vfxPos, Quaternion.identity);
            Destroy(vfx, lightningVfxLifetime);
        }

        PlayLightningSound();


        int count = Physics.OverlapSphereNonAlloc(
            center,
            lightningRadius,
            strikeResults,
            unitLayer
        );

        Debug.Log($"[GodMode] OverlapSphere found {count} colliders.");

        for (int i = 0; i < count; i++)
        {
            Collider col = strikeResults[i];
            if (col == null) continue;

            Transform t = col.transform;
            string hitName = t.name;

            bool damaged = false;

            MeleeUnit melee = col.GetComponentInParent<MeleeUnit>();
            if (melee != null && melee.IsAlive)
            {
                melee.TakeDamage(lightningDamage);
                Debug.Log($"[GodMode] Lightning HIT Melee: {hitName}, dmg={lightningDamage}");
                damaged = true;
            }

            if (!damaged)
            {
                ArcherUnit archer = col.GetComponentInParent<ArcherUnit>();
                if (archer != null && archer.IsAlive)
                {
                    archer.TakeDamage(lightningDamage);
                    Debug.Log($"[GodMode] Lightning HIT Archer: {hitName}, dmg={lightningDamage}");
                    damaged = true;
                }
            }

            if (!damaged)
            {
                CommanderUnit commander = col.GetComponentInParent<CommanderUnit>();
                if (commander != null && commander.IsAlive)
                {
                    commander.TakeDamage(lightningDamage);
                    Debug.Log($"[GodMode] Lightning HIT Commander: {hitName}, dmg={lightningDamage}");
                    damaged = true;
                }
            }

            if (!damaged)
            {
                Debug.Log($"[GodMode] Lightning overlapped {hitName} but no known unit component found.");
            }
        }

        if (count == 0)
        {
            Debug.Log("[GodMode] Lightning strike hit NO units.");
        }
    }

    private void PlayLightningSound()
    {
        if (lightningSfx == null)
            return;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        audioSource.PlayOneShot(lightningSfx, lightningSfxVolume);
    }
}
