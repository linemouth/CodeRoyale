using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Utils;
using Utils.Unity;
using Math = Utils.Math;
using Color = UnityEngine.Color;

public class GameManager : EntityComponent
{
    public class BoatControllerStats
    {
        public string Name => Type.Name;
        public readonly Type Type;
        public StatGroup statGroup;
        public int totalKills;
        public int totalDeaths;
        public int totalPowerupsKilled;
        public int totalPowerupsCollected;
        public int totalShots;
        public float totalShotEnergy;
        public float totalDamageSustained;
        public float totalDamageDealtToBoats;
        public float totalDamageDealtToPowerups;
        public float totalDistanceTravelled;
        public float AverageKills => totalKills / (totalDeaths + 1);
        public float AveragePowerupsKilled => totalPowerupsKilled / (totalDeaths + 1);
        public float AveragePowerupsCollected => totalPowerupsCollected / (totalDeaths + 1);
        public float AverageShotEnergy => Math.SafeDivide(totalShotEnergy, totalShots, totalShotEnergy);
        public float AverageShotsPerKill => Math.SafeDivide(totalShots, totalKills, totalShots);
        public float AverageDamageSustained => totalDamageSustained / (totalDeaths + 1);
        public float AverageDamageDealtToBoats => totalDamageDealtToBoats / (totalDeaths + 1);
        public float AverageDamageDealthToPowerups => totalDamageDealtToPowerups / (totalDeaths + 1);
        public float AverageDistanceTravelled => totalDistanceTravelled / (totalDeaths + 1);

        public BoatControllerStats(Type type) => Type = type;
    }
    public static GameObject BoatPrefab;
    public static GameObject PowerupPrefab;
    public static readonly Dictionary<Type, BoatControllerStats> BoatControllers = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsSubclassOf(typeof(BoatController))).ToDictionary(type => type, type => new BoatControllerStats(type));
    public static readonly BasisVectors basisVectors = new BasisVectors(Vector3.up, Vector3.right, Vector3.forward);

    private static List<Boat> Boats = new List<Boat>();
    private static readonly HashSet<Bounds> GridCells = GetGridCells();
    private static readonly HashSet<Powerup> powerups = new HashSet<Powerup>();
    private const float gridSize = 30;
    private ActionCamera actionCamera = null;

    public static IEnumerable<TargetInformation> GetRadarPings(Boat boat, float headingMin, float headingMax)
    {
        headingMin -= 3;
        headingMax += 3;
        Debug.DrawLine(boat.radar.position, boat.radar.position + Geometry.FromPlanarPoint(BoatController.HeadingToDirection(headingMin) * boat.RadarRange, basisVectors), new Color(0, 1, 0, 0.2f));
        Debug.DrawLine(boat.radar.position, boat.radar.position + Geometry.FromPlanarPoint(BoatController.HeadingToDirection(headingMax) * boat.RadarRange, basisVectors), new Color(0, 1, 0, 0.2f));
        HashSet <TargetInformation> hits = new HashSet<TargetInformation>();
        float sqrRange = boat.RadarRange * boat.RadarRange;
        Vector2 radarPosition = boat.RadarPosition;

        // Check for boats.
        foreach(Boat otherBoat in Boats)
        {
            if(otherBoat != boat)
            {
                Vector2 relativePosition = otherBoat.Position - radarPosition;
                if(relativePosition.sqrMagnitude <= sqrRange)
                {
                    float angle = BoatController.DirectionToHeading(relativePosition);
                    if((headingMax > headingMin) ? (angle <= headingMax && angle >= headingMin) : (angle <= headingMax || angle >= headingMin))
                    {

                        hits.Add(new TargetInformation(otherBoat));
                        Debug.DrawLine(boat.transform.position, otherBoat.transform.position, new Color(1, 0, 0, 0.2f));
                    }
                }
            }
        }

        // Check for powerups.
        foreach(Powerup powerup in powerups)
        {
            Vector2 relativePosition = powerup.Position - radarPosition;
            if(relativePosition.sqrMagnitude <= sqrRange)
            {
                float angle = BoatController.DirectionToHeading(relativePosition);
                if((headingMax > headingMin) ? (angle <= headingMax && angle >= headingMin) : (angle <= headingMax || angle >= headingMin))
                {

                    hits.Add(new TargetInformation(powerup));
                    Debug.DrawLine(boat.transform.position, powerup.transform.position, new Color(1, 1, 0, 0.2f));
                }
            }
        }

        // Check for obstacles.
        /*foreach(Boat otherBoat in Boats)
        {
            if(otherBoat != boat)
            {
                Vector2 relativePosition = otherBoat.Position - radarPosition;
                if(relativePosition.sqrMagnitude <= sqrRange)
                {
                    float angle = BoatController.DirectionToHeading(relativePosition);
                    if((headingMax > headingMin) ? (angle <= headingMax && angle >= headingMin) : (angle <= headingMax || angle >= headingMin))
                    {

                        hits.Add(new TargetInformation(otherBoat));
                        Debug.DrawLine(boat.transform.position, otherBoat.transform.position, Color.red);
                    }
                }
            }
        }*/

        return hits;
    }
    public static void RecordDistanceTravelled(Boat boat, float distance)
    {
        if(boat?.Controller != null && distance > 0)
        {
            BoatControllerStats stats = BoatControllers[boat.Controller.GetType()];
            stats.totalDistanceTravelled += distance;
        }
    }
    public static void RecordShot(Boat boat, float energy)
    {
        if(boat?.Controller != null)
        {
            BoatControllerStats stats = BoatControllers[boat.Controller.GetType()];
            stats.totalShots++;
            stats.totalShotEnergy += energy;
        }
    }
    public static void RecordHitBoat(Boat boat, Boat target, float energy, bool killed)
    {
        if(boat?.Controller != null && target != null)
        {
            BoatControllerStats stats = BoatControllers[boat.Controller.GetType()];
            stats.totalDamageDealtToBoats += energy;
            if(killed)
            {
                stats.totalKills++;
            }

            if(target.Controller != null)
            {
                BoatControllerStats targetStats = BoatControllers[target.Controller.GetType()];
                targetStats.totalDamageSustained += energy;
                if(killed)
                {
                    targetStats.totalDeaths++;
                }
            }
        }
    }
    public static void RecordHitPowerup(Boat boat, Powerup powerup, float energy, bool collected)
    {
        if(boat?.Controller != null && powerup != null)
        {
            BoatControllerStats stats = BoatControllers[boat.Controller.GetType()];
            stats.totalDamageDealtToPowerups += energy;
            if(collected)
            {
                stats.totalPowerupsKilled++;
            }
        }
    }
    public static void RecordCollectedPowerup(Boat boat, Powerup powerup)
    {
        if(boat?.Controller != null && powerup != null)
        {
            BoatControllerStats stats = BoatControllers[boat.Controller.GetType()];
            stats.totalPowerupsCollected++;
        }
    }

    private static HashSet<Bounds> GetGridCells()
    {
        HashSet<Bounds> cells = new HashSet<Bounds>();
        for(float x = gridSize * 0.5f; x <= 100; x += gridSize)
        {
            for(float y = gridSize * 0.5f; y <= 100; y += gridSize)
            {
                cells.Add(new Bounds(new Vector3( x, 0,  y), new Vector3(gridSize, gridSize, gridSize)));
                cells.Add(new Bounds(new Vector3( x, 0, -y), new Vector3(gridSize, gridSize, gridSize)));
                cells.Add(new Bounds(new Vector3(-x, 0,  y), new Vector3(gridSize, gridSize, gridSize)));
                cells.Add(new Bounds(new Vector3(-x, 0, -y), new Vector3(gridSize, gridSize, gridSize)));
            }
        }
        return cells;
    }
    private static Bounds GetEmptyCell()
    {
        IEnumerable<Vector3> positions = Boats.Select(boat => boat.transform.position);
        positions.Concat(powerups.Select(powerup => powerup.transform.position));

        foreach(Bounds cell in GridCells.OrderBy(p => Math.Random(0f, 1f)))
        {
            if(
                !positions.Any(point => cell.Contains(point)) &&
                !Physics.CheckBox(cell.center, cell.extents)
            )
            {
                return cell;
            }
        }
        return default;
    }
    private static Vector3 GetSpawnPosition(float spawnRange)
    {
        Bounds cell = GetEmptyCell();
        Vector3 position = cell.center;
        position.x += Math.Random(-spawnRange, spawnRange);
        position.z += Math.Random(-spawnRange, spawnRange);
        return position;
    }
    private Boat AddBoat(Type type)
    {
        Vector3 position = GetSpawnPosition(8);
        GameObject boatObject = Instantiate(BoatPrefab, position, Quaternion.Euler(0, Math.Random(0, 360), 0));
        Boat boat = boatObject.GetOrAddComponent<Boat>();
        if(boat == null)
        {
            boat = boatObject.AddComponent<Boat>();
        }
        boat.Controller = (BoatController)Activator.CreateInstance(type);
        boat.Destroyed += BoatDestroyed;
        Boats.Add(boat);

        // If the camera doesn't have a subject, make this boat the subject.
        if(actionCamera.Subject == null)
        {
            actionCamera.Subject = boat;
        }

        return boat;
    }
    private void BoatDestroyed(Boat boat)
    {
        Boats.Remove(boat);

        // Update the camera subject
        if(actionCamera.Subject == boat)
        {
            actionCamera.Subject = null;
        }
    }
    private Powerup AddPowerup()
    {
        string type = Powerup.Types.Random();
        Vector3 position = GetSpawnPosition(12);
        return AddPowerup(type, position);
    }
    private Powerup AddPowerup(string type, Vector3 position)
    {
        GameObject go = Instantiate(PowerupPrefab, position, Quaternion.Euler(0, Math.Random(0f, 360f), 0));
        Powerup powerup = go.GetComponent<Powerup>();
        powerup.Type = type;
        powerups.Add(powerup);
        powerup.Collected += PowerupDestroyed;
        return powerup;
    }
    private void PowerupDestroyed(Powerup powerup)
    {
        powerups.Remove(powerup);
    }
    protected override void Awake()
    {
        base.Awake();

        BoatPrefab = Resources.Load<GameObject>("Prefabs/Gunboat");
        PowerupPrefab = Resources.Load<GameObject>("Prefabs/Powerup Crate");
        actionCamera = Camera.main.GetComponent<ActionCamera>();

        /*foreach((Type type, BoatControllerStats stats) in BoatControllers)
        {
            AddBoat(type);
            StatGroup statGroup = stats.statGroup = new StatGroup(stats.Name);
            statGroup.Add(new StatDivider());
            statGroup.Add(new StatLabel(() => stats.Name, "Name"));
            statGroup.Add(new StatLabel(() => $"Kills: {stats.totalKills:0}, Deaths: {stats.totalDeaths:0}", "Kills/Deaths"));
            statGroup.Add(new StatLabel(() => $"Damage dealt: {stats.totalDamageDealtToBoats:0}, taken: {stats.totalDamageSustained:0}", "Damage"));
            statGroup.Add(new StatLabel(() => $"Shots/kill: {stats.AverageShotsPerKill:0.0}, powerups: {stats.AveragePowerupsCollected:0.0}", "Effectiveness"));
            StatBlock.Add(statGroup);
        }*/
    }
    protected virtual void Start()
    {
        //StatBlock.Canvas = ScreenUI.Canvas;
        //StatBlock.transform.parent = ScreenUI.LeftSidebar.transform;
    }
    protected virtual void Update()
    {
        if(powerups.Count < 20)
        {
            AddPowerup();
        }

        foreach((Type type, BoatControllerStats stat) in BoatControllers)
        {
            int count = Boats.Count(boat => boat.Controller.GetType() == type);
            if(count < 1)
            {
                AddBoat(type);
            }
        }

        // When the user presses the spacebar, switch the camera subject.
        if(Input.GetKeyDown(KeyCode.Space)) {
            int index = (Boats.IndexOf(actionCamera.Subject) + 1) % Boats.Count;
            actionCamera.Subject = Boats[index];
        }
    }
}
