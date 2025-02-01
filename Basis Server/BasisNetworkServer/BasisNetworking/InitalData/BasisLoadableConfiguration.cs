using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace BasisNetworking.InitialData
{
    [Serializable]
    public class BasisLoadableConfiguration
    {
        public byte Mode = 0;
        public string LoadedNetID = "";
        public string UnlockPassword = "";
        public string MetaURL = "";
        public string BundleURL = "";
        public bool IsLocalLoad = false;

        public float PositionX = 0f;
        public float PositionY = 0f;
        public float PositionZ = 0f;

        public float QuaternionX = 0f;
        public float QuaternionY = 0f;
        public float QuaternionZ = 0f;
        public float QuaternionW = 0f;

        public float ScaleX = 1f;
        public float ScaleY = 1f;
        public float ScaleZ = 1f;

        public bool Persist = false;

        public static BasisLoadableConfiguration LoadFromXml(string filePath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            return new BasisLoadableConfiguration
            {
                Mode = byte.Parse(doc.SelectSingleNode("/Resource/Mode")?.InnerText ?? "0"),
                LoadedNetID = doc.SelectSingleNode("/Resource/LoadedNetID")?.InnerText ?? "",
                UnlockPassword = doc.SelectSingleNode("/Resource/UnlockPassword")?.InnerText ?? "",
                MetaURL = doc.SelectSingleNode("/Resource/MetaURL")?.InnerText ?? "",
                BundleURL = doc.SelectSingleNode("/Resource/BundleURL")?.InnerText ?? "",
                IsLocalLoad = bool.Parse(doc.SelectSingleNode("/Resource/IsLocalLoad")?.InnerText ?? "false"),

                PositionX = float.Parse(doc.SelectSingleNode("/Resource/PositionX")?.InnerText ?? "0"),
                PositionY = float.Parse(doc.SelectSingleNode("/Resource/PositionY")?.InnerText ?? "0"),
                PositionZ = float.Parse(doc.SelectSingleNode("/Resource/PositionZ")?.InnerText ?? "0"),

                QuaternionX = float.Parse(doc.SelectSingleNode("/Resource/QuaternionX")?.InnerText ?? "0"),
                QuaternionY = float.Parse(doc.SelectSingleNode("/Resource/QuaternionY")?.InnerText ?? "0"),
                QuaternionZ = float.Parse(doc.SelectSingleNode("/Resource/QuaternionZ")?.InnerText ?? "0"),
                QuaternionW = float.Parse(doc.SelectSingleNode("/Resource/QuaternionW")?.InnerText ?? "1"),

                ScaleX = float.Parse(doc.SelectSingleNode("/Resource/ScaleX")?.InnerText ?? "1"),
                ScaleY = float.Parse(doc.SelectSingleNode("/Resource/ScaleY")?.InnerText ?? "1"),
                ScaleZ = float.Parse(doc.SelectSingleNode("/Resource/ScaleZ")?.InnerText ?? "1"),

                Persist = bool.Parse(doc.SelectSingleNode("/Resource/Persist")?.InnerText ?? "false")
            };
        }

        public static BasisLoadableConfiguration[] LoadAllFromFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"The folder '{folderPath}' does not exist.");
            }

            List<BasisLoadableConfiguration> configurations = new List<BasisLoadableConfiguration>();

            string[] xmlFiles = Directory.GetFiles(folderPath, "*.xml");
            foreach (var file in xmlFiles)
            {
                configurations.Add(LoadFromXml(file));
            }

            return configurations.ToArray();
        }
    }
}
