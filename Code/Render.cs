using System;
using System.Security.Cryptography.X509Certificates;
using Sandbox;
using Sandbox.Engine.Settings;

public class RenderWaveSim : BasePostProcess<RenderWaveSim>
{
    [Property] public Material material;
    [Property] public Shader shader;
    private Texture targetTexture;
    private ComputeShader computeShader;

     int width = 1024;
     int height = 1024;
     int gridSize => width * height;

    double[] gridValues;

    GpuBuffer<double> grid;
    GpuBuffer<double> tempGrid;
    GpuBuffer<double> prevGrid;

    private int IndexOf(int x, int y, int width, int height){
        return (y * width + x);
    }

    private int IndexOf(int x, int y )
    {
        return IndexOf(x,y, width, height);
    }

    private void SetResolution(int width, int height )
    {
        targetTexture = Texture.CreateRenderTarget()
            .WithSize( width, height )
            .WithFormat( ImageFormat.RGBA16161616F )
            .WithUAVBinding()
            .Create();

        double[] oldGridValues = null;

        if(gridValues is not null )
        {
            oldGridValues = new double[gridValues.Length];
            grid.GetData(gridValues);
            Array.Copy(gridValues, oldGridValues, gridValues.Length);
        }

        gridValues = new double[width * height];

        if(oldGridValues is not null )
        {
            for(int x = 0; Math.Min(width, this.width) > x; x++){
                for(int y = 0; Math.Min(height, this.height) > y; y++){
                    double value = oldGridValues[IndexOf(x,y, this.width, this.height)];
                    gridValues[IndexOf(x,y, width, height)] = value;
                }
            }
        }
        
        this.width = width;
        this.height = height;

        grid = new GpuBuffer<double>( gridSize, GpuBuffer.UsageFlags.Structured );
        tempGrid = new GpuBuffer<double>( gridSize, GpuBuffer.UsageFlags.Structured );
        prevGrid = new GpuBuffer<double>( gridSize, GpuBuffer.UsageFlags.Structured );
        
        grid.SetData( gridValues );
        tempGrid.SetData( gridValues );
        prevGrid.SetData( gridValues);

        computeShader.Attributes.Set("Width", width);
        computeShader.Attributes.Set("Height", height);
        computeShader.Attributes.Set( "WaveGrid", grid );
        computeShader.Attributes.Set( "TempGrid", tempGrid );
        computeShader.Attributes.Set( "PrevGrid", prevGrid );
    }

    protected override void OnEnabled()
    {
        base.OnEnabled();
        material = Material.FromShader(shader);

        computeShader = new ComputeShader( "shaders\\test" );

        SetResolution((int)Screen.Width, (int)Screen.Height);

        var midPoint = (width / 2, height / 2);

        for ( int x = 0; x < width; x++ )
        {
            for ( int y = 0; y < height; y++ )
            {
                double dist = Math.Sqrt( Math.Pow( midPoint.Item1 - x, 2 ) + Math.Pow( midPoint.Item2 - y, 2 ) );
                gridValues[IndexOf(x,y)] = dist > 50 ? 0 : 1 - Math.Clamp((double)(dist - 25) / 25,0.0,1.0);
            }
        }

        grid.SetData( gridValues );
        tempGrid.SetData( gridValues );
        prevGrid.SetData( gridValues );

        computeShader.Attributes.Set( "WaveGrid", grid );
        computeShader.Attributes.Set( "TempGrid", tempGrid );
        computeShader.Attributes.Set( "PrevGrid", prevGrid );
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    public override void Render()
    {
        if((int)Screen.Height != height || (int)Screen.Width != width)
            SetResolution((int)Screen.Width, (int)Screen.Height);

        for(int i = 0; i < 50; i++ )
        {
            computeShader.Attributes.Set( "DeltaTime", 0.1);
            computeShader.Attributes.Set( "Init", false);
            computeShader.Attributes.Set( "Result", targetTexture );
            computeShader.Attributes.Set( "WaveGrid", grid );
            computeShader.Attributes.Set( "TempGrid", tempGrid );

            computeShader.Dispatch( width, height, 1 );

            (grid, tempGrid) = (tempGrid, grid);
        }

        if ( Graphics.IsActive )
        {
            Graphics.UavBarrier( targetTexture );
        }

        Attributes.Set("waveTex", targetTexture);
        Blit( BlitMode.Simple( material, Sandbox.Rendering.Stage.AfterPostProcess, 200), "blit" );
    }
}