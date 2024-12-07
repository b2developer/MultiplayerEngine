using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum EPlayerProperties
{
    USERNAME = 1 << 7,
    BRANDING = 1 << 8,
}

public class PlayerEntity : TransformEntity
{
    //refernces to internal components
    public Rigidbody body = null;
    public CapsuleCollider capsule = null;
    public MeshRenderer meshRenderer = null;
    public PlayerCosmetics cosmetics = null;
    public ProjectileSpawner projectileSpawner = null;

    public TransformEntity animator = null;

    public Vector3 nameTagLocalPosition;
    public GameObject nameTag = null;

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

    public override void Initialise()
    {
        if (!isServer)
        {
            nameTagLocalPosition = new Vector3(nameTag.transform.localPosition.x * transform.localScale.x, nameTag.transform.localPosition.y * transform.localScale.y, nameTag.transform.localPosition.z * transform.localScale.z);
        }

        base.Initialise();
        dirtyFlagLength += 2;
    }

    public override void WriteToStream(ref BitStream stream)
    {
        base.WriteToStream(ref stream);

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
        stream.WriteFloat(body.position.x, 32);
        stream.WriteFloat(body.position.y, 32);
        stream.WriteFloat(body.position.z, 32);

        stream.WriteFloat(body.rotation.x, 32);
        stream.WriteFloat(body.rotation.y, 32);
        stream.WriteFloat(body.rotation.z, 32);
        stream.WriteFloat(body.rotation.w, 32);

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

        cosmetics.ReadFromStream(ref stream);

        if (!isServer)
        {
            transform.position = position.vector;
            transform.rotation = rotation.quaternion;

            body.position = position.vector;
            body.rotation = rotation.quaternion;
        }
    }

    public override int GetBitLength()
    {
        int total = base.GetBitLength();

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
        }

        if (!manual)
        {
            return;
        }

        if (isLocal)
        {
            projectileSpawner.Tick();

            Vector2 inputVector = Vector2.zero;

            if (input != null)
            {
                inputVector = input.GetMovementVector();

                float yaw = input.yaw;
                inputVector = MathExtension.RotateVector2(inputVector, yaw);
            }
            //----------

            currentPlane = Vector3.up;

            if (noGroundTimer > 0.0f)
            {
                noGroundTimer -= Time.fixedDeltaTime;
            }

            //distance from the centre of the capsule to the start of the sphere from each e1nds
            float capsuleExtent = (capsule.height * 0.5f - capsule.radius) * transform.localScale.x;

            bool grounded = false;
            groundBody = null;

            RaycastHit groundInfo;

            Quaternion q = Quaternion.identity;
            Quaternion qi = Quaternion.identity;

            capsule.enabled = false;

            //ground collision check
            if (noGroundTimer <= 0.0f && Physics.SphereCast(transform.position, capsule.radius * transform.localScale.x - EPSILON, Vector3.down, out groundInfo, capsuleExtent + EPSILON * 2.0f))
            {
                RaycastHit exactInfo;

                Physics.Raycast(transform.position, (groundInfo.point - transform.position).normalized, out exactInfo, Mathf.Infinity);

                //walking angle check
                if (Vector3.Angle(Vector3.up, exactInfo.normal) <= maxWalkAngle)
                {
                    currentPlane = exactInfo.normal;

                    q = Quaternion.FromToRotation(Vector3.up, currentPlane);
                    qi = Quaternion.Inverse(q);

                    groundBody = exactInfo.collider.GetComponent<Rigidbody>();

                    //readjust the velocity to slide down the plane instead of bounce off it
                    if (!wasGrounded)
                    {
                        body.linearVelocity = q * new Vector3(previousVelocity.x, 0.0f, previousVelocity.z);
                    }

                    grounded = true;
                    walkAngle = Vector3.Angle(Vector3.up, currentPlane);
                    airTimer = 0.0f;
                }
            }

            //CLAMP TO DOWNWARDS SLOPE
            if (wasGrounded && !grounded && noGroundTimer <= 0.0f)
            {
                //ratio of current velocity to apply
                float t = Mathf.Tan(walkAngle * Mathf.Deg2Rad);
                Vector2 horizontalVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.z);

                //additional distance for the ground check
                float additionalDistance = horizontalVelocity.magnitude * t * Time.fixedDeltaTime;

                if (Physics.SphereCast(transform.position, capsule.radius * transform.localScale.x - EPSILON, Vector3.down, out groundInfo, capsuleExtent + EPSILON * 2.0f + additionalDistance))
                {
                    RaycastHit exactInfo;

                    Physics.SphereCast(transform.position, EXACT_EPSILON, ((groundInfo.point + new Vector3(horizontalVelocity.x, 0.0f, horizontalVelocity.y).normalized * EXACT_EPSILON) - transform.position).normalized, out exactInfo, Mathf.Infinity);

                    //walking angle check
                    if (Vector3.Angle(Vector3.up, exactInfo.normal) <= maxWalkAngle)
                    {
                        currentPlane = exactInfo.normal;

                        q = Quaternion.FromToRotation(Vector3.up, currentPlane);
                        qi = Quaternion.Inverse(q);

                        transform.position = groundInfo.point + groundInfo.normal * (capsule.radius * transform.localScale.x - EPSILON) + Vector3.up * (capsuleExtent + EPSILON * 1.0f);
                        body.linearVelocity = q * new Vector3(body.linearVelocity.x, 0.0f, body.linearVelocity.z);

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
                //source engine style air-strafing
                Vector2 horizontalVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.z);

                Vector2 horizontalDirection = horizontalVelocity.normalized;

                float magnitude = horizontalVelocity.magnitude;

                Vector2 a = inputVector;
                float aMagnitude = a.magnitude;

                Vector2 ta = airAcceleration * Time.fixedDeltaTime * inputVector;
                float taMagnitude = ta.magnitude;

                float vprojta = magnitude * Mathf.Cos(Vector2.Angle(horizontalVelocity, ta) * Mathf.Deg2Rad);

                Vector2 newVelocity = Vector2.zero;

                if (vprojta < maxAirSpeed - taMagnitude)
                {
                    newVelocity = horizontalVelocity + ta;
                }
                else if (vprojta < maxAirSpeed)
                {
                    newVelocity = horizontalVelocity + (maxAirSpeed - vprojta) * a.normalized;
                }
                else
                {
                    newVelocity = horizontalVelocity;
                }

                body.linearVelocity = new Vector3(newVelocity.x, body.linearVelocity.y, newVelocity.y);

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
                    body.linearVelocity = new Vector3(body.linearVelocity.x, jumpPower, body.linearVelocity.z);

                    noGroundTimer = NO_GROUND_TIME;
                    airTimer = AIR_TIME;

                    groundBody = null;
                    q = Quaternion.identity;
                    qi = Quaternion.identity;
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
                        body.linearVelocity = new Vector3(body.linearVelocity.x, jumpPower, body.linearVelocity.z);

                        noGroundTimer = NO_GROUND_TIME;
                        airTimer = AIR_TIME;

                        groundBody = null;
                        q = Quaternion.identity;
                        qi = Quaternion.identity;
                    }
                }

                //apply gravity
                body.linearVelocity += gravity * Time.fixedDeltaTime * Vector3.down;
            }

            Vector3 groundSpeed = qi * body.linearVelocity;
            Vector2 horizontalSpeed = new Vector2(groundSpeed.x, groundSpeed.z);

            //clamp speed
            if (grounded && horizontalSpeed.sqrMagnitude > maxSpeed * maxSpeed)
            {
                horizontalSpeed = horizontalSpeed.normalized * maxSpeed;
                body.linearVelocity = q * new Vector3(horizontalSpeed.x, groundSpeed.y, horizontalSpeed.y);
            }

            //apply downward force to ground (if it is a rigidbody)
            if (groundBody != null)
            {
                Vector3 contact = transform.position + Vector3.down * capsuleExtent - capsule.radius * transform.localScale.x * currentPlane;

                float forceDot = Vector3.Dot(Vector3.down, -currentPlane);

                groundBody.AddForceAtPosition(gravity * body.mass * forceDot * Vector3.down, contact);
                body.AddForce(-gravity * forceDot * currentPlane, ForceMode.Acceleration);
            }

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
