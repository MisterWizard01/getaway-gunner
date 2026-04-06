using System.IO;
using System.Runtime.CompilerServices;
using Engine.JsonConverters;
using Engine.Managers;
using Engine.Nodes;
using Newtonsoft.Json.Linq;

namespace BulletHail;

public class CustomTiledParser : TiledParser
{
    public override (string name, Node node) ParseObject(JuicyContentManager contentManager, JObject obj)
    {
        string name = "";
        Node objectNode;
        //var properties = ParseProperties(contentManager, obj);

        switch (obj.Value<string>("type"))
        {
            default:
                return base.ParseObject(contentManager, obj);
        }
        return (name, objectNode);
    }

    public Room ParseRoom(JuicyContentManager juicyCM, string path)
    {
        using StreamReader reader = new(path);
        var json = reader.ReadToEnd();
        var jObject = JObject.Parse(json);
        var scene = ParseMap(juicyCM, jObject);
        var room = new Room()
        {
            Dimensions = new((int)jObject["width"] * (int)jObject["tilewidth"], (int)jObject["height"]* (int)jObject["tileheight"]),
        };

        for (int i = 0; i < scene.CountChildren; i++)
        {
            var layer = scene.GetChild(i) as TileLayerNode;
            if (layer is not null)
                room.TileLayers.Add(layer);
        }

        for (var i = 0; i < scene.GetChild("Walls").CountChildren; i++)
        {
            var wall = scene.GetChild("Walls").GetChild(i) as ColliderNode;
            if (wall is not null)
                room.Walls.Add(wall);
        }

        var enemySpawns = scene.GetChild("Enemy spawns");
        if (enemySpawns is not null) {
            for (var i = 0; i < enemySpawns.CountChildren; i++)
            {
                var spawnPoint = scene.GetChild("Enemy spawns").GetChild(i) as Node2D;
                if (spawnPoint is not null)
                    room.Enemies.Add(new Enemy(spawnPoint.Position)
                    {
                        Sprites = [juicyCM.GenerateSprite("spritesheet", "enemydown")],
                        Colliders = [new ColliderNode(0, 0, 16, 16)],
                        Velocity = Game1.PickRandomVelocity(Game1.enemySpeed),
                    });
            }
        }
        return room;
    }
}