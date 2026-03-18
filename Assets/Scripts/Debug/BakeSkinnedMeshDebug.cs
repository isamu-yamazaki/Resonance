using UnityEngine;

public class BakeSkinnedMeshDebug : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer _source;

    [ContextMenu("Bake and Spawn")]
    public void BakeAndSpawn()
    {
        var mesh = new Mesh();
        _source.BakeMesh(mesh);

        var go = new GameObject("BakedSkinnedMesh");
        go.transform.SetPositionAndRotation(_source.transform.position, _source.transform.rotation);
        go.AddComponent<MeshFilter>().mesh = mesh;
        go.AddComponent<MeshRenderer>().materials = _source.materials;
    }
}
