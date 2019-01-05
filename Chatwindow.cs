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

        string[] chatMemIPs = null;
        string[] chatUsers = null;

        TcpClient localclient;

        public Chatwindow()
        {
            InitializeComponent();
        }

        public Chatwindow(string members, TcpClient tcpClient, string receiveMsg, string tempIP)
        {
            InitializeComponent();
            //可以进行跨线程操作
            CheckForIllegalCrossThreadCalls = false;

            localUser = receiveMsg.Split('|')[0].Split(',')[0];
            localIP = receiveMsg.Split('|')[0].Split(',')[1];

            string[] chatUserInfos = receiveMsg.Split('|');
            chatMemIPs = new string[chatUserInfos.Length];
            chatUsers = new string[chatUserInfos.Length];

            string[] uesr_info = new string[2];
            ListViewItem item = null;
            for (int i = 0; i < chatUserInfos.Length; i++)
            {
                chatUsers[i] = chatUserInfos[i].Split(',')[0];
                chatMemIPs[i] = chatUserInfos[i].Split(',')[1];

                uesr_info[0] = chatUsers[i];
                uesr_info[1] = chatMemIPs[i];
                item = new ListViewItem(uesr_info);
                chatList.Items.Add(item);
            }

            localclient = tcpClient;
            TcpReceive(tcpClient);

        }

        public Chatwindow(string members, TcpClient tcpClient, int userNumber, string tempIP)
        {
            InitializeComponent();
            //可以进行跨线程操作
            CheckForIllegalCrossThreadCalls = false;

            localUser = members.Split(',')[0];
            localIP = tempIP.Split(',')[0];

            chatMemIPs = tempIP.Split(','); 
            chatUsers = members.Split(',');

            string[] uesr_info = new string[2];
            ListViewItem item = null;
            for (int i = 0; i <= userNumber; i++)
            {
                uesr_info[0] = chatUsers[i];
                uesr_info[1] = chatMemIPs[i];
                item = new ListViewItem(uesr_info);
                chatList.Items.Add(item);
            }

            localclient = tcpClient;
            TcpReceive(tcpClient);
        }

        private void sendB_Click(object sender, EventArgs e)
        {
            string sendMsg = sendTextBox.Text;
            TcpSend(localclient, sendMsg);
            sendTextBox.Text = null;
        }

        //Tcp异步接受消息
        public void TcpReceive(TcpClient tcpclient)
        {
            if (tcpclient == null)
            {
                MessageBox.Show("网络连接中断", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (tcpclient.Connected == false)
            {
                MessageBox.Show("网络连接中断", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                NetworkStream rcvstream = tcpclient.GetStream();
                string rcvmsg = null;
                //读取长度
                byte[] buffer = new byte[4];
                rcvstream.BeginRead(buffer, 0, 4, ar =>
                {
                    int len = BitConverter.ToInt32(buffer, 0);
                    if (len == 0)
                    {
                        MessageBox.Show("对方已断开连接", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    //读取正文
                    buffer = new byte[len];
                    rcvstream.Read(buffer, 0, len);
                    rcvmsg = Encoding.UTF8.GetString(buffer);

                    string sendUser = rcvmsg.Split('$')[0];
                    rcvmsg = rcvmsg.Substring(rcvmsg.IndexOf("$")+1);

                    receiveTextBox.SelectionAlignment = HorizontalAlignment.Right;
                    receiveTextBox.AppendText(sendUser + " " + DateTime.Now.ToLongTimeString() + " ");
                    receiveTextBox.AppendText(DateTime.Now.ToLongDateString() + "\n");
                    receiveTextBox.AppendText(rcvmsg + "\n");
                    //更新接受信息
                    TcpReceive(tcpclient);
                }, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //Tcp异步发送消息
        public void TcpSend(TcpClient tcpclient, string sendMsg)
        {
            if (tcpclient == null) return;
            if (tcpclient.Connected == false) return;
            try
            {
                string remotehost = tcpclient.Client.RemoteEndPoint.ToString();
                NetworkStream sendstream = tcpclient.GetStream();

                //更新信息
                receiveTextBox.SelectionAlignment = HorizontalAlignment.Right;
                receiveTextBox.AppendText(localUser + " " + DateTime.Now.ToLongTimeString() + " ");
                receiveTextBox.AppendText(DateTime.Now.ToLongDateString() + "\n");
                receiveTextBox.AppendText(sendMsg + "\n");

                //
                sendMsg = localUser + "$" + sendMsg; 
                byte[] data = Encoding.UTF8.GetBytes(sendMsg);
                byte[] buffer = BitConverter.GetBytes(data.Length);
                sendstream.Write(buffer, 0, 4);
                sendstream.Write(data, 0, data.Length);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString(), "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
        }

    }

}
