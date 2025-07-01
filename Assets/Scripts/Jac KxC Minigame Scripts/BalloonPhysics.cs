using UnityEngine;

public class BalloonPhysics : MonoBehaviour
{
    [Header("Physics Setup")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform handAttachPoint;     
    [SerializeField] private Rigidbody balloonRigidbody;

    [Header("String Physics")]
    [SerializeField] private float stringLength = 3f;
    [SerializeField] private float stringSpring = 200f;     
    [SerializeField] private float stringDamper = 20f;     

    [Header("Balloon Properties")]
    [SerializeField] private float balloonMass = 0.1f;
    [SerializeField] private float buoyancyForce = 2f;
    [SerializeField] private float windResistance = 0.5f;
    [SerializeField] private float maxDrift = 5f;

    [Header("Wind Effect")]
    [SerializeField] private float windStrength = 1f;
    [SerializeField] private Vector3 windDirection = Vector3.right;

    private SpringJoint springJoint;
    private Vector3 lastPlayerPosition;
    private GameObject anchorObject;    

    void Start()
    {
        SetupBalloonPhysics();
        SetupStringConnection();

        lastPlayerPosition = player.position;
    }

    void SetupBalloonPhysics()
    {
        if (balloonRigidbody == null)
            balloonRigidbody = GetComponent<Rigidbody>();

        if (balloonRigidbody == null)
            balloonRigidbody = gameObject.AddComponent<Rigidbody>();

        balloonRigidbody.mass = balloonMass;
        balloonRigidbody.useGravity = true;
        balloonRigidbody.linearDamping = windResistance;
        balloonRigidbody.angularDamping = 2f;
    }

    void SetupStringConnection()
    {
        anchorObject = new GameObject("BalloonAnchor");
        anchorObject.transform.SetParent(handAttachPoint != null ? handAttachPoint : player);
        anchorObject.transform.localPosition = Vector3.zero;

        Rigidbody anchorRb = anchorObject.AddComponent<Rigidbody>();
        anchorRb.isKinematic = true;
        anchorRb.useGravity = false;

        springJoint = gameObject.AddComponent<SpringJoint>();
        springJoint.connectedBody = anchorRb;      
        springJoint.autoConfigureConnectedAnchor = false;
        springJoint.connectedAnchor = Vector3.zero;
        springJoint.anchor = Vector3.zero;

        springJoint.spring = stringSpring;
        springJoint.damper = stringDamper;
        springJoint.minDistance = 0f;
        springJoint.maxDistance = stringLength;
        springJoint.tolerance = 0.025f;
        springJoint.enableCollision = false;       
    }

    void FixedUpdate()
    {
        ApplyBuoyancy();
        ApplyWindForce();
        LimitDrift();
    }

    void Update()
    {
    }

    void ApplyBuoyancy()
    {
        balloonRigidbody.AddForce(Vector3.up * buoyancyForce, ForceMode.Force);
    }

    void ApplyWindForce()
    {
        Vector3 playerVelocity = Vector3.zero;

        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerVelocity = playerRb.linearVelocity;
        }
        else
        {
            playerVelocity = (player.position - lastPlayerPosition) / Time.fixedDeltaTime;
        }

        Vector3 relativeWind = -playerVelocity * 0.3f + windDirection * windStrength;   
        balloonRigidbody.AddForce(relativeWind * windResistance, ForceMode.Force);
        lastPlayerPosition = player.position;
    }

    void LimitDrift()
    {
        Vector3 displacement = transform.position - player.position;
        if (displacement.magnitude > maxDrift)
        {
            Vector3 pullBack = -displacement.normalized * 2f;
            balloonRigidbody.AddForce(pullBack, ForceMode.Force);
        }
    }

    public void OnPlayerHitObstacle()
    {
        Vector3 randomForce = new Vector3(
            Random.Range(-2f, 2f),
            Random.Range(1f, 3f),
            Random.Range(-1f, 1f)
        );
        balloonRigidbody.AddForce(randomForce, ForceMode.Impulse);
    }

    void OnDestroy()
    {
        if (anchorObject != null)
        {
            Destroy(anchorObject);
        }
    }
}