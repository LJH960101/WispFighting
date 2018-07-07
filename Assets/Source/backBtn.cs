using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class backBtn : MonoBehaviour
{
    void Start()
    {
        if(ClientNetworkManager.GetInstance().isWin) GameObject.Find("Canvas").transform.Find("MatchingText").GetComponent<UnityEngine.UI.Text>().text = "<color=green>승리!</color>\n" + ClientNetworkManager.GetInstance().lastKda + "점";
        else GameObject.Find("Canvas").transform.Find("MatchingText").GetComponent<UnityEngine.UI.Text>().text = "<color=red>패배!</color>\n" + ClientNetworkManager.GetInstance().lastKda + "점";
    }
    public void OnPushed()
    {
        ClientNetworkManager.GetInstance().ChangeState(ClientNetworkManager.ClientNetworkState.Ready);
    }
}
