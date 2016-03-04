using System;
using System.Collections.Generic;
using DigitalRune.Animation.Character;
using DigitalRune.Geometry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DigitalRune.Graphics.SceneGraph;
using System.Linq;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Graphics;

using DigitalRune.Physics;
using DigitalRune.Physics.Constraints;
using DigitalRune.Physics.Specialized;




namespace KinectGame.AppCore.MappingSkeleton
{
    public class SkeletonMapping : AppBase
    {
        // The 3D models.
        private Model _modelA;
        private SkeletonPose _skeletonPoseA;
        private SkeletonMapper _skeletonMapperA;
        private SkeletonPoseFilter _filterA;

        private bool _drawModelSkeletons = true;

        public SkeletonMapping(Game game) : base(game)
         {
         }

        protected override void LoadContent()
        {
            InitializeModels();
            _filterA = new SkeletonPoseFilter(_skeletonPoseA);
            InitializeSkeletonMappers();
            UpdateDisplayMessage();
            base.LoadContent();
        }

        private void InitializeModels()
        {
            // Load the two different 3D human models.
            // The models use our custom SkinnedModelProcessor as the content processor! 
            // This content processor stores the model's skeleton in the additional data of the model.
            //95mannnequin-group/95mannnequin-group

            _modelA = Game.Content.Load<Model>("SixthModel/SecondModel");
                        
            var additionalData = (Dictionary<string, object>)_modelA.Tag;
            var skeleton = (Skeleton)additionalData["Skeleton"];
            _skeletonPoseA = SkeletonPose.Create(skeleton);
        }

        private void InitializeSkeletonMappers()
        {
                 _skeletonMapperA = new SkeletonMapper(KinectWrapper.SkeletonPoseA, _skeletonPoseA);
                 var ks = KinectWrapper.SkeletonPoseA.Skeleton;
                 var ms = _skeletonPoseA.Skeleton;

                 _skeletonMapperA.BoneMappers.Add(new DirectBoneMapper(ks.GetIndex("HipCenter"), ms.GetIndex("HipCenter"))
                 {
                     MapTranslations = true,
                     ScaleAToB = 1f,           // TODO: Make this scale factor configurable.
                 });

                 // An UpperBackBoneMapper is a special bone mapper that is specifically designed for
                 // spine bones. It uses the spine, neck and shoulders to compute the rotation of the spine
                 // bone. This rotations is transferred to the Dude's "Spine" bone. 
                 // (An UpperBackBoneMapper does not transfer bone translations.)
                 _skeletonMapperA.BoneMappers.Add(new UpperBackBoneMapper(
                   ks.GetIndex("Spine"), ks.GetIndex("ShoulderCenter"), ks.GetIndex("ShoulderLeft"), ks.GetIndex("ShoulderRight"),
                   ms.GetIndex("Spine"), ms.GetIndex("ShoulderCenter"), ms.GetIndex("ShoulderLeft"), ms.GetIndex("ShoulderRight")));

                 // A ChainBoneMapper transfers the rotation of a bone chain. In this case, it rotates
                 // the Dude's "R_UpperArm" bone. It makes sure that the direction from the Dude's
                 // "R_Forearm" bone origin to the "R_UpperArm" origin is parallel, to the direction
                 // "ElbowLeft" to "ShoulderLeft" of the Kinect skeleton.
                 // (An ChainBoneMapper does not transfer bone translations.)
                 _skeletonMapperA.BoneMappers.Add(new ChainBoneMapper(ks.GetIndex("ShoulderLeft"), ks.GetIndex("ElbowLeft"), ms.GetIndex("ShoulderLeft"), ms.GetIndex("ElbowLeft")));

                 // And so on...
                 _skeletonMapperA.BoneMappers.Add(new ChainBoneMapper(ks.GetIndex("ShoulderRight"), ks.GetIndex("ElbowRight"), ms.GetIndex("ShoulderRight"), ms.GetIndex("ElbowRight")));
                 _skeletonMapperA.BoneMappers.Add(new ChainBoneMapper(ks.GetIndex("ElbowLeft"), ks.GetIndex("WristLeft"), ms.GetIndex("ElbowLeft"), ms.GetIndex("WristLeft")));
                 _skeletonMapperA.BoneMappers.Add(new ChainBoneMapper(ks.GetIndex("ElbowRight"), ks.GetIndex("WristRight"), ms.GetIndex("ElbowRight"), ms.GetIndex("WristRight")));
                 _skeletonMapperA.BoneMappers.Add(new ChainBoneMapper(ks.GetIndex("HipLeft"), ks.GetIndex("KneeLeft"), ms.GetIndex("HipLeft"), ms.GetIndex("KneeLeft")));
                 _skeletonMapperA.BoneMappers.Add(new ChainBoneMapper(ks.GetIndex("HipRight"), ks.GetIndex("KneeRight"), ms.GetIndex("HipRight"), ms.GetIndex("KneeRight")));
                 _skeletonMapperA.BoneMappers.Add(new ChainBoneMapper(ks.GetIndex("KneeLeft"), ks.GetIndex("AnkleLeft"), ms.GetIndex("KneeLeft"), ms.GetIndex("AnkleLeft")));
                 _skeletonMapperA.BoneMappers.Add(new ChainBoneMapper(ks.GetIndex("KneeRight"), ks.GetIndex("AnkleRight"), ms.GetIndex("KneeRight"), ms.GetIndex("AnkleRight")));
                 _skeletonMapperA.BoneMappers.Add(new ChainBoneMapper(ks.GetIndex("ShoulderCenter"), ks.GetIndex("Head"), ms.GetIndex("ShoulderCenter"), ms.GetIndex("Head")));

                 // We could also try to map the hand bones - but the Kinect input for the hands jitters a lot. 
                 // It looks better if we do not animate the hands.
                 _skeletonMapperA.BoneMappers.Add(new ChainBoneMapper(ks.GetIndex("WristLeft"), ks.GetIndex("HandLeft"), ms.GetIndex("WristLeft"), ms.GetIndex("HandLeft")));
                 _skeletonMapperA.BoneMappers.Add(new ChainBoneMapper(ks.GetIndex("WristRight"), ks.GetIndex("HandRight"), ms.GetIndex("WristRight"), ms.GetIndex("HandRight")));


        }


        public override void Update(GameTime gameTime)
        {

            switch (st.currentGameState)
            {
                case "Game":
                case "Learning":
                    float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
                    // Map the _kinectSkeletonPoses of tracked players to the _modelSkeletonPoses.
                    if (KinectWrapper.IsTrackedA)
                    {
                        _skeletonMapperA.MapAToB();
                        _filterA.Update(deltaTime);
                        UpdateDisplayMessage();
                    }
                    if (InputService.IsPressed(Keys.D1, false))
                    {
                        _drawModelSkeletons = !_drawModelSkeletons;
                        UpdateDisplayMessage();
                    }
                    // <2> --> Increase filter strength.
                    if (InputService.IsDown(Keys.D2))
                    {
                        _filterA.TimeConstant += 0.05f * deltaTime;
                        UpdateDisplayMessage();
                    }
                    // <3> --> Decrease filter strength.
                    if (InputService.IsDown(Keys.D3))
                    {
                        _filterA.TimeConstant = Math.Max(0, _filterA.TimeConstant - 0.05f * deltaTime);
                        UpdateDisplayMessage();
                    }
                    base.Update(gameTime);
                    break;
                case "Menu":
                    UpdateDisplayMessage();
                    break;
                case "Loading":
                    UpdateDisplayMessage();
                    break;

            }



           
        }

        private void UpdateDisplayMessage()
        {
            switch (st.currentGameState)
            {
                case "Game":
                    DisplayMessage = "Skeleton Mapping On"
                    + "\nPress <1> to toggle drawing of the model skeletons (green): " + _drawModelSkeletons
                    + "\nPress <2>/<3> to increase/decrease the filter strength: " + _filterA.TimeConstant;
                    break;
                case "Learning":
                    DisplayMessage = "Learning Window On"
                        + "\nPress <2>/<3> to increase/decrease the filter strength: " + _filterA.TimeConstant
                        +" \n Learning a new pose it takes about 30 frames. Aproximatively 1 second.";
                    break;
                case "Loading":
                    DisplayMessage = String.Empty;
                    break;
                case "Menu":
                    DisplayMessage = String.Empty;
                    break;
            }

        }

        protected override void OnDrawSample(GameTime gameTime)
        {
            // Draw models of tracked players.
            switch (st.currentGameState)
            {
                case "Game":
                case "Learning":
                    if (KinectWrapper.IsTrackedA)
                        DrawSkinnedModel(_modelA, Pose.Identity, _skeletonPoseA);

                    // Draw model skeletons of tracked players for debugging.
                    if (_drawModelSkeletons)
                    {
                        if (KinectWrapper.IsTrackedA)
                            _skeletonPoseA.DrawBones(GraphicsDevice, BasicEffect, 0.1f, SpriteBatch, SpriteFont, Color.GreenYellow);
                    }

                    base.OnDrawSample(gameTime);
                    break;
                case "Loading":
                    break;
                case "Menu":
                    break;
            }

        }




    }
    }
     