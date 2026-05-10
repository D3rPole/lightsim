MODES
{
    Default();
}

CS
{
    #include "system.fxc"

    
    int WIDTH < Attribute("Width"); >;
    int HEIGHT < Attribute("Height"); >;

    RWTexture2D<float4> Result < Attribute("Result"); >;
    RWStructuredBuffer<double> grid < Attribute("WaveGrid"); >;
    RWStructuredBuffer<double> tempGrid < Attribute("TempGrid"); >;
    RWStructuredBuffer<double> prevGrid < Attribute("PrevGrid"); >;

    float deltaTime < Attribute("DeltaTime"); >;

    int IndexOf(float2 coords){
        return (coords.y * WIDTH + coords.x);
    }

    [numthreads(32, 32, 1)]
    void MainCs(uint3 id : SV_DispatchThreadID)
    {
        int index = IndexOf(id.xy);

        float position = grid[index];

        Result[id.xy] = float4(position < 0 ? 0 : position, position < 0 ? -position : 0, 0, 1);

        float forceApplied = 0;

        if(id.y + 1 < HEIGHT)   forceApplied += grid[IndexOf(uint2(id.x, id.y + 1))] - position;
        if(id.y > 0)            forceApplied += grid[IndexOf(uint2(id.x, id.y - 1))] - position;
        if(id.x + 1 < WIDTH)    forceApplied += grid[IndexOf(uint2(id.x + 1, id.y))] - position;
        if(id.x > 0)            forceApplied += grid[IndexOf(uint2(id.x - 1, id.y))] - position;

        forceApplied = forceApplied / 10;
        
        double damping = 0.9999;

        tempGrid[index] = grid[index] + (grid[index] - prevGrid[index] + forceApplied * deltaTime * deltaTime) * damping;
        prevGrid[index] = position;
    } 
}