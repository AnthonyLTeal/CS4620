using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace CS4620IS.Components;
public class Cursor
{
    public Vector3? Location;
    public Action OnClick;
    public bool isListening = true;
    public short[] TriangleCollided;
}

public class InputState
{
    public KeyboardState KeyState;
    public MouseState MouseState;
    public bool RightMousePressed = false;
}