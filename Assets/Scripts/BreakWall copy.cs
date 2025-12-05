// using UnityEngine;
// using System.Collections;

// public class SimpleBreakableWall : MonoBehaviour
// {


//     public Camera maincamera;

//     private float shake = 0f;
//     public float shakeAmount = 0.7f;
//     private float shakedecreasefactor = 1.0f;
//     private Vector3 originalCameraPos;


//     [SerializeField] private OrbData orbData;
//      [SerializeField] private LossData LossData;

//     /* wall objects */
//     public GameObject intactWall;
//     public GameObject brokenWall;

//     /* explosion vars */
//     public float maxRecordedSpeed = 2f;
//     public float minExplosionForce = 0.2f;
//     public float maxExplosionForce = 2f;
//     public float explosionRadius = 5f;
//     public float upwardsModifier = 0.5f;
//     public float debrisLifetime = 5f;

//     /* orb cost to destroy wall */
//     public float orbcost = 0;
//     public float requiredSpeed = 6f;
    
//     [Header("Bounce Settings")]
//     public float bounceForce = 10f;

//     [Header("Slowdown After Break")]
//     public float slowdownDuration = 0.8f;

//     // 0 = barely slow them, 1 = slow nearly to minSpeed
//     [Range(0f, 1f)]
//     public float slowdownStrength = 0.5f;

//     [Header("Audio")]
//     // Assign these in the Inspector
//     public AudioSource breakAudioSource;   // plays when wall breaks
//     public AudioSource bounceAudioSource;  // plays when player bounces off



//     /**
//         references OrbData to get number of orbs collected. 
//         If higher than or equal to orbcost of wall, player can break
//      **/
//     private float NumOrbs => orbData != null ? orbData.OrbCount : 0f;
//     bool hasBroken = false;
    
//     // Bounce cooldown to prevent repeated triggering
//     private float lastBounceTime = -999f;
//     private float bounceCooldown = 0.5f;
//     private GameObject lastBouncedPlayer = null;

//     private Collider wallCollider;

//     void Awake()
//     {
//         if (orbData == null)
//         {
//             orbData = FindAnyObjectByType<OrbData>();
//         }
        
//         // Cache the collider
//         wallCollider = GetComponent<Collider>();
//     }

//     void Shake() {
//         /* play sound for collision */
//         if (maincamera == null)
//             maincamera = Camera.main;
//         if (maincamera != null)
//             originalCameraPos = maincamera.transform.localPosition;
//         shake = shakeAmount;
//     }

//     void Update() {
//         if(shake > 0.0f && maincamera != null) {
//             maincamera.transform.localPosition = originalCameraPos + Random.insideUnitSphere * shake;
//             shake -= Time.deltaTime * shakedecreasefactor;
//             if (shake <= 0f) {
//                 maincamera.transform.localPosition = originalCameraPos;
//                 shake = 0f;
//             }
//         }
//     }

//     void OnTriggerEnter(Collider other)
//     {
//         if (hasBroken) {
           
//             return;
//         }
               

//         if (!other.CompareTag("Player"))
//             return;
        
//         // Prevent repeated bounces - cooldown check
//         if (Time.time - lastBounceTime < bounceCooldown && lastBouncedPlayer == other.gameObject)
//         {
//             return;
//         }

//         float speed = 0f;
//         RailMover railMover = other.GetComponent<RailMover>();

//         // Not enough orbs → bounce
//         if (orbcost > NumOrbs)
//         {
//             // deduct a life
//             Shake();
//             LossData.lives -= 1;
//             LossData.CheckLoss();
           

//             Debug.Log($"Not enough orbs to break this wall. Bouncing back! (Orbs: {NumOrbs}, Required: {orbcost})");
            
//             // Set cooldown to prevent repeated triggers
//             lastBounceTime = Time.time;
//             lastBouncedPlayer = other.gameObject;
            
//             // Trigger bounce: reverses direction and boosts speed for dramatic bounce-back
//             if (railMover != null)
//             {
//                 railMover.TriggerBounce(bounceForce * 0.1f, 2.0f);
//             }
            
//             // Temporarily disable trigger to prevent immediate re-trigger
//             StartCoroutine(DisableTriggerTemporarily(0.3f));
            
//             // Play bounce sound if available
//             if (bounceAudioSource != null)
//             {
//                 bounceAudioSource.Play();
//             }
            
//             return;
//         }

//         // Get speed from RailMover
//         if (railMover != null)
//         {
//             speed = railMover.CurrentSpeedAbs;
//         }

//         // Too slow → bounce
//         if (speed < requiredSpeed)
//         {
//              // deduct a life
//             Shake();
//             LossData.lives -= 1;
//             LossData.CheckLoss();
           

//          //   Debug.Log($"Speed too low to break wall. Bouncing back! (Speed: {speed}, Required: {requiredSpeed})");
            
//             // Set cooldown to prevent repeated triggers
//             lastBounceTime = Time.time;
//             lastBouncedPlayer = other.gameObject;
            
//             // Trigger bounce: reverses direction and boosts speed for dramatic bounce-back
//             if (railMover != null)
//             {
//                 railMover.TriggerBounce(bounceForce * 0.1f, 2.0f);
//             }
            
//             // Temporarily disable trigger to prevent immediate re-trigger
//             StartCoroutine(DisableTriggerTemporarily(0.3f));
            
//             // Play bounce sound if available
//             if (bounceAudioSource != null)
//             {
//                 bounceAudioSource.Play();
//             }
            
//             return;
//         }

//         float clampedSpeed = Mathf.Clamp(speed, 0f, maxRecordedSpeed);
//         float t = (maxRecordedSpeed > 0f) ? (clampedSpeed / maxRecordedSpeed) : 0f;
//         float explosionForce = Mathf.Lerp(minExplosionForce, maxExplosionForce, t);

//         Vector3 explosionPoint = other.transform.position;

//         DeductOrbs();
//         Break(explosionForce, explosionPoint);

//         // Trigger slowdown on the mover after breaking
//         if (railMover != null)
//         {
//             // Compute a target speed between maxSpeed and minSpeed based on strength
//             float targetSpeed = Mathf.Lerp(
//                 railMover.maxSpeed,   // 0 => stay closer to max speed
//                 railMover.minSpeed,   // 1 => drop near min speed
//                 slowdownStrength
//             );

//             railMover.TriggerSlowdown(slowdownDuration, targetSpeed);
//         }

//     }

//     void Break(float explosionForce, Vector3 explosionPoint)
//     {
//         hasBroken = true;
//         Debug.Log("WALL BROKE!");

//         if (intactWall != null)
//             intactWall.SetActive(false);

//         if (brokenWall != null)
//             brokenWall.SetActive(true);

//         if (breakAudioSource != null)
//         {
//             breakAudioSource.Play();
//         }

//         Rigidbody[] pieces = brokenWall.GetComponentsInChildren<Rigidbody>();
//         foreach (Rigidbody rb in pieces)
//         {
//             rb.AddExplosionForce(
//                 explosionForce,
//                 explosionPoint,
//                 explosionRadius,
//                 upwardsModifier,
//                 ForceMode.Impulse
//             );
//         }
//     }

//     void DeductOrbs()
//     {
//         if (orbData == null)
//         {
//             return;
//         }

//         int cost = Mathf.RoundToInt(orbcost);
//         orbData.OrbCount = Mathf.Max(0, orbData.OrbCount - cost);
//     }
    
//     IEnumerator DisableTriggerTemporarily(float duration)
//     {
//         if (wallCollider != null)
//         {
//             wallCollider.enabled = false;
//             yield return new WaitForSeconds(duration);
//             wallCollider.enabled = true;
//         }
//     }
// }
