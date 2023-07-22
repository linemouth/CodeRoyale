using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using Utils;
using Utils.Unity;
using static UnityEngine.GraphicsBuffer;
using Color = UnityEngine.Color;
using Math = Utils.Math;

public class HuntMission : Mission
{
    public float approachDistance = 80;

    protected readonly BoatController boatController;
    protected TargetInformation currentTarget;
    protected float lastKillTime = float.NegativeInfinity;

    private int burstFireRound = 0;

    public HuntMission(BoatController boatController) : this("Hunt", boatController) { }
    public HuntMission(string name, BoatController boatController) : base(name)
    {
        this.boatController = boatController;
    }
    public override float GetPriority()
    {
        // The urge to hunt grows with each moment and must be satisfied!
        float hunger = Math.SmoothClamp((Time.time - lastKillTime - 10) / 10 + 0.5f);

        // Panic from several nearby threats may trigger a hunting spree!
        float generalThreatLevel = UpdateTargets();

        // What's the danger from the closest target?
        float nearestTargetThreat = 0;
        if(currentTarget != null)
        {
            nearestTargetThreat = Math.SmoothClamp(1 - (currentTarget.EstimatedPosition - boatController.Position).sqrMagnitude / 500);
        }

        return hunger * 0.3f
            + generalThreatLevel * 0.4f
            + nearestTargetThreat * 0.6f;
    }
    public override void Update()
    {
        // Abort if we don't have a good target.
        if(!ConfirmTarget())
        {
            Abort();
            return;
        }

        // Let's go!
        ProsecuteTarget();
    }
    public override void OnLostPriority()
    {
        // Reset our state.
        currentTarget = null;
        burstFireRound = 0;
    }

    protected float UpdateTargets()
    {
        Vector2 searchOrigin = boatController.Position + boatController.Forward * 50;
        float generalThreatLevel = 0;

        // Make a list of valid targets with threat levels for each.
        List<(TargetInformation info, float threatLevel)> targets = new List<(TargetInformation info, float distance)>();
        foreach(TargetInformation target in boatController.targets)
        {
            // Check that the target is a hostile boat.
            if(target.Type == "Boat" && (target.Faction == null || target.Faction != boatController.Faction))
            {
                // Calculate the target's threat level based on proximity.
                Vector2 targetFuturePosition = target.Position + target.Velocity * (target.Age + 3);
                float sqrDistance = (targetFuturePosition - searchOrigin).sqrMagnitude;
                float threatLevel = Math.SoftMax(1 - sqrDistance / 1000000, 0, 1);

                generalThreatLevel += threatLevel;
                targets.Add((target, threatLevel));
            }
        }

        if(targets.Count > 0)
        {
            // Sort the targets by threat level.
            targets.SortBy(target => target.threatLevel);

            // Pursue the closest target.
            var newTarget = targets.FirstOrDefault().info;
            if(newTarget != currentTarget)
            {
                currentTarget = newTarget;
            }
        }

        return generalThreatLevel;
    }
    protected bool ConfirmTarget()
    {
        // If there's no current target, then try to find a new one.
        if(currentTarget == null || !currentTarget.IsValid)
        {
            UpdateTargets();
        }

        // If there's still no target available, then we can't hunt..
        if(currentTarget == null || !currentTarget.IsValid)
        {
            return false;
        }

        // Set the radar direction to that of the target, plus a sweeping action using Sin().
        Vector2 relativePosition = currentTarget.EstimatedPosition - boatController.Position;
        float targetHeading = BoatController.DirectionToHeading(relativePosition);
        targetHeading += (float)Math.Sin(Time.timeAsDouble * 6.28) * 15;
        boatController.SetRadarHeading(targetHeading);

        // If the radar is looking at where the target should be, make sure the target is still there. If it isn't, stop pursuing.
        if(Mathf.Abs(targetHeading - boatController.RadarHeading) < 2 && currentTarget.Age > 0.2f)
        {
            currentTarget.Invalidate();
            return false;
        }

        // Otherwise, the current target is greenlit!
        return true;
    }
    protected void ProsecuteTarget()
    {
        // Determine the target's location in local coordinates.
        float distance = (currentTarget.EstimatedPosition - boatController.Position).magnitude;
        float interceptTime = distance / Projectile.muzzleVelocity;
        Vector2 interceptPosition = currentTarget.EstimatedPosition + currentTarget.Velocity * interceptTime;
        BoatController.DrawX(interceptPosition, Color.red);

        // Steer towards the target.
        float interceptAzimuth = boatController.DirectionToAzimuth(interceptPosition);
        boatController.SetRudder(interceptAzimuth / 5);
        Vector2 localInterceptPosition = boatController.WorldToLocalPosition(interceptPosition);
        localInterceptPosition.y -= approachDistance; // Provide an approach distance
        boatController.SetThrust(localInterceptPosition.y, localInterceptPosition.x);

        // Aim gun.
        boatController.SetGunAzimuth(interceptAzimuth);

        // Fire when ready.
        float gunAimError = Math.Abs(boatController.GunAzimuth - interceptAzimuth);
        float gunAimMargin = 450 / distance; // Approximates the equation "Math.RadToDeg * Math.Atan(8 / x)"
        if(gunAimError < gunAimMargin)
        {
            // Fire the main gun using a burst fire pattern. This way, we can shield our most powerful round with cheap rounds to intercept other bullets.
            if(burstFireRound < 2 && boatController.Fire(0))
            {
                burstFireRound++;
            }
            else
            {
                float firingEnergy = gunAimMargin; // Turns out this is pretty close to an ideal firepower, too!
                boatController.Fire(firingEnergy);
                burstFireRound = 0;
            }

            // Maybe fire the shotgun, too?
            if(distance < 50)
            {
                boatController.FireShotgun(15);
            }
            else if(distance < 75)
            {
                boatController.FireShotgun(10);
            }
            else if(distance < 100)
            {
                boatController.FireShotgun(5);
            }
        }
    }
}

