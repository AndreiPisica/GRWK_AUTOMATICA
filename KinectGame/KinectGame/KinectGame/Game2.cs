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

using DigitalRune.Animation;
using DigitalRune.Game.Input;
using DigitalRune.Game.UI;
using DigitalRune.Game.States;
using DigitalRune.Game.UI.Controls;

namespace KinectGame
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game2 : Microsoft.Xna.Framework.Game
    {
     
        SpriteBatch spriteBatch;   //used for drawing textures
        private GraphicsDeviceManager _graphicsManager;  // gets the graphics device
        private InputManager _inputManager;  //digital rune class used for handeling input 
        private Simulation _simulation;     //simulation of physics in game
        private KinectWrapper _kinectWrapper;   //class that handles the aquisition of data from kinect and streaming
  
        private Func<AppBase>[] _createSampleDelegates;  //dont know what the fuck it does
        private AppBase _activeSample;   // creates a class of appbase which starts the mapping of skeleton 



        private UIManager _uiManager;           //gets the screens and updates them 
        private AnimationManager _animationManager;   //sets the animation parameters and type of animation

        static Game2()
        {
            DigitalRune.Licensing.AddSerialNumber("tgCYABno3eJAKNEBc9Q3hdtH0gEgACNBbmRyZWkgUGlzaWNhIzEjMSNOb25Db21tZXJjaWFsQIOyw5DNUVQEAwH8DzKvEPaI4Ziu5xO6/5FIs1jH5G/L6KNy97OiAYLIW38n7AqaRpYZfNoaQ9eV6DAoxg==");
        }

        public Game2()
        {
            _graphicsManager = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1280,
                PreferredBackBufferHeight = 720,
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            //input service
            _inputManager = new InputManager(false);
            Services.AddService(typeof(IInputService), _inputManager);

            _uiManager = new UIManager(this, _inputManager);
            Services.AddService(typeof(IUIService), _uiManager);

            _simulation = new Simulation();
            Services.AddService(typeof(Simulation), _simulation);

            _animationManager = new AnimationManager();
            Services.AddService(typeof(IAnimationService), _animationManager);

            Components.Add(new Camera(this));
            Components.Add(new RigidBodyRenderer(this, _simulation));
            Components.Add(new Help(this) { DrawOrder = 30 });
            Components.Add(new States(this));
            Components.Add(new KinectWrapper(this));
            Components.Add(new Poses(this));
            Components.Add(new Learning(this));
            Components.Add(new DynamicTimeWarping(this));
            Components.Add(new Recognition(this));
            




            _createSampleDelegates = new Func<AppBase>[]
            {
                () => new SkeletonMapping(this),
            };
           
            _activeSample = _createSampleDelegates[0]();
            Components.Add(_activeSample);

            BasicEffect bf = new BasicEffect(GraphicsDevice);
            bf.EnableDefaultLighting();

            _inputManager.EnableMouseCentering = false;
           
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
            
            base.LoadContent();
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
            _uiManager.Update(deltaTime);
            _animationManager.Update(deltaTime);
            _animationManager.ApplyAnimations();
            _simulation.Update(deltaTime);
            base.Update(gameTime);            
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
           // GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            base.Draw(gameTime);
            
        }
    }
}
