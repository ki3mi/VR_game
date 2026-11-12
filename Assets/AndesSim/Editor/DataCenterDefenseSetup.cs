using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AndesSim.Editor
{
    public static class DataCenterDefenseSetup
    {
        const string BeaconMaterialPath = "Assets/AndesSim/Art/Materials/AlarmBeacon.mat";

        [MenuItem("Andes-Sim/Etapa 3/Integrar modo defensa")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Sal de Play Mode antes de integrar el modo defensa.");

            var scene = SceneManager.GetActiveScene();
            if (scene.path != DataCenterSceneBuilder.ScenePath)
                throw new InvalidOperationException("Abre DataCenter_VR antes de integrar el modo defensa.");

            var roots = scene.GetRootGameObjects();
            var game = roots.SelectMany(root => root.GetComponentsInChildren<GameManager>(true)).Single();
            var ui = game.GetComponent<UIManager>();
            if (ui == null || game.data == null)
                throw new InvalidOperationException("Integra primero la reconstrucción del ataque.");

            var targets = roots.SelectMany(root => root.GetComponentsInChildren<ExplorationTarget>(true)).ToArray();
            var panel = targets.Single(target => target.uniqueId == "panel_controles");
            var terminal = targets.Single(target => target.uniqueId == "terminal_swift");
            var switches = new Transform[DefenseControl.RequiredCount];
            var indicators = new Renderer[DefenseControl.RequiredCount];
            for (int i = 0; i < switches.Length; i++)
            {
                switches[i] = panel.transform.Find("Interruptor_" + i);
                indicators[i] = panel.transform.Find("Testigo_" + i)?.GetComponent<Renderer>();
                if (switches[i] == null || indicators[i] == null)
                    throw new InvalidOperationException("Falta un interruptor o un testigo del panel de seguridad.");
            }

            var ids = new[] { DefenseControl.MfaId, DefenseControl.ScheduleId, DefenseControl.SiemId };
            var controls = new DefenseControl[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                var control = GetOrAdd<DefenseControl>(switches[i].gameObject);
                Undo.RecordObject(control, "Configurar control de defensa");
                control.defenseId = ids[i];
                control.indicator = indicators[i];
                controls[i] = control;
            }

            Undo.RecordObject(panel, "Conectar panel de seguridad");
            panel.body = "Configura MFA, alerta fuera de horario y SIEM durante la preparación del modo defensa.";
            panel.highlightRenderers = panel.highlightRenderers.Where(renderer => !indicators.Contains(renderer)).ToArray();
            PrefabUtility.RecordPrefabInstancePropertyModifications(panel);
            Undo.RecordObject(ui, "Conectar controles a la interfaz");
            ui.defenseControls = controls;

            var alarmTransform = game.transform.Find("Alarma de acceso SWIFT");
            if (alarmTransform == null)
            {
                var alarmObject = new GameObject("Alarma de acceso SWIFT");
                Undo.RegisterCreatedObjectUndo(alarmObject, "Agregar alarma de acceso");
                alarmTransform = alarmObject.transform;
                Undo.SetTransformParent(alarmTransform, game.transform, "Organizar alarma de acceso");
            }
            Undo.RecordObject(alarmTransform, "Ubicar luz de alarma");
            alarmTransform.position = terminal.transform.position + new Vector3(0, 2.8f, -1.3f);
            var light = GetOrAdd<Light>(alarmTransform.gameObject);
            Undo.RecordObject(light, "Configurar luz roja de alarma");
            light.type = LightType.Point;
            light.color = new Color(1f, .06f, .025f);
            light.intensity = 5.5f;
            light.range = 9f;
            light.enabled = false;
            var alarm = GetOrAdd<AlarmSystem>(alarmTransform.gameObject);
            Undo.RecordObject(alarm, "Conectar alarma al bloqueo");
            alarm.game = game;
            alarm.redLight = light;
            alarm.redBeacon = SetUpBeacon(alarmTransform, terminal.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = panel.gameObject;
            Debug.Log("[ANDES-SIM] Modo defensa integrado en DataCenter_VR.");
        }

        static T GetOrAdd<T>(GameObject target) where T : Component =>
            target.TryGetComponent(out T component) ? component : Undo.AddComponent<T>(target);

        static Renderer SetUpBeacon(Transform alarm, Transform terminal)
        {
            var lens = alarm.Find("Baliza roja");
            if (lens == null)
            {
                var lensObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(lensObject, "Agregar baliza roja");
                lensObject.name = "Baliza roja";
                Undo.DestroyObjectImmediate(lensObject.GetComponent<Collider>());
                lens = lensObject.transform;
                Undo.SetTransformParent(lens, alarm, "Organizar baliza roja");
            }

            Undo.RecordObject(lens, "Ubicar baliza roja");
            lens.position = terminal.TransformPoint(new Vector3(.62f, 2.29f, .18f));
            lens.localScale = new Vector3(.25f, .22f, .25f);

            var material = AssetDatabase.LoadAssetAtPath<Material>(BeaconMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                material.SetColor("_BaseColor", new Color(1f, .06f, .025f));
                AssetDatabase.CreateAsset(material, BeaconMaterialPath);
            }
            var renderer = lens.GetComponent<Renderer>();
            Undo.RecordObject(renderer, "Configurar baliza roja");
            renderer.sharedMaterial = material;
            renderer.enabled = false;
            return renderer;
        }
    }
}
