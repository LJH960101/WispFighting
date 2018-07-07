using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class weapon_2 : MonoBehaviour
{
    public GameObject particle;
    bool onlyEffect = false;
    public void IsEffectOnly(bool swi) { onlyEffect = swi; }
    private void OnCollisionEnter(Collision collision)
    {
        
        float distance = Vector3.Distance(LocalCharacter.GetInstance().gameObject.transform.position, transform.position);
        if (distance <= 2f && !onlyEffect)
        {
            GameMain.GetInstance().Attack(CharacterType.Local, (int)((4f - (distance+2f)) * 40f));
        }
        distance = Vector3.Distance(Enemy.GetInstance().gameObject.transform.position, transform.position);
        if (distance <= 2f && !onlyEffect)
        {
            GameMain.GetInstance().Attack(CharacterType.Enemy, (int)((4f - (distance + 2f)) * 40f));
        }
        Instantiate(particle, transform.position, transform.rotation);
        Destroy(gameObject);
    }
}
