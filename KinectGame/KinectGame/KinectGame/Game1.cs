using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using DigitalRune.Game.Input;
using DigitalRune.Physics;
using KinectGame.GeneralComponents;
using KinectGame.GeneralComponents.RigidBodyRendered;
using KinectGame.AppCore;
using KinectGame.AppCore.MappingSkeleton;

namespace KinectGame
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
       
        SpriteBatch spriteBatch;
        private GraphicsDeviceManager _graphicsManager;
        private InputManager _inputManager;
        private Simulation _simulation;
     //   private KinectWrapper _kinectWrapper;

        private Func<AppBase>[] _createSampleDelegates;
        private int _activeSampleIndex = -1;
        private AppBase _activeSample;

        static Game1()
        {
            DigitalRune.Licensing.AddSerialNumber("tgCYABno3eJAKNEBc9Q3hdtH0gEgACNBbmRyZWkgUGlzaWNhIzEjMSNOb25Db21tZXJjaWFsQIOyw5DNUVQEAwH8DzKvEPaI4Ziu5xO6/5FIs1jH5G/L6KNy97OiAYLIW38n7AqaRpYZfNoaQ9eV6DAoxg==");
        }

        public Game1()
        {
            _graphicsManager = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1280,
                PreferredBackBufferHeight = 720,
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = false;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            //input service
            _inputManager = new InputManager(false);
            Services.AddService(typeof(IInputService), _inputManager);
            _simulation = new Simulation();
            Services.AddService(typeof(Simulation), _simulation);
           

            Components.Add(new Camera(this));
            Components.Add(new RigidBodyRenderer(this, _simulation));
            Components.Add(new Help(this) { DrawOrder = 30 });
            Components.Add(new KinectWrapper(this));
        
            
            

            _createSampleDelegates = new Func<AppBase>[]
            {
                () => new SkeletonMapping(this),
            };
            _activeSampleIndex = 0;
            _activeSample = _createSampleDelegates[0]();
            Components.Add(_activeSample);

            BasicEffect bf = new BasicEffect(GraphicsDevice);
            bf.EnableDefaultLighting();


            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            

            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            TimeSpan deltaTime = gameTime.ElapsedGameTime;
            _inputManager.Update(deltaTime);

            if (_inputManager.IsDown(Keys.Escape) || _inputManager.IsPressed(Buttons.Back, false, PlayerIndex.One))
            {
                Exit();
            }

            _simulation.Update(deltaTime);

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // TODO: Add your update logic here
                       
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            

            base.Draw(gameTime);
            
        }
    }
}
