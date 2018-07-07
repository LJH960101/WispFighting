using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalCharacter : MonoBehaviour {
    public GameObject w1, w2, w3;
    public Transform spawnPosition;
    public UnityEngine.UI.Image hpBar;
    public UnityEngine.UI.Text hpText;
    [HideInInspector] public int hp;
    private void Awake()
    {
        if(ClientNetworkManager.GetInstance().targetIp == "0")
        {
            transform.position = new Vector3(4, 2, 6);
        }
        hp = 100;
        StartCoroutine("SendPosition");
    }
    void OnDestroy()
    {
        StopCoroutine("SendPosition");
    }
    public AudioClip hit;
    public bool GetDamage(int damage)
    {
        gameObject.GetComponent<AudioSource>().PlayOneShot(hit);
        hp -= damage;
        hpText.text = hp + "";
        hpBar.fillAmount = ((float)hp) / 100f;
        if (hp <= 0)
        {
            Respawn();
            return true;
        }
        return false;
    }
    void Respawn()
    {
        transform.position = new Vector3(Random.Range(-6, 6), 5, Random.Range(-6, 6));
        hp = 100;
        hpText.text = hp + "";
        hpBar.fillAmount = ((float)hp) / 100f;
        GameMain.GetInstance().Death(CharacterType.Local, transform.position);
    }
    public void InstantiateMissle(int type, Vector3 pos, Quaternion rotation, bool onlyEffect = false)
    {
        switch (type)
        {
            case 0:
                {
                    GameObject newObj = Instantiate(w1, pos, rotation);
                    newObj.GetComponent<Rigidbody>().velocity = newObj.transform.forward * 30f;
                    newObj.GetComponent<weapon_1>().IsEffectOnly(onlyEffect);
                }
                break;
            case 1:
                {
                    GameObject newObj = Instantiate(w2, pos, rotation);
                    newObj.GetComponent<Rigidbody>().velocity = newObj.transform.forward * 10f;
                    newObj.GetComponent<weapon_2>().IsEffectOnly(onlyEffect);
                }
                break;
            case 2:
                {
                    GameObject newObj = Instantiate(w3, pos, rotation);
                    newObj.GetComponent<Rigidbody>().velocity = newObj.transform.forward * 3f;
                    newObj.GetComponent<weapon_3>().IsEffectOnly(onlyEffect);
                }
                break;
            default:
                print("Unexpected type");
                break;
        }
    }
    void Update ()
    {
        if (Input.GetMouseButtonDown(0))
        {
            InstantiateMissle(0, spawnPosition.position, spawnPosition.rotation);
            ClientNetworkManager.GetInstance().SendUseSkill(0, spawnPosition.position, spawnPosition.rotation);
        }
        else if (Input.GetMouseButtonDown(1))
        {
            InstantiateMissle(1, spawnPosition.position, spawnPosition.rotation);
            ClientNetworkManager.GetInstance().SendUseSkill(1, spawnPosition.position, spawnPosition.rotation);
        }
        else if (Input.GetMouseButtonDown(2))
        {
            InstantiateMissle(2, spawnPosition.position, spawnPosition.rotation);
            ClientNetworkManager.GetInstance().SendUseSkill(2, spawnPosition.position, spawnPosition.rotation);
        }
    }
    IEnumerator SendPosition()
    {
        while (true)
        {
            ClientNetworkManager.GetInstance().SendPosition(transform.position, transform.GetComponent<Rigidbody>().velocity);
            yield return new WaitForSeconds(0.1f);
        }
    }

    private static LocalCharacter instance;
    public static LocalCharacter GetInstance()
    {
        if (!instance)
        {
            instance = GameObject.FindObjectOfType<LocalCharacter>();
            if (!instance)
                Debug.LogError("There needs to be one active LocalCharacter script on a GameObject in your scene.");
        }

        return instance;
    }
}
