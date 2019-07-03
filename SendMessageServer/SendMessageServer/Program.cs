using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SendMessageServer
{
    class Program
    {
        const string LoginConst = "Login";          // send
        const string UsersListConst = "UsersList";  // send, receive
        const string AllConst = "All";              // send, receive
        const string NomerConst = "Nomer";          // send, receive
        const string RazdConst = ";";               // send, receive

        public static Thread SrvThread;
        public static Socket socServer;
        static List<string> usersList;
        static List<object> socketsList = new List<object>();

        static void Main(string[] args)
        {
            //// сериализация в файл
            //using (FileStream fs = new FileStream("streets.xml", FileMode.Create))
            //{
            //    formatter.Serialize(fs, streetsNew);
            //}


            socServer =
                new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            string srvAddress = "0.0.0.0";
            int srvPort = 12345;
            socServer.Bind(new
            IPEndPoint(IPAddress.Parse(srvAddress), srvPort));
            socServer.Listen(100);
            // далее должна быть команда Accept()
            SrvThread = new Thread(ServerThreadRoutine);
            //SrvThread.IsBackground = true;
            SrvThread.Start(socServer);


            while (true)
            {
                Socket client = socServer.Accept();

                Console.WriteLine("Клиент подключен: ");
                Console.WriteLine(
                   client.RemoteEndPoint.ToString());
                Console.WriteLine("socketList.Count: " + socketsList.Count);
                ThreadPool.QueueUserWorkItem(
                  ClientThreadProc, client);
            }
        }

        static void ServerThreadRoutine(object obj)
        {
            TcpListener srvSock = obj as TcpListener;
            // синхронный вариант сервера
            try
            {
                while (true)
                {
                    // не ассинхронной блокирующий вызов Accept()
                    // работа с клиентом в отдельном потоке
                    TcpClient client = srvSock.AcceptTcpClient();
                    //   запуск клиентского потока -
                    ThreadPool.QueueUserWorkItem(
                        ClientThreadProc, client);
                }
            }
            catch
            {
                return;
            }
        }


        // поток обслуживания удаленного клиента
        static void ClientThreadProc(object obj)
        {
            // протокол работы сервера - эхо-сервер
            Socket client = (Socket)obj;// as Socket;
            string index, message;
            try
            {
                while (true)
                {
                    // получаем ответ
                    byte[] data = new byte[4 * 1024]; // буфер для ответа
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0; // количество полученных байт
                    int socketNomer;

                    do
                    {
                        bytes = client.Receive(data, data.Length, 0);
                        builder.Append(Encoding.UTF8.GetString(data, 0, bytes));
                    }
                    while (client.Available > 0);

                    // отправляем ответ
                    index = builder.ToString();
                    //                    var findStreet = streets.Where(t => t.Index == index); ;
                    message = "";
                    if (index.StartsWith(LoginConst) == true)
                    {
                        //                        message = index.Substring(5);
                        message = "Login - " + index.Substring(5);
                        socketsList.Add(client);
                        //                        usersList.Add(index.Substring(5));

                        socketNomer = -1;
                        foreach (var t in socketsList)
                        {
                            if (t == client)
                            {
                                socketNomer = socketsList.IndexOf(t);
                                Console.WriteLine($"Найден {socketsList.IndexOf(t)}");

                                break;

                            }

                        }
                        //usersServer.Add(new UserServer()
                        //{
                        //    Nomer = 1,
                        //    Name = "User1",
                        //    UserEndPoint= client.RemoteEndPoint
                        //});

                        message = (socketNomer + 1).ToString();
                        data = Encoding.UTF8.GetBytes(message);
                        client.Send(data);

                    }

                    else if (index.StartsWith(UsersListConst) == true)
                    {
                        string usersXML_list;
                        List<string> usersXML = new List<string>();

                        int i = 0;
                        foreach (var t in socketsList)
                        {
                            i++;
                            //socketNomer = socketsList.IndexOf(t);
                            //usersXML.Add("Клиент № " + socketNomer.ToString());
                            socketNomer = socketsList.IndexOf(t);
                            usersXML.Add("Клиент № " + (socketNomer+1).ToString());
                        }

                        XmlSerializer formatter = new XmlSerializer(typeof(List<string>));

                        usersXML_list = "";
                        using (StringWriter textWriter = new StringWriter())
                        {
                            formatter.Serialize(textWriter, usersXML);
                            usersXML_list = textWriter.ToString();
                        }
                        message = usersXML_list;
                        data = Encoding.UTF8.GetBytes(message);
                        client.Send(data);
                        Console.WriteLine("Отправлено - " + message);
                    }
                    else if (index.StartsWith(AllConst) == true)
                    {
                        message = index.Substring(AllConst.Length);
                        data = Encoding.UTF8.GetBytes(message);

                        foreach (var t in socketsList)
                        {
                            Socket s = (Socket)t;
                            s.Send(data);
                            Console.WriteLine("Отправлено - " + message);
                        }

                    }
                    else if (index.StartsWith(NomerConst) == true)
                    {

                        string stroka;
                        int ind, nomer;
                        stroka = index.Substring(NomerConst.Length);
                        ind =stroka.IndexOf(RazdConst);
                        Console.WriteLine("stroka - " + stroka);
                        Console.WriteLine("ind - " + ind.ToString());
                        nomer = Int32.Parse(stroka.Remove(ind));
                        message = stroka.Substring(ind + 1);
                        Console.WriteLine("nomer - " + nomer.ToString());

                        data = Encoding.UTF8.GetBytes(message);

                            var t = socketsList[nomer - 1];
                            Socket s = (Socket) t;
                            s.Send(data);
                            Console.WriteLine("Отправлено - " + message);

                    }
                    else
                    {
                        data = Encoding.UTF8.GetBytes(message);
                        client.Send(data);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR:" + ex.Message);
            }
            client.Shutdown(SocketShutdown.Both);
            client.Close();

        }
    }
}
