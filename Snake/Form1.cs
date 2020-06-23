using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Snake
{
    public partial class Snake : Form
    {
        readonly BackgroundWorker _worker;

        string currentDir = "u";

        bool playing = false;

        int index;

        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        PictureBox[,] field = new PictureBox[50, 50];

        int[,] data = new int[50, 50];

        public Snake()
        {
            InitializeComponent();

            CheckForIllegalCrossThreadCalls = false;

            _worker = new BackgroundWorker();

            _worker.DoWork += DoUpdate;

            clientSocket.ReceiveTimeout = 100;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Initialize();
        }

        private void DoUpdate(object sender, DoWorkEventArgs e)
        {
            do
            {
                //if (dead) return;

                GetData();

                VisualUpdate();

                Thread.Sleep(200);
            } while (true);
        }

        private int[,] GetField(string encoded)
        {
            int[,] temp = new int[50, 50];
            int x = 0, y = 0;

            foreach (var item in encoded.Split('|'))
            {
                foreach (var itegm in item.Split(';'))
                {
                    try
                    {
                        temp[x, y] = Convert.ToInt32(itegm);
                    }
                    catch (Exception)
                    {

                    }

                    x++;
                }

                x = 0;

                y++;
            }

            return temp;
        }

        private void Initialize()
        {
            for (int y = 0; y < field.GetLength(1); y++)
            {
                for (int x = 0; x < field.GetLength(0); x++)
                {
                    field[x, y] = new PictureBox();

                    field[x, y].Location = new Point(x * 10, y * 10);
                }
            }

            foreach (var item in field)
            {
                item.BackColor = Color.White;

                item.Height = 10;

                item.Width = 10;

                item.Visible = false;

                Controls.Add(item);
            }
        }

        public static string SendReceive(Socket server, string msgg)
        {
            byte[] msg = Encoding.ASCII.GetBytes(msgg);
            byte[] bytes = new byte[8092];

            try
            {
                int byteCount = server.Send(msg, 0, msg.Length, SocketFlags.None);

                Console.WriteLine("Sent {0} bytes.", byteCount);

                byteCount = server.Receive(bytes, SocketFlags.None);

                Console.WriteLine(Encoding.ASCII.GetString(bytes));

                byte[] msgBytes = new byte[byteCount];

                Array.Copy(bytes, msgBytes, byteCount);

                return Encoding.ASCII.GetString(msgBytes);
            }
            catch (SocketException e)
            {
                Console.WriteLine("{0} Error code: {1}.", e.Message, e.ErrorCode);

                return e.Message;
            }
        }

        private void GetData()
        {
            string msg = SendReceive(clientSocket, "getField");

            this.data = GetField(msg);
        }

        private void VisualUpdate()
        {
            for (int y = 0; y < field.GetLength(1); y++)
            {
                for (int x = 0; x < field.GetLength(0); x++)
                {
                    if (data[x, y] == 0)
                    {
                        field[x, y].Visible = false;
                    }
                    else if (data[x, y] == 1)
                    {
                        field[x, y].BackColor = Color.AliceBlue;

                        field[x, y].Visible = true;
                    }
                    else if (data[x, y] == 2)
                    {
                        field[x, y].BackColor = Color.BlueViolet;

                        field[x, y].Visible = true;
                    }
                    else if (data[x, y] == 3)
                    {
                        field[x, y].BackColor = Color.MediumVioletRed;

                        field[x, y].Visible = true;
                    }
                    else if (data[x, y] == 4)
                    {
                        field[x, y].BackColor = Color.Bisque;

                        field[x, y].Visible = true;
                    }

                    field[x, y].Update();
                }
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Right && currentDir != "r" && currentDir != "l")
            {
                clientSocket.Send(Encoding.ASCII.GetBytes($"changeDirectionr{index}"));

                currentDir = "r";
            }
            else if (keyData == Keys.Left && currentDir != "l" && currentDir != "r")
            {
                clientSocket.Send(Encoding.ASCII.GetBytes($"changeDirectionl{index}"));

                currentDir = "l";
            }
            else if (keyData == Keys.Up && currentDir != "u" && currentDir != "d")
            {
                clientSocket.Send(Encoding.ASCII.GetBytes($"changeDirectionu{index}"));

                currentDir = "u";
            }
            else if (keyData == Keys.Down && currentDir != "d" && currentDir != "u")
            {
                clientSocket.Send(Encoding.ASCII.GetBytes($"changeDirectiond{index}"));

                currentDir = "d";
            }

            return true;
        }

        private void btnJoin_Click(object sender, EventArgs e)
        {
            try
            {
                if (playing)
                {
                    return;
                }

                playing = true;

                btnJoin.Text = "Connecting...";

                btnJoin.Update();

                clientSocket.Connect("40.118.26.21", 3537);

                btnJoin.Text = "Getting frame time...";

                btnJoin.Update();

                string time = SendReceive(clientSocket, "getFrameTime");

                btnJoin.Text = "Entering...";

                btnJoin.Update();

                btnJoin.Enabled = false;

                btnJoin.Visible = false;

                string str = SendReceive(clientSocket, "enterLobby");

                try
                {
                    index = Convert.ToInt32(str) - 1;
                }
                catch (Exception)
                {
                    throw;
                }

                GetData();

                VisualUpdate();

                Thread.Sleep(Convert.ToInt32(time));

                _worker.RunWorkerAsync();
            }
            catch (SocketException)
            {
                MessageBox.Show("Failed to connect to the server.", "Snake", MessageBoxButtons.OK, MessageBoxIcon.Error);

                playing = false;
            }

            btnJoin.Text = "Play!";
        }
    }
}
