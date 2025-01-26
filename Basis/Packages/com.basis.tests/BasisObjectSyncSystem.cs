using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class BasisObjectSyncSystem : MonoBehaviour
{
    public static BasisObjectSyncSystem Instance { get; private set; }

    private BasisObjectSyncNetworking[] objectsToSync;
    private int objectCount = 0; // Number of active objects
    private int arrayCapacity = 16; // Initial capacity
    private NativeArray<float3> positions;
    private NativeArray<quaternion> rotations;
    private NativeArray<float3> scales;
    private NativeArray<float3> targetPositions;
    private NativeArray<quaternion> targetRotations;
    private NativeArray<float3> targetScales;
    private NativeArray<float> lerpMultipliers;

    private JobHandle syncJobHandle;
    private bool initialized = false;
    public ObjectSyncJob job;
    public int BatchSize = 16;
    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        objectsToSync = new BasisObjectSyncNetworking[arrayCapacity];
    }

    public void Update()
    {
        if (!initialized || objectCount == 0) return;

        job.DeltaTime = Time.deltaTime;
        syncJobHandle = job.Schedule(objectCount, BatchSize);
    }
    public void LateUpdate()
    {
        if (!initialized || objectCount == 0) return;

        syncJobHandle.Complete();

        // Apply results back to transforms on the main thread.
        for (int Index = 0; Index < objectCount; Index++)
        {
            BasisObjectSyncNetworking obj = objectsToSync[Index];
            Transform transform = obj.transform;

            transform.SetLocalPositionAndRotation(positions[Index], rotations[Index]);
            transform.localScale = scales[Index];
        }
    }
    public static void StopApplyRemoteData(BasisObjectSyncNetworking obj)
    {
        if (Instance == null)
        {
            BasisDebug.LogError("Missing Instance of " + nameof(BasisObjectSyncSystem));
            return;
        }
        Instance.unregisterObject(obj);
    }
    public static void StartApplyRemoteData(BasisObjectSyncNetworking obj)
    {
        if (Instance == null)
        {
            BasisDebug.LogError("Missing Instance of " + nameof(BasisObjectSyncSystem));
            return;
        }
        Instance.registerObject(obj);
    }
    public void registerObject(BasisObjectSyncNetworking obj)
    {
        if (objectsToSync.Contains(obj))
        {
            BasisDebug.LogError($"Already Have This Registered {obj.GetInstanceID()}");
            return;
        }
        EnsureCapacity(objectCount + 1);

        objectsToSync[objectCount] = obj;
        objectCount++;

        ResizeNativeArrays(objectCount);
        UpdateNativeData();
        // Dynamically adjust batch size based on object count
        BatchSize = CalculateBatchSize(objectCount);
    }
    public void unregisterObject(BasisObjectSyncNetworking obj)
    {
        if (objectsToSync.Contains(obj) == false)
        {
            BasisDebug.LogError($"No Registered Object {obj.GetInstanceID()}");
            return;
        }
        // Find the index of the object to remove
        int removeIndex = -1;
        for (int Index = 0; Index < objectCount; Index++)
        {
            if (objectsToSync[Index] == obj)
            {
                removeIndex = Index;
                break;
            }
        }

        if (removeIndex == -1)
        {
            return; // Object not found
        }
        // Move the last object in the list to fill the gap
        objectsToSync[removeIndex] = objectsToSync[objectCount - 1];
        objectCount--;

        // Compact the NativeArrays
        CompactNativeArrays(removeIndex);
        // Dynamically adjust batch size based on object count
        BatchSize = CalculateBatchSize(objectCount);
    }

    /// <summary>
    /// Compacts the NativeArrays by shifting elements after a removal.
    /// </summary>
    /// <param name="removedIndex">Index of the removed object.</param>
    private void CompactNativeArrays(int removedIndex)
    {
        // Shift elements after the removed index to fill the gap
        for (int i = removedIndex; i < objectCount; i++)
        {
            int Index = i + 1;
            positions[i] = positions[Index];
            rotations[i] = rotations[Index];
            scales[i] = scales[Index];
            targetPositions[i] = targetPositions[Index];
            targetRotations[i] = targetRotations[Index];
            targetScales[i] = targetScales[Index];
            lerpMultipliers[i] = lerpMultipliers[Index];
        }
    }
    private void EnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity <= arrayCapacity) return;

        // Double the capacity until it fits
        arrayCapacity = math.max(arrayCapacity * 2, requiredCapacity);
        BasisObjectSyncNetworking[] newArray = new BasisObjectSyncNetworking[arrayCapacity];
        for (int Index = 0; Index < objectCount; Index++)
        {
            newArray[Index] = objectsToSync[Index];
        }

        objectsToSync = newArray;
    }

    private void ResizeNativeArrays(int size)
    {
        // Dispose and reinitialize only when necessary to minimize overhead.
        if (initialized)
        {
            positions.Dispose();
            rotations.Dispose();
            scales.Dispose();
            targetPositions.Dispose();
            targetRotations.Dispose();
            targetScales.Dispose();
            lerpMultipliers.Dispose();
        }
        job = new ObjectSyncJob();
        positions = new NativeArray<float3>(size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        rotations = new NativeArray<quaternion>(size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        scales = new NativeArray<float3>(size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        targetPositions = new NativeArray<float3>(size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        targetRotations = new NativeArray<quaternion>(size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        targetScales = new NativeArray<float3>(size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        lerpMultipliers = new NativeArray<float>(size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        job.CurrentPositions = positions;
        job.CurrentRotations = rotations;
        job.CurrentScales = scales;
        job.TargetPositions = targetPositions;
        job.TargetRotations = targetRotations;
        job.TargetScales = targetScales;
        job.LerpMultipliers = lerpMultipliers;

        initialized = true;
    }

    private void UpdateNativeData()
    {
        for (int Index = 0; Index < objectCount; Index++)
        {
            var obj = objectsToSync[Index];
            var transform = obj.transform;

            transform.GetLocalPositionAndRotation(out Vector3 position, out Quaternion rotation);
            positions[Index] = position;
            rotations[Index] = rotation;
            scales[Index] = transform.localScale;

            targetPositions[Index] = obj.StoredData.Position;
            targetRotations[Index] = obj.StoredData.Rotation;
            targetScales[Index] = obj.StoredData.Scale;
            lerpMultipliers[Index] = obj.LerpMultiplier;
        }
    }

    private void OnDestroy()
    {
        if (initialized)
        {
            positions.Dispose();
            rotations.Dispose();
            scales.Dispose();
            targetPositions.Dispose();
            targetRotations.Dispose();
            targetScales.Dispose();
            lerpMultipliers.Dispose();
        }
    }
    /// <summary>
    /// Calculates an optimized batch size based on the number of objects.
    /// </summary>
    /// <param name="elementCount">Total number of elements</param>
    /// <returns>Optimized batch size</returns>
    private int CalculateBatchSize(int elementCount)
    {
        if (elementCount < 64)
            return 1;  // No need for batching on small counts
        if (elementCount < 512)
            return 16; // Small batch sizes for moderate workloads
        if (elementCount < 2048)
            return 64; // Balanced performance for mid-range counts
        return 128;    // Larger batch size for heavy workloads
    }

    [BurstCompile]
    public struct ObjectSyncJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> TargetPositions;
        [ReadOnly] public NativeArray<quaternion> TargetRotations;
        [ReadOnly] public NativeArray<float3> TargetScales;
        [ReadOnly] public NativeArray<float> LerpMultipliers;
        [ReadOnly] public float DeltaTime;

        public NativeArray<float3> CurrentPositions;
        public NativeArray<quaternion> CurrentRotations;
        public NativeArray<float3> CurrentScales;

        public void Execute(int index)
        {
            float lerp = LerpMultipliers[index] * DeltaTime;

            CurrentPositions[index] = math.lerp(CurrentPositions[index], TargetPositions[index], lerp);
            CurrentRotations[index] = math.slerp(CurrentRotations[index], TargetRotations[index], lerp);
            CurrentScales[index] = math.lerp(CurrentScales[index], TargetScales[index], lerp);
        }
    }
}
