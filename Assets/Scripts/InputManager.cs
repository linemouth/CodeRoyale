using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public int controllerCount = 4;

    private readonly List<VirtualController> controllers = new List<VirtualController>();

    public VirtualController GetController()
    {
        VirtualController controller = controllers.FirstOrDefault(controller => !controller.checkedOut);
        if (controller != null)
        {
            controller.checkedOut = true;
        }
        return controller;
    }
    public void ReturnController(VirtualController controller)
    {
        controller.checkedOut = false;
    }

    private void Awake()
    {
        Color[] colors = new Color[]
        {
            Color.red,
            Color.blue,
            Color.yellow,
            Color.green
        };
        for (int i = 1; i <= controllerCount; ++i)
        {
            controllers.Add(new VirtualController(i, colors[i - 1]));
        }
    }
    // Update is called once per frame
    void Update()
    {
        for (int i = 1; i <= controllerCount; ++i)
        {
            VirtualController controller = controllers[i - 1];
            controller.leftAxis = new Vector2(
                Input.GetAxis($"Controller {i} Left X-Axis"),
                Input.GetAxis($"Controller {i} Left Y-Axis")
            );

            controller.rightAxis = new Vector2(
                Input.GetAxis($"Controller {i} Right X-Axis"),
                Input.GetAxis($"Controller {i} Right Y-Axis")
            );
            controller.leftTrigger = Input.GetAxis($"Controller {i} Left Trigger");
            controller.rightTrigger = Input.GetAxis($"Controller {i} Right Trigger");
        }
    }
}
