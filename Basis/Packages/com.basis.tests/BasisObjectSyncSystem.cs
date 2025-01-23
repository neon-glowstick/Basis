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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        objectsToSync = new BasisObjectSyncNetworking[arrayCapacity];
    }

    private void Update()
    {
        if (!initialized || objectCount == 0) return;

        var job = new ObjectSyncJob
        {
            CurrentPositions = positions,
            CurrentRotations = rotations,
            CurrentScales = scales,
            TargetPositions = targetPositions,
            TargetRotations = targetRotations,
            TargetScales = targetScales,
            LerpMultipliers = lerpMultipliers,
            DeltaTime = Time.deltaTime
        };

        syncJobHandle = job.Schedule(objectCount, 128); // Larger batch size for fewer thread syncs.
    }

    private void LateUpdate()
    {
        if (!initialized || objectCount == 0) return;

        syncJobHandle.Complete();

        // Apply results back to transforms on the main thread.
        for (int i = 0; i < objectCount; i++)
        {
            var obj = objectsToSync[i];
            var transform = obj.transform;

            transform.SetPositionAndRotation(positions[i], rotations[i]);
            transform.localScale = scales[i];
        }
    }

    public void RegisterObject(BasisObjectSyncNetworking obj)
    {
        EnsureCapacity(objectCount + 1);

        objectsToSync[objectCount] = obj;
        objectCount++;

        ResizeNativeArrays(objectCount);
        UpdateNativeData();
    }
    public void UnregisterObject(BasisObjectSyncNetworking obj)
    {
        // Find the index of the object to remove
        int removeIndex = -1;
        for (int i = 0; i < objectCount; i++)
        {
            if (objectsToSync[i] == obj)
            {
                removeIndex = i;
                break;
            }
        }

        if (removeIndex == -1) return; // Object not found

        // Move the last object in the list to fill the gap
        objectsToSync[removeIndex] = objectsToSync[objectCount - 1];
        objectCount--;

        // Compact the NativeArrays
        CompactNativeArrays(removeIndex);
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
            positions[i] = positions[i + 1];
            rotations[i] = rotations[i + 1];
            scales[i] = scales[i + 1];
            targetPositions[i] = targetPositions[i + 1];
            targetRotations[i] = targetRotations[i + 1];
            targetScales[i] = targetScales[i + 1];
            lerpMultipliers[i] = lerpMultipliers[i + 1];
        }
    }
    private void EnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity <= arrayCapacity) return;

        // Double the capacity until it fits
        arrayCapacity = math.max(arrayCapacity * 2, requiredCapacity);
        var newArray = new BasisObjectSyncNetworking[arrayCapacity];
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

        positions = new NativeArray<float3>(size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        rotations = new NativeArray<quaternion>(size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        scales = new NativeArray<float3>(size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        targetPositions = new NativeArray<float3>(size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        targetRotations = new NativeArray<quaternion>(size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        targetScales = new NativeArray<float3>(size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        lerpMultipliers = new NativeArray<float>(size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        initialized = true;
    }

    private void UpdateNativeData()
    {
        for (int i = 0; i < objectCount; i++)
        {
            var obj = objectsToSync[i];
            var transform = obj.transform;

            transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
            positions[i] = position;
            rotations[i] = rotation;
            scales[i] = transform.localScale;

            targetPositions[i] = obj.StoredData.Position;
            targetRotations[i] = obj.StoredData.Rotation;
            targetScales[i] = obj.StoredData.Scale;
            lerpMultipliers[i] = obj.LerpMultiplier;
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

    [BurstCompile]
    private struct ObjectSyncJob : IJobParallelFor
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
