namespace Mapbox.Unity.MeshGeneration.Factories
{
    using UnityEngine;
    using Mapbox.Directions;
    using System.Collections.Generic;
    using System.Linq;
    using Mapbox.Unity.Map;
    using Data;
    using Modifiers;
    using Mapbox.Utils;
    using Mapbox.Unity.Utilities;

    public class DirectionsFactory : MonoBehaviour
    {
        [SerializeField]
        AbstractMap _map;

        [SerializeField]
        MeshModifier[] MeshModifiers;
        [SerializeField]
        Material _material;

        [SerializeField]
        public Transform[] _waypoints;
        private List<Vector3> _cachedWaypoints;
        
        [Header("Update Settings")]
        [SerializeField]
        [Tooltip("Jarak minimum (dalam meter Unity) waypoint harus bergerak sebelum rute dihitung ulang.")]
        private float _recalculationThreshold = 10f;

        [SerializeField]
        [Tooltip("Jika jarak ke tujuan kurang dari nilai ini, rute akan otomatis dihancurkan.")]
        private float _clearRouteDistance = 1f;

        private Directions _directions;
        private int _counter;
        GameObject _directionsGO;
        private bool _isQuerying = false;

        public string targetId;

        protected virtual void Awake()
        {
            if (_map == null)
            {
                _map = FindObjectOfType<AbstractMap>();
            }
            _directions = MapboxAccess.Instance.Directions;
        }

        public void Start()
        {
            _cachedWaypoints = new List<Vector3>(_waypoints.Length);
            foreach (var item in _waypoints)
            {
                _cachedWaypoints.Add(item.position);
            }

            foreach (var modifier in MeshModifiers)
            {
                modifier.Initialize();
            }

            Query();
        }

        void Update()
        {
            if (_isQuerying)
            {
                return;
            }
            
            // Pengecekan ini sekarang akan menghentikan SEMUA logika di bawahnya jika rute sudah di-clear.
            if (_waypoints[0] == null || _waypoints[1] == null) return;

            // Logika Recalculate Rute
            bool needsRecalculation = false;
            for (int i = 0; i < _waypoints.Length; i++)
            {
                if (Vector3.Distance(_waypoints[i].position, _cachedWaypoints[i]) > _recalculationThreshold)
                {
                    needsRecalculation = true;
                    break;
                }
            }

            if (needsRecalculation)
            {
                Query();
            }
            
            // if (_directionsGO != null)
            // {
            //     float distanceToDestination = Vector3.Distance(_waypoints[0].position, _waypoints[1].position);
            //     if (distanceToDestination <= _clearRouteDistance)
            //     {
            //         Debug.Log("Tujuan tercapai! Menghapus rute.");
            //         ClearRoute();
            //     }
            // }
        }

        void Query()
        {
            if (_waypoints[0] == null || _waypoints[1] == null)
            {
                Debug.LogWarning("Query dibatalkan karena salah satu waypoint null.");
                return;
            }
            
            _isQuerying = true; 

            var count = _waypoints.Length;
            var wp = new Vector2d[count];
            for (int i = 0; i < count; i++)
            {
                wp[i] = _waypoints[i].GetGeoPosition(_map.CenterMercator, _map.WorldRelativeScale);
                _cachedWaypoints[i] = _waypoints[i].position;
            }
            var _directionResource = new DirectionResource(wp, RoutingProfile.Walking);
            _directionResource.Steps = true;
            _directions.Query(_directionResource, HandleDirectionsResponse);
        }

       public void SetRoute(Transform newStartPoint, Transform newDestination,string idBuilding)
       {
           if (newStartPoint == null || newDestination == null)
           {
               Debug.LogError("Titik awal atau tujuan baru tidak valid (null).", this);
               return;
           }

           Debug.Log($"Rute diubah ke: '{newStartPoint.name}' -> '{newDestination.name}'");
           
           _waypoints[0] = newStartPoint;
           _waypoints[1] = newDestination;
            targetId = idBuilding;

           Query();
       }

        void HandleDirectionsResponse(DirectionsResponse response)
        {
            if (response == null || null == response.Routes || response.Routes.Count < 1)
            {
                _isQuerying = false;
                return;
            }

            var meshData = new MeshData();
            var dat = new List<Vector3>();
            foreach (var point in response.Routes[0].Geometry)
            {
                dat.Add(Conversions.GeoToWorldPosition(point.x, point.y, _map.CenterMercator, _map.WorldRelativeScale).ToVector3xz());
            }

            var feat = new VectorFeatureUnity();
            feat.Points.Add(dat);

            foreach (MeshModifier mod in MeshModifiers.Where(x => x.Active))
            {
                mod.Run(feat, meshData, _map.WorldRelativeScale);
            }

            CreateGameObject(meshData);
            _isQuerying = false;
        }

        GameObject CreateGameObject(MeshData data)
        {
            if (_directionsGO != null)
            {
                Destroy(_directionsGO);
            }
            _directionsGO = new GameObject("direction waypoint entity");
            var mesh = _directionsGO.AddComponent<MeshFilter>().mesh;
            mesh.subMeshCount = data.Triangles.Count;

            mesh.SetVertices(data.Vertices);
            _counter = data.Triangles.Count;
            for (int i = 0; i < _counter; i++)
            {
                var triangle = data.Triangles[i];
                mesh.SetTriangles(triangle, i);
            }

            _counter = data.UV.Count;
            for (int i = 0; i < _counter; i++)
            {
                var uv = data.UV[i];
                mesh.SetUVs(i, uv);
            }

            mesh.RecalculateNormals();
            _directionsGO.AddComponent<MeshRenderer>().material = _material;
            return _directionsGO;
        }

        public void HideRoute()
        {
            if (_directionsGO != null)
            {
                _directionsGO.SetActive(false);
            }
        }

        public void ShowRoute()
        {
            if (_directionsGO != null)
            {
                _directionsGO.SetActive(true);
            }
        }

        public void ClearRoute()
        {
            if (_directionsGO != null)
            {
                Destroy(_directionsGO);
            }

            // ✅ PERBAIKAN: Set waypoints menjadi null untuk menghentikan semua proses di Update().
            if (_waypoints != null && _waypoints.Length > 1)
            {
                _waypoints[0] = null;
                _waypoints[1] = null;
            }
        }


        // Speciall
    }
}