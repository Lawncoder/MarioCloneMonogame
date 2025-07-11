using System;
using System.Collections.Generic;
using System.Text.Json;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Mario.Components;
using Mario.Enemy;
using Mario.Helpers;
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
    
    private Color _scoreColor = Color.Red;
    private Color _targetColor = Color.Red;

    private int _score = 0;
    
    private CollisionLayers Ground =  CollisionLayers.Ground;
    private CollisionLayers Player =  CollisionLayers.Player;
    private CollisionLayers Enemy = CollisionLayers.Enemy;
    private CollisionLayers Block = CollisionLayers.Block;
    private CollisionLayers Wall = CollisionLayers.Wall;
    private CollisionLayers Boundary = CollisionLayers.Boundary;
    
    
    private Sprite mario;
    private Sprite goomba;
    private Sprite questionBlock;
    private Sprite shell;
    private Sprite koopaTroopa;
    private Body collisionBoundary;

    RenderTarget2D _renderTarget;

    private Tilemap _map;
    private SpriteFont _defaultX5;
    
    private MouseInfo _mouseInfo = new MouseInfo();

    public static float MaxCameraX = 160;
   
   
    
    
    
    public bool ResolveCollisions(Contact contact)
    {
           
       
        if (CollisionAssistant.CategoryInCategories(Player, contact.FixtureB.CollisionCategories) &&
            CollisionAssistant.CategoryInCategories(Block, contact.FixtureA.CollisionCategories))
        {
            //Player collided with a block, that's it. Will have a query handle all game logic
            var blockHitComponent = new HitComponent();
            var normal = Vector2.One;
            contact.GetWorldManifold(out normal, out blockHitComponent.Points);
            blockHitComponent.CollisionLayer = contact.FixtureB.CollisionCategories;
            blockHitComponent.CollisionNormal = normal;
            
        

            var query = new QueryDescription().WithAll<QuestionBlockComponent, PhysicsComponent>();
            EntityWorld.Query(query, (Entity entity, ref PhysicsComponent physicsComponent) =>
            {
                if (physicsComponent.Fixture == contact.FixtureA)
                {
                    
                    if (!EntityWorld.Has<HitComponent>(entity))
                    {
                        EntityWorld.Add(entity, blockHitComponent);
                        

                    }
                }
            });

        }
        else
        {
            var query = new QueryDescription().WithAll<PhysicsComponent>();
            Entity entityA = Entity.Null;
            Entity entityB = Entity.Null;
            EntityWorld.Query(in query, (Entity entity, ref PhysicsComponent physics) =>
            {
                if (physics.Fixture == contact.FixtureA)
                {
                    
                    entityA = entity;
                    
                }
                if (physics.Fixture ==  contact.FixtureB)
                {
                    entityB = entity;
                }
            });
            
            var hitComponent = new HitComponent();
            var normal = Vector2.One;
            contact.GetWorldManifold(out normal, out hitComponent.Points);
            hitComponent.CollisionLayer = contact.FixtureB.CollisionCategories;
            hitComponent.CollisionNormal = normal;
            hitComponent.Position = contact.FixtureB.Body.Position;
            hitComponent.Fixture = contact.FixtureB;
            hitComponent.Body = contact.FixtureB.Body;
            hitComponent.Entity = entityB;
                    
                    
                    
            CommandBuffer.Add(entityA, hitComponent);
            
            hitComponent = new HitComponent();
            var normal2 = Vector2.One;
            contact.GetWorldManifold(out normal2, out hitComponent.Points);
            hitComponent.CollisionLayer = contact.FixtureA.CollisionCategories;
            hitComponent.CollisionNormal = normal2;
            hitComponent.Position = contact.FixtureA.Body.Position;
            hitComponent.Fixture = contact.FixtureA;
            hitComponent.Body = contact.FixtureA.Body;
            hitComponent.Entity = entityA;
                    
                    
                    
            CommandBuffer.Add(entityB, hitComponent);
        }
        
        
        
        return true;
    }
    
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





        Systems.Add(new DamageSystem(EntityWorld));
        Systems.Add(new KoopaTroopaShellSystem(EntityWorld)); 
        Systems.Add(new EnemyPatrolSystem(EntityWorld));
        Systems.Add(new CollisionResetter(EntityWorld));
        Systems.Add(new PlatformerControllerSystem(EntityWorld));
       
        


        PhysicsWorld.ContactManager.BeginContact += new BeginContactDelegate(ResolveCollisions);

    }


 
    
    protected override void Initialize()
    {
        
        
    
        
        
        base.Initialize();
        _renderTarget = new RenderTarget2D(GraphicsDevice, SCREEN_WIDTH, SCREEN_HEIGHT);


        CreateQuestionBlock(new QuestionBlockComponent()
        {
            QuestionBlockComponentType = QuestionBlockComponent.QuestionBlockComponentTypes.Score,
            Score = 100
        }, new Vector2(24, 5));
        CreateQuestionBlock(new QuestionBlockComponent()
        {
            QuestionBlockComponentType = QuestionBlockComponent.QuestionBlockComponentTypes.Score,
            Score = 100
        }, new Vector2(25, 5));
        CreateQuestionBlock(new QuestionBlockComponent()
        {
            QuestionBlockComponentType = QuestionBlockComponent.QuestionBlockComponentTypes.Score,
            Score = 100
        }, new Vector2(26, 5));

        #region Initialize Mario and Camera

        PlatformerCharacter marioData = new PlatformerCharacter();
        marioData.Acceleration = 30f;
        marioData.MaxSpeed = 5f;
        marioData.State = PlatformerCharacter.States.Falling;
        marioData.AutoCalculateForce(PhysicsWorld.Gravity.Y, 4.5f);
       
        
        var marioBody = PhysicsWorld.CreateBody(new Vector2(2, 10), 0f, BodyType.Dynamic);
        marioBody.FixedRotation = true;
        var marioCollider = marioBody.CreateRectangle(1f, 1f, 1, Vector2.Zero);
        marioCollider.Tag = "Mario";
        marioCollider.Friction = 0f;
        marioCollider.Restitution = 0f;
        marioCollider.CollidesWith = CollisionAssistant.CategoryFromLayers(Enemy, Ground, Block, Boundary, Wall);
        marioCollider.CollisionCategories = CollisionAssistant.CategoryFromLayers(Player);

   
        Transform marioTransform = new Transform();
        marioTransform.Position = new Vector2(0, 0);
        marioTransform.Rotation = 0f;
        marioTransform.Scale = new Vector2(16/348f,16/348f);
        EntityWorld.Create(new PhysicsComponent(marioBody, marioCollider), "mario",marioTransform, new SpriteTypes(SpriteTypes.SpriteTypesEnum.Mario), marioData);
        
       

       
        
        
        var cameraBoundryBody = PhysicsWorld.CreateBody(new Vector2(0, 15f), 0f, BodyType.Static);
        var cameraBoundryFixture = cameraBoundryBody.CreateRectangle(0.5f,40f, 1f, Vector2.Zero);
        cameraBoundryFixture.Tag = "CameraBoundry";
        cameraBoundryFixture.CollidesWith = CollisionAssistant.CategoryFromLayers(Player);
        cameraBoundryFixture.CollisionCategories = CollisionAssistant.CategoryFromLayers(Boundary);
        collisionBoundary = cameraBoundryBody;

        EntityWorld.Create(new PhysicsComponent(cameraBoundryBody, cameraBoundryFixture), "cameraBoundry", new Transform());

        #endregion
        
        
       
        CreateKoopa(new Vector2(12,10));
        CreateGoomba(new Vector2(13,10));
        CreateHurtbox(new Vector2(22, 11), 3, 2);

       

    }

    private void CreateKoopa(Vector2 position)
    {
        var koopaTroopaBody = PhysicsWorld.CreateBody(position, 0f, BodyType.Dynamic);
        koopaTroopaBody.FixedRotation = true;
        var koopaTroopaFixture =  koopaTroopaBody.CreateRectangle(1f, 1f, 1f, Vector2.Zero);
        koopaTroopaFixture.Tag = "Koopa";
        koopaTroopaFixture.Restitution = 0f;
        koopaTroopaFixture.Friction = 0f;
        koopaTroopaFixture.CollidesWith = CollisionAssistant.CategoryFromLayers(Ground, Block, Enemy, Player);
        koopaTroopaFixture.CollisionCategories = CollisionAssistant.CategoryFromLayers(Enemy);
        var koopaPatrol = new Patrol();
        var koopaTransform = new Transform()
        {
            Scale = new  Vector2(TILE_WIDTH/196f,TILE_WIDTH/290f),
        };
        var koopaKoopa = new KoopaTroopaComponent();
        EntityWorld.Create(koopaKoopa, koopaPatrol, koopaTransform, new PhysicsComponent(body : koopaTroopaBody, fixture: koopaTroopaFixture), "Koopa", new SpriteTypes(SpriteTypes.SpriteTypesEnum.Koopa));
    }

    private void CreateQuestionBlock(QuestionBlockComponent blockComponent, Vector2 position)
    {
       
       
        var blockBody = PhysicsWorld.CreateBody(position + Vector2.One*.5f, 0f, BodyType.Static);
        var blockFixture = blockBody.CreateRectangle(TILE_SIZE, TILE_SIZE, 1f, Vector2.Zero);
        blockFixture.CollisionCategories = CollisionAssistant.CategoryFromLayers(Block);
        
        blockFixture.CollidesWith = CollisionAssistant.CategoryFromLayers(Player, Enemy);
        blockFixture.Tag = "Block";


        EntityWorld.Create("QBlock", blockComponent, new PhysicsComponent(fixture: blockFixture, body:blockBody), new SpriteTypes(SpriteTypes.SpriteTypesEnum.Question), new Transform()
        {
            Scale= new  Vector2(16/200f, 16/191f)
        });

    }
    private void CreateGoomba(Vector2 position)
    {
        var goombaBody = PhysicsWorld.CreateBody(position, 0f, BodyType.Dynamic);
        goombaBody.FixedRotation = true;
        var goombaFixture = goombaBody.CreateRectangle(1, 1, 1, Vector2.Zero);
        goombaFixture.Tag = "Goomba";
        goombaFixture.Restitution = 0.1f;
        goombaFixture.Friction = 0f;
        goombaFixture.CollidesWith = CollisionAssistant.CategoryFromLayers(Player, Ground, Block, Enemy);
        goombaFixture.CollisionCategories = CollisionAssistant.CategoryFromLayers(Enemy);
        var goombaPatrol = new Patrol();
        var goombaTransform = new Transform();
        goombaTransform.Scale = new Vector2(TILE_WIDTH/728f,TILE_WIDTH/660f);
        EntityWorld.Create(goombaPatrol, new PhysicsComponent(goombaBody, goombaFixture), "goomba",goombaTransform, new SpriteTypes(SpriteTypes.SpriteTypesEnum.Goomba));
    }



    protected override void LoadContent()
    {
        
        _default = Content.Load<SpriteFont>("Default");
        _defaultX5 = Content.Load<SpriteFont>("DefaultX5");
        Texture2D texture = Content.Load<Texture2D>("mario");
        goomba = Sprite.FromFile("goomba");
        mario = Sprite.FromFile("mario");
        
        questionBlock = Sprite.FromFile("questionBlock");
        koopaTroopa =  Sprite.FromFile("koopaTroopa");
        shell =  Sprite.FromFile("shell");
        _map = Tilemap.FromFile(Content,"map.json", true);
        CreateCollisionLayer("map.json");

    }

 
    public void CreateCollisionLayer(string jsonFilename)
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
                foreach (var tile in doc.RootElement.GetProperty("layers")[0].GetProperty("tiles").EnumerateArray())
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

    public void CreateGround(Vector2 position, int width, int height)
    {
        var fixture = PhysicsWorld.CreateBody(position).CreateRectangle(width, height, 1, new Vector2(width/2f, height/2f));
        fixture.CollidesWith = Category.All;
        fixture.CollisionCategories = CollisionAssistant.CategoryFromLayers(Ground);
        fixture.Tag = "Ground";
        fixture.Body.Tag = "Ground";
        EntityWorld.Create(new PhysicsComponent(fixture.Body, fixture), "ground");
    }

    public void CreateHurtbox(Vector2 position, int width, int height)
    {
        var fixture = PhysicsWorld.CreateBody(position).CreateRectangle(width, height, 1, new Vector2(width/2f, height/2f));
        fixture.CollidesWith = Category.All;
        fixture.CollisionCategories = CollisionAssistant.CategoryFromLayers(CollisionLayers.Killer, Ground);
        fixture.Tag = "Ground";
        fixture.Body.Tag = "Ground";
        EntityWorld.Create(new PhysicsComponent(fixture.Body, fixture), "ground");
    }



    private bool _goombaSpawnFlag = false;
    protected override void Update(GameTime gameTime)
    {
        
       

        if (MaxCameraX / 16f > 15 && !_goombaSpawnFlag)
        {
            _goombaSpawnFlag = true;
            CreateGoomba(new Vector2(27,9));
        }
        
        
   
        _mouseInfo.Update();
        //Escaping the Program
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit(); 
        
      
        
        
        _scoreColor = Color.Lerp(_scoreColor, _targetColor, 0.3f);
        
        
        
        

        
       
       
            
       
       
       
       
      
     
       
       //Handling Question Boxes
       var query = new QueryDescription().WithAll<HitComponent, QuestionBlockComponent, PhysicsComponent>();
       EntityWorld.Query(query, (Entity entity, ref HitComponent block, ref QuestionBlockComponent blockComponent, ref PhysicsComponent physicsComponent) =>
       {
           if (block.CollisionNormal.Equals(GameDevMath.Up))
           {
               
               PhysicsWorld.Remove(physicsComponent.Body);
               _score += blockComponent.Score;
               _targetColor = Color.Lerp(Color.Red, Color.Green, blockComponent.Score/100f);
               if (entity.IsAlive())
               {
                   CommandBuffer.Destroy(entity);
               }
              
           }
          
                                
       });
       
       
      
       
       
       
       
       collisionBoundary.Position = new Vector2(-Camera.Position.X/TILE_WIDTH - .5f, collisionBoundary.Position.Y );
      
       

      
      


       
      
       
        base.Update(gameTime);

      
        CommandBuffer.Playback(EntityWorld);
    }



    public override void PhysicsUpdate()
    {
        
        
        base.PhysicsUpdate();
        
 

        
       

        
       
    }
    protected override void Draw(GameTime gameTime)
    {
       
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(Color.White);
        

        SpriteBatch.Begin(transformMatrix: Camera.ToMatrix(), samplerState:  SamplerState.PointClamp);
        //Draw Tilemap
        SpriteBatch.DrawCircle(Microsoft.Xna.Framework.Vector2.Zero, 5f, 20, Color.Wheat);
       _map.Draw(SpriteBatch);
       
        
       

        var query = new QueryDescription().WithAll<SpriteTypes>();
        EntityWorld.Query(in query, (Entity entity, ref SpriteTypes spriteType, ref Transform transform) =>
        {
            var sprite =  spriteType.SpriteType switch
            {
                SpriteTypes.SpriteTypesEnum.Mario => mario,
                SpriteTypes.SpriteTypesEnum.Koopa => koopaTroopa,
                SpriteTypes.SpriteTypesEnum.Goomba =>  goomba,
                SpriteTypes.SpriteTypesEnum.Shell =>  shell,
                SpriteTypes.SpriteTypesEnum.Question =>   questionBlock
            };
           
            
            sprite.Position = GameDevMath.Physics2ScreenVector(transform.Position);
            sprite.Scale = new Microsoft.Xna.Framework.Vector2(transform.Scale.X, transform.Scale.Y);
            sprite.Rotation = transform.Rotation;
            sprite.CenterOrigin();
            
            sprite.Effects = spriteType.Flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            
            sprite.Draw(spriteBatch: SpriteBatch);
            
        });
        
        query = new QueryDescription().WithAll<PhysicsComponent>();
        EntityWorld.Query(in query, (Entity entity, ref PhysicsComponent physics) =>
        {
            var shape = (PolygonShape)physics.Fixture.Shape;
            for (int i = 0; i < shape.Vertices.Count; i++)
            {
                bool killer = CollisionAssistant.CategoryInCategories(CollisionLayers.Killer, physics.Fixture.CollisionCategories);
                SpriteBatch.DrawLine(GameDevMath.Physics2ScreenVector(physics.Body.Position + shape.Vertices[i]), GameDevMath.Physics2ScreenVector(physics.Body.Position + shape.Vertices[(i+1)%shape.Vertices.Count]), killer ? Color.Red : Color.Aquamarine);
            }
        });
        
        
        
        SpriteBatch.End();
        
        
        
        
        GraphicsDevice.SetRenderTarget(null);
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        var region = new TextureRegion(_renderTarget, 0, 0, _renderTarget.Width, _renderTarget.Height);
        region.Draw(SpriteBatch, Microsoft.Xna.Framework.Vector2.Zero, Color.White, 0f, Microsoft.Xna.Framework.Vector2.Zero, 6f, SpriteEffects.None, 1f);
        SpriteBatch.End();
        //UI
        SpriteBatch.Begin();
        
        SpriteBatch.DrawString(_defaultX5, _score.ToString(), new Microsoft.Xna.Framework.Vector2(0, 0), _scoreColor);
        SpriteBatch.End();

        base.Draw(gameTime);
    }

    public enum Type
    {
        Player, Goomba, Koopa, Tile, QuestionBlock
    }
    struct Hitbox(FixedArray4<Vector2> vertices,Vector2 position,string name)
    {
        public readonly FixedArray4<Vector2> Vertices = vertices;
        public readonly Vector2 Position = position;
        public readonly string Name = name;
    }
    
}