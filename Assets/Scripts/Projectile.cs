using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Boat Source { get; private set; }
    public float Energy { get; private set; }
    public const float muzzleVelocity =100;

    private double expirationTime = double.PositiveInfinity;

    public void Initialize(Boat source, float energy, Vector3 position, Vector3 direction, Vector3 relativeVelocity)
    {
        Source = source;
        Energy = energy;
        transform.position = position;
        Vector3 velocity = direction.normalized * muzzleVelocity + relativeVelocity;
        transform.rotation = Quaternion.LookRotation(velocity, Vector3.up);
        Rigidbody rb = transform.GetComponent<Rigidbody>();
        rb.velocity = velocity;
        expirationTime = Time.timeAsDouble + 10;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Get contact position.
        ContactPoint contact = collision.GetContact(0);

        // Hit a boat.
        Boat boat = collision.gameObject.GetComponentInParent<Boat>();
        if(boat != null)
        {
            if(Source.Faction == null || boat.Faction != Source.Faction)
            {
                boat.Damage(Source, Energy);
                boat.rigidbody.AddForceAtPosition(contact.impulse, contact.point, ForceMode.Impulse);
                Hit(contact.point);
            }
            return;
        }

        // Hit a powerup.
        Powerup powerup = collision.gameObject.GetComponentInParent<Powerup>();
        if(powerup != null)
        {
            powerup.Damage(Source, Energy);
            Hit(contact.point);
            return;
        }

        // Hit another projectile. Wow!
        Projectile projectile = collision.gameObject.GetComponentInParent<Projectile>();
        if(projectile != null)
        {
            projectile.Hit(contact.point);
            Hit(contact.point);
            return;
        }

        // Hit a different obstacle.
        Hit(contact.point);
    }
    private void Update()
    {
        if(Time.timeAsDouble >= expirationTime)
        {
            Destroy(gameObject);
        }
    }
    private void Hit(Vector3 impactPosition)
    {
        // Spawn particle effect at impact location.

        Destroy(gameObject);
    }
}
