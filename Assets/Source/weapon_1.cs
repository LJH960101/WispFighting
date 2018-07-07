using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class weapon_1 : MonoBehaviour {
    public GameObject particle;
    bool onlyEffect = false;
    public void IsEffectOnly(bool swi) { onlyEffect = swi; }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<Enemy>() != null && !onlyEffect) GameMain.GetInstance().Attack(CharacterType.Enemy, 10);
        Instantiate(particle, transform.position, transform.rotation);
        Destroy(gameObject);
    }
}
