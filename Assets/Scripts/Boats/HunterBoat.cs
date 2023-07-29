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

public class HunterBoat : BoatController
{
    public override Color HullColor => new Color(0.41f, 0.34f, 0.28f);
    public override Color GunColor => new Color(0.28f, 0.23f, 0.19f);
    public override Color WheelhouseColor => new Color(0.19f, 0.16f, 0.13f);
    public override Color EngineColor => new Color(0.28f, 0.23f, 0.19f);

    protected MissionManager missionManager;
    protected StatBar searchUrge;
    protected StatBar repairUrge;
    protected StatBar refuelUrge;
    protected StatBar huntUrge;

    private Mission searchMission;
    private Mission repairMission;
    private Mission refuelMission;
    private Mission huntMission;

    public override void OnRadarHit(TargetInformation target)
    {
        if((target.Type == "Boat" || target.Type == "Powerup")
            && (Faction == null || target.Faction != Faction))
        {
            if(targets.TryGetValue(target, out var oldTarget))
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
        base.Start();

        // Missions
        searchMission = new SearchMission(this);
        repairMission = new RepairMission(this);
        refuelMission = new RefuelMission(this);
        huntMission   = new HuntMission(this);
        missionManager    = new MissionManager() { searchMission, repairMission, refuelMission, huntMission };
        missionManager.Update();

        // Debugging status bars
        var dark = new Color(0, 0, 0, 0.3f);
        //searchUrge = AddStat(new StatBar(Color.green,  Color.black, new Vector2(64, 3), "Search"));
        //repairUrge = AddStat(new StatBar(Color.yellow, Color.black, new Vector2(64, 3), "Repair"));
        //refuelUrge = AddStat(new StatBar(Color.blue,   Color.black, new Vector2(64, 3), "Refuel"));
        //huntUrge   = AddStat(new StatBar(Color.red,    Color.black, new Vector2(64, 3), "Hunt"));

        // Connect events
        //searchMission.PriorityChanged += value => searchUrge.Value = value;
        //repairMission.PriorityChanged += value => repairUrge.Value = value;
        //refuelMission.PriorityChanged += value => refuelUrge.Value = value;
        //huntMission.PriorityChanged   += value => huntUrge.Value = value;
    }
    public override void Update()
    {
        base.Update();

        searchMission.UpdatePriority();
        missionManager.CurrentMission?.Update();
    }
    public override void Update1()
    {
        base.Update1();

        // Remove stale radar ghosts.
        PurgeTargets(target => target.Age > (target.Type == "Boat" ? 5 : 15));

        // Update mission priorities.
        missionManager.Update();

        // Run the current mission.
        missionManager.CurrentMission?.Update1();
    }
}
