using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;


struct ChunkLoader : IJob
{


    public NoiseParameters noiseParameters;
    public TerrainParameters terrainParameters;

    public NativeArray<Voxel> Points;
    public NativeArray<int> NumberOfTriangles;
    public NativeArray<Vector3> Vertices;
    public NativeArray<int> Triangles;
    public NativeArray<bool> UpdateMainThread;

    public ChunkLoader(NoiseParameters noiseP, TerrainParameters terrainP)
    {
        noiseParameters = noiseP;
        terrainParameters = terrainP;
        NumberOfTriangles = new NativeArray<int>(1, Allocator.Persistent);
        UpdateMainThread = new NativeArray<bool>(1, Allocator.Persistent);

        if (terrainParameters.SamplingLength == 0 || terrainParameters.SamplingWidth == 0 || terrainParameters.SamplingHeight == 0)
            throw new UnityException("Cannot have zero size volume");

        int size =(terrainParameters.SamplingLength + 1) * (terrainParameters.SamplingWidth+ 1) * (terrainParameters.SamplingHeight+ 1) * terrainParameters.Scale;

        int numPossiblePoints = (terrainParameters.SamplingLength * terrainParameters.Scale + 1) * (terrainParameters.SamplingWidth * terrainParameters.Scale + 1) * (terrainParameters.SamplingHeight * terrainParameters.Scale + 1) ;
        Points = new NativeArray<Voxel>(numPossiblePoints, Allocator.Persistent);
        Vertices = new NativeArray<Vector3>(size * 15, Allocator.Persistent);
        Triangles = new NativeArray<int>(size * 15, Allocator.Persistent);

    }

    public void ReInitialize(NoiseParameters noiseP, TerrainParameters terrainP) {

        // The array may change size, dump them then set the struct back up
        Points.Dispose();
        Vertices.Dispose();
        Triangles.Dispose();

        noiseParameters = noiseP;
        terrainParameters = terrainP;

        if (terrainParameters.SamplingLength == 0 || terrainParameters.SamplingWidth == 0 || terrainParameters.SamplingHeight == 0)
            throw new UnityException("Cannot have zero size volume");

        int size = 32 * 32 * 32 * terrainParameters.Scale;// (terrainParameters.SamplingLength + 1) * (terrainParameters.SamplingWidth+ 1) * (terrainParameters.SamplingHeight+ 1) * terrainParameters.Scale;

        
        Points = new NativeArray<Voxel>(size, Allocator.Persistent);
        Vertices = new NativeArray<Vector3>(size * 15, Allocator.Persistent);
        Triangles = new NativeArray<int>(size * 15, Allocator.Persistent);



    }


    public void Execute()
    {
        
        GenerateScalarField(terrainParameters.Origin);
        // have a set of volumetric data
        // turn all that data into cubes to march over
        List<VoxelCell> VoxelCells = CreateCubeData();
        CalulateMesh(VoxelCells);
        UpdateMainThread[0] = true;

    }
    [BurstCompile]
    private void CalulateMesh(List<VoxelCell> VoxelCells)
    {

        //for each voxel cell we get a mesh that consists of vertices and triangle.
        //now we need to put all the vertices and indices into a single array
        // since cooridinate are givine in world space we will need to shift all the but the first cell
        // into the correct world space location by adding the the column number to the given triangle
        // we also must correctly index the new triangles

        // We will have at most the amount of cells times 5 since we can have no more than 5 triangles per mesh
        List<Vector3[]> agVertices = new List<Vector3[]>(VoxelCells.Count);// TODO: Dont allocate as much space 
        List<int[]> agTriangles = new List<int[]>();   // has up to 5 triangles


        for (int i = 0; i < VoxelCells.Count; i++)
        {

            agVertices.Add(VoxelCells[i].mTriangleVertices); // has 3,6,9,12, or 15 vertices
            agTriangles.Add(VoxelCells[i].mTriangleIndex);   // has up to 5 triangles
            NumberOfTriangles[0] += VoxelCells[i].mNumberTriangles;

        }

        Assert.IsTrue(agVertices.Count == agTriangles.Count);

        List<Vector3> linVertices = new List<Vector3>();
        // now we must linearlize the vertices List<> into a array[]
        for (int i = 0; i < agVertices.Count; i++)
        {
            Vector3[] vertList = agVertices[i];
            for (int j = 0; j < vertList.Length; j++)
            {
                linVertices.Add(agVertices[i][j]);
            }
        }
        List<int> iinIndexedTriangles = new List<int>();
        int offset = 0;
        // now we must linearlize the index List<> into a array[]
        for (int i = 0; i < agTriangles.Count; i++)
        {
            int[] vertList = agTriangles[i];
            for (int j = 0; j < vertList.Length; j++)
            {
                iinIndexedTriangles.Add(agTriangles[i][j] + offset);
            }

            offset += vertList.Length;
        }

        Assert.IsTrue(linVertices.Count == iinIndexedTriangles.Count);

        for (int i = 0; i < linVertices.Count; i++)
        {
            Vertices[i] = linVertices[i];
            Triangles[i] = iinIndexedTriangles[i];

        }



    }
    [BurstCompile]
    private List<VoxelCell> CreateCubeData()
    {
        int levelOffset, rowOffset;
        List<VoxelCell> VoxelCells = new List<VoxelCell>();

        int Length = terrainParameters.SamplingLength;
        int Width = terrainParameters.SamplingWidth;
        int Height = terrainParameters.SamplingHeight;
        int Scale = terrainParameters.Scale;

        int levelSize = ((Width * Scale + 1) * (Length * Scale + 1));
        int rowSize = Width*Scale + 1;

        for (int level = 0; level < Height * Scale; level++)
        {

            levelOffset = level * levelSize;

            for (int row = 0; row < Length * Scale; row++)
            {
                rowOffset = row * (Width*Scale + 1);
                for (int coloumn = 0; coloumn < Width * Scale; coloumn++)
                {


                    VoxelCell cell = new VoxelCell(0);
                    cell.mVoxel[0] = Points[coloumn + rowOffset + levelOffset];  //(c, r) 
                    cell.mVoxel[1] = Points[coloumn + rowOffset + levelOffset + levelSize];  //(c, r) 
                    cell.mVoxel[2] = Points[coloumn + rowOffset + levelOffset + levelSize + 1];  //(c, r) 
                    cell.mVoxel[3] = Points[coloumn + rowOffset + levelOffset + 1];  //(c, r) 

                    cell.mVoxel[4] = Points[coloumn + rowOffset + levelOffset + rowSize];  //(c, r) 
                    cell.mVoxel[5] = Points[coloumn + rowOffset + levelOffset + rowSize + levelSize];  //(c, r) 
                    cell.mVoxel[6] = Points[coloumn + rowOffset + levelOffset + rowSize + levelSize + 1];  //(c, r) 
                    cell.mVoxel[7] = Points[coloumn + rowOffset + levelOffset + rowSize + 1];  //(c, r) 

                    // create the edge list
                    cell.CreateEdgeList();

                    // check if the cell in on the surface
                    if (cell.IsOnSurface())
                    {
                        cell.CalculateMesh();
                        VoxelCells.Add(cell);
                    }


                }

            }
        }

        return VoxelCells;
    }
    //[BurstCompile]
    private void GenerateScalarField(Vector3 around)
    {

      

        int levelOffset = 0, rowOffset = 0;
        float x, y, z;

        int Length = terrainParameters.SamplingLength;
        int Width = terrainParameters.SamplingWidth;
        int Height = terrainParameters.SamplingHeight;
        int Scale = terrainParameters.Scale;



        for (int level = 0; level <= Height * Scale; level++)
        {
            levelOffset = level * ((Width * Scale + 1) * (Length * Scale + 1));

            for (int row = 0; row <= Length * Scale; row++)
            {
                rowOffset = row * (Width* Scale + 1);
                for (int column = 0; column <= Width * Scale; column++)
                {

                    x = -((Width) / 2f) + (column / (float)Scale);
                    y = -((Height) / 2f) + (level / (float)Scale);
                    z = -((Length) / 2f) + (row / (float)Scale);

                    Vector3 vect = new Vector3(x, y, z);
                    // TODO: Generate better noise - create a flat plane

                    float noise = Noise.GenerateNoise(vect, terrainParameters.Seed, noiseParameters);
                    Voxel v = new Voxel(vect, noise);
                    Points[levelOffset + rowOffset + column] = v;

                }

            }
        }
    }
}
