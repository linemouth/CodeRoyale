using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

/*public class CornerBoat : BoatController
{
    public override void OnRadarHit(TargetInformation target)
    {
        if(Faction == null || target.Faction != Faction)
        {
            if(target.Type == "Boat" || target.Type == "Powerup")
            {
                if(targets.Contains(target))
                {
                    // Update existing target;
                    targets.Remove(target);
                }
                targets.Add(target);
            }
        }
    }
    public override void Start()
    {
        Vector2 corner = 100000 * Position.normalized;
        SetHeading(DirectionToHeading(corner));
        SetThrust(1, 0);
        SetRadarRotationSpeed(30);
    }
    public override void Update()
    {
        foreach(var target in targets)
        {
            if(target.Age > 1)
            {
                targets.Remove(target);
                break;
            }
            float targetHeading = DirectionToHeading(target.EstimatedPosition - GunPosition);
            float gunAimError = targetHeading - GunHeading;
            if(Mathf.Abs(gunAimError) < 0.5f)
            {
                if(target.Name == "Health Powerup" && Energy > 15)
                {
                    Fire(5);
                    break;
                }
                else if(target.Name == "Energy Powerup" && Energy < 20)
                {
                    Fire(5);
                    break;
                }
                else if(target.Type == "Boat" && Energy > 10)
                {
                    Fire(Energy * 0.25f);
                    break;
                }
            }
        }
    }
}*/
