using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Color = UnityEngine.Color;
using Utils;
using Utils.Unity;

/*public class PlayerBoat : BoatController
{
    public override Color HullColor
    {
        get
        {
            Hsl hsl = WheelhouseColor.ToHsl();
            hsl.s *= 0.75f;
            hsl.l *= 0.75f;
            return hsl.ToUnityColor();
        }
    }
    public override Color GunColor => WheelhouseColor;
    public override Color WheelhouseColor => controller?.color ?? Color.red;
    public override Color EngineColor => WheelhouseColor;
    public VirtualController controller;

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
    public override void OnKilled(string killerName)
    {
        // Return the controller to the InputManager.
        controller.checkedOut = false;
    }
}
public class PlayerBoat2 : PlayerBoat { }
public class PlayerBoat3 : PlayerBoat { }*/
