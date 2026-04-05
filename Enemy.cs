using Engine;
using Engine.Nodes;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulletHail;

public enum EnemyState
{
    Patrolling,
    Chasing,
    Attacking,
}

public class Enemy(Vector2 position) : GameObject(position)
{
    public const int ShotDelay = 60;

    public int Health = 5, NextShot = 0;

    public EnemyState state;

    public Vector2 target;
}
