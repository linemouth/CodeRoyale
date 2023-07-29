using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SittingDuck : BoatController
{
    public override Color HullColor => new Color(0.81f, 0.59f, 0.16f);
    public override Color GunColor => new Color(0.94f, 0.47f, 0.14f);
    public override Color WheelhouseColor => new Color(0.81f, 0.64f, 0.29f);
    public override Color EngineColor => WheelhouseColor;

    // Does nothing
}
