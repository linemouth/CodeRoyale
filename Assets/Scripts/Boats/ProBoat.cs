using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class ProBoat : BoatController
{
    protected float gunTargetAzimuth = 120;
    protected enum Priority
    {
        Dumbfire,
        Hunt,
        Refuel,
        Repair
    }
    protected Priority priority = Priority.Hunt;
    
    public override void OnRadarHit(TargetInformation target)
    {
        if ((Faction == null || target.Faction != Faction)
            && (target.Type == "Boat" || target.Type == "Powerup")
        )
        {
            if (targets.Contains(target))
            {
                // Update existing target.
                targets.Remove(target);
            }
            targets.Add(target);
            //Debug.Log($"ProBoat sees {target.Name}");
        }
    }
    public override void Start()
    {
        SetGunAzimuth(gunTargetAzimuth);
        SetRadarRotationSpeed(1000);
    }
    public override void Update()
    {
        switch (priority)
        {
            case Priority.Dumbfire:
                // Swing the turret back and forth.
                float gunAimError = Math.Abs(GunAzimuth - gunTargetAzimuth);
                if (gunAimError < 1)
                {
                    gunTargetAzimuth = -gunTargetAzimuth;
                    SetGunAzimuth(gunTargetAzimuth);
                }

                // Shoot as fast as we can.
                Fire(0);
                break;

            case Priority.Hunt:
                TargetInformation target = targets
                    // Get only boat targets
                    .Where(target => target.Type == "Boat")
                    // And sort them by square distance because that's faster than actual distance
                    .OrderBy(boat => Vector2.SqrMagnitude(boat.EstimatedPosition - Position))
                    // Then get the closest one
                    .FirstOrDefault();

                if (target?.IsValid ?? false)
                {
                    // Aim at the target.
                    float gunAzimuth = DirectionToAzimuth(target.EstimatedPosition - Position);
                    SetGunAzimuth(gunAzimuth);

                    // Shoot furiously!
                    Fire(1f);
                }
                else
                {
                    target = new TargetInformation("Origin", "Location", null, Vector2.zero, Vector2.zero, -1);
                }

                // Turn to face the target.
                float targetHeading = DirectionToHeading(target.EstimatedPosition - Position);
                SetHeading(targetHeading);

                // Drive towards the target until you're 25 meters from it.
                Vector2 localRelativePosition = WorldToLocalPosition(target.EstimatedPosition);
                SetThrust(localRelativePosition.y - 25, localRelativePosition.x);

                break;

            case Priority.Refuel:
                // Search for energy powerups.
                break;

            case Priority.Repair:
                // Search for health powerups.
                break;
        }
    }
    public override void Update1()
    {
        // Clear stale target information.
        targets.RemoveWhere(target => target.Age > 1.5f);
    }
}
