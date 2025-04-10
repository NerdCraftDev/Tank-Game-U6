using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Image
using System.Collections.Generic; // Required for List

[RequireComponent(typeof(RectTransform))] // Ensure this script is on a UI element
[RequireComponent(typeof(HorizontalLayoutGroup))] // We rely on this for layout
public class AmmoUIController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The TankController to monitor for ammo changes.")]
    public TankController targetTank; // Assign the player's TankController

    [Tooltip("The sprite to use for each ammo icon.")]
    public Sprite ammoIconSprite; // Assign your ammo icon sprite here

    [Header("Appearance")]
    [Tooltip("The alpha value (0=transparent, 1=opaque) for unloaded ammo icons.")]
    [Range(0f, 1f)]
    public float unloadedAlpha = 0.3f;

    // Private variables
    private List<Image> ammoImages = new List<Image>(); // Managed internally now
    private int lastKnownAmmo = -1; // For optimization
    private Color currentLoadedColor;
    private Color currentUnloadedColor;
    private bool needsVisualUpdate = true; // Flag to force update on changes

    void Start()
    {
        if (!ValidateSetup())
        {
            enabled = false; // Disable script if setup is wrong
            return;
        }

        // Clear any existing icons this script might have created previously
        // or if the user added children manually
        ClearExistingIcons();

        // Create the required number of icons
        InstantiateAmmoIcons();

        // Perform initial visual update
        ForceUpdateAmmoDisplay();
    }

    void Update()
    {
        // Simple check if tank still exists
        if (targetTank == null)
        {
            // Optionally disable icons or the panel if the tank is destroyed
            // gameObject.SetActive(false); // Example
            return;
        }

        int currentAmmo = targetTank.GetCurrentAmmo();
        Color currentTeamColor = targetTank.teamColor;

        // Check if ammo count or team color has changed, requiring a visual update
        if (currentAmmo != lastKnownAmmo || currentLoadedColor != currentTeamColor)
        {
            needsVisualUpdate = true;
            lastKnownAmmo = currentAmmo;
            // Recalculate colors based on potentially changed team color
            currentLoadedColor = currentTeamColor;
            currentUnloadedColor = new Color(currentTeamColor.r, currentTeamColor.g, currentTeamColor.b, unloadedAlpha);
        }

        // Apply visual update if needed
        if (needsVisualUpdate)
        {
            UpdateAmmoDisplayVisuals();
            needsVisualUpdate = false;
        }
    }

    // Public method to force an update if needed externally (e.g., Max Ammo changes)
    public void ForceUpdateAmmoDisplay()
    {
        if (!Application.isPlaying || !ValidateSetup()) return; // Don't run outside play mode or if invalid

        lastKnownAmmo = -1; // Ensure Update logic runs
        needsVisualUpdate = true; // Flag for update
        ClearExistingIcons();
        InstantiateAmmoIcons();
        // The Update method will handle the visual refresh on the next frame
    }


    bool ValidateSetup()
    {
        if (targetTank == null)
        {
            Debug.LogError("AmmoUIController: Target TankController not assigned!", this);
            return false;
        }
        if (ammoIconSprite == null)
        {
            Debug.LogError("AmmoUIController: Ammo Icon Sprite not assigned!", this);
            return false;
        }
        return true;
    }

    void ClearExistingIcons()
    {
        // Destroy previously instantiated icons before creating new ones
        foreach (Image img in ammoImages)
        {
            if (img != null && img.gameObject != null)
            {
                Destroy(img.gameObject);
            }
        }
        ammoImages.Clear();

        // Also destroy any other children just in case
        foreach (Transform child in transform) {
             Destroy(child.gameObject);
         }
    }

    void InstantiateAmmoIcons()
    {
        if (targetTank == null || ammoIconSprite == null) return;

        for (int i = 0; i < targetTank.maxAmmo; i++)
        {
            // Create a new GameObject for the icon
            GameObject iconGO = new GameObject($"AmmoIcon_{i}");
            // Set its parent to this panel. RectTransform is handled automatically.
            iconGO.transform.SetParent(transform, false);

            // Add the Image component
            Image iconImage = iconGO.AddComponent<Image>();
            iconImage.sprite = ammoIconSprite;
            iconImage.preserveAspect = true; // Often looks better for icons
            // Disable raycasting unless needed
            iconImage.raycastTarget = false;

            // Add to our managed list
            ammoImages.Add(iconImage);
        }
        // Reset lastKnownAmmo to ensure the display updates correctly
        lastKnownAmmo = -1;
        needsVisualUpdate = true;
    }


    void UpdateAmmoDisplayVisuals()
    {
        int currentAmmo = targetTank.GetCurrentAmmo();

        // Loop through all the UI image slots we manage
        for (int i = 0; i < ammoImages.Count; i++)
        {
            if (ammoImages[i] != null)
            {
                // Set color based on whether this index is < current ammo count
                ammoImages[i].color = (i < currentAmmo) ? currentLoadedColor : currentUnloadedColor;
            }
        }
    }
}