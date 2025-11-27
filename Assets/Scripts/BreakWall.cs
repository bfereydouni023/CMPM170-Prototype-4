using UnityEngine;

public class SimpleBreakableWall : MonoBehaviour
{
    [SerializeField] private OrbData orbData;
   /* wall objects */
    public GameObject intactWall;
    public GameObject brokenWall;


    /* explosion vars */
    public float maxRecordedSpeed = 2f;
    public float minExplosionForce = 0.2f;
    public float maxExplosionForce = 2f;
    public float explosionRadius = 5f;
    public float upwardsModifier = 0.5f;
    public float debrisLifetime = 5f;
    public AudioClip breakSound;
    /* orb cost to destroy wall */
    public float orbcost = 0;
    public float requiredSpeed = 6f;     
    [Header("Slowdown After Break")]
    public float slowdownDuration = 0.8f;

    // 0 = barely slow them, 1 = slow nearly to minSpeed
    [Range(0f, 1f)]
    public float slowdownStrength = 0.5f;


    /**
        references OrbData to get number of orbs collected. 
        If higher than or equal to orbcost of wall, player can break
     **/
    private float NumOrbs => orbData != null ? orbData.OrbCount : 0f;
    bool hasBroken = false;

    private AudioSource _breakSoundSource;

    void Awake()
    {
        if (orbData == null)
        {
            orbData = FindAnyObjectByType<OrbData>();
        }
    }

    void Start()
    {
        _breakSoundSource = gameObject.AddComponent<AudioSource>();
        _breakSoundSource.playOnAwake = false;
        _breakSoundSource.loop = false;
        _breakSoundSource.spatialBlend = 1f;
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasBroken)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (orbcost > NumOrbs)
        {
            Debug.Log("Not enough orbs to break this wall.");
            return;
        }

        float speed = 0f;
        RailMover railMover = other.GetComponent<RailMover>();

        if (railMover != null)
        {
            speed = railMover.CurrentSpeedAbs;
        }
        else
        {
            GridMover oldMover = other.GetComponent<GridMover>();
            if (oldMover != null)
            {
                speed = oldMover.CurrentSpeedAbs;
            }
        }

        if (speed < requiredSpeed)
        {
            return;
        }

        float clampedSpeed = Mathf.Clamp(speed, 0f, maxRecordedSpeed);
        float t = (maxRecordedSpeed > 0f) ? (clampedSpeed / maxRecordedSpeed) : 0f;
        float explosionForce = Mathf.Lerp(minExplosionForce, maxExplosionForce, t);

        Vector3 explosionPoint = other.transform.position;

        DeductOrbs();
        Break(explosionForce, explosionPoint);

        // Trigger slowdown on the mover after breaking
        if (railMover != null)
        {
            // Compute a target speed between maxSpeed and minSpeed based on strength
            float targetSpeed = Mathf.Lerp(
                railMover.maxSpeed,   // 0 => stay closer to max speed
                railMover.minSpeed,   // 1 => drop near min speed
                slowdownStrength
            );

            railMover.TriggerSlowdown(slowdownDuration, targetSpeed);
        }

    }

    void Break(float explosionForce, Vector3 explosionPoint)
    {
        hasBroken = true;
        Debug.Log($"WALL BROKE!");

        if (intactWall != null)
            intactWall.SetActive(false);

        if (brokenWall != null)
            brokenWall.SetActive(true);

        _breakSoundSource.PlayOneShot(breakSound);

        Rigidbody[] pieces = brokenWall.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in pieces)
        {
            rb.AddExplosionForce(
                explosionForce,
                explosionPoint,
                explosionRadius,
                upwardsModifier,
                ForceMode.Impulse
            );

        }
    }

    void DeductOrbs()
    {
        if (orbData == null)
        {
            return;
        }

        int cost = Mathf.RoundToInt(orbcost);
        orbData.OrbCount = Mathf.Max(0, orbData.OrbCount - cost);
    }
}
