using UnityEngine;

// wasd / arrows to walk hold left ctrl to crouch and walk more slowly
// the character controller handles collisions, slopes, and steps, see the movement guide for setup
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float crouchSpeed = 2.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField, Min(0f)] private float groundStickSpeed = 6f;

    [Header("Slopes and Stairs")]
    [SerializeField, Range(0f, 89f)] private float slopeLimit = 45f;
    [SerializeField, Min(0f)] private float stepHeight = 0.3f;

    [Header("Crouch")]
    [SerializeField, Range(0.1f, 1f)] private float crouchHeightMultiplier = 0.5f;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [Tooltip("Optional model transform. It must be a direct child of the player.")]
    [SerializeField] private Transform playerVisual;

    public bool IsCrouching { get; private set; }

    private const float MaximumFallSpeed = 50f;

    private CharacterController controller;
    private float standingHeight;
    private Vector3 standingCenter;
    private Vector3 visualStandingScale;
    private Vector3 visualStandingPosition;
    private float verticalVelocity;
    private readonly Collider[] standingOverlaps = new Collider[32];

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        // also supports a player that already had this script before the upgrade
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }

        standingHeight = Mathf.Max(controller.height, controller.radius * 2f);
        standingCenter = controller.center;
        controller.height = standingHeight;
        controller.minMoveDistance = 0f;

        if (playerVisual != null && playerVisual.parent != transform)
        {
            Debug.LogWarning("Player Visual must be a direct child of the player.", this);
            playerVisual = null;
        }

        if (playerVisual != null)
        {
            visualStandingScale = playerVisual.localScale;
            visualStandingPosition = playerVisual.localPosition;
        }
    }

    private void Update()
    {
        if (!controller.enabled)
        {
            return;
        }

        bool wasGrounded = controller.isGrounded;
        UpdateCrouch();
        MovePlayer(wasGrounded);
    }

    private void MovePlayer(bool wasGrounded)
    {
        // let unity handle climbing disable automatic steps while airborne
        controller.slopeLimit = slopeLimit;
        controller.stepOffset = wasGrounded ? Mathf.Min(stepHeight, controller.height) : 0f;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        // keep diagonal movement at the same speed as walking straight
        Vector3 direction = Vector3.ClampMagnitude(new Vector3(horizontal, 0f, vertical), 1f);
        float speed = IsCrouching ? crouchSpeed : moveSpeed;

        // apply downward movement to maintain contact when descending slopes
        if (wasGrounded && verticalVelocity <= 0f)
        {
            verticalVelocity = -groundStickSpeed;
        }
        verticalVelocity = Mathf.Max(verticalVelocity - Mathf.Abs(gravity) * Time.deltaTime, -MaximumFallSpeed);

        Vector3 velocity = direction * speed + Vector3.up * verticalVelocity;
        CollisionFlags collisions = controller.Move(velocity * Time.deltaTime);
        if ((collisions & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
        {
            verticalVelocity = -groundStickSpeed;
        }
    }

    private void UpdateCrouch()
    {
        bool wantsToCrouch = Input.GetKey(crouchKey);
        // releasing ctrl only stands the player up if there is room overhead
        if (!wantsToCrouch && IsCrouching && !CanStand())
        {
            wantsToCrouch = true;
        }

        if (wantsToCrouch == IsCrouching)
        {
            return;
        }

        IsCrouching = wantsToCrouch;
        // a capsule cannot be shorter than its diameter
        float height = IsCrouching
            ? Mathf.Max(standingHeight * crouchHeightMultiplier, controller.radius * 2f)
            : standingHeight;

        // keep the bottom of the capsule at the same position
        controller.stepOffset = Mathf.Min(controller.stepOffset, height);
        controller.height = height;
        controller.center = standingCenter + Vector3.up * ((height - standingHeight) * 0.5f);

        if (playerVisual != null)
        {
            float ratio = height / standingHeight;
            float feetY = standingCenter.y - standingHeight * 0.5f;
            Vector3 scale = visualStandingScale;
            scale.y *= ratio;
            Vector3 position = visualStandingPosition;
            position.y = feetY + (visualStandingPosition.y - feetY) * ratio;
            playerVisual.localScale = scale;
            playerVisual.localPosition = position;
        }
    }

    private bool CanStand()
    {
        // check the space the full standing capsule would occupy
        Vector3 scale = transform.lossyScale;
        float horizontalScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float worldRadius = controller.radius * horizontalScale;
        float worldHeight = Mathf.Max(standingHeight * Mathf.Abs(scale.y), worldRadius * 2f);
        Vector3 worldCenter = transform.TransformPoint(standingCenter);
        Vector3 sphereOffset = transform.up * (worldHeight * 0.5f - worldRadius);

        // inset the query so normal floor contact does not prevent standing
        float worldSkinWidth = controller.skinWidth * horizontalScale;
        float queryRadius = Mathf.Max(0.001f, worldRadius - worldSkinWidth);
        int count = Physics.OverlapCapsuleNonAlloc(worldCenter - sphereOffset, worldCenter + sphereOffset,
            queryRadius, standingOverlaps, Physics.AllLayers, QueryTriggerInteraction.Ignore);

        // if the buffer fills, stay crouched rather than miss a possible ceiling
        if (count == standingOverlaps.Length)
        {
            return false;
        }

        for (int i = 0; i < count; i++)
        {
            // ignore the player itself and objects excluded by collision settings
            Collider obstacle = standingOverlaps[i];
            if (obstacle.transform.IsChildOf(transform)
                || Physics.GetIgnoreLayerCollision(gameObject.layer, obstacle.gameObject.layer)
                || Physics.GetIgnoreCollision(controller, obstacle))
            {
                continue;
            }
            return false;
        }
        return true;
    }

    private void OnDisable()
    {
        verticalVelocity = 0f;
    }
}
