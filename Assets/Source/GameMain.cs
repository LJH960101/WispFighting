using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMain : MonoBehaviour
{
    public UnityEngine.UI.Text timerText;
    public UnityEngine.UI.Image timerImage;
    [HideInInspector] public float timer;
    public enum GameState
    {
        OnGame,
        EndGame
    }
    [HideInInspector] public GameState currentState = GameState.OnGame;

    void EndGame() {
		ClientNetworkManager.GetInstance().lastKda = kill - death/2;
        if(kill>death) ClientNetworkManager.GetInstance().isWin = true;
        else ClientNetworkManager.GetInstance().isWin = false;
        ClientNetworkManager.GetInstance().SendRanking();
        ClientNetworkManager.GetInstance().ChangeState(ClientNetworkManager.ClientNetworkState.EndGame);
        Cursor.visible = true;
    }
    private void Update()
    {
        switch (currentState)
        {
            case GameState.OnGame:
                timer -= Time.deltaTime;
                timerText.text = (int)Mathf.Clamp(timer, 0f, 60f) + "";
                timerImage.fillAmount = timer / 60f;
                if (timer < 0) EndGame();
                break;
        }
    }
    int kill, death;
	void Start () {
        timer = 60.0f;
        currentState = GameState.OnGame;
    }
    public void Attack(CharacterType type, int damage, bool isReceivedData = false)
    {
        bool onRepawn = false;
        switch (type)
        {
            case CharacterType.Enemy:
                onRepawn = Enemy.GetInstance().GetDamage(damage);
                break;
            case CharacterType.Local:
                onRepawn = LocalCharacter.GetInstance().GetDamage(damage);
                break;
        }
        if(!isReceivedData && !onRepawn) ClientNetworkManager.GetInstance().SendAttack(type, damage);
    }
    public void Death(CharacterType type, Vector3 position, bool isReceivedData = false)
    {
        switch (type)
        {
            case CharacterType.Enemy:
                if (Enemy.GetInstance().hp > 60) break;
                Enemy.GetInstance().transform.position = position;
                Enemy.GetInstance().hp = 100;
                Enemy.GetInstance().hpText.text = Enemy.GetInstance().hp + "";
                Enemy.GetInstance().hpBar.fillAmount = ((float)Enemy.GetInstance().hp) / 100f;
                ++kill;
                break;
            case CharacterType.Local:
                if (LocalCharacter.GetInstance().hp > 60) break;
                LocalCharacter.GetInstance().transform.position = position;
                LocalCharacter.GetInstance().hp = 100;
                LocalCharacter.GetInstance().hpText.text = LocalCharacter.GetInstance().hp + "";
                LocalCharacter.GetInstance().hpBar.fillAmount = ((float)LocalCharacter.GetInstance().hp) / 100f;
                ++death;
                break;
        }
        GameObject.Find("KillDeathText").GetComponent<UnityEngine.UI.Text>().text = kill + "킬 " + death + "데스";
        if (!isReceivedData) ClientNetworkManager.GetInstance().SendDeath(type, position);
    }

    private static GameMain instance;
    public static GameMain GetInstance()
    {
        if (!instance)
        {
            instance = GameObject.FindObjectOfType<GameMain>();
            if (!instance)
                Debug.LogError("There needs to be one active GameMain script on a GameObject in your scene.");
        }

        return instance;
    }
}
