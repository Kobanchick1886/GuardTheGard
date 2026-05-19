using UnityEngine;
using System.Collections.Generic;

public class ThreatRing : MonoBehaviour
{
    [Header("UI Settings")]
    [Tooltip("A Sprite of an arrow pointing perfectly to the RIGHT.")]
    public GameObject arrowPrefab;

    [Tooltip("Distance from the edge of the screen (0.0 to 1.0). 0.05 keeps it slightly inside the screen.")]
    public float edgeMargin = 0.05f;

    [Tooltip("Scale modifier. 0.66 makes the arrow exactly 1.5x smaller.")]
    public float arrowScale = 0.66f;

    private Dictionary<GameObject, GameObject> activeArrows = new Dictionary<GameObject, GameObject>();
    private Camera mainCam;

    void Start()
    {
        // Cache the main camera to save performance
        mainCam = Camera.main;
    }

    void Update()
    {
        SyncArrowsWithEnemies();
        UpdateArrowPositions();
    }

    private void SyncArrowsWithEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        HashSet<GameObject> currentEnemies = new HashSet<GameObject>(enemies);
        List<GameObject> deadEnemies = new List<GameObject>();

        // 1. Destroy arrows for enemies that died or were destroyed
        foreach (var enemy in activeArrows.Keys)
        {
            if (enemy == null || !currentEnemies.Contains(enemy))
            {
                if (activeArrows[enemy] != null) Destroy(activeArrows[enemy]);
                deadEnemies.Add(enemy);
            }
        }
        foreach (var dead in deadEnemies) activeArrows.Remove(dead);

        // 2. Create or destroy arrows based on camera visibility
        foreach (GameObject enemy in enemies)
        {
            // Convert enemy's world position to screen viewport (0 to 1)
            Vector3 viewportPos = mainCam.WorldToViewportPoint(enemy.transform.position);

            // Check if the enemy is completely outside the visible screen
            bool isOffScreen = viewportPos.x < 0 || viewportPos.x > 1 || viewportPos.y < 0 || viewportPos.y > 1;

            if (isOffScreen && !activeArrows.ContainsKey(enemy))
            {
                // Enemy went off-screen, create an arrow
                GameObject newArrow = Instantiate(arrowPrefab, transform.position, Quaternion.identity);

                // Requirement 3: Make the arrow 1.5x smaller (1 / 1.5 = 0.66)
                newArrow.transform.localScale *= arrowScale;

                activeArrows.Add(enemy, newArrow);
            }
            else if (!isOffScreen && activeArrows.ContainsKey(enemy))
            {
                // Enemy entered the screen, destroy the arrow
                Destroy(activeArrows[enemy]);
                activeArrows.Remove(enemy);
            }
        }
    }

    private void UpdateArrowPositions()
    {
        // 3. Pin the arrows to the edge of the screen and rotate them
        foreach (KeyValuePair<GameObject, GameObject> pair in activeArrows)
        {
            GameObject enemy = pair.Key;
            GameObject arrow = pair.Value;

            if (enemy != null && arrow != null)
            {
                Vector3 enemyPos = enemy.transform.position;
                Vector3 viewportPos = mainCam.WorldToViewportPoint(enemyPos);

                // Requirement 1: Clamp the position exactly to the camera's edges
                viewportPos.x = Mathf.Clamp(viewportPos.x, edgeMargin, 1f - edgeMargin);
                viewportPos.y = Mathf.Clamp(viewportPos.y, edgeMargin, 1f - edgeMargin);

                // Convert the clamped viewport math back into a real World Position
                Vector3 edgeWorldPos = mainCam.ViewportToWorldPoint(viewportPos);
                edgeWorldPos.z = 0f; // Force 2D layer so it doesn't clip behind the background

                arrow.transform.position = edgeWorldPos;

                // Rotation: Point the arrow from its edge position directly at the enemy
                Vector3 direction = (enemyPos - edgeWorldPos).normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                arrow.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }
}