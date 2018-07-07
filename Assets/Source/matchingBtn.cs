using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class matchingBtn : MonoBehaviour {
    public void Start()
    {
        if (ClientNetworkManager.GetInstance().currentState == ClientNetworkManager.ClientNetworkState.Ready)
        {
            var mtext = GameObject.Find("Canvas").transform.Find("MatchingText").GetComponent<UnityEngine.UI.Text>();
            if (mtext != null) mtext.text = "매칭 시작을 눌러주세요.";
            var mtext2 = GameObject.Find("Canvas").transform.Find("Button").Find("MatchingButtonText").GetComponent<UnityEngine.UI.Text>();
            if (mtext2 != null) mtext2.text = "매칭 시작";
        }
        else
        {
            var mtext = GameObject.Find("Canvas").transform.Find("MatchingText").GetComponent<UnityEngine.UI.Text>();
            if (mtext != null) mtext.text = "매칭 중!";
            var mtext2 = GameObject.Find("Canvas").transform.Find("Button").Find("MatchingButtonText").GetComponent<UnityEngine.UI.Text>();
            if (mtext2 != null) mtext2.text = "매칭 취소";
        }
    }
    public void OnPushed()
    {
        ClientNetworkManager.GetInstance().OnPushedMatchingButton();
    }
}
