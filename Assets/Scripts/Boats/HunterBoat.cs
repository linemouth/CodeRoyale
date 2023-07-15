using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using Color = UnityEngine.Color;

public class HunterBoat : BoatController
{
    private const float approachDistance = 80;
    private HashSet<TargetInformation> targets = new HashSet<TargetInformation>();
    private HashSet<TargetInformation> ignoredTargets = new HashSet<TargetInformation>();
    private float lastSearchCompletedTime = 0;
    private float searchTimeRemaining = 1f;
    private int burstFireRound = 0;
    
    protected class Mission
    {
        public string name;
        public float priority;
        public TargetInformation target;

        private Action<HunterBoat, Mission> update;

        public Mission(string name, Action<HunterBoat, Mission> update) : this(name, 0)
        {
            this.name = name;
            this.update = update;
        }
        public Mission(string name, float priority)
        {
            this.name = name;
            this.priority = priority;
        }
        public void Update(HunterBoat boat)
        {
            update?.Invoke(boat, this);
        }
        public override string ToString() => $"{name}: {priority:0.00}";
    }
    protected List<Mission> missions = new List<Mission>
    {
        new Mission("Hunt", UpdateHuntMission),
        new Mission("Refuel", UpdateRefuelMission),
        new Mission("Repair", UpdateRepairMission),
        new Mission("Search", UpdateSearchMission)
    };

    public override void OnRadarHit(TargetInformation target)
    {
        if((Faction == null || target.Faction != Faction)
            && (target.Type == "Boat" || target.Type == "Powerup")
            && !ignoredTargets.Contains(target)
        )
        {
            if(targets.Contains(target))
            {
                // Update existing target.
                targets.Remove(target);
            }
            targets.Add(target);
        }
    }
    public override void Start()
    {
        Mission hunt = missions[0];
        Mission refuel = missions[1];
        Mission repair = missions[2];
        Mission search = missions[3];
        StatBlock.Add(new StatBar(() => hunt.priority,   Color.red,    new Color(0, 0, 0, 0.3f), new Vector2(50, 2), "Hunt Priority"));
        StatBlock.Add(new StatBar(() => refuel.priority, Color.blue,   new Color(0, 0, 0, 0.3f), new Vector2(50, 2), "Refuel Priority"));
        StatBlock.Add(new StatBar(() => repair.priority, Color.yellow, new Color(0, 0, 0, 0.3f), new Vector2(50, 2), "Repair Priority"));
        StatBlock.Add(new StatBar(() => search.priority, Color.green,  new Color(0, 0, 0, 0.3f), new Vector2(50, 2), "Search Priority"));
    }
    public override void Update()
    {
        // Remove stale target data.
        targets.RemoveWhere(t => t.Type == "Boat" && t.Age >= 1 || t.Age >= 10);
        ignoredTargets.RemoveWhere(t => t.Age >= 3);

        // Update mission priorities and select top priority.
        foreach(Mission mission in missions)
        {
            mission.Update(this);
        }
        missions.SortBy(m => m.priority, true);
        Mission activeMission = missions.First();
        
        if(activeMission.target.IsValid)
        {
            Vector2 relativePosition = (activeMission.target.EstimatedPosition - Position);
            float targetHeading = DirectionToHeading(relativePosition);
            // If the target isn't up to date, and the radar is pointed at the right spot, we must not know where it is. Remove it.
            if(Mathf.Abs(targetHeading - RadarHeading) < 2 && activeMission.target.Age > 0.2f)
            {
                targets.Remove(activeMission.target);
                activeMission.target = default;
            }
        }

        // If we have an active mission target, prosecute it.
        if(activeMission.target.IsValid)
        {
            if(activeMission.name == "Hunt")
            {
                ProsecuteTarget(activeMission.target);
            }
            else
            {
                RamTarget(activeMission.target);
                Search();
            }
        }
        else
        {
            FullStop();
            Search();
        }
    }

    protected bool TryGetNearestTarget(Func<TargetInformation, bool> predicate, out TargetInformation target)
    {
        target = targets.Where(predicate).OrderBy(t => (t.Position - Position).sqrMagnitude).FirstOrDefault();
        return target.IsValid;
    }
    protected static void UpdateHuntMission(HunterBoat boat, Mission mission)
    {
        if(boat.TryGetNearestTarget(t => t.Type == "Boat", out mission.target))
        {
            float distance = (mission.target.Position - boat.Position).magnitude;
            float threat = Utils.Math.Sigmoid(distance / 150 - 1);
            mission.priority = 2 * threat * Mathf.Min(boat.EnergyFraction, boat.HealthFraction);
        }
        else
        {
            mission.priority = 0;
        }
    }
    protected static void UpdateRefuelMission(HunterBoat boat, Mission mission)
    {
        if(boat.TryGetNearestTarget(t => t.Name == "Energy Powerup", out mission.target))
        {
            mission.priority = Mathf.Pow(1 - boat.EnergyFraction, 2);
        }
        else
        {
            mission.priority = 0;
        }
    }
    protected static void UpdateRepairMission(HunterBoat boat, Mission mission)
    {
        if(boat.TryGetNearestTarget(t => t.Name == "Health Powerup", out mission.target))
        {
            mission.priority = Mathf.Pow(1 - boat.HealthFraction, 2);
        }
        else
        {
            mission.priority = 0;
        }
    }
    protected static void UpdateSearchMission(HunterBoat boat, Mission mission)
    {
        float timeSinceLastSearch = Time.time - boat.lastSearchCompletedTime;
        mission.priority = boat.searchTimeRemaining > 0 ? 0.15f : timeSinceLastSearch * 0.01f;
    }

    private void FullStop()
    {
        SetThrust(0, 0);
        SetRudder(0);
        SetGunAzimuth(0);
    }
    private void Search()
    {
        if(searchTimeRemaining == 0)
        {
            // Start a sweep.
            searchTimeRemaining = 1.5f;
        }
        else
        {
            // Continue an ongoing sweep.
            searchTimeRemaining -= Time.deltaTime;

            if(searchTimeRemaining < 0)
            {
                lastSearchCompletedTime = Time.time;
            }
        }

        // Sweep the radar slower and slower as we search to gradually increase our range.
        SetRadarRotationSpeed(20 + 25 * Mathf.Max(0, searchTimeRemaining));
    }
    private void LockRadar(TargetInformation target)
    {
        searchTimeRemaining = 0;
        Vector2 relativePosition = target.EstimatedPosition - Position;
        float targetHeading = DirectionToHeading(relativePosition);
        SetRadarHeading(targetHeading);
    }
    private Vector3 RamTarget(TargetInformation target)
    {
        Vector2 relativePosition = target.EstimatedPosition - Position;
        float interceptTime = (float)relativePosition.magnitude / Projectile.muzzleVelocity;
        Vector2 relativeInterceptPosition = relativePosition + target.Velocity * interceptTime;
        DrawX(target.Velocity * interceptTime + target.Position, Color.red);
        float interceptHeading = DirectionToHeading(relativeInterceptPosition);
        SetHeading(interceptHeading);
        float interceptAzimuth = Mathf.Repeat(interceptHeading - Heading + 180, 360) - 180;
        SetThrust(1 - 0.0111f * Mathf.Abs(interceptAzimuth), 0.05f * interceptAzimuth);
        SetGunAzimuth(interceptAzimuth);
        return relativeInterceptPosition;
    }
    private void ProsecuteTarget(TargetInformation target)
    {
        LockRadar(target);

        Vector3 relativeInterceptPosition = RamTarget(target);

        // Maintain approach distance from the target.
        float interceptDistance = relativeInterceptPosition.magnitude;
        float interceptHeading = DirectionToHeading(relativeInterceptPosition);
        SetThrust(0.1f * Utils.Math.Deadzone(interceptDistance - approachDistance, 15), 0);

        // If the gun is aimed, take a shot.
        float angularErrorMargin = Utils.Math.SoftMin(30, 400 / interceptDistance, 30);
        bool gunAimed = Mathf.Abs(interceptHeading - GunHeading) < angularErrorMargin;
        if(gunAimed)
        {
            // Fire a couple shots to intercept other projectiles.
            if (burstFireRound < 2)
            {
                Fire(0);
            }
            else
            {
                float aggression = 3 * Mathf.Pow(approachDistance * EnergyFraction / interceptDistance, 2);
                Fire(aggression);
            }

            burstFireRound++;
            if(burstFireRound > 2)
            {
                burstFireRound = 0;
            }
        }
    }
}
