using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HangarRobot : MonoBehaviour
{
    private Animator animator;
    [SerializeField] ParticleSystem sparks;

    private float timeToNextSparks = 3f;
    private float timeToDeactivate = 12f;
    private float timeToRotate = 5f;
    private float targetRot = 0f;
    private float initRot = 0f;
    private bool isActive = true;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // Start is called before the first frame update
    void Start()
    {
        initRot = transform.localEulerAngles.y;
        targetRot = transform.localEulerAngles.y + Random.Range(-20f, 20f);
    }

    // Update is called once per frame
    void Update()
    {
        if (timeToNextSparks > 0f && isActive)
        {
            timeToNextSparks -= Time.deltaTime;

            if (timeToNextSparks <= 0f)
            {
                timeToNextSparks = Random.Range(1f, 3f);
                sparks.Play();
            }
        }

        if (timeToRotate > 0f && isActive)
        {
            timeToRotate -= Time.deltaTime;

            if (timeToRotate <= 0f)
            {
                timeToRotate = Random.Range(5f, 7f);
                targetRot = initRot + Random.Range(-20f, 20f);
            }
        }

        transform.localEulerAngles = Vector3.Lerp(transform.localEulerAngles, new Vector3(transform.localEulerAngles.x, targetRot, transform.localEulerAngles.z), 6f * Time.deltaTime);

        if (timeToDeactivate > 0f)
        {
            timeToDeactivate -= Time.deltaTime;

            if (timeToDeactivate <= 0f)
            {
                timeToDeactivate = Random.Range(10f, 20f);
                isActive = !isActive;
                if (!isActive)
                    animator.SetTrigger("Die");
                else
                    animator.SetTrigger("Attack");
            }
        }
    }
}
