using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.IO.Ports;

namespace ArcSoft_Face_Demo_X64
{
    public partial class ArcSoft_Face_Demo_X64 : Form
    {
        private VideoCapture capture;
        bool runflag = false;
        bool addflag = false;
        bool checkflag = false;

        int currentfacenum = -1;
        float changecheckvalue = 0.0f;
        string filepath = "facelibrary.json";

        public struct FaceLibrary               //人脸库
        {
            public string face_name;
            public byte[] face_Feature;
        }


        public struct DetectFaceFeature   //人脸检测结果
        {
            public bool faceflag;
            public byte[] face_Feature;
        }

        DetectFaceFeature detectfacefeature;
        FaceLibrary facelibrary;
        List<FaceLibrary> listface;


        /* 委托的调用方法  */
        public delegate void Refresh(float value, string name);    //定义委托         
        Refresh refresh;                                           //定义委托变量 
        //refresh = new  Refresh(refreshprogressbar);        //绑定
        //Invoke(refresh, value, name);                //执行操作
        private void refreshprogressbar(float value,string name)              //绑定操作
        {
            progressBar1.Value = (Int32)value;
            label6.Text = value + "%";
            label7.Text = name;
        }


        /* 委托的调用方法  */
        public delegate void RefreshText(TextBox textbox,  string str);  //定义委托         
        RefreshText refreshtext;                       //定义委托变量 
        private void refreshTextbox(TextBox textbox, string str)        //绑定操作
        {
            //textBox1.Text = str;
            textbox.Text = str;
        }

        public ArcSoft_Face_Demo_X64()
        {
            InitializeComponent();
        }

        private void ImageGrabbedProcess(object sender, EventArgs arg)
        {
            try
            {
                Mat mat = new Mat();
                DateTime now = DateTime.Now;
                capture.Retrieve(mat);   // 捕获摄像头图片
                Bitmap bitmap = mat.Bitmap;
                detectfacefeature = GetDetectFaceFeature(bitmap, pictureBox0, false);
                if (detectfacefeature.faceflag)
                {   
                    //人脸识别
                    if (checkflag)
                    {
                        float similar = 0.0f;
                        int num = 0;
                        for (int i = 0; i < listface.Count; i++)
                        {
                            float facesimilar = recogitionAndFacePairMatching(listface[i].face_Feature, detectfacefeature.face_Feature);
                            Console.WriteLine("编号: " + i + " -- " + "人脸姓名: " + listface[i].face_name + " -- " + "相似度: " + facesimilar);
                            if (facesimilar > similar)
                            {
                                similar = facesimilar;
                                num = i;
                            }
                        }
                        //刷新进度条
                        refresh = new Refresh(refreshprogressbar);
                        Invoke(refresh, (similar * 100), listface[num].face_name);
                        if (similar > changecheckvalue)
                        {
                            setControlText(label1, "识别结果： " + listface[num].face_name);
                            bool sendflag = false;
                            if (radioButton1.Checked)
                            {
                                sendflag = Serial_Device_API.GateAPI.GateSendData(Serial_Device_API.DeviceSerialPort.GateSerialPort, Serial_Device_API.GateAPI.gateleft);
                            }
                            else if (radioButton2.Checked)
                            {
                                sendflag = Serial_Device_API.GateAPI.GateSendData(Serial_Device_API.DeviceSerialPort.GateSerialPort, Serial_Device_API.GateAPI.gateright);
                            }
                            if (sendflag)
                            {
                                Console.WriteLine("数据发送成功！");
                            }
                            else
                            {
                                Console.WriteLine("数据发送失败！");
                            }
                        }
                        else
                        {
                            setControlText(label1, "识别结果： " + "无此人");
                        }
                    }
                    else
                    {
                        setControlText(label1, "识别结果： " + "未开启人脸识别");
                        //刷新进度条
                        refresh = new Refresh(refreshprogressbar);
                        Invoke(refresh, 0.00f, "");
                    }

                    //添加人脸
                    if (addflag)
                    {
                        pictureBox0.Image = null;
                        facelibrary = new FaceLibrary();
                        facelibrary.face_name = textBox1.Text.Trim();
                        detectfacefeature = GetDetectFaceFeature(bitmap, pictureBox0, true);
                        facelibrary.face_Feature = detectfacefeature.face_Feature;
                        if (currentfacenum > -1)    //覆盖当前人脸信息
                        {
                            listface[currentfacenum] = facelibrary;
                        }
                        else
                        {
                            listface.Add(facelibrary);
                        }
                        addflag = false;
                        currentfacenum = -1;
                        string listfacelibrary = JsonListToString(listface);
                        FileWrite(filepath, listfacelibrary);
                        MessageBox.Show("人脸注册成功!");
                        //清除人脸用户名
                        refreshtext = new RefreshText(refreshTextbox);
                        Invoke(refreshtext, textBox1, "");
                    }
                }
                else
                {
                    if(checkflag)
                    {
                        setControlText(label1, "识别结果： " + "未检测到人脸");
                    }
                    if (addflag)
                    {
                        MessageBox.Show("未检测到人脸，人脸注册失败!");
                        addflag = false;
                    }
                }
                //设置显示图片
                imageBox0.Image = mat;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void ArcSoft_Face_Demo_X64_Load(object sender, EventArgs e)
        {
            radioButton2.Checked = true;
            Serial_Device_API.DeviceSerialPort.GateSerialPort = new SerialPort();
            Serial_Device_API.GateAPI.GatePortInit(Serial_Device_API.DeviceSerialPort.GateSerialPort, "COM7", 115200, 8, Parity.None, StopBits.One, 3000, 3000);
            bool flag = Serial_Device_API.GateAPI.GatePortOpen(Serial_Device_API.DeviceSerialPort.GateSerialPort);
            if (flag)
            {
                Serial_Device_API.DeviceSerialPort.GateSerialPort.DataReceived += new SerialDataReceivedEventHandler(Serial_Device_API.GateAPI.GateReceivedData);
                Console.WriteLine("闸机初始化成功！");
            }
            else
            {
                Console.WriteLine("闸机初始化失败！");
            }

            DetectAndRecogition_Init();
            for (int i = 100; i > 25; i--)
            {
                comboBox1.Items.Add(i);
            }
            comboBox1.SelectedIndex = 20;
            //加载人脸库
            String listfacelibrary = FileRead("facelibrary.json");
            listface = StringToJsonList(listfacelibrary);
            if (listface == null)
            {
                listface = new List<FaceLibrary>();
            }
            //检测摄像头是否安装          
            if (!runflag)
            {
                capture = new VideoCapture();
                capture.Start();
                if (!capture.IsOpened)
                {
                    Console.WriteLine("摄像头检测失败，请确认摄像头是否安装！");
                    //this.Close();
                }
                capture.ImageGrabbed += new EventHandler(ImageGrabbedProcess);
                capture.Start();
                runflag = true;
            }
        }

        private void DetectAndRecogition_Init()
        {
            int retCode = 0;
            //初始化人脸检测引擎
            IntPtr pMemDetect = Marshal.AllocHGlobal(ArcSoft_FACE_API.FSDK_FACE_SHARE.detectSize);
            retCode = ArcSoft_FACE_API.FSDK_FACE_DETECTION.AFD_FSDK_InitialFaceEngine(
                ArcSoft_FACE_API.FSDK_FACE_SHARE.app_Id_X64,
                ArcSoft_FACE_API.FSDK_FACE_SHARE.sdk_Key_FD_X64,
                pMemDetect,
                ArcSoft_FACE_API.FSDK_FACE_SHARE.detectSize,
                ref ArcSoft_FACE_API.FSDK_FACE_SHARE.detectEngine,
                (int)ArcSoft_FACE_API.FSDK_FACE_DETECTION.AFD_FSDK_OrientPriority.AFD_FSDK_OPF_0_HIGHER_EXT,
                ArcSoft_FACE_API.FSDK_FACE_SHARE.nScale,
                ArcSoft_FACE_API.FSDK_FACE_SHARE.nMaxFaceNum
            );
            if (retCode != 0)
            {
                Console.WriteLine("人脸检测引擎初始化失败:错误码为:" + retCode);
                this.Close();
            }
            else
            {
                Console.WriteLine("人脸检测引擎初始化成功");
            }
            //初始化人脸识别引擎
            IntPtr pMemRecogition = Marshal.AllocHGlobal(ArcSoft_FACE_API.FSDK_FACE_SHARE.recogitionSize);
            retCode = ArcSoft_FACE_API.FSDK_FACE_RECOGNITION.AFR_FSDK_InitialEngine(
                ArcSoft_FACE_API.FSDK_FACE_SHARE.app_Id_X64,
                ArcSoft_FACE_API.FSDK_FACE_SHARE.sdk_Key_FR_X64,
                pMemRecogition,
                ArcSoft_FACE_API.FSDK_FACE_SHARE.recogitionSize,
                ref ArcSoft_FACE_API.FSDK_FACE_SHARE.recogitionEngine
            );
            if (retCode != 0)
            {
                Console.WriteLine("人脸识别引擎初始化失败:错误码为:" + retCode);
                this.Close();
            }
            else
            {
                Console.WriteLine("人脸识别引擎初始化成功");
            }
        }



        //检测人脸、提取特征
        private DetectFaceFeature GetDetectFaceFeature(Image imageParam,PictureBox picbox,bool cutimageflag)
        {
            DetectFaceFeature getdetectfacefeature = new DetectFaceFeature();
            Console.WriteLine("/********************* GetDetectFaceFeature Start*********************/");
            Console.WriteLine("  StartTime:{0}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.ffff"));
            //检测人脸
            //Console.WriteLine("/********************* Face Delect Start *********************/");
            int width = 0;
            int height = 0;
            int pitch = 0;
            Bitmap bitmapx = new Bitmap(imageParam);
            byte[] imageData = ArcSoft_FACE_API.FSDK_FACE_SHARE.ReadBmpToByte(bitmapx, ref width, ref height, ref pitch);
            IntPtr imageDataPtr = Marshal.AllocHGlobal(imageData.Length);
            Marshal.Copy(imageData, 0, imageDataPtr, imageData.Length);
            ArcSoft_FACE_API.FSDK_FACE_SHARE.ASVLOFFSCREEN offInput = new ArcSoft_FACE_API.FSDK_FACE_SHARE.ASVLOFFSCREEN();
            offInput.u32PixelArrayFormat = 513;
            offInput.ppu8Plane = new IntPtr[4];
            offInput.ppu8Plane[0] = imageDataPtr;
            offInput.i32Width = width;
            offInput.i32Height = height;
            offInput.pi32Pitch = new int[4];
            offInput.pi32Pitch[0] = pitch;
            IntPtr offInputPtr = Marshal.AllocHGlobal(Marshal.SizeOf(offInput));
            Marshal.StructureToPtr(offInput, offInputPtr, false);
            ArcSoft_FACE_API.FSDK_FACE_DETECTION.AFD_FSDK_FACERES detectface = new ArcSoft_FACE_API.FSDK_FACE_DETECTION.AFD_FSDK_FACERES();
            IntPtr faceResPtr = Marshal.AllocHGlobal(Marshal.SizeOf(detectface));
            Stopwatch watchTime = new Stopwatch();
            watchTime.Start();
            //检测结果
            int detectResult = ArcSoft_FACE_API.FSDK_FACE_DETECTION.AFD_FSDK_StillImageFaceDetection(
                ArcSoft_FACE_API.FSDK_FACE_SHARE.detectEngine,
                offInputPtr,
                ref faceResPtr
            );
            watchTime.Stop();
            //Console.WriteLine(String.Format("检测耗时:{0}ms", watchTime.ElapsedMilliseconds));
           // Console.WriteLine("/********************* Face Delect End *********************/");
            detectface = (ArcSoft_FACE_API.FSDK_FACE_DETECTION.AFD_FSDK_FACERES)Marshal.PtrToStructure(faceResPtr, typeof(ArcSoft_FACE_API.FSDK_FACE_DETECTION.AFD_FSDK_FACERES));
            ArcSoft_FACE_API.FSDK_FACE_SHARE.MRECT[] rectArr = new ArcSoft_FACE_API.FSDK_FACE_SHARE.MRECT[detectface.nFace];
            if (detectface.nFace > 0)
            {
                getdetectfacefeature.faceflag = true;

                //人脸画框
                for (int j = 0; j < rectArr.Length; j++)
                {
                    rectArr[j] = (ArcSoft_FACE_API.FSDK_FACE_SHARE.MRECT)Marshal.PtrToStructure(detectface.rcFace + Marshal.SizeOf(typeof(ArcSoft_FACE_API.FSDK_FACE_SHARE.MRECT)) * j, typeof(ArcSoft_FACE_API.FSDK_FACE_SHARE.MRECT));
                }
                imageParam = ArcSoft_FACE_API.FSDK_FACE_SHARE.DrawRectangleInPicture2(imageParam, rectArr, Color.Red, 3, DashStyle.Dash);

                //截取人脸
                ArcSoft_FACE_API.FSDK_FACE_SHARE.MRECT rect = rectArr[0];
                if (cutimageflag)
                {
                    for (int j = 0; j < rectArr.Length; j++)
                    {
                        Image imgResult = ArcSoft_FACE_API.FSDK_FACE_SHARE.CutFace((Bitmap)imageParam, rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
                        picbox.Image = imgResult;
                    }
                }

                //获取人脸特征值
                // Console.WriteLine("/********************* Face Recognition Start *********************/");
                ArcSoft_FACE_API.FSDK_FACE_RECOGNITION.AFR_FSDK_FACEINPUT faceResult = new ArcSoft_FACE_API.FSDK_FACE_RECOGNITION.AFR_FSDK_FACEINPUT();
                int orient = (int)Marshal.PtrToStructure(detectface.lfaceOrient, typeof(int));
                faceResult.lfaceOrient = orient;

                faceResult.rcFace = new ArcSoft_FACE_API.FSDK_FACE_SHARE.MRECT();
                rect = (ArcSoft_FACE_API.FSDK_FACE_SHARE.MRECT)Marshal.PtrToStructure(detectface.rcFace, typeof(ArcSoft_FACE_API.FSDK_FACE_SHARE.MRECT));
                faceResult.rcFace = rect;

                IntPtr faceResultPtr = Marshal.AllocHGlobal(Marshal.SizeOf(faceResult));
                Marshal.StructureToPtr(faceResult, faceResultPtr, false);
                ArcSoft_FACE_API.FSDK_FACE_RECOGNITION.AFR_FSDK_FACEMODEL localFaceModels = new ArcSoft_FACE_API.FSDK_FACE_RECOGNITION.AFR_FSDK_FACEMODEL();
                IntPtr localFaceModelsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(localFaceModels));

                watchTime.Start();
                int extractResult = ArcSoft_FACE_API.FSDK_FACE_RECOGNITION.AFR_FSDK_ExtractFRFeature(ArcSoft_FACE_API.FSDK_FACE_SHARE.recogitionEngine, offInputPtr, faceResultPtr, localFaceModelsPtr);
                watchTime.Stop();
                //Console.WriteLine(String.Format("抽取特征耗时:{0}ms", watchTime.ElapsedMilliseconds));
                localFaceModels = (ArcSoft_FACE_API.FSDK_FACE_RECOGNITION.AFR_FSDK_FACEMODEL)Marshal.PtrToStructure(localFaceModelsPtr, typeof(ArcSoft_FACE_API.FSDK_FACE_RECOGNITION.AFR_FSDK_FACEMODEL));
                //Console.WriteLine("" + localFaceModels.lFeatureSize);

                if (localFaceModels.lFeatureSize > 0)
                {
                    getdetectfacefeature.face_Feature = new byte[localFaceModels.lFeatureSize];
                    Marshal.Copy(localFaceModels.pbFeature, getdetectfacefeature.face_Feature, 0, localFaceModels.lFeatureSize);
                }
                else
                {
                    Console.WriteLine("获取人脸特征值失败");
                }
                Console.WriteLine("/********************* Face Recognition End  *********************/");
                faceResult = new ArcSoft_FACE_API.FSDK_FACE_RECOGNITION.AFR_FSDK_FACEINPUT();
                Marshal.FreeHGlobal(faceResultPtr);
                rect = new ArcSoft_FACE_API.FSDK_FACE_SHARE.MRECT();
                localFaceModels = new ArcSoft_FACE_API.FSDK_FACE_RECOGNITION.AFR_FSDK_FACEMODEL();
                Marshal.FreeHGlobal(localFaceModelsPtr);
            }
            else
            {
                picbox.Image = null;
            }
            bitmapx.Dispose();
            rectArr = null;
            offInput = new ArcSoft_FACE_API.FSDK_FACE_SHARE.ASVLOFFSCREEN();
            Marshal.FreeHGlobal(offInputPtr);
            imageData = null;
            Marshal.FreeHGlobal(imageDataPtr);

            Console.WriteLine("    EndTime:{0}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.ffff"));
            Console.WriteLine("/********************* GetDetectFaceFeature End *********************/");
            return getdetectfacefeature;
        }



        //检测人脸
        private ArcSoft_FACE_API.FSDK_FACE_DETECTION.AFD_FSDK_FACERES DetectFace(Image imageParam, PictureBox picbox, bool drawrectangleflag, bool cutpicboxflag)
        {
            ArcSoft_FACE_API.FSDK_FACE_DETECTION.AFD_FSDK_FACERES  detectface = new ArcSoft_FACE_API.FSDK_FACE_DETECTION.AFD_FSDK_FACERES(); 
            Console.WriteLine("############### Face Detect Start #########################");
            int width = 0;
            int height = 0;
            int pitch = 0;
            Bitmap bitmapx = new Bitmap(imageParam);
            byte[] imageData = ArcSoft_FACE_API.FSDK_FACE_SHARE.ReadBmpToByte(bitmapx, ref width, ref height, ref pitch);
            IntPtr imageDataPtr = Marshal.AllocHGlobal(imageData.Length);
            Marshal.Copy(imageData, 0, imageDataPtr, imageData.Length);
            //Marshal.StructureToPtr(imageData, imageDataPtr, false);
            ArcSoft_FACE_API.FSDK_FACE_SHARE.ASVLOFFSCREEN offInput = new ArcSoft_FACE_API.FSDK_FACE_SHARE.ASVLOFFSCREEN();
            offInput.u32PixelArrayFormat = 513;
            offInput.ppu8Plane = new IntPtr[4];
            offInput.ppu8Plane[0] = imageDataPtr;
            offInput.i32Width = width;
            offInput.i32Height = height;
            offInput.pi32Pitch = new int[4];
            offInput.pi32Pitch[0] = pitch;
            IntPtr offInputPtr = Marshal.AllocHGlobal(Marshal.SizeOf(offInput));
            Marshal.StructureToPtr(offInput, offInputPtr, false);
            IntPtr faceResPtr = Marshal.AllocHGlobal(Marshal.SizeOf(detectface));
            Console.WriteLine("StartTime:{0}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.ffff"));
            Stopwatch watchTime = new Stopwatch();
            watchTime.Start();
            //人脸检测
            int detectResult = ArcSoft_FACE_API.FSDK_FACE_DETECTION.AFD_FSDK_StillImageFaceDetection(
                ArcSoft_FACE_API.FSDK_FACE_SHARE.detectEngine,
                offInputPtr,
                ref faceResPtr
            );
            watchTime.Stop();
            Console.WriteLine(String.Format("检测耗时:{0}ms", watchTime.ElapsedMilliseconds));
            Marshal.FreeHGlobal(offInputPtr);
            return detectface;
        }

        //获取相似度
        private float recogitionAndFacePairMatching(byte[] firstFeature, byte[] secondFeature)
        {
            float similar = 0f;
            if (secondFeature != null)
            {
                ArcSoft_FACE_API.FSDK_FACE_RECOGNITION.AFR_FSDK_FACEMODEL firstFaceModels = new ArcSoft_FACE_API.FSDK_FACE_RECOGNITION.AFR_FSDK_FACEMODEL();
                IntPtr firstFeaturePtr = Marshal.AllocHGlobal(firstFeature.Length);
                Marshal.Copy(firstFeature, 0, firstFeaturePtr, firstFeature.Length);
                firstFaceModels.lFeatureSize = firstFeature.Length;
                firstFaceModels.pbFeature = firstFeaturePtr;
                IntPtr firstPtr = Marshal.AllocHGlobal(Marshal.SizeOf(firstFaceModels));
                Marshal.StructureToPtr(firstFaceModels, firstPtr, false);
                ArcSoft_FACE_API.FSDK_FACE_RECOGNITION.AFR_FSDK_FACEMODEL secondFaceModels = new ArcSoft_FACE_API.FSDK_FACE_RECOGNITION.AFR_FSDK_FACEMODEL();
                IntPtr secondFeaturePtr = Marshal.AllocHGlobal(secondFeature.Length);
                Marshal.Copy(secondFeature, 0, secondFeaturePtr, secondFeature.Length);
                secondFaceModels.lFeatureSize = secondFeature.Length;
                secondFaceModels.pbFeature = secondFeaturePtr;
                IntPtr secondPtr = Marshal.AllocHGlobal(Marshal.SizeOf(secondFaceModels));
                Marshal.StructureToPtr(secondFaceModels, secondPtr, false);
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                int result = ArcSoft_FACE_API.FSDK_FACE_RECOGNITION.AFR_FSDK_FacePairMatching(ArcSoft_FACE_API.FSDK_FACE_SHARE.recogitionEngine, firstPtr, secondPtr, ref similar);
                stopwatch.Stop();
                //Console.WriteLine("相似度:" + similar.ToString() + " 耗时:" + stopwatch.ElapsedMilliseconds.ToString() + "ms");
                firstFaceModels = new ArcSoft_FACE_API.FSDK_FACE_RECOGNITION.AFR_FSDK_FACEMODEL();
                secondFaceModels = new ArcSoft_FACE_API.FSDK_FACE_RECOGNITION.AFR_FSDK_FACEMODEL();
                Marshal.FreeHGlobal(firstFeaturePtr);
                Marshal.FreeHGlobal(secondFeaturePtr);
                Marshal.FreeHGlobal(firstPtr);
                Marshal.FreeHGlobal(secondPtr);
            }
            return similar;
        }

        //多线程设置控件的文本
        private void setControlText(Control control, string value)
        {
            control.Invoke(new Action<Control, string>((ct, v) => { ct.Text = v; }), new object[] { control, value });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Trim() != "")
            {
                for (int i = 0; i < listface.Count; i++)
                {
                    if (listface[i].face_name == textBox1.Text.Trim())
                    {
                        DialogResult dr = MessageBox.Show("该用户名的人脸已经注册，确认将覆盖该人脸！", "提示", MessageBoxButtons.OKCancel);
                        if (dr == DialogResult.Cancel)
                        {
                            textBox1.Text = "";
                            addflag = false;
                            return;
                        }
                        else
                        {
                            currentfacenum = i;
                        }
                    }
                }
               addflag = true;
            }
            else
            {
                MessageBox.Show("请输入注册人脸的用户名！");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (button2.Text.Trim() == "识别人脸")
            {
                //加载人脸库
                //String listfacelibrary = FileRead("facelibrary.json");
                //listface = StringToJsonList(listfacelibrary);
                pictureBox0.Image = null;
                textBox1.Text = "";
                changecheckvalue = float.Parse(comboBox1.Text) / 100;
                button2.Text = "停止识别";
                checkflag = true;
                button1.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
                button5.Enabled = false;
                comboBox1.Enabled = false;
                textBox1.Enabled = false;
            }
            else
            {
                checkflag = false;
                button2.Text = "识别人脸";
                button1.Enabled = true;
                button3.Enabled = true;
                button4.Enabled = true;
                button5.Enabled = true;
                comboBox1.Enabled = true;
                textBox1.Enabled = true;

            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "图片文件|*.bmp;*.jpg;*.jpeg;*.png|所有文件|*.*;";
            openFile.Multiselect = false;
            openFile.FileName = "";
            string filename = "";
            if (openFile.ShowDialog() == DialogResult.OK)
            {
                pictureBox0.Image = null;
                Image imageface = Image.FromFile(openFile.FileName);
                filename = openFile.FileName.Substring(openFile.FileName.LastIndexOf("\\") + 1); //去掉了路径                   
                textBox1.Text = filename.Substring(0, filename.LastIndexOf("."));
                pictureBox0.Image = new Bitmap(imageface);
                imageface.Dispose();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Trim() != "" && pictureBox0.Image != null)
            {
                for (int i = 0; i < listface.Count; i++)
                {
                    if (listface[i].face_name == textBox1.Text.Trim())
                    {
                        DialogResult dr = MessageBox.Show("该用户名的人脸已经注册，确认将覆盖该人脸！", "提示", MessageBoxButtons.OKCancel);
                        if (dr == DialogResult.Cancel)
                        {
                            textBox1.Text = "";
                            return;
                        }
                        else
                        {
                            currentfacenum = i;
                        }
                    }
                }
                DetectFaceFeature picboxdetectandextractfeature = GetDetectFaceFeature((Bitmap)pictureBox0.Image, pictureBox0, true);
                if (picboxdetectandextractfeature.faceflag)
                {
                    facelibrary = new FaceLibrary();
                    facelibrary.face_name = textBox1.Text.Trim();
                    facelibrary.face_Feature = picboxdetectandextractfeature.face_Feature;
                    if (currentfacenum > -1)    //覆盖当前人脸信息
                    {
                        listface[currentfacenum] = facelibrary;
                    }
                    else
                    {
                        listface.Add(facelibrary);
                    }
                    string listfacelibrary = JsonListToString(listface);
                    FileWrite(filepath, listfacelibrary);
                    MessageBox.Show("人脸注册成功!");
                    textBox1.Text = "";
                }
                else
                {
                    MessageBox.Show("未检测到人脸，人脸注册失败!");
                }
                currentfacenum = -1;
            }
            else
            {
                if (textBox1.Text.Trim() == "" && pictureBox0.Image == null)
                {
                    MessageBox.Show("请输入注册人脸的用户名和图片！");
                }
                else if (textBox1.Text.Trim() == "")
                {
                    MessageBox.Show("请输入注册人脸的用户名！");
                }
                else if (pictureBox0.Image == null)
                {
                    MessageBox.Show("请输入人脸图片！");
                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Trim() != "")
            {
                for (int i = 0; i < listface.Count; i++)
                {
                    if (listface[i].face_name == textBox1.Text.Trim())
                    {
                        currentfacenum = i;
                        DialogResult dr = MessageBox.Show("该用户名的人脸已经注册，确认将删除该人脸！", "提示", MessageBoxButtons.OKCancel);
                        if (dr == DialogResult.Cancel)
                        {
                            MessageBox.Show("取消删除人脸操作!");
                            textBox1.Text = "";
                            return;
                        }
                        else
                        {
                            facelibrary = new FaceLibrary();
                            facelibrary.face_name = listface[currentfacenum].face_name;
                            facelibrary.face_Feature = listface[currentfacenum].face_Feature;
                            listface.Remove(facelibrary);
                            string listfacelibrary = JsonListToString(listface);
                            FileWrite(filepath, listfacelibrary);
                            MessageBox.Show("删除人脸操作成功!");
                            textBox1.Text = "";
                            currentfacenum = -1;
                        }
                    }
                }
                if(currentfacenum < 0)
                {
                    currentfacenum = -1;
                    MessageBox.Show("该用户名未注册！");
                }
            }
            else
            {
                MessageBox.Show("请输入注册人脸的用户名！");
            }
            textBox1.Text = "";
        }

        private void ArcSoftFaceDemoX64_DragEnter(object sender, DragEventArgs e)
        {
            // Check if the Dataformat of the data can be accepted
            // (we only accept file drops from Explorer, etc.)
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy; // Okay
            }
            else
            {
                e.Effect = DragDropEffects.None; // Unknown data, ignore it
            }
        }


        private void ArcSoftFaceDemoX64_FormClosing(object sender, FormClosingEventArgs e)
        {
            capture.ImageGrabbed -= new EventHandler(ImageGrabbedProcess);
            Application.Exit();
        }

        //JSON序列化,将List<T>转换为String
        private String JsonListToString (List<FaceLibrary> list)
        {
            JavaScriptSerializer Serializerx = new JavaScriptSerializer();
            string changestr = Serializerx.Serialize(list);
            return changestr;
        }


        //JSON反序列化,将String转换为List<T>
        private List<FaceLibrary> StringToJsonList(string str)
        {
            JavaScriptSerializer Serializer = new JavaScriptSerializer();
            List<FaceLibrary> face = Serializer.Deserialize<List<FaceLibrary>>(str);
            return face;
        }

        //写入文件
        private void FileWrite(string filepath,string writestr)
        {
            FileStream fs = new FileStream(filepath, FileMode.Truncate,FileAccess.ReadWrite);   //清空文件内容
            fs.Close();
            fs = new FileStream(filepath, FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);
            sw.Write(writestr);
            sw.Close();
            fs.Close();
        }

        //读取文件
        private string  FileRead(string filepath)
        {
            FileStream fs = new FileStream(filepath, FileMode.OpenOrCreate);
            StreamReader sr = new StreamReader(fs);
            string str = sr.ReadToEnd();
            sr.Close();
            fs.Close();
            return str;
        }

    }
}
