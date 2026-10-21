using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace PingPong.EditorTools
{
    /// <summary>
    /// Adds "PingPong → Build Scene" to the Unity menu. It builds the whole game from primitives and
    /// procedural textures: a sports hall with barriers, bleachers and spectators, the table, paddles,
    /// ball, scoreboard, lights and camera. No external assets are needed.
    /// </summary>
    public static class PingPongSceneBuilder
    {
        const string Root = "Assets/PingPong";
        const string MatDir = Root + "/Materials";
        const string TexDir = Root + "/Textures";
        const string SceneDir = Root + "/Scenes";
        const string ScenePath = SceneDir + "/PingPong.unity";

        // Hall
        const float HallX = 9f;      // half width
        const float HallZ = 10f;     // half length
        const float HallH = 7f;
        const float CourtX = 3.6f;   // barriers around the court
        const float CourtZ = 6.2f;

        [MenuItem("PingPong/Build Scene", priority = 0)]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder(MatDir);
            EnsureFolder(TexDir);
            EnsureFolder(SceneDir);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // remove assets from earlier versions that are no longer used
            AssetDatabase.DeleteAsset(TexDir + "/Ring.asset");
            AssetDatabase.DeleteAsset(MatDir + "/AimRing.mat");

            // ---------------- textures
            Texture2D texSport = GetTex("SportFloor", 256, 256, true, SportFloorPixel);
            Texture2D texParquet = GetTex("Parquet", 512, 512, true, ParquetPixel);
            Texture2D texConcrete = GetTex("Concrete", 256, 256, true, ConcretePixel);
            Texture2D texNet = GetTex("Net", 512, 48, false, NetPixel);
            Texture2D texBlob = GetTex("Blob", 64, 64, false, BlobPixel);

            // ---------------- materials
            Material mSport = GetMat("SportFloor", new Color(1f, 1f, 1f), 0.25f, texSport, new Vector2(5f, 8f));
            Material mParquet = GetMat("Parquet", new Color(1f, 1f, 1f), 0.35f, texParquet, new Vector2(6f, 7f));
            Material mWall = GetMat("Wall", new Color(0.86f, 0.88f, 0.9f), 0.05f, texConcrete, new Vector2(6f, 3f));
            Material mWoodPanel = GetMat("WoodPanel", new Color(0.85f, 0.75f, 0.65f), 0.3f, texParquet, new Vector2(8f, 0.6f));
            Material mCeiling = GetMat("Ceiling", new Color(0.3f, 0.31f, 0.34f), 0.05f);
            Material mBeam = GetMat("Beam", new Color(0.18f, 0.19f, 0.21f), 0.4f);
            Material mWindow = GetMat("Window", new Color(0.55f, 0.7f, 0.9f), 0.9f, null, Vector2.zero, new Color(0.45f, 0.6f, 0.85f));
            Material mLightPanel = GetMat("LightPanel", Color.white, 0.5f, null, Vector2.zero, new Color(1.6f, 1.55f, 1.4f));
            Material mFrame = GetMat("Frame", new Color(0.2f, 0.21f, 0.23f), 0.3f);
            Material mBarrier = GetMat("Barrier", new Color(0.06f, 0.14f, 0.42f), 0.35f);
            Material mBarrierStripe = GetMat("BarrierStripe", new Color(0.95f, 0.95f, 0.95f), 0.3f);
            Material mBarrierAccent = GetMat("BarrierAccent", new Color(0.95f, 0.55f, 0.1f), 0.3f);
            Material mTable = GetMat("Table", new Color(0.04f, 0.22f, 0.5f), 0.4f);
            Material mTableEdge = GetMat("TableEdge", new Color(0.05f, 0.18f, 0.36f), 0.3f);
            Material mLine = GetMat("Line", Color.white, 0.3f);
            Material mMetal = GetMat("Metal", new Color(0.15f, 0.15f, 0.17f), 0.6f, null, Vector2.zero, null, 0.6f);
            Material mWood = GetMat("Wood", new Color(0.78f, 0.58f, 0.36f), 0.3f);
            Material mRubberRed = GetMat("RubberRed", new Color(0.8f, 0.08f, 0.08f), 0.25f);
            Material mRubberBlack = GetMat("RubberBlack", new Color(0.06f, 0.06f, 0.07f), 0.25f);
            Material mBall = GetMat("Ball", new Color(1f, 0.55f, 0.12f), 0.65f, null, Vector2.zero, new Color(0.35f, 0.14f, 0f));
            Material mStep = GetMat("BleacherStep", new Color(0.55f, 0.56f, 0.58f), 0.1f, texConcrete, new Vector2(4f, 1f));
            Material mSeat = GetMat("Seat", new Color(0.1f, 0.3f, 0.75f), 0.4f);
            Material mBoard = GetMat("Board", new Color(0.05f, 0.05f, 0.06f), 0.4f);
            Material mBoardRed = GetMat("BoardRed", new Color(0.6f, 0.06f, 0.06f), 0.3f);
            Material mBoardBlue = GetMat("BoardBlue", new Color(0.05f, 0.12f, 0.45f), 0.3f);
            Material mBannerRed = GetMat("BannerRed", new Color(0.75f, 0.1f, 0.12f), 0.2f);
            Material mBannerBlue = GetMat("BannerBlue", new Color(0.1f, 0.25f, 0.65f), 0.2f);
            Material mPot = GetMat("Pot", new Color(0.85f, 0.85f, 0.82f), 0.3f);
            Material mPlant = GetMat("Plant", new Color(0.16f, 0.45f, 0.2f), 0.2f);
            Material mTowel = GetMat("Towel", new Color(0.95f, 0.95f, 0.95f), 0.05f);
            Material mBottle = GetMat("Bottle", new Color(0.2f, 0.55f, 0.95f), 0.8f);

            Material mNet = GetSpriteMat("Net", texNet, new Color(0.92f, 0.92f, 0.92f, 1f));
            Material mBlob = GetSpriteMat("BallShadow", texBlob, new Color(0f, 0f, 0f, 0.55f));
            Material mTrail = GetSpriteMat("Trail", null, Color.white);

            Material[] shirts =
            {
                GetMat("Shirt0", new Color(0.8f, 0.15f, 0.15f), 0.1f), GetMat("Shirt1", new Color(0.15f, 0.35f, 0.8f), 0.1f),
                GetMat("Shirt2", new Color(0.95f, 0.8f, 0.2f), 0.1f), GetMat("Shirt3", new Color(0.2f, 0.6f, 0.3f), 0.1f),
                GetMat("Shirt4", new Color(0.9f, 0.9f, 0.9f), 0.1f), GetMat("Shirt5", new Color(0.15f, 0.15f, 0.17f), 0.1f),
                GetMat("Shirt6", new Color(0.55f, 0.25f, 0.7f), 0.1f), GetMat("Shirt7", new Color(0.95f, 0.5f, 0.15f), 0.1f),
            };
            Material[] skins =
            {
                GetMat("Skin0", new Color(0.96f, 0.8f, 0.68f), 0.2f), GetMat("Skin1", new Color(0.85f, 0.65f, 0.5f), 0.2f),
                GetMat("Skin2", new Color(0.6f, 0.42f, 0.3f), 0.2f), GetMat("Skin3", new Color(0.4f, 0.27f, 0.2f), 0.2f),
            };

            // ---------------- lighting and quality
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.65f, 0.72f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.42f, 0.4f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.18f, 0.16f);
            RenderSettings.fog = false;
            QualitySettings.shadowDistance = 25f;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowCascades = 2;
            QualitySettings.pixelLightCount = 8;
            QualitySettings.antiAliasing = 4;

            // ---------------- hall
            GameObject hall = new GameObject("Hall");
            Transform h = hall.transform;
            Prim(PrimitiveType.Cube, "Parquet Floor", h, new Vector3(0f, -0.05f, 0f), new Vector3(HallX * 2f, 0.1f, HallZ * 2f), mParquet);
            Prim(PrimitiveType.Cube, "Court Floor", h, new Vector3(0f, 0.002f, 0f), new Vector3(CourtX * 2f, 0.004f, CourtZ * 2f), mSport);

            // walls
            Prim(PrimitiveType.Cube, "Wall Front", h, new Vector3(0f, HallH * 0.5f, HallZ + 0.15f), new Vector3(HallX * 2f + 0.6f, HallH, 0.3f), mWall);
            Prim(PrimitiveType.Cube, "Wall Back", h, new Vector3(0f, HallH * 0.5f, -HallZ - 0.15f), new Vector3(HallX * 2f + 0.6f, HallH, 0.3f), mWall);
            Prim(PrimitiveType.Cube, "Wall Left", h, new Vector3(-HallX - 0.15f, HallH * 0.5f, 0f), new Vector3(0.3f, HallH, HallZ * 2f), mWall);
            Prim(PrimitiveType.Cube, "Wall Right", h, new Vector3(HallX + 0.15f, HallH * 0.5f, 0f), new Vector3(0.3f, HallH, HallZ * 2f), mWall);

            // wooden panelling at the bottom
            Prim(PrimitiveType.Cube, "Wood Panel Front", h, new Vector3(0f, 0.6f, HallZ - 0.02f), new Vector3(HallX * 2f, 1.2f, 0.06f), mWoodPanel);
            Prim(PrimitiveType.Cube, "Wood Panel Left", h, new Vector3(-HallX + 0.02f, 0.6f, 0f), new Vector3(0.06f, 1.2f, HallZ * 2f), mWoodPanel);
            Prim(PrimitiveType.Cube, "Wood Panel Right", h, new Vector3(HallX - 0.02f, 0.6f, 0f), new Vector3(0.06f, 1.2f, HallZ * 2f), mWoodPanel);

            // windows (upper band) with frames
            BuildWindowBand(h, new Vector3(0f, 3.9f, HallZ - 0.03f), Vector3.right, HallX * 2f - 1f, 1.6f, mWindow, mFrame);
            BuildWindowBand(h, new Vector3(-HallX + 0.03f, 4.6f, 0f), Vector3.forward, HallZ * 2f - 1f, 1.8f, mWindow, mFrame);
            BuildWindowBand(h, new Vector3(HallX - 0.03f, 4.6f, 0f), Vector3.forward, HallZ * 2f - 1f, 1.8f, mWindow, mFrame);

            // ceiling, beams and light panels
            Prim(PrimitiveType.Cube, "Ceiling", h, new Vector3(0f, HallH + 0.1f, 0f), new Vector3(HallX * 2f + 0.6f, 0.2f, HallZ * 2f + 0.6f), mCeiling);
            for (int i = -3; i <= 3; i++)
                Prim(PrimitiveType.Cube, "Beam", h, new Vector3(0f, HallH - 0.25f, i * 3f), new Vector3(HallX * 2f, 0.4f, 0.2f), mBeam);
            for (int ix = -1; ix <= 1; ix++)
                for (int iz = -2; iz <= 2; iz++)
                    Prim(PrimitiveType.Cube, "Light Panel", h, new Vector3(ix * 4.5f, HallH - 0.05f, iz * 3f + 1.5f), new Vector3(2.2f, 0.06f, 0.7f), mLightPanel);

            // ---------------- barriers around the court
            GameObject barriers = new GameObject("Barriers");
            BarrierLine(barriers.transform, new Vector3(-CourtX, 0f, -CourtZ), new Vector3(-CourtX, 0f, CourtZ), 6, mBarrier, mBarrierStripe, mBarrierAccent);
            BarrierLine(barriers.transform, new Vector3(CourtX, 0f, -CourtZ), new Vector3(CourtX, 0f, CourtZ), 6, mBarrier, mBarrierStripe, mBarrierAccent);
            BarrierLine(barriers.transform, new Vector3(-CourtX, 0f, CourtZ), new Vector3(CourtX, 0f, CourtZ), 4, mBarrier, mBarrierStripe, mBarrierAccent);
            BarrierLine(barriers.transform, new Vector3(-CourtX, 0f, -CourtZ), new Vector3(CourtX, 0f, -CourtZ), 4, mBarrier, mBarrierStripe, mBarrierAccent);

            // ---------------- bleachers with spectators (left)
            BuildBleachers(new Vector3(-5.1f, 0f, 2f), 12f, 4, mStep, mSeat, shirts, skins);

            // ---------------- extra props (right and back)
            GameObject props = new GameObject("Props");
            Transform pr = props.transform;
            GameObject sideTable = BuildTable("Practice Table", pr, new Vector3(6.1f, 0f, 5.2f), 90f, mTable, mTableEdge, mLine, mMetal, mNet);
            BarrierLine(pr, new Vector3(4.2f, 0f, 3.2f), new Vector3(8.4f, 0f, 3.2f), 2, mBarrier, mBarrierStripe, mBarrierAccent);
            sideTable.transform.SetParent(pr, true);

            BuildBench(pr, new Vector3(4.6f, 0f, -0.6f), mWood, mMetal, mTowel, mBottle);
            BuildPlant(pr, new Vector3(-8.2f, 0f, 9.2f), mPot, mPlant);
            BuildPlant(pr, new Vector3(8.2f, 0f, 9.2f), mPot, mPlant);
            BuildPlant(pr, new Vector3(8.2f, 0f, -2f), mPot, mPlant);

            // banners on the far wall
            Font font = GetFont();
            BuildBanner(pr, new Vector3(-5.2f, 2.1f, HallZ - 0.08f), mBannerRed, mBarrierStripe, "PING PONG", font);
            BuildBanner(pr, new Vector3(5.2f, 2.1f, HallZ - 0.08f), mBannerBlue, mBarrierStripe, "FAIR PLAY", font);

            // ---------------- main table
            BuildTable("Table", null, Vector3.zero, 0f, mTable, mTableEdge, mLine, mMetal, mNet);

            // ---------------- paddles
            float paddleZ = Table.HalfLength + 0.18f;
            PingPongPaddle player = CreatePaddle("Player Paddle", Side.Player, new Vector3(0f, 1.0f, -paddleZ), mRubberRed, mRubberBlack, mWood);
            PingPongPaddle ai = CreatePaddle("CPU Paddle", Side.AI, new Vector3(0f, 1.0f, paddleZ), mRubberRed, mRubberBlack, mWood);
            ai.controlledByAI = true;
            ai.hitSize = new Vector2(0.2f, 0.2f);


            // ---------------- ball
            GameObject ballGo = Prim(PrimitiveType.Sphere, "Ball", null, new Vector3(0f, 1.1f, -1.4f), Vector3.one, mBall);
            AudioSource audio = ballGo.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0.5f;
            PingPongBall ball = ballGo.AddComponent<PingPongBall>();
            ballGo.transform.localScale = Vector3.one * ball.radius * 2f;

            TrailRenderer trail = ballGo.AddComponent<TrailRenderer>();
            trail.time = 0.14f;
            trail.minVertexDistance = 0.01f;
            trail.widthMultiplier = ball.radius * 1.5f;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.startColor = new Color(1f, 0.85f, 0.55f, 0.6f);
            trail.endColor = new Color(1f, 0.85f, 0.55f, 0f);
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            if (mTrail != null) trail.sharedMaterial = mTrail;

            GameObject blob = Prim(PrimitiveType.Quad, "Ball Shadow", null, new Vector3(0f, Table.Height + 0.002f, -1f), Vector3.one * 0.07f, mBlob);
            blob.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Renderer blobR = blob.GetComponent<Renderer>();
            blobR.shadowCastingMode = ShadowCastingMode.Off;
            blobR.receiveShadows = false;
            ball.blobShadow = blob.transform;

            // ---------------- lights
            GameObject lights = new GameObject("Lights");
            Light sun = AddLight(lights.transform, "Main Light", LightType.Directional, new Vector3(0f, 6f, 0f), new Vector3(78f, -15f, 0f), new Color(1f, 0.96f, 0.9f), 0.55f, LightShadows.Soft);
            sun.shadowStrength = 0.7f;
            Light spotA = AddLight(lights.transform, "Spotlight 1", LightType.Spot, new Vector3(0f, 5.6f, -1.1f), new Vector3(90f, 0f, 0f), new Color(1f, 0.97f, 0.92f), 2.0f, LightShadows.Soft);
            spotA.range = 12f;
            spotA.spotAngle = 70f;
            Light spotB = AddLight(lights.transform, "Spotlight 2", LightType.Spot, new Vector3(0f, 5.6f, 1.1f), new Vector3(90f, 0f, 0f), new Color(1f, 0.97f, 0.92f), 2.0f, LightShadows.None);
            spotB.range = 12f;
            spotB.spotAngle = 70f;
            Light fill = AddLight(lights.transform, "Fill Light", LightType.Directional, new Vector3(0f, 5f, 0f), new Vector3(25f, 160f, 0f), new Color(0.75f, 0.82f, 1f), 0.25f, LightShadows.None);
            fill.name = "Fill Light";
            Light stands = AddLight(lights.transform, "Bleacher Light", LightType.Point, new Vector3(-6.5f, 4.5f, 3f), Vector3.zero, new Color(1f, 0.92f, 0.8f), 1.2f, LightShadows.None);
            stands.range = 9f;

            // ---------------- camera
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 80f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.11f, 0.14f);
            cam.allowMSAA = true;
            // The camera is high enough that your paddle doesn't block the opponent's side.
            camGo.transform.position = new Vector3(0f, 2.2f, -3.5f);
            camGo.transform.LookAt(new Vector3(0f, 0.75f, 0.35f));

            // ---------------- game manager and references
            GameObject gameGo = new GameObject("Game");
            PingPongGame game = gameGo.AddComponent<PingPongGame>();
            game.ball = ball;
            game.playerPaddle = player;
            game.aiPaddle = ai;

            ball.game = game;
            ball.paddles = new[] { player, ai };
            player.game = game;
            player.ball = ball;
            ai.game = game;
            ai.ball = ball;

            // ---------------- scoreboard next to the table
            BuildScoreboard(game, font, cam.transform.position, mBoard, mBoardRed, mBoardBlue, mMetal);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = gameGo;
            Debug.Log("PingPong: scene built and saved to " + ScenePath + ". Press Play!");
            EditorUtility.DisplayDialog("PingPong", "The scene is ready and saved to\n" + ScenePath + "\n\nPress Play (▶) and have fun!", "OK");
        }

        // =================================================================== scene parts

        static GameObject BuildTable(string name, Transform parent, Vector3 pos, float yaw, Material top, Material edge, Material line, Material metal, Material net)
        {
            GameObject root = new GameObject(name);
            Transform t = root.transform;
            float topThick = 0.03f;
            Prim(PrimitiveType.Cube, "Tabletop", t, new Vector3(0f, Table.Height - topThick * 0.5f, 0f), new Vector3(Table.Width, topThick, Table.Length), top);
            Prim(PrimitiveType.Cube, "Edge", t, new Vector3(0f, Table.Height - topThick - 0.03f, 0f), new Vector3(Table.Width - 0.12f, 0.06f, Table.Length - 0.12f), edge);

            float lineY = Table.Height + 0.001f;
            float lw = 0.02f;
            Prim(PrimitiveType.Cube, "Line Left", t, new Vector3(-Table.HalfWidth + lw * 0.5f, lineY, 0f), new Vector3(lw, 0.002f, Table.Length), line);
            Prim(PrimitiveType.Cube, "Line Right", t, new Vector3(Table.HalfWidth - lw * 0.5f, lineY, 0f), new Vector3(lw, 0.002f, Table.Length), line);
            Prim(PrimitiveType.Cube, "Line Near", t, new Vector3(0f, lineY, -Table.HalfLength + lw * 0.5f), new Vector3(Table.Width, 0.002f, lw), line);
            Prim(PrimitiveType.Cube, "Line Far", t, new Vector3(0f, lineY, Table.HalfLength - lw * 0.5f), new Vector3(Table.Width, 0.002f, lw), line);
            Prim(PrimitiveType.Cube, "Center Line", t, new Vector3(0f, lineY, 0f), new Vector3(0.004f, 0.002f, Table.Length), line);

            // legs with crossbars and wheels
            float legH = Table.Height - topThick - 0.06f;
            for (int iz = -1; iz <= 1; iz += 2)
            {
                float z = iz * (Table.HalfLength - 0.35f);
                for (int ix = -1; ix <= 1; ix += 2)
                {
                    float x = ix * (Table.HalfWidth - 0.18f);
                    Prim(PrimitiveType.Cube, "Leg", t, new Vector3(x, legH * 0.5f + 0.04f, z), new Vector3(0.05f, legH, 0.05f), metal);
                    Prim(PrimitiveType.Cylinder, "Wheel", t, new Vector3(x, 0.035f, z), new Vector3(0.07f, 0.02f, 0.07f), edge);
                }
                Prim(PrimitiveType.Cube, "Crossbar", t, new Vector3(0f, 0.22f, z), new Vector3(Table.Width - 0.36f, 0.04f, 0.04f), metal);
            }
            Prim(PrimitiveType.Cube, "Long Crossbar", t, new Vector3(0f, 0.22f, 0f), new Vector3(0.04f, 0.04f, Table.Length - 0.7f), metal);

            // net
            float netW = Table.Width + Table.NetOverhang * 2f;
            GameObject netGo = Prim(PrimitiveType.Quad, "Net", t, new Vector3(0f, Table.Height + Table.NetHeight * 0.5f, 0f), new Vector3(netW, Table.NetHeight, 1f), net);
            netGo.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
            Prim(PrimitiveType.Cube, "Net Tape", t, new Vector3(0f, Table.Height + Table.NetHeight - 0.008f, 0f), new Vector3(netW, 0.016f, 0.008f), line);
            for (int ix = -1; ix <= 1; ix += 2)
            {
                Prim(PrimitiveType.Cube, "Net Post", t, new Vector3(ix * (netW * 0.5f), Table.Height + Table.NetHeight * 0.5f, 0f), new Vector3(0.02f, Table.NetHeight + 0.02f, 0.03f), metal);
                Prim(PrimitiveType.Cube, "Net Clamp", t, new Vector3(ix * (Table.HalfWidth + 0.06f), Table.Height - 0.02f, 0f), new Vector3(0.14f, 0.04f, 0.05f), metal);
            }

            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (parent != null) root.transform.SetParent(parent, true);
            return root;
        }

        static void BarrierLine(Transform parent, Vector3 a, Vector3 b, int segments, Material body, Material stripe, Material accent)
        {
            Vector3 dir = (b - a);
            float len = dir.magnitude;
            dir /= len;
            float seg = len / segments;
            Quaternion rot = Quaternion.LookRotation(Vector3.Cross(dir, Vector3.up));
            for (int i = 0; i < segments; i++)
            {
                Vector3 c = a + dir * (seg * (i + 0.5f));
                GameObject s = new GameObject("Barrier");
                s.transform.SetParent(parent, false);
                s.transform.position = c;
                s.transform.rotation = rot;
                float w = seg - 0.05f;
                LocalPrim(PrimitiveType.Cube, "Panel", s.transform, new Vector3(0f, 0.37f, 0f), new Vector3(w, 0.66f, 0.04f), body);
                LocalPrim(PrimitiveType.Cube, "Cap", s.transform, new Vector3(0f, 0.71f, 0f), new Vector3(w, 0.03f, 0.05f), stripe);
                LocalPrim(PrimitiveType.Cube, "Stripe", s.transform, new Vector3(0f, 0.25f, 0f), new Vector3(w, 0.06f, 0.045f), i % 2 == 0 ? accent : stripe);
                LocalPrim(PrimitiveType.Cube, "Leg", s.transform, new Vector3(-w * 0.4f, 0.02f, 0f), new Vector3(0.06f, 0.04f, 0.4f), body);
                LocalPrim(PrimitiveType.Cube, "Leg", s.transform, new Vector3(w * 0.4f, 0.02f, 0f), new Vector3(0.06f, 0.04f, 0.4f), body);
            }
        }

        static void BuildBleachers(Vector3 frontCenter, float length, int rows, Material step, Material seat, Material[] shirts, Material[] skins)
        {
            GameObject root = new GameObject("Bleachers");
            Transform t = root.transform;
            System.Random rng = new System.Random(67);
            const float depth = 0.9f, rise = 0.42f;

            for (int r = 0; r < rows; r++)
            {
                float x = frontCenter.x - r * depth - depth * 0.5f;
                float top = rise * (r + 1);
                Prim(PrimitiveType.Cube, "Step", t, new Vector3(x, top * 0.5f, frontCenter.z), new Vector3(depth, top, length), step);
                Prim(PrimitiveType.Cube, "Seat", t, new Vector3(x + 0.18f, top + 0.04f, frontCenter.z), new Vector3(0.38f, 0.08f, length - 0.2f), seat);

                for (float z = -length * 0.5f + 0.4f; z < length * 0.5f - 0.3f; z += 0.62f)
                {
                    if (rng.NextDouble() < 0.4) continue;
                    float jitter = (float)(rng.NextDouble() - 0.5) * 0.12f;
                    Material shirt = shirts[rng.Next(shirts.Length)];
                    Material skin = skins[rng.Next(skins.Length)];
                    float bodyH = 0.6f + (float)rng.NextDouble() * 0.12f;
                    Vector3 basePos = new Vector3(x + 0.12f, top + 0.08f, frontCenter.z + z + jitter);
                    Prim(PrimitiveType.Capsule, "Spectator", t, basePos + new Vector3(0f, bodyH * 0.5f, 0f), new Vector3(0.4f, bodyH * 0.5f, 0.3f), shirt);
                    Prim(PrimitiveType.Sphere, "Head", t, basePos + new Vector3(0.02f, bodyH + 0.1f, 0f), Vector3.one * 0.21f, skin);
                    Prim(PrimitiveType.Cube, "Legs", t, basePos + new Vector3(0.28f, 0.05f, 0f), new Vector3(0.36f, 0.12f, 0.26f), shirts[5]);
                }
            }
        }

        static void BuildWindowBand(Transform parent, Vector3 center, Vector3 along, float length, float height, Material glass, Material frame)
        {
            bool alongX = Mathf.Abs(along.x) > 0.5f;
            Vector3 size = alongX ? new Vector3(length, height, 0.04f) : new Vector3(0.04f, height, length);
            Prim(PrimitiveType.Cube, "Windows", parent, center, size, glass);
            int mullions = Mathf.RoundToInt(length / 1.6f);
            for (int i = 0; i <= mullions; i++)
            {
                float o = -length * 0.5f + i * (length / mullions);
                Vector3 p = center + along * o;
                Vector3 s = alongX ? new Vector3(0.08f, height + 0.1f, 0.08f) : new Vector3(0.08f, height + 0.1f, 0.08f);
                Prim(PrimitiveType.Cube, "Frame", parent, p, s, frame);
            }
            Vector3 hs = alongX ? new Vector3(length + 0.1f, 0.08f, 0.08f) : new Vector3(0.08f, 0.08f, length + 0.1f);
            Prim(PrimitiveType.Cube, "Frame", parent, center + Vector3.up * height * 0.5f, hs, frame);
            Prim(PrimitiveType.Cube, "Frame", parent, center - Vector3.up * height * 0.5f, hs, frame);
        }

        static void BuildBench(Transform parent, Vector3 pos, Material wood, Material metal, Material towel, Material bottle)
        {
            GameObject root = new GameObject("Bench");
            Transform t = root.transform;
            Prim(PrimitiveType.Cube, "Seat", t, pos + new Vector3(0f, 0.45f, 0f), new Vector3(0.45f, 0.05f, 1.8f), wood);
            Prim(PrimitiveType.Cube, "Backrest", t, pos + new Vector3(0.2f, 0.75f, 0f), new Vector3(0.05f, 0.4f, 1.8f), wood);
            for (int i = -1; i <= 1; i += 2)
                Prim(PrimitiveType.Cube, "Leg", t, pos + new Vector3(0f, 0.22f, i * 0.75f), new Vector3(0.4f, 0.44f, 0.05f), metal);
            Prim(PrimitiveType.Cube, "Towel", t, pos + new Vector3(0f, 0.49f, 0.4f), new Vector3(0.4f, 0.03f, 0.3f), towel);
            Prim(PrimitiveType.Cylinder, "Bottle", t, pos + new Vector3(-0.05f, 0.58f, -0.3f), new Vector3(0.07f, 0.1f, 0.07f), bottle);
            Prim(PrimitiveType.Cylinder, "Bottle", t, pos + new Vector3(0.08f, 0.58f, -0.45f), new Vector3(0.07f, 0.1f, 0.07f), bottle);
        }

        static void BuildPlant(Transform parent, Vector3 pos, Material pot, Material leaves)
        {
            GameObject root = new GameObject("Plant");
            root.transform.SetParent(parent, false);
            Transform t = root.transform;
            Prim(PrimitiveType.Cylinder, "Pot", t, pos + new Vector3(0f, 0.25f, 0f), new Vector3(0.5f, 0.25f, 0.5f), pot);
            Prim(PrimitiveType.Sphere, "Leaves", t, pos + new Vector3(0f, 0.85f, 0f), new Vector3(0.8f, 0.9f, 0.8f), leaves);
            Prim(PrimitiveType.Sphere, "Leaves", t, pos + new Vector3(0.15f, 1.3f, 0.05f), new Vector3(0.55f, 0.7f, 0.55f), leaves);
            Prim(PrimitiveType.Sphere, "Leaves", t, pos + new Vector3(-0.12f, 1.15f, -0.1f), new Vector3(0.5f, 0.6f, 0.5f), leaves);
        }

        static void BuildBanner(Transform parent, Vector3 pos, Material cloth, Material trim, string text, Font font)
        {
            GameObject root = new GameObject("Banner " + text);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            LocalPrim(PrimitiveType.Cube, "Cloth", root.transform, Vector3.zero, new Vector3(3.2f, 1.1f, 0.03f), cloth);
            LocalPrim(PrimitiveType.Cube, "Edge", root.transform, new Vector3(0f, 0.5f, -0.01f), new Vector3(3.2f, 0.08f, 0.03f), trim);
            LocalPrim(PrimitiveType.Cube, "Edge", root.transform, new Vector3(0f, -0.5f, -0.01f), new Vector3(3.2f, 0.08f, 0.03f), trim);
            AddText(root.transform, "Text", new Vector3(0f, 0f, -0.03f), text, 0.55f, Color.white, font);
        }

        static void BuildScoreboard(PingPongGame game, Font font, Vector3 camPos, Material board, Material red, Material blue, Material metal)
        {
            Vector3 pos = new Vector3(2.15f, 0f, 0.2f);
            GameObject root = new GameObject("Scoreboard");
            Transform t = root.transform;
            t.position = pos;
            Vector3 look = pos - camPos;
            look.y = 0f;
            t.rotation = Quaternion.LookRotation(look.normalized);

            LocalPrim(PrimitiveType.Cube, "Stand", t, new Vector3(0f, 0.72f, 0f), new Vector3(1.1f, 0.04f, 0.5f), board);
            for (int ix = -1; ix <= 1; ix += 2)
                LocalPrim(PrimitiveType.Cube, "Leg", t, new Vector3(ix * 0.48f, 0.36f, 0f), new Vector3(0.04f, 0.72f, 0.4f), metal);
            LocalPrim(PrimitiveType.Cube, "Back", t, new Vector3(0f, 1.05f, 0.08f), new Vector3(1.0f, 0.62f, 0.04f), board);
            LocalPrim(PrimitiveType.Cube, "Panel YOU", t, new Vector3(-0.25f, 1.0f, 0.05f), new Vector3(0.44f, 0.42f, 0.02f), red);
            LocalPrim(PrimitiveType.Cube, "Panel CPU", t, new Vector3(0.25f, 1.0f, 0.05f), new Vector3(0.44f, 0.42f, 0.02f), blue);

            AddText(t, "Label YOU", new Vector3(-0.25f, 1.28f, 0.03f), "YOU", 0.11f, Color.white, font);
            AddText(t, "Label CPU", new Vector3(0.25f, 1.28f, 0.03f), "CPU", 0.11f, Color.white, font);
            TextMesh ps = AddText(t, "Score YOU", new Vector3(-0.25f, 0.99f, 0.035f), "0", 0.3f, Color.white, font);
            TextMesh ais = AddText(t, "Score CPU", new Vector3(0.25f, 0.99f, 0.035f), "0", 0.3f, Color.white, font);
            TextMesh sv = AddText(t, "Serve", new Vector3(0f, 0.78f, -0.1f), "< serve", 0.08f, new Color(1f, 0.85f, 0.3f), font);
            sv.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);

            PingPongScoreboard sb = root.AddComponent<PingPongScoreboard>();
            sb.game = game;
            sb.playerScore = ps;
            sb.aiScore = ais;
            sb.serveMark = sv;
        }

        static PingPongPaddle CreatePaddle(string name, Side side, Vector3 pos, Material red, Material black, Material wood)
        {
            GameObject root = new GameObject(name);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, side == Side.Player ? 0f : 180f, 0f);

            // A cylinder (diameter 1, height 2) flattened into a disc and turned to face along the Z axis.
            GameObject blade = LocalPrim(PrimitiveType.Cylinder, "Blade", root.transform, Vector3.zero, new Vector3(0.265f, 0.004f, 0.265f), wood);
            blade.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject a = LocalPrim(PrimitiveType.Cylinder, "Rubber Red", root.transform, new Vector3(0f, 0f, -0.006f), new Vector3(0.255f, 0.002f, 0.255f), red);
            a.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject b = LocalPrim(PrimitiveType.Cylinder, "Rubber Black", root.transform, new Vector3(0f, 0f, 0.006f), new Vector3(0.255f, 0.002f, 0.255f), black);
            b.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            LocalPrim(PrimitiveType.Cube, "Handle", root.transform, new Vector3(0f, -0.18f, 0f), new Vector3(0.034f, 0.11f, 0.026f), wood);

            PingPongPaddle paddle = root.AddComponent<PingPongPaddle>();
            paddle.side = side;
            return paddle;
        }

        static Light AddLight(Transform parent, string name, LightType type, Vector3 pos, Vector3 euler, Color color, float intensity, LightShadows shadows)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(euler);
            Light l = go.AddComponent<Light>();
            l.type = type;
            l.color = color;
            l.intensity = intensity;
            l.shadows = shadows;
            return l;
        }

        static TextMesh AddText(Transform parent, string name, Vector3 localPos, string text, float height, Color color, Font font)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 120;                  // 120 px = 12 units at characterSize 1
            tm.characterSize = height / 9f;     // capital letter height ≈ height
            tm.color = color;
            if (font != null)
            {
                tm.font = font;
                go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
            return tm;
        }

        static Font GetFont()
        {
            string v = Application.unityVersion;
            bool legacyArial = v.StartsWith("2019") || v.StartsWith("2020") || v.StartsWith("2021");
            return Resources.GetBuiltinResource<Font>(legacyArial ? "Arial.ttf" : "LegacyRuntime.ttf");
        }

        // =================================================================== procedural textures

        class TileNoise
        {
            readonly int n;
            readonly float[] v;

            public TileNoise(int cells, int seed)
            {
                n = cells;
                v = new float[n * n];
                System.Random r = new System.Random(seed);
                for (int i = 0; i < v.Length; i++) v[i] = (float)r.NextDouble();
            }

            public float Sample(float u, float w)
            {
                float x = u * n, y = w * n;
                int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
                float fx = x - x0, fy = y - y0;
                fx = fx * fx * (3f - 2f * fx);
                fy = fy * fy * (3f - 2f * fy);
                int xa = ((x0 % n) + n) % n, ya = ((y0 % n) + n) % n;
                int xb = (xa + 1) % n, yb = (ya + 1) % n;
                float a = Mathf.Lerp(v[ya * n + xa], v[ya * n + xb], fx);
                float b = Mathf.Lerp(v[yb * n + xa], v[yb * n + xb], fx);
                return Mathf.Lerp(a, b, fy);
            }
        }

        static float Hash(int i)
        {
            unchecked
            {
                uint x = (uint)i * 747796405u + 2891336453u;
                x = ((x >> (int)((x >> 28) + 4u)) ^ x) * 277803737u;
                x = (x >> 22) ^ x;
                return (x & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        static readonly TileNoise noiseA = new TileNoise(8, 1);
        static readonly TileNoise noiseB = new TileNoise(32, 2);
        static readonly TileNoise noiseC = new TileNoise(16, 3);

        static Color SportFloorPixel(float u, float v)
        {
            float n = 0.88f + noiseA.Sample(u, v) * 0.14f + noiseB.Sample(u, v) * 0.08f;
            Color c = new Color(0.5f, 0.19f, 0.14f) * n;
            c.a = 1f;
            return c;
        }

        static Color ParquetPixel(float u, float v)
        {
            const int cols = 6;
            int col = Mathf.Min(cols - 1, Mathf.FloorToInt(u * cols));
            float vv = v + (col % 3) * 0.33f;
            int row = Mathf.FloorToInt(vv * 2f);
            float lu = u * cols - col;
            float lv = vv * 2f - row;
            float tone = 0.82f + Hash(col * 31 + (row % 2) * 7 + 3) * 0.3f;
            float grain = 0.93f + 0.07f * Mathf.Sin((lu * 9f + noiseC.Sample(u, v) * 3f) * 6.2832f);
            float n = 0.95f + noiseB.Sample(u, v) * 0.1f;
            Color c = new Color(0.66f, 0.46f, 0.28f) * (tone * grain * n);
            if (lu < 0.025f || lu > 0.975f || lv < 0.008f) c *= 0.55f;
            c.a = 1f;
            return c;
        }

        static Color ConcretePixel(float u, float v)
        {
            float n = 0.92f + noiseA.Sample(u, v) * 0.08f + noiseB.Sample(u, v) * 0.06f;
            return new Color(n, n, n, 1f);
        }

        static Color NetPixel(float u, float v)
        {
            // 512 x 48 px, 4 px cells → about 1.4 cm on the real net
            float x = u * 128f, y = v * 12f;
            bool lineX = x - Mathf.Floor(x) < 0.3f;
            bool lineY = y - Mathf.Floor(y) < 0.3f;
            bool edge = v > 0.9f || v < 0.04f;
            return new Color(1f, 1f, 1f, (lineX || lineY || edge) ? 1f : 0f);
        }

        static Color BlobPixel(float u, float v)
        {
            float dx = u - 0.5f, dy = v - 0.5f;
            float r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) * 2f);
            float a = (1f - r);
            return new Color(1f, 1f, 1f, a * a);
        }

        static Texture2D GetTex(string name, int w, int hgt, bool repeat, System.Func<float, float, Color> pixel)
        {
            string path = TexDir + "/" + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null) AssetDatabase.DeleteAsset(path);

            Texture2D tex = new Texture2D(w, hgt, TextureFormat.RGBA32, true);
            tex.name = name;
            tex.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Trilinear;
            tex.anisoLevel = 8;
            Color[] px = new Color[w * hgt];
            for (int y = 0; y < hgt; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = pixel((x + 0.5f) / w, (y + 0.5f) / hgt);
            tex.SetPixels(px);
            tex.Apply(true);
            AssetDatabase.CreateAsset(tex, path);
            return tex;
        }

        // =================================================================== materials and helpers

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 worldPos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col); // we do our own physics, Unity colliders aren't needed
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static GameObject LocalPrim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject go = Prim(type, name, parent, Vector3.zero, scale, mat);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            return go;
        }

        static Shader FindLitShader()
        {
            bool srp = GraphicsSettings.currentRenderPipeline != null;
            string[] names = srp
                ? new[] { "Universal Render Pipeline/Lit", "HDRP/Lit", "Standard" }
                : new[] { "Standard", "Universal Render Pipeline/Lit" };
            foreach (string n in names)
            {
                Shader s = Shader.Find(n);
                if (s != null) return s;
            }
            return null;
        }

        static Material LoadOrCreate(string name, Shader shader)
        {
            string path = MatDir + "/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                if (shader == null) return null;
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (shader != null && mat.shader != shader)
            {
                mat.shader = shader;
            }
            return mat;
        }

        static Material GetMat(string name, Color color, float smoothness, Texture2D tex = null, Vector2 tiling = default(Vector2), Color? emission = null, float metallic = 0f)
        {
            Material mat = LoadOrCreate(name, FindLitShader());
            if (mat == null) return null;

            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            Vector2 scale = tiling == Vector2.zero ? Vector2.one : tiling;
            if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", tex);
                mat.SetTextureScale("_MainTex", scale);
            }
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTextureScale("_BaseMap", scale);
            }

            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Unlit, transparent material (net, shadow, trail).</summary>
        static Material GetSpriteMat(string name, Texture2D tex, Color tint)
        {
            Shader s = Shader.Find("Sprites/Default");
            if (s == null) s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            Material mat = LoadOrCreate(name, s);
            if (mat == null) return null;
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            mat.color = tint;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        static void AddSceneToBuild(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes)
                if (s.path == path) return;
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
