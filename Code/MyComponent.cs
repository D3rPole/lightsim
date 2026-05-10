using System;
using System.ComponentModel.Design;
using System.Runtime.CompilerServices;
using Sandbox;
using Sandbox.Rendering;

namespace LightSim;

public sealed class MyComponent : Component
{
    [Property] public Material material;
    private Texture targetTexture;
    private ComputeShader computeShader;

    const int width = 512;
    const int height = 512;
    const int gridSize = width * height * 2;
    float[] gridValues = new float[gridSize];
    float[] tempGridValues = new float[gridSize];

    GpuBuffer<float> grid;
    GpuBuffer<float> tempGrid;

    protected override void OnStart()
    {
        base.OnStart();

        // 1. Create the render target texture
        targetTexture = Texture.CreateRenderTarget()
            .WithSize( width, height )
            .WithFormat( ImageFormat.RGBA16161616F )
            .WithUAVBinding() // Required for ComputeShader to write to the texture
            .Create();

        computeShader = new ComputeShader( "shaders\\test" );

        grid = new GpuBuffer<float>(gridSize, GpuBuffer.UsageFlags.Structured );
        tempGrid = new GpuBuffer<float>(gridSize, GpuBuffer.UsageFlags.Structured );

        var midPoint = (width / 2, height / 2);

        for(int i = 0; i < width; i++)
        {
            for(int j = 0; j < height; j++)
            {
                double dist = Math.Sqrt(Math.Pow(midPoint.Item1 - i,2) + Math.Pow(midPoint.Item2 - j,2));
                gridValues[(i * width + j) * 2] = (float)Math.Max(0,10 / (dist + 1));
                gridValues[(i * width + j) * 2 + 1] = 0;
            }
        }
        grid.SetData(gridValues);
        tempGrid.SetData(tempGridValues);
        
        computeShader.Attributes.Set( "WaveGrid", grid );
        computeShader.Attributes.Set( "TempGrid", tempGrid );
    }

    protected override void OnUpdate()
    {
        if ( targetTexture == null || computeShader == null )
            return;

        computeShader.Attributes.Set("DeltaTime", Time.Delta);
        computeShader.Attributes.Set( "Result", targetTexture );
        computeShader.Attributes.Set( "WaveGrid", grid );
        computeShader.Attributes.Set( "TempGrid", tempGrid );

        computeShader.Dispatch( width, height, 1 );

        (grid, tempGrid) = (tempGrid, grid);

        if ( Graphics.IsActive )
        {
            Graphics.UavBarrier( targetTexture );
        }

        material.Set( "g_tColor", targetTexture );
    }

    protected override void OnDestroy()
    {
        targetTexture?.Dispose();

        base.OnDestroy();
    }
}