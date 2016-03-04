using System.Linq;
using DigitalRune.Animation.Character;
using DigitalRune.Game.Input;
using DigitalRune.Geometry;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using KinectGame.GeneralComponents;
using KinectGame.GeneralComponents.RigidBodyRendered;
using Microsoft.Kinect;
using System.Text;

namespace KinectGame.AppCore
{
    public class AppBase : DrawableGameComponent
    {
        private bool _drawKinectSkeletons = false;

        // The height of the Kinect sensor position. In our case it was 0.8 m above the floor.
        protected float KinectSensorHeight = 0.8f;
        // Services and resources that can be accessed by the derived classes.
        protected IInputService InputService { get; private set; }
        protected Simulation Simulation { get; private set; }
        protected SpriteBatch SpriteBatch { get; private set; }
        protected SpriteFont SpriteFont { get; private set; }
        protected BasicEffect BasicEffect { get; private set; }
        protected Camera Camera { get; private set; }
        protected KinectWrapper KinectWrapper { get; private set; } 
        protected RigidBodyRenderer RigidBodyRenderer { get; private set; }
        protected Model SandboxModel { get; private set; }
        protected States st { get; set; }
        private Microsoft.Kinect.Skeleton _pose_view { get; set; }
        private Learning Learning { get;  set; }
        private DynamicTimeWarping DTW { get; set; }

        // The message that is printed on top of the screen.
        protected string DisplayMessage { get; set; }

        public AppBase(Game game)
          : base(game)
        {
            // Get services.
            InputService = (IInputService)game.Services.GetService(typeof(IInputService));
            Simulation = (Simulation)game.Services.GetService(typeof(Simulation));

            // Get game components.
            Camera = Game.Components.OfType<Camera>().First();
            KinectWrapper = Game.Components.OfType<KinectWrapper>().First();
            RigidBodyRenderer = Game.Components.OfType<RigidBodyRenderer>().First();
            st = Game.Components.OfType<States>().First();
            Learning = Game.Components.OfType<Learning>().First();
            DTW = Game.Components.OfType<DynamicTimeWarping>().First();

            _pose_view = new Microsoft.Kinect.Skeleton();

            DisplayMessage = "App Base";
        }


        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            SpriteFont = Game.Content.Load<SpriteFont>("SpriteFont1");
            BasicEffect = new BasicEffect(GraphicsDevice);
            // SandboxModel = Game.Content.Load<Model>("Sandbox/Sandbox");
            
            base.LoadContent();
        }


        public override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // <PageUp>/<PageDown> --> Change sensor height.
            if (InputService.IsDown(Keys.PageUp))
                KinectSensorHeight += 0.1f * deltaTime;
            if (InputService.IsDown(Keys.PageDown))
                KinectSensorHeight -= 0.1f * deltaTime;

            // <Up>/<Down> --> Change y offset of Kinect skeleton data.
            if (InputService.IsDown(Keys.Up))
                KinectWrapper.Offset = KinectWrapper.Offset + new Vector3F(0, 0.1f * deltaTime, 0);
            if (InputService.IsDown(Keys.Down))
                KinectWrapper.Offset = KinectWrapper.Offset - new Vector3F(0, 0.1f * deltaTime, 0);

            // <+>/<-> --> Change scale of Kinect skeleton data.
           /* if (InputService.IsDown(Keys.OemPlus) || InputService.IsDown(Keys.Add))
                KinectWrapper.Scale = KinectWrapper.Scale + new Vector3F(0.1f * deltaTime);
            if (InputService.IsDown(Keys.OemMinus) || InputService.IsDown(Keys.Subtract))
                KinectWrapper.Scale = KinectWrapper.Scale - new Vector3F(0.1f * deltaTime);*/

            // <Back> --> Toggle drawing of Kinect skeletons.
            if (InputService.IsPressed(Keys.Back, false))
                _drawKinectSkeletons = !_drawKinectSkeletons;

            base.Update(gameTime);
        }


        public override void Draw(GameTime gameTime)
        {
            // Restore render states.
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.SamplerStates[0] = SamplerState.AnisotropicWrap;
            // Update effect parameters.
            BasicEffect.World = Matrix.Identity;
            BasicEffect.View = Camera.View;
            BasicEffect.Projection = Camera.Projection;

            // Draw background.
            //SandboxModel.Draw(Matrix.Identity, Camera.View, Camera.Projection);

            // Let derived classes do some drawing.
            OnDrawSample(gameTime);
            
            // Draw Kinect skeletons for debugging.
            if (_drawKinectSkeletons)
            {
                BasicEffect.World = Matrix.CreateTranslation(0, KinectSensorHeight, 15);
                
                SkeletonHelper.DrawBones(KinectWrapper.SkeletonPoseA, GraphicsDevice, BasicEffect, 0.1f, SpriteBatch, SpriteFont, Color.Orange);
            }

            SpriteBatch.Begin();

            // Draw display message.      
            float left = MathHelper.Max(GraphicsDevice.Viewport.TitleSafeArea.Left, 20);
            float top = MathHelper.Max(GraphicsDevice.Viewport.TitleSafeArea.Top, 20);
            Vector2 position = new Vector2(left, top);
            SpriteBatch.DrawString(SpriteFont, DisplayMessage, position, Color.White);
            
            
            
            if (KinectWrapper.IsRunning)
            {
                StringBuilder a = new StringBuilder();

                if (KinectWrapper.IsTrackedA)
                {
                    if (Learning.pose_taken == false)
                    {
                        _pose_view = KinectWrapper.getSkeletons();

                    }

                        foreach (Joint jnt in _pose_view.Joints)
                        {
                            a.Append(jnt.JointType.ToString() + ":");
                            a.Append(" ");
                            a.Append(jnt.Position.X.ToString());
                            a.Append(" ");
                            a.Append(jnt.Position.Y.ToString());
                            a.Append(" ");
                            a.Append(jnt.Position.Z.ToString());
                            a.Append("\n");
                        
                    }
                    // Draw Kinect settings info.
                    string message = "Kinect sensor height (<PageUp>/<PageDown>):  " + KinectSensorHeight
                                     + "\nKinect skeleton Y offset (<Up>/<Down>):  " + KinectWrapper.Offset.Y
                                     + "\nJoints Positions:\n"
                                     + a;
                  
                        a.Clear();
                    


                    //  + "\nKinect skeleton scale (<+>/<->):  " + KinectWrapper.Scale.X
                    //  + "\nDraw Kinect skeletons (<Back>):  " + _drawKinectSkeletons;
                    left = MathHelper.Max(GraphicsDevice.Viewport.TitleSafeArea.Right - 500, 20);
                    position = new Vector2(left, top);
                    SpriteBatch.DrawString(SpriteFont, message, position, Color.White);
                }
                    KinectWrapper.DrawColorImage(SpriteBatch, GraphicsDevice, new Rectangle(0, 450, 320, 240));
                
            }
            else
            {
                // Kinect not found. Draw error message.
                string message = "COULD NOT INITIALIZE KINECT DEVICE! PLEASE MAKE SURE THE DEVICE IS CONNECTED.";
                position = new Vector2(100, 200);
                SpriteBatch.DrawString(SpriteFont, message, position, Color.Red);
            }

            SpriteBatch.End();
           
                base.Draw(gameTime);
            
        }


        // Derived class can override this method to draw stuff.
        protected virtual void OnDrawSample(GameTime gameTime)
        {
        }


        // Helper method that draws a model with SkinnedEffects.
        protected void DrawSkinnedModel(Model model, Pose pose, SkeletonPose skeletonPose)
        {
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (SkinnedEffect effect in mesh.Effects)
                {
                    // SkeletonPose.SkinningMatricesXna provides an array of transformations as needed
                    // by the SkinnedEffect.
                    effect.SetBoneTransforms(skeletonPose.SkinningMatricesXna);

                    // The world space transformation.
                    effect.World = pose;

                    // Camera transformation.
                    effect.View = Camera.View;
                    effect.Projection = Camera.Projection;

                    // Lighting.
                    effect.EnableDefaultLighting();
                    effect.SpecularColor = new Vector3(0.25f);
                    effect.SpecularPower = 16;
                }

                mesh.Draw();
            }
        }
    }
}
