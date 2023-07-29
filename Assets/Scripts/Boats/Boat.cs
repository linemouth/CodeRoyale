using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Utils;
using Utils.Unity;
using Math = Utils.Math;
using Color = UnityEngine.Color;
using UnityEngine.Assertions;

public class Boat : EntityComponent
{
    // Boat
    public new Rigidbody rigidbody;
    public BoatController Controller
    {
        get => controller;
        set
        {
            if(value != null)
            {
                controller = value;
                controller.Boat = this;
                name = controller.Name;
                controller.Start();

                // Customize the boat.
                SetColor("Hull", Controller.HullColor);
                SetColor("Hull/Accommodation", Controller.WheelhouseColor);
                SetColor("Hull/Accommodation/Bridge", Controller.WheelhouseColor);
                SetColor("Hull/Accommodation/Bridge/Radar", Controller.WheelhouseColor);
                SetColor("Hull/Accommodation/Engine", Controller.EngineColor);
                SetColor("Hull/Gimbal", Controller.GunColor);
                SetColor("Hull/Gimbal/Turret", Controller.GunColor);
                SetColor("Hull/Gimbal/Turret/Barrel", Controller.GunColor);
            }
        }
    }
    public ResourceCache health = new ResourceCache(40, 40);
    public ResourceCache energy = new ResourceCache(20, 20);
    public static float energyRegenRate = 0.5f;
    public Transform turret;
    public Transform barrel;
    public Transform muzzle;
    public Transform radar;
    public event Action<Boat> Killed;

    // Hull
    public Vector2 Position => Geometry.ToPlanarPoint(transform.position, GameManager.basisVectors);
    public float Heading => transform.localRotation.eulerAngles.y;
    public Vector2 Forward => Geometry.ToPlanarPoint(transform.forward, GameManager.basisVectors).normalized;
    public Vector2 Velocity => Geometry.ToPlanarPoint(rigidbody.velocity, GameManager.basisVectors);

    // Gun
    public Vector2 GunPosition => Geometry.ToPlanarPoint(turret.position, GameManager.basisVectors);
    public float GunAzimuth => Mathf.Repeat(turret.localRotation.eulerAngles.y + 180, 360) - 180;
    public float GunHeading => BoatController.DirectionToHeading(new Vector2(turret.forward.x, turret.forward.z));
    public Vector2 GunForward => Geometry.ToPlanarPoint(turret.forward, GameManager.basisVectors).normalized;
    public float gunAzimuth = 0;

    // Radar
    public Vector2 RadarPosition => Geometry.ToPlanarPoint(radar.position, GameManager.basisVectors);
    public float RadarAzimuth => Mathf.Repeat(radar.localRotation.eulerAngles.y + 180, 360) - 180;
    public float RadarHeading => BoatController.DirectionToHeading(new Vector2(radar.forward.x, radar.forward.z));
    public Vector2 RadarForward => Geometry.ToPlanarPoint(radar.forward, GameManager.basisVectors).normalized;
    public float RadarRange => Math.Clamp(18000 / Mathf.Max(1, Mathf.Abs(radarAngularVelocity)), 25, 2500); // Radar range is [25m, 2.5km]
    public const float radarMaxAngularVelocity = 360;
    public const float radarAzimuthSmoothTime = 0.05f;

    // Internal
    private static GameObject ProjectilePrefab;
    private static GameObject ShotgunProjectilePrefab;
    private double nextUpdate1 = 0;
    private BoatController controller = null; // The custom class used to control the Boat's behavior.
    private float heading = 0;
    private float rudder = 0;
    private bool rudderSteering = true;
    private Vector2 thrust = Vector2.zero;
    private Vector2 lastPosition = Vector2.zero;
    private const float maxTorque = 50000; // Nm
    private const float maxForwardThrust = 40000; // N
    private const float maxReverseThrust = 20000; // N
    private const float maxLateralThrust = 10000; // N
    private float gunAzimuthVelocity = 0;
    private double nextFireTime = 0;
    private double nextShotgunFireTime = 0;
    private float radarAzimuth = 0;
    private bool radarRelative = false;
    private float radarAngularVelocity = 0;
    private bool spinRadarContinuously = true;

    public void SetHeading(float heading)
    {
        this.heading = heading;
        rudderSteering = false;
    }
    public void SetRudder(float rudder)
    {
        this.rudder = Mathf.Clamp(rudder, -1, 1);
        rudderSteering = true;
    }
    public void SetThrust(float forward, float right) => thrust = new Vector2(right, forward);
    public void SetGunAzimuth(float azimuth)
    {
        gunAzimuth = Mathf.Clamp(azimuth, -120, 120);
    }
    public bool Fire(float energy)
    {
        energy = Mathf.Clamp(energy, 0.1f, 5);
        if(Time.timeAsDouble >= nextFireTime && this.energy.Take(energy))
        {
            // Shoot a bullet
            GameObject go = Instantiate(ProjectilePrefab);
            Projectile projectile = go.GetComponent<Projectile>();
            projectile.Initialize(this, energy, muzzle.position, muzzle.forward, rigidbody.velocity);
            nextFireTime = Time.timeAsDouble + 0.5 * Mathf.Sqrt(energy);
            GameManager.RecordShot(this, energy);
            return true;
        }
        return false;
    }
    public bool FireShotgun(int fragmentCount)
    {
        fragmentCount = Mathf.Clamp(fragmentCount, 5, 15);
        float energy = fragmentCount;
        if(Time.timeAsDouble >= nextShotgunFireTime && this.energy.Take(energy))
        {
            nextShotgunFireTime = Time.timeAsDouble + fragmentCount;

            // Shoot fragments
            for(int i = 0; i < fragmentCount; i++)
            {
                GameObject go = Instantiate(ShotgunProjectilePrefab);
                Projectile projectile = go.GetComponent<Projectile>();
                Vector3 direction = muzzle.forward;
                Quaternion variation = Quaternion.Euler((float)Math.GaussianRandom(0, 2), (float)Math.GaussianRandom(0, 2), 0);
                direction = variation * direction;
                projectile.Initialize(this, 1, muzzle.position, direction, rigidbody.velocity);
                GameManager.RecordShot(this, energy);
            }
            return true;
        }
        return false;
    }
    public void SetRadarRotationSpeed(float rotationSpeed)
    {
        radarAzimuth = Math.Clamp(rotationSpeed, -radarMaxAngularVelocity, radarMaxAngularVelocity);
        spinRadarContinuously = true;
    }
    public void SetRadarAzimuth(float azimuth)
    {
        radarAzimuth = azimuth;
        radarRelative = true;
        spinRadarContinuously = false;
    }
    public void SetRadarHeading(float heading)
    {
        radarAzimuth = heading;
        radarRelative = false;
        spinRadarContinuously = false;
    }
    public void Damage(Boat source, float energy)
    {
        source.energy.Offer((float)health.Request(energy) * 1.5f);
        bool killed = health.IsEmpty;
        GameManager.RecordHitBoat(source, this, energy, killed);
        if(killed)
        {
            Controller?.OnKilled(source.controller?.Name);

            Killed?.Invoke(this);

            // Todo: Add destruction effects.
            Destroy(gameObject);
        }
    }
    public void SelfDestruct()
    {
        Destroy(gameObject);
    }

    protected override void Awake()
    {
        base.Awake();

        // Static initializations
        if(ProjectilePrefab == null)
        {
            ProjectilePrefab = Resources.Load<GameObject>("Prefabs/Projectile");
            ShotgunProjectilePrefab = Resources.Load<GameObject>("Prefabs/Shotgun Projectile");
        }

        // Instance initializations
        Transform hull = transform.Find("Hull");
        turret = transform.Find("Hull/Gimbal/Turret");
        barrel = transform.Find("Hull/Gimbal/Turret/Barrel");
        muzzle = transform.Find("Hull/Gimbal/Turret/Barrel/Muzzle");
        radar = transform.Find("Hull/Accommodation/Bridge/Radar");
        rigidbody = gameObject.GetOrAddComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationX;
        rigidbody.drag = 5;
        rigidbody.angularDrag = 5;
        rigidbody.mass = 1000;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }
    protected void Start()
    {
        //StatBlock.Canvas = ScreenUI.Canvas;
        lastPosition = Position;
        UI.Add(new StatBar(() => (float)energy.Fraction, new Color(0, 0.5f, 1), Color.black, new Vector2(64, 3), "Energy"));
        UI.Add(new StatBar(() => (float)health.Fraction, new Color(0, 1, 0), Color.black, new Vector2(64, 3), "Health"));
        UI.updatePeriod = 0.1f;

        // Customize the boat.
    }
    protected void FixedUpdate()
    {
        // Thrust
        float forwardThrust = thrust.y * (thrust.y >= 0 ? maxForwardThrust : maxReverseThrust);
        float lateralThrust = thrust.x * maxLateralThrust;
        rigidbody.AddRelativeForce(
            new Vector3(
                Mathf.Clamp(lateralThrust, -maxLateralThrust, maxLateralThrust),
                0,
                Mathf.Clamp(forwardThrust, -maxReverseThrust, maxForwardThrust)
            ),
            ForceMode.Force
        );

        // Steering
        if (!rudderSteering)
        {
            rudder = Mathf.Clamp(0.1f * (Mathf.Repeat(Mathf.DeltaAngle(Heading, heading) + 180, 360) - 180), -1, 1);
        }
        rigidbody.AddRelativeTorque(
            new Vector3(
                0,
                rudder * maxTorque,
                0
            )
        );

        // Distance Traveled
        GameManager.RecordDistanceTravelled(this, (Position - lastPosition).magnitude);
        lastPosition = Position;

        // Gun
        {
            float currentAzimuth = Mathf.Repeat(GunAzimuth + 180, 360) - 180;
            float targetAzimuth = Mathf.Repeat(gunAzimuth + 180, 360) - 180;
            float azimuth = Mathf.SmoothDamp(currentAzimuth, targetAzimuth, ref gunAzimuthVelocity, 0.1f, 120);
            turret.localEulerAngles = new Vector3(0, azimuth, 0);
            //float elevation = Mathf.SmoothDamp(GunElevation, gunElevation, ref gunElevationVelocity, 0.25f, 20);
        }
    }
    protected void Update()
    {
        // Radar.
        {
            float radarHeadingMin = RadarHeading;
            Vector3 radarRotation = radar.localEulerAngles;
            if(spinRadarContinuously)
            {
                float targetAngle = radarRotation.y + (radarAzimuth > 0 ? 179 : -179);
                radarRotation.y = Mathf.SmoothDampAngle(radarRotation.y, targetAngle, ref radarAngularVelocity, radarAzimuthSmoothTime, Mathf.Min(Math.Abs(radarMaxAngularVelocity), Mathf.Abs(radarAzimuth)), Time.deltaTime);
            }
            else
            {
                float azimuth = radarRelative ? radarAzimuth : radarAzimuth - Heading;
                radarRotation.y = Mathf.SmoothDampAngle(radarRotation.y, azimuth, ref radarAngularVelocity, radarAzimuthSmoothTime, radarMaxAngularVelocity, Time.deltaTime);
            }
            radar.localEulerAngles = radarRotation;
            float radarHeadingMax = RadarHeading;
            if(radarAngularVelocity < 0)
            {
                // Swap to maintain order.
                float temp = radarHeadingMin;
                radarHeadingMin = radarHeadingMax;
                radarHeadingMax = temp;
            }
            foreach(TargetInformation target in GameManager.GetRadarPings(this, radarHeadingMin, radarHeadingMax))
            {
                Controller?.DrawLine(target.Position, Color.red);
                Controller?.OnRadarHit(target);
            }
        }

        // Energy regeneration.
        energy.Offer(energyRegenRate * Time.deltaTime);

        // 1Hz Update.
        double time = Time.timeAsDouble;
        if(time >= nextUpdate1)
        {
            nextUpdate1 = (time - nextUpdate1 < 1.5 ? nextUpdate1 : time) + 1;
            Controller?.Update1();
        }
		
        // Frame Update.
        Controller?.Update();
    }

    private void SetColor(string name, Color color)
    {
        Renderer renderer = transform.Find(name).GetComponent<Renderer>();
        Material material = new Material(renderer.material);
        material.color = color;
        renderer.material = material;
    }
}
