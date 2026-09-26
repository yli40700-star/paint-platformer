using UnityEngine;

/// <summary>
/// Anything that kills the player on touch (floor spikes, ceiling spikes).
/// B's LevelBuilder adds this to spike objects. Works with trigger or solid colliders;
/// for spikes, a trigger box covering the pointy half of the tile feels fair.
/// </summary>
public class Hazard : MonoBehaviour
{
    public string reason = "Spiked!";

    void OnTriggerEnter2D(Collider2D other) { if (other.GetComponent<PlayerController>() != null) KillPlayer(reason); }
    void OnCollisionEnter2D(Collision2D c) { if (c.collider.GetComponent<PlayerController>() != null) KillPlayer(reason); }

    /// <summary>Kill and respawn the player. Also used for red paint and falling off the level.</summary>
    public static void KillPlayer(string why)
    {
        if (PlayerController.Instance != null) PlayerController.Instance.Die(why);
    }
}
