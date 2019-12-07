using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Thijs.Framework.MarchingSquares
{
    [ExecuteInEditMode]
    public class TileTerrainRenderer : TileTerrainComponent
    {
        private static readonly int tileTerrainBlitTarget = Shader.PropertyToID("_TileTerrainBlitTarget");
        private static readonly int screenCopy = Shader.PropertyToID("_ScreenCopy");
        private static readonly int blurBlitTargetOne = Shader.PropertyToID("_BlurBlitTargetOne");
        private static readonly int blurBlitTargetTwo = Shader.PropertyToID("_BlurBlitTargetTwo");
        private static readonly CameraEvent SHADOW_CAMERA_EVENT = CameraEvent.AfterEverything;
        
        private List<ChunkRenderer> renderers = new List<ChunkRenderer>();
        private List<Camera> cameras = new List<Camera>();
        
        [SerializeField] private Material shadowMaskMaterial;
        [SerializeField] private Material blitMaterial;
        [SerializeField] private Material blurMaterial;

        private CommandBuffer shadowCommandBuffer;
        private bool renderesChanged;
        
        private void OnEnable()
        {
            TileTerrain.OnChunkInitialized += OnChunkInitialized;
        }

        private void OnDisable()
        {
            TileTerrain.OnChunkInitialized -= OnChunkInitialized;
            ClearCommandBuffers();
            
        }

        private void OnChunkInitialized(int2 chunkIndex, ChunkData chunkData)
        {
            GameObject gameObject = new GameObject("Chunk Renderer");
            gameObject.hideFlags = HideFlags.DontSave;
            gameObject.transform.SetParent(transform);
            gameObject.transform.position = transform.TransformPoint(chunkData.origin.x, chunkData.origin.y, 0f);

            ChunkRenderer chunkRenderer = gameObject.AddComponent<ChunkRenderer>();
            chunkData.dependencies.Add(chunkRenderer);
            renderers.Add(chunkRenderer);

            renderesChanged = true;
        }

        private void LateUpdate()
        {
            if (renderesChanged)
            {
                UpdateCameraBuffers();
                renderesChanged = false;
            }
        }

        private void UpdateCameraBuffers()
        {
            ClearCommandBuffers();
            
            shadowCommandBuffer = CreateShadowCommandBuffer();
            CameraUtility.GetActiveCameras(ref cameras);
            for (int i = 0; i < cameras.Count; i++)
            {
                cameras[i].AddCommandBuffer(SHADOW_CAMERA_EVENT, shadowCommandBuffer);
            }
        }

        private void ClearCommandBuffers()
        {
            if (shadowCommandBuffer == null) 
                return;
            
            for (int i = 0; i < cameras.Count; i++)
            {
                if (cameras[i] != null)
                    cameras[i].RemoveCommandBuffer(SHADOW_CAMERA_EVENT, shadowCommandBuffer);
            }
            cameras.Clear();
        }

        private CommandBuffer CreateShadowCommandBuffer()
        {
            CommandBuffer buffer = new CommandBuffer();
            buffer.name = "Tile Terrain Shadows";
            
            buffer.GetTemporaryRT(tileTerrainBlitTarget, -1, -1, 0);
            buffer.GetTemporaryRT(screenCopy, -1, -1, 0);
            buffer.GetTemporaryRT(blurBlitTargetOne, -1, -1, 0);
            buffer.GetTemporaryRT(blurBlitTargetTwo, -1, -1, 0);
            
            buffer.Blit(BuiltinRenderTextureType.CameraTarget, screenCopy);
            buffer.SetGlobalTexture("_ScreenCopy", screenCopy);

            // Render terrain to temp RT target
            buffer.SetRenderTarget(tileTerrainBlitTarget);
            buffer.ClearRenderTarget(true, true, Color.clear);
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                    buffer.DrawRenderer(renderers[i].MeshRenderer, shadowMaskMaterial);
            }

            buffer.Blit(tileTerrainBlitTarget, blurBlitTargetOne, blurMaterial, 0);
            buffer.Blit(blurBlitTargetOne, blurBlitTargetTwo, blurMaterial, 1);
            
            buffer.Blit(blurBlitTargetTwo, BuiltinRenderTextureType.CameraTarget, blitMaterial);
            //buffer.Blit(tileTerrainBlitTarget, BuiltinRenderTextureType.CameraTarget, blitMaterial);

            buffer.ReleaseTemporaryRT(tileTerrainBlitTarget);
            buffer.ReleaseTemporaryRT(screenCopy);
            buffer.ReleaseTemporaryRT(blurBlitTargetOne);
            buffer.ReleaseTemporaryRT(blurBlitTargetTwo);
            
            buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            //buffer.ClearRenderTarget(true, true, Color.red);

            return buffer;
        }
    }
}