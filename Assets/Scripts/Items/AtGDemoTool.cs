using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public class AtGDemoTool : MonoBehaviour {
    public Transform StartTransform;

    public List<Transform> EndTransforms = new();

    public AtGMissileProjectileDemo ProjectilePrefab;
    
    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {
        
    }

    public static int lastIndex = 0;

    public void SpawnProjectile() {
        StartCoroutine(Spawn());
    }

    private IEnumerator Spawn() {
        // Pick a random transform in the list
        AtGMissileProjectileDemo projectile = Instantiate(
            ProjectilePrefab!,
            StartTransform.position,
            Quaternion.identity
        );

        int index = (lastIndex + 1) % EndTransforms.Count;

        projectile.currentTarget = EndTransforms[index];

        lastIndex = index;

        yield return new WaitForSeconds(0.5f);

        StartCoroutine(Spawn());
    }
}
