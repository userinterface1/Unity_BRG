using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

public class BRG : MonoBehaviour
{
    public Mesh mesh;
    public Material[] material;

    private BatchRendererGroup m_BRG;

    private GraphicsBuffer m_InstanceData;
    private BatchID m_BatchID;
    private BatchMeshID m_MeshID;
    private BatchMaterialID m_MaterialID;

    private GraphicsBuffer m_InstanceData2;
    private BatchID m_BatchID2;
    private BatchMaterialID m_MaterialID2;

    private const int kSizeOfMatrix = sizeof(float) * 4 * 4;
    private const int kSizeOfPackedMatrix = sizeof(float) * 4 * 3;
    private const int kSizeOfFloat = sizeof(float);
    private const int kBytesPerInstance = (kSizeOfPackedMatrix * 2) + kSizeOfFloat;
    private const int kExtraBytes = kSizeOfMatrix * 2;
    private const int kNumInstances = 3;

    struct PackedMatrix
    {
        public float c0x;
        public float c0y;
        public float c0z;
        public float c1x;
        public float c1y;
        public float c1z;
        public float c2x;
        public float c2y;
        public float c2z;
        public float c3x;
        public float c3y;
        public float c3z;

        public PackedMatrix(Matrix4x4 m)
        {
            c0x = m.m00;
            c0y = m.m10;
            c0z = m.m20;
            c1x = m.m01;
            c1y = m.m11;
            c1z = m.m21;
            c2x = m.m02;
            c2y = m.m12;
            c2z = m.m22;
            c3x = m.m03;
            c3y = m.m13;
            c3z = m.m23;
        }
    }

    private void Start()
    {
        m_BRG = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);
        m_MeshID = m_BRG.RegisterMesh(mesh);
        m_MaterialID = m_BRG.RegisterMaterial(material[0]);
        m_MaterialID2 = m_BRG.RegisterMaterial(material[1]);

        AllocateInstanceDateBuffer();
        AllocateInstanceDateBuffer2();

        PopulateInstanceDataBuffer();
        PopulateInstanceDataBuffer2();
    }

    private void AllocateInstanceDateBuffer()
    {
        m_InstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Raw,
            BufferCountForInstances(kBytesPerInstance, kNumInstances, kExtraBytes),
            sizeof(int));
    }

    private void AllocateInstanceDateBuffer2()
    {
        m_InstanceData2 = new GraphicsBuffer(GraphicsBuffer.Target.Raw,
            BufferCountForInstances(kBytesPerInstance, kNumInstances, kExtraBytes),
            sizeof(int));
    }

    private void PopulateInstanceDataBuffer()
    {
        var zero = new Matrix4x4[1] { Matrix4x4.zero };

        var objectToWorld = new PackedMatrix[kNumInstances]
        {
            new PackedMatrix(Matrix4x4.Translate(new Vector3(0,0,0))),
            new PackedMatrix(Matrix4x4.Translate(new Vector3(1,0,0))),
            new PackedMatrix(Matrix4x4.Translate(new Vector3(2,0,0))),
        };
        var worldToObject = new PackedMatrix[kNumInstances]
        {
            new PackedMatrix(Matrix4x4.Translate(new Vector3(0,0,0)).inverse),
            new PackedMatrix(Matrix4x4.Translate(new Vector3(1,0,0)).inverse),
            new PackedMatrix(Matrix4x4.Translate(new Vector3(2,0,0)).inverse),
        };
        var uvs = new int[kNumInstances]
        {
            0,
            2,
            3,
        };

        uint byteAddressObjectToWorld = kSizeOfPackedMatrix * 2;
        uint byteAddressWorldToObject = byteAddressObjectToWorld + kSizeOfPackedMatrix * kNumInstances;
        uint byteAddressIndex = byteAddressWorldToObject + kSizeOfPackedMatrix * kNumInstances;

        m_InstanceData.SetData(zero, 0, 0, 1);
        m_InstanceData.SetData(objectToWorld, 0, (int)(byteAddressObjectToWorld / kSizeOfPackedMatrix), objectToWorld.Length);
        m_InstanceData.SetData(worldToObject, 0, (int)(byteAddressWorldToObject / kSizeOfPackedMatrix), worldToObject.Length);
        m_InstanceData.SetData(uvs, 0, (int)(byteAddressIndex / kSizeOfFloat), uvs.Length);

        var metadata = new NativeArray<MetadataValue>(3, Allocator.Temp);
        metadata[0] = new MetadataValue { NameID = Shader.PropertyToID("unity_ObjectToWorld"), Value = 0x80000000 | byteAddressObjectToWorld, };
        metadata[1] = new MetadataValue { NameID = Shader.PropertyToID("unity_WorldToObject"), Value = 0x80000000 | byteAddressWorldToObject, };
        metadata[2] = new MetadataValue { NameID = Shader.PropertyToID("_Index"), Value = 0x80000000 | byteAddressIndex, };

        m_BatchID = m_BRG.AddBatch(metadata, m_InstanceData.bufferHandle);
    }

    private void PopulateInstanceDataBuffer2()
    {
        var zero = new Matrix4x4[1] { Matrix4x4.zero };

        var objectToWorld = new PackedMatrix[kNumInstances]
        {
            new PackedMatrix(Matrix4x4.Translate(new Vector3(0,0,1))),
            new PackedMatrix(Matrix4x4.Translate(new Vector3(0,0,2))),
            new PackedMatrix(Matrix4x4.Translate(new Vector3(0,0,3))),
        };
        var worldToObject = new PackedMatrix[kNumInstances]
        {
            new PackedMatrix(Matrix4x4.Translate(new Vector3(0,0,1)).inverse),
            new PackedMatrix(Matrix4x4.Translate(new Vector3(0,0,2)).inverse),
            new PackedMatrix(Matrix4x4.Translate(new Vector3(0,0,3)).inverse),
        };
        var uvs = new int[kNumInstances]
        {
            0,
            2,
            3,
        };

        uint byteAddressObjectToWorld = kSizeOfPackedMatrix * 2;
        uint byteAddressWorldToObject = byteAddressObjectToWorld + kSizeOfPackedMatrix * kNumInstances;
        uint byteAddressIndex = byteAddressWorldToObject + kSizeOfPackedMatrix * kNumInstances;

        m_InstanceData2.SetData(zero, 0, 0, 1);
        m_InstanceData2.SetData(objectToWorld, 0, (int)(byteAddressObjectToWorld / kSizeOfPackedMatrix), objectToWorld.Length);
        m_InstanceData2.SetData(worldToObject, 0, (int)(byteAddressWorldToObject / kSizeOfPackedMatrix), worldToObject.Length);
        m_InstanceData2.SetData(uvs, 0, (int)(byteAddressIndex / kSizeOfFloat), uvs.Length);

        var metadata = new NativeArray<MetadataValue>(3, Allocator.Temp);
        metadata[0] = new MetadataValue { NameID = Shader.PropertyToID("unity_ObjectToWorld"), Value = 0x80000000 | byteAddressObjectToWorld, };
        metadata[1] = new MetadataValue { NameID = Shader.PropertyToID("unity_WorldToObject"), Value = 0x80000000 | byteAddressWorldToObject, };
        metadata[2] = new MetadataValue { NameID = Shader.PropertyToID("_Index"), Value = 0x80000000 | byteAddressIndex, };

        m_BatchID2 = m_BRG.AddBatch(metadata, m_InstanceData2.bufferHandle);
    }

    int BufferCountForInstances(int bytesPerInstance, int numInstances, int extraBytes = 0)
    {
        bytesPerInstance = (bytesPerInstance + sizeof(int) - 1) / sizeof(int) * sizeof(int);
        extraBytes = (extraBytes + sizeof(int) - 1) / sizeof(int) * sizeof(int);
        int totalBytes = bytesPerInstance * numInstances + extraBytes;
        return totalBytes / sizeof(int);
    }


    private void OnDisable()
    {
        m_BRG.Dispose();
    }

    [BurstCompile]
    public unsafe struct Job : IJobParallelFor
    {
        [NativeDisableUnsafePtrRestriction]
        public int* visibleInstances;

        [BurstCompile]
        public void Execute(int index)
        {
            visibleInstances[index] = index;
        }
    }

    [BurstCompile]
    public unsafe JobHandle OnPerformCulling(
        BatchRendererGroup rendererGroup,
        BatchCullingContext cullingContext,
        BatchCullingOutput cullingOutput,
        IntPtr userContext)
    {
        int alignment = UnsafeUtility.AlignOf<long>();

        var drawCommands = (BatchCullingOutputDrawCommands*)cullingOutput.drawCommands.GetUnsafePtr();

        drawCommands->drawCommands = (BatchDrawCommand*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawCommand>() * 2, alignment, Allocator.TempJob);
        drawCommands->drawRanges = (BatchDrawRange*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawRange>(), alignment, Allocator.TempJob);
        drawCommands->visibleInstances = (int*)UnsafeUtility.Malloc(kNumInstances * 2 * sizeof(int), alignment, Allocator.TempJob);
        drawCommands->drawCommandPickingInstanceIDs = null;

        drawCommands->drawCommandCount = 2;
        drawCommands->drawRangeCount = 1;
        drawCommands->visibleInstanceCount = kNumInstances * 2;

        drawCommands->instanceSortingPositions = null;
        drawCommands->instanceSortingPositionFloatCount = 0;

        drawCommands->drawCommands[0].visibleOffset = 0;
        drawCommands->drawCommands[0].visibleCount = kNumInstances;
        drawCommands->drawCommands[0].batchID = m_BatchID;
        drawCommands->drawCommands[0].materialID = m_MaterialID;
        drawCommands->drawCommands[0].meshID = m_MeshID;
        drawCommands->drawCommands[0].submeshIndex = 0;
        drawCommands->drawCommands[0].splitVisibilityMask = 0xff;
        drawCommands->drawCommands[0].flags = 0;
        drawCommands->drawCommands[0].sortingPosition = 0;

        drawCommands->drawRanges[0].drawCommandsBegin = 0;
        drawCommands->drawRanges[0].drawCommandsCount = 2;

        drawCommands->drawRanges[0].filterSettings = new BatchFilterSettings { renderingLayerMask = 0xffffffff, };

        drawCommands->drawCommands[1].visibleOffset = 0;
        drawCommands->drawCommands[1].visibleCount = kNumInstances;
        drawCommands->drawCommands[1].batchID = m_BatchID2;
        drawCommands->drawCommands[1].materialID = m_MaterialID2;
        drawCommands->drawCommands[1].meshID = m_MeshID;
        drawCommands->drawCommands[1].submeshIndex = 0;
        drawCommands->drawCommands[1].splitVisibilityMask = 0xff;
        drawCommands->drawCommands[1].flags = 0;
        drawCommands->drawCommands[1].sortingPosition = 0;

        for (int i = 0; i < kNumInstances * 2; ++i)
        {
            drawCommands->visibleInstances[i] = i;
        }

        //Job job = new Job()
        //{
        //    visibleInstances = drawCommands->visibleInstances,
        //};
        //return job.Schedule(kNumInstances * 2, kNumInstances * 2 / 16);

        return new JobHandle();
    }
}
