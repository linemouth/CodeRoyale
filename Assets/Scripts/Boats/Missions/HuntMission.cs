using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
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

    protected override float CalculatePriority()
    {
        // The urge to hunt grows with each moment and must be satisfied!
        float hunger = Math.SmoothClamp((Time.time - lastKillTime - 10) / 10 + 0.5f);

        // Panic from several nearby threats may trigger a hunting spree!
        float generalThreatLevel = UpdateTargets();

        // What's the danger from the closest target?
        float nearestTargetThreat = 0;
        if(currentTarget?.IsValid ?? false)
        {
            float distance = (currentTarget.EstimatedPosition - boatController.Position).magnitude;
            nearestTargetThreat = 1 - distance / 1000;
            nearestTargetThreat = Math.SmoothClamp(nearestTargetThreat);

            hunger *= 0.3f;
            generalThreatLevel *= 0.2f;
            nearestTargetThreat *= 0.6f;
            return hunger + generalThreatLevel + nearestTargetThreat;
        }

        List<TargetInformation> targets = boatController.targets.Where(target => target.IsValid && target.Type == "Boat").ToList();

        return 0;
    }
    protected float UpdateTargets()
    {
        Vector2 searchOrigin = boatController.Position + boatController.Forward * 50;
        float generalThreatLevel = 0;

        // Check if the current target was invalidated.
        if(!currentTarget?.IsValid ?? false)
        {
            currentTarget = null;
        }

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
            targets.SortBy(target => target.threatLevel, true);

            // Pursue the closest target.
            var newTarget = targets.FirstOrDefault().info;

            if(newTarget != currentTarget)
            {
                currentTarget = newTarget;
                //Debug.Log($"Switching to pursue {currentTarget}.");
            }
            else if(currentTarget != null)
            {
                //Debug.Log($"Pursuing {currentTarget}.");
            }
            else
            {
                //Debug.Log("wut");
            }
        }
        else
        {
            //Debug.Log("I have no targets.");
        }

        return generalThreatLevel;
    }
    protected bool ConfirmTarget()
    {
        // If there's no current target, then try to find a new one.
        if(currentTarget == null || !currentTarget.IsValid)
        {
            if(currentTarget == null)
            {
                //Debug.Log($"currentTarget is null.");
            }
            else
            {
                //Debug.Log($"currentTarget is invalid.");
            }
            UpdateTargets();


            // If there's still no target available, then we can't hunt..
            if(currentTarget == null || !currentTarget.IsValid)
            {
                currentTarget = null;
                return false;
            }
        }

        // Set the radar direction to that of the target, plus a sweeping action using Sin().
        Vector2 relativePosition = currentTarget.EstimatedPosition - boatController.RadarPosition;
        float targetHeading = BoatController.DirectionToHeading(relativePosition);
        float distance = relativePosition.magnitude;
        float margin = (float)Math.RadToDeg * Math.Atan(currentTarget.Age * 10 / distance) + 10;
        targetHeading += (float)Math.Sin(Time.timeAsDouble * 15) * Math.Clamp(margin);
        boatController.SetRadarHeading(targetHeading);

        // If the radar is looking at where the target should be, make sure the target is still there. If it isn't, stop pursuing.
        if(currentTarget.LastSeen > LastTimeAcquiredPriority)
        {
            if(Mathf.Abs(targetHeading - boatController.RadarHeading) < 1 && currentTarget.Age > 0.25f)
            {
                //Debug.Log($"Aborting pursuit of {currentTarget} because I'm looking and it's not there.");
                currentTarget.Invalidate();
                UpdateTargets();
                return false;
            }
            else if(currentTarget.Age > 1)
            {
                //Debug.Log($"Aborting pursuit of {currentTarget} because I haven't seen it in a while.");
                currentTarget.Invalidate();
                UpdateTargets();
                return false;
            }
        }

        // Otherwise, the current target is greenlit!
        return true;
    }
    protected void ProsecuteTarget()
    {
        // Determine the target's location in local coordinates.
        float distance = (currentTarget.EstimatedPosition - boatController.GunPosition).magnitude;
        float interceptTime = distance / Projectile.muzzleVelocity;
        BoatController.DrawX(currentTarget.Position, Color.white);
        BoatController.DrawX(currentTarget.EstimatedPosition, Color.red);
        Vector2 interceptPosition = currentTarget.EstimatedPosition + currentTarget.Velocity * interceptTime;
        BoatController.DrawX(interceptPosition, Color.yellow);
        Vector2 compensatedInterceptPosition = interceptPosition - boatController.Velocity * interceptTime;
        BoatController.DrawX(compensatedInterceptPosition, Color.green);

        // Steer towards the target.
        float interceptAzimuth = boatController.DirectionToAzimuth(interceptPosition - boatController.GunPosition);
        boatController.SetRudder(interceptAzimuth / 5);
        Vector2 localInterceptPosition = boatController.WorldToLocalPosition(interceptPosition);
        localInterceptPosition.y -= approachDistance; // Provide an approach distance
        boatController.SetThrust(localInterceptPosition.y, localInterceptPosition.x);

        // Aim gun.
        boatController.SetGunAzimuth(interceptAzimuth);

        // Fire when ready.
        float gunAimError = Math.Abs(boatController.GunAzimuth - interceptAzimuth);
        float gunAimMargin = (float)Math.RadToDeg * Math.Atan(1 / distance);
        if(gunAimError < gunAimMargin)
        {
            // Maybe fire the shotgun?
            if(distance < 100)
            {
                boatController.FireShotgun(15);
            }
            else if(distance < 200)
            {
                boatController.FireShotgun(10);
            }
            else if(distance < 400)
            {
                boatController.FireShotgun(5);
            }

            // Fire the main gun using a burst fire pattern. This way, we can shield our most powerful round with cheap rounds to intercept other bullets.
            if(burstFireRound < 2)
            {
                if(boatController.Fire(0))
                {
                    burstFireRound++;
                }
            }
            else
            {
                if(boatController.Fire(boatController.Energy))
                {
                    burstFireRound = 0;
                }
            }
        }
    }
}

