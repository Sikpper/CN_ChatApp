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
    public partial class LogIn : Form
    {
        string filePath = Application.StartupPath;
        IPAddress server = IPAddress.Parse("166.111.140.14");
        int port = 8000;

        public string username = null;
        string password = null;
        Socket client = null;
        public LogIn()
        {
            InitializeComponent();
            //读取上次保存的用户和密码，如没有则不保存
            try
            {
                FileStream fs = new FileStream(filePath + "//usrinfo//checkstate", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                StreamReader datareader = new StreamReader(fs, System.Text.Encoding.UTF8);
                string tempdata = datareader.ReadLine();
                if (tempdata[0] == '1')
                {
                    RememberKeyC.CheckState = CheckState.Checked;
                    tempdata = null;
                    tempdata = datareader.ReadLine();
                    if (tempdata != null)
                    {
                        username = tempdata;
                        usernameT.Text = username;
                        datareader = File.OpenText(filePath + "//usrinfo//" + username + "//password");
                        tempdata = datareader.ReadLine();
                        passwordT.Text = tempdata;
                    }
                }
                datareader.Close();
                datareader.Dispose();


            }
            catch (FileNotFoundException)
            {
                return;
            }


        }

        private void logButton_Click(object sender, EventArgs e)
        {
            username = usernameT.Text.ToString();
            password = passwordT.Text.ToString();

            //检查用户名和密码
            if (username == null)
            {
                MessageBox.Show("用户名不能为空", "登陆错误", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            else if (password == null)
            {
                MessageBox.Show("密码不能为空", "登陆错误", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            //初始化端口号和地址
            IPEndPoint serverPoint = new IPEndPoint(server, port);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            //尝试用同步套接字连接
            try
            {
                client.Connect(serverPoint);
            }
            catch (SocketException)
            {
                MessageBox.Show("网络故障，请检查网络后重新连接", "网络错误", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }

            //传输登陆信息
            string login_mess = username + "_" + password;
            byte[] login_byte = new byte[1024];
            login_byte = Encoding.ASCII.GetBytes(login_mess);

            try
            {
                client.Send(login_byte, login_byte.Length, 0);
            }
            catch (SocketException)
            {
                MessageBox.Show("连接超时，请重新连接", "连接错误", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }

            //接收登录信息
            byte[] receive_byte = new byte[1024];
            int bytenumber = client.Receive(receive_byte, receive_byte.Length, 0);

            string receive_mess = Encoding.Default.GetString(receive_byte, 0, bytenumber);

            if (receive_mess == "lol")
            {
                MessageBox.Show("登陆成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                string infoPath = null;

                //记住密码选项
                if (RememberKeyC.Checked == true)
                {
                    //如果选择记住密码，则下次打开时会保留上次登陆时的账户和密码
                    infoPath = filePath + "//usrinfo//" + username;
                    if (!Directory.Exists(infoPath))
                    {
                        Directory.CreateDirectory(infoPath);
                    }
                    infoPath = filePath + "//usrinfo//" + username + "//" + "password";
                    FileStream fs = new FileStream(infoPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    StreamWriter fWriter = new StreamWriter(fs, System.Text.Encoding.UTF8);
                    fWriter.Write(password);
                    fWriter.Flush();
                    fWriter.Close();

                    infoPath = filePath + "//usrinfo//checkstate";
                    fs = new FileStream(infoPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    fWriter = new StreamWriter(fs, System.Text.Encoding.UTF8);
                    fWriter.WriteLine("1");
                    fWriter.WriteLine(username);
                    fWriter.Close();
                    fWriter.Dispose();
                }
                else
                {
                    //如果选择不记住密码，则不会保存
                    infoPath = filePath + "//usrinfo//checkstate";
                    StreamWriter fWriter = File.CreateText(infoPath);
                    fWriter.Write("0\n");
                    fWriter.Close();
                }

            }
            else
            {
                MessageBox.Show("账号或密码错误", "登陆错误", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }

            //登陆成功，打开主窗口界面
            this.DialogResult = DialogResult.OK;
            this.Close();

        }
    }
}
