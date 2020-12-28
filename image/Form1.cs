using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;

namespace image
{
    public partial class Form1 : Form
    {

        #region 全局变量

        int bmp_width  = 240;                           //定义接收数据的宽度
        int bmp_height = 320;                           //定义接受图片的高度

        Bitmap bmp;                                     //定义一个bmp变量,用于显示与储存图片

        int hang = 0, lie = 0;                          //用于写入图像时的行列指针
        //串口发送回的数据以8为为依据,将rgb565拆分成2个字节发送,故需要以下变量
        bool isheight = true;                           //判断是否为高位的标志位true表示当前数据为高位数据
        int heightdate = 0;                             //用于存储高8位的储存单元

        bool pic_MoveFlag = false;                      //pic移动的标志位
        int pic_xPos = 0;                               //鼠标移动的偏移量
        int pic_yPos = 0;

        string SelectPath = "";

        int orderID = 1;

        #endregion

        /// <summary>
        /// 窗口初始化
        /// </summary>
        public Form1()
        {
            InitializeComponent();
        }

        private void ShowDebug(string str)
        {
            try
            {
                int MaxLength = 14;
                int lines = (str.Length / MaxLength) + 1;
                textBox1.Text += DateTime.Now.ToString(orderID.ToString("000") + "·[yy-MM-dd HH:mm:ss]:\r\n");
                orderID++;
                for (int i = 0; i < lines; i++)
                {
                    if (i == lines - 1)
                    {
                        textBox1.Text += "- "
                            + str.Substring(i * MaxLength, str.Length % MaxLength)
                            + "\r\n";
                    }
                    else
                    {
                        textBox1.Text += "- "
                            + str.Substring(i * MaxLength, MaxLength) + "\r\n";
                    }
                }
                textBox1.Text += "\r\n\r\n";
            }
            catch { }
        }

        /// <summary>
        /// 窗口UI初始化调用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            button1_Click(null, null);                  //更新串口号   
            comboBox2.SelectedIndex = 4;                //选中默认波特率
            ShowDebug("系统开始运行。");
            bmp = new Bitmap(bmp_height, bmp_width);
        }

        /// <summary>
        /// 更新按键,按下可自动查找可用串口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            for (int i = 0; i < 30; i++)                //在此处修改数值以更高刷新范围
            {
                try
                {
                    SerialPort sp = new SerialPort("COM" + (i + 1).ToString());
                    sp.Open();
                    sp.Close();
                    comboBox1.Items.Add("COM" + (i + 1).ToString());
                }
                catch { }
            }
            try { comboBox1.SelectedIndex = 0; }
            catch { }
            ShowDebug("串口刷新成功。");
        }

        /// <summary>
        /// 打开串口前需要对串口属性进行赋值
        /// 如需要修改停止位等信息,可在此处添加
        /// </summary>
        private void SetPortProperty()
        {
            SerialPort1.PortName = comboBox1.Text.Trim();
            SerialPort1.BaudRate = Convert.ToInt32(comboBox2.Text.Trim());
        }

        /// <summary>
        /// 打开串口按键
        /// 根据当前串口是否已经开启选择打开串口或者关闭串口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            if (!SerialPort1.IsOpen)
            {
                try
                {
                    SetPortProperty();
                    SerialPort1.Open();
                    byte [] SendBuff = new byte[]{0x00};
                    SerialPort1.Write(SendBuff, 0, 1);
                    button2.Text = "关闭串口";
                    comboBox1.Enabled = false;
                    comboBox2.Enabled = false;
                    numericUpDown1.Enabled = false;
                    numericUpDown2.Enabled = false;
                    hang = 0;
                    lie = 0;
                    isheight = true;
                    Graphics g = Graphics.FromImage(bmp);
                    g.Clear(Color.Black);               //清除图像信息
                    ShowDebug("串口打开成功。");
                }
                catch (Exception ee)
                {
                    ShowDebug("串口打开失败。");
                    MessageBox.Show(ee.Message);
                    return;
                }
            }
            else
            {
                try
                {
                    byte[] SendBuff = new byte[] { 0xff };
                    SerialPort1.Write(SendBuff, 0, 1);
                    SerialPort1.Close();
                    button2.Text = "打开串口";
                    comboBox1.Enabled = true;
                    comboBox2.Enabled = true;
                    numericUpDown1.Enabled = true;
                    numericUpDown2.Enabled = true;
                    ShowDebug("串口关闭成功。");
                }
                catch (Exception ee)
                {
                    ShowDebug("串口关闭失败。");
                    MessageBox.Show(ee.Message);
                    return;
                }
            }
        }

        /// <summary>
        /// 将一个16位的RGB565格式颜色转化成RGB24格式颜色,并返回Color类
        /// </summary>
        /// <param name="RGB565_H"></param>
        /// <param name="RGB565_L"></param>
        /// <returns></returns>
        Color RGB565_To_RGB24(int RGB565_H,int RGB565_L)
        {
            int RGB565_MASK_RED = 0xF800;
            int RGB565_MASK_GREEN = 0x07E0;
            int RGB565_MASK_BLUE = 0x001F;
            int RGB565;
            int R, G, B;
            RGB565_H <<= 8;
            RGB565 = RGB565_H | RGB565_L;
            R = (RGB565 & RGB565_MASK_RED) >> 11;
            G = (RGB565 & RGB565_MASK_GREEN) >> 5;
            B = (RGB565 & RGB565_MASK_BLUE);
            R <<= 3;
            G <<= 2;
            B <<= 3;
            return Color.FromArgb(R, G, B);
        }

        /// <summary>
        /// 在bmp中写入一个像素点的颜色,并自动将指针向下一个是像素点移动
        /// 当指针移动到像素点的最后一个像素时,将返回图像起点
        /// </summary>
        /// <param name="c"></param>
        void Write_A_Color(Color c)
        {
            this.Invoke((EventHandler)(delegate { bmp.SetPixel(hang, lie, c); }));
            if (hang < bmp_height - 1) hang++;
            else
            {
                hang = 0;
                //TODO：在此处添加写入一行数据后的行为。
                this.BeginInvoke((EventHandler)(delegate { pictureBox1.Image = bmp; }));
                if (lie < bmp_width - 1) lie++;
                else
                {
                    lie = 0;
                    //TODO：在此处添加写完一整幅图像后的行为。
                    if (checkBox1.Checked)              //如果选择保存图片
                    {
                        string FileName = DateTime.Now.ToString("HHmmss") + ".png";
                        this.BeginInvoke((EventHandler)(delegate { bmp.Save(SelectPath + "\\" + FileName); }));
                        ShowDebug(FileName + "保存成功");
                    }
                    byte[] SendBuff = new byte[] { 0x00 };
                    SerialPort1.Write(SendBuff, 0, 1);
                    Graphics g = Graphics.FromImage(bmp);
                    g.Clear(Color.Black);               //清除图像信息
                }
            }
        }

        /// <summary>
        /// 根据接收的数组绘制一副图像
        /// 注意：byte[] Data数据中的数据不一定为一副图像的完整数据
        /// 若数据不完整,下次调用时将自动完成绘制
        /// </summary>
        /// <param name="Data"></param>
        void Paint_A_bmp(byte[] Data)
        {
            foreach(byte color in Data)
            {
                if (isheight)                           //判断是否为高位
                {
                    isheight = false;
                    heightdate = color;
                }
                else
                {
                    isheight = true;                    //若为低8位,则转化颜色,并写入bmp
                    Color c = RGB565_To_RGB24(heightdate, color);
                    Write_A_Color(c);
                }
            }
        }

        /// <summary>
        /// 串口接收数据事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                byte res;
                System.Threading.Thread.Sleep(100);                             //线程等待
                if (SerialPort1.IsOpen)
                {
                    Byte[] ReceivedData = new Byte[SerialPort1.BytesToRead];    //创建接收字节数组
                    SerialPort1.Read(ReceivedData, 0, ReceivedData.Length);     //读取所接收到的数据
                    for (int i = 0; i < ReceivedData.Length; i++)
                    {
                        if (ReceivedData[i] == 0x00) ;

                    }
                        Paint_A_bmp(ReceivedData);                                  //绘制已经接收到的数据图像
                }
            }
            catch { ShowDebug("接收异常！"); }
        }

        /// <summary>
        /// 系统关闭前执行的函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                SerialPort1.Close();                                        //如果串口没有关,则关闭串口后再关闭窗口
            }
            catch { }
        }

        /// <summary>
        /// textBox1自动换行事件
        /// 为了防止其缓存占用大导致系统Bug,设定其字符总数大于5000时清空
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.ScrollToCaret();
            if (textBox1.Text.Length > 5000)
                textBox1.Text = "";
        }

        /// <summary>
        /// 设定波特率的comboBox控件自定义波特率事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedIndex == 0)
            {
                comboBox2.DropDownStyle = ComboBoxStyle.DropDown;
                comboBox2.Text = "";
            }
            else
            {
                comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            }
        }

        /// <summary>
        /// 鼠标滑轮放大缩小图像的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox1_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            double temp = (double)pictureBox1.Width / bmp_width;
            int WidthSplit = bmp_width / 20;
            int HeightSplit = bmp_height / 20;
            //调整缩放比例时注意与原图的比例关系
            if (e.Delta > 0)                            //如果向上滚动
            {
                pictureBox1.Width += HeightSplit;
                pictureBox1.Height += WidthSplit;
                pictureBox1.Left -= HeightSplit >> 1;
                pictureBox1.Top -= WidthSplit >> 1;
            }
            else                                        //如果向下滚动
            {
                if (pictureBox1.Width > HeightSplit)
                {
                    pictureBox1.Width -= HeightSplit;
                    pictureBox1.Height -= WidthSplit;
                    pictureBox1.Left += HeightSplit >> 1;
                    pictureBox1.Top += WidthSplit >> 1;
                }
            }
            label3.Text = " X" + temp.ToString("0.00");
        }

        /// <summary>
        /// 鼠标对pic按下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            pic_MoveFlag = true;
            pic_xPos = e.X;
            pic_yPos = e.Y;
        }

        /// <summary>
        /// 鼠标对pic控件放开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            pic_MoveFlag = false;
        }

        /// <summary>
        /// 鼠标对pic控件的移动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (pic_MoveFlag)
            {
                pictureBox1.Left += Convert.ToInt16(e.X - pic_xPos);
                pictureBox1.Top += Convert.ToInt16(e.Y - pic_yPos);
            }
        }

        /// <summary>
        /// 选择文件夹事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                SelectPath = folderBrowserDialog1.SelectedPath;
                button3.Text = "已选择文件夹";
                this.Text = "@zzudongxiang制作  " + SelectPath;
                checkBox1.Enabled = true;
                ShowDebug("已选择：" + Path.GetFileName(SelectPath));
                checkBox1.Checked = true;
            }
        }

        /// <summary>
        /// 单击链接,弹出提示窗,显示通讯协议及其他内容
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show(FAQ,"帮助 FAQ");
        }

        /// <summary>
        /// 在窗口大小发生改变时,将图片移动到界面正中心
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panel2_SizeChanged(object sender, EventArgs e)
        {
            pictureBox1.Left = (panel2.Width - pictureBox1.Width) >> 1;
            pictureBox1.Top = (panel2.Height - pictureBox1.Height) >> 1;
        }

        #region FAQ内容

        string FAQ = @"
----------------------------------FAQ----------------------------------
@zzudongxiang制作
串口显示图像上位机Demo程序

注：本版本可能会在关闭串口时有假死机现象发生,发生错误时请使用任务管理器
关闭程序。
-----------------------------------
串口属性：
波特率：     自定义
停止位：     1
奇偶校验：   0
数据位：     8
-----------------------------------
数据传输协议：
打开串口后上位机会发送一个0x00作为起始信号,随后下位机开始传输图像,当
上位机关闭串口时会发送一个0xff作为终止信号,下位机收到信号后可采取相应
操作,例如停止传输。
注：每传输完成一帧图像,上位机都将发送一个0x00,下位机可在收到0x00后开始
下一幅图像的传输,以便同步图像信号。数据格式为,先发送一个颜色的高8位,再
发送低8位数据即可。

*:Warning：请勿在单片机正在回传数据时开启串口！！！
";       

        #endregion

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            bmp_height = (int)numericUpDown1.Value;
            bmp_width = (int)numericUpDown2.Value;
        }
    }
}
