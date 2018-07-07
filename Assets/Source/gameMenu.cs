using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class gameMenu : MonoBehaviour
{
    public void GotoServer()
    {
        ClientNetworkManager.GetInstance().ChangeState(ClientNetworkManager.ClientNetworkState.Destroy);
    }
    public void GotoMatching()
    {
        ClientNetworkManager.GetInstance().ChangeState(ClientNetworkManager.ClientNetworkState.Ready);
    }
}
