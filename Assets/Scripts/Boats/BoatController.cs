using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BoatController
{
    // Boat
    public string Name => GetType().Name;
    public Faction Faction => Boat.Faction;
    public Boat Boat { private get; set; }
    public StatBlock StatBlock => Boat.StatBlock;
    public float Health => (float)Boat.health.Stored;
    public float HealthFraction => (float)Boat.health.Fraction;
    public float Energy => (float)Boat.energy.Stored;
    public float EnergyFraction => (float)Boat.energy.Fraction;

    // Hull
    public Vector2 Position => Boat.Position;
    public float Heading => Boat.Heading;
    public Vector2 Forward => Boat.Forward;
    public Vector2 Velocity => Boat.Velocity;

    // Gun
    public Vector2 GunPosition => Boat.GunPosition;
    public float GunAzimuth => Boat.GunAzimuth;
    public float GunHeading => Boat.GunHeading;
    public Vector2 GunForward => Boat.GunForward;
    //public float GunElevation => Boat.GunElevation;

    // Radar
    public Vector2 RadarPosition => Boat.RadarPosition;
    public float RadarAzimuth => Boat.RadarAzimuth;
    public float RadarHeading => Boat.RadarHeading;
    public Vector2 RadarForward => Boat.RadarForward;
    public float RadarRange => Boat.RadarRange;

    public static float DirectionToHeading(Vector2 direction) => Mathf.Repeat(Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg, 360);
    public static Vector2 HeadingToDirection(float heading)
    {
        float radians = heading * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
    }
    public Vector2 WorldToLocalPosition(Vector2 relativePosition)
    {
        Vector3 relativePosition3D = new Vector3(relativePosition.x, 0, relativePosition.y);
        Vector3 localPosition3D = Boat.transform.worldToLocalMatrix * relativePosition;
        return new Vector2(localPosition3D.x, localPosition3D.z);
    }
    public float DirectionToAzimuth(Vector2 direction) => Mathf.Repeat(Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg - Heading + 180, 360) - 180;
    public virtual void OnRadarHit(TargetInformation target) { }
    public virtual void Start() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }

    protected void SetHeading(float heading) => Boat.SetHeading(heading);
    protected void SetRudder(float position) => Boat.SetRudder(position);
    /// <summary>Sets the forward thrust to a fractional amount from -1 to 1.</summary>
    protected void SetThrust(float amount) => SetThrust(amount, 0);
    /// <summary>Sets the forward and lateral thrust to a fractional amount from -1 to 1.</summary>
    protected void SetThrust(float forward, float right) => Boat.SetThrust(forward, right);
    protected void SetGunAzimuth(float azimuth) => Boat.SetGunAzimuth(azimuth);
    //protected void SetGunElevation(float angle) => Boat.SetGunElevation(angle);
    protected bool Fire(float velocity) => Boat.Fire(velocity);
    protected void SetRadarRotationSpeed(float rpm) => Boat.SetRadarRotationSpeed(rpm);
    protected void SetRadarAzimuth(float azimuth) => Boat.SetRadarAzimuth(azimuth);
    protected void SetRadarHeading(float heading) => Boat.SetRadarHeading(heading);

    // Debug Tools
    protected void DrawX(Vector2 position, Color color, bool localCoordinates = false)
    {
        Vector3 p = localCoordinates ? Vector3.zero : Geometry.FromPlanarPoint(position, GameManager.basisVectors);
        Debug.DrawLine(p + new Vector3(-5, 0, -5), p + new Vector3(5, 0, 5), Color.red);
        Debug.DrawLine(p + new Vector3(-5, 0, 5), p + new Vector3(5, 0, -5), Color.red);
    }
}
