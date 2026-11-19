using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AndesSim.Editor
{
    [InitializeOnLoad]
    public static class AndroidDelivery
    {
        const string Pending="Andes.Android.BuildPending";
        static AndroidDelivery() { EditorApplication.update+=ScheduleBuild; EditorApplication.update+=FinalizeReport; }
        static void FinalizeReport()
        {
            if(!SessionState.GetBool("Andes.Android.ReportPending",false) || BuildPipeline.isBuildingPlayer || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            var report=BuildReport.GetLatestReport();
            if(report==null || report.summary.result==UnityEditor.Build.Reporting.BuildResult.Unknown) return;
            if(!report.summary.outputPath.Replace('\\','/').EndsWith("Build/ANDES-SIM.apk")) return;
            WriteReport(report);
        }
        static void WriteReport(BuildReport report)
        {
            File.WriteAllText("Build/BuildResult.json",JsonUtility.ToJson(new BuildResult { result=report.summary.result.ToString(), errors=report.summary.totalErrors, warnings=report.summary.totalWarnings, bytes=report.summary.totalSize, path=Path.GetFullPath("Build/ANDES-SIM.apk") },true));
            SessionState.SetBool("Andes.Android.ReportPending",false);
        }
        static void ScheduleBuild()
        {
            if(!SessionState.GetBool(Pending,false) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android) return;
            ContinueBuild();
        }
        [MenuItem("Andes-Sim/Android/Preparar y generar APK")]
        public static void Request()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Sal de Play antes de compilar.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/Scenes/DataCenter_VR.unity") throw new InvalidOperationException("Abre DataCenter_VR.");
            var game=UnityEngine.Object.FindAnyObjectByType<GameManager>();
            if(game.GetComponent<MobileExperience>()==null) Undo.AddComponent<MobileExperience>(game.gameObject);
            foreach(var sim in UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRInteractionSimulator>()) { Undo.RecordObject(sim.gameObject,"Desactivar movimiento libre"); sim.gameObject.SetActive(false); }
            var explorer=UnityEngine.Object.FindAnyObjectByType<ExplorationController>(); explorer.simulatorInstructions="Arrastra sobre la sala o usa el giroscopio para mirar.\nToca los equipos y destinos de teletransporte.";
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            PlayerSettings.companyName="AndesSim"; PlayerSettings.productName="ANDES-SIM";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.andessim.datacenter");
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});
            var xr=UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if(xr!=null)
            {
                xr.InitManagerOnStart=false;
                if(xr.Manager!=null) foreach(var loader in xr.Manager.activeLoaders.ToArray()) xr.Manager.TryRemoveLoader(loader);
                EditorUtility.SetDirty(xr);
                if(xr.Manager!=null) EditorUtility.SetDirty(xr.Manager);
            }
            EditorUserBuildSettings.buildAppBundle=false;
            SessionState.SetBool(Pending,true);
            AssetDatabase.SaveAssets();
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android) EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Android,BuildTarget.Android);
            // The update callback runs the build outside the MCP/PlayerLoop call.
        }
        static void ContinueBuild()
        {
            if(!SessionState.GetBool(Pending,false)) return;
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android) return;
            SessionState.SetBool(Pending,false);
            Directory.CreateDirectory("Build");
            SessionState.SetBool("Andes.Android.ReportPending",true);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{"Assets/Scenes/DataCenter_VR.unity"}, locationPathName="Build/ANDES-SIM.apk", target=BuildTarget.Android, options=BuildOptions.None });
            WriteReport(report);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) Debug.LogError("[ANDES] Falló el APK. Revisa Build/BuildResult.json y consola.");
        }
        [Serializable] class BuildResult { public string result,path; public int errors,warnings; public ulong bytes; }
    }
}
