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
using System.Timers;
using System.Text;
using System.Threading;


using DigitalRune.Animation;
using DigitalRune.Game.Input;
using DigitalRune.Game.UI;
using DigitalRune.Game.States;
using DigitalRune.Game.UI.Controls;
using DigitalRune.Game.UI.Rendering;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Game;

using KinectSkeleton = Microsoft.Kinect.Skeleton;
using Excel = Microsoft.Office.Interop.Excel;
using DigitalRune.Graphics;
using Microsoft.Kinect;
using System.Collections;

namespace KinectGame.AppCore
{
    class DynamicTimeWarping : DrawableGameComponent
    {
        private KinectSensor kinectSensor;
        private KinectWrapper _kinectWrapper;
        protected SpriteBatch SpriteBatch { get; private set; }
        protected SpriteFont SpriteFont { get; private set; }
        private readonly int _noOfJoints;
        private readonly int _dataSetSize;
        private readonly double _positionThreshold;
        private readonly int _minimumLength;
        private readonly double _recognitionThreshold;
        private readonly int _maxSlope;

        private ArrayList _video;
        private const int BufferSize = 32;
        private const int MinimumFrames = 6;

        private Gestures[] _gestures;
        private Poses pose_to_be_analysed;
        private Poses[] poses;
        private static Skeleton[] _Frameskeleton;
        private String recognised_gesture;
        private JointType[,] segments = new JointType[2, 10];
        private int _flipFlop;
        private int frames_per_sec;

        public DynamicTimeWarping(Game game) : base(game)
        {
            _video = new ArrayList();
            _kinectWrapper = Game.Components.OfType<KinectWrapper>().First();
           
            this._positionThreshold = 0.9;
            this._minimumLength = 6;
            this._maxSlope = int.MaxValue;
            this._recognitionThreshold = 0.9;
            this.recognised_gesture = "UNDEFINED_GESTURE";
            //_flipFlop = 0;
            this.frames_per_sec = 0;

            segments[0, 0] = JointType.WristRight; segments[0, 5] = JointType.HipRight; segments[0, 8] = JointType.ShoulderCenter;
            segments[1, 0] = JointType.ElbowRight; segments[1, 5] = JointType.KneeRight; segments[1, 8] = JointType.ShoulderLeft;
            segments[0, 1] = JointType.WristLeft; segments[0, 6] = JointType.KneeLeft; segments[0, 9] = JointType.ShoulderCenter;
            segments[1, 1] = JointType.ElbowLeft; segments[1, 6] = JointType.AnkleLeft; segments[1, 9] = JointType.ShoulderRight;
            segments[0, 2] = JointType.ElbowRight; segments[0, 7] = JointType.KneeRight;
            segments[1, 2] = JointType.ShoulderRight; segments[1, 7] = JointType.AnkleRight;
            segments[0, 3] = JointType.ElbowLeft;
            segments[1, 3] = JointType.ShoulderLeft;
            // segments[0, 4] = JointType.Spine;           
            // segments[1, 4] = JointType.HipCenter;       
            // segments[0, 5] = JointType.HipCenter;       
            // segments[1, 5] = JointType.HipLeft;         
            // segments[0, 6] = JointType.HipCenter;
            // segments[1, 6] = JointType.HipRight;
            segments[0, 4] = JointType.HipLeft;
            segments[1, 4] = JointType.KneeLeft;
            Load_Gestures();
        }

        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            SpriteFont = Game.Content.Load<SpriteFont>("SpriteFont1");
            // LoadLearnedGestures();
        }

        public override void Draw(GameTime gameTime)
        {
            var position = new Vector2(150, 350);
            var position2 = new Vector2(150, 150);

            SpriteBatch.Begin();

            Rectangle titleSafeRectangle = GraphicsDevice.Viewport.TitleSafeArea;

            SpriteBatch.DrawString(SpriteFont,_video.Count.ToString() , position2, Color.Red, 0, new Vector2(10, 10), 3, SpriteEffects.None, 1);
          //  SpriteBatch.DrawString(SpriteFont, recognised_gesture, position, Color.Red, 0, new Vector2(10, 10), 3, SpriteEffects.None, 1);

            SpriteBatch.End();

            //    check_for_Gesture();

            // check_for_pose();

            base.Draw(gameTime);
        }

        public string Recognize(ArrayList video)
        {
            double minDist = double.PositiveInfinity;
            recognised_gesture = "UNDEFINED_GESTURE";
            List<Poses> _video = new List<Poses>();
            List<Poses> _gesture = new List<Poses>();
            foreach (Poses instance in video)
            {
                _video.Add(instance);
            }
            
            foreach(Gestures gesture in _gestures)
            {
                _gesture.Clear();

                int lastGesturePosition = gesture.get_gesture().Count - 1;
                int lastVideoPosition = _video.Count - 1;

                for(int i=0;i<10;i++)
                {
                    if (DistanceCalc(_video, lastVideoPosition, gesture.get_gesture(), lastGesturePosition) < _positionThreshold)
                    {
                        double d = DTW(_video,gesture.get_gesture()) / gesture.get_gesture().Count;
                        if (d < minDist)
                        {
                            //Mark the gesture this is most simiilar to. 
                            minDist = d;
                            recognised_gesture = gesture.nume_gest;
                        }
                    }
                }

            }

            return minDist < _recognitionThreshold ? recognised_gesture : "UNDEFINED_GESTURE";
        }



        private double DTW(List<Poses> _video,List<Poses> _gesture)
        {
            var inputVideoIterator = _video.GetEnumerator();
            inputVideoIterator.MoveNext();
            int videoLength = _video.Count;

            var gestureIterator = _gesture.GetEnumerator();
            gestureIterator.MoveNext();
            int gestureLength = _gesture.Count;

            var tab = new double[videoLength + 1, gestureLength + 1];
            var horizStepsMoved = new int[videoLength + 1, gestureLength + 1];
            var vertStepsMoved = new int[videoLength + 1, gestureLength + 1];

            for(int i=0;i<videoLength+1;i++)
            {
                for(int j=0;j<gestureLength+1;j++)
                {
                    tab[i, j] = double.PositiveInfinity;
                    horizStepsMoved[i, j] = 0;
                    vertStepsMoved[i, j] = 0;
                }
            }
            tab[videoLength, gestureLength] = 0;
            for (int i = videoLength - 1; i > -1; i--)
            {
                for (int j = gestureLength - 1; j > -1; j--)
                {
                    if (tab[i, j + 1] < tab[i + 1, j + 1] && tab[i, j + 1] < tab[i + 1, j] && horizStepsMoved[i, j + 1] < _maxSlope)
                    {
                        tab[i, j] = DistanceCalc(_video, i, _gesture, j) + tab[i, j + 1];
                        horizStepsMoved[i, j] = horizStepsMoved[i, j + 1] + 1;
                        vertStepsMoved[i, j] = vertStepsMoved[i, j + 1];
                    }
                    else if (tab[i + 1, j] < tab[i + 1, j + 1] && tab[i + 1, j] < tab[i, j + 1] &&
                             vertStepsMoved[i + 1, j] < _maxSlope)
                    {
                        tab[i, j] = DistanceCalc(_video, i, _gesture, j) + tab[i + 1, j];
                        horizStepsMoved[i, j] = horizStepsMoved[i + 1, j];
                        vertStepsMoved[i, j] = vertStepsMoved[i + 1, j] + 1;
                    }

                    else
                    {
                        //Move diagonally down-right
                        if (tab[i + 1, j + 1] == double.PositiveInfinity)
                        {
                            tab[i, j] = double.PositiveInfinity;
                        }
                        else
                        {
                            tab[i, j] = DistanceCalc(_video, i, _gesture, j) + tab[i + 1, j + 1];
                        }

                        horizStepsMoved[i, j] = 0;
                        vertStepsMoved[i, j] = 0;

                    }
                }
            }
            double bestMatch = double.PositiveInfinity;

            for (int i = 0; i < videoLength; ++i)
            {
                if (tab[i, 0] < bestMatch)
                {
                    bestMatch = tab[i, 0];
                }
            }
            return bestMatch;

        }



        private double DistanceCalc(List<Poses> input,int indexInput,List<Poses> gesture,int indexgesture)
        {
            double d = 0;
            for(int i = 0; i < 10; i++)
            {
                var v1 = new Vector3((float)input[indexInput].dictionar_pozitii[i][0], (float)input[indexInput].dictionar_pozitii[i][1], (float)input[indexInput].dictionar_pozitii[i][2]);
                var v2 = new Vector3((float)gesture[indexgesture].dictionar_pozitii[i][0], (float)gesture[indexgesture].dictionar_pozitii[i][1], (float)gesture[indexgesture].dictionar_pozitii[i][2]);

                d += (Vector3.DistanceSquared(v1,v2));
            }

            return Math.Sqrt(d);

        }

       




        public void StartRecognition()
        {
            kinectSensor = _kinectWrapper.get_sensor();
            kinectSensor.SkeletonFrameReady += KinectSensor_SkeletonFrameReady;
            _Frameskeleton = new Skeleton[kinectSensor.SkeletonStream.FrameSkeletonArrayLength];
        }
        public void StopRecognition()
        {
            kinectSensor.SkeletonFrameReady -= KinectSensor_SkeletonFrameReady;
        }

        private void KinectSensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletonFrame.CopySkeletonDataTo(_Frameskeleton);
                    Skeleton data = (from s in _Frameskeleton
                                     where s.TrackingState == SkeletonTrackingState.Tracked
                                     select s).FirstOrDefault();
                    ProcessData(data);
                }
            }
        }

        private void ProcessData(Skeleton skeleton)
        {
            frames_per_sec++;
            if (frames_per_sec == 10)
            {
                pose_to_be_analysed = new Poses(Game);

                
                int a;
                var center = new Vector3D((skeleton.Joints[JointType.ShoulderLeft].Position.X + skeleton.Joints[JointType.ShoulderRight].Position.X) / 2, (skeleton.Joints[JointType.ShoulderLeft].Position.Y + skeleton.Joints[JointType.ShoulderRight].Position.Y) / 2, (skeleton.Joints[JointType.ShoulderLeft].Position.Z + skeleton.Joints[JointType.ShoulderRight].Position.Z) / 2);
                double shoulderDist =
                    Math.Sqrt(Math.Pow((skeleton.Joints[JointType.ShoulderLeft].Position.X - skeleton.Joints[JointType.ShoulderRight].Position.X), 2) +
                              Math.Pow((skeleton.Joints[JointType.ShoulderLeft].Position.Y - skeleton.Joints[JointType.ShoulderRight].Position.Y), 2) +
                              Math.Pow((skeleton.Joints[JointType.ShoulderLeft].Position.Z - skeleton.Joints[JointType.ShoulderRight].Position.Z), 2));
                #region calcul_segmente 
                for (int i = 0; i < 9; i++)
                {
                    pose_to_be_analysed.dictionar_pozitii[i][0] = (skeleton.Joints[segments[0, i]].Position.X - center.X) / shoulderDist - (skeleton.Joints[segments[1, i]].Position.X - center.X) / shoulderDist;
                    pose_to_be_analysed.dictionar_pozitii[i][1] = (skeleton.Joints[segments[0, i]].Position.Y - center.Y) / shoulderDist - (skeleton.Joints[segments[1, i]].Position.Y - center.Y) / shoulderDist;
                    pose_to_be_analysed.dictionar_pozitii[i][2] = (skeleton.Joints[segments[0, i]].Position.Z - center.Z) / shoulderDist - (skeleton.Joints[segments[1, i]].Position.Z - center.Z) / shoulderDist;
                }
                #endregion
                Pose_Ready_for_Buffer(pose_to_be_analysed);
                frames_per_sec = 0;
            }

        }

        private void Pose_Ready_for_Buffer(Poses pose)
        {

            if (_video.Count > MinimumFrames)
            {
                recognised_gesture = Recognize(_video);
                if (!recognised_gesture.Contains("UNDEFINED_GESTURE"))
                {
                    _video = new ArrayList();
                }
            }
            if (_video.Count >= BufferSize)
            {
                _video.RemoveRange(0, _video.Count - 1);
                //_video.RemoveAt(0);
            }


            int l = 0;
                for(int j=0;j<10;j++)
                {
                    if ((pose.dictionar_pozitii[j][0] != 0 && pose.dictionar_pozitii[j][1] != 0 && pose.dictionar_pozitii[j][2] != 0)&&
                    (!double.IsNaN(pose.dictionar_pozitii[j][0]+ pose.dictionar_pozitii[j][1]+ pose.dictionar_pozitii[j][2])))
                    {
                        l++;
                    }
                    else
                    {
                        break;
                    }
                }

                if(l>=8)
            {
                    _video.Add(pose);
            }
        }



        public void Load_Gestures()
        {
            Microsoft.Office.Interop.Excel.Application xlApp;
            xlApp = new Microsoft.Office.Interop.Excel.Application();
            int nr_gesturi;
            Excel.Workbook xlWorkBook;
            if (xlApp == null)
            {
                //afisare eroare pe ecran
                return;
            }
            if (!System.IO.Directory.GetFiles("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent", "gestures.xls", System.IO.SearchOption.AllDirectories).Any())
            {
                return;
            }
            else
            {
                xlWorkBook = xlApp.Workbooks.Open("D:\\College\\anul 4\\Licenta\\KinectGame\\KinectGame\\KinectGameContent\\gestures.xls", 0, false, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
                nr_gesturi = xlWorkBook.Worksheets.Count;
                _gestures = new Gestures[nr_gesturi];
                for (int i = 0; i < nr_gesturi; i++)
                {
                    _gestures[i] = new Gestures();
                }

                int j = 0;
                foreach (Excel.Worksheet xlWork in xlWorkBook.Worksheets)
                {
                    int number_of_rows = 0;
                    int p = 0;
                    _gestures[j].nume_gest = xlWork.Name.ToString();
                    do
                    {
                        p++;
                        if (Convert.ToString(xlWork.Cells[p, 1].Value) == "END")
                        {
                            break;
                        }
                    } while (Convert.ToString(xlWork.Cells[p, 1].Value) != "END");

                  
                    int number_of_poses = p / 10;
                    poses = new Poses[number_of_poses];

                    for (int i = 0; i < number_of_poses; i++)
                    {
                        poses[i] = new Poses(Game);
                    }

                    int offset = 0;
                    for (int k = 0; k < number_of_poses; k++)
                    {
                        for (int i = 1; i <= 10; i++)
                        {
                            offset += 1;
                            for (int c = 2; c < 5; c++)
                            {
                                poses[k].dictionar_pozitii[i - 1][c - 2] = (float)Convert.ToDouble(xlWork.Cells[offset, c].Value);
                            }

                        }
                        _gestures[j].add_pose_to_gesture(poses[k]);
                    }
                    j += 1;
                    offset = 0;


                }
            }
            xlApp.Quit();
            releaseObject(xlApp);
            releaseObject(xlWorkBook);
        }

        public void Unload_Gestures()
        {
   
        }

        private void releaseObject(object obj)
        {
            try
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
                obj = null;
            }
            catch (Exception ex)
            {
                obj = null;
                //eroare
            }
            finally
            {
                GC.Collect();
            }
        }

    }
}
