using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System;
using System.Net;
using UnityEngine.SceneManagement;
using System.Text;

public class ClientNetworkManager : MonoBehaviour
{
    const int CS_PORT = 42654;
    const int CC_PORT = 42655;
    public enum ClientNetworkState
    {
        Ready, // 매칭 미요청
        Destroy, // 서버로 넘어갈 때는 파괴
        Matching, // 매칭 요청 중
        InGame, // 인게임 진행 중
        EndGame // 게임 종료 화면
    }
    [HideInInspector] public ClientNetworkState currentState = ClientNetworkState.Ready;

    Socket udpClient;
    EndPoint targetEndPoint;
    Thread listnerThread;
    string name = "MyName";
    [HideInInspector] public float lastKda;

    List<byte[]> networkSendList;
    Queue<byte[]> networkReceivedList;
    private object listenerLocker = new object();
    [HideInInspector] public bool isWin;
    
    public void OnChangedName(string _name) { name = _name; }
    private void Awake()
    {
        networkSendList = new List<byte[]>();
        networkReceivedList = new Queue<byte[]>();
        DontDestroyOnLoad(this.gameObject);
    }
    public void ChangeState(int state)
    {
        ChangeState((ClientNetworkState)state);
    }
    void WriteLog(string log)
    {
        StreamWriter writer = new StreamWriter("Log.txt");
        writer.WriteLine(log);
        writer.Close();
    }
    public void ChangeState(ClientNetworkState state)
    {
        switch (state)
        {
            case ClientNetworkState.EndGame:
                Cursor.visible = true;
                if (listnerThread != null) listnerThread.Abort();
                if (udpClient != null) udpClient.Close();
                SceneManager.LoadScene("EndGame");
                break;
            case ClientNetworkState.Destroy:
                OnApplicationQuit();
                SceneManager.LoadScene("Server");
                Destroy(this);
                break;
            case ClientNetworkState.InGame:
                {
                    networkSendList.Clear();
                    networkReceivedList.Clear();
                    cnm = new ClientNetworkMessage();
                    cnm.first_id = 0;
                    cnm.first = cnm.second = cnm.third = null;

                    if (SceneManager.GetActiveScene().name != "InGame")  SceneManager.LoadScene("InGame");
                    if (listnerThread != null) listnerThread.Abort();
                    listnerThread = new Thread(new ThreadStart(InGameListner));
                    listnerThread.Start();
                }
                break;
            case ClientNetworkState.Matching:
                {
                    if (currentState == ClientNetworkState.Matching && udpClient!=null)
                    {
                        try
                        {
                            byte[] buf2 = new byte[1];
                            buf2[0] = (byte)CTSType.Disconnect;
                            udpClient.SendTo(buf2, targetEndPoint);
                        }
                        catch
                        {
                            udpClient = null;
                            ChangeState(ClientNetworkState.Matching);
                            return;
                        }
                    }
                    Cursor.visible = true;
                    targetIp = "";
                    // 서버 아이피를 받음
                    if (udpClient != null) udpClient.Close();
                    if (listnerThread != null) listnerThread.Abort();
                    if (SceneManager.GetActiveScene().name != "Matching") SceneManager.LoadScene("Matching");
                    if (SceneManager.GetSceneByName("Matching").GetRootGameObjects().Length > 0)
                    {
                        var objects = SceneManager.GetSceneByName("Matching").GetRootGameObjects();
                        Transform root_Matching = objects[0].transform;
                        foreach (var myObject in objects) if (myObject.transform.name == "Canvas") root_Matching = myObject.transform;
                        var mtext = root_Matching.transform.Find("MatchingText").GetComponent<UnityEngine.UI.Text>();
                        if (mtext != null) mtext.text = "매칭 중!";
                        var mtext2 = root_Matching.transform.Find("Button").Find("MatchingButtonText").GetComponent<UnityEngine.UI.Text>();
                        if (mtext2 != null) mtext2.text = "매칭 취소";
                    }

                    StreamReader reader = new StreamReader("server.txt");
                    var serverIp = reader.ReadLine();
                    reader.Close();
                    print(serverIp + "에 요청");

                    // 서버에 매칭 요청을 함.
                    IPEndPoint serverEndpoint_IP = new IPEndPoint(IPAddress.Parse(serverIp), CS_PORT);
                    targetEndPoint = (EndPoint)serverEndpoint_IP;
                    IPEndPoint Remote_IP = new IPEndPoint(IPAddress.Any, 0);
                    EndPoint remoteEndpoint = (EndPoint)Remote_IP;
                    udpClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    udpClient.ReceiveTimeout = 5000;
                    udpClient.SendTimeout = 5000;
                    byte[] buf = new byte[1];
                    buf[0] = (byte)CTSType.Matching;
                    udpClient.SendTo(buf, targetEndPoint);
                    byte[] data = new byte[1024];
                    try
                    {
                        udpClient.ReceiveFrom(data, ref remoteEndpoint);
                        if ((STCType)data[0] != STCType.Connected)
                        {
                            SceneManager.LoadScene("Aleart");
                        }
                        else
                        {
                            // 모든게 성공했다면 쓰레드에서 대기한다
                            listnerThread = new Thread(new ThreadStart(MatchingListner));
                            listnerThread.Start();
                        }
                    }
                    catch (Exception e)
                    {
                        print(e);
                        SceneManager.LoadScene("Aleart");
                    }
                }
                break;
            case ClientNetworkState.Ready:
                {
                    Cursor.visible = true;
                    if (currentState == ClientNetworkState.Matching && udpClient!=null)
                    {
                        byte[] buf = new byte[1];
                        buf[0] = (byte)CTSType.Disconnect;
                        udpClient.SendTo(buf, targetEndPoint);
                    }
                    if (listnerThread != null) listnerThread.Abort();
                    if (udpClient != null) udpClient.Close();
                    if (SceneManager.GetActiveScene().name != "Matching") SceneManager.LoadScene("Matching");

                    if (SceneManager.GetSceneByName("Matching").GetRootGameObjects().Length > 0)
                    {
                        var objects = SceneManager.GetSceneByName("Matching").GetRootGameObjects();
                        Transform root_Matching = objects[0].transform;
                        foreach (var myObject in objects) if (myObject.transform.name == "Canvas") root_Matching = myObject.transform;
                        var mtext = root_Matching.transform.Find("MatchingText").GetComponent<UnityEngine.UI.Text>();
                        if (mtext != null) mtext.text = "매칭 시작을 눌러주세요.";
                        var mtext2 = root_Matching.transform.Find("Button").Find("MatchingButtonText").GetComponent<UnityEngine.UI.Text>();
                        if (mtext2 != null) mtext2.text = "매칭 시작";
                    }
                    break;
                }
            default:
                print("Unexpected State");
                break;
        }
        currentState = state;
        print("상태 전환 : " + currentState);
    }
    public void OnPushedMatchingButton()
    {
        if(currentState == ClientNetworkState.Ready)
            ChangeState(ClientNetworkState.Matching);
        else
            ChangeState(ClientNetworkState.Ready);
    }
    void ConnectToPlayerServer()
    {
        IPEndPoint Target_IP = new IPEndPoint(IPAddress.Parse(targetIp), CC_PORT);
        EndPoint targetEp = (EndPoint)Target_IP;
        IPEndPoint Remote_IP2 = new IPEndPoint(IPAddress.Any, 0);
        EndPoint remoteEndpoint2 = (EndPoint)Remote_IP2;

        udpClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udpClient.ReceiveTimeout = 5000;
        udpClient.SendTimeout = 5000;
        targetEndPoint = targetEp;

        try
        {
            byte[] newConnectionBuf = new byte[1];
            newConnectionBuf[0] = (byte)CTCType.Connect;
            udpClient.SendTo(newConnectionBuf, targetEp);

            byte[] newConnectionOkay = new byte[1024];
            udpClient.ReceiveFrom(newConnectionOkay, ref remoteEndpoint2);
            targetEndPoint = remoteEndpoint2;
            if ((CTCType)newConnectionOkay[0] == CTCType.ConnectRequest)
            {
                ChangeState(ClientNetworkState.InGame);
                Invoke("SendTime", 5f);
            }
        }
        catch (Exception e)
        {
            ChangeState(ClientNetworkState.Matching);
            print("클라 매칭실패 : " + e.Message);
        }
    }
    void BindPlayerServer()
    {
        udpClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udpClient.ReceiveTimeout = 5000;
        udpClient.SendTimeout = 5000;

        try
        {
            IPEndPoint localIp = new IPEndPoint(IPAddress.Any, CC_PORT);
            EndPoint localEndPoint = (EndPoint)localIp;
            udpClient.Bind(localEndPoint);

            IPEndPoint Remote_IP2 = new IPEndPoint(IPAddress.Any, 0);
            EndPoint remoteEndpoint2 = (EndPoint)Remote_IP2;
            byte[] data = new byte[1024];
            udpClient.ReceiveFrom(data, ref remoteEndpoint2);
            if ((CTCType)data[0] == CTCType.Connect)
            {
                data = new byte[1];
                data[0] = (byte)CTCType.ConnectRequest;
                udpClient.SendTo(data, remoteEndpoint2);
                targetEndPoint = remoteEndpoint2;
                ChangeState(ClientNetworkState.InGame);
            }
        }
        catch (Exception e)
        {
            ChangeState(ClientNetworkState.Matching);
            print("클라 매칭실패 : " + e.Message);
        }
    }
    [HideInInspector] public string targetIp = "";
    void MatchingListner()
    {
		print("매칭 시작");
        while (true)
        {
            try
            {
                IPEndPoint Remote_IP = new IPEndPoint(IPAddress.Any, 0);
                EndPoint remoteEndpoint = (EndPoint)Remote_IP;
                byte[] buf = new byte[1024];
                udpClient.ReceiveFrom(buf, ref remoteEndpoint);
                if (buf.Length > 0)
                {
                    if ((STCType)buf[0] == STCType.Mathched_C)
                    {
                        // 클라로 매칭됬다면 연결처리.
                        udpClient.Close();

                        byte[] stringBuf = new byte[buf.Length - 1];
                        for (int i = 0; i < buf.Length - 1; ++i) stringBuf[i] = buf[i + 1];
                        lock (listenerLocker)
                        {
                            targetIp = Encoding.Default.GetString(stringBuf);
                        }
                        listnerThread = null;
                        break;
                    }
                    else if ((STCType)buf[0] == STCType.Mathched_S)
                    {
                        // 서버로 매칭됬다면 바인드 처리.
                        udpClient.Close();
                        lock (listenerLocker)
                        {
                            targetIp = "0";
                        }
                        listnerThread = null;
                        break;
                    }
                }
            }
            catch
            {
				print("매칭 오류 발생!");
                matchingFlag = true;
				break;
            }
        }
		print("매칭 정상 종료!");
    }
    void InGameListner()
    {
        int counter = 0;
        print("Start");
        while (true)
        {
            byte[] buf = new byte[1024];
            try { 
                udpClient.ReceiveFrom(buf, ref targetEndPoint);
                if (buf.Length > 0)
                {
                    // CNM에서 현재 아이디와 일치하는 정보를 빼온다. 누락됬다면 몽땅 처리해버림.
                    ClientNetworkMessage cnm = MyTool.BytesToStruct<ClientNetworkMessage>(buf);
                    while (counter != cnm.first_id + 1) // 마지막(첫번째) 데이터와 일치할때까지
                    {
                        byte[] currentBuf;
                        if (cnm.first_id == counter)
                        {
                            ++counter;
                            currentBuf = cnm.first;
                        }
                        else if (cnm.first_id + 1 == counter)
                        {
                            currentBuf = cnm.second;
                            ++counter;
                        }
                        else if (cnm.first_id + 2 == counter)
                        {
                            ++counter;
                            currentBuf = cnm.third;
                        }
                        else
                        {
                            counter = cnm.first_id + 1;
                            currentBuf = cnm.third;
                        }
                        lock (listenerLocker)
                        {
                            networkReceivedList.Enqueue(currentBuf);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                print("리스너 폭팔 : " + e.Message);
                refreshThread = true;
                break;
            }
        }
        print("Exit");
    }
    bool refreshThread = false;
    void OnApplicationQuit()
    {
        if (currentState == ClientNetworkState.Matching)
        {
            byte[] buf = new byte[1];
            buf[0] = (byte)CTSType.Disconnect;
            udpClient.SendTimeout = 5000;
            udpClient.SendTo(buf, targetEndPoint);
        }
        else if (currentState == ClientNetworkState.InGame)
        {
            byte[] buf = new byte[1];
            buf[0] = (byte)CTCType.Disconnect;
            udpClient.SendTimeout = 5000;
            udpClient.SendTo(buf, targetEndPoint);
        }
        if (udpClient != null) udpClient.Close();
        if (listnerThread != null) listnerThread.Abort();
    }
    public void ClearSendList()
    {
        networkSendList.Clear();
    }
    public void SendAttack(CharacterType type, int damage)
    {
        CM_AttackSuccess myMsg = new CM_AttackSuccess();
        myMsg.type = type;
        myMsg.damage = damage;
        byte[] newMsg = MyTool.StructToBytes<CM_AttackSuccess>(myMsg);
        byte[] newBuf = new byte[newMsg.Length+1];
        newBuf[0] = (byte)CTCType.AttackSuccess;
        for (int i = 0; i < newMsg.Length; ++i) newBuf[i + 1] = newMsg[i];
        networkSendList.Add(newBuf);
    }
    public void SendTime()
    {
        if (currentState != ClientNetworkState.InGame) return;
        CM_SendTime myMsg = new CM_SendTime();
        myMsg.timer = GameMain.GetInstance().timer;
        myMsg.dateTime = DateTime.Now.ToString();
        byte[] newMsg = MyTool.StructToBytes<CM_SendTime>(myMsg);
        byte[] newBuf = new byte[newMsg.Length + 1];
        newBuf[0] = (byte)CTCType.SendTime;
        for (int i = 0; i < newMsg.Length; ++i) newBuf[i + 1] = newMsg[i];
        networkSendList.Add(newBuf);
        Invoke("SendTime", 5f);
    }
    public void SendDeath(CharacterType type, Vector3 position)
    {
        CM_NoticeDeath myMsg = new CM_NoticeDeath();
        myMsg.characterType = type;
        myMsg.position = position;
        byte[] newMsg = MyTool.StructToBytes<CM_NoticeDeath>(myMsg);
        byte[] newBuf = new byte[newMsg.Length + 1];
        newBuf[0] = (byte)CTCType.NoticeDeath;
        for (int i = 0; i < newMsg.Length; ++i) newBuf[i + 1] = newMsg[i];
        networkSendList.Add(newBuf);
    }
    public void SendUseSkill(int type, Vector3 position, Quaternion rotation)
    {
        CM_UseSkill myMsg = new CM_UseSkill();
        myMsg.skillType = type;
        myMsg.position = position;
        myMsg.rotation = rotation;
        byte[] newMsg = MyTool.StructToBytes<CM_UseSkill>(myMsg);
        byte[] newBuf = new byte[newMsg.Length + 1];
        newBuf[0] = (byte)CTCType.UseSkill;
        for (int i = 0; i < newMsg.Length; ++i) newBuf[i + 1] = newMsg[i];
        networkSendList.Add(newBuf);
    }
    public void SendPosition(Vector3 position, Vector3 velocity)
    {
        CM_UpdatePosition myMsg = new CM_UpdatePosition();
        myMsg.position = position;
        myMsg.velocity = velocity;
        byte[] newMsg = MyTool.StructToBytes<CM_UpdatePosition>(myMsg);
        byte[] newBuf = new byte[newMsg.Length + 1];
        newBuf[0] = (byte)CTCType.UpdatePosition;
        for (int i = 0; i < newMsg.Length; ++i) newBuf[i + 1] = newMsg[i];
        networkSendList.Add(newBuf);
    }
    public string ReceiveRanking()
    {
        try
        {
            StreamReader reader = new StreamReader("server.txt");
            var serverIp = reader.ReadLine();
            reader.Close();

            // 서버에 랭킹 요청을 함.
            IPEndPoint serverEndpoint_IP = new IPEndPoint(IPAddress.Parse(serverIp), CS_PORT);
            EndPoint targetEndPoint = (EndPoint)serverEndpoint_IP;
            IPEndPoint Remote_IP = new IPEndPoint(IPAddress.Any, 0);
            EndPoint remoteEndpoint = (EndPoint)Remote_IP;
            Socket udpClient_server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpClient_server.ReceiveTimeout = 5000;
            udpClient_server.SendTimeout = 5000;
            byte[] newBuf = new byte[1];
            newBuf[0] = (byte)CTSType.SelectRanking;
            udpClient_server.SendTo(newBuf, targetEndPoint);
            byte[] data = new byte[1024];
            udpClient_server.ReceiveFrom(data, ref remoteEndpoint);
            if((STCType)data[0] == STCType.SelectRanking)
            {
                byte[] realData = new byte[data.Length - 1];
                for (int i = 0; i < data.Length - 1; ++i) realData[i] = data[i + 1];
                udpClient_server.Close();
                return Encoding.Default.GetString(realData);
            }
            udpClient_server.Close();
        }
        catch (Exception e)
        {
            print(e.Message);
            return "서버가 DB를 지원하지 않습니다.";
        }
        return "서버가 DB를 지원하지 않습니다.";
    }
    public void SendRanking()
    {
        StreamReader reader = new StreamReader("server.txt");
        var serverIp = reader.ReadLine();
        reader.Close();

        // 서버에 매칭 요청을 함.
        IPEndPoint serverEndpoint_IP = new IPEndPoint(IPAddress.Parse(serverIp), CS_PORT);
        EndPoint targetEndPoint = (EndPoint)serverEndpoint_IP;
        Socket udpClient_server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udpClient_server.ReceiveTimeout = 5000;
        udpClient_server.SendTimeout = 5000;
        CM_UpdateRanking myMsg = new CM_UpdateRanking();
        myMsg.name = name;
        myMsg.score = lastKda;
        byte[] newMsg = MyTool.StructToBytes<CM_UpdateRanking>(myMsg);
        byte[] newBuf = new byte[newMsg.Length + 1];
        newBuf[0] = (byte)CTSType.UpdateRanking;
        for (int i = 0; i < newMsg.Length; ++i) newBuf[i + 1] = newMsg[i];
        udpClient_server.SendTo(newBuf, targetEndPoint);
        udpClient_server.Close();
    }
    private void Update()
    {
        if(listnerThread!=null) print(listnerThread.ThreadState + "상태");
        switch (currentState)
        {
            case ClientNetworkState.Matching:
                if (targetIp.Length != 0)
                {
                    string newTartget;
                    lock (listenerLocker)
                    {
                        newTartget = targetIp;
                    }
                    if (newTartget == "0") BindPlayerServer();
                    else ConnectToPlayerServer();
                }
                break;
            case ClientNetworkState.InGame:
                // 메세지큐 수신 처리
                {
                    while (networkReceivedList.Count > 0)
                    {
                        byte[] recievedBuf;
                        lock (listenerLocker)
                        {
                            recievedBuf = networkReceivedList.Dequeue();
                        }
                        switch ((CTCType)recievedBuf[0])
                        {
                            case CTCType.AttackSuccess:
                                {
                                    byte[] realBuf = new byte[recievedBuf.Length - 1];
                                    for (int i = 0; i < recievedBuf.Length - 1; ++i) realBuf[i] = recievedBuf[i + 1];
                                    CM_AttackSuccess msg = MyTool.BytesToStruct<CM_AttackSuccess>(realBuf);
                                    GameMain.GetInstance().Attack((msg.type == CharacterType.Local ? CharacterType.Enemy : CharacterType.Local), msg.damage, true);
                                }
                                break;
                            case CTCType.SendTime:
                                {
                                    byte[] realBuf = new byte[recievedBuf.Length - 1];
                                    for (int i = 0; i < recievedBuf.Length - 1; ++i) realBuf[i] = recievedBuf[i + 1];
                                    CM_SendTime msg = MyTool.BytesToStruct<CM_SendTime>(realBuf);
                                    GameMain.GetInstance().timer = msg.timer - (float)(DateTime.Now - DateTime.Parse(msg.dateTime)).TotalSeconds;
                                }
                                break;
                            case CTCType.Disconnect:
                                {
                                    ChangeState(ClientNetworkState.Matching);
                                    return;
                                }
                                break;
                            case CTCType.NoticeDeath:
                                {
                                    byte[] realBuf = new byte[recievedBuf.Length - 1];
                                    for (int i = 0; i < recievedBuf.Length - 1; ++i) realBuf[i] = recievedBuf[i + 1];
                                    CM_NoticeDeath msg = MyTool.BytesToStruct<CM_NoticeDeath>(realBuf);
                                    GameMain.GetInstance().Death((msg.characterType == CharacterType.Local ? CharacterType.Enemy : CharacterType.Local), msg.position, true);
                                }
                                break;
                            case CTCType.UpdatePosition:
                                {
                                    byte[] realBuf = new byte[recievedBuf.Length - 1];
                                    for (int i = 0; i < recievedBuf.Length - 1; ++i) realBuf[i] = recievedBuf[i + 1];
                                    CM_UpdatePosition msg = MyTool.BytesToStruct<CM_UpdatePosition>(realBuf);
                                    Enemy.GetInstance().UpdatePosition(msg.position, msg.velocity);
                                }
                                break;
                            case CTCType.UseSkill:
                                {
                                    byte[] realBuf = new byte[recievedBuf.Length - 1];
                                    for (int i = 0; i < recievedBuf.Length - 1; ++i) realBuf[i] = recievedBuf[i + 1];
                                    CM_UseSkill msg = MyTool.BytesToStruct<CM_UseSkill>(realBuf);
                                    LocalCharacter.GetInstance().InstantiateMissle(msg.skillType, msg.position, msg.rotation, true);
                                }
                                break;
                        }
                    }
                }

                // 메세지 전송 처리
                {
                    for (int i = 0; i < networkSendList.Count; ++i)
                    {
                        SendToServer(networkSendList[i]);
                    }
                    networkSendList.Clear();
                }
                break;
        }
        if (matchingFlag)
        {
            if (currentState!=ClientNetworkState.Ready) ChangeState(ClientNetworkState.Matching);
            matchingFlag = false;
        }
        if(refreshThread && listnerThread.ThreadState == ThreadState.Stopped)
        {
            listnerThread.Start();
            refreshThread = true;
        }
    }
    bool matchingFlag = false;
    ClientNetworkMessage cnm;
    private void SendToServer(byte[] msg)
    {
        cnm.third = cnm.second;
        cnm.second = cnm.first;
        cnm.first = msg;
        cnm.first_id = (cnm.first_id + 1 > 100000) ? 1 : cnm.first_id + 1;
        udpClient.SendTo(MyTool.StructToBytes<ClientNetworkMessage>(cnm), targetEndPoint);
    }

    private static ClientNetworkManager instance;
    public static ClientNetworkManager GetInstance()
    {
        if (!instance)
        {
            instance = GameObject.FindObjectOfType<ClientNetworkManager>();
            if (!instance)
                Debug.LogError("There needs to be one active ClientNetworkManager script on a GameObject in your scene.");
        }

        return instance;
    }
}
