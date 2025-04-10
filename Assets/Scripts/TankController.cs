using System.Collections.Generic;
using UnityEngine;

public class TankController : MonoBehaviour
{
    // Movement & Camera
    public float moveSpeed = 5f;
    public float rotationSpeed = 5f; // Consider separate speeds for body and turret?
    public Vector3 cameraOffset = new(0, 5, -10);
    public Vector3 cameraRotation = new(30, 0, 0);

    // Turret & Shooting
    public Transform turretTransform;
    public Transform firePoint;
    public GameObject bulletPrefab;
    public Color teamColor = Color.blue;
    public int maxAmmo = 4;
    public float shootCooldown = 0.5f; // Min time between shots
	public List<GameObject> ActiveBullets { get; private set; } = new List<GameObject>();// Active bullets tracking

    // Reloading (Passive Regeneration)
    public float reloadInitialDelay = 1.5f;    // Time after LAST shot before FIRST bullet reload starts
    public float reloadSubsequentDelay = 0.5f; // Time between subsequent bullet reloads (after the first)

    // Private State
    private Camera mainCamera;
    private int currentAmmo;
    private bool CanShoot => Time.time >= nextShootTime;
    private float nextShootTime = 0f;
    private float nextPossibleReloadTime = float.PositiveInfinity; // When the next single bullet *could* be added. Init high.

    private static MaterialPropertyBlock _propBlock;

    void Start()
    {
        mainCamera = Camera.main;
        currentAmmo = maxAmmo;
        nextPossibleReloadTime = float.PositiveInfinity; // Don't reload initially

        // Null checks are good practice
        if (mainCamera == null) Debug.LogError("Main Camera not found!");
        if (turretTransform == null) Debug.LogError("Turret Transform not assigned!");
        if (firePoint == null) Debug.LogError("Fire Point not assigned!");
        if (bulletPrefab == null) Debug.LogError("Bullet Prefab not assigned!");
    }

    private void Update() {
        HandleMovement();
        HandleCamera();
        AimTransformAtMouse(turretTransform);
        HandleShootingAndReloading(); // Combined logic
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 targetDirection = new Vector3(horizontal, 0, vertical).normalized;

        if (targetDirection != Vector3.zero) {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (horizontal != 0 || vertical != 0)
        {
            transform.Translate(moveSpeed * Time.deltaTime * Vector3.forward, Space.Self);
        }
    }

    void HandleCamera()
    {
        if (mainCamera != null) {
			mainCamera.transform.SetPositionAndRotation(transform.position + cameraOffset, Quaternion.Euler(cameraRotation));
		}
    }

    void AimTransformAtMouse(Transform objTransform)
    {
         if (mainCamera == null || objTransform == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane targetPlane = new Plane(Vector3.up, objTransform.position);

        if (targetPlane.Raycast(ray, out float distance))
        {
            Vector3 targetPoint = ray.GetPoint(distance);
            Vector3 directionToTarget = targetPoint - objTransform.position;
            directionToTarget.y = 0;

            if (directionToTarget.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                 objTransform.rotation = targetRotation;
            }
        }
    }

    void HandleShootingAndReloading()
    {
        // --- Shooting ---
        bool wantsToShoot = Input.GetButton("Fire1"); // Only auto-fire while holding

        if (wantsToShoot && currentAmmo > 0 && CanShoot)
        {
            Shoot(); // Perform the shot action
            currentAmmo--;
            nextShootTime = Time.time + shootCooldown; // Set cooldown for next SHOT

            // --- Reset Reload Timer on Shot ---
            // Schedule the *potential* first reload after the initial delay
            nextPossibleReloadTime = Time.time + reloadInitialDelay;

            // Debug.Log($"Shot! Ammo: {currentAmmo}/{maxAmmo}. Next reload check at: {nextPossibleReloadTime:F2}");
        }

        // --- Passive Reload Check ---
        // Check if ammo is missing AND enough time has passed for the next potential reload
        if (currentAmmo < maxAmmo && Time.time >= nextPossibleReloadTime)
        {
            currentAmmo++; // Reload one bullet

            // Schedule the NEXT reload check using the subsequent delay
            nextPossibleReloadTime = Time.time + reloadSubsequentDelay;

            // Debug.Log($"Reloaded! Ammo: {currentAmmo}/{maxAmmo}. Next reload check at: {nextPossibleReloadTime:F2}");

            // Optional: If ammo is now full, we can stop checking by setting time high
            if (currentAmmo == maxAmmo) {
                 nextPossibleReloadTime = float.PositiveInfinity;
                 // Debug.Log("Ammo Full!");
            }
        }
    }

	private void Shoot()
	{
		if (bulletPrefab != null && firePoint != null)
		{
			GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
			bullet.GetComponent<Bullet>().parent = gameObject; // Set the parent for collision checks
			ActiveBullets.Add(bullet); // Track active bullets
			
			_propBlock ??= new MaterialPropertyBlock();
			
			// Get renderers from both the bullet itself and all its children
			Renderer[] renderers = bullet.GetComponentsInChildren<Renderer>();
			
			// Apply team color to all found renderers
			foreach (Renderer renderer in renderers)
			{
				renderer.GetPropertyBlock(_propBlock);
				_propBlock.SetColor("_BaseColor", teamColor);
				renderer.SetPropertyBlock(_propBlock);
			}
		}
	}
    
    public void RemoveBullet(GameObject bulletToRemove)
    {
        ActiveBullets.Remove(bulletToRemove);
    }

    // Optional: For UI display
    public int GetCurrentAmmo() => currentAmmo;
}