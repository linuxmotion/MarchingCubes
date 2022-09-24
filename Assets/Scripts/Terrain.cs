using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

class Terrain : MonoBehaviour
{


    // A list of all point that are in the terrain mesh that will be created.
    public List<List<Voxel>> VoxelLayers { get; private set; }

    // Terrain detail - Higher is more detail

    [Header("Terrain Creation parameters")]
    [SerializeField]
    private int _Seed;
    [SerializeField]
    private int _MaxWorldSize;
    [SerializeField]
    private Transform _InitialPlayerLocation;

    [Header("Volume Sampling Parameters")]
    [SerializeField]
    private int _Scale;
    [SerializeField]
    private int _SamplingHeight;
    [SerializeField]
    private int _SamplingLength;
    [SerializeField]
    private int _SamplingWidth;


    [SerializeField]
    private int _ISO_Level;



    private Vector3 mOrigin;

    private List<VoxelCell> mVoxelCells;

    private Noise mNoise;
    private Mesh mTerrainMesh;



    public void Start()
    {

    }

    public void Initialize()
    {

        mNoise = GetComponent<Noise>();
        mVoxelCells = new List<VoxelCell>();
        mOrigin = _InitialPlayerLocation.position;
        mOrigin.y = 0;
        mTerrainMesh = new Mesh();
        mTerrainMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;


    }
    public void GenerateInitialTerrain()
    {

        VoxelLayers = new List<List<Voxel>>();
        GenerateTerrain(mOrigin);

    }
    public void GenerateTerrain(Vector3 generateAround)
    {
        // Generate all the points in the world
        GeneratePoints(generateAround, _Seed);
    }


    private void GeneratePoints(Vector3 generateAround, in int seed)
    {
        // Generate +1 to height since a since it n+1 row of point to make the cube later
        generateAround.y = -5;
        for (int i = 0; i <= _SamplingHeight * _Scale; i++)
        {
            VoxelLayers.Add(GenerateSingleElevation(generateAround, seed));
            generateAround.y += 1 / (float)_Scale;
        }

    }

    private List<Voxel> GenerateSingleElevation(Vector3 around, in int seed)
    {

        List<Voxel> points = new List<Voxel>();

        // Loop through each row of points to generate
        for (int row = 0; row <= _SamplingLength * _Scale; row++)
        {
            // Loop through each coloumn within a row, generating points for the world
            for (int column = 0; column <= _SamplingWidth * _Scale; column++)
            {
                Voxel vox = new Voxel();
                float xdisplacement = -((_SamplingWidth) / 2f) + (column / (float)_Scale);
                float zdisplacement = -((_SamplingLength) / 2f) + (row / (float)_Scale);

                vox.Point = new Vector3(around.x + xdisplacement,
                                        around.y,
                                        around.z + zdisplacement
                                        );

                vox.Density = mNoise.GenerateNoise(vox.Point, seed);
                // Add the point to the list
                points.Add(vox);

            }
        }

        return points;

    }

    public Mesh GetTerrainMesh()
    {

        //for each voxel cell we get a mesh that consists of vertices and triangle.
        //now we need to put all the vertices and indices into a single array
        // since cooridinate are givine in world space we will need to shift all the but the first cell
        // into the correct world space location by adding the the column number to the given triangle
        // we also must correctly index the new triangles

        // We will have at most the amount of cells times 5 since we can have no more than 5 triangles per mesh
        List<Vector3[]> agVertices = new List<Vector3[]>(mVoxelCells.Count);// TODO: Dont allocate as much space 
        List<int[]> agTriangles = new List<int[]>();   // has up to 5 triangles


        foreach (VoxelCell vc in mVoxelCells)
        {

            Mesh m = vc.CalculateMesh(); // retieve the cubesa mesh      
            agVertices.Add(m.vertices); // has 3,6,9,12, or 15 vertices
            agTriangles.Add(m.triangles);   // has up to 5 triangles

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



        mTerrainMesh.vertices = linVertices.ToArray();
        mTerrainMesh.triangles = iinIndexedTriangles.ToArray();
        mTerrainMesh.RecalculateNormals();
        mTerrainMesh.RecalculateTangents();


        return mTerrainMesh;
    }

    public void GenerateVoxelSurfaceList()
    {
        // We have a list of point densities arranged in layers
        // so from here we need to find the isoSurface
        //List<Vector3> surfacePoints = FindISOSurface();

        // To determine where the surface lies, we gather 8 voxels that form the nearest 
        // cube arranged on a grid with the player at the center 
        List<Voxel> lowerLayer;
        List<Voxel> upperLayer;
        // Loop through each layer 
        int numLayers = VoxelLayers.Count;
        // for every two layer
        for (int i = 0; i < _SamplingHeight * _Scale; i++)
        {
            // we need two layers to test if whether the voxel is inside or outside of the surface 

            // but first check to see if we have reached the top of world that was sampled


            lowerLayer = VoxelLayers[i];
            upperLayer = VoxelLayers[i + 1];

            // check that our data has at least the correct amount of voxels in each list
            if (lowerLayer.Count != upperLayer.Count)
                throw new UnityException("List count mismatch: Cannot process voxel list - Terrain generation failed");


            // for each layer i need the four points of the cell j, j+1, i + Wr, j+1+wr
            // where j is the cell coulumn number and w is the number of cells in a row
            // the formula is derives as f(c,r) = <(c,r),(c+1,r),(c+1,r+1),(c,r+1)>

            int scaledSampleWidth = (_SamplingWidth * _Scale);

            for (int row = 0; row < _SamplingLength * _Scale; row++)
            {


                for (int column = 0; column < scaledSampleWidth; column++)
                {


                    int columnRowOffset = column + (scaledSampleWidth + 1) * row;
                    VoxelCell cell = new VoxelCell(_ISO_Level);
                    cell.mVoxel[0] = lowerLayer[columnRowOffset];  //(c, r)
                    cell.mVoxel[1] = upperLayer[columnRowOffset];  //(c, r)

                    cell.mVoxel[2] = upperLayer[columnRowOffset + 1]; // (c, r+1)
                    cell.mVoxel[3] = lowerLayer[columnRowOffset + 1]; // (c, r+1)

                    cell.mVoxel[4] = lowerLayer[columnRowOffset + scaledSampleWidth + 1];  //(c, r+1)
                    cell.mVoxel[5] = upperLayer[columnRowOffset + scaledSampleWidth + 1];  // (c+1, r+1)


                    cell.mVoxel[6] = upperLayer[columnRowOffset + scaledSampleWidth + 2];  //(c, r+1)
                    cell.mVoxel[7] = lowerLayer[columnRowOffset + scaledSampleWidth + 2];  // (c+1, r+1)       

                    cell.CreateEdgeList();
                    if (cell.IsOnSurface())
                        mVoxelCells.Add(cell);

                }

            }

        }

    }

    public List<VoxelCell> GetUnderlyingVoxelCells()
    {

        return mVoxelCells;
    }


}
