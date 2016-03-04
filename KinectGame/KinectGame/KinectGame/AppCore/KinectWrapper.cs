using System;
using DigitalRune.Animation.Character;
using DigitalRune.Mathematics.Algebra;
using Microsoft.Kinect;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using DRSkeleton = DigitalRune.Animation.Character.Skeleton;
using KinectSkeleton = Microsoft.Kinect.Skeleton;
using MathHelper = DigitalRune.Mathematics.MathHelper;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;


namespace KinectGame.AppCore
{
    public class KinectWrapper : GameComponent
    {

        #region Fields
        Game2 Sgame;
        Color[] sLatestColorData;
        Texture2D sColorImage;
        private String command;
        // The Kinect device.
        private KinectSensor _kinect;
        private SpeechRecognitionEngine speechRecognizer;

        private static RecognizerInfo GetKinectRecognizer()
        {
            Func<RecognizerInfo, bool> matchingFunc = r =>
            {
                string value;
                r.AdditionalInfo.TryGetValue("Kinect", out value);
                return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase) && "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
            };
            return SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();
        }

        // A buffer for Kinect skeleton data (see OnSkeletonFrameReady());
        private KinectSkeleton[] _kinectSkeletons;
        private Dictionary<int, Vector3F> pozitii = new Dictionary<int, Vector3F>();
        KinectSkeleton skeletonData_For_Learning;

        //  If a player is not tracked, the ID is 0.
        private int _trackingIdA;

        protected States st { get; private set; }
        #endregion 

        public bool IsRunning
        {
            get { return _kinect != null; }
        }
        public bool IsTrackedA
        {
            get { return _trackingIdA > 0; }
        }

        public SkeletonPose SkeletonPoseA { get; private set; } //kinect data for user 
        public Vector3F Offset { get; set; }
        public Vector3F Scale { get; set; }
        
        


        public KinectWrapper(Game game) : base(game)
        {
            Offset = new Vector3F(0, 0, 0);
            Scale = new Vector3F(1f, 1f, 1f);//initial 0.5
            InitializeSkeletonPoses();
            st = Game.Components.OfType<States>().First();
        }

        private void InitializeKinect()
        {
            // Wait until a Kinect sensor is connected and ready.
            if (KinectSensor.KinectSensors.Count == 0 || KinectSensor.KinectSensors[0].Status != KinectStatus.Connected)
                return;

            // Start Kinect.
            _kinect = KinectSensor.KinectSensors[0];
            this.command = "nocommand";

            _kinect.SkeletonStream.Enable(new TransformSmoothParameters
            {
                Correction = 0.5f,
                JitterRadius = 0.05f,
                MaxDeviationRadius = 0.04f,
                Prediction = 0.5f,
                Smoothing = 0.9f,
            });
           
         //   _kinect.SkeletonStream.Enable();
            _kinect.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            speechRecognizer = CreateSpeechRecognizer();
            _kinect.Start();
            Start();
            _kinect.SkeletonFrameReady += OnSkeletonFrameReady;
            _kinect.ColorFrameReady += OnColorFrameReady;
        }

        private SpeechRecognitionEngine CreateSpeechRecognizer()
        {
            //set recognizer info
            RecognizerInfo ri = GetKinectRecognizer();
            //create instance of SRE
            SpeechRecognitionEngine sre;
            sre = new SpeechRecognitionEngine(ri.Id);

            //Now we need to add the words we want our program to recognise
            var grammar = new Choices();
            grammar.Add("delete");
            grammar.Add("save");

            //set culture - language, country/region
            var gb = new GrammarBuilder { Culture = ri.Culture };
            gb.Append(grammar);

            //set up the grammar builder
            var g = new Grammar(gb);
            sre.LoadGrammar(g);

            //Set events for recognizing, hypothesising and rejecting speech
            sre.SpeechRecognized += SreSpeechRecognized;
           // sre.SpeechHypothesized += SreSpeechHypothesized;
            sre.SpeechRecognitionRejected += SreSpeechRecognitionRejected;
            return sre;
        }
        private void RejectSpeech(RecognitionResult result)
        {
            command = "nocommand";
        }
        private void SreSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            RejectSpeech(e.Result);
        }
        private void SreSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            //Very important! - change this value to adjust accuracy - the higher the value
            //the more accurate it will have to be, lower it if it is not recognizing you
            if (e.Result.Confidence < .2)
            {
                RejectSpeech(e.Result);
            }
            //and finally, here we set what we want to happen when 
            //the SRE recognizes a word
            switch (e.Result.Text.ToUpperInvariant())
            {
                case "DELETE":
                    command = "D";
                    break;
                case "SAVE":
                    command = "S";
                    break;
                default:
                    break;
            }
        }

        public void StopAudio()
        {
            var audioSource = _kinect.AudioSource;
            audioSource.Stop();
        }

        public void Start()
        {
            //set sensor audio source to variable
            var audioSource = _kinect.AudioSource;
            //Set the beam angle mode - the direction the audio beam is pointing
            //we want it to be set to adaptive
          
            //start the audiosource 
            var kinectStream = audioSource.Start();
            //configure incoming audio stream
            speechRecognizer.SetInputToAudioStream(
                kinectStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            //make sure the recognizer does not stop after completing     
            speechRecognizer.RecognizeAsync(RecognizeMode.Multiple);
            //reduce background and ambient noise for better accuracy
            _kinect.AudioSource.EchoCancellationMode = EchoCancellationMode.None;
            _kinect.AudioSource.AutomaticGainControlEnabled = false;
            
        }

        public String get_command()
        {
            return this.command;
        }

        public void set_command(String A)
        {
            this.command = A;
        }


        private void InitializeSkeletonPoses()
        {
            // Create a list of the bone/joint names of a Kinect skeleton.
            int numberOfJoints = Enum.GetNames(typeof(JointType)).Length;
            var boneNames = new string[numberOfJoints];
            for (int i = 0; i < numberOfJoints; i++)
                boneNames[i] = ((JointType)i).ToString();

            // Create list with one entry per bone. Each entry is the index of the parent bone.
            var boneParents = new[]
            {
        -1,
        (int)JointType.HipCenter,
        (int)JointType.Spine,
        (int)JointType.ShoulderCenter,
        (int)JointType.ShoulderCenter,
        (int)JointType.ShoulderLeft,
        (int)JointType.ElbowLeft,
        (int)JointType.WristLeft,
        (int)JointType.ShoulderCenter,
        (int)JointType.ShoulderRight,
        (int)JointType.ElbowRight,
        (int)JointType.WristRight,
        (int)JointType.HipCenter,
        (int)JointType.HipLeft,
        (int)JointType.KneeLeft,
        (int)JointType.AnkleLeft,
        (int)JointType.HipCenter,
        (int)JointType.HipRight,
        (int)JointType.KneeRight,
        (int)JointType.AnkleRight,
      };

            var boneBindPoses = new SrtTransform[numberOfJoints];
            for (int i = 0; i < numberOfJoints; i++)
                boneBindPoses[i] = SrtTransform.Identity;

            var skeleton = new DRSkeleton(boneParents, boneNames, boneBindPoses);
            SkeletonPoseA = SkeletonPose.Create(skeleton);
          
            

         /*   for (int i = 0; i < 21; i++)
            {
                pozitii.Add(i, Vector3F.Zero);
            }*/
            
        }

        private void OnColorFrameReady(object sender,ColorImageFrameReadyEventArgs eventArgs)
        {
            ColorImageFrame frame = eventArgs.OpenColorImageFrame();
            if(frame==null)
            {
                return;
            }
            byte[] pixelData = new byte[frame.PixelDataLength];
            frame.CopyPixelDataTo(pixelData);
            sLatestColorData = new Color[pixelData.Length / 4]; //because there are 4 bytes per pixel
            int offset = 0;
            for(int i = 0; i < sLatestColorData.Length; i++)
            {
                sLatestColorData[i] = new Color(pixelData[offset + 2], pixelData[offset + 1], pixelData[offset]);
                offset = offset + 4;
            }
            frame.Dispose();
        }

        public void DrawColorImage(SpriteBatch batch, GraphicsDevice device, Rectangle bounds)
        {
            switch (st.currentGameState)
            {
                case "Game":
                case "Learning":
                    if(st.drawColorBox==true)
                    {
                        if (sLatestColorData == null)
                        {
                            return;
                        }
                        sColorImage = new Texture2D(device, 640, 480);
                        sColorImage.SetData<Color>(sLatestColorData);
                        batch.Draw(sColorImage, bounds, Color.White);
                    }
                    break;
                case "Loading":
                    break;
                case "Menu":
                    break;
            }
           

        }

        private void OnSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs eventArgs)
        {
            using (var skeletonFrame = eventArgs.OpenSkeletonFrame())
            {
                if (skeletonFrame == null)
                    return;

                if (_kinectSkeletons == null || _kinectSkeletons.Length != skeletonFrame.SkeletonArrayLength)
                    _kinectSkeletons = new KinectSkeleton[skeletonFrame.SkeletonArrayLength];

                skeletonFrame.CopySkeletonDataTo(_kinectSkeletons);
            }

            KinectSkeleton skeletonDataA = null;
            foreach (var skeleton in _kinectSkeletons)
            {
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                {
                    if (skeletonDataA == null)
                    {
                        skeletonDataA = skeleton;
                    }
                 
                }
            }
            _trackingIdA = (skeletonDataA != null) ? skeletonDataA.TrackingId : 0;
            skeletonData_For_Learning = skeletonDataA;

            UpdateKinectSkeletonPose(skeletonDataA, SkeletonPoseA);

       
            

        }

        private void UpdateKinectSkeletonPose(KinectSkeleton skeletonData, SkeletonPose skeletonPose)
        {
            if (skeletonData == null)
                return;

            // Update the skeleton pose using the data from Kinect. 
            for (int i = 0; i < skeletonPose.Skeleton.NumberOfBones; i++)
            {
                var joint = (JointType)i;
                if (skeletonData.Joints[joint].TrackingState != JointTrackingState.NotTracked)
                {
                    // The joint position in "Kinect space".
                    SkeletonPoint kinectPosition = skeletonData.Joints[joint].Position;

                    // Convert Kinect joint position to a Vector3F.
                    // z is negated because in XNA the camera forward vectors is -z, but the Kinect
                    // forward vector is +z. 
                    Vector3F position = new Vector3F(-kinectPosition.X, kinectPosition.Y, -kinectPosition.Z);

                    // Apply scale and offset.
                    position = position * Scale + Offset;

                 //   pozitii[i] = position;

                    skeletonPose.SetBonePoseAbsolute(i, new SrtTransform(QuaternionF.Identity, position));
                }
            }
        }

        public KinectSensor get_sensor()
        {
            return this._kinect;
        }

        public KinectSkeleton getSkeletons()
        {


            return skeletonData_For_Learning;
        }

        protected override void Dispose(bool disposing)
        {
            // Clean up.
            if (_kinect != null)
            {
                _kinect.SkeletonFrameReady -= OnSkeletonFrameReady;
                _kinect.ColorFrameReady -= OnColorFrameReady;
                _kinect.Stop();
                _kinect = null;
            }

            base.Dispose(disposing);
        }

        public override void Update(GameTime gameTime)
        {
            // Kinect was not found yet. (Re-)Try initialization.
            if (_kinect == null)
                InitializeKinect();

            base.Update(gameTime);
        }
    }
}