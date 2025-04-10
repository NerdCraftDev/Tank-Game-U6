using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 20f;
    public int bounces = 3;
    public float lifetime = 15f;
    private int currentBounces = 0;
    public GameObject parent;
    private TankController ownerTank;

    private void Start()
    {
        // Destroy the bullet after a set lifetime
        Destroy(gameObject, lifetime);
        ownerTank = parent.GetComponent<TankController>();
    }

    private void Update()
    {
        // Move the bullet forward
        transform.Translate(speed * Time.deltaTime * transform.forward, Space.World);
    }

    void OnCollisionEnter(Collision collision)
    {
        // --- Filter Collisions ---
        if (collision.gameObject == parent ||
        (parent != null && collision.transform.IsChildOf(parent.transform)) ||
        (ownerTank != null && ownerTank.ActiveBullets.Contains(collision.gameObject)))
        {
            return; // Ignore collision with owner, its children, or sibling bullets
        }

        // --- Handle Collision Response ---
        // Debug.Log("Bullet collided with: " + collision.gameObject.name + " Tag: " + collision.gameObject.tag); // More detailed log
        // Hit a non-bounceable surface with bounces remaining
        if (collision.gameObject.TryGetComponent(out IShootable target))
        {
            // Assuming Option 2 for the interface method:
            target.OnHit(this, collision.contacts[0].point, collision.contacts[0].normal, parent);
        }

        if (currentBounces >= bounces)
        {
            DestroySelf(); // Out of bounces
            return;
        }

        // If we have bounces left, check if the surface is bounceable
        if (collision.gameObject.CompareTag("Bounceable"))
        {
            // Reflect
            Vector3 reflectDirection = Vector3.Reflect(transform.forward, collision.contacts[0].normal).normalized; // Normalize just in case
            reflectDirection.y = 0; // Keep the bullet on the horizontal plane

            // Avoid zero vector if reflected perfectly back (unlikely with normalized normal)
            if(reflectDirection.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(reflectDirection);
            }
            else
            {
                // What to do if reflect direction is zero? Destroy? Reverse?
                Debug.LogWarning("Bullet reflect direction is zero, destroying bullet to avoid stuck state.");
                DestroySelf();
            }

            currentBounces++;
            // Play bounce sound?
        }
        else
        {
            // Hit a non-bounceable surface with bounces remaining
            DestroySelf();
            // Play impact sound/effect? Call damage function? (See IHittable below)
        }
    }

    void OnDestroy()
    {
        // Notify the owner TankController to remove this bullet from its list
        if (ownerTank != null)
        {
            ownerTank.RemoveBullet(gameObject);
        }
        // Add explosion effect/sound here?
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }
}
