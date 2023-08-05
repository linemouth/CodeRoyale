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

public class SniperBoat : BoatController
{
    private List<TargetInformation> Victims = new List<TargetInformation>();
    private float radarDirection = 1;
    public override Color HullColor => new Color(0.20f, 0.20f, 0.20f);
    public override Color GunColor => new Color(0.30f, 0.30f, 0.30f);
    public override Color WheelhouseColor => new Color(0.50f, 0.50f, 0.50f);
    public override Color EngineColor => new Color(0.40f, 0.40f, 0.40f);

    public TargetInformation target;

    public override void OnRadarHit(TargetInformation target)
    {
        if ((target.Type == "Boat" || target.Type == "Powerup")
            && (Faction == null || target.Faction != Faction))
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
    public override void Start()
    {
        SetRadarRotationSpeed(60);
    }
    public override void Update()
    {
        //Move to corner and face the origin
        float distanceToOrigin = Position.magnitude;
        float directionFromOrigin = DirectionToHeading(Position);
        Vector2 directionToCorner = new Vector2(Math.Sign(Position.x), Math.Sign(Position.y));
        Vector2 cornerPosition = directionToCorner * 115;
        directionFromOrigin = (Math.Floor(directionFromOrigin / 90) + 0.5f) * 90;
        if (distanceToOrigin < 100)
        {
            SetThrust(1, 0);
            SetHeading(directionFromOrigin);
        }
        else
        {
            Vector2 thrustDirection = (cornerPosition - Position) / 10;
            Vector2 thrust = WorldToLocalDirection(thrustDirection);
            SetThrust(thrust.y, thrust.x);
            SetHeading(directionFromOrigin - 180);
        }

        //Sweep radar
        float radarError = Math.Abs(RadarAzimuth - 45 * radarDirection);
        if (radarDirection > 0 && RadarAzimuth > 45)
        {
            radarDirection = -1;
            SetRadarRotationSpeed(-60);
        }
        else if (radarDirection < 0 && RadarAzimuth < -45)
        {
            radarDirection = 1;
            SetRadarRotationSpeed(60);
        }
        //Select a victim.
        List<TargetInformation> availableTargets = targets.Where(target => !Victims.Contains(target)).Where(TargetIsValid).ToList();
        TargetInformation target = null;
        if (availableTargets.Count > 0)
        {
            target = SelectTarget(availableTargets);
        }
        else
        {
            target = SelectTarget(Victims.Where(TargetIsValid));
        }

        if (target != null)
        {
            Vector2 targetPosition = target.EstimatedPosition;
            Vector2 relativePosition = targetPosition - Position;
            float distance = relativePosition.magnitude;
            float interceptTime = distance / Projectile.muzzleVelocity;
            Vector2 interceptPosition = targetPosition + target.Velocity * interceptTime;
            Vector2 relativeInterceptPosition = interceptPosition - GunPosition;
            float targetInterceptAzimuth = this.DirectionToAzimuth(relativeInterceptPosition);
            SetGunAzimuth(targetInterceptAzimuth);

            //Fire :)
            float aimError = Math.Abs(GunAzimuth - targetInterceptAzimuth);
            float aimMargin = (float)Math.RadToDeg * Math.Atan(3 / distance);
            if (aimError < aimMargin)
            {
                Fire(Math.Min(Energy, 5));
                if (distance < 100)
                {
                    FireShotgun(Math.Min(15, Math.FloorToInt(Energy)));
                }
                if (Fire(Math.Min(Energy, 5)))
                {
                    if (Victims.Contains(target))
                    {
                        Victims.Remove(target);
                    }
                    Victims.Add(target);
                }
            }
        }
    }
    public override void Update1()
    {
        PurgeTargets(target => target.Age > 3);
    }
    protected static bool TargetIsValid(TargetInformation target)
    {
        return target.IsValid
            && target.Type == "Boat"
            && !target.Name.Contains("Vengeful");
    }
    protected TargetInformation SelectTarget(IEnumerable<TargetInformation> targets)
    {
        List<(TargetInformation target, float distance)> infos = new List<(TargetInformation target, float distance)>();
        if (targets.Count() > 0)
        {
            foreach (TargetInformation target in targets)
            {
                infos.Add((target, (target.EstimatedPosition - GunPosition).sqrMagnitude));
            }
            infos.SortBy(info => info.distance);
            return infos.First().target;
        }
        return null;
    }
}

        
       
        
