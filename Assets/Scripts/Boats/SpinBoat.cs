using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class SpinBoat : BoatController
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
        SetRadarAzimuth(30);
        SetGunAzimuth(30);
        SetThrust(1, 0);
        SetRudder(1);
    }
    public override void Update()
    {
        // Sweep radar.
        SetRadarAzimuth(30 + 10 * (float)Math.Sin(Time.timeAsDouble * 10));

        // Shoot at convenient targets.
        foreach(var target in targets)
        {
            Vector2 relativePosition = target.EstimatedPosition - GunPosition;
            float targetHeading = DirectionToHeading(relativePosition);
            float gunAimError = Mathf.Abs(targetHeading - GunHeading);

            if(target.Type == "Boat" && relativePosition.magnitude < 100 && gunAimError < 10)
            {
                Fire(200);
                FireShotgun(15);
            }
            else if(gunAimError < 0.5f)
            {
                if(target.Name == "Health Powerup" && Health < 15 && Energy > 10)
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
    public override void Update1()
    {
        targets.RemoveWhere(target => target.Age > 1);
    }
}
