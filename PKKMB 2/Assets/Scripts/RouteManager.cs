using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using Mapbox.Directions;
using Mapbox.Unity.Utilities;
using Mapbox.Unity;

public class RouteManager : MonoBehaviour
{
    public static RouteManager Instance;

    [Header("References")]
    public AbstractMap map;
    public LineRenderer lineRenderer;
    public Transform playerTransform;

    [Header("Settings")]
    public float heightOffset = 0.1f;
    public float waypointThreshold = 5f; 
    public float arrivalThreshold = 8f; 
    
    [Header("Shortcut Settings")]
    public int lookAheadCount = 5;

    // Cache Data
    private List<BuildingTrigger> sceneBuildings;
    private List<GoldenQuestTrigger> sceneNonBuildings;
    private Directions directions;

    // State Navigasi
    private bool isNavigating = false;
    private List<Vector3> currentRoutePath = new List<Vector3>(); 
    private Vector3 currentTargetPos; 

    // --- VARIABEL BARU UNTUK TUR BERURUTAN ---
    private List<string> activeTourList = new List<string>(); // Daftar antrian gedung
    private int currentTourIndex = -1; // Kita sedang di urutan keberapa?
    private bool isTourMode = false;   // Apakah sedang mode tur?

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        directions = MapboxAccess.Instance.Directions;
    }

    private void Start()
    {
        if (map == null) map = FindObjectOfType<AbstractMap>();
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        if (playerTransform == null)
        {
            GameObject p = GameObject.Find("PlayerTarget");
            if (p != null) playerTransform = p.transform;
        }
        ClearRoute();
        RefreshBuildingList();
    }

    public void RefreshBuildingList()
    {
        sceneBuildings = new List<BuildingTrigger>(FindObjectsOfType<BuildingTrigger>());
        sceneNonBuildings = new List<GoldenQuestTrigger>(FindObjectsOfType<GoldenQuestTrigger>());
    }

    // --- FUNGSI BARU: MEMULAI TUR (DIPANGGIL DARI START BUTTON) ---
    public void StartCampusTour(List<string> buildingIds)
    {
        if (buildingIds == null || buildingIds.Count == 0) return;

        Debug.Log($"🚀 Memulai Tur Kampus! Total tujuan: {buildingIds.Count}");
        
        // 1. Simpan Daftar Antrian
        activeTourList = new List<string>(buildingIds);
        currentTourIndex = 0; // Mulai dari gedung pertama (index 0)
        isTourMode = true;    // Aktifkan mode tur

        // 2. Gambar rute ke gedung PERTAMA
        DrawRouteToBuilding(activeTourList[0]);
    }

    // --- FUNGSI UTAMA: TARIK GARIS (SINGLE) ---
    public void DrawRouteToBuilding(string buildingId)
    {
        RefreshBuildingList(); 
        
        Transform targetTransform = GetTargetTransform(buildingId);

        if (targetTransform == null)
        {
            Debug.LogError($"❌ Gedung ID '{buildingId}' tidak ditemukan! Skip ke gedung selanjutnya...");
            // Kalau gedung gak ketemu, otomatis lanjut ke next step biar gak macet
            if(isTourMode) NextTourStep(); 
            return;
        }

        if (playerTransform == null || map == null) return;

        Debug.Log($"🗺️ Navigasi ke target: {buildingId}");
        currentTargetPos = targetTransform.position;

        Vector2d startGeo = map.WorldToGeoPosition(playerTransform.position);
        Vector2d endGeo = map.WorldToGeoPosition(targetTransform.position);

        var waypoints = new Vector2d[] { startGeo, endGeo };
        var directionResource = new DirectionResource(waypoints, RoutingProfile.Walking);
        directionResource.Steps = true;

        directions.Query(directionResource, OnDirectionsResponse);
    }

    private void Update()
    {
        if (!isNavigating || currentRoutePath == null || currentRoutePath.Count == 0) return;
        if (playerTransform == null) return;

        Vector3 playerPos = playerTransform.position;
        Vector3 lineStartPos = playerPos;
        lineStartPos.y += heightOffset; 
        currentRoutePath[0] = lineStartPos;

        if (currentRoutePath.Count <= 2)
        {
            CheckArrival(playerPos);
            RenderLine(currentRoutePath);
            return;
        }

        // Logic Shortcut (Look Ahead)
        int closestIndex = 1;
        float closestDistance = Vector3.Distance(playerPos, currentRoutePath[1]);
        int searchLimit = Mathf.Min(currentRoutePath.Count, lookAheadCount + 1);

        for (int i = 2; i < searchLimit; i++)
        {
            float dist = Vector3.Distance(playerPos, currentRoutePath[i]);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestIndex = i;
            }
        }

        if (closestIndex > 1) currentRoutePath.RemoveRange(1, closestIndex - 1);

        if (closestDistance < waypointThreshold)
        {
            if (currentRoutePath.Count > 1) currentRoutePath.RemoveAt(1);
        }

        CheckArrival(playerPos);
        RenderLine(currentRoutePath);
    }

    // --- LOGIC SAMPAI DI TUJUAN (DIMODIFIKASI) ---
    private void CheckArrival(Vector3 playerPos)
    {
        float distanceToTarget = Vector3.Distance(playerPos, currentTargetPos);
        
        if (distanceToTarget < arrivalThreshold)
        {
            Debug.Log("🏁 Sampai di satu titik tujuan!");

            // Jika sedang Mode Tur, lanjut ke gedung berikutnya
            if (isTourMode)
            {
                NextTourStep();
            }
            // Jika Mode Biasa (Daily Quest), langsung hapus
            else
            {
                ClearRoute();
            }
        }
    }

    // --- LOGIC PINDAH KE GEDUNG BERIKUTNYA ---
    private void NextTourStep()
    {
        currentTourIndex++; // Naik ke index berikutnya

        // Cek apakah masih ada gedung di daftar?
        if (currentTourIndex < activeTourList.Count)
        {
            string nextBuildingId = activeTourList[currentTourIndex];
            Debug.Log($"➡️ Lanjut ke tujuan #{currentTourIndex + 1}: {nextBuildingId}");
            
            // Gambar rute baru (otomatis rute lama terhapus di dalam fungsi ini)
            DrawRouteToBuilding(nextBuildingId);
        }
        else
        {
            Debug.Log("🎉 SELURUH TUR SELESAI! Selamat!");
            FinishTour();
        }
    }

    private void FinishTour()
    {
        isTourMode = false;
        activeTourList.Clear();
        ClearRoute();
        // Disini Anda bisa menambahkan logic reward atau pop-up "Event Complete"
    }

    void OnDirectionsResponse(DirectionsResponse response)
    {
        if (response == null || response.Routes == null || response.Routes.Count == 0) return;

        var routePoints = response.Routes[0].Geometry;
        currentRoutePath.Clear();

        Vector3 startPos = playerTransform.position;
        startPos.y += heightOffset;
        currentRoutePath.Add(startPos);

        foreach (var point in routePoints)
        {
            Vector3 worldPos = map.GeoToWorldPosition(point, true);
            worldPos.y += heightOffset; 
            currentRoutePath.Add(worldPos);
        }

        isNavigating = true;
        RenderLine(currentRoutePath);
    }

    void RenderLine(List<Vector3> path)
    {
        if (lineRenderer == null) return;
        lineRenderer.positionCount = path.Count;
        lineRenderer.SetPositions(path.ToArray());
        lineRenderer.enabled = true;
    }

    public void ClearRoute()
    {
        isNavigating = false;
        // Jika clear dipanggil paksa (manual), matikan mode tur juga biar gak error
        if(!isTourMode) currentRoutePath.Clear();
        
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }
    }

    private Transform GetTargetTransform(string id)
    {
        var b = sceneBuildings.FirstOrDefault(x => x.buildingId == id);
        if (b != null) return b.transform;
        var g = sceneNonBuildings.FirstOrDefault(x => x.buildingId == id);
        if (g != null) return g.transform;
        return null;
    }
}