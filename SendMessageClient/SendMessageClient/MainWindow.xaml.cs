using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
using System.Xml.Serialization;

namespace SendMessageClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string LoginConst = "Login";          // send
        const string UsersListConst = "UsersList";  // send, receive
        const string AllConst = "All";              // send, receive
        const string NomerConst = "Nomer";          // send, receive
        const string RazdConst = ";";               // send, receive
        static public Socket socket;
        IPEndPoint ipPoint;
        public MainWindow()
        {
            InitializeComponent();
            btnConnect.IsEnabled = true;
            btDisconnect.IsEnabled = false;
            Main.Title = "";
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            // адрес и порт сервера, к которому будем подключаться
            int port;
            string address;
            if (tbUserName.Text == "")
            {
                MessageBox.Show("Введите имя пользователя!");
                return;
            }
            address = tbIpAdress.Text;
            port = Convert.ToInt32(tbPort.Text);
            try
            {
                ipPoint = new IPEndPoint(IPAddress.Parse(address), port);
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // подключаемся к удаленному хосту
                socket.Connect(ipPoint);
                LoginSend();
                btnConnect.IsEnabled = false;
                btDisconnect.IsEnabled = true;
                tbUserName.IsEnabled = false;
                UsersRefresh();
            }
            catch (Exception exc)
            {
                ListDataAdd(exc.Message + "\n");
                return;
            }
        }

        private async void btSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cbPoluch.SelectedIndex == 0)
                {
                    StringBuilder sb = await ThreadSendReceiveAsync(AllConst + tbSend.Text);

                    ListDataAdd(sb.ToString() + "\n");
                }
                else
                {
                    StringBuilder sb = await ThreadSendReceiveAsync(NomerConst + cbPoluch.SelectedIndex.ToString() + RazdConst + tbSend.Text);

                    ListDataAdd(sb.ToString() + "\n");
                }

            }
            catch (Exception exc)
            {
                ListDataAdd(exc.Message + "\n");
                return;
            }
        }

        static StringBuilder ThreadSendReceive(string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            socket.Send(data);
            // получаем ответ
            data = new byte[4 * 1024]; // буфер для ответа
            StringBuilder builder = new StringBuilder();
            int bytes = 0; // количество полученных байт
            do
            {
                bytes = socket.Receive(data, data.Length, 0);
                builder.Append(Encoding.UTF8.GetString(data, 0, bytes));
            }
            while (socket.Available > 0);
            return builder;
        }

        // определение асинхронного метода
        static async Task<StringBuilder> ThreadSendReceiveAsync(string text)
        {
            return await Task.Run(() => ThreadSendReceive(text));
        }

        private void btClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btDisconnect_Click(object sender, RoutedEventArgs e)
        {
            // закрываем сокет
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            btnConnect.IsEnabled = true;
            btDisconnect.IsEnabled = false;
            tbUserName.IsEnabled = true;
        }

        public void ListDataAdd(string text)
        {
            var textBlockWork = new TextBlock();
            textBlockWork.Width = 450;
            textBlockWork.Height = 15;
            textBlockWork.Text = text;
            ListData.Items.Add(textBlockWork);

        }

        private void btUsersRefresh_Click(object sender, RoutedEventArgs e)
        {
            UsersRefresh();
        }

        public async void UsersRefresh()
        {
            try
            {
                List<string> usersXML = new List<string>();
                StringBuilder sb = await ThreadSendReceiveAsync(UsersListConst);

                ListDataAdd(sb.ToString() + "\n");

                XmlSerializer formatter = new XmlSerializer(typeof(List<string>));

                using (TextReader reader = new StringReader(sb.ToString()))
                {
                    usersXML = (List<string>)formatter.Deserialize(reader);
                }

                cbPoluch.Items.Clear();
                cbPoluch.Items.Add("Всем");
                foreach (var element in usersXML)
                {
                    cbPoluch.Items.Add(element);
                }
                cbPoluch.SelectedIndex = 0;

            }
            catch (Exception exc)
            {
                ListDataAdd(exc.Message + "\n");
                return;
            }
        }

        public async void LoginSend()
        {
            try
            {
                StringBuilder sb = await ThreadSendReceiveAsync(LoginConst + tbUserName.Text);

                ListDataAdd(sb.ToString() + "\n");
                Main.Title = "Номер вашего клиента = " + sb.ToString(); 

            }
            catch (Exception exc)
            {
                ListDataAdd(exc.Message + "\n");
                return;
            }
        }

    }
}
