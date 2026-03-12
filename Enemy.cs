using Engine;
using Engine.Nodes;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulletHail;

public class Enemy(Vector2 position) : GameObject(position)
{
    public const int ShotDelay = 60;

    public int Health = 5, NextShot = 0;
}
