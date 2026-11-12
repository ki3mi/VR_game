using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AndesSim.Editor
{
    /// <summary>Adds the guided layer without rebuilding any environment objects.</summary>
    public static class NarrativeSetup
    {
        [MenuItem("Andes-Sim/Narrativa/Integrar recorrido guiado")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Integra el recorrido fuera de Play Mode.");
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != DataCenterSceneBuilder.ScenePath)
                throw new InvalidOperationException("Abre Assets/Scenes/DataCenter_VR.unity.");
            var roots = scene.GetRootGameObjects();
            var game = roots.SelectMany(root => root.GetComponentsInChildren<GameManager>(true)).Single();
            var explorer = roots.SelectMany(root => root.GetComponentsInChildren<ExplorationController>(true)).Single();
            if (game.data == null || roots.SelectMany(root => root.GetComponentsInChildren<DefenseControl>(true)).Count() != 3)
                throw new InvalidOperationException("El entrenamiento requiere la base de ataque y los tres controles.");
            var mentor = game.GetComponent<MentorPresenter>();
            if (mentor == null) mentor = Undo.AddComponent<MentorPresenter>(game.gameObject);
            var director = game.GetComponent<TrainingDirector>();
            if (director == null) director = Undo.AddComponent<TrainingDirector>(game.gameObject);
            Undo.RecordObject(director, "Conectar entrenamiento guiado");
            director.game = game;
            director.exploration = explorer;
            director.mentor = mentor;
            director.transmissionSeconds = 8;
            director.enabled = true;
            foreach (var target in roots.SelectMany(root => root.GetComponentsInChildren<ExplorationTarget>(true)))
            {
                if (target.kind != ExplorationTarget.Kind.Inspection) continue;
                Undo.RecordObject(target, "Actualizar fichas del laboratorio");
                switch (target.uniqueId)
                {
                    case "terminal_swift":
                        target.body = "Terminal de órdenes financieras. Investiga los accesos, consulta los envíos y verifica los eventos de seguridad con Elliot Alderson a través del chat del teléfono."; break;
                    case "switch_barato":
                        target.body = "Switch de la red local. Conserva su identificación como contexto de infraestructura; su apariencia no demuestra una intrusión."; break;
                    case "impresora_apagada":
                        target.body = "Impresora de confirmaciones apagada. Contrasta la ausencia de papel con la cola de órdenes enviadas."; break;
                    case "panel_controles":
                        target.body = "Prepara MFA, política de horario con bloqueo y SIEM con integridad y acceso restringido antes de repetir el intento."; break;
                    case "rack_01":
                        target.body = "Los racks alojan los servidores del centro de datos. Sus indicadores muestran actividad; para atribuir una intrusión se necesitan registros y otras evidencias."; break;
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
            foreach (var label in roots.SelectMany(root => root.GetComponentsInChildren<TextMeshPro>(true)))
            {
                string content = label.name switch
                {
                    "Lobby_Etapa" => "ANDES-SIM\nENTRENAMIENTO GUIADO",
                    "Lobby_Instruccion" => "UNIDAD DE CIBERSEGURIDAD\nENTRENAMIENTO OPERATIVO",
                    "Acceder" => "ACCESO / CENTRO DE DATOS",
                    "Estado_SWIFT" => "REGISTRO DE ACCESOS\nTERMINAL DE ENTRENAMIENTO",
                    _ => null
                };
                if (content == null) continue;
                Undo.RecordObject(label, "Actualizar señal del entrenamiento");
                label.text = content;
                PrefabUtility.RecordPrefabInstancePropertyModifications(label);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[ANDES-SIM] Recorrido narrativo integrado en DataCenter_VR. Mentor: Elliot Alderson. Chat de teléfono y tres consultas internas.");
        }
    }
}
