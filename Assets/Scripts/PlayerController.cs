using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public bool isInCar = false;
    public GameObject carToEnter;

    [Header("Aiming")]
    public Transform torso;               // cloak + head parent
    [Tooltip("How fast the torso rotates to face the cursor")]
    public float aimSmoothSpeed = 10f;

    [Header("Torso Bobbing")]
    [Tooltip("Vertical bob amplitude when walking")]
    public float bobAmplitude = 0.1f;
    [Tooltip("Bob frequency (cycles per second) at full speed")]
    public float bobFrequency = 5f;

    // Internals
    Vector3 lastMoveDir = Vector3.forward;
    Vector3 torsoBaseLocalPos;
    float bobTimer;

    public EnterExitCar EnterExitScript;


    void Start()
    {
        if (torso != null)
            torsoBaseLocalPos = torso.localPosition;



    }

    void Update()
    {
        if (isInCar) return;

        // 1) Read movement input and move
        float v = -Input.GetAxisRaw("Horizontal");
        float h = Input.GetAxisRaw("Vertical");
        Vector3 moveDir = new Vector3(h, 0, v).normalized;
        transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);
        if (moveDir.sqrMagnitude > 0.001f)
            lastMoveDir = moveDir;

        // 2) Aim torso at mouse
        AimTorsoAtMouse();

        // 3) Bob torso if moving
        BobTorso(moveDir.magnitude);

        if (Input.GetKeyDown(KeyCode.E) && carToEnter != null)
        {
            print("Pressed E");
            if (EnterExitScript != null)
            {
                print("entering script");
                EnterExitScript.EnterCar(this, carToEnter);
            }
            else
            {
                Debug.LogError($"Object {carToEnter.name} has no CarScript!");
            }
        }



    }
    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("EnterCar"))
            carToEnter = other.transform.parent.gameObject;
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("EnterCar"))
            carToEnter = null;
    }



    void AimTorsoAtMouse()
    {
        if (torso == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, transform.position);
        if (ground.Raycast(ray, out float enter))
        {
            Vector3 hit = ray.GetPoint(enter);
            Vector3 dir = (hit - transform.position);
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
                torso.rotation = Quaternion.Slerp(
                    torso.rotation,
                    targetRot,
                    aimSmoothSpeed * Time.deltaTime
                );
            }
        }
    }

    void BobTorso(float moveAmount)
    {
        if (torso == null) return;

        // advance timer proportional to how fast we're moving
        bobTimer += moveAmount * bobFrequency * Time.deltaTime;

        // compute vertical offset
        float bobOffset = Mathf.Sin(bobTimer * Mathf.PI * 2f) * bobAmplitude * moveAmount;

        // apply on top of the base local position
        Vector3 pos = torsoBaseLocalPos;
        pos.y += bobOffset;
        torso.localPosition = pos;
    }


    // Called by CarScript
    public void SetActive(bool active)
    {
        isInCar = !active;
        gameObject.SetActive(active);
    }
}
