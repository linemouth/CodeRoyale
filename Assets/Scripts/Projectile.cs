using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;
using Utils.Unity;

public class Projectile : MonoBehaviour
{
    public Boat Source { get; private set; }
    public float Energy { get; private set; }
    public const float muzzleVelocity =100;
    public static GameObject impactEffectPrefab = null;

    private double expirationTime = double.PositiveInfinity;
    private new Collider collider;

    public void Initialize(Boat source, float energy, Vector3 position, Vector3 direction, Vector3 relativeVelocity)
    {
        Source = source;
        Energy = energy;
        transform.position = position;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        Vector3 velocity = transform.forward * muzzleVelocity + relativeVelocity;

        Rigidbody rb = transform.GetComponent<Rigidbody>();
        rb.velocity = velocity;
        expirationTime = Time.timeAsDouble + 10;
    }

    private void Awake()
    {
        if(impactEffectPrefab == null)
        {
            impactEffectPrefab = Resources.Load<GameObject>("Prefabs/Impact Sparks");
        }

        collider = GetComponent<Collider>();
    }
    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];

        if(collision.collider.TryGetComponentInParent(out Boat boat))
        {
            // We hit a boat. Let's make sure it wasn't the source boat or an ally.
            if(boat == Source || (Source.Faction != null && boat.Faction == Source.Faction))
            {
                // Ignore Source and allied boats.
                Physics.IgnoreCollision(collider, collision.collider);
            }
            else {
                // Damage the boat.
                boat.Damage(Source, Energy);
                Hit(contact.point, contact.normal);
            }
        }
        else if(collision.collider.TryGetComponentInParent(out Projectile projectile))
        {
            // Hit another projectile. Let's check to see if its another projectile from Source or an ally.
            if(Source == projectile.Source || (Source.Faction != null && projectile.Source.Faction == Source.Faction))
            {
                // Ignore projectiles of Source and allied boats.
                Physics.IgnoreCollision(collider, collision.collider);
            }
            else
            {
                // Cancel the other projectile.
                projectile.Hit(contact.point, projectile.transform.forward);
                Hit(contact.point, transform.forward);
            }
        }
        else if(collision.collider.TryGetComponentInParent(out Powerup powerup))
        {
            // Damage the powerup.
            powerup.Damage(Source, Energy);
            Hit(contact.point, contact.normal);
        }
        else
        {
            // We hit something else, like an obstacle. Just show the impact effect.
            Hit(contact.point, contact.normal);
        }
    }
    private void Hit(Vector3 position, Vector3 effectDirection)
    {
        // Spawn particle effect at impact location.
        Instantiate(impactEffectPrefab, position, Quaternion.LookRotation(effectDirection));

        Destroy(gameObject);
    }
}
