using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;//命名空间


namespace ChatApp
{
    public partial class MainWin : Form
    {
        string filePath = Application.StartupPath;

        IPAddress server = IPAddress.Parse("166.111.140.14");
        int port = 8000;
        Socket client = null;
        IPEndPoint serverPoint = null;
        
        string localIP;
        TcpListener tcpServListener;
        int listenport = 50000;//50000以上的监听端口系统一般不会用到

        public MainWin()
        {
            InitializeComponent();
        }

        public MainWin(string username)
        {
            InitializeComponent();
            usernameL.Text = username;
            //尝试建立套接字
            serverPoint = new IPEndPoint(server, port);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            try
            {
                client.Connect(serverPoint);
            }
            catch (SocketException)
            {
                return;
            }

            //传输并接收IP信息
            string send_mess = "q" + usernameL.Text;
            byte[] send_byte = new byte[1024];
            send_byte = Encoding.ASCII.GetBytes(send_mess);

            try
            {
                client.Send(send_byte, send_byte.Length, 0);
            }
            catch (SocketException)
            {
                return;
            }

            byte[] receive_byte = new byte[1024];
            int bytenumber = client.Receive(receive_byte, receive_byte.Length, 0);

            string receive_mess = Encoding.Default.GetString(receive_byte, 0, bytenumber);
            IpL.Text = receive_mess;
            localIP = receive_mess;

            string infoPath = filePath + "//usrinfo//" + usernameL.Text + "//friendlist";
            FileStream fs = new FileStream(infoPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
            StreamReader fReader = new StreamReader(fs, System.Text.Encoding.UTF8);

            //更新好友列表信息
            string tempIP;
            string nextline = null;
            while (true)
            {
                nextline = fReader.ReadLine();
                if (nextline == null)
                {
                    break;
                }
                LookupId(nextline, false, false, true, out tempIP);

            }

            listenport += (username[6] - '0') * 1000 + (username[7] - '0') * 100 + (username[8] - '0') * 10 + username[9] - '0';
            tcpServListener = new TcpListener(IPAddress.Parse(localIP),listenport);
            //开始监听
            tcpServListener.Start();
            //开始接受信息
            TcpAccept(tcpServListener);
        }

        #region 查询信息
        //查询子函数
        //除在线外其余IP均返回0.0.0.0
        //返回-1表示用户名为空
        //返回-2表示连接超时
        //返回11表示不在线且在好友列表里有但状态未变
        //返回12表示不在线且在好友列表里有但状态改变
        //返回13表示不在线且不在好友列表里
        //返回21表示在线且在好友列表里有但状态未变
        //返回22表示在线且在好友列表里有但状态改变
        //返回23表示在线且不在好友列表里
        private int LookupId(string lookupId, bool messBoxQ, bool textChangeQ,bool addOfflineQ,out string IP)
        {
            IP = "0.0.0.0";
            if (lookupId == null)
            {
                if(messBoxQ)
                {
                    MessageBox.Show("查找的用户名不能为空", "错误", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
                return -1;
            }

            //传输并接收在线信息
            string lookupinfo = "q" + lookupId;
            byte[] lookupinfo_byte = new byte[1024];
            lookupinfo_byte = Encoding.ASCII.GetBytes(lookupinfo);

            try
            {
                client.Send(lookupinfo_byte, lookupinfo_byte.Length, 0);
            }
            catch (SocketException)
            {
                if (messBoxQ)
                {
                    MessageBox.Show("连接超时，请重新连接", "连接错误", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
                return -2;
            }

            byte[] receive_byte = new byte[1024];
            int bytenumber = client.Receive(receive_byte, receive_byte.Length, 0);

            string receive_mess = Encoding.Default.GetString(receive_byte, 0, bytenumber);

            if (receive_mess == "n")
            {
                if (messBoxQ)
                {
                    MessageBox.Show("对方此时不在线", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (textChangeQ)
                {
                    lookupIpL.Text = "未知";
                    lookupStateL.Text = "不在线";
                }

                //检测列表中是否有该查询好友，若信息变更则更新列表
                string[] nfriend_info = new string[3];
                nfriend_info[0] = lookupId;
                nfriend_info[1] = "未知";
                nfriend_info[2] = "不在线";
                foreach (ListViewItem item in friendListV.Items)
                {
                    if (item.Text.Equals(lookupId))
                    {
                        if (item.SubItems[1].Text.Equals("未知"))
                        {
                            return 11;
                        }
                        else
                        {
                            friendListV.Items.Remove(item);
                            friendListV.Items.Add(new ListViewItem(nfriend_info));
                            return 12;
                        }
                    }
                }
                friendListV.Items.Add(new ListViewItem(nfriend_info));
                return 13;
            }
            else if (receive_mess == "Incorrect No."|| receive_mess == "Please send the correct message.")
            {
                if (messBoxQ)
                {
                    MessageBox.Show("此账户不存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (textChangeQ)
                {
                    lookupIpL.Text = "未知";
                    lookupStateL.Text = "不存在";
                }
                return 0;
            }
            else
            {
                if (textChangeQ)
                {
                    lookupIpL.Text = receive_mess;
                    lookupStateL.Text = "在线";
                }
                string[] nfriend_info = new string[3];
                nfriend_info[0] = lookupId;
                nfriend_info[1] = receive_mess;
                nfriend_info[2] = "在线";

                //检测列表中是否有该查询好友，若没有或信息变更则更新列表
                foreach (ListViewItem item in friendListV.Items)
                {
                    if (item.Text.Equals(lookupId))
                    {
                        if (item.SubItems[1].Text.Equals(receive_mess))
                        {
                            return 21;
                        }
                        else
                        {
                            friendListV.Items.Remove(item);
                            friendListV.Items.Add(new ListViewItem(nfriend_info));
                            return 22;
                        }
                    }
                }
                friendListV.Items.Add(new ListViewItem(nfriend_info));
                IP = receive_mess;
                return 23;
            }
        }

        //查询某人信息
        private void lookupB_Click(object sender, EventArgs e)
        {
            string tempIP;
            LookupId(lookupIdT.Text, true, true, false, out tempIP); 
        }
        #endregion

        #region 下线
        //下线子函数
        private bool Logout()
        {
            string infoPath = filePath + "//usrinfo//" + usernameL.Text
                + "//friendlist";
            FileStream fs = new FileStream(infoPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            StreamWriter fWriter = new StreamWriter(fs, System.Text.Encoding.UTF8);
            foreach (ListViewItem item in friendListV.Items)
            {
                fWriter.WriteLine(item.Text);
            }
            fWriter.Close();

            string send_mess = "logout" + usernameL.Text;
            byte[] send_byte = new byte[1024];
            send_byte = Encoding.ASCII.GetBytes(send_mess);

            try
            {
                client.Send(send_byte, send_byte.Length, 0);
            }
            catch (SocketException)
            {
                 return false;
            }

            byte[] receive_byte = new byte[1024];
            int bytenumber = client.Receive(receive_byte, receive_byte.Length, 0);

            string receive_mess = Encoding.Default.GetString(receive_byte, 0, bytenumber);

            if (receive_mess == "loo")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //下线
        private void offlineB_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("你确定要退出吗！\n(退出后会自动下线)", "提示信息", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (result == DialogResult.OK)
            {
                if (Logout() == true)
                {
                    MessageBox.Show("下线成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("注销失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
                this.Close();
            }

        }

        //重载关闭窗口的函数
        private void MainWin_FormClosing(object sender, FormClosingEventArgs e)
        {
            /*DialogResult result = MessageBox.Show("你确定要关闭吗！\n(关闭后会自动退出)", "提示信息", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (result == DialogResult.OK)
            {
                e.Cancel = false;  //点击OK
                if (Logout() == true)
                {
                    MessageBox.Show("下线成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("注销失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Asterisk)
                }
            }
            else
            {
                e.Cancel = true;
            }
            */
        }
        #endregion

        #region 好友列表操作 

        //删除好友
        private void deleteFriend_Click(object sender, EventArgs e)
        {
            foreach(ListViewItem item in friendListV.Items)
            {
                if (item.Selected)
                {
                    friendListV.Items.Remove(item);
                    return;
                }
            }
            MessageBox.Show("您未选中任何好友", "错误", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

        //刷新好友列表
        private void friendRefresh_Click(object sender, EventArgs e)
        {
            string tempIp;
            foreach (ListViewItem item in friendListV.Items)
            {
                LookupId(item.Text,false, false, false, out tempIp);
            }
        }
        #endregion

        //新建聊天窗口

        //接受信息
        public void TcpReceive(TcpClient tcpclient)
        {
            Task.Run(() =>
            {
                string remotehost = tcpclient.Client.RemoteEndPoint.ToString();
                NetworkStream rcvstream = tcpclient.GetStream();
                while (true)
                {
                    //读取长度
                    byte[] buffer = new byte[4];
                    rcvstream.Read(buffer, 0, 4);
                    int len = BitConverter.ToInt32(buffer, 0);
                    //读取正文
                    buffer = new byte[len];
                    rcvstream.Read(buffer, 0, len);
                    string rcvmsg = Encoding.UTF8.GetString(buffer);
                    Thread server_client_connection = new Thread(() => Application.Run(new Chatwindow(usernameL.Text, tcpclient, rcvmsg, localIP)));
                    server_client_connection.SetApartmentState(System.Threading.ApartmentState.STA);//单线程监听控制
                    server_client_connection.Start();
                }
            });
        }

        //接受连接
        public void TcpAccept(TcpListener tcpListener)
        {
            tcpServListener.BeginAcceptSocket(ar=> 
            {
                TcpClient tcpclient = tcpListener.EndAcceptTcpClient(ar);
                TcpAccept(tcpListener);//继续监听其余连接
                TcpReceive(tcpclient);//监听信息
            }, tcpServListener);
        }


        private void startChatB_Click(object sender, EventArgs e)
        {
            //查看是否有人选中，开始聊天模式（单聊、群聊集成了；因为程序类似）
            if (friendListV.SelectedItems.Count > 0)
            {
                //选中人是否在线
                foreach (ListViewItem item in friendListV.Items)
                {
                    if (item.Selected == true && item.SubItems[2].ToString() == "OffLine")
                    {
                        MessageBox.Show("好友不在线", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                int connect_num = 0;//选中的人数
                string broad_mess;//广播信息(学号)，包括本机地址
                string all_part = usernameL.Text;//所有参与对话者
                Socket[] clients = new Socket[friendListV.SelectedItems.Count];//套接字对象
                foreach (ListViewItem item in friendListV.SelectedItems)
                {
                    //所有人的学号
                    broad_mess = usernameL.Text;
                    foreach (ListViewItem item1 in friendListV.SelectedItems)
                    {
                        if (item1.SubItems[0].Text != item.SubItems[0].Text)
                            broad_mess = broad_mess + "," + item1.SubItems[0].Text;
                    }
                    //参与对话者，用来建立线程
                    all_part = all_part + "," + item.SubItems[0].Text;

                    clients[connect_num] = Chat_group(item.SubItems[0].Text, broad_mess);//群聊
                    connect_num++;
                }

                //开启对话框，开始对话
                Thread server_client_connection = new Thread(() => Application.Run(new Chatwindow(all_part, clients, connect_num)));
                server_client_connection.SetApartmentState(System.Threading.ApartmentState.STA);//单线程监听控制
                server_client_connection.Start();
            }
        }

        public Socket Chat_group(string user, string broad)
        {
            string ip;
            LookupId(user, false, false, false, out ip);
            IPEndPoint user_ip = new IPEndPoint(IPAddress.Parse(ip), 50000);
            Socket user_tcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            user_tcp.Connect(user_ip);

            byte[] data = Encoding.UTF8.GetBytes(broad);
            user_tcp.Send(data);
            return user_tcp;
        }
    }
}
