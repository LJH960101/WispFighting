using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Data;
using MySql.Data;
using MySql.Data.MySqlClient;
using System;
using System.Text;

public class MyServerScript : MonoBehaviour {
    enum ServerState
    {
        Ready,
        Starting,
        End
    };
    struct NetworkData
    {
        public EndPoint ep;
        public byte[] bytes;
    };

    const int PORT = 42654;
    ServerState currentState = ServerState.Ready;

    // SQL 관련 변수
    MySqlConnection sqlconn = null;
    private string sqlDBip = "127.0.0.1";
    private string sqlDBname = "MFFE";
    private string sqlDBid = "root";
    private string sqlDBpw = "1234";

    string logs = "";
    Socket udpSocket;
    List<EndPoint> matchingReadyClients;
    Queue<NetworkData> messageQueue;
    Thread listenerThread;

    private object listenerLocker = new object();

    public void OnChangedId(string id) { sqlDBid = id; }
    public void OnChangedPw(string pw) { sqlDBpw = pw; }
    void Print(string log)
    {
        logs = log + "\n" + logs;
        transform.Find("Log").GetComponent<UnityEngine.UI.Text>().text = logs;
    }
    void Start ()
    {
        // 여러 컨테이너를 초기화 하고 소켓을 설치.
        matchingReadyClients = new List<EndPoint>();
        udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPEndPoint localIp = new IPEndPoint(IPAddress.Any, PORT);
        EndPoint localEndPoint = (EndPoint)localIp;
        udpSocket.Bind(localEndPoint);
        messageQueue = new Queue<NetworkData>();
        CheckMySql();

        // 리스너 쓰레드를 돌림
        currentState = ServerState.Starting;
        listenerThread = new Thread(new ThreadStart(NetworkListener));
        listenerThread.Start();
    }
    public void CheckMySql()
    {
        //DB정보 입력
        string sqlDatabase = "Server=" + sqlDBip + ";Database=" + sqlDBname + ";UserId=" + sqlDBid + ";Password=" + sqlDBpw + "";
        //접속 확인하기
        try
        {
            sqlconn = new MySqlConnection(sqlDatabase);
            sqlconn.Open();

            DataTable dt = new DataTable(); //데이터 테이블을 선언함

            MySqlDataAdapter adapter = new MySqlDataAdapter("select * from MYRANK", sqlconn);
            adapter.Fill(dt); //데이터 테이블에  채워넣기를함

            sqlconn.Close();
            transform.Find("DBState").GetComponent<UnityEngine.UI.Text>().text = "DB상태 : <color=green>접속성공</color>";
        }
        catch(Exception e)
        {
            Print("DB 접속 실패 : " + e.Message);
            transform.Find("DBState").GetComponent<UnityEngine.UI.Text>().text = "DB상태 : <color=red>접속실패</color>";
        }
    }
    public void CreateTable()
    {
        //DB정보 입력
        string sqlDatabase = "Server=" + sqlDBip + ";UserId=" + sqlDBid + ";Password=" + sqlDBpw + "";
        //접속 확인하기
        try
        {
            sqlconn = new MySqlConnection(sqlDatabase);
            sqlconn.Open();

            MySqlCommand dbcmd = new MySqlCommand("Create Database " + sqlDBname + ";", sqlconn); //명령어를 커맨드에 입력
            dbcmd.ExecuteNonQuery(); //명령어를 SQL에 보냄

            dbcmd.CommandText = "create table " + sqlDBname + ".MYRANK ( id varchar(20), kda float, DateCreated DATETIME DEFAULT NOW());";
            dbcmd.ExecuteNonQuery(); //명령어를 SQL에 보냄

            sqlconn.Close();
            CheckMySql();
        }
        catch (Exception e)
        {
            Print("DB 접속 실패 : " + e.Message);
        }
    }
    void OnApplicationQuit()
    {
        if (listenerThread != null) listenerThread.Abort();
        if(udpSocket!=null) udpSocket.Close();
    }
    void NetworkListener()
    {
        IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, PORT);
        EndPoint remoteEP = (EndPoint)clientEndPoint;
        byte[] receive_byte_array = new byte[1024];
        int recv;
        while (true)
        {
            try
            {
                print(1);
                recv = udpSocket.ReceiveFrom(receive_byte_array, ref remoteEP);
                NetworkData newData = new NetworkData();
                newData.bytes = receive_byte_array;
                newData.ep = remoteEP;
                if (recv == 0) continue;
                lock (listenerLocker)
                {
                    messageQueue.Enqueue(newData);
                }
                clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                remoteEP = new IPEndPoint(IPAddress.Any, 0);
                receive_byte_array = new byte[1024];
            }
            catch(Exception e)
            {
                print(2);
                dieFlag = true;
                break;
            }
        }
    }
    bool dieFlag = false;
    void Update () {
        while (messageQueue.Count != 0) { // 네트워크 메세지를 처리함.
            if (messageQueue.Count > 0)
            {
                NetworkData receivedData;
                lock(listenerLocker)
                {
                    receivedData = messageQueue.Dequeue();
                }
                // 네트워크 처리
                if ((CTSType)receivedData.bytes[0] == CTSType.Matching)
                {
                    for(int i = matchingReadyClients.Count - 1; i >= 0; --i)
                    {
                        if (MyTool.EndPointToIp(matchingReadyClients[i]) == MyTool.EndPointToIp(receivedData.ep))
                        {
                            matchingReadyClients.Remove(matchingReadyClients[i]);
                        }
                    }
                    matchingReadyClients.Add(receivedData.ep);
                    byte[] buf = new byte[1];
                    buf[0] = (byte)STCType.Connected;
                    udpSocket.SendTo(buf, receivedData.ep);
                    Print("매칭 요청 성공, 매칭 중인 유저 수 : " + matchingReadyClients.Count + " " + MyTool.EndPointToIp(receivedData.ep));
                }
                else if ((CTSType)receivedData.bytes[0] == CTSType.Disconnect)
                {
                    matchingReadyClients.Remove(receivedData.ep);
                    Print("매칭 해제 성공");
                }
                else if ((CTSType)receivedData.bytes[0] == CTSType.UpdateRanking)
                {
                    byte[] buf = new byte[receivedData.bytes.Length - 1];
                    for (int i = 0; i < receivedData.bytes.Length - 1; ++i) buf[i] = receivedData.bytes[i + 1];
                    CM_UpdateRanking rankingRequest = MyTool.BytesToStruct<CM_UpdateRanking>(buf);
                    // 랭킹을 등록 시켜줌
                    {
                        //DB정보 입력
                        string sqlDatabase = "Server=" + sqlDBip + ";Database=" + sqlDBname + ";UserId=" + sqlDBid + ";Password=" + sqlDBpw + "";
                        //접속 확인하기
                        try
                        {
                            sqlconn = new MySqlConnection(sqlDatabase);
                            sqlconn.Open();

                            MySqlCommand dbcmd = new MySqlCommand("insert into myrank (id, kda) values ('" + rankingRequest.name + "', " + rankingRequest.score + ");", sqlconn); //명령어를 커맨드에 입력
                            dbcmd.ExecuteNonQuery(); //명령어를 SQL에 보냄

                            sqlconn.Close();
                            Print("랭킹 등록 : " + rankingRequest.name + " " + rankingRequest.score);
                        }
                        catch (Exception e)
                        {
                            Print("DB 접속 실패 : " + e.Message);
                            transform.Find("DBState").GetComponent<UnityEngine.UI.Text>().text = "DB상태 : <color=red>접속실패</color>";
                        }
                    }
                }
                else if ((CTSType)receivedData.bytes[0] == CTSType.SelectRanking)
                {
                    // 랭킹을 조회 시켜줌
                    {
                        //DB정보 입력
                        string sqlDatabase = "Server=" + sqlDBip + ";Database=" + sqlDBname + ";UserId=" + sqlDBid + ";Password=" + sqlDBpw + "";
                        //접속 확인하기
                        try
                        {

                            sqlconn = new MySqlConnection(sqlDatabase);
                            sqlconn.Open();

                            DataTable dt = new DataTable(); //데이터 테이블을 선언함
                            MySqlCommand cmd = new MySqlCommand("select id, kda from myrank order by kda desc limit 10;", sqlconn);                            MySqlDataReader rdr = cmd.ExecuteReader();

                            string newString = "";                            int count = 1;                            while (rdr.Read())
                            {
                                newString += count + "@" + rdr["id"] + "&" + int.Parse(rdr["kda"]+"") + "$";
                                count++;
                            }
                            rdr.Close();
                            sqlconn.Close();

                            byte[] stringBuf = Encoding.UTF8.GetBytes(newString);
                            byte[] sendBuf = new byte[stringBuf.Length + 1];
                            sendBuf[0] = (byte)STCType.SelectRanking;
                            for (int i = 0; i < stringBuf.Length; ++i) sendBuf[i + 1] = stringBuf[i];
                            udpSocket.SendTo(sendBuf, receivedData.ep);

                            Print("랭킹 조회 : " + newString);
                        }
                        catch (Exception e)
                        {
                            Print("DB 접속 실패 : " + e.Message);
                            transform.Find("DBState").GetComponent<UnityEngine.UI.Text>().text = "DB상태 : <color=red>접속실패</color>";
                        }
                    }
                }
            }
        }
        while (matchingReadyClients.Count >= 2) // 리스트의 0번과 1번을 매칭 시켜줌.
        {
            // 1의 정보를 0에 전송
            {
                byte[] newBuf = new byte[1];
                newBuf[0] = (byte)STCType.Mathched_S;
                udpSocket.SendTo(newBuf, matchingReadyClients[0]);
            }
            // 0의 정보를 1에 전송
            {
                byte[] newMsg = Encoding.UTF8.GetBytes(MyTool.EndPointToIp(matchingReadyClients[0]));
                byte[] newBuf = new byte[newMsg.Length + 1];
                newBuf[0] = (byte)STCType.Mathched_C;
                for (int i = 0; i < newMsg.Length; ++i) newBuf[i + 1] = newMsg[i];
                udpSocket.SendTo(newBuf, matchingReadyClients[1]);
            }
            matchingReadyClients.Remove(matchingReadyClients[1]);
            matchingReadyClients.Remove(matchingReadyClients[0]);
            Print("매칭 성사 !, 매칭 중인 유저 수 : " + matchingReadyClients.Count);
        }
        if (dieFlag)
        {
            Print("쓰레드 재실행!");
            listenerThread = new Thread(new ThreadStart(NetworkListener));
            listenerThread.Start();
            dieFlag = false;
        }
    }
}