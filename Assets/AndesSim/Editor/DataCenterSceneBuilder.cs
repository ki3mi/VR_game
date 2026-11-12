using System;
using System.Collections.Generic;
using System.IO;
using AndesSim;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace AndesSim.Editor
{
    /// <summary>Authors editable scene objects and reusable prefabs for the first milestone.</summary>
    public static class DataCenterSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/DataCenter_VR.unity";
        const string Root = "Assets/AndesSim";
        const string SimulatorPath = "Assets/Samples/XR Interaction Toolkit/3.6.0/XR Interaction Simulator/XR Interaction Simulator.prefab";
        static Material dark, metal, floor, wall, teal, cyan, amber, white, screen;
        static TMP_FontAsset font;
        static int interactiveLayer;

        [MenuItem("Andes-Sim/Etapa 1/Crear centro de datos")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Sal de Play Mode antes de crear la escena.");
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                Debug.Log("[ANDES-SIM] Escena existente abierta. Se conservan sus cambios.");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPath) == null)
                throw new InvalidOperationException("Falta el prefab XR Interaction Simulator 3.6.0.");

            Directory.CreateDirectory(Root + "/Art/Materials");
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory("Assets/Scenes");
            AssetDatabase.Refresh();
            EnsureLayer();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null) font = TMP_Settings.defaultFontAsset;
            dark = Mat("Obsidian", new Color(.025f, .044f, .061f));
            metal = Mat("ServerMetal", new Color(.09f, .13f, .16f), .65f);
            floor = Mat("RaisedFloor", new Color(.12f, .17f, .20f), .3f);
            wall = Mat("ArchitecturalSlate", new Color(.17f, .23f, .27f), .25f);
            teal = Mat("SignalTeal", new Color(.015f, .46f, .35f), .1f, new Color(.02f, .62f, .43f));
            cyan = Mat("SignalCyan", new Color(.08f, .65f, .78f), .1f, new Color(.05f, .8f, 1f));
            amber = Mat("SignalAmber", new Color(.9f, .5f, .10f), 0f, new Color(1f, .42f, .035f));
            white = Mat("Frost", new Color(.66f, .75f, .78f), .2f);
            screen = Mat("Display", new Color(.018f, .065f, .075f), .05f, new Color(.015f, .09f, .105f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "DataCenter_VR";
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.28f, .36f, .43f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.fog = false;
            RenderSettings.skybox = null;

            var environment = Group("01_Entorno");
            BuildArchitecture(environment);
            var props = Group("02_Equipamiento");
            BuildEquipment(props);
            var navigation = Group("03_Navegacion");
            var destinations = BuildNavigation(navigation);
            BuildPortal(environment, destinations["TP_Racks"]);
            BuildLighting(Group("04_Iluminacion"));
            BuildRig(Group("05_Entrada_XR"));
            Group("06_Logica_Etapas_Posteriores");

            var ambient = new GameObject("Ambiente_Servidores");
            ambient.transform.SetParent(environment);
            ambient.AddComponent<ServerAmbience>();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (var entry in EditorBuildSettings.scenes)
                if (entry.path != ScenePath) scenes.Add(entry);
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = GameObject.Find("Terminal_SWIFT");
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.LookAt(new Vector3(0, 1.4f, 3), Quaternion.Euler(18, -18, 0), 14f);
            Debug.Log("[ANDES-SIM] Etapa 1 creada: 4 racks, 6 destinos, portal, 5 inspecciones y XR Interaction Simulator.");
        }

        static void EnsureLayer()
        {
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tags.FindProperty("layers");
            interactiveLayer = LayerMask.NameToLayer("Interactive");
            if (interactiveLayer < 0)
            {
                for (var i = 8; i < 32; i++)
                {
                    if (!string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) continue;
                    layers.GetArrayElementAtIndex(i).stringValue = "Interactive";
                    interactiveLayer = i;
                    tags.ApplyModifiedProperties();
                    break;
                }
            }
            if (interactiveLayer < 0) throw new InvalidOperationException("No hay capa disponible para Interactive.");
        }

        static Material Mat(string name, Color color, float metallic = 0, Color? emission = null)
        {
            var path = Root + "/Art/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", .38f);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Transform Group(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static TextMeshPro Label(string name, Transform parent, string text, Vector3 position, float size = 2.4f, float width = 3, Color? color = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var label = go.AddComponent<TextMeshPro>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color ?? new Color(.79f, .94f, .94f);
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(width, 1f);
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        static void BuildArchitecture(Transform parent)
        {
            Box("Suelo_Con_Colision", parent, new Vector3(0, -.14f, 1), new Vector3(12.4f, .25f, 22.4f), dark, true);
            for (var x = -6; x < 6; x++)
                for (var z = -10; z < 12; z++)
                    Box("Baldosa_" + x + "_" + z, parent, new Vector3(x + .5f, -.006f, z + .5f), new Vector3(.97f, .015f, .97f), floor);
            Box("Pared_Izquierda", parent, new Vector3(-6.1f, 2, 1), new Vector3(.2f, 4.1f, 22.4f), wall, true);
            Box("Pared_Derecha", parent, new Vector3(6.1f, 2, 1), new Vector3(.2f, 4.1f, 22.4f), wall, true);
            Box("Pared_Fondo", parent, new Vector3(0, 2, 12.1f), new Vector3(12.4f, 4.1f, .2f), wall, true);
            Box("Pared_Lobby", parent, new Vector3(0, 2, -10.1f), new Vector3(12.4f, 4.1f, .2f), dark, true);
            Box("Techo", parent, new Vector3(0, 4.15f, 1), new Vector3(12.4f, .2f, 22.4f), dark, true);
            for (var z = -9; z < 12; z += 3)
            {
                Box("Viga_Techo", parent, new Vector3(0, 3.91f, z), new Vector3(12f, .24f, .18f), metal);
                Box("Luminaria_Central", parent, new Vector3(0, 3.94f, z + .65f), new Vector3(.08f, .035f, 1.8f), cyan);
                Box("Banda_Izquierda", parent, new Vector3(-5.96f, .25f, z), new Vector3(.025f, .035f, 2.85f), teal);
                Box("Banda_Derecha", parent, new Vector3(5.96f, .25f, z), new Vector3(.025f, .035f, 2.85f), teal);
            }
            for (var x = -2; x <= 2; x += 4)
                Box("Guia_Pasillo", parent, new Vector3(x, .012f, 2.9f), new Vector3(.035f, .012f, 16f), teal);
            for (var z = -4; z <= 8; z += 2)
                Box("Marca_Ruta", parent, new Vector3(0, .015f, z), new Vector3(.24f, .01f, .45f), metal);
            Box("Frente_Puerta_Izquierda", parent, new Vector3(-4, 2, -5.5f), new Vector3(4, 4.1f, .22f), dark, true);
            Box("Frente_Puerta_Derecha", parent, new Vector3(4, 2, -5.5f), new Vector3(4, 4.1f, .22f), dark, true);
            Box("Dintel", parent, new Vector3(0, 3.66f, -5.5f), new Vector3(4, .8f, .3f), dark, true);
            Label("Nombre_Entrada", parent, "BANCO ANDINO", new Vector3(0, 3.65f, -5.69f), 4.1f, 4);
            Label("Subtitulo_Entrada", parent, "CENTRO DE DATOS  /  ACCESO 03", new Vector3(0, 3.22f, -5.71f), 1.7f, 4, new Color(.13f, .91f, .71f));
            Label("Senal_Terminal", parent, "01   /   OPERACIONES SWIFT", new Vector3(0, 3.3f, 11.96f), 3.2f, 6);
            Label("Senal_Seguridad", parent, "02   /   CONTROLES", new Vector3(3.7f, 2.9f, 11.94f), 1.75f, 3);
            Label("Lobby_Instruccion", parent, "UNIDAD DE RESPUESTA\nExplora. Inspecciona. Comprende.", new Vector3(-3.9f, 1.85f, -5.66f), 2.4f, 3.5f);
            Label("Lobby_Etapa", parent, "ANDES-SIM\nETAPA 01  /  EXPLORACION", new Vector3(3.9f, 1.85f, -5.66f), 2.4f, 3.5f);
            Box("Panel_Marca_Fondo", parent, new Vector3(-3.9f, 2.1f, 11.94f), new Vector3(3, 1.5f, .05f), dark);
            Label("Marca_Fondo", parent, "ANDES-SIM\nINFRAESTRUCTURA CRITICA\nSIMULACION EDUCATIVA", new Vector3(-3.9f, 2.1f, 11.89f), 2.4f, 2.8f);
        }

        static void BuildEquipment(Transform parent)
        {
            var prototype = Group("Rack_Unit");
            Box("Carcasa", prototype, new Vector3(0, 1.35f, 0), new Vector3(1.2f, 2.7f, 1.15f), dark);
            Box("Puerta", prototype, new Vector3(0, 1.35f, -.59f), new Vector3(1.03f, 2.45f, .06f), metal);
            for (var row = 0; row < 10; row++)
            {
                var y = .31f + row * .225f;
                Box("Servidor_" + row, prototype, new Vector3(0, y, -.637f), new Vector3(.9f, .19f, .045f), dark);
                for (var slot = 0; slot < 4; slot++)
                    Box("Bandeja_Disco", prototype, new Vector3(-.29f + slot * .18f, y, -.667f), new Vector3(.14f, .12f, .02f), metal);
                Box("LED_Estado", prototype, new Vector3(.39f, y, -.681f), new Vector3(.035f, .025f, .012f), row % 3 == 0 ? cyan : teal);
            }
            Box("Borde_Luz_I", prototype, new Vector3(-.55f, 1.35f, -.64f), new Vector3(.018f, 2.3f, .02f), teal);
            Box("Borde_Luz_D", prototype, new Vector3(.55f, 1.35f, -.64f), new Vector3(.018f, 2.3f, .02f), teal);
            var rackPrefab = PrefabUtility.SaveAsPrefabAsset(prototype.gameObject, Root + "/Prefabs/Rack_Unit.prefab");
            UnityEngine.Object.DestroyImmediate(prototype.gameObject);
            for (var i = 0; i < 4; i++)
            {
                var rack = (GameObject)PrefabUtility.InstantiatePrefab(rackPrefab, parent);
                rack.name = "Rack_0" + (i + 1);
                rack.transform.localPosition = new Vector3(i < 2 ? -3.8f : 3.8f, 0, i % 2 == 0 ? .5f : 3.6f);
                rack.transform.localRotation = Quaternion.Euler(0, i < 2 ? -90 : 90, 0);
                var leds = new List<Renderer>();
                foreach (var r in rack.GetComponentsInChildren<Renderer>()) if (r.name == "LED_Estado") leds.Add(r);
                var blink = rack.AddComponent<LedBlink>();
                blink.leds = leds.ToArray();
                Label("Etiqueta_Rack", rack.transform, "RACK 0" + (i + 1), new Vector3(0, 2.9f, -.63f), 1.9f, 1.9f);
                if (i == 0)
                    Inspect(rack, "rack_01", "RACK 01 / SERVIDORES", "Los racks alojan los equipos del centro de datos. Sus indicadores permiten reconocer actividad.\n\nEn esta etapa puedes explorar la infraestructura; la reconstruccion del ataque se incorporara despues.", new Vector3(0, 1.35f, 0), new Vector3(1.25f, 2.7f, 1.3f), new Vector3(0, 1.55f, -.71f));
                else
                {
                    var rackCollider = rack.AddComponent<BoxCollider>();
                    rackCollider.center = new Vector3(0, 1.35f, 0);
                    rackCollider.size = new Vector3(1.25f, 2.7f, 1.3f);
                }
            }

            var terminal = Group("Terminal_SWIFT", parent);
            terminal.localPosition = new Vector3(0, 0, 9.5f);
            Box("Mesa", terminal, new Vector3(0, .85f, 0), new Vector3(2.5f, .12f, 1.2f), metal);
            Box("Soporte_Mesa_I", terminal, new Vector3(-.9f, .4f, 0), new Vector3(.16f, .8f, .8f), dark);
            Box("Soporte_Mesa_D", terminal, new Vector3(.9f, .4f, 0), new Vector3(.16f, .8f, .8f), dark);
            Box("Monitor", terminal, new Vector3(0, 1.65f, .18f), new Vector3(1.9f, 1.1f, .14f), dark);
            Box("Pantalla", terminal, new Vector3(0, 1.65f, .095f), new Vector3(1.75f, .94f, .015f), screen);
            Box("Pie_Monitor", terminal, new Vector3(0, 1.1f, .2f), new Vector3(.15f, .45f, .15f), metal);
            Box("Teclado", terminal, new Vector3(0, .94f, -.29f), new Vector3(.95f, .05f, .3f), dark);
            Label("Titulo_SWIFT", terminal, "SWIFT / TERMINAL 03", new Vector3(0, 1.9f, .075f), 1.55f, 1.65f);
            Label("Estado_SWIFT", terminal, "REGISTRO DE ACCESOS\n\n[ INSPECCIONAR TERMINAL ]", new Vector3(0, 1.55f, .07f), 1.15f, 1.6f, new Color(.2f, 1f, .72f));
            Inspect(terminal.gameObject, "terminal_swift", "TERMINAL SWIFT", "Esta terminal transmite ordenes de pago internacionales.\n\nAqui se incorporara el visor de registros: permitira revisar los accesos y detectar huecos en el log durante la etapa 2.", new Vector3(0, 1.2f, 0), new Vector3(2.5f, 2.4f, 1.2f), new Vector3(0, 1.65f, -.1f));
            SaveProp(terminal);

            var network = Group("Switch_Barato", parent);
            network.localPosition = new Vector3(-5, 0, 7);
            network.localRotation = Quaternion.Euler(0, -90, 0);
            Box("Soporte", network, new Vector3(0, .75f, 0), new Vector3(1.5f, 1.5f, .65f), dark);
            Box("Switch", network, new Vector3(0, 1.55f, -.1f), new Vector3(1.45f, .28f, .55f), metal);
            for (var i = 0; i < 8; i++)
            {
                Box("Puerto_RJ45_" + i, network, new Vector3(-.56f + i * .16f, 1.55f, -.39f), new Vector3(.10f, .09f, .035f), dark);
                Box("LED_Puerto_" + i, network, new Vector3(-.56f + i * .16f, 1.65f, -.39f), new Vector3(.025f, .02f, .015f), teal);
            }
            Label("Etiqueta_Switch", network, "RED / SWITCH", new Vector3(0, 2.1f, -.42f), 2.25f, 2);
            Inspect(network.gameObject, "switch_barato", "SWITCH / INFRAESTRUCTURA DE RED", "El switch conecta equipos del banco. La arquitectura de red y su segmentacion influyen en el alcance de una intrusion.\n\nEste objeto se convertira en evidencia del contexto de infraestructura en la siguiente etapa.", new Vector3(0, 1, 0), new Vector3(1.6f, 2f, .8f), new Vector3(0, 1.55f, -.45f));
            SaveProp(network);

            var printer = Group("Impresora", parent);
            printer.localPosition = new Vector3(5, 0, 7);
            printer.localRotation = Quaternion.Euler(0, 90, 0);
            Box("Mueble", printer, new Vector3(0, .45f, 0), new Vector3(1.4f, .9f, .9f), metal);
            Box("Cuerpo", printer, new Vector3(0, 1.12f, 0), new Vector3(1.15f, .44f, .75f), white);
            Box("Tapa", printer, new Vector3(0, 1.38f, .13f), new Vector3(.96f, .12f, .42f), dark);
            Box("Salida_Papel", printer, new Vector3(0, 1.18f, -.39f), new Vector3(.8f, .07f, .05f), dark);
            Box("Display_Apagado", printer, new Vector3(.4f, 1.28f, -.39f), new Vector3(.15f, .07f, .015f), dark);
            Label("Etiqueta_Impresora", printer, "CONFIRMACIONES", new Vector3(0, 2.2f, -.45f), 2.0f, 2.3f);
            Label("Estado_Impresora", printer, "APAGADA", new Vector3(0, 1.75f, -.45f), 1.7f, 2, new Color(1f, .67f, .28f));
            Inspect(printer.gameObject, "impresora_apagada", "IMPRESORA / APAGADA", "La impresora genera confirmaciones de las operaciones. En la reconstruccion del ataque permanece apagada.\n\nInspeccionarla permite reconocer una anomalia que sera registrada como evidencia en la etapa 2.", new Vector3(0, .85f, 0), new Vector3(1.5f, 1.7f, 1f), new Vector3(0, 1.25f, -.51f));
            SaveProp(printer);

            var controls = Group("Panel_Controles", parent);
            controls.localPosition = new Vector3(3.1f, 0, 11);
            Box("Gabinete", controls, new Vector3(0, 1.5f, 0), new Vector3(2.1f, 2.3f, .35f), dark);
            Box("Fondo_Panel", controls, new Vector3(0, 1.5f, -.19f), new Vector3(1.95f, 2.12f, .04f), screen);
            Label("Titulo_Controles", controls, "CONTROLES DE SEGURIDAD", new Vector3(0, 2.27f, -.23f), 1.45f, 1.9f);
            var titles = new[] { "MFA", "FUERA DE HORARIO", "SIEM" };
            for (var i = 0; i < 3; i++)
            {
                var y = 1.9f - i * .5f;
                Box("Interruptor_" + i, controls, new Vector3(-.65f, y, -.26f), new Vector3(.25f, .16f, .08f), metal);
                Box("Testigo_" + i, controls, new Vector3(-.7f, y, -.31f), new Vector3(.08f, .11f, .02f), amber);
                Label("Nombre_Control_" + i, controls, titles[i], new Vector3(.15f, y, -.26f), 1.2f, 1.25f);
            }
            Inspect(controls.gameObject, "panel_controles", "CONTROLES / DEFENSA", "MFA: autenticacion multifactor.\n\nFUERA DE HORARIO: alerta y bloqueo de accesos nocturnos.\n\nSIEM: centralizacion y proteccion de registros.\n\nLos interruptores se habilitaran en la etapa 3.", new Vector3(0, 1.5f, 0), new Vector3(2.1f, 2.3f, .45f), new Vector3(0, 1.65f, -.29f));
            SaveProp(controls);
        }

        static ExplorationTarget Inspect(GameObject go, string id, string title, string body, Vector3 center, Vector3 size, Vector3 aim)
        {
            var collider = go.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
            var target = go.AddComponent<ExplorationTarget>();
            target.kind = ExplorationTarget.Kind.Inspection;
            target.uniqueId = id;
            target.title = title;
            target.body = body;
            var aimTransform = Group("Punto_Mirada", go.transform);
            aimTransform.localPosition = aim;
            target.aimPoint = aimTransform;
            target.highlightRenderers = go.GetComponentsInChildren<MeshRenderer>();
            go.layer = interactiveLayer;
            return target;
        }

        static void SaveProp(Transform prop)
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(prop.gameObject, Root + "/Prefabs/" + prop.name + ".prefab", InteractionMode.AutomatedAction);
        }

        static Dictionary<string, Transform> BuildNavigation(Transform parent)
        {
            var result = new Dictionary<string, Transform>();
            string[] names = { "TP_Entrada", "TP_Racks", "TP_Terminal", "TP_Switch", "TP_Impresora", "TP_Controles" };
            string[] labels = { "ENTRADA", "PASILLO DE RACKS", "TERMINAL SWIFT", "RED / SWITCH", "IMPRESORA", "CONTROLES" };
            Vector3[] points = { new Vector3(0, 0, -8), new Vector3(0, 0, 1), new Vector3(0, 0, 6.4f), new Vector3(-2.5f, 0, 6.7f), new Vector3(2.6f, 0, 6.4f), new Vector3(3.1f, 0, 9.1f) };
            float[] angles = { 0, 0, 0, -90, 90, 0 };
            for (var i = 0; i < names.Length; i++)
            {
                var root = Group(names[i], parent);
                root.localPosition = points[i];
                var visual = Group("Baliza", root);
                Cylinder("Anillo", visual, new Vector3(0, .025f, 0), new Vector3(1.15f, .02f, 1.15f), cyan);
                Cylinder("Base", visual, new Vector3(0, .052f, 0), new Vector3(1.02f, .009f, 1.02f), dark);
                var marker = Box("Indicador", visual, new Vector3(0, .08f, .19f), new Vector3(.18f, .025f, .3f), teal);
                var name = Label("Nombre_Destino", root, (i + 1).ToString("00") + "  /  " + labels[i], new Vector3(0, .38f, 0), 1.55f, 2.7f);
                // Labels face the entrance; destination heading is stored separately.
                var destination = Group("Destino", root);
                destination.localRotation = Quaternion.Euler(0, angles[i], 0);
                var aim = Group("Punto_Mirada", root);
                aim.localPosition = new Vector3(0, .12f, 0);
                var collider = root.gameObject.AddComponent<BoxCollider>();
                collider.center = new Vector3(0, .08f, 0);
                collider.size = new Vector3(1.2f, .2f, 1.2f);
                var target = root.gameObject.AddComponent<ExplorationTarget>();
                target.kind = ExplorationTarget.Kind.Teleport;
                target.uniqueId = names[i];
                target.title = labels[i];
                target.body = "Selecciona para teletransportarte a esta estacion.";
                target.destination = destination;
                target.aimPoint = aim;
                target.highlightRenderers = new[] { marker.GetComponent<Renderer>() };
                root.gameObject.layer = interactiveLayer;
                result[names[i]] = destination;
                SaveProp(root);
            }
            return result;
        }

        static void BuildPortal(Transform parent, Transform destination)
        {
            var portal = Group("Portal_Entrada", parent);
            portal.localPosition = new Vector3(0, 0, -5.5f);
            var left = Box("Marco_Luz_I", portal, new Vector3(-1.85f, 1.55f, 0), new Vector3(.09f, 3.1f, .18f), cyan);
            var right = Box("Marco_Luz_D", portal, new Vector3(1.85f, 1.55f, 0), new Vector3(.09f, 3.1f, .18f), cyan);
            Box("Marco_Luz_Superior", portal, new Vector3(0, 3.08f, 0), new Vector3(3.8f, .07f, .18f), cyan);
            Label("Acceder", portal, "ENTRAR AL CENTRO DE DATOS\n[ MIRAR + ESPACIO ]", new Vector3(0, 1.55f, -.14f), 2.1f, 3.4f);
            var collider = portal.gameObject.AddComponent<BoxCollider>();
            // Keep the lower doorway clear for sight lines to floor destinations.
            collider.center = new Vector3(0, 1.8f, 0);
            collider.size = new Vector3(3.65f, .7f, .28f);
            collider.isTrigger = true;
            var target = portal.gameObject.AddComponent<ExplorationTarget>();
            target.kind = ExplorationTarget.Kind.Portal;
            target.uniqueId = "portal_entrada";
            target.stationId = "TP_Racks";
            target.title = "ENTRAR / CENTRO DE DATOS";
            target.destination = destination;
            var aim = Group("Punto_Mirada", portal);
            aim.localPosition = new Vector3(0, 1.65f, -.2f);
            target.aimPoint = aim;
            target.highlightRenderers = new[] { left.GetComponent<Renderer>(), right.GetComponent<Renderer>() };
            portal.gameObject.layer = interactiveLayer;
        }

        static void BuildRig(Transform parent)
        {
            var rig = Group("XR Origin (Exploration)", parent);
            rig.position = new Vector3(0, 0, -8);
            var offset = Group("Camera Offset", rig);
            offset.localPosition = Vector3.up * 1.65f;
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(offset, false);
            var camera = camGo.GetComponent<Camera>();
            camera.nearClipPlane = .05f;
            camera.farClipPlane = 65f;
            camera.fieldOfView = 72f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.015f, .025f, .035f);
            var origin = rig.gameObject.AddComponent<XROrigin>();
            origin.Camera = camera;
            origin.CameraFloorOffsetObject = offset.gameObject;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
            origin.CameraYOffset = 1.65f;
            var driver = camGo.AddComponent<TrackedPoseDriver>();
            driver.positionInput = new InputActionProperty(new InputAction("Head Position", InputActionType.Value, "<XRHMD>/centerEyePosition"));
            driver.rotationInput = new InputActionProperty(new InputAction("Head Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation"));
            driver.trackingStateInput = new InputActionProperty(new InputAction("Head Tracking", InputActionType.Value, "<XRHMD>/trackingState"));
            var controller = rig.gameObject.AddComponent<ExplorationController>();
            controller.viewCamera = camera;
            controller.xrOrigin = origin;
            controller.raycastMask = ~0;

            var simulatorGo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPath), parent);
            simulatorGo.name = "XR Interaction Simulator";
            var simulator = simulatorGo.GetComponent<XRInteractionSimulator>();
            simulator.cameraTransform = camera.transform;
            simulator.translateXSpeed = .2f;
            simulator.translateYSpeed = .2f;
            simulator.translateZSpeed = .2f;
            simulator.usePointAndClick = false;
            simulator.interactionSimulatorUI = null;
            simulator.leftControllerTransform = Group("Left Controller Tracking", offset);
            simulator.rightControllerTransform = Group("Right Controller Tracking", offset);
            simulator.leftHandAimTransform = Group("Left Hand Tracking", offset);
            simulator.rightHandAimTransform = Group("Right Hand Tracking", offset);
            // The project has its own compact instructions; keep the simulator input device active.
            foreach (var canvas in simulatorGo.GetComponentsInChildren<Canvas>(true))
                canvas.gameObject.SetActive(false);
        }

        static void BuildLighting(Transform parent)
        {
            for (var z = -7; z <= 10; z += 4)
            {
                var lightGo = new GameObject("Luz_Techo_" + z);
                lightGo.transform.SetParent(parent, false);
                lightGo.transform.position = new Vector3(0, 3.5f, z);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(.65f, .85f, 1f);
                light.intensity = 2.3f;
                light.range = 8;
                light.shadows = LightShadows.None;
            }
            var fill = new GameObject("Luz_Operaciones");
            fill.transform.SetParent(parent, false);
            fill.transform.position = new Vector3(0, 2.8f, 8);
            var lamp = fill.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = new Color(.3f, 1f, .75f);
            lamp.intensity = 1.6f;
            lamp.range = 6f;
        }
    }
}
