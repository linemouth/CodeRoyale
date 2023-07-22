using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Utils.Unity;

public abstract class BoatController
{
    // Boat
    /// <summary>The name of the Boat.</summary>
    public string Name => GetType().Name;
    /// <summary>The Faction to which the boat belongs.</summary>
    public Faction Faction => Boat.Faction;
    /// <summary>The reference to the Boat MonoBehaviour: unavailable to subclasses of BostController.</summary>
    public Boat Boat { private get; set; }
    /// <summary>The current health of the Boat.</summary>
    public float Health => (float)Boat.health.Stored;
    /// <summary>The current fractional health of the Boat from [0, 1].</summary>
    public float HealthFraction => (float)Boat.health.Fraction;
    /// <summary>The current energy of the Boat.</summary>
    public float Energy => (float)Boat.energy.Stored;
    /// <summary>The current fractional energy of the Boat from [0, 1].</summary>
    public float EnergyFraction => (float)Boat.energy.Fraction;

    // Hull
    /// <summary>The position of the Boat in world coordinates.</summary>
    public Vector2 Position => Boat.Position;
    /// <summary>The heading in degrees at which the Boat is facing.</summary>
    public float Heading => Boat.Heading;
    /// <summary>A normalized Vector2 in the same direction as the Boat is facing in world coordinates.</summary>
    public Vector2 Forward => Boat.Forward;
    /// <summary>The velocity of the Boat in world coordinates.</summary>
    public Vector2 Velocity => Boat.Velocity;

    // Gun
    /// <summary>The position of the gun in world coordinates.</summary>
    public Vector2 GunPosition => Boat.GunPosition;
    /// <summary>The azimuth in degrees relative to the Boat at which the gun is aimed.</summary>
    public float GunAzimuth => Boat.GunAzimuth;
    /// <summary>The heading in degrees at which the gun is aimed.</summary>
    public float GunHeading => Boat.GunHeading;
    /// <summary>A normalized Vector2 in the same direction as the gun is aimed in world coordinates.</summary>
    public Vector2 GunForward => Boat.GunForward;
    //public float GunElevation => Boat.GunElevation;

    // Radar
    /// <summary>The position of the radar in world coordinates.</summary>
    public Vector2 RadarPosition => Boat.RadarPosition;
    /// <summary>The azimuth in degrees relative to the Boat at which the radar is pointing.</summary>
    public float RadarAzimuth => Boat.RadarAzimuth;
    /// <summary>The heading in degrees at which the radar is pointing.</summary>
    public float RadarHeading => Boat.RadarHeading;
    /// <summary>A normalized Vector2 in the same direction as the radar is pointing in world coordinates.</summary>
    public Vector2 RadarForward => Boat.RadarForward;
    /// <summary>The current maximum range of the radar. This decreases as the radar rotation speed increases.</summary>
    public float RadarRange => Boat.RadarRange;
    /// <summary>A collection in which to store targets from OnRadarHit(). Note: This is unmanaged! YOU must write the code to use it.</summary>
    public readonly HashSet<TargetInformation> targets = new HashSet<TargetInformation>();

    // Helper functions: These can be helpful for doing harder math.
    /// <summary>Converts a Vector2 direction to a heading in degrees.</summary>
    public static float DirectionToHeading(Vector2 direction) => Mathf.Repeat(Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg, 360);
    /// <summary>Converts a heading in degrees to a normalized Vector2 direction.</summary>
    public static Vector2 HeadingToDirection(float heading)
    {
        float radians = heading * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
    }
    /// <summary>Converts a Vector2 direction to an azimuth in degrees relative to the Boat.</summary>
    public float DirectionToAzimuth(Vector2 direction) => Mathf.Repeat(DirectionToHeading(direction) - Heading + 180, 360) - 180;
    /// <summary>Converts an azimuth in degrees relative to the Boat to a normalized Vector2 direction.</summary>
    public Vector2 AzimuthToDirection(float azimuth) => HeadingToDirection(azimuth + Heading);
    /// <summary>Converts a position relative to the Boat to position in the world.</summary>
    public Vector2 WorldToLocalPosition(Vector2 worldPosition)
    {
        Vector3 worldPosition3D = new Vector3(worldPosition.x, 0, worldPosition.y);
        Vector3 localPosition3D = Boat.transform.InverseTransformPoint(worldPosition3D);
        return new Vector2(localPosition3D.x, localPosition3D.z);
    }
    /// <summary>Converts a position in the world to a position relative to the Boat.</summary>
    public Vector2 LocalToWorldPosition(Vector2 localPosition)
    {
        Vector3 localPosition3D = new Vector3(localPosition.x, 0, localPosition.y);
        Vector3 worldPosition3D = Boat.transform.TransformPoint(localPosition3D);
        return new Vector2(worldPosition3D.x, worldPosition3D.z);
    }

    // Event callbacks: These functions can be overridden in your BoatController subclass.
    /// <summary>Called when the radar detects something.</summary>
    public virtual void OnRadarHit(TargetInformation target) { }
    /// <summary>Called when the Boat spawns.</summary>
    public virtual void Start() { }
    /// <summary>Called every frame.</summary>
    public virtual void Update() { }
    /// <summary>Called every second.</summary>
    public virtual void Update1() { }
    /// <summary>Called every Physics loop update.</summary>
    public virtual void FixedUpdate() { }
    /// <summary>Called when the boat is destroyed.</summary>
    public virtual void OnDestroy() { }

    // Control functions: Use these to control your Boat.
    /// <summary>Adds something to the Boat's stat block.</summary>
    public void AddStat(IStat stat) => Boat.StatBlock.Add(stat);
    /// <summary>Sets the heading to which the Boat will turn.</summary>
    public void SetHeading(float heading) => Boat.SetHeading(heading);
    /// <summary>Sets the rate at which the Boat will turn.</summary>
    public void SetRudder(float position) => Boat.SetRudder(position);
    /// <summary>Sets the forward thrust to a fractional amount from -1 to 1.</summary>
    public void SetThrust(float amount) => SetThrust(amount, 0);
    /// <summary>Sets the forward and lateral thrust to a fractional amount from -1 to 1.</summary>
    public void SetThrust(float forward, float right) => Boat.SetThrust(forward, right);
    /// <summary>Sets the direction the gun will aim, relative to the Boat.</summary>
    public void SetGunAzimuth(float azimuth) => Boat.SetGunAzimuth(azimuth);
    //protected void SetGunElevation(float angle) => Boat.SetGunElevation(angle);
    /// <summary>Attempts to fire a bullet. The impact damage and cooldown are both proportional to the energy used.</summary>
    public bool Fire(float energy) => Boat.Fire(energy);
    /// <summary>Attempts to fire a spread of bullets. The impact damage and cooldown are both proportional to the number of fragments requested.</summary>
    public bool FireShotgun(int fragmentCount) => Boat.FireShotgun(fragmentCount);
    /// <summary>Sets the rate at which the radar will turn.</summary>
    public void SetRadarRotationSpeed(float rpm) => Boat.SetRadarRotationSpeed(rpm);
    /// <summary>Sets the direction the radar will aim, relative to the Boat.</summary>
    public void SetRadarAzimuth(float azimuth) => Boat.SetRadarAzimuth(azimuth);
    /// <summary>Sets the direction the radar will aim, relative to the world.</summary>
    public void SetRadarHeading(float heading) => Boat.SetRadarHeading(heading);
    public void SelfDestruct() => Boat.SelfDestruct();

    // Debug Tools: You can use these to draw what your boat is doing.
#if DEBUG
    /// <summary>Draws an "X" at the given world position.</summary>
    public static void DrawX(Vector2 position, Color color)
    {
        Vector3 p = Geometry.FromPlanarPoint(position, GameManager.basisVectors);
        Debug.DrawLine(p + new Vector3(-5, 0, -5), p + new Vector3(5, 0, 5), color);
        Debug.DrawLine(p + new Vector3(-5, 0, 5), p + new Vector3(5, 0, -5), color);
    }
    /// <summary>Draws a line from the Boat to a position in the world.</summary>
    public void DrawLine(Vector2 end, Color color) => DrawLine(Position, end, color);
    /// <summary>Draws a line from one world position to another.</summary>
    public static void DrawLine(Vector2 start, Vector2 end, Color color)
    {
        Vector3 a = Geometry.FromPlanarPoint(start, GameManager.basisVectors);
        Vector3 b = Geometry.FromPlanarPoint(end, GameManager.basisVectors);
        Debug.DrawLine(a, b, color);
    }
#endif
}
