

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Mario.Components;
using Mario.Helpers;
using Mario.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Scenes;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Dynamics;
using Vector2 = nkast.Aether.Physics2D.Common.Vector2;



namespace Mario.Scenes;



public class LevelOneScene : Scene
{
    
    private CollisionLayers Ground =  CollisionLayers.Ground;
    private CollisionLayers Player =  CollisionLayers.Player;
    private CollisionLayers Enemy = CollisionLayers.Enemy;
    private CollisionLayers Block = CollisionLayers.Block;
    private CollisionLayers Wall = CollisionLayers.Wall;
    private CollisionLayers Boundary = CollisionLayers.Boundary;
    private Sprite _mario;
    private Sprite _goomba;
    private Sprite _questionBlock;
    private Sprite _shell;
    private Sprite _koopaTroopa;
    private Body _collisionBoundary;
    private Color _scoreColor = Color.Red;
    private Color _targetColor = Color.Red;
    private Tilemap _map;
    private RenderTarget2D _renderTarget;
    private float _maxCameraX = 160;
    private int _score = 0;
    private Dictionary<Vector2, string> _enemySpawns = new();
    private SpriteFont _marioFont;
    private float _timeLeft = 10f; // The default time for nes mario
    

    


    public void CreateEntity(string type, Vector2 position, bool init)
    {
        switch (type)
        {
            case Game1.EntityTypes.Koopa:
                if (!init) Game1.CreateKoopa(position);
                else _enemySpawns.Add(position, Game1.EntityTypes.Koopa);
                break;
            case Game1.EntityTypes.Goomba:
                if (!init) Game1.CreateGoomba(position);
                else _enemySpawns.Add(position, Game1.EntityTypes.Goomba);
                break;
            case Game1.EntityTypes.QuestionBlock :
                Game1.CreateQuestionBlock(new QuestionBlockComponent {Score = 100, QuestionBlockComponentType = QuestionBlockComponent.QuestionBlockComponentTypes.Score}, position);
                break;
            default:
                throw new InvalidEnumArgumentException("Not a valid entity type");
        }
    }
    public void CreateEntitiesFromData(string json, string type )
    {
        using (var stream = TitleContainer.OpenStream(Path.Combine(Content.RootDirectory, json)))
        {
            using (var document = JsonDocument.Parse(stream))
            {
                var enumerator = document.RootElement.GetProperty("layers").EnumerateArray().Where(e =>
                {
                    return e.GetProperty("name").GetString().Equals(type);
                }).ToList()[0].GetProperty("tiles").EnumerateArray();
                Console.WriteLine(enumerator.ToString());
                foreach (var tile in enumerator )
                {
                    CreateEntity(type, new Vector2(tile.GetProperty("x").GetInt16(), tile.GetProperty("y").GetInt16()),true);
                }
            }
        }
    }
    
    public override void Initialize()
    {
        base.Initialize();
        Systems.Add(new EnemyPatrolSystem(Core.EntityWorld));
        Systems.Add(new KoopaTroopaShellSystem(Core.EntityWorld));
        Systems.Add(new PlatformerControllerSystem(Core.EntityWorld));
        Systems.Add(new DamageSystem(Core.EntityWorld));
        Systems.Add(new CollisionResetter(Core.EntityWorld));
        
        
        _renderTarget = new RenderTarget2D(Core.GraphicsDevice, Game1.SCREEN_WIDTH, Game1.SCREEN_HEIGHT);
         #region Initialize Mario and Camera

        PlatformerCharacter marioData = new PlatformerCharacter();
        marioData.Acceleration = 30f;
        marioData.MaxSpeed = 5f;
        marioData.State = PlatformerCharacter.States.Falling;
        marioData.AutoCalculateForce(Core.PhysicsWorld.Gravity.Y, 4.5f);
       
        
        var marioBody = Core.PhysicsWorld.CreateBody(new Vector2(2, 10), 0f, BodyType.Dynamic);
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
        Core.EntityWorld.Create(new PhysicsComponent(marioBody, marioCollider), "mario",marioTransform, new SpriteTypes(SpriteTypes.SpriteTypesEnum.Mario), marioData);
        
       

       
        
        
        var cameraBoundryBody = Core.PhysicsWorld.CreateBody(new Vector2(0, 15f), 0f, BodyType.Static);
        var cameraBoundryFixture = cameraBoundryBody.CreateRectangle(0.5f,40f, 1f, Vector2.Zero);
        cameraBoundryFixture.Tag = "CameraBoundry";
        cameraBoundryFixture.CollidesWith = CollisionAssistant.CategoryFromLayers(Player);
        cameraBoundryFixture.CollisionCategories = CollisionAssistant.CategoryFromLayers(Boundary);
        _collisionBoundary = cameraBoundryBody;

        Core.EntityWorld.Create(new PhysicsComponent(cameraBoundryBody, cameraBoundryFixture), "cameraBoundry", new Transform());

        #endregion
        
        
       
        CreateEntitiesFromData("map.json", Game1.EntityTypes.Goomba);
        CreateEntitiesFromData("map.json", Game1.EntityTypes.Koopa);
        CreateEntitiesFromData("map.json", Game1.EntityTypes.QuestionBlock);
        Game1.CreateCollisionLayer("map.json");
        Core.Camera.Position = new Vector2(Core.Camera.Position.X, -16);
    }

    public override void LoadContent()
    {
        base.LoadContent();
        _goomba = Sprite.FromFile("goomba");
        _mario = Sprite.FromFile("mario");
        _marioFont = Content.Load<SpriteFont>("smbNes");
        _questionBlock = Sprite.FromFile("questionBlock");
        _koopaTroopa =  Sprite.FromFile("koopaTroopa");
        _shell =  Sprite.FromFile("shell");
        _map = Tilemap.FromFile(Content,"map.json", "Graphics");
        
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        _timeLeft -= gameTime.ElapsedGameTime.Milliseconds / 1000f;
        
        
        var buffer = new List<Vector2>();
        foreach (var position in _enemySpawns.Keys)
        {
            if (position.X - _maxCameraX/16f <=10f)
            {
                CreateEntity(_enemySpawns[position], position, false);
                buffer.Add(position);
            }
        }

        foreach (var position in buffer)
        {
            _enemySpawns.Remove(position);
        }
        _scoreColor = Color.Lerp(_scoreColor, _targetColor, 0.3f);
        
        //Handling Question Boxes
        var query = new QueryDescription().WithAll<HitComponent, QuestionBlockComponent, PhysicsComponent>();
        Core.EntityWorld.Query(query, (Entity entity, ref HitComponent block, ref QuestionBlockComponent blockComponent, ref PhysicsComponent physicsComponent) =>
        {
            if (block.CollisionNormal.Y < -.9f)
            {
               
                Core.PhysicsWorld.Remove(physicsComponent.Body);
                _score += blockComponent.Score;
                _targetColor = Color.Lerp(Color.Red, Color.Green, blockComponent.Score/100f);
                if (entity.IsAlive())
                {
                    Core.CommandBuffer.Destroy(entity);
                }
              
            }
          
                                
        });
        query = new QueryDescription().WithAll<PlatformerCharacter, PhysicsComponent>();
        Core.EntityWorld.Query(in query, (Entity entity, ref PhysicsComponent physicsComponent) =>
        {
            if (Core.EntityWorld.Has<DeathComponent>(entity))
            {
                if (Core.EntityWorld.Get<DeathComponent>(entity).TimeToActuallyDie <= 0)
                {
                    KillScene(); // Shouldn't be using a magic number here but this just means resetting the position of the camera
                }
            }
            else
            {
                if (_timeLeft <= 0)
                {
                    Core.CommandBuffer.Add<DeathComponent>(entity);
                }
            }

            _maxCameraX = Math.Max(_maxCameraX, GameDevMath.Physics2ScreenVector(physicsComponent.Body.Position).X);
            Core.Camera.Position.X = GameDevMath.LerpWithClamp(Core.Camera.Position.X, -_maxCameraX + Game1.SCREEN_WIDTH * .5f/Core.Camera.Scale.X, .4f);

        });
        
        _collisionBoundary.Position = new Vector2(-Core.Camera.Position.X/Game1.TILE_WIDTH - .5f, _collisionBoundary.Position.Y );
        Console.WriteLine(_maxCameraX);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.SetRenderTarget(_renderTarget);
        Core.GraphicsDevice.Clear(Color.CornflowerBlue);
        

        Core.SpriteBatch.Begin(transformMatrix: Core.Camera.ToMatrix(), samplerState:  SamplerState.PointClamp);
        //Draw Tilemap
        
       _map.Draw(Core.SpriteBatch);
       
        
       

        var query = new QueryDescription().WithAll<SpriteTypes>();
        Core.EntityWorld.Query(in query, (Entity entity, ref SpriteTypes spriteType, ref Transform transform) =>
        {
            var sprite =  spriteType.SpriteType switch
            {
                SpriteTypes.SpriteTypesEnum.Mario => _mario,
                SpriteTypes.SpriteTypesEnum.Koopa => _koopaTroopa,
                SpriteTypes.SpriteTypesEnum.Goomba =>  _goomba,
                SpriteTypes.SpriteTypesEnum.Shell =>  _shell,
                SpriteTypes.SpriteTypesEnum.Question =>   _questionBlock
            };
           
            
            sprite.Position = GameDevMath.Physics2ScreenVector(transform.Position);
            sprite.Scale = new Microsoft.Xna.Framework.Vector2(transform.Scale.X, transform.Scale.Y);
            sprite.Rotation = transform.Rotation;
            sprite.CenterOrigin();
            
            sprite.Effects = spriteType.Flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            
            sprite.Draw(spriteBatch: Core.SpriteBatch);
            
        });
#if DEBUG
        query = new QueryDescription().WithAll<PhysicsComponent>();
        Core.EntityWorld.Query(in query, (Entity entity, ref PhysicsComponent physics) =>
        {
            var shape = (PolygonShape)physics.Fixture.Shape;
            for (int i = 0; i < shape.Vertices.Count; i++)
            {
                bool killer = CollisionAssistant.CategoryInCategories(CollisionLayers.Killer, physics.Fixture.CollisionCategories);
                Core.SpriteBatch.DrawLine(GameDevMath.Physics2ScreenVector(physics.Body.Position + shape.Vertices[i]), GameDevMath.Physics2ScreenVector(physics.Body.Position + shape.Vertices[(i+1)%shape.Vertices.Count]), killer ? Color.Red : Color.Aquamarine);
            }
        });
#endif
        
        
        
        Core.SpriteBatch.End();
        
        
        
        
        Core.GraphicsDevice.SetRenderTarget(null);
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        var region = new TextureRegion(_renderTarget, 0, 0, _renderTarget.Width, _renderTarget.Height);
        region.Draw(Core.SpriteBatch, Microsoft.Xna.Framework.Vector2.Zero, Color.White, 0f, Microsoft.Xna.Framework.Vector2.Zero, 6f, SpriteEffects.None, 1f);
        Core.SpriteBatch.End();
        //UI
        Core.SpriteBatch.Begin();

        float scoreXPos = Game1.SCREEN_WIDTH * 6 * .2f;
        
        Core.SpriteBatch.DrawString(_marioFont, $"Score:{_score}", new Microsoft.Xna.Framework.Vector2(scoreXPos, 5f), Color.Black * .4f);
        Core.SpriteBatch.DrawString(_marioFont, $"Score:{_score}", new Microsoft.Xna.Framework.Vector2(scoreXPos, 0), Color.White);
        Core.SpriteBatch.DrawString(_marioFont, $"Time:{(int)_timeLeft}", new Microsoft.Xna.Framework.Vector2(Game1.SCREEN_WIDTH*6 - scoreXPos, 5f), Color.Black * .4f, 0f, new Microsoft.Xna.Framework.Vector2(_marioFont.MeasureString($"Time:{(int)_timeLeft}").X, 0f), 1f, SpriteEffects.None, 0f);

        Core.SpriteBatch.DrawString(_marioFont, $"Time:{(int)_timeLeft}", new Microsoft.Xna.Framework.Vector2(Game1.SCREEN_WIDTH*6 - scoreXPos, 0), Color.White, 0f, new Microsoft.Xna.Framework.Vector2(_marioFont.MeasureString($"Time:{(int)_timeLeft}").X, 0f), 1f, SpriteEffects.None, 0f);
        
        Core.SpriteBatch.End();
        base.Draw(gameTime);
    }

    private static void KillScene()
    {
       
        Core.ChangeScene(new LevelOneScene());
    }
}