#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace ConductorSymphony.EditorTools
{
    public static class PrefabGenerator
    {
        [MenuItem("Tools/Generate Team Prefabs")]
        public static void GenerateAllPrefabs()
        {
            string baseDir = "Assets/Prefabs/";
            Directory.CreateDirectory(baseDir + "Player");
            Directory.CreateDirectory(baseDir + "Instruments");
            Directory.CreateDirectory(baseDir + "Enemies");
            Directory.CreateDirectory(baseDir + "Combat");
            Directory.CreateDirectory(baseDir + "Items");
            Directory.CreateDirectory(baseDir + "UI");

            // 1. Player Prefab
            GameObject playerObj = new GameObject("Player");
            playerObj.AddComponent<SpriteRenderer>();
            CircleCollider2D pCol = playerObj.AddComponent<CircleCollider2D>();
            pCol.radius = 0.65f;
            pCol.isTrigger = true;
            Rigidbody2D pRb = playerObj.AddComponent<Rigidbody2D>();
            pRb.bodyType = RigidbodyType2D.Kinematic;
            playerObj.AddComponent<ConductorSymphony.Player.PlayerController>();
            playerObj.AddComponent<ConductorSymphony.Player.PlayerExperience>();
            PrefabUtility.SaveAsPrefabAsset(playerObj, baseDir + "Player/Player.prefab");
            Object.DestroyImmediate(playerObj);

            // 2. Enemy Prefabs
            GameObject enemyObj = new GameObject("EnemyMonster");
            enemyObj.AddComponent<SpriteRenderer>();
            CircleCollider2D eCol = enemyObj.AddComponent<CircleCollider2D>();
            eCol.radius = 0.3f;
            eCol.isTrigger = true;
            Rigidbody2D eRb = enemyObj.AddComponent<Rigidbody2D>();
            eRb.bodyType = RigidbodyType2D.Kinematic;
            enemyObj.AddComponent<ConductorSymphony.Enemy.EnemyMonster>();
            PrefabUtility.SaveAsPrefabAsset(enemyObj, baseDir + "Enemies/EnemyMonster.prefab");
            Object.DestroyImmediate(enemyObj);

            GameObject bossObj = new GameObject("BossMonster");
            bossObj.AddComponent<SpriteRenderer>();
            CircleCollider2D bCol = bossObj.AddComponent<CircleCollider2D>();
            bCol.radius = 0.8f;
            bCol.isTrigger = true;
            Rigidbody2D bRb = bossObj.AddComponent<Rigidbody2D>();
            bRb.bodyType = RigidbodyType2D.Kinematic;
            bossObj.AddComponent<ConductorSymphony.Enemy.BossMonster>();
            PrefabUtility.SaveAsPrefabAsset(bossObj, baseDir + "Enemies/BossMonster.prefab");
            Object.DestroyImmediate(bossObj);

            // 3. Instrument Orbit Prefab
            GameObject orbitObj = new GameObject("InstrumentOrbit");
            orbitObj.AddComponent<SpriteRenderer>();
            orbitObj.AddComponent<ConductorSymphony.Instrument.InstrumentOrbit>();
            PrefabUtility.SaveAsPrefabAsset(orbitObj, baseDir + "Instruments/InstrumentOrbit.prefab");
            Object.DestroyImmediate(orbitObj);

            // 4. Chest Prefab
            GameObject chestObj = new GameObject("EliteRewardChest");
            chestObj.AddComponent<SpriteRenderer>();
            CircleCollider2D cCol = chestObj.AddComponent<CircleCollider2D>();
            cCol.radius = 0.75f;
            cCol.isTrigger = true;
            chestObj.AddComponent<ConductorSymphony.Item.EliteRewardChest>();
            PrefabUtility.SaveAsPrefabAsset(chestObj, baseDir + "Items/EliteRewardChest.prefab");
            Object.DestroyImmediate(chestObj);

            // 5. ExpGem Prefab
            GameObject gemObj = new GameObject("ExpGem");
            gemObj.AddComponent<SpriteRenderer>();
            CircleCollider2D gCol = gemObj.AddComponent<CircleCollider2D>();
            gCol.radius = 0.3f;
            gCol.isTrigger = true;
            gemObj.AddComponent<ConductorSymphony.Player.ExpGem>();
            PrefabUtility.SaveAsPrefabAsset(gemObj, baseDir + "Items/ExpGem.prefab");
            Object.DestroyImmediate(gemObj);

            AssetDatabase.Refresh();
            Debug.Log("All Team Prefabs generated successfully in Assets/Prefabs/");
        }
    }
}
#endif
