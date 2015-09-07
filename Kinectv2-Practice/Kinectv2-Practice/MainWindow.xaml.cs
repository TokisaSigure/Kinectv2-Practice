using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
/*取りあえず色々なサイトを参考に名前空間の読み込み*/
using System.ComponentModel;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
/*ここまで*/

namespace Kinectv2_Practice
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>

    public partial class MainWindow : Window
    {
        /*キネクトに必要な変数群の宣言*/
        KinectSensor kinect;
        BodyFrameReader bodyFrameReader;//現状死に体
        ColorFrameReader colorFrameReader = null;
        ColorImageFormat colorImageFormat;
        /// <summary>
        /// フレームの説明情報を保有、つまりフレームの情報を格納するための場所じゃないかと
        /// </summary>
        FrameDescription colorFrameDescription;
        Body[] bodies;
        /*宣言終わり*/

        /// <summary>
        /// 起動したときに一回だけ自動実行されるよ！（念のため）
        /// </summary>
        /// 

        Class.BoneUtil boneUtil = new Class.BoneUtil();

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                ///キネクト本体の接続を確保、たしか接続されてない場合はfalseとかになった記憶
                this.kinect = KinectSensor.GetDefault();
                ///読み込む画像のフォーマット(rgbとか)を指定、どうやって読み込むかのリーダの設定も
                this.colorImageFormat = ColorImageFormat.Bgra;
                this.colorFrameDescription = this.kinect.ColorFrameSource.CreateFrameDescription(this.colorImageFormat);
                this.colorFrameReader = this.kinect.ColorFrameSource.OpenReader();
                this.colorFrameReader.FrameArrived += colorFrameReader_FrameArrived;
                this.kinect.Open();//キネクト起動！！
                if (!kinect.IsOpen)
                {
                    this.errorLog.Visibility = Visibility.Visible;
                    this.errorLog.Content = "キネクトが見つからないよ！残念！";
                    throw new Exception("キネクトが見つかりませんでした！！！");
                }
                ///bodyを格納するための配列作成
                bodies = new Body[kinect.BodyFrameSource.BodyCount];

                ///ボディリーダーを開く
                bodyFrameReader = kinect.BodyFrameSource.OpenReader();
                bodyFrameReader.FrameArrived += bodyFrameReader_FrameArrived;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Close();
            }


        }

        void bodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (var bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame == null)
                {
                    return;
                }
                else
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }

                if (dataReceived)
                {
                    foreach (Body body in this.bodies)
                    {
                        if (body.IsTracked)
                        {
                            this.textLabel.Content = body.IsTracked.ToString();
                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;
                            // convert the joint points to depth (display) space
                            // ジョイントポイントを深度（ディスプレイ）スペースに変換
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // 時々推測されるjointの深さが負として表示される場合があります
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                // 0.1フレーム前のcoordinatemapperに戻り、チェックする(負数の防止？)
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = boneUtil.InferredZPositionClamp;
                                }
                            }
                        }
                    }

                    // ボディデータを取得する
                    bodyFrame.GetAndRefreshBodyData(bodies);
                    //認識しているBodyに対して
                    foreach (var body in bodies.Where(b => b.IsTracked))
                    {
                        //左手のX座標を取得
                        System.Diagnostics.Debug.WriteLine("X=" + body.Joints[JointType.HandLeft].Position.X);
                        if(body.HandRightState == HandState.Closed)
                        {
                            this.textLabel.Content = "グー";
                        }
                        if(body.HandRightState == HandState.Open)
                        {
                            this.textLabel.Content = "パー";
                        }
                        if(body.HandRightState == HandState.Lasso)
                        {
                            this.textLabel.Content = "チョキ";
                        }
                    }
                }
            }
        }
        

        private void colorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            ColorFrame colorFrame = e.FrameReference.AcquireFrame();

            if (colorFrame == null)
                return;
            /*以下はカラーデータの取得等の処理が入る・・・らしい*/
            byte[] colors = new byte[colorFrameDescription.Width * colorFrameDescription.Height * colorFrameDescription.BytesPerPixel];
            colorFrame.CopyConvertedFrameDataToArray(colors, this.colorImageFormat);
            /*ここの処理が複雑なので解説、(画像の横幅指定,画像の縦幅指定,dpi...?,dpi?,フォーマット,(たしか)エフェクト,画素データのバッファ量指定,ストライド(横一列)のbyte数指定*/
            BitmapSource bitmapSource = BitmapSource.Create(colorFrameDescription.Width, colorFrameDescription.Height, 96, 96, PixelFormats.Bgr32, null, colors, colorFrameDescription.Width * (int)colorFrameDescription.BytesPerPixel);
            //キャンバスに表示する。
            this.canvas.Background = new ImageBrush(bitmapSource);
            /*ここまで*/
            colorFrame.Dispose();//データの破棄、メモリの解放
        }
    }
}
