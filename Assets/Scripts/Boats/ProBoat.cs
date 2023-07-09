﻿using System;
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
    protected HashSet<TargetInformation> targets = new HashSet<TargetInformation>();

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
            Debug.Log($"ProBoat sees {target.Name}");
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

                // Aim at the target
                float gunAzimuth = DirectionToAzimuth(target.EstimatedPosition - Position);
                SetGunAzimuth(gunAzimuth);

                // Turn to face the target
                float targetHeading = DirectionToHeading(target.EstimatedPosition - Position);
                SetHeading(targetHeading);

                // Drive towards the target
                Vector2 localRelativePosition = WorldToLocalPosition(target.EstimatedPosition);
                SetThrust(localRelativePosition.x, localRelativePosition.y);

                // Shoot furiously!
                Fire(0);

                break;

            case Priority.Refuel:
                // Search for energy powerups.
                break;

            case Priority.Repair:
                // Search for health powerups.
                break;
        }
    }
}
