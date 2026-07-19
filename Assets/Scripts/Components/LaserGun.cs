using UnityEngine;

public class LaserGun : MonoBehaviour
{
    [Header("Laser Settings")]
    [SerializeField] private float laserWidth = 0.1f;
    [SerializeField] private Color laserColor = Color.red;
    
    [Header("Stage 1: Growth")]
    [SerializeField] private float growthDuration = 0.3f; // Time to reach target
    
    [Header("Stage 2: Hold")]
    [SerializeField] private float holdDuration = 0.5f; // Time to hold on target
    
    [Header("Effects")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float hitEffectLifetime = 1f;
    [SerializeField] private Material laserMaterial;

    [Header("Debug")]
    [SerializeField] private Transform target;
    [SerializeField] private bool isFire;
    
    private LineRenderer lineRenderer;
    private Transform currentTarget;
    private float fireTime;
    private bool isFiring;
    
    // Stage tracking
    private enum LaserStage { Growth, Hold, Finished }
    private LaserStage currentStage;
    private float targetDistance;
    private bool hasContactedTarget;
    
    void Awake()
    {
        SetupLineRenderer();
    }
    
    void SetupLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = laserWidth;
        lineRenderer.endWidth = laserWidth;
        
        if (laserMaterial != null)
        {
            lineRenderer.material = laserMaterial;
        }
        else
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            // Make it glow
            lineRenderer.material.EnableKeyword("_EMISSION");
            lineRenderer.material.SetColor("_EmissionColor", laserColor * 2f);
        }
        
        lineRenderer.startColor = laserColor;
        lineRenderer.endColor = laserColor;
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
    }
    
    void Update()
    {
        if (isFire)
        {
            isFire = false;
            Fire(target);
        }
        
        if (isFiring)
        {
            UpdateLaser();
        }
    }
    
    void UpdateLaser()
    {
        float elapsedTime = Time.time - fireTime;
        Vector3 startPos = transform.position;
        Vector3 endPos;
        
        if (currentTarget != null)
        {
            Vector3 direction = (currentTarget.position - startPos).normalized;
            targetDistance = Vector3.Distance(startPos, currentTarget.position);
            
            // STAGE 1: GROWTH - Laser growing towards target
            if (currentStage == LaserStage.Growth)
            {
                // Calculate current length based on time (0 to targetDistance over growthDuration)
                float growthProgress = elapsedTime / growthDuration;
                float currentLength = Mathf.Lerp(0f, targetDistance, growthProgress);
                
                // Raycast to check if we hit the target
                RaycastHit hit;
                if (Physics.Raycast(startPos, direction, out hit, currentLength))
                {
                    endPos = hit.point;
                    
                    // Check if we hit the target
                    if (hit.transform == currentTarget && !hasContactedTarget)
                    {
                        // === FIRST CONTACT WITH TARGET ===
                        OnTargetContact(hit);
                        hasContactedTarget = true;
                        currentStage = LaserStage.Hold;
                        fireTime = Time.time; // Reset timer for hold stage
                    }
                }
                else
                {
                    endPos = startPos + direction * currentLength;
                }
                
                // Check if growth phase completed
                if (growthProgress >= 1f && !hasContactedTarget)
                {
                    currentStage = LaserStage.Hold;
                    fireTime = Time.time;
                }
            }
            // STAGE 2: HOLD - Laser holding on target
            else if (currentStage == LaserStage.Hold)
            {
                // Keep laser at full length, tracking target
                RaycastHit hit;
                if (Physics.Raycast(startPos, direction, out hit, targetDistance * 2f))
                {
                    endPos = hit.point;
                }
                else
                {
                    endPos = currentTarget.position;
                }
                
                // Check if hold duration expired
                if (elapsedTime >= holdDuration)
                {
                    StopFiring();
                    return;
                }
            }
            else
            {
                endPos = startPos;
            }
        }
        else
        {
            // No target, shoot forward
            if (currentStage == LaserStage.Growth)
            {
                float growthProgress = elapsedTime / growthDuration;
                float currentLength = Mathf.Lerp(0f, 100f, growthProgress);
                endPos = startPos + transform.forward * currentLength;
                
                if (growthProgress >= 1f)
                {
                    currentStage = LaserStage.Hold;
                    fireTime = Time.time;
                }
            }
            else if (currentStage == LaserStage.Hold)
            {
                endPos = startPos + transform.forward * 100f;
                
                if (elapsedTime >= holdDuration)
                {
                    StopFiring();
                    return;
                }
            }
            else
            {
                endPos = startPos;
            }
        }
        
        // Update line renderer positions
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
    }
    
    /// <summary>
    /// Called when laser makes first contact with target
    /// THIS IS WHERE YOU ADD DAMAGE, EFFECTS, OR OTHER LOGIC
    /// </summary>
    void OnTargetContact(RaycastHit hit)
    {
        Debug.Log($"Laser contacted target: {hit.transform.name}");
        
        // Create hit effect at contact point
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(effect, hitEffectLifetime);
        }
        
        // ADD YOUR DAMAGE/EFFECT CODE HERE
        // Example: hit.transform.GetComponent<Enemy>()?.TakeDamage(10);
    }
    
    /// <summary>
    /// Fire the laser at a target
    /// </summary>
    /// <param name="targetTransform">Transform of the target to track</param>
    public void Fire(Transform targetTransform)
    {
        currentTarget = targetTransform;
        isFiring = true;
        fireTime = Time.time;
        lineRenderer.enabled = true;
        
        // Reset stage tracking
        currentStage = LaserStage.Growth;
        hasContactedTarget = false;
        targetDistance = 0f;
    }
    
    /// <summary>
    /// Fire the laser in the gun's forward direction
    /// </summary>
    public void Fire()
    {
        Fire(null);
    }
    
    void StopFiring()
    {
        isFiring = false;
        lineRenderer.enabled = false;
        currentTarget = null;
        currentStage = LaserStage.Finished;
        hasContactedTarget = false;
    }
    
    // Optional: Draw gizmo to visualize gun position
    void OnDrawGizmos()
    {
        if (isFiring && currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTarget.position);
            Gizmos.DrawWireSphere(currentTarget.position, 0.5f);
        }
    }
}