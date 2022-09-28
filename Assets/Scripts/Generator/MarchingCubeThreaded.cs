using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;

public class MarchingCubeThreaded : MonoBehaviour
{
    NativeArray<Voxel> Voxels;
    struct ChunkLoader : IJob
    {

        public int Length;
        public int Width;
        public int Height;
        public int Scale;
        public Vector3 Origin;
        public NativeArray<Voxel> Voxels;
        public NativeArray<int> NumberOfTriangles;
        public NativeArray<Vector3> Vertices;
        public NativeArray<int> Triangles;
        public NativeArray<bool> UpdateMainThread;

        public ChunkLoader(int length, int width, int height, int scale, Vector3 origin)
        {
            Origin = origin;
            NumberOfTriangles = new NativeArray<int>(1, Allocator.Persistent);
            UpdateMainThread = new NativeArray<bool>(1, Allocator.Persistent);

            if (length == 0 || width == 0 || height == 0)
                throw new UnityException("Cannot have zero size volume");
            Length = length;
            Width = width;
            Height = height;
            Scale = scale;

            int size = (length + 1) * (Width + 1) * (Height + 1) * Scale;
            Voxels = new NativeArray<Voxel>(size, Allocator.Persistent);
            Vertices = new NativeArray<Vector3>(size*15, Allocator.Persistent);
            Triangles = new NativeArray<int>(size*15, Allocator.Persistent);

        }



        public void Execute()
        {
            UpdateMainThread[0] = true;
            GenerateScalarField(Origin);
            // have a set of volumetric data
            // turn all that data into cubes to march over
            List<VoxelCell> VoxelCells = CreateCubeData();
            CalulateMesh(VoxelCells);

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

            int levelSize = (Width + 1) * (Length + 1);
            int rowSize = (Width + 1);
            for (int level = 0; level < Height; level++)
            {
                levelOffset = (level * levelSize);

                for (int row = 0; row < Length; row++)
                {
                    rowOffset = row * rowSize;
                    for (int coloumn = 0; coloumn < Width; coloumn++)
                    {



                        VoxelCell cell = new VoxelCell(0);
                        cell.mVoxel[0] = Voxels[coloumn + rowOffset + levelOffset];  //(c, r) 
                        cell.mVoxel[1] = Voxels[coloumn + rowOffset + levelOffset + levelSize];  //(c, r) 
                        cell.mVoxel[2] = Voxels[coloumn + rowOffset + levelOffset + levelSize + 1];  //(c, r) 
                        cell.mVoxel[3] = Voxels[coloumn + rowOffset + levelOffset + 1];  //(c, r) 

                        cell.mVoxel[4] = Voxels[coloumn + rowOffset + levelOffset + rowSize];  //(c, r) 
                        cell.mVoxel[5] = Voxels[coloumn + rowOffset + levelOffset + rowSize + levelSize];  //(c, r) 
                        cell.mVoxel[6] = Voxels[coloumn + rowOffset + levelOffset + rowSize + levelSize + 1];  //(c, r) 
                        cell.mVoxel[7] = Voxels[coloumn + rowOffset + levelOffset + rowSize + 1];  //(c, r) 

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
        [BurstCompile]
        private void GenerateScalarField(Vector3 around)
        {
            int levelOffset = 0, rowOffset = 0;
            float x , y , z ;

    

            for (int level = 0; level <= Height * Scale; level++)
            {
                levelOffset = (level * ((Width + 1) * (Length + 1)) ) * Scale;

                for (int row = 0; row <= Length * Scale; row++)
                {
                    rowOffset = row * (Width + 1) * Scale;
                    for (int column = 0; column <= Width * Scale; column++)
                    {

                        x = -((Width) / 2f) + (column / (float)Scale);
                        y = -((Height) / 2f) + (level / (float)Scale);
                        z = -((Length) / 2f) + (row / (float)Scale);

                        Vector3 vect = new Vector3(x,y,z);
                        // TODO: Generate better noise - create a flat plane

                        float noise = -y;// Noise.GenerateNoise(vect, 100, new NoiseParameters(1,1,1,1,1));
                        Voxel v = new Voxel(vect, noise);
                        Voxels[levelOffset + rowOffset + column] = v;

                    }

                }
            }
        }
    }


    MeshFilter filter;
    ChunkLoader loader;

    public NativeArray<Vector3> Vertices;
    public NativeArray<int> Triangles;
    public NativeArray<bool> UpdateMainThread;
    public NativeArray<int> NumberOfTriangles;

    // Start is called before the first frame update
    void Start()
    {

        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        meshRenderer.material.SetFloat("_Cull", 0);
        filter = gameObject.AddComponent<MeshFilter>();
        filter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;


        //TODO: Make feilds as seireilized properties
        loader = new ChunkLoader(1, 1, 1, 1, Vector3.zero);

        Vertices = loader.Vertices;
        Triangles = loader.Triangles;
        UpdateMainThread = loader.UpdateMainThread;
        NumberOfTriangles = loader.NumberOfTriangles;

        UpdateChunk();
    }

    public void UpdateChunk() {

        JobHandle jhandle = loader.Schedule();
        jhandle.Complete();

    }

    // Update is called once per frame
    void Update()
    {
        if (UpdateMainThread[0])
        {
            int index = (NumberOfTriangles[0] * 3);

            Vector3[] ver = Vertices.GetSubArray(0, index).ToArray();
            int[] ind = Triangles.GetSubArray(0, index).ToArray();

            filter.mesh.vertices = ver;
            filter.mesh.triangles = ind;
            UpdateMainThread[0] = false;

        }

    }
    private void OnDestroy()
    {
        Voxels.Dispose();
    }
}
