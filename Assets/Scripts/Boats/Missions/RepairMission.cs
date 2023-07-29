using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils.Unity;
using Math = Utils.Math;

public class RepairMission : Mission
{
    protected readonly BoatController boatController;
    protected TargetInformation currentTarget;

    public RepairMission(BoatController boatController) : this("Repair", boatController) { }
    public RepairMission(string name, BoatController boatController) : base(name)
    {
        this.boatController = boatController;
    }
    public override void Update()
    {
        if(currentTarget == null || !currentTarget.IsValid)
        {
            Abort();
            return;
        }

        Vector2 relativePosition = boatController.WorldToLocalPosition(currentTarget.Position);
        float targetAzimuth = (float)(Math.Atan2(relativePosition.x, relativePosition.y) * Math.RadToDeg);
        boatController.SetRudder(targetAzimuth);
        boatController.SetThrust(relativePosition.y, relativePosition.x);
        boatController.DrawLine(currentTarget.Position, Color.yellow);
    }
    public override void OnAcquiredPriority()
    {
        boatController.SetRadarRotationSpeed(100000);
        boatController.SetGunAzimuth(0);
    }

    protected override float CalculatePriority()
    {
        UpdateTarget();

        float priority = 0.75f * Math.Pow(1 - boatController.HealthFraction, 2);
        if(currentTarget?.IsValid ?? false)
        {
            Vector2 searchOrigin = boatController.Position + boatController.Forward * 50;
            float distance = (currentTarget.Position - searchOrigin).magnitude;
            priority += Math.Remap(distance, 0, 500, 0.25f, 0);
        }
        return priority;
    }
    protected void UpdateTarget()
    {
        Vector2 searchOrigin = boatController.Position + boatController.Forward * 50;
        float bestSqrDistance = float.PositiveInfinity;
        currentTarget = default;

        foreach(TargetInformation target in boatController.targets.Where(target => target.Name == "Health Powerup"))
        {
            float sqrDistance = (target.Position - searchOrigin).sqrMagnitude;
            if(sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                currentTarget = target;
            }
        }

        if(!currentTarget?.IsValid ?? true)
        {
            Abort();
        }
    }
}
