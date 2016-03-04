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
using Microsoft.Kinect;
using DigitalRune.Animation;
using DigitalRune.Game.Input;
using DigitalRune.Game.UI;
using DigitalRune.Game.States;
using DigitalRune.Game.UI.Controls;
using DigitalRune.Mathematics.Algebra;
using KinectSkeleton = Microsoft.Kinect.Skeleton;

namespace KinectGame.AppCore
{
    class Gestures
    {

        public String nume_gest;
        private List<Poses> gesture;

        public Gestures(/*Game game*/)
          /*: base(game)*/
        {
            gesture = new List<Poses>();
        }


        public void add_pose_to_gesture(Poses pose)
        {
            gesture.Add(pose);
        }

        public List<Poses> get_gesture()
        {
            return gesture;
        }

    }
}
