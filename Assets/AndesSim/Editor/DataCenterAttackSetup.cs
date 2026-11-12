using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AndesSim.Editor
{
    public static class DataCenterAttackSetup
    {
        const string DataFolder = "Assets/AndesSim/Data";
        const string DataPath = DataFolder + "/DataCenterAttack.asset";

        [MenuItem("Andes-Sim/Etapa 2/Integrar modo ataque")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Sal de Play Mode antes de integrar el modo ataque.");

            var scene = SceneManager.GetActiveScene();
            if (scene.path != DataCenterSceneBuilder.ScenePath)
                throw new InvalidOperationException("Abre DataCenter_VR antes de integrar el modo ataque.");

            var roots = scene.GetRootGameObjects();
            var exploration = roots.SelectMany(root => root.GetComponentsInChildren<ExplorationController>(true)).Single();
            var targets = roots.SelectMany(root => root.GetComponentsInChildren<ExplorationTarget>(true)).ToArray();
            var terminal = targets.Single(target => target.uniqueId == "terminal_swift");
            var printer = targets.Single(target => target.uniqueId == ScenarioData.PrinterEvidenceId);
            var network = targets.Single(target => target.uniqueId == ScenarioData.SwitchEvidenceId);
            var rack = targets.Single(target => target.uniqueId == "rack_01");

            var scenarioData = AssetDatabase.LoadAssetAtPath<ScenarioData>(DataPath);
            if (scenarioData == null)
            {
                if (!AssetDatabase.IsValidFolder(DataFolder)) AssetDatabase.CreateFolder("Assets/AndesSim", "Data");
                scenarioData = ScriptableObject.CreateInstance<ScenarioData>();
                AssetDatabase.CreateAsset(scenarioData, DataPath);
            }

            var game = roots.SelectMany(root => root.GetComponentsInChildren<GameManager>(true)).SingleOrDefault();
            if (game == null)
            {
                var managerObject = new GameObject("Reconstrucción SWIFT");
                Undo.RegisterCreatedObjectUndo(managerObject, "Integrar modo ataque");
                var logicRoot = roots.FirstOrDefault(root => root.name == "06_Logica_Etapas_Posteriores");
                if (logicRoot != null) Undo.SetTransformParent(managerObject.transform, logicRoot.transform, "Organizar modo ataque");
                game = Undo.AddComponent<GameManager>(managerObject);
            }

            Undo.RecordObject(game, "Configurar modo ataque");
            if (game.data == null) game.data = scenarioData;
            game.mode = SimMode.Ataque;
            var audio = GetOrAdd<AudioManager>(game.gameObject);
            var ui = GetOrAdd<UIManager>(game.gameObject);
            Undo.RecordObject(ui, "Conectar interfaz de ataque");
            ui.exploration = exploration;
            ui.game = game;
            ui.audioManager = audio;

            GetOrAdd<LogViewer>(terminal.gameObject);
            SetEvidence(terminal.gameObject, LogViewer.EvidenceId);
            SetEvidence(printer.gameObject, ScenarioData.PrinterEvidenceId);
            SetEvidence(network.gameObject, ScenarioData.SwitchEvidenceId);

            SetBody(terminal, "La terminal SWIFT conserva el registro de accesos de la noche. Desde aquí puedes iniciar la reconstrucción y revisar el log.");
            SetBody(printer, "La impresora de confirmaciones permanece apagada. Durante la sesión no se imprimieron comprobantes de las órdenes.");
            SetBody(network, "Este switch conecta equipos de la red local. Su presencia ayuda a identificar la infraestructura disponible durante el acceso a la terminal.");
            SetBody(rack, "Los racks alojan los servidores del centro de datos. Sus indicadores permiten reconocer la actividad de los equipos durante la reconstrucción.");
            var terminalScreen = terminal.transform.Find("Estado_SWIFT");
            if (terminalScreen != null && terminalScreen.TryGetComponent(out TextMeshPro screen))
            {
                Undo.RecordObject(screen, "Actualizar pantalla SWIFT");
                screen.text = "REGISTRO DE ACCESOS\n\nMIRA Y PULSA ESPACIO";
                PrefabUtility.RecordPrefabInstancePropertyModifications(screen);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = game.gameObject;
            Debug.Log("[ANDES-SIM] Modo ataque integrado en DataCenter_VR.");
        }

        static T GetOrAdd<T>(GameObject target) where T : Component =>
            target.TryGetComponent(out T component) ? component : Undo.AddComponent<T>(target);

        static void SetEvidence(GameObject target, string id)
        {
            var evidence = GetOrAdd<EvidenceItem>(target);
            Undo.RecordObject(evidence, "Configurar evidencia");
            evidence.evidenceId = id;
        }

        static void SetBody(ExplorationTarget target, string body)
        {
            Undo.RecordObject(target, "Actualizar ficha de inspección");
            target.body = body;
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
