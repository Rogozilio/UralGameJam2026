using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Player))]
[CanEditMultipleObjects]
public sealed class PlayerEditor : Editor
{
    private const string SelectedTabKey = "PlayerEditor.SelectedTab";

    private static readonly GUIContent[] Tabs =
    {
        new("References"),
        new("Movement"),
        new("Camera"),
        new("LifeTime")
    };

    private SerializedProperty _render;
    private SerializedProperty _characterController;
    private SerializedProperty _footstepAudio;

    private SerializedProperty _moveSpeed;
    private SerializedProperty _jumpHeight;
    private SerializedProperty _gravity;
    private SerializedProperty _jumpBufferTime;
    private SerializedProperty _fallGravityMultiplier;
    private SerializedProperty _coyoteTime;
    private SerializedProperty _isIdleFire;

    private SerializedProperty _cameraTarget;
    private SerializedProperty _mouseSensitivity;
    private SerializedProperty _gamepadSensitivity;
    private SerializedProperty _pitchMin;
    private SerializedProperty _pitchMax;
    private SerializedProperty _isStaticCamera;
    private SerializedProperty _staticCameraTransform;
    private SerializedProperty _nextCamera;

    private SerializedProperty _deathSound;
    private SerializedProperty _deathSoundVolume;
    private SerializedProperty _deathDuration;
    private SerializedProperty _disintegrate;
    private SerializedProperty _lifeTimeDuration;
    private SerializedProperty _lifeTimeText;
    private SerializedProperty _fire;
    private SerializedProperty _fireSizeCurve;
    private SerializedProperty _skinnedMeshRenderers;
    private SerializedProperty _blendValue;
    private SerializedProperty _isFireZero;
    private SerializedProperty _gradientRenderers;

    private int _selectedTab;

    private void OnEnable()
    {
        _selectedTab = SessionState.GetInt(SelectedTabKey, 0);

        _render = serializedObject.FindProperty("render");
        _characterController = serializedObject.FindProperty("characterController");
        _footstepAudio = serializedObject.FindProperty("footstepAudio");

        _moveSpeed = serializedObject.FindProperty("moveSpeed");
        _jumpHeight = serializedObject.FindProperty("jumpHeight");
        _gravity = serializedObject.FindProperty("gravity");
        _jumpBufferTime = serializedObject.FindProperty("jumpBufferTime");
        _fallGravityMultiplier = serializedObject.FindProperty("fallGravityMultiplier");
        _coyoteTime = serializedObject.FindProperty("coyoteTime");
        _isIdleFire = serializedObject.FindProperty("isIdleFire");

        _cameraTarget = serializedObject.FindProperty("cameraTarget");
        _mouseSensitivity = serializedObject.FindProperty("mouseSensitivity");
        _gamepadSensitivity = serializedObject.FindProperty("gamepadSensitivity");
        _pitchMin = serializedObject.FindProperty("pitchMin");
        _pitchMax = serializedObject.FindProperty("pitchMax");
        _isStaticCamera = serializedObject.FindProperty("isStaticCamera");
        _staticCameraTransform = serializedObject.FindProperty("staticCameraTransform");
        _nextCamera = serializedObject.FindProperty("nextCamera");

        _deathSound = serializedObject.FindProperty("deathSound");
        _deathSoundVolume = serializedObject.FindProperty("deathSoundVolume");
        _deathDuration = serializedObject.FindProperty("deathDuration");
        _disintegrate = serializedObject.FindProperty("disintegrate");
        _lifeTimeDuration = serializedObject.FindProperty("lifeTimeDuration");
        _lifeTimeText = serializedObject.FindProperty("lifeTimeText");
        _fire = serializedObject.FindProperty("fire");
        _fireSizeCurve = serializedObject.FindProperty("fireSizeCurve");
        _skinnedMeshRenderers = serializedObject.FindProperty("skinnedMeshRenderers");
        _blendValue = serializedObject.FindProperty("blendValue");
        _isFireZero = serializedObject.FindProperty("isFireZero");
        _gradientRenderers = serializedObject.FindProperty("gradientRenderers");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptReference();

        var selectedTab = GUILayout.Toolbar(_selectedTab, Tabs);
        if (selectedTab != _selectedTab)
        {
            _selectedTab = selectedTab;
            SessionState.SetInt(SelectedTabKey, _selectedTab);
        }

        EditorGUILayout.Space(4f);

        switch (_selectedTab)
        {
            case 0:
                DrawReferences();
                break;
            case 1:
                DrawMovement();
                break;
            case 2:
                DrawCamera();
                break;
            case 3:
                DrawDeath();
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptReference()
    {
        using (new EditorGUI.DisabledScope(true))
        {
            var player = target as Player;
            EditorGUILayout.ObjectField(
                "Script",
                player != null ? MonoScript.FromMonoBehaviour(player) : null,
                typeof(MonoScript),
                false);
        }

        EditorGUILayout.Space(2f);
    }

    private void DrawReferences()
    {
        EditorGUILayout.PropertyField(_render);
        EditorGUILayout.PropertyField(_characterController);
        EditorGUILayout.PropertyField(_footstepAudio);
    }

    private void DrawMovement()
    {
        EditorGUILayout.PropertyField(_moveSpeed);
        EditorGUILayout.PropertyField(_jumpHeight);
        EditorGUILayout.PropertyField(_gravity);
        EditorGUILayout.PropertyField(_jumpBufferTime);
        EditorGUILayout.PropertyField(_fallGravityMultiplier);
        EditorGUILayout.PropertyField(_coyoteTime);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Initial State", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_isIdleFire);
    }

    private void DrawCamera()
    {
        EditorGUILayout.PropertyField(_cameraTarget);
        EditorGUILayout.PropertyField(_mouseSensitivity);
        EditorGUILayout.PropertyField(_gamepadSensitivity);
        EditorGUILayout.PropertyField(_pitchMin);
        EditorGUILayout.PropertyField(_pitchMax);
        EditorGUILayout.PropertyField(_isStaticCamera);
        EditorGUILayout.PropertyField(_staticCameraTransform);
        EditorGUILayout.PropertyField(_nextCamera);
    }

    private void DrawDeath()
    {
        EditorGUILayout.LabelField("Death", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_deathSound);
        EditorGUILayout.PropertyField(_deathSoundVolume);
        EditorGUILayout.PropertyField(_deathDuration);
        EditorGUILayout.PropertyField(_disintegrate);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Life Time", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_lifeTimeDuration, new GUIContent("Time"));
        EditorGUILayout.PropertyField(_lifeTimeText, new GUIContent("Text"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Blend Shape", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_fire);
        EditorGUILayout.PropertyField(_fireSizeCurve, new GUIContent("Fire Size Curve"));
        EditorGUILayout.PropertyField(_skinnedMeshRenderers);
        EditorGUILayout.PropertyField(_gradientRenderers);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Initial State", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_blendValue);
        EditorGUILayout.PropertyField(_isFireZero);
    }
}
