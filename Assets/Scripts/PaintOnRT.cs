using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PaintOnRT : MonoBehaviour
{
    public RawImage maskImg;
    /// <summary>
    /// ��ˢ��ͼ
    /// </summary>
    public Texture2D brushTex;

    public Vector2 offsetFrom = new Vector2(20f, 20f);
    public Vector2 offsetTo = new Vector2(200f, 200f);

    /// <summary>
    /// ��ˢ�ߴ�
    /// </summary>
    public float brushSize = 10f;
    /// <summary>
    /// ��ˢ����
    /// </summary>
    [Range(1f, 20f)]
    public float precision = 5f;

    private RenderTexture _rt;
    private CommandBuffer _cb;
    private bool _isDirty;
    private Vector2 _beginPos;
    private Vector2 _endPos;
    
    private Mesh _quad;
    private Matrix4x4 _matrixProj;
    private Material _material;
    private int _instanceCountPerBatch = 200; // ÿһ���ε�ʵ���������ޣ�̫����Щ�豸�����쳣��
    private Matrix4x4[] _arrMatrixs;

    void Start()
    {
        if(maskImg == null)
        {
            enabled = false;
            return;
        }

        _arrMatrixs = new Matrix4x4[_instanceCountPerBatch];
        _rt = new RenderTexture(600, 600, 24, RenderTextureFormat.ARGB32,0); // TODO �ĳ��� Imageͬ���ߴ�
        _rt.antiAliasing = 2;
        maskImg.texture = _rt;

        Init();

        _cb = new CommandBuffer() { name = "paint cb" };
        ResetCB(true);
        _isDirty = false;
    }

    private void ResetCB(bool clearRT)
    {
        _cb.Clear();
        _cb.SetRenderTarget(_rt);

        if(clearRT)
            _cb.ClearRenderTarget(true, true, Color.green);
        
        _cb.SetViewProjectionMatrices(Matrix4x4.identity, _matrixProj);

        if(brushTex == null)
            brushTex = Texture2D.whiteTexture;
    }


    void LateUpdate()
    {
        _isDirty = true;
        if(BuildCommands())
        {
            Graphics.ExecuteCommandBuffer(_cb);
        }
    }

    private bool BuildCommands()
    {
        if (!_isDirty)
            return false;

        {// test
            _beginPos = offsetFrom;
            _endPos = offsetTo;
        }

        ResetCB(true);

        Vector2 fromToVec = _endPos - _beginPos;
        Vector2 dir = fromToVec.normalized;
        float len = fromToVec.magnitude;

        float offset = 0;
        int instCount = 0;
        while(offset <= len)
        {
            if (instCount >= _instanceCountPerBatch)
            {
                _cb.DrawMeshInstanced(_quad, 0, _material, 0, _arrMatrixs, instCount);
                instCount = 0;
            }

            Vector2 tmpPt = _beginPos + dir * offset;
            tmpPt -= Vector2.one * brushSize * 0.5f; // ����ˢ���е����Ƶ�
            offset += precision;

            _arrMatrixs[instCount++] = Matrix4x4.TRS(new Vector3(tmpPt.x, tmpPt.y, 0), Quaternion.identity, Vector3.one * brushSize);
        }

        if(instCount > 0)
        {
            _cb.DrawMeshInstanced(_quad, 0, _material, 0, _arrMatrixs, instCount);
        }
        // Instancing Draw Points

        _isDirty = false;
        return true;
    }

    private void Init()
    {
        _quad = new Mesh();
        _quad.SetVertices(new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 1, 0)
        });

        _quad.SetUVs(0, new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(1, 0),
                new Vector2(1, 1)
            });

        _quad.SetIndices(new int[] { 0, 1, 2, 3, 2, 1 }, MeshTopology.Triangles, 0, false);
        _quad.UploadMeshData(true);

        _matrixProj = Matrix4x4.Ortho(0, _rt.width, 0, _rt.height, -1f, 1f);

        Shader shader = Resources.Load<Shader>("PaintOnRT");
        _material = new Material(shader);
        _material.enableInstancing = true;
    }
}
