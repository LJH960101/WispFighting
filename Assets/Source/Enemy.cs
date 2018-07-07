using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public UnityEngine.UI.Image hpBar;
    public UnityEngine.UI.Text hpText;
    [HideInInspector] public int hp;
    void Start () {
        hp = 100;
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
        GameMain.GetInstance().Death(CharacterType.Enemy, transform.position);
    }
    public void UpdatePosition(Vector3 pos, Vector3 velocity)
    {
        transform.position = pos;
        transform.GetComponent<Rigidbody>().velocity = velocity;
    }

    private static Enemy instance;
    public static Enemy GetInstance()
    {
        if (!instance)
        {
            instance = GameObject.FindObjectOfType<Enemy>();
            if (!instance)
                Debug.LogError("There needs to be one active Enemy script on a GameObject in your scene.");
        }

        return instance;
    }
}
