using System.Security.Policy;
using Unity.VisualScripting;
using UnityEngine;

public class ShipEntity : TransformEntity
{
    public static Vector3 BLACKHOLE_POSITION = new Vector3(-100f, -9.5f, -250f);
    public static float BLACKHOLE_MAX_DIST = 12.5f;
    public static float BLACKHOLE_MIN_DIST = 150.0f;
    public static float BLACKHOLE_STRENGTH_MAX = 9.0f;
    public static float BLACKHOLE_STRENGTH_MIN = 0.0f;

    public Vector3 startingPosition;
    public Quaternion startingRotation;

    public GameObject tail;
    public Follow tailScript;

    public Rigidbody body;

    public BoxCollider inside;
    public BoxCollider drive;

    public PlayerEntity controller = null;

    public float MAX_SPEED = 5.0f;
    public float ACCELERATION = 3.0f;
    public float FRICTION = 0.5f;
    public float SIDE_FRICTION = 0.5f;
    public float EPSILON = 0.1f;

    public float KP = 5.0f;
    public float KD = 1.0f;

    public float KPA = 2.0f * 0.01f;
    public float KDA = 2.0f * 0.01f;

    public float battery = 1.0f;
    public float depletionRate = 0.002f;
    public bool isStarted = false;

    public float CUSHION = 10.0f;
    public float DRIFT = 0.2f;

    public override void Initialise()
    {
        base.Initialise();

        //UpdateManager.instance.entityFunction -= Tick;
        //UpdateManager.instance.preFunction += Tick;

        startingPosition = transform.position;
        startingRotation = transform.rotation;
       
        tail = new GameObject("Tail(ShipEntity)");
        tailScript = tail.AddComponent<Follow>();
        tailScript.target = transform;
    }

    public override void WriteToStream(ref BitStream stream)
    {
        base.WriteToStream(ref stream);

        stream.WriteFloat(battery, 32);
    }

    public override void ReadFromStream(ref BitStream stream)
    {
        base.ReadFromStream(ref stream);

        battery = stream.ReadFloat(32);
    }

    public override int GetBitLength()
    {
        int total = base.GetBitLength();

        total += 32;

        return total;
    }

    public override void ReadFromStreamPartial(ref BitStream stream)
    {
        base.ReadFromStreamPartial(ref stream);

        battery = stream.ReadFloat(32);
    }

    public override void WriteToStreamPartial(ref BitStream stream)
    {
        base.WriteToStreamPartial(ref stream);

        stream.WriteFloat(battery, 32);
    }

    public override int GetBitLengthPartial()
    {
        int total = base.GetBitLengthPartial();

        total += 32;

        return total;
    }

    public override void Tick()
    {
        Vector2 inputVector = Vector2.zero;

        bool isBeingPulled = false;

        Vector3 blackholeRelative = BLACKHOLE_POSITION - transform.position;

        float blackholeDistSqr = blackholeRelative.sqrMagnitude;

        if (blackholeDistSqr > BLACKHOLE_MAX_DIST * BLACKHOLE_MAX_DIST && blackholeDistSqr < BLACKHOLE_MIN_DIST * BLACKHOLE_MIN_DIST)
        {
            float blackholeDist = Mathf.Sqrt(blackholeDistSqr);

            float lerp = Mathf.InverseLerp(BLACKHOLE_MAX_DIST, BLACKHOLE_MIN_DIST, blackholeDist);

            lerp = 1.0f - lerp;
            lerp *= lerp;
            lerp = 1.0f - lerp;

            float power = Mathf.Lerp(BLACKHOLE_STRENGTH_MAX, BLACKHOLE_STRENGTH_MIN, lerp) * ACCELERATION;

            Vector3 forward = Vector3.forward;

            if (controller != null)
            {
                forward = transform.rotation * Vector3.forward;
            }

            float dot = Vector3.Dot(forward, blackholeRelative);
            float multiplier = 1.0f;

            if (dot < 0.0f)
            {
                multiplier = 0.1f;
            }

            body.linearVelocity += (blackholeRelative / blackholeDist) * power * multiplier * Time.fixedDeltaTime;

            isBeingPulled = true;
        }

        if (controller != null)
        {
            Vector3 lookVector = Vector3.zero;

            if (controller.input != null)
            {
                inputVector = controller.input.GetMovementVector();
                lookVector = controller.frame * -controller.input.GetLookVector();
            }

            Vector3 direction = transform.rotation * Vector3.forward;
            Vector3 upDirection = transform.rotation * Vector3.up;

            float MINIMUM_DOT = 0.9f;
            float lookDot = Vector3.Dot(direction, lookVector);

            if (lookDot < MINIMUM_DOT)
            {
                {
                    //apply pid
                    Vector3 rotor = Vector3.Cross(direction, lookVector);
                    float distance = (direction - lookVector).magnitude;
                    Vector3 axis = rotor.normalized;

                    Vector3 P = axis * distance * KP;
                    Vector3 V = -body.angularVelocity * KD;

                    Vector3 control = P + V;

                    body.angularVelocity += control * Time.fixedDeltaTime;
                }

                {
                    //anti-roll
                    float roll = transform.eulerAngles.z;

                    if (roll > 180.0f)
                    {
                        roll -= 360.0f;
                    }

                    float sign = Mathf.Sign(roll);
                    float distance = Mathf.Abs(roll);

                    float p = sign * distance * KPA;
                    float v = -Vector3.Dot(body.angularVelocity, direction) * KDA;

                    float controlScalar = p + v;

                    body.angularVelocity -= direction * controlScalar * Time.fixedDeltaTime;
                }
            }
            else
            {
                Vector3 V = -body.angularVelocity * KD;
                Vector3 control = V;

                body.angularVelocity += control * Time.fixedDeltaTime;

                {
                    //anti-roll
                    float roll = transform.eulerAngles.z;

                    if (roll > 180.0f)
                    {
                        roll -= 360.0f;
                    }

                    float sign = Mathf.Sign(roll);
                    float distance = Mathf.Abs(roll);

                    float p = sign * distance * KPA;
                    float v = -Vector3.Dot(body.angularVelocity, direction) * KDA;

                    float controlScalar = p + v;

                    body.angularVelocity -= direction * controlScalar * Time.fixedDeltaTime;
                }
            }

            Vector3 input3 = new Vector3(-inputVector.x, 0.0f, inputVector.y);
            Vector3 acceleration = transform.rotation * input3 * ACCELERATION;

            float magnitude = body.linearVelocity.sqrMagnitude;

            if (magnitude > MAX_SPEED * MAX_SPEED)
            {
                float NEW_SPEED = Mathf.Sqrt(magnitude);

                body.linearVelocity += acceleration * Time.fixedDeltaTime;

                float magnitude2 = body.linearVelocity.sqrMagnitude;

                if (magnitude2 > NEW_SPEED * NEW_SPEED)
                {
                    float ratio = NEW_SPEED / body.linearVelocity.magnitude;
                    body.linearVelocity *= ratio;
                }
            }
            else
            {
                body.linearVelocity += acceleration * Time.fixedDeltaTime;
            }

            if (inputVector == Vector2.zero)
            {
                if (!isBeingPulled)
                {
                    float frictionScalar = Mathf.Pow(FRICTION, Time.fixedDeltaTime);
                    body.linearVelocity *= frictionScalar;
                }
            }
            else
            {
                float sideFrictionScalar = Mathf.Pow(SIDE_FRICTION, Time.fixedDeltaTime);

                Vector3 forward = acceleration.normalized;
                Vector3 normal = Vector3.Cross(forward, Vector3.up);
                Vector3 biNormal = Vector3.Cross(normal, forward);

                float f = Vector3.Dot(body.linearVelocity, forward);
                float n = Vector3.Dot(body.linearVelocity, normal);
                float bn = Vector3.Dot(body.linearVelocity, biNormal);

                n *= sideFrictionScalar;
                bn *= sideFrictionScalar;

                body.linearVelocity = forward * f + normal * n + biNormal * bn;
            }
        }
        else
        {
            Vector3 direction = transform.rotation * Vector3.forward;

            if (!isBeingPulled)
            {
                float frictionScalar = Mathf.Pow(FRICTION, Time.fixedDeltaTime);
                body.linearVelocity *= frictionScalar;

                Vector3 V = -body.angularVelocity * KD;
                Vector3 control = V;

                body.angularVelocity += control * Time.fixedDeltaTime;
            }

            {
                //anti-roll
                float roll = transform.eulerAngles.z;

                if (roll > 180.0f)
                {
                    roll -= 360.0f;
                }

                float sign = Mathf.Sign(roll);
                float distance = Mathf.Abs(roll);

                float p = sign * distance * KPA;
                float v = -Vector3.Dot(body.angularVelocity, direction) * KDA;

                float controlScalar = p + v;

                body.angularVelocity -= direction * controlScalar * Time.fixedDeltaTime;
            }

            if (type == EObject.SMALL_SHIP)
            {
                if (transform.position.y < GameServer.DEATH_Y + CUSHION)
                {
                    transform.position += Vector3.up * DRIFT * Time.fixedDeltaTime;
                }
                else if (transform.position.y > -GameServer.DEATH_Y - CUSHION)
                {
                    transform.position -= Vector3.up * DRIFT * Time.fixedDeltaTime;
                }
            }        
        }

        bool hasMoved = (startingPosition - transform.position).sqrMagnitude > EPSILON * EPSILON;

        if (hasMoved)
        {
            isStarted = true;
        }

        if (isStarted)
        {
            battery -= depletionRate * Time.fixedDeltaTime;

            if (battery <= 0.0f)
            {
                battery = 1.0f;
                isStarted = false;

                if (controller != null)
                {
                    controller.parentId = -1;
                    controller.frame = Quaternion.identity;
                    controller.isRouted = false;
                    controller.transform.SetParent(null);

                    PlayerEntity[] players = GetComponentsInChildren<PlayerEntity>();

                    for (int i = 0; i < players.Length; i++)
                    {
                        players[i].parentId = -1;
                        players[i].frame = Quaternion.identity;
                        players[i].transform.SetParent(null);
                    }

                    controller = null;
                }

                transform.position = startingPosition;
                transform.rotation = startingRotation;

                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        position.vector = transform.position;
        rotation.quaternion = transform.rotation;

        if (position.vector.x != previousPosition.x)
        {
            dirtyFlag = dirtyFlag | (int)ETransformEntityProperties.POSITION_X;
        }

        if (position.vector.y != previousPosition.y)
        {
            dirtyFlag = dirtyFlag | (int)ETransformEntityProperties.POSITION_Y;
        }

        if (position.vector.z != previousPosition.z)
        {
            dirtyFlag = dirtyFlag | (int)ETransformEntityProperties.POSITION_Z;
        }

        if (rotation.quaternion.x != previousRotation.x)
        {
            dirtyFlag = dirtyFlag | (int)ETransformEntityProperties.ROTATION_X;
            dirtyFlag = dirtyFlag | (int)ETransformEntityProperties.ROTATION_W;
        }

        if (rotation.quaternion.y != previousRotation.y)
        {
            dirtyFlag = dirtyFlag | (int)ETransformEntityProperties.ROTATION_Y;
            dirtyFlag = dirtyFlag | (int)ETransformEntityProperties.ROTATION_W;
        }

        if (rotation.quaternion.z != previousRotation.z)
        {
            dirtyFlag = dirtyFlag | (int)ETransformEntityProperties.ROTATION_Z;
            dirtyFlag = dirtyFlag | (int)ETransformEntityProperties.ROTATION_W;
        }

        if (rotation.quaternion.w != previousRotation.w)
        {
            dirtyFlag = dirtyFlag | (int)ETransformEntityProperties.ROTATION_W;
        }

        previousPosition = position.vector;
        previousRotation = rotation.quaternion;
    }

    public void OnDestroy()
    {
        //cleanup
        if (tail == null)
        {
            return;
        }

        Destroy(tail);
    }
}