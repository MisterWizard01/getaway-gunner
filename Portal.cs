using System;
using System.Collections.Generic;
using Engine;
using Engine.Nodes;
using Microsoft.Xna.Framework;
using MathHelper = Engine.MathHelper;

namespace BulletHail;

public class Portal(Vector2 position) : GameObject(position)
{
    public List<Particle> Particles = [];

    public bool Open;

    public float OpenFrames;

    public override void Update(Node parent, int frameNumber, InputState inputState)
    {
        base.Update(parent, frameNumber, inputState);

        if (Open && OpenFrames < Sprites[0].Animation.Width)
        {
            OpenFrames += 0.5f;
        }

        for (int i = Particles.Count - 1; i >= 0; i--)
        {
            var particle = Particles[i];
            particle.Update(null, frameNumber, inputState);
            if (particle.Sprites[0].AnimationOver)
            {
                Particles.Remove(particle);
            }
        }
    }

    public override void Draw(Node parent, Camera camera, Vector2 referencePoint)
    {
        base.Draw(parent, camera, referencePoint);

        foreach(var particle in Particles)
        {
            particle.Draw(this, camera, referencePoint + Position);
        }
    }
}
