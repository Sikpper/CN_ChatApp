using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;//命名空间
using System.Collections.Generic;


namespace ChatApp
{
    public partial class Chatwindow : Form
    {
        int listenport = 50000;
        string localIP = null;
        string localUser = null;
        Socket allSocket;

        public Chatwindow()
        {
            InitializeComponent();
        }

        public Chatwindow(string username, TcpClient tcpListenr, string receiveText, string tempIP)
        {
            localUser = username;
            localIP = tempIP;
            listenport += (username[6] - '0') * 1000 + (username[7] - '0') * 100 + (username[8] - '0') * 10 + (username[9] - '0');
            StartListening(localIP, listenport);

            InitializeComponent();
        }

        #region 启用监听
        public void StartListening(string localIP, int listenport)
        {
            //主机IP
            IPEndPoint serverIp = new IPEndPoint(IPAddress.Parse(localIP), listenport);
            Socket tcpServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            tcpServer.Bind(serverIp);
            tcpServer.Listen(100);
            AsynRecive(allSocket);
        }
        #endregion

        #region 异步连接客户端

        public void AsynAccept(Socket tcpServer)
        {
            tcpServer.BeginAccept(asyncResult =>
            {
                Socket tcpClient = tcpServer.EndAccept(asyncResult);

                AsynAccept(tcpServer);
                AsynRecive(tcpClient);
            }, null);
        }
        #endregion

        #region 异步接受客户端消息

        public void AsynRecive(Socket tcpClient)
        {
            byte[] data = new byte[1024];
            try
            {
                tcpClient.BeginReceive(data, 0, data.Length, SocketFlags.None,
                asyncResult =>
                {
                    int length = tcpClient.EndReceive(asyncResult);
                    string recieveMess = Encoding.UTF8.GetString(data, 0, length);
                    receiveTextBox.SelectionAlignment = HorizontalAlignment.Right;
                    receiveTextBox.AppendText(recieveMess);
                    AsynRecive(tcpClient);
                }, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("异常信息：", ex.Message);
            }
        }
        #endregion

        #region 异步发送消息

        public void AsynSend(Socket tcpClient, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            try
            {
                tcpClient.BeginSend(data, 0, data.Length, SocketFlags.None, asyncResult =>
                {
                    int length = tcpClient.EndSend(asyncResult);
                }, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("异常信息：{0}", ex.Message);
            }
        }

        #endregion

        private void sendB_Click(object sender, EventArgs e)
        {
            string sendMess = sendTextBox.Text;
            AsynSend(allSocket, sendMess);
            sendTextBox.Text = null;
        }
    }
}
