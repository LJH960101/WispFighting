using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rankingPanel : MonoBehaviour {

	// Use this for initialization
	void Start() {  
        string receivedRanking = ClientNetworkManager.GetInstance().ReceiveRanking();
        receivedRanking = receivedRanking.Replace("@", "등-<size=20>");
        receivedRanking = receivedRanking.Replace("&", "</size>-<color=yellow>");
        receivedRanking = receivedRanking.Replace("$", "</color>\n");
        GetComponent<UnityEngine.UI.Text>().text = receivedRanking;
	}
	
}
