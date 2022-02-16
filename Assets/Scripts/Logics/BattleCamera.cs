using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleCamera : MonoBehaviour
{
    public static BattleCamera instance;

    [SerializeField]
    private float cameraOffsetY = 110f;

    private PlayerController target;
    private Vector3 targetCameraPos;
    private bool nexusUsed = false;
    private bool isMissionMode = false;
    private bool initialized = false;

    public void SetTarget(PlayerController t)
    {
        target = t;
        initialized = true;
    }

    public void OnNexusUsed(Vector3 pos)
    {
        targetCameraPos = pos;
        nexusUsed = true;
    }

    private void Awake()
    {
        if (!instance)
            instance = this;
    }

    void Start()
    {
        if (MissionController.instance != null)
            isMissionMode = true;
    }

    void Update()
    {
        if (!initialized || target == null) return;

        if (!target.IsDied)
        {
            if (targetCameraPos != null && nexusUsed)
            {
                if (!isMissionMode || (isMissionMode && MissionController.instance.IsObjectiveDone))
                    transform.position = Vector3.Lerp(transform.position, targetCameraPos + Vector3.up * cameraOffsetY, Time.deltaTime * 8f);
                else
                    transform.position = Vector3.Lerp(transform.position, target.transform.position + Vector3.up * cameraOffsetY, Time.deltaTime * 8f);
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, target.transform.position + Vector3.up * cameraOffsetY, Time.deltaTime * 8f);
            }
        }
        else if (target.LastEnemyTransform != null && !target.ShipRenderer.gameObject.activeSelf)
            transform.position = Vector3.Lerp(transform.position, target.LastEnemyTransform.position + Vector3.up * cameraOffsetY, Time.deltaTime * 8f);
        else if (target.IsDied && target.ShipRenderer.gameObject.activeSelf)
            transform.position = Vector3.Lerp(transform.position, target.transform.position + Vector3.up * cameraOffsetY, Time.deltaTime * 8f);
    }
}
