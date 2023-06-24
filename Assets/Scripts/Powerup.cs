using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Utils;

public class Powerup : MonoBehaviour
{
    public static string[] Types => TypeInfos.Keys.ToArray();
    public string Name => $"{Type} Powerup";
    public string Type
    {
        get => type;
        set
        {
            type = value;
            PowerupResources info = TypeInfos[type];
            transform.Find("Icon Layer").GetComponent<MeshRenderer>().material = info.material;
            amount = Utils.Math.Random(info.amount);
            gameObject.name = Name;
        }
    }
    public Vector2 Position => Geometry.ToPlanarPoint(transform.position, GameManager.basisVectors);
    public Action<Powerup> Collected;
    public ResourceCache health = new ResourceCache(5, 5);

    private class PowerupResources
    {
        public Material material;
        public float amount;

        public PowerupResources(Material material, float amount)
        {
            this.material = material;
            this.amount = amount;
        }
    }
    private static Dictionary<string, PowerupResources> TypeInfos
    {
        get
        {
            if(typeInfos == null)
            {
                typeInfos = new Dictionary<string, PowerupResources>
                {
                    { "Health", new PowerupResources(Resources.Load<Material>("Materials/HealthPowerup"), 10) },
                    { "Energy", new PowerupResources(Resources.Load<Material>("Materials/EnergyPowerup"), 10) }
                };
            }
            return typeInfos;
        }
    }
    private static Dictionary<string, PowerupResources> typeInfos;
    private string type;
    private float amount;
    private MeshFilter meshFilter;
    private new MeshRenderer renderer;
    private new SphereCollider collider;

    public void ApplyTo(Boat boat)
    {
        switch(type)
        {
            case "Health":
                boat.health.Offer(amount);
                break;
            case "Energy":
                boat.energy.Offer(amount);
                break;
        }
        Destroy(gameObject);
    }
    public void Damage(Boat source, float energy)
    {
        energy = (float)health.Request(energy);
        bool collect = health.IsEmpty;
        GameManager.RecordHitPowerup(source, this, energy, collect);
        if(collect)
        {
            ApplyTo(source);
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        Vector3 rotation = transform.localEulerAngles;
        rotation.y += 120 * Time.deltaTime;
        transform.localEulerAngles = rotation;
    }
    private void OnCollisionEnter(Collision collision)
    {
        Boat boat = collision.collider.GetComponentInParent<Boat>();
        if(boat != null)
        {
            GameManager.RecordCollectedPowerup(boat, this);
            ApplyTo(boat);
        }
    }
    private void OnDestroy()
    {
        Collected?.Invoke(this);
    }
}
