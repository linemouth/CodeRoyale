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
                if (targets.TryGetValue(target, out var oldTarget))
                {
                    oldTarget.Update(target);
                }
                else
                {
                    targets.Add(target);
                }
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
            float distance = (target.EstimatedPosition - GunPosition).magnitude;
            float interceptTime = distance / Projectile.muzzleVelocity;
            Vector2 relativeInterceptPosition = target.EstimatedPosition - GunPosition + (target.Velocity - Velocity) * interceptTime;
            float targetHeading = DirectionToHeading(relativeInterceptPosition);
            float gunAimError = Math.Abs(GunHeading - targetHeading);
            float gunAimMargin = (float)Math.RadToDeg * Math.Atan(2 / distance);
            
            if(gunAimError < gunAimMargin) {
                if(target.Type == "Boat")
                {
                    if(relativeInterceptPosition.magnitude < 100 && Energy >= 15)
                    {
                        FireShotgun(15);
                        Fire(Energy);
                    }
                    else
                    {
                        FireShotgun(5);
                        Fire(Energy);
                    }
                    break;
                }
                if(target.Name == "Health Powerup" && Health < 15 && Energy > 10)
                {
                    Fire(5);
                    break;
                }
                if(target.Name == "Energy Powerup" && Energy < 20)
                {
                    Fire(5);
                    break;
                }
            }
        }
    }
    public override void Update1()
    {
        PurgeTargets(target => target.Age > 1);
    }
}
