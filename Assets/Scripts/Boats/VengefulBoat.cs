using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using Utils.Unity;
using static UnityEngine.EventSystems.EventTrigger;
using Color = UnityEngine.Color;
using Math = Utils.Math;

public class VengefulBoat : BoatController
{
    public override Color HullColor => new Color(0.00f, 0.40f, 0.10f);
    public override Color GunColor => new Color(0.00f, 0.80f, 0.20f);
    public override Color WheelhouseColor => new Color(0.00f, 0.80f, 0.20f);
    public override Color EngineColor => new Color(0.00f, 0.80f, 0.20f);

    private static string killedBy = null;
    private TargetInformation target;
    private int burstFireRound = 0;

    public override void OnRadarHit(TargetInformation target)
    {
        if(target.Name == killedBy)
        {
            this.target = target;
        }
    }
    public override void Update()
    {
        if(target == null || !target.IsValid || target.Age > 1)
        {
            target = null;

            // Scan quickly.
            SetRadarRotationSpeed(180);

            // Move to the origin.
            float originHeading = DirectionToHeading(-Position);
            Vector2 originPosition = WorldToLocalPosition(Vector2.zero);
            SetHeading(originHeading);
            SetThrust(originPosition.y, originPosition.x);
        }
        else
        {
            // Calculate target position.
            Vector2 relativePosition = target.EstimatedPosition - Position;
            float distance = relativePosition.magnitude;
            float interceptTime = distance / Projectile.muzzleVelocity;
            Vector2 interceptPosition = target.EstimatedPosition + target.Velocity * interceptTime;

            // Approach target.
            float targetHeading = DirectionToHeading(relativePosition);
            SetHeading(targetHeading);
            Vector2 localPosition = WorldToLocalPosition(target.EstimatedPosition);
            SetThrust(localPosition.y - 100, localPosition.x);

            // Look at target.
            float radarHeading = DirectionToHeading(target.EstimatedPosition - RadarPosition);
            SetRadarHeading(radarHeading + Math.Sin(Time.time * 20) * 15);

            // Aim at target.
            Vector2 relativeInterceptPosition = interceptPosition - GunPosition;
            float interceptAzimuth = DirectionToAzimuth(relativeInterceptPosition);
            SetGunAzimuth(interceptAzimuth);
            float targetAzimuth = (float)(Math.Atan2(relativePosition.x, relativePosition.y) * Math.RadToDeg);
            DrawLine(GunPosition, interceptPosition, Color.green);

            // Fire!
            float aimError = Math.Abs(GunAzimuth - interceptAzimuth);
            float aimMargin = (float)Math.RadToDeg * Math.Atan(3 / distance);
            if(aimError < aimMargin)
            {
                if(burstFireRound < 2)
                {
                    // Fire the main gun using a burst fire pattern. This way, we can shield our most powerful round with cheap rounds to intercept other bullets.
                    if(Fire(0))
                    {
                        burstFireRound++;
                    }
                }
                else
                {
                    // Shoot as hard as we can!
                    FireShotgun(Math.FloorToInt(Energy));
                    if(Fire(Energy))
                    {
                        burstFireRound = 0;
                    }
                }
            }
        }
    }
    public override void OnKilled(string killerName)
    {
        killedBy = killerName;
    }
}
