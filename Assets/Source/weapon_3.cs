using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class weapon_3 : MonoBehaviour
{
    public GameObject particle;
    float timer;
    const float LIMIT_TIME = 2f;
    bool onlyEffect = false;
    public void IsEffectOnly(bool swi) { onlyEffect = swi; }
    // Use this for initialization
    void Start () {
        timer = 0f;
	}
	
	// Update is called once per frame
	void Update () {
        timer += Time.deltaTime;
        if (timer >= LIMIT_TIME)
        {
            float distance = Vector3.Distance(LocalCharacter.GetInstance().gameObject.transform.position, transform.position);
            if (distance <= 4f && !onlyEffect)
            {
                GameMain.GetInstance().Attack(CharacterType.Local, (int)((8f - (distance + 4f)) * 20f));
            }
            distance = Vector3.Distance(Enemy.GetInstance().gameObject.transform.position, transform.position);
            if (distance <= 4f && !onlyEffect)
            {
                GameMain.GetInstance().Attack(CharacterType.Enemy, (int)((8f - (distance + 4f)) * 20f));
            }
            Instantiate(particle, transform.position, transform.rotation);
            Destroy(gameObject);
        }
	}
}
