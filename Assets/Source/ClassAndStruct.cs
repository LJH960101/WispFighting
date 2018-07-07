using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using System.Net;

public enum CharacterType { Local=0, Enemy=1 };
public enum CTCType // 인게임 통신 Client<->Client
{
    Connect,        //연결 요청 <-> ConnectRequest
    ConnectRequest, //연결 응답 <-> Connect
    AttackSuccess,  //타격 통보
    NoticeDeath,    //죽음 통보
    UseSkill,       //미사일 통보
    UpdatePosition, //위치 갱신 통보
    Disconnect,     //단절 통보
    SendTime        //시간 동기화
}
public enum STCType // 매칭 통신 Server->Client
{
    Connected,      //매칭 수락 통보 <-> Matching
    Mathched_C,     //클라이언트로 매칭 성공 통보
    Mathched_S,     //서버로 매칭 성공 통보
    SelectRanking   //랭킹 조회 정보
}
public enum CTSType // 매칭 통신 Client->Server
{
    Matching,       //매칭 요청 <-> Connected
    Disconnect,     //단절 통보
    UpdateRanking,  //랭킹 갱신 통보
    SelectRanking   //랭킹 조회 요청
}
[StructLayout(LayoutKind.Sequential)]
public struct ClientNetworkMessage // 정보를 3중으로 날려서 소실을 막음.
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 70)] public byte[] first;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst=70)] public byte[] second;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst=70)] public byte[] third;
    public int first_id;
}
[StructLayout(LayoutKind.Sequential)]
public struct CM_UpdateRanking // 등록할 랭킹 정보
{
    public float score;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)] public string name;
}
[StructLayout(LayoutKind.Sequential)]
public struct CM_SendTime // 시간 동기화 정보
{
    public float timer;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 25)] public string dateTime;
}
public struct CM_AttackSuccess // 타격 통보
{
    public CharacterType type;
    public int damage;
}
public struct CM_NoticeDeath // 죽음 통보
{
    public CharacterType characterType;
    public Vector3 position;
}
public struct CM_UseSkill // 스킬 사용 통보
{
    public int skillType;
    public Vector3 position;
    public Quaternion rotation;
}
public struct CM_UpdatePosition // 위치 갱신 통보
{
    public Vector3 position;
    public Vector3 velocity;
}
public class MyTool
{
    public static string EndPointToIp(EndPoint ep) { return (((IPEndPoint)ep).Address).ToString(); }

    public static byte[] StructToBytes<T>(T str)  // 클래스를 byte배열로 바꾼다
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);
        return arr;
    }
    
    public static T BytesToStruct<T>(byte[] arr) where T : new() //byte배열을 클래스로 바꾼다.
    {
        T str = new T();

        int size = Marshal.SizeOf(str);
        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.Copy(arr, 0, ptr, size);

        str = (T)Marshal.PtrToStructure(ptr, str.GetType());
        Marshal.FreeHGlobal(ptr);

        return str;
    }
}