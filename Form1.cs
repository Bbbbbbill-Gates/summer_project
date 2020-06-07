using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace FrontBoardVersion0
{
    public partial class Form1 : Form
    {
        DateTime lastAction;

        private bool canDraw = false;
        private bool isDrawing = false;
        private bool isTool = false;
        private bool isselected = false;
        private bool canPutDown = false;
        private bool isCutting = false;
        private bool isoriginal = false;

        private Point st, ed;
        private Rectangle rect;

        private struct toolRange
        {
            public int Xmin, Xmax;
            public int Ymin, Ymax;
        }
        private toolRange[] toolRanges;
        private PictureBox[] toolbox;
        private toolType[] toolboxState;
        private int toolLen;
        private int selectedTool;

        private Bitmap bmp;
        private Bitmap originBmp;
        Graphics g;
        Graphics gBmp;

        private enum lineType { freeDraw, rectangle, straightLine, curve, dottedLine };
        private lineType lineState;

        private enum toolType { teamBlue, teamRed, block, goal, upright, horizontal, ladder, circle, ball, cone };
        string[] toolDir;
        private toolType toolState;

        public Form1()
        {
            InitializeComponent();
            bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            originBmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            pictureBox1.DrawToBitmap(this.bmp, pictureBox1.ClientRectangle);
            pictureBox1.DrawToBitmap(this.originBmp, pictureBox1.ClientRectangle);
            pictureBox1.SendToBack();

            g = pictureBox1.CreateGraphics();
            gBmp = Graphics.FromImage(this.bmp);

            toolbox = new PictureBox[1000];
            toolboxState = new toolType[1000];
            toolRanges = new toolRange[1000];
            toolDir = new string[10];
            toolDir[0] = @"D:\ajy\summer_project\front_project\FrontBoardVersion0\sources\images\m_teamblue.png";
            toolDir[1] = @"D:\ajy\summer_project\front_project\FrontBoardVersion0\sources\images\m_teamred.png";
            toolDir[2] = @"D:\ajy\summer_project\front_project\FrontBoardVersion0\sources\images\m_block.png";
            toolDir[3] = @"D:\ajy\summer_project\front_project\FrontBoardVersion0\sources\images\m_goal.png";
            toolDir[4] = @"D:\ajy\summer_project\front_project\FrontBoardVersion0\sources\images\m_upright.png";
            toolDir[5] = @"D:\ajy\summer_project\front_project\FrontBoardVersion0\sources\images\m_horizontal.png";
            toolDir[6] = @"D:\ajy\summer_project\front_project\FrontBoardVersion0\sources\images\m_ladder.png";
            toolDir[7] = @"D:\ajy\summer_project\front_project\FrontBoardVersion0\sources\images\m_circle.png";
            toolDir[8] = @"D:\ajy\summer_project\front_project\FrontBoardVersion0\sources\images\m_ball.png";
            toolDir[9] = @"D:\ajy\summer_project\front_project\FrontBoardVersion0\sources\images\m_cone.png";
        }

        /// <summary>
        /// 自定义鼠标样式转换
        /// </summary>
        /// <param name="img">bitmap读入的图片的引用</param>
        /// <param name="HotSpotX">鼠标作用点的横坐标</param>
        /// <param name="HotSpotY">鼠标作用点的纵坐标</param>
        /// <returns></returns>
        private Cursor GetCursor(Bitmap img, int HotSpotX = 0, int HotSpotY = 0)
        {
            Bitmap curImg = new Bitmap(img.Width * 2, img.Height * 2);
            Graphics g = Graphics.FromImage(curImg);

            g.Clear(Color.FromArgb(0, 0, 0, 0));
            g.DrawImage(img, img.Width - HotSpotX, img.Width - HotSpotY, img.Width, img.Height);

            Cursor cur = new Cursor(curImg.GetHicon());

            g.Dispose();
            curImg.Dispose();

            return cur;
        }

        /// <summary>
        /// 点击变为默认鼠标样式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mousebtn_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Default;
            this.canDraw = false;
            this.isTool = false;
        }

        /// <summary>
        /// 点击开启画笔，可以随意在战术板画画
        /// 五种画笔：pen, box, straight, curve, dotted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void penbtn_Click(object sender, EventArgs e)
        {
            string dir = @"D:\ajy\summer_project\picture_material\mouse\m_pen.png";
            Bitmap image = new Bitmap(dir);
            this.Cursor = GetCursor(image, 0, 30);

            this.canDraw = true;
            this.isTool = false;
            this.isCutting = false;
            lineState = lineType.freeDraw;
        }

        private void boxbtn_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Cross;

            this.canDraw = true;
            this.isTool = false;
            this.isCutting = false;
            lineState = lineType.rectangle;
        }

        private void straightlinebtn_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Cross;

            this.canDraw = true;
            this.isTool = false;
            this.isCutting = false;
            lineState = lineType.straightLine;
        }

        private void curvebtn_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Cross;

            this.canDraw = true;
            this.isCutting = false;
            this.lineState = lineType.curve;
        }

        private void dottedlinebtn_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Cross;

            this.canDraw = true;
            this.isTool = false;
            this.isCutting = false;
            lineState = lineType.dottedLine;
        }



        /// <summary>
        /// 画笔下方的各项工具栏，共十二种。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void screencut_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Cross;

            this.isCutting = true;
            this.canDraw = true;
            this.isTool = false;
            lineState = lineType.rectangle;
        }

        private void video_Click(object sender, EventArgs e)
        {

        }
        
        private void teambluebtn_Click(object sender, EventArgs e)
        {
            string dir = @"D:\ajy\summer_project\picture_material\mouse\m_teamblue.png";
            Bitmap image = new Bitmap(dir);
            this.Cursor = GetCursor(image);

            isTool = true;
            toolState = toolType.teamBlue;
        }

        private void teamredbtn_Click(object sender, EventArgs e)
        {
            string dir = @"D:\ajy\summer_project\picture_material\mouse\m_teamred.png";
            Bitmap image = new Bitmap(dir);
            this.Cursor = GetCursor(image);

            isTool = true;
            toolState = toolType.teamRed;
        }

        private void horizontal_Click(object sender, EventArgs e)
        {
            string dir = @"D:\ajy\summer_project\picture_material\mouse\m_horizontal.png";
            Bitmap image = new Bitmap(dir);
            this.Cursor = GetCursor(image);

            isTool = true;
            toolState = toolType.horizontal;
        }

        private void block_Click(object sender, EventArgs e)
        {
            string dir = @"D:\ajy\summer_project\picture_material\mouse\m_block.png";
            Bitmap image = new Bitmap(dir);
            this.Cursor = GetCursor(image);

            isTool = true;
            toolState = toolType.block;
        }

        private void upright_Click(object sender, EventArgs e)
        {
            string dir = @"D:\ajy\summer_project\picture_material\mouse\m_upright.png";
            Bitmap image = new Bitmap(dir);
            this.Cursor = GetCursor(image);

            isTool = true;
            toolState = toolType.upright;
        }

        private void goal_Click(object sender, EventArgs e)
        {
            string dir = @"D:\ajy\summer_project\picture_material\mouse\m_goal.png";
            Bitmap image = new Bitmap(dir);
            this.Cursor = GetCursor(image);

            isTool = true;
            toolState = toolType.goal;
        }

        private void circle_Click(object sender, EventArgs e)
        {
            string dir = @"D:\ajy\summer_project\picture_material\mouse\m_circle.png";
            Bitmap image = new Bitmap(dir);
            this.Cursor = GetCursor(image);

            isTool = true;
            toolState = toolType.circle;
        }

        private void ladder_Click(object sender, EventArgs e)
        {
            string dir = @"D:\ajy\summer_project\picture_material\mouse\m_ladder.png";
            Bitmap image = new Bitmap(dir);
            this.Cursor = GetCursor(image);

            isTool = true;
            toolState = toolType.ladder;
        }

        private void cone_Click(object sender, EventArgs e)
        {
            string dir = @"D:\ajy\summer_project\picture_material\mouse\m_cone.png";
            Bitmap image = new Bitmap(dir);
            this.Cursor = GetCursor(image);

            isTool = true;
            toolState = toolType.cone;
        }

        private void ball_Click(object sender, EventArgs e)
        {
            string dir = @"D:\ajy\summer_project\picture_material\mouse\m_ball.png";
            Bitmap image = new Bitmap(dir);
            this.Cursor = GetCursor(image);

            isTool = true;
            toolState = toolType.ball;
        }



        /// <summary>
        /// picturebox 重绘
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (canDraw && rect != null && this.lineState == lineType.rectangle)
            {
                Pen p = new Pen(Color.Blue, 2);
                p.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
                p.DashPattern = new float[] { 5, 5 };
                e.Graphics.DrawRectangle(p, rect);
            }

            if (canDraw && this.lineState == lineType.straightLine)
            {
                Pen p = new Pen(Color.Blue, 2);
                p.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(4, 4, true);
                e.Graphics.DrawLine(p, st, ed);
            }

            if (canDraw && this.lineState == lineType.dottedLine)
            {
                Pen p = new Pen(Color.Blue, 2);
                p.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
                p.DashPattern = new float[] { 5, 5 };
                p.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(4, 4, true);
                e.Graphics.DrawLine(p, st, ed);
            }

            if (canDraw && this.lineState == lineType.curve)
            {
                Point p1 = new Point(ed.X, ed.Y);
                Point p2 = new Point((ed.X + st.X) * 7 / 10, (ed.Y + st.Y) * 7 / 10);
                Point p3 = new Point((ed.X + st.X) * 3 / 10, (ed.Y + st.Y) * 3 / 10);
                Pen p = new Pen(Color.Blue, 2);
                p.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(4, 4, true);
                p.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
                p.DashPattern = new float[] { 5, 5 };

                e.Graphics.DrawBezier(p, st, p2, p3, p1);
            }

            
        }

        /// <summary>
        /// 鼠标点下事件，实现功能：记录坐标，实例化工具，选择工具
        /// 
        /// 实例化工具选择工具：
        ///     1.实例化时，检查鼠标位置不允许已经存在对象；
        ///     2.选择时，从保存对象的数组中寻找，用 Capture 检测是否选中
        ///     3.选择时，记录选中的工具类型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            st = ed = new Point(e.X, e.Y);
            if (!isTool && !canDraw && !canPutDown) return;
            if (!isTool && canDraw) isDrawing = true;

            for (int i = 0; i < toolLen; i++)
            {
                toolbox[i].Visible = false;
            }
            Image picImg = pictureBox1.Image;
            if (picImg != null) picImg.Dispose();
            if (! this.isoriginal) pictureBox1.Image = Image.FromHbitmap(this.bmp.GetHbitmap());
            else pictureBox1.Image = Image.FromHbitmap(this.originBmp.GetHbitmap());
            for (int i = 0; i < toolLen; i++)
            {
                toolbox[i].Visible = true;
            }
            
            if (this.isTool && ! canPutDown)
            {
                //获取当前新建picturebox的下标
                int index = toolLen;
                
                //获取范围，检查是否可以新建
                Image img = Image.FromFile(toolDir[(int)toolState]);
                if (! checkInRange(e, img.Width, img.Height))
                {
                    //添加并修改新建picturebox的属性
                    toolbox[index] = new PictureBox();

                    toolbox[index].Image = img;
                    this.Controls.Add(toolbox[index]);
                    toolbox[index].BringToFront();
                    toolbox[index].Parent = pictureBox1;
                    toolbox[index].BackColor = Color.FromArgb(0, 255, 255, 255);

                    toolbox[index].SizeMode = PictureBoxSizeMode.AutoSize;
                    toolbox[index].Location = new Point(e.X, e.Y);
                    toolbox[index].Visible = true;
                    toolboxState[index] = toolState;

                    //MessageBox.Show(toolState.ToString());

                    toolbox[index].Tag = index;
                    toolbox[index].MouseDown += toolbox_MouseDown;

                    //将新建的工具对象添加到范围数组中用以检测
                    int wid = toolbox[index].ClientSize.Width, hei = toolbox[index].ClientSize.Height;
                    int locX = toolbox[index].Location.X, locY = toolbox[index].Location.Y;

                    toolRanges[index].Xmin = locX;
                    toolRanges[index].Xmax = locX + wid;
                    toolRanges[index].Ymin = locY;
                    toolRanges[index].Ymax = locY + hei;
                    toolLen ++;
                }
                else //处理叠加、邻近
                {

                }
            }

            if (canPutDown)
            {
                //获取当前移动picturebox的下标
                int index = selectedTool;
                //MessageBox.Show(toolState.ToString());
                //获取范围，检查是否可以完成移动
                Image img = Image.FromFile(toolDir[(int)toolboxState[index]]);
                if (!checkInRange(e, img.Width, img.Height))
                {
                    toolbox[index].Dispose();
                    //添加并修改新建picturebox的属性
                    toolbox[index] = new PictureBox();

                    toolbox[index].Image = img;
                    this.Controls.Add(toolbox[index]);
                    toolbox[index].BringToFront();
                    toolbox[index].Parent = pictureBox1;
                    toolbox[index].BackColor = Color.FromArgb(0, 255, 255, 255);

                    toolbox[index].SizeMode = PictureBoxSizeMode.AutoSize;
                    toolbox[index].Location = new Point(e.X, e.Y);
                    toolbox[index].Visible = true;

                    toolbox[index].Tag = index;
                    toolbox[index].MouseDown += toolbox_MouseDown;

                    //将新建的工具对象添加到范围数组中用以检测
                    int wid = toolbox[index].ClientSize.Width, hei = toolbox[index].ClientSize.Height;
                    int locX = toolbox[index].Location.X, locY = toolbox[index].Location.Y;

                    toolRanges[index].Xmin = locX;
                    toolRanges[index].Xmax = locX + wid;
                    toolRanges[index].Ymin = locY;
                    toolRanges[index].Ymax = locY + hei;
                }
                else //处理叠加、邻近
                {
                    toolbox[index].Visible = true;

                    //或者做其他操作改进
                }
                this.Cursor = Cursors.Default;
                this.canDraw = false;
                this.isTool = false;

                this.isselected = false;
                this.canPutDown = false;
            }
        }

        //查找鼠标处不处于某对象范围
        private bool checkInRange(MouseEventArgs e, int w, int h)
        {
            for (int i = 0; i < this.toolLen; i ++ )
            {
                if (e.X + w <= toolRanges[i].Xmin || e.X >= toolRanges[i].Xmax) continue;
                if (e.Y + h <= toolRanges[i].Ymin || e.Y >= toolRanges[i].Ymax) continue;
                return true;
            }
            return false;
        }

        //得到最近的空地
        //

        //toolbox中绑定的事件函数
        private void toolbox_MouseDown(object sender, MouseEventArgs e)
        {
            if (canDraw || isDrawing || isTool || isselected) return;
            this.isselected = true;
            
            PictureBox p = (PictureBox)sender;
            selectedTool = (int)p.Tag;
            toolbox[selectedTool].Visible = false;

        }

        /// <summary>
        /// 鼠标抬起事件，实现功能：结束绘画，拖动的工具实例化
        /// 
        /// 拖动工具实例化：
        ///     1.抬起时，检查抬起位置是否合格
        ///     2.合格，重新实例化对象，代替原来对象。
        ///     3.不合格，原来对象visible = true;
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (! isDrawing && ! isTool) return;
            if (isDrawing)
            {
                isDrawing = false;
                
                for (int i = 0; i < toolLen; i++)
                {
                    toolbox[i].Visible = false;
                }
                if (lineState == lineType.freeDraw) pictureBox1.Image = Image.FromHbitmap(this.bmp.GetHbitmap());
                if (! this.isCutting) pictureBox1.DrawToBitmap(this.bmp, pictureBox1.ClientRectangle);
                for (int i = 0; i < toolLen; i++)
                {
                    toolbox[i].Visible = true;
                }

                if (this.isCutting)
                {
                    Rectangle myRectSrc = rect;//截图矩形
                    Rectangle myRectDest = new Rectangle(new Point(0, 0), myRectSrc.Size);//输出矩形
                    Bitmap cutBmp = new Bitmap(myRectSrc.Width, myRectSrc.Height);//建立图片
                    Graphics cutg = Graphics.FromImage(cutBmp);
                    cutg.DrawImage(pictureBox1.Image, myRectDest, myRectSrc, GraphicsUnit.Pixel);//将pictureBox1的截图画到图片上
                    cutg.Dispose();

                    SaveFileDialog saveImageDialog = new SaveFileDialog();
                    saveImageDialog.Title = "Capture screen image savedialog";
                    saveImageDialog.Filter = @"jpeg|.jpg|bmp|.bmp|png|*.png";
                    if (saveImageDialog.ShowDialog() == DialogResult.OK)
                    {
                        string fileName = saveImageDialog.FileName.ToString();
                        if (fileName != "" && fileName != null)
                        {
                            string fileExtName = fileName.Substring(fileName.LastIndexOf(".") + 1).ToString();
                            if (fileExtName != "")
                            {
                                switch (fileExtName)
                                {
                                    case "jpg":
                                        cutBmp.Save(saveImageDialog.FileName, ImageFormat.Jpeg);
                                        break;
                                    case "bmp":
                                        cutBmp.Save(saveImageDialog.FileName, ImageFormat.Bmp);
                                        break;
                                    case "png":
                                        cutBmp.Save(saveImageDialog.FileName, ImageFormat.Png);
                                        break;
                                    default:
                                        MessageBox.Show("只能存取为:jpg,bmp,png格式");
                                        break;
                                }
                            }
                        }
                    }

                    cutBmp.Dispose();
                    this.Cursor = Cursors.Default;
                    this.canDraw = false;
                    this.isTool = false;
                    this.isCutting = false;

                    pictureBox1.Refresh();
                    pictureBox1.Image = Image.FromHbitmap(this.bmp.GetHbitmap());
                }
            }
        }

        /// <summary>
        /// 鼠标移动(实现为，按下后抬起之前的起作用)，实现功能：任意绘画，拖拽期间的滑动效果。
        /// 
        /// 拖动：
        ///     1.拖动工具发生时，原来对象 visible = false;
        ///     2.改变鼠标样式。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (new TimeSpan(DateTime.Now.Ticks - lastAction.Ticks).TotalMilliseconds > 50)
            {
                lastAction = DateTime.Now;
                if (this.lineState == lineType.freeDraw) freeDraw(e);
                if (this.lineState == lineType.rectangle) rectangle(e);
                if (this.lineState == lineType.straightLine) straightLine(e);
                if (this.lineState == lineType.dottedLine) dottedLine(e);
                if (this.lineState == lineType.curve) curve(e);

                if (isselected && ! canPutDown)
                {
                    toolbox[selectedTool].Visible = false;
                    this.canPutDown = true;

                    string dir = toolDir[(int)toolboxState[selectedTool]];
                    Bitmap image = new Bitmap(dir);
                    this.Cursor = GetCursor(image);
                }
            }
        }

        /// <summary>
        /// freeDraw rectangle straightLine dottedLine curve 分别实现不同的画图效果。
        /// 待优化：pen是非托管资源，用完需要dispose，暂时无太大影响，后期修改。
        /// </summary>
        /// <param name="e"></param>
        private void freeDraw(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (isDrawing)
                {
                    Point currentPoint = new Point(e.X, e.Y);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;//消除锯齿  
                    g.DrawLine(new Pen(Color.Blue, 2), ed, currentPoint);
                    gBmp.DrawLine(new Pen(Color.Blue, 2), ed, currentPoint);
                    ed.X = currentPoint.X;
                    ed.Y = currentPoint.Y;
                }
            }
        }

        private void rectangle(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (isDrawing)
                {
                    Point p1 = new Point(e.X, e.Y);
                    Point p2 = new Point(e.X, st.Y);
                    Point p3 = new Point(st.X, e.Y);
                    Pen p = new Pen(Color.Blue, 2);
                    p.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
                    p.DashPattern = new float[] { 5, 5 };
                    g.DrawLine(p, p1, p2);
                    g.DrawLine(p, p1, p3);
                    g.DrawLine(p, st, p2);
                    g.DrawLine(p, st, p3);
                    
                    ed.X = p1.X;
                    ed.Y = p1.Y;
                    rect.Location = new Point(Math.Min(st.X, ed.X), Math.Min(st.Y, ed.Y));
                    rect.Size = new Size(Math.Abs(st.X - ed.X), Math.Abs(st.Y - ed.Y));
                    pictureBox1.Invalidate();
                }
            }
        }

        private void straightLine(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (isDrawing)
                {
                    Point p1 = new Point(e.X, e.Y);
                    Pen p = new Pen(Color.Blue, 2);
                    p.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(4, 4, true);
                    g.DrawLine(p, st, p1);

                    ed.X = p1.X;
                    ed.Y = p1.Y;
                    pictureBox1.Invalidate();
                }
            }
        }

        private void dottedLine(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (isDrawing)
                {
                    Point p1 = new Point(e.X, e.Y);
                    Pen p = new Pen(Color.Blue, 2);
                    p.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
                    p.DashPattern = new float[] { 5, 5 };
                    p.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(4, 4, true);
                    g.DrawLine(p, st, p1);

                    ed.X = p1.X;
                    ed.Y = p1.Y;
                    pictureBox1.Invalidate();
                }
            }
        }

        private void curve(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (isDrawing)
                {
                    Point p1 = new Point(e.X, e.Y);
                    Point p2 = new Point((e.X + st.X) * 7 / 10, (e.Y + st.Y) * 7 / 10);
                    Point p3 = new Point((e.X + st.X) * 3 / 10, (e.Y + st.Y) * 3 / 10);
                    Pen p = new Pen(Color.Blue, 2);
                    p.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(4, 4, true);
                    p.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
                    p.DashPattern = new float[] { 5, 5 };

                    g.DrawBezier(p, st, p2, p3, p1);
                    ed.X = p1.X;
                    ed.Y = p1.Y;
                    pictureBox1.Invalidate();
                }
            }
        }

        private void delete_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null)
            {
                MessageBox.Show("已经清空！", "提示");
                return;
            }
            for (int i = 0; i < toolLen; i++)
            {
                toolbox[i].Dispose();
            }


            pictureBox1.Image = Image.FromHbitmap(this.bmp.GetHbitmap()); 
            Image img = pictureBox1.Image; 
            pictureBox1.Image = null; 
            img.Dispose();
            img = pictureBox1.BackgroundImage;
            pictureBox1.BackgroundImage = null;
            img.Dispose();

            this.bmp.Dispose();
            this.bmp = null;
            this.bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            
            pictureBox1.BackgroundImage = Image.FromFile(@"D:\ajy\summer_project\picture_material\pitch.png"); 
            pictureBox1.DrawToBitmap(this.bmp, pictureBox1.ClientRectangle);
            
            gBmp = Graphics.FromImage(this.bmp);
            toolLen = 0;
            this.Cursor = Cursors.Default; this.canDraw = false; this.isTool = false;

            MessageBox.Show("已经清空！", "提示");
        }
    }
}
