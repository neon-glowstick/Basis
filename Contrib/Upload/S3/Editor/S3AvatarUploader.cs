using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        private static Button _uploadButton;
        private static S3Config _config;

        private static string ConfigDirectory => Application.persistentDataPath;

        private static void OnInspectorGuiCreated(BasisAvatarSDKInspector inspector)
        {
            _config = LoadConfig();
            inspector.rootElement.Add(BuildGui(_config));
        }

        private static VisualElement BuildGui(S3Config config)
        {
            var container = new VisualElement();

            var accessKeyField = PasswordField("Access key", config.AccessKey);
            accessKeyField.RegisterValueChangedCallback(value => config.AccessKey = value.newValue);
            container.Add(accessKeyField);

            var secretKeyField = PasswordField("Secret key", config.SecretKey);
            secretKeyField.RegisterValueChangedCallback(value => config.SecretKey = value.newValue);
            container.Add(secretKeyField);

            var serviceUrlField = Textfield("ServiceUrl", config.ServiceUrl);
            serviceUrlField.RegisterValueChangedCallback(value => config.ServiceUrl = value.newValue);
            container.Add(serviceUrlField);

            var bucketNameField = Textfield("Bucket name", config.AvatarBucket);
            bucketNameField.RegisterValueChangedCallback(value => config.AvatarBucket = value.newValue);
            container.Add(bucketNameField);

            _uploadButton = new Button(OnClickedUpload)
            {
                text = "Upload to bucket"
            };

            container.Add(_uploadButton);
            return container;
        }

        private static async void OnClickedUpload()
        {
            _uploadButton.SetEnabled(false);
            try
            {
                await SaveConfigAsync(_config, Application.exitCancellationToken);

                // todo Actually upload
                // todo Progress bar for upload of bundle and meta file
            }
            catch (Exception e)
            {
                Debug.LogError($"Upload failed: {e.Message}");
            }
            _uploadButton.SetEnabled(true);
        }

        private static S3Config LoadConfig()
        {
            var directory = ConfigDirectory;
            EnsureDirectoryExists(directory);

            var path = Path.Combine(directory, ConfigFile);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var config = JsonUtility.FromJson<S3Config>(json);
                return config;
            }

            return new S3Config();
        }

        private static Task SaveConfigAsync(S3Config config, CancellationToken cancellationToken)
        {
            var directory = ConfigDirectory;
            EnsureDirectoryExists(directory);

            var path = Path.Combine(directory, ConfigFile);
            var json = JsonUtility.ToJson(config);
            return File.WriteAllTextAsync(path, json, cancellationToken);
        }

        private static void EnsureDirectoryExists(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
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
