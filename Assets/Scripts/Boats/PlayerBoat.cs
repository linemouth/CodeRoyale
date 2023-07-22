using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utils;

public class PlayerBoat : BoatController
{
    public VirtualController controller;
    public UnityEngine.Color Color => controller.color;

    protected float gunTargetAzimuth = 0;
    
    public override void Update()
    {
        // Verify that we have a controller attached.
        if(controller == null)
        {
            return;
        }

        // Steer the boat.
        if (Math.Abs(controller.leftAxis.x) > 0.5)
        {
            SetRudder(controller.leftAxis.x * 4);
        }
        else
        {
            SetRudder(0);
        }
        SetThrust(controller.leftAxis.y * 4);

        // Aim the gun.
        if (Math.Abs(controller.rightAxis.x) > 0.5)
        {
            gunTargetAzimuth = Math.Clamp(gunTargetAzimuth + controller.rightAxis.x * 90 * Time.deltaTime, -140, 140);
            SetGunAzimuth(gunTargetAzimuth);
        }

        // Fire!
        if(controller.rightTrigger > 0.5)
        {
            Fire(1);
        }
        if (controller.leftTrigger > 0.5)
        {
            FireShotgun(5);
        }
    }
    public override void OnDestroy()
    {
        // Return the controller to the InputManager.
        controller.checkedOut = false;
    }
}
public class PlayerBoat2 : PlayerBoat { }
public class PlayerBoat3 : PlayerBoat { }
