
using UnityEngine;

public class VirtualController
{
    public int index { get; private set; }
    public Color color { get; private set; }
    public bool checkedOut = false;
    public Vector2 leftAxis;
    public Vector2 rightAxis;
    public float leftTrigger;
    public float rightTrigger;

    public VirtualController(int index, Color color)
    {
        this.index = index;
        this.color = color;
    }
}
