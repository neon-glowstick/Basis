using UnityEngine;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Analytics.IAnalytic;
namespace BattlePhaze.SettingsManager.Intergrations
{
    public class SMModuleQualityAndQualitySetURP : SettingsManagerOption
    {
        public UniversalAdditionalCameraData Data;
        public override void ReceiveOption(SettingsMenuInput Option, SettingsManager Manager)
        {
            if (NameReturn(0, Option))
            {
                ChangeQualityLevel(Option.SelectedValue);
            }
        }
        public AnisotropicFiltering VeryLow = AnisotropicFiltering.Disable;
        public AnisotropicFiltering low = AnisotropicFiltering.Disable;
        public AnisotropicFiltering medium = AnisotropicFiltering.Enable;
        public AnisotropicFiltering high = AnisotropicFiltering.Enable;
        public AnisotropicFiltering ultra = AnisotropicFiltering.Enable;
        public Camera Camera;
        public void ChangeQualityLevel(string Quality)
        {
            if (Camera == null)
            {
                Camera = Camera.main;
                Data = Camera.GetComponent<UniversalAdditionalCameraData>();
            }
            switch (Quality)
            {
                case "very low":
                    QualitySettings.anisotropicFiltering = VeryLow;
                    QualitySettings.realtimeReflectionProbes = false;
                    QualitySettings.softParticles = false;
                    QualitySettings.particleRaycastBudget = 256;
                    QualitySettings.SetQualityLevel(QualitySettings.GetQualityLevel(), true);
                    if (Data != null)
                    {
                        Data.renderPostProcessing = false;
                        Data.requiresColorOption = CameraOverrideOption.Off;
                        Data.requiresDepthOption = CameraOverrideOption.Off;
                        Data.renderShadows = false;
                        Data.stopNaN = false;
                    }
                    break;
                case "low":
                    QualitySettings.anisotropicFiltering = low;
                    QualitySettings.realtimeReflectionProbes = true;
                    QualitySettings.softParticles = true;
                    QualitySettings.particleRaycastBudget = 512;
                    QualitySettings.SetQualityLevel(QualitySettings.GetQualityLevel(), true);
                    if (Data != null)
                    {
                        Data.renderPostProcessing = true;
                        Data.requiresColorOption = CameraOverrideOption.UsePipelineSettings;
                        Data.requiresDepthOption = CameraOverrideOption.UsePipelineSettings;
                        Data.renderShadows = true;
                        Data.stopNaN = true;
                    }
                    break;
                case "medium":
                    QualitySettings.anisotropicFiltering = medium;
                    QualitySettings.realtimeReflectionProbes = true;
                    QualitySettings.softParticles = true;
                    QualitySettings.particleRaycastBudget = 1024;
                    QualitySettings.SetQualityLevel(QualitySettings.GetQualityLevel(), true);
                    if (Data != null)
                    {
                        Data.renderPostProcessing = true;
                        Data.requiresColorOption = CameraOverrideOption.UsePipelineSettings;
                        Data.requiresDepthOption = CameraOverrideOption.UsePipelineSettings;
                        Data.renderShadows = true;
                        Data.stopNaN = true;
                    }
                    break;
                case "high":
                    QualitySettings.anisotropicFiltering = high;
                    QualitySettings.realtimeReflectionProbes = true;
                    QualitySettings.softParticles = true;
                    QualitySettings.particleRaycastBudget = 2048;
                    QualitySettings.SetQualityLevel(QualitySettings.GetQualityLevel(), true);
                    if (Data != null)
                    {
                        Data.renderPostProcessing = true;
                        Data.requiresColorOption = CameraOverrideOption.UsePipelineSettings;
                        Data.requiresDepthOption = CameraOverrideOption.UsePipelineSettings;
                        Data.renderShadows = true;
                        Data.stopNaN = true;
                    }
                    break;
                case "ultra":
                    QualitySettings.anisotropicFiltering = ultra;
                    QualitySettings.realtimeReflectionProbes = true;
                    QualitySettings.softParticles = true;
                    QualitySettings.particleRaycastBudget = 4096;
                    QualitySettings.SetQualityLevel(QualitySettings.GetQualityLevel(), true);
                    if (Data != null)
                    {
                        Data.renderPostProcessing = true;
                        Data.requiresColorOption = CameraOverrideOption.UsePipelineSettings;
                        Data.requiresDepthOption = CameraOverrideOption.UsePipelineSettings;
                        Data.renderShadows = true;
                        Data.stopNaN = true;
                    }
                    break;
            }
        }
    }
}
