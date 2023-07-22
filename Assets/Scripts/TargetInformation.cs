using System;
using UnityEngine;
using Utils.Unity;

public class TargetInformation : IEquatable<TargetInformation>
{
    /// <summary>Returns the number of seconds since the target was last observed.</summary>
    public float Age => (float)(Time.timeAsDouble - seenAt);
    /// <summary>Returns the estimated position of the target by extrapolating its last known position and velocity.</summary>
    public Vector2 EstimatedPosition => Position + Velocity * Age;
    public readonly string Type;
    public readonly int Id;
    public string Name { get; private set; }
    public Faction Faction { get; private set; }
    public Vector2 Position { get; private set; }
    public Vector2 Velocity { get; private set; }
    public bool IsValid { get; private set; } = true;

    private double seenAt;

    public TargetInformation(Boat boat) : this(boat.name, "Boat", boat.Faction, boat.Position, boat.Velocity, boat.GetHashCode()) { }
    public TargetInformation(Powerup powerup) : this(powerup.name, "Powerup", null, Geometry.ToPlanarPoint(powerup.transform.position, GameManager.basisVectors), Vector2.zero, powerup.GetHashCode()) { }
    public TargetInformation(string name, string type, Faction faction, Vector2 position, Vector2 velocity, int id)
    {
        Name = name;
        Type = type;
        Faction = faction;
        Position = position;
        Velocity = velocity;
        Id = id;
        seenAt = Time.timeAsDouble;
    }
    public void Update(TargetInformation other)
    {
        // If the other target doesn't match this target, return immediately.
        if(other != this)
        {
            return;
        }

        // Update dynamic information about the target.
        seenAt = other.seenAt;
        Name = other.Name;
        Faction = other.Faction;
        Position = other.Position;
        Velocity = other.Velocity;
    }
    public void Invalidate() { IsValid = false; }
    public override bool Equals(object obj) => obj is TargetInformation other && other.Id == Id;
    public bool Equals(TargetInformation other) => other.Id == Id;
    public override int GetHashCode() => Id;
    public override string ToString() => $"TargetInformation: {Name}, {Age:0.0}";
    public static bool operator ==(TargetInformation a, TargetInformation b) => a.Equals(b);
    public static bool operator !=(TargetInformation a, TargetInformation b) => !a.Equals(b);
}
