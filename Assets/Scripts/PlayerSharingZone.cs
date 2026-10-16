using UnityEngine;

/// <summary>
/// Small dedicated trigger zone for multiplayer sharing between players.
/// When two players physically enter each other's sharing zone, they equalize spray charges and EXP.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class PlayerSharingZone : MonoBehaviour
{
    public Player ownerPlayer;
    public CircleCollider2D circleCollider;

    private void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider == null)
        {
            circleCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        circleCollider.isTrigger = true;

        if (ownerPlayer == null)
        {
            ownerPlayer = GetComponentInParent<Player>();
        }
    }

    public void Init(Player owner, float radius)
    {
        ownerPlayer = owner;
        if (circleCollider == null)
        {
            circleCollider = GetComponent<CircleCollider2D>();
        }
        if (circleCollider != null)
        {
            circleCollider.isTrigger = true;
            if (radius > 0f)
            {
                circleCollider.radius = radius;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        CheckAndShare(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        CheckAndShare(collision);
    }

    private void CheckAndShare(Collider2D otherCollider)
    {
        if (ownerPlayer == null || otherCollider == null) return;

        // Игнорируем коллайдер Magnet (радиус 20), чтобы не запускать обмен на расстоянии
        if (otherCollider.name == "Magnet" || otherCollider.GetComponent<Magnet>() != null)
        {
            return;
        }

        Player otherPlayer = otherCollider.GetComponentInParent<Player>();
        if (otherPlayer != null && otherPlayer != ownerPlayer)
        {
            Player.ShareBetween(ownerPlayer, otherPlayer);
        }
    }
}
