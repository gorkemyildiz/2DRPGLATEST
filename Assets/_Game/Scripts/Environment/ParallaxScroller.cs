using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Environment
{
    [Serializable]
    public class ParallaxLayer
    {
        public Transform root;
        public float scrollSpeed = 1f;
        public bool enabled = true;
        public bool loop = true;
    }

    public class ParallaxScroller : MonoBehaviour
    {
        [Header("Layers")]
        [SerializeField] private ParallaxLayer farLayer = new ParallaxLayer { scrollSpeed = 0.25f, loop = true };
        [SerializeField] private ParallaxLayer midLayer = new ParallaxLayer { scrollSpeed = 0.6f, loop = true };
        [SerializeField] private ParallaxLayer foregroundLayer = new ParallaxLayer { scrollSpeed = 1.2f, loop = true };

        [Header("Settings")]
        [SerializeField] private float speedMultiplier = 1f;
        [SerializeField] private int tilesPerLayer = 2;

        private bool isScrolling = false;
        private readonly List<LayerRuntime> layerRuntimes = new List<LayerRuntime>();

        private class LayerRuntime
        {
            public ParallaxLayer layer;
            public Vector3 startRootPos;
            public float tileWidth;
            public readonly List<Transform> tiles = new List<Transform>();
        }

        private void Awake()
        {
            SetupLayer(farLayer);
            SetupLayer(midLayer);
            SetupLayer(foregroundLayer);
        }

        private void Update()
        {
            if (!isScrolling)
            {
                return;
            }

            float dt = Time.deltaTime;
            for (int i = 0; i < layerRuntimes.Count; i++)
            {
                ScrollLayer(layerRuntimes[i], dt);
            }
        }

        public void StartScrolling()
        {
            isScrolling = true;
        }

        public void StopScrolling()
        {
            isScrolling = false;
        }

        public void ResetPosition()
        {
            for (int i = 0; i < layerRuntimes.Count; i++)
            {
                LayerRuntime runtime = layerRuntimes[i];
                if (runtime.layer.root != null)
                {
                    runtime.layer.root.position = runtime.startRootPos;
                }

                for (int t = 0; t < runtime.tiles.Count; t++)
                {
                    Transform tile = runtime.tiles[t];
                    if (tile == null)
                    {
                        continue;
                    }

                    Vector3 local = tile.localPosition;
                    local.x = t * runtime.tileWidth;
                    tile.localPosition = local;
                }
            }
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = multiplier;
        }

        private void SetupLayer(ParallaxLayer layer)
        {
            if (layer == null || layer.root == null)
            {
                return;
            }

            LayerRuntime runtime = new LayerRuntime
            {
                layer = layer,
                startRootPos = layer.root.position
            };

            // Prefer explicit Tile_* children; ignore legacy/inactive Background objects
            List<SpriteRenderer> sourceTiles = CollectActiveTiles(layer.root);

            if (sourceTiles.Count == 0)
            {
                Debug.LogWarning($"[ParallaxScroller] No active sprites under '{layer.root.name}'.");
                layerRuntimes.Add(runtime);
                return;
            }

            SpriteRenderer source = sourceTiles[0];
            runtime.tileWidth = GetTileWidth(source);

            if (runtime.tileWidth <= 0.01f)
            {
                Debug.LogWarning($"[ParallaxScroller] Invalid tile width on '{layer.root.name}'. Loop disabled.");
                layer.loop = false;
                runtime.tiles.Add(source.transform);
                layerRuntimes.Add(runtime);
                return;
            }

            runtime.tiles.Add(source.transform);

            if (layer.loop)
            {
                int needed = Mathf.Max(2, tilesPerLayer);

                // Reuse already present active tiles first
                for (int i = 1; i < sourceTiles.Count && runtime.tiles.Count < needed; i++)
                {
                    runtime.tiles.Add(sourceTiles[i].transform);
                }

                // Clone extras if still short
                while (runtime.tiles.Count < needed)
                {
                    GameObject clone = Instantiate(source.gameObject, layer.root);
                    clone.name = $"{source.gameObject.name}_Loop{runtime.tiles.Count}";
                    clone.SetActive(true);
                    runtime.tiles.Add(clone.transform);
                }

                // Place visible tiles side by side in local space
                for (int i = 0; i < runtime.tiles.Count; i++)
                {
                    Transform tile = runtime.tiles[i];
                    Vector3 local = source.transform.localPosition;
                    local.x = i * runtime.tileWidth;
                    tile.localPosition = local;
                    tile.localScale = source.transform.localScale;
                }
            }

            layerRuntimes.Add(runtime);
            Debug.Log($"[ParallaxScroller] Layer '{layer.root.name}' ready. tiles={runtime.tiles.Count}, width={runtime.tileWidth:F2}, loop={layer.loop}");
        }

        private static List<SpriteRenderer> CollectActiveTiles(Transform root)
        {
            List<SpriteRenderer> result = new List<SpriteRenderer>();

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (!child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                // Skip legacy single Background if Tile_* already exist
                if (child.name == "Background")
                {
                    continue;
                }

                SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    result.Add(sr);
                }
            }

            // Fallback: allow a single active Background if no Tile_* children
            if (result.Count == 0)
            {
                SpriteRenderer[] all = root.GetComponentsInChildren<SpriteRenderer>(false);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].sprite != null && all[i].gameObject.activeInHierarchy)
                    {
                        result.Add(all[i]);
                    }
                }
            }

            return result;
        }

        private void ScrollLayer(LayerRuntime runtime, float deltaTime)
        {
            ParallaxLayer layer = runtime.layer;
            if (layer.root == null || !layer.enabled)
            {
                return;
            }

            float scrollDistance = layer.scrollSpeed * speedMultiplier * deltaTime;
            if (scrollDistance <= 0f)
            {
                return;
            }

            if (!layer.loop || runtime.tiles.Count == 0 || runtime.tileWidth <= 0.01f)
            {
                layer.root.position += Vector3.left * scrollDistance;
                return;
            }

            Camera cam = Camera.main;
            float leftBound = cam != null
                ? cam.transform.position.x - (cam.orthographicSize * cam.aspect) - 0.5f
                : -20f;

            float wrapDistance = runtime.tileWidth * runtime.tiles.Count;
            float halfTile = runtime.tileWidth * 0.5f;

            for (int i = 0; i < runtime.tiles.Count; i++)
            {
                Transform tile = runtime.tiles[i];
                if (tile == null)
                {
                    continue;
                }

                tile.position += Vector3.left * scrollDistance;

                // If the tile's right edge is fully left of the camera, move it to the far right.
                while (tile.position.x + halfTile < leftBound)
                {
                    tile.position += Vector3.right * wrapDistance;
                }
            }
        }

        private static float GetTileWidth(SpriteRenderer renderer)
        {
            if (renderer == null || renderer.sprite == null)
            {
                return 0f;
            }

            // Use sprite local size * lossy scale X so placement/wrapping stay consistent
            return renderer.sprite.bounds.size.x * Mathf.Abs(renderer.transform.lossyScale.x);
        }
    }
}
