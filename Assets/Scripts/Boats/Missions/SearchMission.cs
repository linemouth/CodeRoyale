using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using Utils.Unity;
using static UnityEditor.SearchableEditorWindow;
using Color = UnityEngine.Color;
using Math = Utils.Math;

public class SearchMission : Mission
{
    public enum Range
    {
        Short,
        Medium,
        Long
    }

    protected readonly BoatController boatController;
    protected float nextSearchTime = 0;
    protected float startTime = 0;
    protected Range range = Range.Short;

    public SearchMission(BoatController boatController) : this("Search", boatController) { }
    public SearchMission(string name, BoatController boatController) : base(name)
    {
        this.boatController = boatController;
    }
    public override float GetPriority() => Math.SmoothClamp((Time.time - nextSearchTime) / 10 + 0.5f);
    public override void Update()
    {
        float elapsedTime = Time.time - startTime;
        switch(range)
        {
            case Range.Short:
                if(elapsedTime > 1.2)
                {
                    nextSearchTime = Time.time + 10;
                    SetRange(Range.Medium);
                }
                break;
            case Range.Medium:
                if(elapsedTime > 4.2)
                {
                    nextSearchTime = Time.time + 20;
                    SetRange(Range.Long);
                }
                break;
            case Range.Long:
                if(elapsedTime > 12.2)
                {
                    nextSearchTime = Time.time + 20;
                    SetRange(Range.Medium);
                }
                break;
        }
    }
    public override void OnAcquiredPriority()
    {
        SetRange(0);
        startTime = 0;
    }
    public void SetRange(Range range)
    {
        this.range = range;
        startTime = Time.time;
        switch(range)
        {
            case Range.Short:
                boatController.SetRadarRotationSpeed(100000);
                boatController.SetRudder(1000); // Spin right to increase radar speed.
                break;
            case Range.Medium:
                boatController.SetRadarRotationSpeed(100000);
                boatController.SetRudder(1000); // Spin right to increase radar speed.
                break;
            case Range.Long:
                boatController.SetRadarRotationSpeed(100000);
                boatController.SetRudder(1000); // Spin right to increase radar speed.
                break;
        }
        boatController.SetGunAzimuth(0);
        boatController.SetThrust(0);
    }
}
