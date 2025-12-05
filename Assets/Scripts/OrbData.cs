using UnityEngine;

/** Public class for storing and accessing OrbCount (the number of orbs the player has collected).
 Everytime player collides with a wall, OrbCost is deducted from OrbCount **/

public class OrbData : MonoBehaviour
{
    //* maxes out at 5 orbs *//
    public int OrbCount = 5;
    public int MaxOrbs = 5;
    public AudioSource pickupSound;

    public void AddOrb(int amount = 1)
    {
        OrbCount = Mathf.Clamp(OrbCount + amount, 0, MaxOrbs);
        if (pickupSound != null)
        {
            pickupSound.Play();
        }
    }

    
    public void RemoveOrb(int amount = 1)
    {
        OrbCount = Mathf.Clamp(OrbCount - amount, 0, MaxOrbs);
    }
}
