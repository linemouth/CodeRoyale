using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using Utils.Unity;
using Color = UnityEngine.Color;
using Math = Utils.Math;

public class HunterBoat : BoatController
{
    protected MissionManager missionManager;
    protected StatBar searchUrge;
    protected StatBar repairUrge;
    protected StatBar refuelUrge;
    protected StatBar huntUrge;

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

        var searchMission = new SearchMission(this);
        var repairMission = new RepairMission(this);
        var refuelMission = new RefuelMission(this);
        var huntMission   = new HuntMission(this);
        missionManager    = new MissionManager() { searchMission, repairMission, refuelMission, huntMission };
        var dark = new Color(0, 0, 0, 0.3f);
        searchUrge = new StatBar(Color.green,  dark, new Vector2(50, 2), "Search");
        repairUrge = new StatBar(Color.yellow, dark, new Vector2(50, 2), "Repair");
        refuelUrge = new StatBar(Color.blue,   dark, new Vector2(50, 2), "Refuel");
        huntUrge   = new StatBar(Color.red,    dark, new Vector2(50, 2), "Hunt");
    }
    public override void Update()
    {
        base.Update();
        
        missionManager.CurrentMission?.Update();
    }
    public override void Update1()
    {
        base.Update1();

        // Remove stale radar ghosts.
        targets.RemoveWhere(target => target.Age > (target.Type == "Boat" ? 3 : 5));

        // Update mission priorities.
        missionManager.Update();
    }
}
