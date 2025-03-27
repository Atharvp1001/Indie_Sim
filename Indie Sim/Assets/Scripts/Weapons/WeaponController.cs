using UnityEngine;

public class WeaponController : MonoBehaviour
{

    public GameObject prefab;
    public float damage;
    public float speed;
    public float coolDownDuration;
    float currentCoolDown;
    public int pierce;

    void Start()
    {
        currentCoolDown = coolDownDuration;
    }

    // Update is called once per frame
    void Update()
    {
        /*
        currentCoolDown -= Time.deltaTime;
        if (currentCoolDown <= 0f) {
            Attack();
        }
        */
    }

    void Attack() 
    {
        //currentCoolDown = coolDownDuration;
    }

}
