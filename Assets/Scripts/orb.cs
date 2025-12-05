using UnityEngine;

public class Orb : MonoBehaviour
{
    [SerializeField] private OrbData orbData;

    [Header("Movement / Animation")]
    public float magnetstrength = 2f;
    public float radius = 20f;
    public float rotationSpeed = 100f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.25f;

    [Header("References")]
    public GameObject player;
    private Vector3 startPos;   // store original spawn position

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float DistFromPlayer = Vector3.Distance(transform.position, player.transform.position);
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;

        if (DistFromPlayer < radius)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                player.transform.position,
                magnetstrength * Time.deltaTime
            );
        }

        /* subtle animation for orbs */
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == player && orbData != null)
        {

            // use the AddOrb function instead of touching OrbCount directly
            orbData.AddOrb(1);
            Debug.Log("Orbs: " + orbData.OrbCount);

            Destroy(gameObject);
        }
    }
}
