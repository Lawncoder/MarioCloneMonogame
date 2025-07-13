using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Mario.Components;
using Mario.Enemy;
using Mario.Helpers;
using Mario.Scenes;
using Mario.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Input;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using Color = Microsoft.Xna.Framework.Color;
using Path = System.IO.Path;
using Vector2 = nkast.Aether.Physics2D.Common.Vector2;
using World = Arch.Core.World;



namespace Mario;

public class Game1 : Core
{
    public const int SCREEN_WIDTH = 320;
    public const int SCREEN_HEIGHT = 180;
    public const int TILE_WIDTH = 16;
    public const float TILE_SIZE = 1f;
    
    private SpriteFont _default;
    private SpriteFont _defaultX5;
    
    public Game1() : base("Mario", 1920, 1080, false)
    {
        
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        
        EntityWorld = World.Create();
        Camera = new Transform();
        Camera.Position = new Vector2(0, 0);
        Camera.Scale = new Vector2(1,1);
        Camera.Rotation = 0f;
        PhysicsWorld.Gravity = PhysicsWorld.Gravity * -5;

    }
    protected override void Initialize()
    {
        base.Initialize();
        ChangeScene(new LevelOneScene());
    }

    public static void CreateKoopa(Vector2 position)
    {
        var koopaTroopaBody = PhysicsWorld.CreateBody(position, 0f, BodyType.Dynamic);
        koopaTroopaBody.FixedRotation = true;
        var koopaTroopaFixture =  koopaTroopaBody.CreateRectangle(1f, 1f, 1f, Vector2.Zero);
        koopaTroopaFixture.Tag = "Koopa";
        koopaTroopaFixture.Restitution = 0f;
        koopaTroopaFixture.Friction = 0f;
        koopaTroopaFixture.CollidesWith = CollisionAssistant.CategoryFromLayers(CollisionLayers.Ground, CollisionLayers.Block, CollisionLayers.Enemy, CollisionLayers.Player);
        koopaTroopaFixture.CollisionCategories = CollisionAssistant.CategoryFromLayers(CollisionLayers.Enemy);
        var koopaPatrol = new Patrol();
        var koopaTransform = new Transform()
        {
            Scale = new  Vector2(TILE_WIDTH/196f,TILE_WIDTH/290f),
        };
        var koopaKoopa = new KoopaTroopaComponent();
        EntityWorld.Create(koopaKoopa, koopaPatrol, koopaTransform, new PhysicsComponent(body : koopaTroopaBody, fixture: koopaTroopaFixture), "Koopa", new SpriteTypes(SpriteTypes.SpriteTypesEnum.Koopa));
    }

    public static void CreateQuestionBlock(QuestionBlockComponent blockComponent, Vector2 position)
    {
       
       
        var blockBody = PhysicsWorld.CreateBody(position + Vector2.One*.5f, 0f, BodyType.Static);
        var blockFixture = blockBody.CreateRectangle(TILE_SIZE, TILE_SIZE, 1f, Vector2.Zero);
        blockFixture.CollisionCategories = CollisionAssistant.CategoryFromLayers(CollisionLayers.Block);
        
        blockFixture.CollidesWith = CollisionAssistant.CategoryFromLayers(CollisionLayers.Player, CollisionLayers.Enemy);
        blockFixture.Tag = "Block";


        EntityWorld.Create("QBlock", blockComponent, new PhysicsComponent(fixture: blockFixture, body:blockBody), new SpriteTypes(SpriteTypes.SpriteTypesEnum.Question), new Transform()
        {
            Scale= new  Vector2(16/200f, 16/191f)
        });

    }
    public static void CreateGoomba(Vector2 position)
    {
        position += new Vector2(.5f, .5f);
        var goombaBody = PhysicsWorld.CreateBody(position, 0f, BodyType.Dynamic);
        goombaBody.FixedRotation = true;
        var goombaFixture = goombaBody.CreateRectangle(1, 1, 1, Vector2.Zero);
        goombaFixture.Tag = "Goomba";
        goombaFixture.Restitution = 0.1f;
        goombaFixture.Friction = 0f;
        goombaFixture.CollidesWith = CollisionAssistant.CategoryFromLayers(CollisionLayers.Player, CollisionLayers.Ground, CollisionLayers.Block, CollisionLayers.Enemy);
        goombaFixture.CollisionCategories = CollisionAssistant.CategoryFromLayers(CollisionLayers.Enemy);
        var goombaPatrol = new Patrol();
        var goombaTransform = new Transform();
        goombaTransform.Scale = new Vector2(TILE_WIDTH/728f,TILE_WIDTH/660f);
        EntityWorld.Create(goombaPatrol, new PhysicsComponent(goombaBody, goombaFixture), "goomba",goombaTransform, new SpriteTypes(SpriteTypes.SpriteTypesEnum.Goomba));
    }



    protected override void LoadContent()
    {
        
        _default = Content.Load<SpriteFont>("Default");
        _defaultX5 = Content.Load<SpriteFont>("DefaultX5");
        
       
        CreateCollisionLayer("map.json");

    }

 
    public static void CreateCollisionLayer(string jsonFilename)
    {
        bool[][] collisionTile;
        string filePath = Path.Combine(Content.RootDirectory, jsonFilename);
        using (var stream = TitleContainer.OpenStream(filePath))
        {
           
            using (JsonDocument doc = JsonDocument.Parse(stream))
            {
                collisionTile = new bool[doc.RootElement.GetProperty("mapWidth").GetInt32()][];
                for (var i = 0; i < doc.RootElement.GetProperty("mapWidth").GetInt32(); i++)
                {
                    collisionTile[i] = new bool[doc.RootElement.GetProperty("mapHeight").GetInt32()];
                }
                foreach (var tile in doc.RootElement.GetProperty("layers").EnumerateArray().Where(element =>
                         {
                             return element.GetProperty("name").GetString().Equals("Collision");
                         }).ToList()[0].GetProperty("tiles").EnumerateArray())
                {
                    collisionTile[tile.GetProperty("x").GetInt32()][tile.GetProperty("y").GetInt32()] = true;

                }
            }
        }

        List<Span> spans = new List<Span>();
       

        for (int i = 0; i < collisionTile[0].Length; i++)
        {
            Span currentSpan = null;
            for (int j = 0; j < collisionTile.Length; j++)
            {
                if (collisionTile[j][i])
                {
                    if (currentSpan == null)
                    {
                        currentSpan = new Span
                        {
                            x_start = j,
                            y = i,
                            length = 1
                        };
                    }
                    else
                    {
                        currentSpan.length++;
                    }
                }
                else
                {
                    if (currentSpan != null)
                    {
                        spans.Add(currentSpan);
                        
                    }
                    currentSpan = null;
                    
                }
            }

            if (currentSpan != null)
            {
                spans.Add(currentSpan);
            }
        }
        List<Rectangle> rectangles = new List<Rectangle>();
        Dictionary<Vector2, List<Span>> spanMerges = new Dictionary<Vector2, List<Span>>();
        foreach (var span in spans)
        {
           
            Vector2 key = new Vector2(span.length, span.x_start);
            if (!spanMerges.ContainsKey(key))
            {
                spanMerges.Add(key, new List<Span>());
            }
            spanMerges[key].Add(span);
        }
        
        foreach (var list in spanMerges.Values)
        {
            
            int minY = list[0].y;
            int maxY = list[0].y;
            foreach (var span in list)
            {
                minY = Math.Min(minY, span.y);
                maxY = Math.Max(maxY, span.y);
                
            }
     
            Rectangle newRect = new Rectangle();
            newRect.X = list[0].x_start;
            newRect.Y = minY;
            newRect.Width = list[0].length;
            newRect.Height = maxY - minY + 1;
            
            CreateGround(new Vector2(newRect.X, newRect.Y), newRect.Width, newRect.Height);
        }
    }

    private class Span
    {
        public int x_start;
        public int length;
        public int y;
        public override string ToString()
        {
            return $"X: {x_start}, Y: {y}, Length: {length}";
        }
    }

    private static void CreateGround(Vector2 position, int width, int height)
    {
        var fixture = PhysicsWorld.CreateBody(position).CreateRectangle(width, height, 1, new Vector2(width/2f, height/2f));
        fixture.CollidesWith = Category.All;
        fixture.CollisionCategories = CollisionAssistant.CategoryFromLayers(CollisionLayers.Ground);
        fixture.Tag = "Ground";
        fixture.Body.Tag = "Ground";
        EntityWorld.Create(new PhysicsComponent(fixture.Body, fixture), "ground");
    }

    private static void CreateHurtbox(Vector2 position, int width, int height)
    {
        var fixture = PhysicsWorld.CreateBody(position).CreateRectangle(width, height, 1, new Vector2(width/2f, height/2f));
        fixture.CollidesWith = Category.All;
        fixture.CollisionCategories = CollisionAssistant.CategoryFromLayers(CollisionLayers.Killer, CollisionLayers.Ground);
        fixture.Tag = "Ground";
        fixture.Body.Tag = "Ground";
        EntityWorld.Create(new PhysicsComponent(fixture.Body, fixture), "ground");
    }




    protected override void Update(GameTime gameTime)
    {
        
        //Escaping the Program
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
        base.Update(gameTime);

      
        
    }

    public class EntityTypes
    {
        public const string Koopa = "Koopa";
        public const string Goomba = "Goomba"; 
        public const string QuestionBlock = "QuestionBlock";
        
    }

   
    
    
    

    
    
    public override void PhysicsUpdate()
    {
        
        
        base.PhysicsUpdate();
        
 

        
       

        
       
    }
    protected override void Draw(GameTime gameTime)
    {
       
        

        base.Draw(gameTime);
    }

    public enum Type
    {
        Player, Goomba, Koopa, Tile, QuestionBlock
    }
    public struct Hitbox(FixedArray4<Vector2> vertices,Vector2 position,string name)
    {
        public readonly FixedArray4<Vector2> Vertices = vertices;
        public readonly Vector2 Position = position;
        public readonly string Name = name;
    }
    
}