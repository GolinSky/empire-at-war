AssetDatabase.Refresh();
var modelPath = "Assets/Art/Models/SeparatistShips/Ginivex/Ginivex.fbx";
var textureRoot = "Assets/Art/Textures/Models/SeparatistShips/Ginivex/";
foreach (var path in System.IO.Directory.GetFiles(textureRoot, "*.png"))
{
    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
    importer.mipmapEnabled = true;
    importer.maxTextureSize = 1024;
    if (path.Contains("Normal")) { importer.textureType = TextureImporterType.NormalMap; importer.flipGreenChannel = true; }
    importer.SaveAndReimport();
}
var iconPath = "Assets/Art/Textures/Ui/Icons/SquadronIcon/Separatist/GinivexIcon.png";
var iconImporter = (TextureImporter)AssetImporter.GetAtPath(iconPath);
iconImporter.textureType = TextureImporterType.Sprite;
iconImporter.spriteImportMode = SpriteImportMode.Single;
iconImporter.alphaIsTransparency = true;
iconImporter.mipmapEnabled = false;
iconImporter.SaveAndReimport();
var matRoot = "Assets/Art/Materials/Models/SeparatistShips/Ginivex/";
var hull = new Material(Shader.Find("EmpireAtWar/Ship Lit")) { name = "Ginivex_Hull" };
hull.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textureRoot + "Ginivex_Hull_Albedo.png"));
hull.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textureRoot + "Ginivex_Hull_Normal.png"));
hull.EnableKeyword("_NORMALMAP"); hull.SetFloat("_Smoothness", 0.25f); hull.SetFloat("_Cull", 0);
hull.SetFloat("_TeamLiveryHue", 0); hull.SetFloat("_TeamLiveryHueRange", 0.07f); hull.SetFloat("_TeamLiveryMinSaturation", 0.4f); hull.SetFloat("_TeamLiveryStrength", 1);
AssetDatabase.CreateAsset(hull, matRoot + "Ginivex_Hull.mat");
var cockpit = new Material(hull) { name = "Ginivex_Cockpit" };
cockpit.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textureRoot + "Ginivex_Cockpit_Albedo.png"));
cockpit.SetTexture("_BumpMap", null); cockpit.DisableKeyword("_NORMALMAP"); cockpit.SetFloat("_Smoothness", 0.45f);
AssetDatabase.CreateAsset(cockpit, matRoot + "Ginivex_Cockpit.mat");
var engine = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Ginivex_Engine" };
engine.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textureRoot + "Ginivex_Engine_Albedo.png"));
engine.SetFloat("_Surface", 1);engine.SetFloat("_Blend", 2);engine.SetFloat("_SrcBlend", 5);engine.SetFloat("_DstBlend", 1);engine.SetFloat("_ZWrite", 0);engine.SetFloat("_Cull", 0);engine.renderQueue=3000;engine.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
AssetDatabase.CreateAsset(engine, matRoot + "Ginivex_Engine.mat");
var modelImporter=(ModelImporter)AssetImporter.GetAtPath(modelPath);
modelImporter.globalScale=0.04f;
modelImporter.importAnimation=false;
modelImporter.importNormals=ModelImporterNormals.Import;
foreach(var material in AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>())
{
    var target=material.name.Contains("M001") ? cockpit : material.name.Contains("M007") || material.name.Contains("M008") ? engine : hull;
    modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(material), target);
}
modelImporter.SaveAndReimport();
AssetDatabase.SaveAssets();
var model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
return string.Join("\n",model.GetComponentsInChildren<Renderer>(true).Select(r=>$"{r.name} center={r.bounds.center} size={r.bounds.size}"));
