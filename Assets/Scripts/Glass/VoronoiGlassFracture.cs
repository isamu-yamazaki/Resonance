using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Resonance.Environment
{
    public static class VoronoiGlassFracture
    {
        private const int GridResolution = 48;

        public static List<GameObject> Generate(
            Transform paneTransform,
            Vector2 paneSize,
            float thickness,
            int shardCount,
            Vector2 hitPointLocal,
            Material material,
            float shardMass)
        {
            List<Vector2> seeds = GenerateSeeds(shardCount, paneSize, hitPointLocal);
            int[] cellMap = BuildCellMap(seeds, paneSize);
            var shards = new List<GameObject>(seeds.Count);

            for (int i = 0; i < seeds.Count; i++)
            {
                Mesh mesh = BuildShardMesh(i, cellMap, seeds, paneSize, thickness);
                if (mesh == null) continue;

                GameObject go = SpawnShard(mesh, material, paneTransform, shardMass);
                if (go != null)
                    shards.Add(go);
            }

            return shards;
        }

        private static List<Vector2> GenerateSeeds(int count, Vector2 paneSize, Vector2 hitPointLocal)
        {
            var seeds = new List<Vector2>(count);
            float hw = paneSize.x * 0.5f;
            float hh = paneSize.y * 0.5f;

            int clustered = Mathf.RoundToInt(count * 0.6f);
            for (int i = 0; i < clustered; i++)
            {
                Vector2 offset = Random.insideUnitCircle * (Mathf.Min(paneSize.x, paneSize.y) * 0.25f);
                Vector2 seed = hitPointLocal + offset;
                seed.x = Mathf.Clamp(seed.x, -hw, hw);
                seed.y = Mathf.Clamp(seed.y, -hh, hh);
                seeds.Add(seed);
            }

            int scattered = count - clustered;
            for (int i = 0; i < scattered; i++)
                seeds.Add(new Vector2(Random.Range(-hw, hw), Random.Range(-hh, hh)));

            return seeds;
        }

        private static int[] BuildCellMap(List<Vector2> seeds, Vector2 paneSize)
        {
            int res = GridResolution;
            int[] cellMap = new int[res * res];

            for (int py = 0; py < res; py++)
            {
                for (int px = 0; px < res; px++)
                {
                    float lx = Mathf.Lerp(-paneSize.x * 0.5f, paneSize.x * 0.5f, (px + 0.5f) / res);
                    float ly = Mathf.Lerp(-paneSize.y * 0.5f, paneSize.y * 0.5f, (py + 0.5f) / res);
                    int nearest = 0;
                    float minDistSq = float.MaxValue;

                    for (int s = 0; s < seeds.Count; s++)
                    {
                        float dx = lx - seeds[s].x;
                        float dy = ly - seeds[s].y;
                        float dSq = dx * dx + dy * dy;
                        if (dSq < minDistSq) { minDistSq = dSq; nearest = s; }
                    }

                    cellMap[py * res + px] = nearest;
                }
            }

            return cellMap;
        }

        private static Mesh BuildShardMesh(int cellIndex, int[] cellMap, List<Vector2> seeds, Vector2 paneSize, float thickness)
        {
            int res = GridResolution;
            var pixelPoints = new List<Vector2>();

            for (int py = 0; py < res; py++)
            {
                for (int px = 0; px < res; px++)
                {
                    if (cellMap[py * res + px] != cellIndex) continue;
                    float lx = Mathf.Lerp(-paneSize.x * 0.5f, paneSize.x * 0.5f, (px + 0.5f) / res);
                    float ly = Mathf.Lerp(-paneSize.y * 0.5f, paneSize.y * 0.5f, (py + 0.5f) / res);
                    pixelPoints.Add(new Vector2(lx, ly));
                }
            }

            if (pixelPoints.Count < 3) return null;

            List<Vector2> hull = ConvexHull(pixelPoints);
            if (hull.Count < 3) return null;

            Vector2 centroid = Centroid(hull);
            for (int i = 0; i < hull.Count; i++)
                hull[i] -= centroid;

            Mesh mesh = ExtrudePolygon(hull, thickness);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.name = $"Shard_{cellIndex}|{centroid.x.ToString("F4", CultureInfo.InvariantCulture)}|{centroid.y.ToString("F4", CultureInfo.InvariantCulture)}";

            return mesh;
        }

        private static Mesh ExtrudePolygon(List<Vector2> poly, float thickness)
        {
            int n = poly.Count;
            float halfT = thickness * 0.5f;

            var verts = new Vector3[n * 2];
            var uvs = new Vector2[n * 2];

            for (int i = 0; i < n; i++)
            {
                verts[i] = new Vector3(poly[i].x, poly[i].y, halfT);
                verts[i + n] = new Vector3(poly[i].x, poly[i].y, -halfT);
                uvs[i] = new Vector2(poly[i].x, poly[i].y);
                uvs[i + n] = new Vector2(poly[i].x, poly[i].y);
            }

            var tris = new List<int>();

            for (int i = 1; i < n - 1; i++) { tris.Add(0); tris.Add(i); tris.Add(i + 1); }
            for (int i = 1; i < n - 1; i++) { tris.Add(n); tris.Add(n + i + 1); tris.Add(n + i); }

            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                tris.Add(i);     tris.Add(next);     tris.Add(i + n);
                tris.Add(next);  tris.Add(next + n); tris.Add(i + n);
            }

            Mesh mesh = new Mesh();
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris.ToArray();
            return mesh;
        }

        private static GameObject SpawnShard(Mesh mesh, Material material, Transform paneTransform, float mass)
        {
            string[] parts = mesh.name.Split('|');
            if (parts.Length < 3) return null;
            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float cx)) return null;
            if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float cy)) return null;

            Vector3 worldPos = paneTransform.position + paneTransform.rotation * new Vector3(cx, cy, 0f);

            GameObject go = new GameObject(parts[0]);
            go.transform.SetPositionAndRotation(worldPos, paneTransform.rotation);
            go.layer = LayerMask.NameToLayer("GlassShard");

            go.AddComponent<MeshFilter>().mesh = mesh;
            go.AddComponent<MeshRenderer>().material = material;

            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = true;

            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            go.AddComponent<AkGameObj>();
            go.AddComponent<WwiseSmartOcclusion>();

            return go;
        }

        private static List<Vector2> ConvexHull(List<Vector2> points)
        {
            int n = points.Count;
            if (n < 3) return points;

            points.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

            var hull = new List<Vector2>(2 * n);

            for (int i = 0; i < n; i++)
            {
                while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], points[i]) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(points[i]);
            }

            int lower = hull.Count + 1;
            for (int i = n - 2; i >= 0; i--)
            {
                while (hull.Count >= lower && Cross(hull[hull.Count - 2], hull[hull.Count - 1], points[i]) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(points[i]);
            }

            hull.RemoveAt(hull.Count - 1);
            return hull;
        }

        private static float Cross(Vector2 o, Vector2 a, Vector2 b)
            => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

        private static Vector2 Centroid(List<Vector2> poly)
        {
            Vector2 sum = Vector2.zero;
            foreach (var p in poly) sum += p;
            return sum / poly.Count;
        }
    }
}
