using Basis.Scripts.Networking.Compression;

namespace BasisNetworkClientConsole
{
    public class Randomizer
    {
        public static Vector3 GetRandomPosition(Vector3 min, Vector3 max)
        {
            Random random = new Random();

            float randomX = GetRandomFloat(random, min.x, max.x);
            float randomY = GetRandomFloat(random, min.y, max.y);
            float randomZ = GetRandomFloat(random, min.z, max.z);

            return new Vector3(randomX, randomY, randomZ);
        }

        public static float GetRandomFloat(Random random, float min, float max)
        {
            return (float)(random.NextDouble() * (max - min) + min);
        }
    }
}
