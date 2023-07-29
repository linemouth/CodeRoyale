using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Utils;
using Utils.Unity;
using Math = Utils.Math;
using Color = UnityEngine.Color;
using UnityEngine.Assertions;
using Unity.VisualScripting;

public class GameManager : EntityComponent
{
    [Serializable]
    public class BoatControllerInfo
    {
        public string Name => Type.Name;
        public readonly Type Type;
        public StatGroup statGroup;
        public bool allowSpawn;
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
        public float AverageKills => (float)totalKills / (totalDeaths + 1);
        public float AveragePowerupsKilled => totalPowerupsKilled / (totalDeaths + 1);
        public float AveragePowerupsCollected => totalPowerupsCollected / (totalDeaths + 1);
        public float AverageShotEnergy => Math.SafeDivide(totalShotEnergy, totalShots, totalShotEnergy);
        public float AverageShotsPerKill => Math.SafeDivide(totalShots, totalKills, totalShots);
        public float AverageDamageSustained => totalDamageSustained / (totalDeaths + 1);
        public float AverageDamageDealtToBoats => totalDamageDealtToBoats / (totalDeaths + 1);
        public float AverageDamageDealthToPowerups => totalDamageDealtToPowerups / (totalDeaths + 1);
        public float AverageDistanceTravelled => totalDistanceTravelled / (totalDeaths + 1);

        public BoatControllerInfo(Type type) => Type = type;
        public BoatController ConstructInstance() => (BoatController)Activator.CreateInstance(Type);
    }
    public static GameObject BoatPrefab;
    public static GameObject PowerupPrefab;
    public static IEnumerable<BoatControllerInfo> SpawnableBoatControllers => boatControllers.Where(info => true || info.allowSpawn);
    public static readonly BoatControllerInfo[] boatControllers = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsSubclassOf(typeof(BoatController))).Select(type => new BoatControllerInfo(type)).ToArray();
    public static readonly BasisVectors basisVectors = new BasisVectors(Vector3.up, Vector3.right, Vector3.forward);

    private static List<Boat> Boats = new List<Boat>();
    private static readonly HashSet<Bounds> GridCells = GetGridCells();
    private static readonly HashSet<Powerup> powerups = new HashSet<Powerup>();
    private const float gridSize = 30;
    private ActionCamera actionCamera = null;
    private InputManager inputManager = null;

    public static IEnumerable<TargetInformation> GetRadarPings(Boat boat, float headingMin, float headingMax)
    {
        headingMin = Math.Repeat(headingMin - 3, 360);
        headingMax = Math.Repeat(headingMax + 3, 360);
        Debug.DrawLine(boat.radar.position, boat.radar.position + Geometry.FromPlanarPoint(BoatController.HeadingToDirection(headingMin) * boat.RadarRange, basisVectors), new Color(0, 1, 0, 0.2f));
        Debug.DrawLine(boat.radar.position, boat.radar.position + Geometry.FromPlanarPoint(BoatController.HeadingToDirection(headingMax) * boat.RadarRange, basisVectors), new Color(0, 1, 0, 0.2f));
        HashSet<TargetInformation> hits = new HashSet<TargetInformation>();
        float sqrRange = boat.RadarRange * boat.RadarRange;
        Vector2 radarPosition = boat.RadarPosition;

        // Check for boats.
        foreach(Boat otherBoat in Boats)
        {
            if(otherBoat != boat)
            {
                if(IsInRadarSlice(otherBoat.Position - radarPosition, headingMin, headingMax, sqrRange)) {
                    hits.Add(new TargetInformation(otherBoat));
                }
            }
        }

        // Check for powerups.
        foreach(Powerup powerup in powerups)
        {
            if(IsInRadarSlice(powerup.Position - radarPosition, headingMin, headingMax, sqrRange))
            {
                hits.Add(new TargetInformation(powerup));
            }
        }

        return hits;
    }
    public static void RecordDistanceTravelled(Boat boat, float distance)
    {
        if(boat?.Controller != null && distance > 0)
        {
            BoatControllerInfo info = GetInfo(boat);
            info.totalDistanceTravelled += distance;
        }
    }
    public static void RecordShot(Boat boat, float energy)
    {
        if(boat?.Controller != null)
        {
            BoatControllerInfo info = GetInfo(boat);
            info.totalShots++;
            info.totalShotEnergy += energy;
        }
    }
    public static void RecordHitBoat(Boat boat, Boat target, float energy, bool killed)
    {
        if(boat?.Controller != null && target != null)
        {
            BoatControllerInfo info = GetInfo(boat);
            info.totalDamageDealtToBoats += energy;
            if(killed)
            {
                info.totalKills++;
            }

            if(target.Controller != null)
            {
                BoatControllerInfo victim = GetInfo(target);
                victim.totalDamageSustained += energy;
                if(killed)
                {
                    victim.totalDeaths++;
                }
            }
        }
    }
    public static void RecordHitPowerup(Boat boat, Powerup powerup, float energy, bool collected)
    {
        if(boat?.Controller != null && powerup != null)
        {
            BoatControllerInfo info = GetInfo(boat);
            info.totalDamageDealtToPowerups += energy;
            if(collected)
            {
                info.totalPowerupsKilled++;
            }
        }
    }
    public static void RecordCollectedPowerup(Boat boat, Powerup powerup)
    {
        if(boat?.Controller != null && powerup != null)
        {
            BoatControllerInfo info = GetInfo(boat);
            info.totalPowerupsCollected++;
        }
    }
    public static BoatControllerInfo GetInfo(Boat boat) => GetInfo(boat.Controller);
    public static BoatControllerInfo GetInfo(BoatController controller) => boatControllers.First(info => info.Type == controller.GetType());

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
    private static bool IsInRadarSlice(Vector2 relativePosition, float minHeading, float maxHeading, float sqrRange)
    {
        float heading = BoatController.DirectionToHeading(relativePosition);
        Assert.IsTrue(heading >= 0 && heading <= 360, $"Heading of {heading} is outside of [0, 360] range.");
        if(maxHeading >= minHeading)
        {
            return heading <= maxHeading && heading >= minHeading && relativePosition.sqrMagnitude <= sqrRange;
        }
        else
        {
            return (heading <= maxHeading || heading >= minHeading) && relativePosition.sqrMagnitude <= sqrRange;
        }
    }
    private Boat AddBoat(BoatController controller)
    {
        Vector3 position = GetSpawnPosition(8);
        GameObject boatObject = Instantiate(BoatPrefab, position, Quaternion.Euler(0, Math.Random(0, 360), 0));
        Boat boat = boatObject.GetOrAddComponent<Boat>();
        if(boat == null)
        {
            boat = boatObject.AddComponent<Boat>();
        }
        boat.Controller = controller;
        if(boat.Controller is PlayerBoat playerBoat)
        {
            playerBoat.controller = inputManager.GetController();
            MeshRenderer renderer = boat.GetComponentInChildren<MeshRenderer>();
            renderer.sharedMaterial = new Material(renderer.material);
            renderer.sharedMaterial.color = playerBoat.controller.color;
        }
        boat.Killed += BoatKilled;
        Boats.Add(boat);

        // If the camera doesn't have a subject, make this boat the subject.
        if(actionCamera.Subject == null)
        {
            actionCamera.Subject = boat;
        }

        return boat;
    }
    private void BoatKilled(Boat boat)
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
		inputManager = gameObject.GetOrAddComponent<InputManager>();

        foreach(BoatControllerInfo info in SpawnableBoatControllers)
        {
            AddBoat(info.ConstructInstance());
            StatGroup statGroup = info.statGroup = new StatGroup(info.Name);
            statGroup.Add(new StatDivider());
            statGroup.Add(new StatLabel(() => info.Name, "Name"));
            statGroup.Add(new StatLabel(() => $"Kills: {info.totalKills:0}, Deaths: {info.totalDeaths:0} ({info.AverageKills:F2}:1)", "Kills/Deaths"));
            statGroup.Add(new StatLabel(() => $"Damage dealt: {info.totalDamageDealtToBoats:0} ({info.AverageDamageDealtToBoats:F1}), taken: {info.totalDamageSustained:0} ({info.AverageDamageSustained:F1})", "Damage"));
            ScreenUI.LeftSidebar.Add(statGroup);
        }
    }
    protected virtual void Update()
    {
        if(powerups.Count < 20)
        {
            AddPowerup();
        }

        foreach(BoatControllerInfo info in SpawnableBoatControllers)
        {
            int count = Boats.Count(boat => boat.Controller.GetType() == info.Type);
            if(count < 1)
            {
                BoatController controller = info.ConstructInstance();
                AddBoat(controller);
            }
        }

        // When the user presses the spacebar, switch the camera subject.
        if(Input.GetKeyDown(KeyCode.Space)) {
            int index = (Boats.IndexOf(actionCamera.Subject) + 1) % Boats.Count;
            actionCamera.Subject = Boats[index];
        }
    }
}
