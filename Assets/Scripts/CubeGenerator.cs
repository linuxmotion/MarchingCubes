using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



class Block  {

    GameObject mBlock;
    Vector3 mLocation;
    int mBlockType;


}

class Layer
{
    public List<GameObject> mLayer;
    public Layer(int width, int length)
    {

        mLayer = new List<GameObject>(width * length);

    }
    public void Clear()
    {
        mLayer.Clear();
    }

    public void Add(GameObject obj)
    {
        mLayer.Add(obj);
    }
}
class Chunk
{

    private List<Layer> mChunk;

    public Chunk(int buildHeight)
    {

        mChunk = new List<Layer>(buildHeight);


    }
    public void Clear()
    {
        mChunk.Clear();
    }
    public void Add(Layer obj)
    {
        mChunk.Add(obj);
    }
}

//[ExecuteInEditMode]
public class CubeGenerator : MonoBehaviour
{
    [SerializeField]
    private bool _GenerateTerrain;
    [SerializeField]
    private GameObject _Prefab;
    [SerializeField]
    private int _RenderDistance;
    [SerializeField]
    private int _SimulationDistance;
    [SerializeField]
    private int _BuildHeight;
    [SerializeField]
    private int _Length = 16;
    [SerializeField]
    private int _Width = 16;
    [SerializeField]
    private Transform _PlayerLocation;

    // The location to generate terrain arround
    private Transform Generated;

    private Layer mLayer;
    // List of all the block on each layer of the chunk
    private Chunk mChunk;
    // Terrain that should be rendered
    private List<Chunk> mRenderChunks;

    // Start is called before the first frame update
    void Start()
    {

        if (!_GenerateTerrain)
            return;

        GenerateAroundPlayer();

        _GenerateTerrain = false;


    }

    private void GenerateAroundPlayer()
    {
        Vector3 playerCenter = _PlayerLocation.position;
        Vector3 bottomLeft = Vector3.zero;
        // set the bottom side to start
        bottomLeft.z = playerCenter.z - (_Length * _RenderDistance);

        for (int j = 0; j < _RenderDistance * 2; j++)
        {
            // set x to the left side of the chunk for each pass
            bottomLeft.x = playerCenter.x - (_Length * _RenderDistance);
            for (int i = 0; i < _RenderDistance * 2; i++)
            {
                GenerateSingleChunk(bottomLeft);
                bottomLeft.x += _Length;
            }
            // advance the generateor one chunk width
            bottomLeft.z += _Width;

        }
    }

    private void GenerateSingleChunk(Vector3 generateFrom)
    {

        float noise;
        int l = 16, m = 16;

        Generated = transform;
        mLayer = new Layer(_Length, _Width);// new List<GameObject>(l * m);
        mChunk = new Chunk(_BuildHeight);  // new List<Chunk>(_BuildHeight);

        for (int k = 0; k < _BuildHeight; k++)
        {
            mLayer.Clear();
            for (int i = 0; i < l; i++)
            {
                for (int j = 0; j < m; j++)
                {
                   
    

                    GameObject clone;
                    clone = Instantiate(_Prefab);
                    var rend = clone.GetComponent<Renderer>();
                    var b = rend.bounds;
                    clone.transform.position = new Vector3(generateFrom.x + b.size.x * j,
                                                            generateFrom.y + b.size.y * k,
                                                             generateFrom.z + b.size.z * i);


                    float x = clone.transform.position.x;// + (i + 1f) * (k + 1f))/256f;
                    float z = clone.transform.position.z; //+ (j + 1f) * (k + 1f))/256f;
                    Vector2 vect = new Vector2(x,z);
                    Debug.Log(vect);
                    noise = Mathf.PerlinNoise(x / 100f, z / 100f);
                    Debug.Log(noise);



                    clone.transform.SetParent(Generated);
                    clone.isStatic = true;
                    clone.SetActive(true);
                    if (noise < 0.5f)
                    {
                        rend.material.color = Color.blue;
                    }

                    mLayer.Add(clone);

                }

            }
            mChunk.Add(mLayer);
        }


    }

    // Update is called once per frame
    void Update()
    {

    }
}
