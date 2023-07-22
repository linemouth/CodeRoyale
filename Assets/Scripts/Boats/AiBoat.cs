using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Utils;
using Utils.AI;

public class AiBoat : BoatController
{
    // Neural network to select a target.
    // Inputs:
    //   0 Distance to closest boat
    //   1 Distance to closest energy powerup
    //   2 Distance to closest health powerup
    //   3 Energy fraction
    //   4 Health fraction
    // Outputs:
    //   0 Select boat as target
    //   1 Select energy powerup as target
    //   2 Select health powerup as target
    public NeuralNet targetSelector = new NeuralNet(new int[] { 5, 10, 10, 3 });
    // Neural network to control mission execution.
    // Inputs:
    //   0 Target is boat
    //   1 Target is energy powerup
    //   2 Target is health powerup
    //   3 Target relative x position
    //   4 Target relative y position
    //   5 Target relative x velocity
    //   6 Target relative y velocity
    //   7 Energy
    //   8 Health
    //   9 Radar azimuth
    //  10 Gun azimuth
    // Outputs:
    //   0 Rudder
    //   1 Throttle x
    //   2 Throttle y
    //   3 Radar azimuth
    //   4 Gun azimuth
    //   5 Fire
    public NeuralNet missionProcessor = new NeuralNet(new int[] { 11, 20, 20, 9 });
    public float fitness = 0;

    private List<float> targetInputs = Enumerable.Repeat<float>(0, 5).ToList();
    private List<float> missionInputs = Enumerable.Repeat<float>(0, 11).ToList();
    private List<(int index, float priority)> targetRanks = new List<(int index, float distance)>(3);
    public string Blah { get; set; }

    public AiBoat() : this(new NeuralNet(new int[] { 5, 10, 10, 3 }), new NeuralNet(new int[] { 11, 20, 20, 9 })) { }
    public AiBoat(NeuralNet targetSelector, NeuralNet missionProcessor)
    {
        this.targetSelector = targetSelector;
        this.missionProcessor = missionProcessor;
    }
    public override void OnRadarHit(TargetInformation target)
    {
        if(Faction == null || target.Faction != Faction)
        {
            switch(target.Type)
            {
                case "Boat":
                case "Powerup":
                    if(targets.Contains(target))
                    {
                        targets.Remove(target);
                    }
                    targets.Add(target);
                    break;
            }
        }
    }
    public override void Start()
    {
        Vector2 corner = 100000 * Position.normalized;
        SetHeading(DirectionToHeading(corner));
        SetThrust(1, 0);
        SetRadarRotationSpeed(30);
    }
    public override void Update()
    {
        // Initialize target selection inputs.
        List<TargetInformation> sortedTargets = targets.OrderBy(target => (target.Position - Position).magnitude).ToList();
        TargetInformation boatTarget = sortedTargets.FirstOrDefault(target => target.Type == "Boat");
        TargetInformation energyTarget = sortedTargets.FirstOrDefault(target => target.Name == "Energy Powerup");
        TargetInformation healthTarget = sortedTargets.FirstOrDefault(target => target.Name == "Health Powerup");
        targetInputs[0] = boatTarget?.IsValid ?? false ? (boatTarget.Position - Position).magnitude : float.PositiveInfinity;
        targetInputs[1] = energyTarget?.IsValid ?? false ? (energyTarget.Position - Position).magnitude : float.PositiveInfinity;
        targetInputs[2] = healthTarget?.IsValid ?? false ? (healthTarget.Position - Position).magnitude : float.PositiveInfinity;
        targetInputs[3] = EnergyFraction;
        targetInputs[4] = HealthFraction;

        // Select target.
        List<float> targetOutputs = targetSelector.ForwardPropagate(targetInputs);
        List<(float priority, TargetInformation target)> rankedTargets = new List<(float priority, TargetInformation target)>
        {
            (targetOutputs[0], boatTarget),
            (targetOutputs[1], energyTarget),
            (targetOutputs[2], healthTarget)
        }.OrderBy(entry => entry.priority).ToList();
        return; // The function is currently breaking here.
        TargetInformation currentTarget = rankedTargets.FirstOrDefault(entry => entry.target?.IsValid ?? false).target;

        // Initialize mission processor inputs.
        if(!currentTarget?.IsValid ?? true)
        {
            missionInputs[0] = currentTarget.Type == "Boat" ? 1 : 0;
            missionInputs[1] = currentTarget.Name == "Energy Powerup" ? 1 : 0;
            missionInputs[2] = currentTarget.Name == "Health Powerup" ? 1 : 0;
            Vector3 relativePosition = currentTarget.Position - Position;
            Vector3 relativeVelocity = currentTarget.Velocity - Velocity;
            // Todo: transform relative vectors into local rotation frame.
            missionInputs[3] = relativePosition.x;
            missionInputs[4] = relativePosition.y;
            missionInputs[5] = relativeVelocity.x;
            missionInputs[6] = relativeVelocity.y;
        }
        else
        {
            missionInputs[0] = 0;
            missionInputs[1] = 0;
            missionInputs[2] = 0;
            missionInputs[3] = 0;
            missionInputs[4] = 0;
            missionInputs[5] = 0;
            missionInputs[6] = 0;
        }
        missionInputs[7] = EnergyFraction;
        missionInputs[8] = HealthFraction;
        missionInputs[9] = RadarAzimuth;
        missionInputs[10] = GunAzimuth;

        // Prosecute target
        List<float> missionOutputs = missionProcessor.ForwardPropagate(missionInputs);
        SetRudder(missionOutputs[0]);
        SetThrust(missionOutputs[2], missionOutputs[1]);
        SetRadarAzimuth(missionOutputs[3]);
        SetGunAzimuth(missionOutputs[4]);
        if(missionOutputs[5] > 0.5f)
        {
            Fire(missionOutputs[5]);
        }
    }
    public AiBoat GetClone() => new AiBoat(targetSelector.Mutated(), missionProcessor.Mutated());
}
