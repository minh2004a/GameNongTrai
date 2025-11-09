using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemSO))]
public class ItemSOEditor : Editor
{
    SerializedProperty idProp;
    SerializedProperty iconProp;
    SerializedProperty seedDataProp;
    SerializedProperty categoryProp;
    SerializedProperty stackableProp;
    SerializedProperty weaponTypeProp;
    SerializedProperty toolTypeProp;
    SerializedProperty toolRangeTilesProp;
    SerializedProperty maxStackProp;
    SerializedProperty hitboxScaleProp;
    SerializedProperty hitboxYOffsetProp;
    SerializedProperty hitboxForwardProp;
    SerializedProperty damageProp;
    SerializedProperty rangeProp;
    SerializedProperty cooldownProp;
    SerializedProperty projectilePrefabProp;
    SerializedProperty projectileSpeedProp;
    SerializedProperty projectileMaxDistanceProp;
    SerializedProperty projectileHitVFXProp;

    void OnEnable()
    {
        idProp = serializedObject.FindProperty("id");
        iconProp = serializedObject.FindProperty("icon");
        seedDataProp = serializedObject.FindProperty("seedData");
        categoryProp = serializedObject.FindProperty("category");
        stackableProp = serializedObject.FindProperty("stackable");
        weaponTypeProp = serializedObject.FindProperty("weaponType");
        toolTypeProp = serializedObject.FindProperty("toolType");
        toolRangeTilesProp = serializedObject.FindProperty("toolRangeTiles");
        maxStackProp = serializedObject.FindProperty("maxStack");
        hitboxScaleProp = serializedObject.FindProperty("hitboxScale");
        hitboxYOffsetProp = serializedObject.FindProperty("hitboxYOffset");
        hitboxForwardProp = serializedObject.FindProperty("hitboxForward");
        damageProp = serializedObject.FindProperty("Dame");
        rangeProp = serializedObject.FindProperty("range");
        cooldownProp = serializedObject.FindProperty("cooldown");
        projectilePrefabProp = serializedObject.FindProperty("projectilePrefab");
        projectileSpeedProp = serializedObject.FindProperty("projectileSpeed");
        projectileMaxDistanceProp = serializedObject.FindProperty("projectileMaxDistance");
        projectileHitVFXProp = serializedObject.FindProperty("projectileHitVFX");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(idProp);
        EditorGUILayout.PropertyField(iconProp);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(categoryProp);
        var category = (ItemCategory)categoryProp.enumValueIndex;

        switch (category)
        {
            case ItemCategory.Weapon:
                DrawWeaponFields();
                break;
            case ItemCategory.Tool:
                DrawToolFields();
                break;
            case ItemCategory.Seed:
                DrawSeedFields();
                break;
            default:
                DrawStackableFields();
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawWeaponFields()
    {
        stackableProp.boolValue = false;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Weapon Settings", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Vũ khí sẽ luôn là vật phẩm đơn, không thể xếp chồng.", MessageType.Info);
        EditorGUILayout.PropertyField(weaponTypeProp);
        EditorGUILayout.PropertyField(damageProp, new GUIContent("Damage"));
        EditorGUILayout.PropertyField(rangeProp);
        EditorGUILayout.PropertyField(cooldownProp);
        EditorGUILayout.PropertyField(hitboxScaleProp);
        EditorGUILayout.PropertyField(hitboxYOffsetProp);
        EditorGUILayout.PropertyField(hitboxForwardProp);

        var weaponType = (WeaponType)weaponTypeProp.enumValueIndex;
        if (weaponType == WeaponType.Bow)
        {
            EditorGUILayout.PropertyField(projectilePrefabProp);
            EditorGUILayout.PropertyField(projectileSpeedProp);
            EditorGUILayout.PropertyField(projectileMaxDistanceProp);
            EditorGUILayout.PropertyField(projectileHitVFXProp);
        }
    }

    void DrawToolFields()
    {
        stackableProp.boolValue = false;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tool Settings", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Công cụ sẽ luôn là vật phẩm đơn, không thể xếp chồng.", MessageType.Info);
        EditorGUILayout.PropertyField(toolTypeProp);
        EditorGUILayout.PropertyField(toolRangeTilesProp);
    }

    void DrawSeedFields()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Seed Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(seedDataProp);
        if (!seedDataProp.objectReferenceValue)
        {
            EditorGUILayout.HelpBox("Chọn dữ liệu hạt giống (SeedSO) tương ứng cho vật phẩm hạt giống.", MessageType.Warning);
        }
        DrawStackableFields();
    }

    void DrawStackableFields()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Inventory Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(stackableProp);
        using (new EditorGUI.DisabledScope(!stackableProp.boolValue))
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(maxStackProp);
            EditorGUI.indentLevel--;
        }
    }
}