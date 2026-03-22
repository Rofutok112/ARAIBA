using System.Collections.Generic;
using Projects.Scripts.Puzzle;
using UnityEditor;
using UnityEngine;

namespace Projects.Scripts.Editor
{
    [CustomEditor(typeof(PuzzlePieceShape))]
    public class PuzzlePieceShapeEditor : UnityEditor.Editor
    {
        private const float CellSize = 36f;
        private const float CellPadding = 2f;
        private const float HeaderSpacing = 8f;

        private static readonly Color FilledColor = new(0.25f, 0.55f, 0.95f, 1f);
        private static readonly Color FilledHoverColor = new(0.45f, 0.70f, 1f, 1f);
        private static readonly Color EmptyColor = new(0.22f, 0.22f, 0.22f, 1f);
        private static readonly Color EmptyHoverColor = new(0.35f, 0.35f, 0.35f, 1f);
        private static readonly Color GridLineColor = new(0.15f, 0.15f, 0.15f, 1f);
        private static readonly Color FilledBorderColor = new(0.15f, 0.40f, 0.80f, 1f);
        private static readonly Color EmptyBorderColor = new(0.30f, 0.30f, 0.30f, 1f);

        private static readonly string[] PresetNames =
        {
            "1x2", "2x1", "1x3", "3x1", "1x4", "4x1", "2x2", "3x3", "L", "J", "T", "S", "Z", "Cross", "U"
        };

        private SerializedProperty _widthProp;
        private SerializedProperty _heightProp;
        private SerializedProperty _cellsProp;
        private SerializedProperty _dishTypeNameProp;
        private SerializedProperty _dishSpritesProp;
        private SerializedProperty _refillIntervalSecondsProp;
        private bool _showPresets;
        private Vector2 _presetsScroll;

        private void OnEnable()
        {
            _widthProp = serializedObject.FindProperty("width");
            _heightProp = serializedObject.FindProperty("height");
            _cellsProp = serializedObject.FindProperty("cells");
            _dishTypeNameProp = serializedObject.FindProperty("dishTypeName");
            _dishSpritesProp = serializedObject.FindProperty("dishSprites");
            _refillIntervalSecondsProp = serializedObject.FindProperty("refillIntervalSeconds");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawTitle();
            EditorGUILayout.Space(HeaderSpacing);

            EditorGUILayout.PropertyField(_dishTypeNameProp, new GUIContent("Dish Type Name"));
            EditorGUILayout.Space(HeaderSpacing);

            DrawSizeControls();
            EditorGUILayout.Space(HeaderSpacing);

            DrawVisualGrid();
            EditorGUILayout.Space(HeaderSpacing);

            DrawToolButtons();
            EditorGUILayout.Space(HeaderSpacing);

            DrawPresets();
            EditorGUILayout.Space(HeaderSpacing);

            DrawDishVisual();
            EditorGUILayout.Space(HeaderSpacing);

            DrawRefillSettings();
            EditorGUILayout.Space(HeaderSpacing);

            DrawStatistics();

            var changed = serializedObject.ApplyModifiedProperties();
            if (changed || GUI.changed)
            {
                ((PuzzlePieceShape)target).ClearAutoSprite();
            }
        }

        private void DrawTitle()
        {
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Puzzle Piece Shape", style);
            EditorGUILayout.Space(2);

            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.5f, 0.8f, 0.6f));
        }

        private void DrawSizeControls()
        {
            EditorGUILayout.LabelField("Grid Size", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("W", GUILayout.Width(16));
            EditorGUI.BeginChangeCheck();
            var newWidth = EditorGUILayout.IntField(_widthProp.intValue, GUILayout.Width(50));
            if (EditorGUI.EndChangeCheck())
            {
                ResizeGrid(Mathf.Clamp(newWidth, 1, 10), _heightProp.intValue);
            }

            GUILayout.Space(16);

            EditorGUILayout.LabelField("H", GUILayout.Width(16));
            EditorGUI.BeginChangeCheck();
            var newHeight = EditorGUILayout.IntField(_heightProp.intValue, GUILayout.Width(50));
            if (EditorGUI.EndChangeCheck())
            {
                ResizeGrid(_widthProp.intValue, Mathf.Clamp(newHeight, 1, 10));
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawVisualGrid()
        {
            var w = _widthProp.intValue;
            var h = _heightProp.intValue;
            EnsureCellsArray(w, h);

            var totalW = w * (CellSize + CellPadding) + CellPadding;
            var totalH = h * (CellSize + CellPadding) + CellPadding;
            const float labelRowHeight = 16f;
            const float labelColWidth = 20f;

            var areaRect = GUILayoutUtility.GetRect(totalW + labelColWidth + 4f, totalH + labelRowHeight + 4f, GUILayout.ExpandWidth(false));
            var gridStartX = areaRect.x + (areaRect.width - totalW - labelColWidth) / 2f + labelColWidth;
            var gridStartY = areaRect.y + labelRowHeight;

            var bgRect = new Rect(gridStartX - 1, gridStartY - 1, totalW + 2, totalH + 2);
            EditorGUI.DrawRect(bgRect, GridLineColor);

            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9
            };

            for (var x = 0; x < w; x++)
            {
                var labelRect = new Rect(gridStartX + CellPadding + x * (CellSize + CellPadding), gridStartY - labelRowHeight, CellSize, labelRowHeight);
                GUI.Label(labelRect, x.ToString(), labelStyle);
            }

            var mousePos = Event.current.mousePosition;
            var repaintNeeded = false;

            for (var y = 0; y < h; y++)
            {
                var displayY = h - 1 - y;
                var yLabelRect = new Rect(gridStartX - labelColWidth - 2, gridStartY + CellPadding + y * (CellSize + CellPadding), labelColWidth, CellSize);
                GUI.Label(yLabelRect, displayY.ToString(), labelStyle);

                for (var x = 0; x < w; x++)
                {
                    var cellRect = new Rect(
                        gridStartX + CellPadding + x * (CellSize + CellPadding),
                        gridStartY + CellPadding + y * (CellSize + CellPadding),
                        CellSize,
                        CellSize);

                    var index = displayY * w + x;
                    var isFilled = index < _cellsProp.arraySize && _cellsProp.GetArrayElementAtIndex(index).boolValue;
                    var isHovered = cellRect.Contains(mousePos);

                    var cellColor = isFilled
                        ? isHovered ? FilledHoverColor : FilledColor
                        : isHovered ? EmptyHoverColor : EmptyColor;
                    var borderColor = isFilled ? FilledBorderColor : EmptyBorderColor;

                    EditorGUI.DrawRect(cellRect, borderColor);
                    var innerRect = new Rect(cellRect.x + 1, cellRect.y + 1, cellRect.width - 2, cellRect.height - 2);
                    EditorGUI.DrawRect(innerRect, cellColor);

                    if (isFilled)
                    {
                        var checkStyle = new GUIStyle(EditorStyles.boldLabel)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = 16
                        };
                        GUI.Label(cellRect, "■", checkStyle);
                    }

                    if (isHovered) repaintNeeded = true;

                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && cellRect.Contains(Event.current.mousePosition))
                    {
                        if (index < _cellsProp.arraySize)
                        {
                            var element = _cellsProp.GetArrayElementAtIndex(index);
                            element.boolValue = !element.boolValue;
                            serializedObject.ApplyModifiedProperties();
                            Event.current.Use();
                            GUI.changed = true;
                        }
                    }
                }
            }

            if (repaintNeeded) Repaint();
        }

        private void DrawToolButtons()
        {
            EditorGUILayout.LabelField("Tools", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Fill", GUILayout.Height(24))) SetAllCells(true);
            if (GUILayout.Button("Clear", GUILayout.Height(24))) SetAllCells(false);
            if (GUILayout.Button("Invert", GUILayout.Height(24))) InvertCells();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Flip H", GUILayout.Height(24))) FlipHorizontal();
            if (GUILayout.Button("Flip V", GUILayout.Height(24))) FlipVertical();
            if (GUILayout.Button("Rotate 90", GUILayout.Height(24))) Rotate90();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPresets()
        {
            _showPresets = EditorGUILayout.Foldout(_showPresets, "Presets", true);
            if (!_showPresets) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _presetsScroll = EditorGUILayout.BeginScrollView(_presetsScroll, GUILayout.MaxHeight(200));
            for (var i = 0; i < PresetNames.Length; i++)
            {
                if (GUILayout.Button(PresetNames[i], GUILayout.Height(22)))
                {
                    ApplyPreset(i);
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawDishVisual()
        {
            EditorGUILayout.LabelField("Dish Visual", EditorStyles.miniLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(_dishSpritesProp, new GUIContent("Variant Sprites"), true);

            var shapeTarget = (PuzzlePieceShape)target;
            var previewSprites = CollectPreviewSprites(shapeTarget);

            if (previewSprites.Count == 0)
            {
                EditorGUILayout.HelpBox("No variant sprite is assigned, so an auto-generated shape sprite is shown.", MessageType.Info);
                var fallback = shapeTarget.GetEffectiveSprite();
                if (fallback != null)
                {
                    previewSprites.Add(fallback);
                }
            }

            if (previewSprites.Count > 0)
            {
                EditorGUILayout.Space(4);
                DrawSpritePreviewGrid(previewSprites);
            }

            EditorGUILayout.EndVertical();
        }

        private static List<Sprite> CollectPreviewSprites(PuzzlePieceShape shapeTarget)
        {
            var sprites = new List<Sprite>();
            if (shapeTarget.DishSprites == null) return sprites;

            for (var i = 0; i < shapeTarget.DishSprites.Count; i++)
            {
                var sprite = shapeTarget.GetSpriteAt(i);
                if (sprite != null)
                {
                    sprites.Add(sprite);
                }
            }

            return sprites;
        }

        private void DrawSpritePreviewGrid(List<Sprite> previewSprites)
        {
            var width = _widthProp.intValue;
            var height = _heightProp.intValue;
            var previewSize = Mathf.Min(CellSize * Mathf.Max(width, height), 96f);
            var aspectRatio = (float)width / height;

            float previewW;
            float previewH;
            if (aspectRatio >= 1f)
            {
                previewW = previewSize;
                previewH = previewSize / aspectRatio;
            }
            else
            {
                previewH = previewSize;
                previewW = previewSize * aspectRatio;
            }

            const int maxColumns = 3;
            for (var i = 0; i < previewSprites.Count; i += maxColumns)
            {
                EditorGUILayout.BeginHorizontal();
                var rowCount = Mathf.Min(maxColumns, previewSprites.Count - i);
                for (var j = 0; j < rowCount; j++)
                {
                    DrawSingleSpritePreview(previewSprites[i + j], previewW, previewH, width, height);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private static void DrawSingleSpritePreview(Sprite sprite, float previewW, float previewH, int width, int height)
        {
            const float infoWidth = 140f;
            var columnWidth = Mathf.Max(previewW + 8f, infoWidth);

            EditorGUILayout.BeginVertical(GUILayout.Width(columnWidth));

            var previewRect = GUILayoutUtility.GetRect(previewW, previewH, GUILayout.ExpandWidth(false));
            previewRect.width = previewW;
            previewRect.height = previewH;
            previewRect.x += (columnWidth - previewW) * 0.5f;
            EditorGUI.DrawRect(previewRect, new Color(0.15f, 0.15f, 0.15f, 1f));

            var tex = sprite.texture;
            var texRect = sprite.textureRect;
            var uvRect = new Rect(
                texRect.x / tex.width,
                texRect.y / tex.height,
                texRect.width / tex.width,
                texRect.height / tex.height);
            GUI.DrawTextureWithTexCoords(previewRect, tex, uvRect);

            var infoStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 9,
                wordWrap = false,
                clipping = TextClipping.Overflow
            };
            EditorGUILayout.LabelField(
                $"{texRect.width}x{texRect.height}px / {width * 16}x{height * 16}px",
                infoStyle,
                GUILayout.Width(columnWidth));

            EditorGUILayout.EndVertical();
        }

        private void DrawRefillSettings()
        {
            EditorGUILayout.LabelField("Refill Settings", EditorStyles.miniLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(_refillIntervalSecondsProp, new GUIContent("Refill Seconds"));
            EditorGUILayout.EndVertical();
        }

        private void DrawStatistics()
        {
            var w = _widthProp.intValue;
            var h = _heightProp.intValue;
            var filledCount = 0;
            var totalCells = w * h;

            for (var i = 0; i < _cellsProp.arraySize; i++)
            {
                if (_cellsProp.GetArrayElementAtIndex(i).boolValue) filledCount++;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Size: {w} x {h} | Cells: {filledCount} / {totalCells}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void EnsureCellsArray(int w, int h)
        {
            var required = w * h;
            if (_cellsProp.arraySize != required)
            {
                _cellsProp.arraySize = required;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void ResizeGrid(int newWidth, int newHeight)
        {
            var oldWidth = _widthProp.intValue;
            var oldHeight = _heightProp.intValue;
            var oldCells = new bool[oldWidth * oldHeight];
            for (var i = 0; i < _cellsProp.arraySize && i < oldCells.Length; i++)
            {
                oldCells[i] = _cellsProp.GetArrayElementAtIndex(i).boolValue;
            }

            _widthProp.intValue = newWidth;
            _heightProp.intValue = newHeight;
            _cellsProp.arraySize = newWidth * newHeight;

            for (var i = 0; i < _cellsProp.arraySize; i++)
            {
                _cellsProp.GetArrayElementAtIndex(i).boolValue = false;
            }

            var copyW = Mathf.Min(oldWidth, newWidth);
            var copyH = Mathf.Min(oldHeight, newHeight);
            for (var y = 0; y < copyH; y++)
            {
                for (var x = 0; x < copyW; x++)
                {
                    var oldIndex = y * oldWidth + x;
                    var newIndex = y * newWidth + x;
                    if (oldIndex < oldCells.Length && newIndex < _cellsProp.arraySize)
                    {
                        _cellsProp.GetArrayElementAtIndex(newIndex).boolValue = oldCells[oldIndex];
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void SetAllCells(bool value)
        {
            Undo.RecordObject(target, value ? "Fill All Cells" : "Clear All Cells");
            for (var i = 0; i < _cellsProp.arraySize; i++)
            {
                _cellsProp.GetArrayElementAtIndex(i).boolValue = value;
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void InvertCells()
        {
            Undo.RecordObject(target, "Invert Cells");
            for (var i = 0; i < _cellsProp.arraySize; i++)
            {
                var element = _cellsProp.GetArrayElementAtIndex(i);
                element.boolValue = !element.boolValue;
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void FlipHorizontal()
        {
            Undo.RecordObject(target, "Flip Horizontal");
            var w = _widthProp.intValue;
            var h = _heightProp.intValue;
            var tempCells = ReadCells(w, h);

            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var newIndex = y * w + (w - 1 - x);
                    _cellsProp.GetArrayElementAtIndex(newIndex).boolValue = tempCells[y * w + x];
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void FlipVertical()
        {
            Undo.RecordObject(target, "Flip Vertical");
            var w = _widthProp.intValue;
            var h = _heightProp.intValue;
            var tempCells = ReadCells(w, h);

            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var newIndex = (h - 1 - y) * w + x;
                    _cellsProp.GetArrayElementAtIndex(newIndex).boolValue = tempCells[y * w + x];
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void Rotate90()
        {
            Undo.RecordObject(target, "Rotate 90");
            var w = _widthProp.intValue;
            var h = _heightProp.intValue;
            var tempCells = ReadCells(w, h);

            var newW = h;
            var newH = w;

            _widthProp.intValue = newW;
            _heightProp.intValue = newH;
            _cellsProp.arraySize = newW * newH;

            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var newX = h - 1 - y;
                    var newY = x;
                    var newIndex = newY * newW + newX;
                    _cellsProp.GetArrayElementAtIndex(newIndex).boolValue = tempCells[y * w + x];
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private bool[] ReadCells(int w, int h)
        {
            var cells = new bool[w * h];
            for (var i = 0; i < _cellsProp.arraySize && i < cells.Length; i++)
            {
                cells[i] = _cellsProp.GetArrayElementAtIndex(i).boolValue;
            }

            return cells;
        }

        private void ApplyPreset(int index)
        {
            Undo.RecordObject(target, "Apply Preset");

            int w;
            int h;
            bool[] cells;

            switch (index)
            {
                case 0:
                    w = 2; h = 1; cells = new[] { true, true };
                    break;
                case 1:
                    w = 1; h = 2; cells = new[] { true, true };
                    break;
                case 2:
                    w = 3; h = 1; cells = new[] { true, true, true };
                    break;
                case 3:
                    w = 1; h = 3; cells = new[] { true, true, true };
                    break;
                case 4:
                    w = 4; h = 1; cells = new[] { true, true, true, true };
                    break;
                case 5:
                    w = 1; h = 4; cells = new[] { true, true, true, true };
                    break;
                case 6:
                    w = 2; h = 2; cells = new[] { true, true, true, true };
                    break;
                case 7:
                    w = 3; h = 3; cells = new[] { true, true, true, true, true, true, true, true, true };
                    break;
                case 8:
                    w = 2; h = 3; cells = new[] { true, false, true, false, true, true };
                    break;
                case 9:
                    w = 2; h = 3; cells = new[] { false, true, false, true, true, true };
                    break;
                case 10:
                    w = 3; h = 2; cells = new[] { true, true, true, false, true, false };
                    break;
                case 11:
                    w = 3; h = 2; cells = new[] { false, true, true, true, true, false };
                    break;
                case 12:
                    w = 3; h = 2; cells = new[] { true, true, false, false, true, true };
                    break;
                case 13:
                    w = 3; h = 3; cells = new[] { false, true, false, true, true, true, false, true, false };
                    break;
                case 14:
                    w = 3; h = 2; cells = new[] { true, true, true, true, false, true };
                    break;
                default:
                    return;
            }

            _widthProp.intValue = w;
            _heightProp.intValue = h;
            _cellsProp.arraySize = w * h;
            for (var i = 0; i < cells.Length; i++)
            {
                _cellsProp.GetArrayElementAtIndex(i).boolValue = cells[i];
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
