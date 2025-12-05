using UnityEngine;
using UnityEngine.UI;

public class Cooldown : MonoBehaviour
{
    [Header("References")]
    public OrbData orbData;

    [Header("UI Images for Orb Fill Levels")]
    public Image bar0;   
    public Image bar20;  
    public Image bar40;  
    public Image bar60;  
    public Image bar80;  
    public Image bar100; 

    private float drainTimer = 0f;
    private int lastOrbCount = -1;

    [Header("Settings")]
    public float drainInterval = 0.5f; // how quickly orbs naturally drain

    private void Start()
    {
        if (orbData == null)
        {
            Debug.LogError("Cooldown: OrbData reference is missing.");
            enabled = false;
            return;
        }

        UpdateUI();
    }

    private void Update()
    {
        if (orbData == null) return;

        // 🔹 UI instantly reacts when any script changes OrbCount
        if (orbData.OrbCount != lastOrbCount)
        {
            UpdateUI();
        }

        // 🔹 Natural cooldown drain
        drainTimer += Time.deltaTime;
        if (drainTimer >= drainInterval)
        {
            drainTimer = 0f;

            if (orbData.OrbCount > 0)
            {
                orbData.OrbCount -= 1;
                UpdateUI();
            }
        }
    }

    public void UpdateUI()
    {
        int count = orbData.OrbCount;
        lastOrbCount = count; 

        if (!bar0 || !bar20 || !bar40 || !bar60 || !bar80 || !bar100)
        {
            Debug.LogWarning("Cooldown: Missing image reference.");
            return;
        }

        bar0.gameObject.SetActive(false);
        bar20.gameObject.SetActive(false);
        bar40.gameObject.SetActive(false);
        bar60.gameObject.SetActive(false);
        bar80.gameObject.SetActive(false);
        bar100.gameObject.SetActive(false);

        switch (count)
        {
            case 0: bar0.gameObject.SetActive(true); break;
            case 1: bar20.gameObject.SetActive(true); break;
            case 2: bar40.gameObject.SetActive(true); break;
            case 3: bar60.gameObject.SetActive(true); break;
            case 4: bar80.gameObject.SetActive(true); break;
            case 5: bar100.gameObject.SetActive(true); break;
        }
    }
}
