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
    class Poses : DrawableGameComponent
    {
        protected int nr_frame;

        KinectSkeleton[] kinect_skeletons;
        bool capture;
      

        private KinectWrapper _kinectWrapper { get; set; }

        #region Pozitii_invatate
        public String nume_pozitie;
        public Dictionary<int, double[]> dictionar_pozitii = new Dictionary<int,double[]>();
        private double sum_angles;
        private double y_axis_angle;
        private double xz_projection_angle;
        private double minimum_value;
      
        #endregion

        public Poses(Game game)
          : base(game)
        {
            y_axis_angle = 0;
            xz_projection_angle = 0;
            for(int i=0;i<10;i++)
            {
                dictionar_pozitii.Add(i,new double[3]);
            }
           
        }



        public void y_ax_angle()
        {
            y_axis_angle = 0;
            for (int i = 0; i < 9; i++)
            {
                double R;
                R = Math.Sqrt(Math.Pow(this.dictionar_pozitii[i][0], 2) + Math.Pow(this.dictionar_pozitii[i][1], 2) + Math.Pow(this.dictionar_pozitii[i][2], 2));
                y_axis_angle = y_axis_angle + Math.Abs(Math.Acos(this.dictionar_pozitii[i][1] / R));
            }
        }
        public void az_proj_angle()
        {
            Vector3D b = new Vector3D(0, 1, 0);
            Vector3D a;
            xz_projection_angle = 0;
            for (int i = 0; i < 9; i++)
            {
                a = new Vector3D(this.dictionar_pozitii[i][0], this.dictionar_pozitii[i][1], this.dictionar_pozitii[i][2]);
                double R;
                Vector3D projection;
                projection = a - Vector3D.Dot(a, b) * b;
                R = Math.Sqrt(Math.Pow(projection.X, 2) + Math.Pow(projection.Y, 2) + Math.Pow(projection.Z, 2));
                xz_projection_angle = xz_projection_angle + Math.Abs(Math.Acos(a.X / R));
            }
        }

        public double get_y_axis_Angles()
        {
           return this.y_axis_angle;
        }
        public double get_proj_xz_Angles()
        {
           return this.xz_projection_angle;
        }

        public void sum_angle()
        {
            sum_angles = 0;
           this.sum_angles=this.y_axis_angle + this.xz_projection_angle;
        }
        public double get_sum_angle()
        {
            return this.sum_angles;
        }

        public void set_min_value(double a)
        {
            this.minimum_value = 0;
            this.minimum_value = a;
        }
        public double get_min_value()
        {
            return this.minimum_value;
        }

        public void set_min_value(Poses a)
        {
            this.minimum_value = 0;
            for(int i=0;i<9;i++)
            {
                double angle_test;
                double angle_DBposture;
                angle_test = single_y_axe_angle(a, i) + single_xz_axe_angle(a, i);
                angle_DBposture = single_xz_axe_angle(this, i) + single_y_axe_angle(this, i);
                minimum_value = minimum_value + Math.Exp(Math.Abs(angle_DBposture - angle_test));
            }

        }
        private double single_y_axe_angle(Poses a,int i)
        {
            y_axis_angle = 0;
            double R;
            R = Math.Sqrt(Math.Pow(a.dictionar_pozitii[i][0], 2) + Math.Pow(a.dictionar_pozitii[i][1], 2) + Math.Pow(a .dictionar_pozitii[i][2], 2));
            y_axis_angle = y_axis_angle + Math.Abs(Math.Acos(a.dictionar_pozitii[i][1] / R));
            return y_axis_angle;

        }

        private double single_xz_axe_angle(Poses c,int i)
        {
            Vector3D b = new Vector3D(0, 1, 0);
            Vector3D a;
            xz_projection_angle = 0;
        
                a = new Vector3D(c.dictionar_pozitii[i][0], c.dictionar_pozitii[i][1], c.dictionar_pozitii[i][2]);
                double R;
                Vector3D projection;
                projection = a - Vector3D.Dot(a, b) * b;
                R = Math.Sqrt(Math.Pow(projection.X, 2) + Math.Pow(projection.Y, 2) + Math.Pow(projection.Z, 2));
                xz_projection_angle = xz_projection_angle + Math.Abs(Math.Acos(a.X / R));
            return xz_projection_angle;
            
        }

        public double apply_correction(KinectSkeleton skeleton)
        {
            double theta=0;
            double R;
            Vector3D vector_shoulder = new Vector3D(skeleton.Joints[JointType.ShoulderCenter].Position.X - skeleton.Joints[JointType.ShoulderRight].Position.X, skeleton.Joints[JointType.ShoulderCenter].Position.Y - skeleton.Joints[JointType.ShoulderRight].Position.Y, skeleton.Joints[JointType.ShoulderCenter].Position.Z - skeleton.Joints[JointType.ShoulderRight].Position.Z);

            R = Math.Sqrt(Math.Pow(vector_shoulder.X, 2) + Math.Pow(vector_shoulder.Y, 2) + Math.Pow(vector_shoulder.Z, 2));

            theta = theta + Math.Abs(Math.Acos(vector_shoulder.X / R));

            if((skeleton.Joints[JointType.ShoulderCenter].Position.Z - skeleton.Joints[JointType.ShoulderRight].Position.Z)<0)
            {
                theta = theta * (-1);
            }

            /* for(int i=0;i<14;i++)
             {
                 double x_old = this.dictionar_pozitii[i][0];
                 double z_old = this.dictionar_pozitii[i][2];
                 this.dictionar_pozitii[i][0] = z_old * Math.Sin(theta) + x_old * Math.Cos(theta);
                 this.dictionar_pozitii[i][2] = z_old * Math.Cos(theta) - x_old * Math.Sin(theta);
             }*/
            return theta;
        }



    }
}

    

