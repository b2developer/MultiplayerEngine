using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum EPlayerProperties
{
    USERNAME = 1 << 7,
    BRANDING = 1 << 8,
    RACE = 1 << 9,
    GOLD = 1 << 10,
}

public class PlayerEntity : TransformEntity
{
    public delegate void CameraCorrectionFunc(Quaternion oldFrame, Quaternion newFrame);
    public delegate void SetCameraFunc(float yaw, float pitch);

    public static float CONTINUOUS_SWITCH = 5.0f;

    //refernces to internal components
    public Rigidbody body = null;
    public CapsuleCollider capsule = null;
    public MeshRenderer meshRenderer = null;
    public PlayerCosmetics cosmetics = null;

    public CameraCorrectionFunc cameraCorrectionCallback;
    public SetCameraFunc setCameraCallback;

    public TransformEntity animator = null;

    public Vector3 nameTagLocalPosition;
    public GameObject nameTag = null;

    public int parentId = -1;
    public bool isRouted = false;

    public string previousUsername = "";
    public string username = "";

    public InputSample input = null;
    public bool isLocal = false;
    public bool isServer = true;
    public bool manual = false;

    private const float EPSILON = 0.01f;
    public const float EXACT_EPSILON = 0.01f;

    //acceleration due to gravity
    public float gravity = -9.8f;

    //1.0f + ...
    public float reverseAccelBonus = 0.2f;

    //walk physics
    public float walkAcceleration = 15.0f;
    public float walkFriction = 0.01f;
    public float walkAngle = 45.0f;

    //air physics
    public float airAcceleration = 2.0f;
    public float airFriction = 0.8f;

    //jump physics
    public const float NO_GROUND_TIME = 0.2f;
    public const float AIR_TIME = 0.3f;

    public float jumpPower = 10.0f;
    public float noGroundTimer = 0.0f;
    public float airTimer = 0.0f;

    public float maxSpeed = 0.0f;
    public float maxAirSpeed = 0.5f;

    //friction applied to velocity not in the direction of the desired movement
    public float sideFriction = 0.01f;

    //maximum angle at which the player can freely move
    public float maxWalkAngle = 45.0f;

    public Rigidbody groundBody = null;
    public Vector3 currentPlane = Vector3.up;
    public bool wasGrounded = false;
    public Vector3 previousVelocity = Vector3.zero;
    public Vector3 previousPlane = Vector3.zero;

    public Quaternion frame = Quaternion.identity;

    public override void Initialise()
    {
        if (!isServer)
        {
            nameTagLocalPosition = new Vector3(nameTag.transform.localPosition.x * transform.localScale.x, nameTag.transform.localPosition.y * transform.localScale.y, nameTag.transform.localPosition.z * transform.localScale.z);
        }

        base.Initialise();
        dirtyFlagLength += 4;

        cameraCorrectionCallback += DefaultFunc;
        setCameraCallback += DefaultFunc;
    }

    public void DefaultFunc(Quaternion oldFrame, Quaternion newFrame)
    {
        
    }

    public void DefaultFunc(float yaw, float pitch)
    {

    }

    public override void WriteToStream(ref BitStream stream)
    {
        base.WriteToStream(ref stream);

        stream.WriteBool(parentId >= 0);

        if (parentId >= 0)
        {
            stream.WriteInt(parentId, Settings.MAX_ENTITY_BITS);
        }

        stream.WriteBool(isRouted);

        //write name with some safety checks
        if (username.Length == 3)
        {
            stream.WriteBool(true);
            byte b1 = MathExtension.ConvertSimplifiedAlphabetToByte(username[0]);
            byte b2 = MathExtension.ConvertSimplifiedAlphabetToByte(username[1]);
            byte b3 = MathExtension.ConvertSimplifiedAlphabetToByte(username[2]);
            stream.WriteBits(b1, 5);
            stream.WriteBits(b2, 5);
            stream.WriteBits(b3, 5);
        }
        else
        {
            stream.WriteBool(false);
        }

        cosmetics.WriteToStream(ref stream);
    }

    public override void ReadFromStream(ref BitStream stream)
    {
        base.ReadFromStream(ref stream);

        bool hasParent = stream.ReadBool();

        if (hasParent)
        {
            parentId = stream.ReadInt(Settings.MAX_ENTITY_BITS);
        }
        else
        {
            parentId = -1;
        }

        bool wasRouted = isRouted;

        isRouted = stream.ReadBool();

        if (setCameraCallback != null && !wasRouted && isRouted)
        {
            setCameraCallback(0.0f, 0.0f);
        }

        bool hasName = stream.ReadBool();

        if (hasName)
        {
            char c1 = MathExtension.ConvertByteToSimplifiedAlphabet(stream.ReadBits(5)[0]);
            char c2 = MathExtension.ConvertByteToSimplifiedAlphabet(stream.ReadBits(5)[0]);
            char c3 = MathExtension.ConvertByteToSimplifiedAlphabet(stream.ReadBits(5)[0]);
            char[] charArray = new char[3] { c1, c2, c3 };

            username = new string(charArray);

            if (!isServer && !isLocal)
            {
                if (!nameTag.activeSelf)
                {
                    nameTag.SetActive(true);
                }

                TextMesh textmesh = nameTag.GetComponentInChildren<TextMesh>();
                textmesh.text = username;
            }
        }
        else
        {
            username = "";

            if ((!isServer && !isLocal) && nameTag.activeSelf)
            {
                nameTag.SetActive(false);
            }
        }

        cosmetics.ReadFromStream(ref stream);
    }

    //special stream functions for sending full player information to it's associated client
    public void WritePlayerToStream(ref BitStream stream)
    {
        Vector3 position = body.position;

        if (parentId >= 0)
        {
            position = transform.parent.InverseTransformPoint(body.position);
        }

        stream.WriteFloat(position.x, 32);
        stream.WriteFloat(position.y, 32);
        stream.WriteFloat(position.z, 32);

        Quaternion rotation = body.rotation;

        if (parentId >= 0)
        {
            rotation = Quaternion.Inverse(transform.parent.rotation) * body.rotation;
        }

        stream.WriteFloat(rotation.x, 32);
        stream.WriteFloat(rotation.y, 32);
        stream.WriteFloat(rotation.z, 32);
        stream.WriteFloat(rotation.w, 32);

        stream.WriteFloat(body.linearVelocity.x, 32);
        stream.WriteFloat(body.linearVelocity.y, 32);
        stream.WriteFloat(body.linearVelocity.z, 32);

        stream.WriteFloat(noGroundTimer, 32);
        stream.WriteFloat(airTimer, 32);

        stream.WriteBool(wasGrounded);

        stream.WriteFloat(previousVelocity.x, 32);
        stream.WriteFloat(previousVelocity.y, 32);
        stream.WriteFloat(previousVelocity.z, 32);

        stream.WriteFloat(previousPlane.x, 32);
        stream.WriteFloat(previousPlane.y, 32);
        stream.WriteFloat(previousPlane.z, 32);

        stream.WriteBool(parentId >= 0);

        if (parentId >= 0)
        {
            stream.WriteInt(parentId, Settings.MAX_ENTITY_BITS);
        }

        stream.WriteBool(isRouted);

        cosmetics.WriteToStream(ref stream);
    }

    public void ReadPlayerFromStream(ref BitStream stream)
    {
        float positionX = stream.ReadFloat(32);
        float positionY = stream.ReadFloat(32);
        float positionZ = stream.ReadFloat(32);

        position.vector = new Vector3(positionX, positionY, positionZ);

        float rotationX = stream.ReadFloat(32);
        float rotationY = stream.ReadFloat(32);
        float rotationZ = stream.ReadFloat(32);
        float rotationW = stream.ReadFloat(32);

        rotation.quaternion = new Quaternion(rotationX, rotationY, rotationZ, rotationW);

        float velocityX = stream.ReadFloat(32);
        float velocityY = stream.ReadFloat(32);
        float velocityZ = stream.ReadFloat(32);

        body.linearVelocity = new Vector3(velocityX, velocityY, velocityZ);

        noGroundTimer = stream.ReadFloat(32);
        airTimer = stream.ReadFloat(32);

        wasGrounded = stream.ReadBool();

        float previousVelocityX = stream.ReadFloat(32);
        float previousVelocityY = stream.ReadFloat(32);
        float previousVelocityZ = stream.ReadFloat(32);

        previousVelocity = new Vector3(previousVelocityX, previousVelocityY, previousVelocityZ);

        float previousPlaneX = stream.ReadFloat(32);
        float previousPlaneY = stream.ReadFloat(32);
        float previousPlaneZ = stream.ReadFloat(32);

        previousPlane = new Vector3(previousPlaneX, previousPlaneY, previousPlaneZ);

        bool hasParent = stream.ReadBool();

        if (hasParent)
        {
            parentId = stream.ReadInt(Settings.MAX_ENTITY_BITS);
        }
        else
        {
            parentId = -1;
        }

        bool wasRouted = isRouted;

        isRouted = stream.ReadBool();

        if (!wasRouted && isRouted)
        {
            setCameraCallback(0.0f, 0.0f);
        }

        cosmetics.ReadFromStream(ref stream);

        if (!isServer)
        {
            transform.localPosition = position.vector;
            transform.localRotation = rotation.quaternion;

            body.position = transform.position;
            body.rotation = transform.rotation;
        }
    }

    public override int GetBitLength()
    {
        int total = base.GetBitLength();

        total += 1;

        if (parentId >= 0)
        {
            total += Settings.MAX_ENTITY_BITS;
        }

        total += 1;

        total += 1;

        if (username.Length == 3)
        {
            total += 15;
        }

        total += cosmetics.GetBitLength();

        return total;
    }

    public override void WriteToStreamPartial(ref BitStream stream)
    {
        base.WriteToStreamPartial(ref stream);

        stream.WriteBool(parentId >= 0);

        if (parentId >= 0)
        {
            stream.WriteInt(parentId, Settings.MAX_ENTITY_BITS);
        }

        stream.WriteBool(isRouted);

        if ((dirtyFlag & (int)EPlayerProperties.USERNAME) > 0)
        {
            //write name with some safety checks
            if (username.Length == 3)
            {
                stream.WriteBool(true);
                byte b1 = MathExtension.ConvertSimplifiedAlphabetToByte(username[0]);
                byte b2 = MathExtension.ConvertSimplifiedAlphabetToByte(username[1]);
                byte b3 = MathExtension.ConvertSimplifiedAlphabetToByte(username[2]);
                stream.WriteBits(b1, 5);
                stream.WriteBits(b2, 5);
                stream.WriteBits(b3, 5);
            }
            else
            {
                stream.WriteBool(false);
            }
        }

        cosmetics.WriteToStreamPartial(ref stream, dirtyFlag);
    }

    public override void ReadFromStreamPartial(ref BitStream stream)
    {
        base.ReadFromStreamPartial(ref stream);

        bool hasParent = stream.ReadBool();

        if (hasParent)
        {
            parentId = stream.ReadInt(Settings.MAX_ENTITY_BITS);
        }
        else
        {
            parentId = -1;
        }

        bool wasRouted = isRouted;

        isRouted = stream.ReadBool();

        if (!wasRouted && isRouted)
        {
            setCameraCallback(0.0f, 0.0f);
        }

        if ((dirtyFlag & (int)EPlayerProperties.USERNAME) > 0)
        {
            bool hasName = stream.ReadBool();

            if (hasName)
            {
                char c1 = MathExtension.ConvertByteToSimplifiedAlphabet(stream.ReadBits(5)[0]);
                char c2 = MathExtension.ConvertByteToSimplifiedAlphabet(stream.ReadBits(5)[0]);
                char c3 = MathExtension.ConvertByteToSimplifiedAlphabet(stream.ReadBits(5)[0]);
                char[] charArray = new char[3] { c1, c2, c3 };

                username = new string(charArray);

                if (!isServer && !isLocal)
                {
                    if (!nameTag.activeSelf)
                    {
                        nameTag.SetActive(true);
                    }

                    TextMesh textmesh = nameTag.GetComponentInChildren<TextMesh>();
                    textmesh.text = username;
                }
            }
            else
            {
                username = "";

                if ((!isServer && !isLocal) && nameTag.activeSelf)
                {
                    nameTag.SetActive(false);
                }
            }
        }

        cosmetics.ReadFromStreamPartial(ref stream, dirtyFlag);
    }

    public override int GetBitLengthPartial()
    {
        int total = base.GetBitLengthPartial();

        total += 1;

        if (parentId >= 0)
        {
            total += Settings.MAX_ENTITY_BITS;
        }

        total += 1;

        total += 1;

        if ((dirtyFlag & (int)EPlayerProperties.USERNAME) > 0)
        {
            total += 15;
        }

        total += cosmetics.GetBitLengthPartial(dirtyFlag);

        return total;
    }

    public void ManualTick()
    {
        manual = true;
        Tick();
        manual = false;
    }

    public override void Tick()
    {
        if (isServer)
        {
            if (previousUsername != username)
            {
                dirtyFlag = dirtyFlag | (int)EPlayerProperties.USERNAME;
            }

            previousUsername = username;

            if (cosmetics.previousBranding != cosmetics.branding)
            {
                dirtyFlag = dirtyFlag | (int)EPlayerProperties.BRANDING;
            }

            cosmetics.previousBranding = cosmetics.branding;

            if (cosmetics.previousRace != cosmetics.race)
            {
                dirtyFlag = dirtyFlag | (int)EPlayerProperties.RACE;
            }

            cosmetics.previousRace = cosmetics.race;

            if (cosmetics.previousIsGold != cosmetics.isGold)
            {
                dirtyFlag = dirtyFlag | (int)EPlayerProperties.GOLD;
            }

            cosmetics.previousIsGold = cosmetics.isGold;

            float speed = body.linearVelocity.sqrMagnitude;

            if (speed > CONTINUOUS_SWITCH * CONTINUOUS_SWITCH)
            {
                body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
            else
            {
                body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }
        }

        if (!manual)
        {
            return;
        }

        if (isLocal)
        {
            Vector2 inputVector = Vector2.zero;

            if (!isRouted && input != null)
            {
                inputVector = input.GetMovementVector();

                float yaw = input.yaw;
                inputVector = MathExtension.RotateVector2(inputVector, yaw);
            }
            //----------

            if (noGroundTimer > 0.0f)
            {
                noGroundTimer -= Time.fixedDeltaTime;
            }

            //distance from the centre of the capsule to the start of the sphere from each e1nds
            float capsuleExtent = (capsule.height * 0.5f - capsule.radius) * transform.localScale.x;

            bool grounded = false;
            groundBody = null;

            RaycastHit groundInfo;

            Quaternion previousFrame = frame;

            if (transform.parent != null)
            {
                frame = transform.parent.rotation;
            }

            if (parentId < 0)
            {
                gravity = PlanetFinder.instance.GetGravity(this);
            }
            else
            {
                gravity = 9.81f;
            }

            Quaternion dq = frame;
            Quaternion dqi = Quaternion.Inverse(dq);

            Quaternion q = dq;
            Quaternion qi = dqi;

            Vector3 UP = frame * Vector3.up;
            Vector3 DOWN = frame * Vector3.down;

            currentPlane = UP;

            capsule.enabled = false;

            //ground collision check
            if (noGroundTimer <= 0.0f && Physics.SphereCast(transform.position, capsule.radius * transform.localScale.x - EPSILON, DOWN, out groundInfo, capsuleExtent + EPSILON * 2.0f))
            {
                RaycastHit exactInfo;

                Physics.Raycast(transform.position, (groundInfo.point - transform.position).normalized, out exactInfo, Mathf.Infinity);

                //walking angle check
                if (Vector3.Angle(UP, exactInfo.normal) <= maxWalkAngle)
                {
                    currentPlane = exactInfo.normal;

                    q = Quaternion.FromToRotation(UP, currentPlane) * frame;
                    qi = Quaternion.Inverse(q);

                    groundBody = exactInfo.collider.GetComponent<Rigidbody>();

                    //readjust the velocity to slide down the plane instead of bounce off it
                    if (!wasGrounded)
                    {
                        Vector3 previousFlatVelocity = qi * previousVelocity;
                        previousFlatVelocity.y = 0.0f;

                        body.linearVelocity = q * previousFlatVelocity;
                    }

                    grounded = true;
                    walkAngle = Vector3.Angle(UP, currentPlane);
                    airTimer = 0.0f;
                }
            }

            //CLAMP TO DOWNWARDS SLOPE
            if (wasGrounded && !grounded && noGroundTimer <= 0.0f)
            {
                //ratio of current velocity to apply
                float t = Mathf.Tan(walkAngle * Mathf.Deg2Rad);

                Vector3 flatVelocity = qi * body.linearVelocity;
                Vector2 horizontalVelocity = new Vector2(flatVelocity.x, flatVelocity.z);

                //additional distance for the ground check
                float additionalDistance = horizontalVelocity.magnitude * t * Time.fixedDeltaTime;

                if (Physics.SphereCast(transform.position, capsule.radius * transform.localScale.x - EPSILON, DOWN, out groundInfo, capsuleExtent + EPSILON * 2.0f + additionalDistance))
                {
                    RaycastHit exactInfo;

                    Physics.SphereCast(transform.position, EXACT_EPSILON, ((groundInfo.point + q * new Vector3(horizontalVelocity.x, 0.0f, horizontalVelocity.y).normalized * EXACT_EPSILON) - transform.position).normalized, out exactInfo, Mathf.Infinity);

                    //walking angle check
                    if (Vector3.Angle(UP, exactInfo.normal) <= maxWalkAngle)
                    {
                        currentPlane = exactInfo.normal;

                        q = Quaternion.FromToRotation(UP, currentPlane) * frame;
                        qi = Quaternion.Inverse(q);

                        transform.position = groundInfo.point + groundInfo.normal * (capsule.radius * transform.localScale.x - EPSILON) + UP * (capsuleExtent + EPSILON * 1.0f);

                        Vector3 flatVelocity2 = qi * body.linearVelocity;
                        flatVelocity2.y = 0.0f;

                        body.linearVelocity = q * flatVelocity2;

                        grounded = true;
                    }
                }
            }

            capsule.enabled = true;

            //HORIZONTAL PHYSICS
            if (grounded)
            {
                float bonus = 1.0f;

                Vector3 flatVelocity = qi * body.linearVelocity;
                Vector2 flat = new Vector2(flatVelocity.x, flatVelocity.z);

                //eliminate vertical velocity error
                if (Mathf.Abs(flatVelocity.y) > 0.0f)
                {
                    body.linearVelocity = q * new Vector3(flatVelocity.x, 0.0f, flatVelocity.z);
                    flatVelocity = qi * body.linearVelocity;
                    flat = new Vector2(flatVelocity.x, flatVelocity.z);
                }

                //reverse movement bonus
                if (Vector2.Dot(flat, inputVector) < 0.0f)
                {
                    bonus += reverseAccelBonus;
                }

                body.linearVelocity += q * new Vector3(inputVector.x, 0.0f, inputVector.y) * walkAcceleration * bonus * Time.fixedDeltaTime;

                //apply friction if the player isn't moving
                if (inputVector == Vector2.zero)
                {
                    Vector3 velocityPlane = qi * body.linearVelocity;

                    float frictionScalar = Mathf.Pow(walkFriction, Time.fixedDeltaTime);

                    velocityPlane.x *= frictionScalar;
                    velocityPlane.z *= frictionScalar;

                    body.linearVelocity = q * velocityPlane;
                }
                else
                {
                    Vector3 velocityPlane = qi * body.linearVelocity;
                    Vector2 groundVelocity = new Vector2(velocityPlane.x, velocityPlane.z);

                    Vector2 r = new Vector2(-inputVector.y, inputVector.x);

                    float frictionScalar = Mathf.Pow(sideFriction, Time.fixedDeltaTime);

                    Vector2 forwards = Vector2.Dot(groundVelocity, inputVector) * inputVector;
                    Vector2 sideways = r * frictionScalar * Vector2.Dot(groundVelocity, r);
                    Vector2 combine = forwards + sideways;

                    body.linearVelocity = q * new Vector3(combine.x, 0.0f, combine.y);
                }
            }
            else
            {
                //old-fashioned aerial movement
                body.linearVelocity += dq * (airAcceleration * Time.fixedDeltaTime * new Vector3(inputVector.x, 0.0f, inputVector.y));

                //apply friction if the player isn't moving
                if (inputVector == Vector2.zero)
                {
                    float frictionScalar = Mathf.Pow(airFriction, Time.fixedDeltaTime);

                    Vector3 flatVelocity = dqi * body.linearVelocity;
                    flatVelocity = new Vector3(flatVelocity.x * frictionScalar, flatVelocity.y, flatVelocity.z * frictionScalar);
                    body.linearVelocity = dq * flatVelocity;
                }

                //increment air timer
                airTimer += Time.fixedDeltaTime;

                if (airTimer > AIR_TIME)
                {
                    airTimer = AIR_TIME;
                }
            }

            //VERTICAL PHYSICS
            if (grounded)
            {
                //ground movement
                if (input != null && input.jump.state == EButtonState.ON_PRESS)
                {
                    Vector3 flatVelocity = dqi * body.linearVelocity;
                    flatVelocity = new Vector3(flatVelocity.x, jumpPower, flatVelocity.z);
                    body.linearVelocity = dq * flatVelocity;

                    noGroundTimer = NO_GROUND_TIME;
                    airTimer = AIR_TIME;

                    groundBody = null;
                    q = dq;
                    qi = dqi;
                }
            }
            else
            {
                //lee-way for jumping
                if (airTimer < AIR_TIME)
                {
                    //ground movement
                    if (input != null && input.jump.state == EButtonState.ON_PRESS)
                    {
                        Vector3 flatVelocity = dqi * body.linearVelocity;
                        flatVelocity = new Vector3(flatVelocity.x, jumpPower, flatVelocity.z);
                        body.linearVelocity = dq * flatVelocity;

                        noGroundTimer = NO_GROUND_TIME;
                        airTimer = AIR_TIME;

                        groundBody = null;
                        q = dq;
                        qi = dqi;
                    }
                }

                //apply gravity
                body.linearVelocity += gravity * Time.fixedDeltaTime * DOWN;
            }

            Vector3 groundSpeed = qi * body.linearVelocity;
            Vector2 horizontalSpeed = new Vector2(groundSpeed.x, groundSpeed.z);

            //clamp speed
            if (horizontalSpeed.sqrMagnitude > maxSpeed * maxSpeed)
            {
                horizontalSpeed = horizontalSpeed.normalized * maxSpeed;
                body.linearVelocity = q * new Vector3(horizontalSpeed.x, groundSpeed.y, horizontalSpeed.y);
            }

            //apply downward force to ground (if it is a rigidbody)
            //if (groundBody != null)
            //{
                //Vector3 contact = transform.position + Vector3.down * capsuleExtent - capsule.radius * transform.localScale.x * currentPlane;

                //float forceDot = Vector3.Dot(Vector3.down, -currentPlane);

                //groundBody.AddForceAtPosition(gravity * body.mass * forceDot * Vector3.down, contact);
                //body.AddForce(-gravity * forceDot * currentPlane, ForceMode.Acceleration);
            //}

            wasGrounded = grounded;
            previousVelocity = body.linearVelocity;
            previousPlane = currentPlane;

            //----------
            if (isServer)
            {
                base.Tick();
            }
        }
        else
        {
            base.Tick();
        }
    }

    public void BaseTick()
    {
        previousPosition = Vector3.zero;
        previousRotation = Quaternion.identity;

        base.Tick();
    }

    public void LateTick()
    {
        
    }

    public new void Update()
    {
        if (!isLocal)
        {
            if (interpolationFilter != null)
            {
                interpolationFilter.Update(transform, Time.deltaTime);
            }
        }
    }

    public override void SetPriority(PlayerEntity player)
    {
        //from the transform entity, max priority with inverse distance is 1
        priority = 2.0f;
    }
}
