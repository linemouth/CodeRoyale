using System;
using UnityEngine;

public struct TargetInformation : IEquatable<TargetInformation>
{
    /// <summary>Returns the number of seconds since the target was last observed.</summary>
    public float Age => (float)(Time.timeAsDouble - seenAt);
    /// <summary>Returns the estimated position of the target by extrapolating its last known position and velocity.</summary>
    public Vector2 EstimatedPosition => Position + Velocity * Age;
    public readonly string Name;
    public readonly string Type;
    public readonly Faction Faction;
    public readonly int Id;
    public readonly Vector2 Position;
    public readonly Vector2 Velocity;
    public bool IsValid => Id != default;

    private readonly double seenAt;

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
    public override bool Equals(object obj) => obj is TargetInformation other && other.Id == Id;
    public bool Equals(TargetInformation other) => other.Id == Id;
    public override int GetHashCode() => Id;
    public override string ToString() => $"TargetInformation: {Name}, {Age:0.0}";
}
