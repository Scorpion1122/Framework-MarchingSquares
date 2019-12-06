using System;
using System.Collections;
using System.Collections.Generic;
using Thijs.Framework.MarchingSquares;
using UnityEngine;
using UnityEngine.Rendering;

public class TileTerrainShadows : MonoBehaviour
{
    private static readonly int tileTerrainBlitTarget = Shader.PropertyToID("_TileTerrainBlitTarget");
    private static readonly int blurBlitTarget = Shader.PropertyToID("_BlurBlitTarget");
    private List<ChunkRenderer> renderers;

    private Material shadowMaskMaterial;
    private Material blurMaterial;

    private void OnEnable()
    {
        Camera.onPreRender += OnPreRender;
    }

    private void OnDisable()
    {
        Camera.onPreRender -= OnPreRender;
    }

    private void OnPreRender(Camera camera)
    {
        AddDepthShadowBuffer(camera);
    }

    private void AddDepthShadowBuffer(Camera camera)
    {
        CommandBuffer buffer = new CommandBuffer();

        buffer.GetTemporaryRT(tileTerrainBlitTarget, -1, -1, 0);

        // Render terrain to temp RT target
        buffer.SetRenderTarget(tileTerrainBlitTarget);
        for (int i = 0; i < renderers.Count; i++)
            buffer.DrawRenderer(renderers[i].MeshRenderer, shadowMaskMaterial);

        buffer.Blit(tileTerrainBlitTarget, blurBlitTarget, blurMaterial);
        buffer.Blit(blurBlitTarget, BuiltinRenderTextureType.CameraTarget);

        buffer.ReleaseTemporaryRT(tileTerrainBlitTarget);
        buffer.ReleaseTemporaryRT(blurBlitTarget);

        camera.AddCommandBuffer(CameraEvent.AfterLighting, buffer);
    }

    public void RegisterTileTerrainRenderer()
    {

    }
}
