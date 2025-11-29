using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class LaserTurretView : BaseTurretView
    {
        [Header("Laser Settings")] [SerializeField]
        private float laserWidth = 0.1f;

        [SerializeField] private Color laserColor = Color.red;

        [Header("Stage 1: Growth")] 
        [SerializeField] private float growthDuration = 0.3f; // Time to reach target

        [Header("Stage 2: Hold")]
        [SerializeField] private float holdDuration = 0.5f; // Time to hold on target

        
        [SerializeField] private Material laserMaterial;

        private LineRenderer _lineRenderer;
        private Transform _currentTarget;
        private float _fireTime;
        private bool _isFiring;

        // Stage tracking
        private enum LaserStage
        {
            Growth,
            Hold,
            Finished
        }

        private LaserStage _currentStage;
        private float _targetDistance;
        private bool _hasContactedTarget;


        private bool _isAttacking;

        private void Awake()
        {
            SetupLineRenderer();
        }

        private void SetupLineRenderer()
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.startWidth = laserWidth;
            _lineRenderer.endWidth = laserWidth;

            if (laserMaterial != null)
            {
                _lineRenderer.material = laserMaterial;
            }
            else
            {
                _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                // Make it glow
                _lineRenderer.material.EnableKeyword("_EMISSION");
                _lineRenderer.material.SetColor("_EmissionColor", laserColor * 2f);
            }

            _lineRenderer.startColor = laserColor;
            _lineRenderer.endColor = laserColor;
            _lineRenderer.positionCount = 2;
            _lineRenderer.enabled = false;
        }


        public override void Attack(IHardPointModel hardPointModel, out float duration)
        {
            
            //float distance = Vector3.Distance(hardPointModel.Position, transform.position);
            duration = growthDuration;
            _hardPointModel = hardPointModel;

            _attackTimer
                .ChangeDelay(duration)
                .StartTimer();

            _busyTimer
                .ChangeDelay(_projectileData.Delay + duration + holdDuration)
                .StartTimer();
            
            _currentTarget = hardPointModel.Transform;
            _isFiring = true;
            _fireTime = Time.time;
            _lineRenderer.enabled = true;
        
            // Reset stage tracking
            _currentStage = LaserStage.Growth;
            _hasContactedTarget = false;
            _targetDistance = 0f;

            _isAttacking = true;
        }
        
   
        public override void SetParent(Transform parent)
        {
            transform.SetParent(parent);
            transform.localPosition = Vector3.zero;
        }

        private void Update()
        {
            if (_isFiring)
            {
                UpdateLaser();
            }
        }

        private void UpdateLaser()
        {
            float elapsedTime = Time.time - _fireTime;
            Vector3 startPos = transform.position;
            Vector3 endPos;

            if (_currentTarget != null)
            {
                Vector3 direction = (_currentTarget.position - startPos).normalized;
                _targetDistance = Vector3.Distance(startPos, _currentTarget.position);

                // STAGE 1: GROWTH - Laser growing towards target
                if (_currentStage == LaserStage.Growth)
                {
                    // Calculate current length based on time (0 to targetDistance over growthDuration)
                    float growthProgress = elapsedTime / growthDuration;
                    float currentLength = Mathf.Lerp(0f, _targetDistance, growthProgress);

                    // Raycast to check if we hit the target
                    RaycastHit hit;
                    if (Physics.Raycast(startPos, direction, out hit, currentLength))
                    {
                        endPos = hit.point;

                        // Check if we hit the target
                        if (hit.transform == _currentTarget && !_hasContactedTarget)
                        {
                            // === FIRST CONTACT WITH TARGET ===
                            OnTargetContact(hit);
                            _hasContactedTarget = true;
                            _currentStage = LaserStage.Hold;
                            _fireTime = Time.time; // Reset timer for hold stage
                        }
                    }
                    else
                    {
                        endPos = startPos + direction * currentLength;
                    }

                    // Check if growth phase completed
                    if (growthProgress >= 1f && !_hasContactedTarget)
                    {
                        _currentStage = LaserStage.Hold;
                        _fireTime = Time.time;
                    }
                }
                // STAGE 2: HOLD - Laser holding on target
                else if (_currentStage == LaserStage.Hold)
                {
                    // Keep laser at full length, tracking target
                    RaycastHit hit;
                    if (Physics.Raycast(startPos, direction, out hit, _targetDistance * 2f))
                    {
                        endPos = hit.point;
                    }
                    else
                    {
                        endPos = _currentTarget.position;
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
                if (_currentStage == LaserStage.Growth)
                {
                    float growthProgress = elapsedTime / growthDuration;
                    float currentLength = Mathf.Lerp(0f, 100f, growthProgress);
                    endPos = startPos + transform.forward * currentLength;

                    if (growthProgress >= 1f)
                    {
                        _currentStage = LaserStage.Hold;
                        _fireTime = Time.time;
                    }
                }
                else if (_currentStage == LaserStage.Hold)
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
            _lineRenderer.SetPosition(0, startPos);
            _lineRenderer.SetPosition(1, endPos);
        }
        
            
        private void StopFiring()
        {
            _isFiring = false;
            _lineRenderer.enabled = false;
            _currentTarget = null;
            _currentStage = LaserStage.Finished;
            _hasContactedTarget = false;
        }

        /// <summary>
        /// Called when laser makes first contact with target
        /// THIS IS WHERE YOU ADD DAMAGE, EFFECTS, OR OTHER LOGIC
        /// </summary>
        private void OnTargetContact(RaycastHit hit)
        {
            Debug.Log($"Laser contacted target: {hit.transform.name}");
        }
    }
}