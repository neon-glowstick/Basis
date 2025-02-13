using System;
using System.IO;
using System.Linq;
using Amazon.Runtime;
using Amazon.S3;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace org.BasisVr.Contrib.Upload.S3
{
    [InitializeOnLoad]
    public static class S3AvatarUploader
    {
        static S3AvatarUploader()
        {
            // We're extending the custom editor of the avatar inspector
            BasisAvatarSDKInspector.InspectorGuiCreated += OnInspectorGuiCreated;
        }

        private const string ConfigFile = "BasisVrS3UploadConfig.json";
        private static S3Config _config = new();

        private static Button _uploadButton;
        private static TextField _accessKeyField;
        private static TextField _secretKeyField;
        private static TextField _serviceUrlField;
        private static TextField _bucketNameField;

        private static string ConfigDirectory => Application.persistentDataPath;

        private static void OnInspectorGuiCreated(BasisAvatarSDKInspector inspector)
        {
            inspector.rootElement.Add(BuildGui(_config));
        }

        private static VisualElement BuildGui(S3Config config)
        {
            var container = new Foldout
            {
                text = "S3 Uploader"
            };

            container.Add(AddConfigFoldout(config));

            _uploadButton = new Button(OnClickedUpload)
            {
                text = "Upload to bucket"
            };
            container.Add(_uploadButton);

            return container;
        }

        private static VisualElement AddConfigFoldout(S3Config config)
        {
            var container = new Foldout
            {
                text = "Config"
            };

            var crudButtons = ConfigCrudButtons();
            container.Add(crudButtons);

            _accessKeyField = PasswordField("Access key", config.AccessKey);
            _accessKeyField.RegisterValueChangedCallback(value => config.AccessKey = value.newValue);
            container.Add(_accessKeyField);

            _secretKeyField = PasswordField("Secret key", config.SecretKey);
            _secretKeyField.RegisterValueChangedCallback(value => config.SecretKey = value.newValue);
            container.Add(_secretKeyField);

            _serviceUrlField = Textfield("ServiceUrl", config.ServiceUrl);
            _serviceUrlField.RegisterValueChangedCallback(value => config.ServiceUrl = value.newValue);
            container.Add(_serviceUrlField);

            _bucketNameField = Textfield("Bucket name", config.AvatarBucket);
            _bucketNameField.RegisterValueChangedCallback(value => config.AvatarBucket = value.newValue);
            container.Add(_bucketNameField);

            return container;
        }

        private static VisualElement ConfigCrudButtons()
        {
            var crudButtons = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row }
            };

            var loadButton = new Button(() =>
            {
                _config = LoadConfig();
                _secretKeyField.SetValueWithoutNotify(_config.SecretKey);
                _accessKeyField.SetValueWithoutNotify(_config.AccessKey);
                _serviceUrlField.SetValueWithoutNotify(_config.ServiceUrl);
                _bucketNameField.SetValueWithoutNotify(_config.AvatarBucket);
            })
            {
                text = "Load",
                style = { flexGrow = -1f }
            };
            crudButtons.Add(loadButton);
            var saveButton = new Button(() => SaveConfig(_config))
            {
                text = "Save",
                style = { flexGrow = -1f }
            };
            crudButtons.Add(saveButton);
            var deleteButton = new Button(DeleteConfig)
            {
                text = "Delete",
                style = { flexGrow = -1f }
            };
            crudButtons.Add(deleteButton);
            return crudButtons;
        }

        private static async void OnClickedUpload()
        {
            _uploadButton.SetEnabled(false);
            try
            {
                var assetBundleDirectory = Path.Combine(Application.dataPath, "../AssetBundles");
                if (!TryGetFilesToUpload(assetBundleDirectory, out var assetBundlePath, out var metaFilePath))
                {
                    Debug.LogError($"Could not find avatar to upload in :{assetBundleDirectory}");
                    return;
                }

                var credentials = new BasicAWSCredentials(_config.AccessKey, _config.SecretKey);
                using var client = new AmazonS3Client(credentials, new AmazonS3Config
                {
                    ServiceURL = _config.ServiceUrl,
                    RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                    ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
                });

                await client.PutObjectAsync(_config.AvatarBucket, assetBundlePath, Application.exitCancellationToken);
                await client.PutObjectAsync(_config.AvatarBucket, metaFilePath, Application.exitCancellationToken);

                // todo Progress bar for upload of bundle and meta file

                // todo Display urls where the avatar and meta file were uploaded. And button to copy to clipboard?
            }
            catch (Exception e)
            {
                Debug.LogError($"Upload failed: {e.Message}");
            }
            _uploadButton.SetEnabled(true);
        }

        private static bool TryGetFilesToUpload(string directory, out string assetBundlePath, out string metaFilePath)
        {
            assetBundlePath = string.Empty;
            metaFilePath = string.Empty;

            // Make some assumptions, at the time of writing:
            // The files are placed in the AssetBundles folder at the root of the Unity project
            // There can only be 1 avatar built at a time. Building a new avatar replaces the files in AssetBundles
            Debug.Log(directory);
            if (!Directory.Exists(directory))
                return false;

            var files = Directory.GetFiles(directory);
            assetBundlePath = files.FirstOrDefault(f => f.EndsWith("BasisEncryptedBundle"));
            metaFilePath = files.FirstOrDefault(f => f.EndsWith("BasisEncryptedMeta"));

            var foundBoth = !string.IsNullOrEmpty(assetBundlePath) && !string.IsNullOrEmpty(metaFilePath);
            return foundBoth;
        }

        private static S3Config LoadConfig()
        {
            var path = Path.Combine(ConfigDirectory, ConfigFile);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var config = JsonUtility.FromJson<S3Config>(json);
                return config;
            }

            return new S3Config();
        }

        private static void SaveConfig(S3Config config)
        {
            var directory = ConfigDirectory;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var path = Path.Combine(directory, ConfigFile);
            var json = JsonUtility.ToJson(config);
            File.WriteAllText(path, json);
        }

        private static void DeleteConfig()
        {
            var path = Path.Combine(ConfigDirectory, ConfigFile);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static TextField PasswordField(string label, string value)
        {
            return new TextField(128, false, true, '•')
            {
                label = label,
                value = value
            };
        }

        private static TextField Textfield(string label, string value)
        {
            return new TextField
            {
                multiline = false,
                label = label,
                value = value
            };
        }
    }
}
