using System;
using UnityEngine;

public class VoxelCellUnitTest
{



    public static Mesh GetTestMesh(int caseNum)
    {

        Mesh TestMesh = new Mesh();


        TestMesh = caseNum switch
        {

            0 => Vertex0(),
            1 => Vertex01(),
            2 => Vertex012(),
            3 => Vertex0123(),
            4 => Vertex0473(),
            5 => Vertex01234(),
            _ => SingleTriangle()
        };


        return TestMesh;
    }

    private static Mesh Vertex0473()
    {

        VoxelCell cell = GetDefaultVoxelCell();
        cell.mVoxel[0].Density = 1;
        cell.mVoxel[4].Density = 1;
        cell.mVoxel[7].Density = 1;
        cell.mVoxel[3].Density = 1;
        cell.CreateEdgeList();

        return cell.CalculateMesh();

    }
    private static Mesh Vertex0123()
    {

        VoxelCell cell = GetDefaultVoxelCell();
        cell.mVoxel[0].Density = 1;
        cell.mVoxel[1].Density = 1;
        cell.mVoxel[2].Density = 1;
        cell.mVoxel[3].Density = 1;
        cell.CreateEdgeList();

        return cell.CalculateMesh();

    }
    
    private static Mesh Vertex01234()
    {

        VoxelCell cell = GetDefaultVoxelCell();
        cell.mVoxel[0].Density = 1;
        cell.mVoxel[1].Density = 1;
        cell.mVoxel[2].Density = 1;
        cell.mVoxel[3].Density = 1;
        cell.mVoxel[4].Density = 1;
        cell.CreateEdgeList();

        return cell.CalculateMesh();

    }

    private static Mesh SingleTriangle()
    {
        Mesh m = new Mesh();
        m.Clear();
        Vector3[] vertices = { new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 0, 0) };
        int[] index = { 0, 1, 2 };

        m.vertices = vertices;
        m.triangles = index;
        return m;
    }

    private static Mesh Vertex0()
    {

        VoxelCell cell = GetDefaultVoxelCell();
        cell.mVoxel[0].Density = 1;
        cell.CreateEdgeList();

        return cell.CalculateMesh();

    }

    private static Mesh Vertex01()
    {

        VoxelCell cell = GetDefaultVoxelCell();
        cell.mVoxel[0].Density = 1;
        cell.mVoxel[1].Density = 1;
        cell.CreateEdgeList();

       return cell.CalculateMesh();

    }
    private static Mesh Vertex012()
    {

        VoxelCell cell = GetDefaultVoxelCell();
        cell.mVoxel[0].Density = 1;
        cell.mVoxel[1].Density = 1;
        cell.mVoxel[2].Density = 1;
        cell.CreateEdgeList();

       return cell.CalculateMesh();

    }

    /// <summary>
    /// Create a default VoxellCell with a cube based at the origin of length one
    /// </summary>
    /// <returns>A voxel cell with length one and density 0</returns>
    private static VoxelCell GetDefaultVoxelCell()
    {
        VoxelCell cell = new VoxelCell(0);
        // Front face 
        cell.mVoxel[0] = new Voxel(new Vector3(0, 0, 0), 0);
        cell.mVoxel[1] = new Voxel(new Vector3(0, 1, 0), 0);
        cell.mVoxel[2] = new Voxel(new Vector3(1, 1, 0), 0);
        cell.mVoxel[3] = new Voxel(new Vector3(1, 0, 0), 0);
        //back face
        cell.mVoxel[4] = new Voxel(new Vector3(0, 0, 1), 0);
        cell.mVoxel[5] = new Voxel(new Vector3(0, 1, 1), 0);
        cell.mVoxel[6] = new Voxel(new Vector3(1, 1, 1), 0);
        cell.mVoxel[7] = new Voxel(new Vector3(1, 0, 1), 0);

        return cell;

    }


}