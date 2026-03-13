using Projects.Scripts.Puzzle;
using UnityEditor;
using UnityEngine;

namespace Projects.Scripts.Editor
{
    /// <summary>
    /// PuzzlePieceShape 用のカスタムエディタ。
    /// セルをクリックでトグルできるビジュアルグリッドエディタを提供する。
    /// </summary>
    [CustomEditor(typeof(PuzzlePieceShape))]
    public class PuzzlePieceShapeEditor : UnityEditor.Editor
    {
        // ─── セルの描画サイズ ───
        private const float CellSize = 36f;
        private const float CellPadding = 2f;
        private const float HeaderSpacing = 8f;

        // ─── 色定義 ───
        private static readonly Color FilledColor = new(0.25f, 0.55f, 0.95f, 1f);
        private static readonly Color FilledHoverColor = new(0.45f, 0.70f, 1f, 1f);
        private static readonly Color EmptyColor = new(0.22f, 0.22f, 0.22f, 1f);
        private static readonly Color EmptyHoverColor = new(0.35f, 0.35f, 0.35f, 1f);
        private static readonly Color GridLineColor = new(0.15f, 0.15f, 0.15f, 1f);
        private static readonly Color FilledBorderColor = new(0.15f, 0.40f, 0.80f, 1f);
        private static readonly Color EmptyBorderColor = new(0.30f, 0.30f, 0.30f, 1f);
        private static readonly Color LabelBgColor = new(0.18f, 0.18f, 0.18f, 0.9f);

        // ─── プリセット定義 ───
        private static readonly string[] PresetNames =
        {
            "━ 横棒 (1×2)", "┃ 縦棒 (2×1)",
            "━━ 横棒 (1×3)", "┃┃ 縦棒 (3×1)",
            "━━━ 横棒 (1×4)", "┃┃┃ 縦棒 (4×1)",
            "■ 2×2 正方形", "■ 3×3 正方形",
            "L字", "逆L字", "T字", "S字", "Z字",
            "十字", "コの字",
        };

        private SerializedProperty _widthProp;
        private SerializedProperty _heightProp;
        private SerializedProperty _cellsProp;
        private SerializedProperty _dishSpriteProp;
        private SerializedProperty _dishColorProp;
        private Texture2D _whiteTexture;
        private bool _showPresets = false;
        private Vector2 _presetsScroll;

        private void OnEnable()
        {
            _widthProp = serializedObject.FindProperty("width");
            _heightProp = serializedObject.FindProperty("height");
            _cellsProp = serializedObject.FindProperty("cells");
            _dishSpriteProp = serializedObject.FindProperty("dishSprite");
            _dishColorProp = serializedObject.FindProperty("dishColor");
            _whiteTexture = Texture2D.whiteTexture;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // ─── タイトル ───
            DrawTitle();

            EditorGUILayout.Space(HeaderSpacing);

            // ─── サイズ設定 ───
            DrawSizeControls();

            EditorGUILayout.Space(HeaderSpacing);

            // ─── ビジュアルグリッド ───
            DrawVisualGrid();

            EditorGUILayout.Space(HeaderSpacing);

            // ─── ツールボタン ───
            DrawToolButtons();

            EditorGUILayout.Space(HeaderSpacing);

            // ─── プリセット ───
            DrawPresets();

            EditorGUILayout.Space(HeaderSpacing);

            // ─── 食器ビジュアル ───
            DrawDishVisual();

            EditorGUILayout.Space(HeaderSpacing);

            // ─── 統計情報 ───
            DrawStatistics();

            var changed = serializedObject.ApplyModifiedProperties();
            if (changed || GUI.changed)
            {
                ((PuzzlePieceShape)target).ClearAutoSprite();
            }
        }

        // =====================================================================
        //  タイトル
        // =====================================================================
        private void DrawTitle()
        {
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.9f, 1f) }
            };

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("🧩 パズルピース シェイプエディタ", style);
            EditorGUILayout.Space(2);

            // 区切り線
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.5f, 0.8f, 0.6f));
        }

        // =====================================================================
        //  サイズコントロール
        // =====================================================================
        private void DrawSizeControls()
        {
            EditorGUILayout.LabelField("グリッドサイズ",
                new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                });

            EditorGUILayout.BeginHorizontal();

            // 幅
            EditorGUILayout.LabelField("幅", GUILayout.Width(24));
            EditorGUI.BeginChangeCheck();
            var newWidth = EditorGUILayout.IntField(_widthProp.intValue, GUILayout.Width(50));
            if (EditorGUI.EndChangeCheck())
            {
                newWidth = Mathf.Clamp(newWidth, 1, 10);
                ResizeGrid(newWidth, _heightProp.intValue);
            }

            GUILayout.Space(16);

            // 高さ
            EditorGUILayout.LabelField("高さ", GUILayout.Width(24));
            EditorGUI.BeginChangeCheck();
            var newHeight = EditorGUILayout.IntField(_heightProp.intValue, GUILayout.Width(50));
            if (EditorGUI.EndChangeCheck())
            {
                newHeight = Mathf.Clamp(newHeight, 1, 10);
                ResizeGrid(_widthProp.intValue, newHeight);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // =====================================================================
        //  ビジュアルグリッド描画
        // =====================================================================
        private void DrawVisualGrid()
        {
            var w = _widthProp.intValue;
            var h = _heightProp.intValue;
            EnsureCellsArray(w, h);

            var totalW = w * (CellSize + CellPadding) + CellPadding;
            var totalH = h * (CellSize + CellPadding) + CellPadding;

            // ラベル行の高さ
            const float labelRowHeight = 16f;

            // グリッド領域を確保（上部にX座標ラベル + 左にY座標ラベル分のスペース）
            const float labelColWidth = 20f;
            var areaRect = GUILayoutUtility.GetRect(
                totalW + labelColWidth + 4f,
                totalH + labelRowHeight + 4f,
                GUILayout.ExpandWidth(false)
            );

            // 中央寄せ
            var gridStartX = areaRect.x + (areaRect.width - totalW - labelColWidth) / 2f + labelColWidth;
            var gridStartY = areaRect.y + labelRowHeight;

            // ─── 背景 ───
            var bgRect = new Rect(gridStartX - 1, gridStartY - 1, totalW + 2, totalH + 2);
            EditorGUI.DrawRect(bgRect, GridLineColor);

            // ─── 座標ラベルスタイル ───
            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
            };

            // ─── X座標ラベル ───
            for (var x = 0; x < w; x++)
            {
                var labelRect = new Rect(
                    gridStartX + CellPadding + x * (CellSize + CellPadding),
                    gridStartY - labelRowHeight,
                    CellSize,
                    labelRowHeight
                );
                GUI.Label(labelRect, x.ToString(), labelStyle);
            }

            // ─── セル描画（上から下、Y座標は反転表示：上が大きいY） ───
            var mousePos = Event.current.mousePosition;
            var repaintNeeded = false;

            for (var y = 0; y < h; y++)
            {
                // Y座標を反転（上が height-1, 下が 0）
                var displayY = h - 1 - y;

                // Y座標ラベル
                var yLabelRect = new Rect(
                    gridStartX - labelColWidth - 2,
                    gridStartY + CellPadding + y * (CellSize + CellPadding),
                    labelColWidth,
                    CellSize
                );
                GUI.Label(yLabelRect, displayY.ToString(), labelStyle);

                for (var x = 0; x < w; x++)
                {
                    var cellRect = new Rect(
                        gridStartX + CellPadding + x * (CellSize + CellPadding),
                        gridStartY + CellPadding + y * (CellSize + CellPadding),
                        CellSize,
                        CellSize
                    );

                    var index = displayY * w + x;
                    var isFilled = index < _cellsProp.arraySize &&
                                   _cellsProp.GetArrayElementAtIndex(index).boolValue;
                    var isHovered = cellRect.Contains(mousePos);

                    // セルの色を決定
                    Color cellColor;
                    Color borderColor;
                    if (isFilled)
                    {
                        cellColor = isHovered ? FilledHoverColor : FilledColor;
                        borderColor = FilledBorderColor;
                    }
                    else
                    {
                        cellColor = isHovered ? EmptyHoverColor : EmptyColor;
                        borderColor = EmptyBorderColor;
                    }

                    // ボーダー描画
                    EditorGUI.DrawRect(cellRect, borderColor);
                    var innerRect = new Rect(
                        cellRect.x + 1, cellRect.y + 1,
                        cellRect.width - 2, cellRect.height - 2
                    );
                    EditorGUI.DrawRect(innerRect, cellColor);

                    // 埋まっているセルにはチェックマーク風の表示
                    if (isFilled)
                    {
                        var checkStyle = new GUIStyle(EditorStyles.boldLabel)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = 16,
                            normal = { textColor = new Color(1f, 1f, 1f, 0.8f) }
                        };
                        GUI.Label(cellRect, "■", checkStyle);
                    }

                    // ホバー時にリペイント
                    if (isHovered)
                        repaintNeeded = true;

                    // クリック検出
                    if (Event.current.type == EventType.MouseDown &&
                        Event.current.button == 0 &&
                        cellRect.Contains(Event.current.mousePosition))
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

            if (repaintNeeded)
                Repaint();
        }

        // =====================================================================
        //  ツールボタン
        // =====================================================================
        private void DrawToolButtons()
        {
            EditorGUILayout.LabelField("ツール",
                new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                });

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("全て埋める", GUILayout.Height(24)))
                SetAllCells(true);

            if (GUILayout.Button("全てクリア", GUILayout.Height(24)))
                SetAllCells(false);

            if (GUILayout.Button("反転", GUILayout.Height(24)))
                InvertCells();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("↔ 左右反転", GUILayout.Height(24)))
                FlipHorizontal();

            if (GUILayout.Button("↕ 上下反転", GUILayout.Height(24)))
                FlipVertical();

            if (GUILayout.Button("↻ 90° 回転", GUILayout.Height(24)))
                Rotate90();

            EditorGUILayout.EndHorizontal();
        }

        // =====================================================================
        //  プリセット
        // =====================================================================
        private void DrawPresets()
        {
            _showPresets = EditorGUILayout.Foldout(_showPresets, "📦 プリセット", true);
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

        // =====================================================================
        //  食器ビジュアル
        // =====================================================================
        private void DrawDishVisual()
        {
            EditorGUILayout.LabelField("🍽 食器ビジュアル",
                new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                });

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(_dishSpriteProp, new GUIContent("食器スプライト"));
            EditorGUILayout.PropertyField(_dishColorProp, new GUIContent("食器カラー"));

            // スプライトのプレビュー表示
            var shapeTarget = (PuzzlePieceShape)target;
            var sprite = shapeTarget.GetEffectiveSprite();
            var isAuto = _dishSpriteProp.objectReferenceValue == null;

            if (sprite != null)
            {
                EditorGUILayout.Space(4);

                if (isAuto)
                {
                    EditorGUILayout.HelpBox("スプライト未設定のため、形状から自動生成されたスプライトを表示しています。\nゲーム内ではこのスプライトが使用されます。", MessageType.Info);
                    EditorGUILayout.Space(2);
                }

                var w = _widthProp.intValue;
                var h = _heightProp.intValue;
                var previewSize = Mathf.Min(CellSize * Mathf.Max(w, h), 120f);
                var aspectRatio = (float)w / h;

                float previewW, previewH;
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

                var previewRect = GUILayoutUtility.GetRect(previewW, previewH, GUILayout.ExpandWidth(false));
                previewRect.x += (EditorGUIUtility.currentViewWidth - previewW) / 2f - 16f;
                previewRect.width = previewW;
                previewRect.height = previewH;

                // 背景
                EditorGUI.DrawRect(previewRect, new Color(0.15f, 0.15f, 0.15f, 1f));

                // スプライトを描画
                var tex = sprite.texture;
                var texRect = sprite.textureRect;
                var uvRect = new Rect(
                    texRect.x / tex.width,
                    texRect.y / tex.height,
                    texRect.width / tex.width,
                    texRect.height / tex.height
                );
                
                // Colortint for autogenerated or colored sprites
                var oldColor = GUI.color;
                GUI.color = shapeTarget.DishColor;
                GUI.DrawTextureWithTexCoords(previewRect, tex, uvRect);
                GUI.color = oldColor;

                var infoStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    fontSize = 9
                };
                EditorGUILayout.LabelField(
                    $"{texRect.width}×{texRect.height}px  |  推奨: {w * 16}×{h * 16}px",
                    infoStyle);
            }

            EditorGUILayout.EndVertical();
        }

        // =====================================================================
        //  統計情報
        // =====================================================================
        private void DrawStatistics()
        {
            var w = _widthProp.intValue;
            var h = _heightProp.intValue;
            var filledCount = 0;
            var totalCells = w * h;

            for (var i = 0; i < _cellsProp.arraySize; i++)
            {
                if (_cellsProp.GetArrayElementAtIndex(i).boolValue)
                    filledCount++;
            }

            var boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 4, 4)
            };

            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField($"📊 サイズ: {w} × {h}  |  セル数: {filledCount} / {totalCells}",
                new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                    fontSize = 11
                });
            EditorGUILayout.EndVertical();
        }

        // =====================================================================
        //  ユーティリティメソッド
        // =====================================================================

        /// <summary>
        /// cells配列が必要な長さか確認し、足りなければリサイズする。
        /// </summary>
        private void EnsureCellsArray(int w, int h)
        {
            var required = w * h;
            if (_cellsProp.arraySize != required)
            {
                _cellsProp.arraySize = required;
                serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// グリッドをリサイズし、可能な限り既存データを保持する。
        /// </summary>
        private void ResizeGrid(int newWidth, int newHeight)
        {
            var oldWidth = _widthProp.intValue;
            var oldHeight = _heightProp.intValue;

            // 既存データを退避
            var oldCells = new bool[oldWidth * oldHeight];
            for (var i = 0; i < _cellsProp.arraySize && i < oldCells.Length; i++)
            {
                oldCells[i] = _cellsProp.GetArrayElementAtIndex(i).boolValue;
            }

            // サイズ更新
            _widthProp.intValue = newWidth;
            _heightProp.intValue = newHeight;
            _cellsProp.arraySize = newWidth * newHeight;

            // 初期化
            for (var i = 0; i < _cellsProp.arraySize; i++)
                _cellsProp.GetArrayElementAtIndex(i).boolValue = false;

            // 既存データをコピー
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

        /// <summary>
        /// 全セルを指定の値に設定する。
        /// </summary>
        private void SetAllCells(bool value)
        {
            Undo.RecordObject(target, value ? "Fill All Cells" : "Clear All Cells");
            for (var i = 0; i < _cellsProp.arraySize; i++)
                _cellsProp.GetArrayElementAtIndex(i).boolValue = value;
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 全セルの値を反転する。
        /// </summary>
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

        /// <summary>
        /// 左右反転する。
        /// </summary>
        private void FlipHorizontal()
        {
            Undo.RecordObject(target, "Flip Horizontal");
            var w = _widthProp.intValue;
            var h = _heightProp.intValue;
            var tempCells = ReadCells(w, h);

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var newIndex = y * w + (w - 1 - x);
                _cellsProp.GetArrayElementAtIndex(newIndex).boolValue = tempCells[y * w + x];
            }
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 上下反転する。
        /// </summary>
        private void FlipVertical()
        {
            Undo.RecordObject(target, "Flip Vertical");
            var w = _widthProp.intValue;
            var h = _heightProp.intValue;
            var tempCells = ReadCells(w, h);

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var newIndex = (h - 1 - y) * w + x;
                _cellsProp.GetArrayElementAtIndex(newIndex).boolValue = tempCells[y * w + x];
            }
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 90度時計回りに回転する（幅と高さが入れ替わる）。
        /// </summary>
        private void Rotate90()
        {
            Undo.RecordObject(target, "Rotate 90°");
            var w = _widthProp.intValue;
            var h = _heightProp.intValue;
            var tempCells = ReadCells(w, h);

            // 回転後のサイズ: 新しい幅 = 旧高さ, 新しい高さ = 旧幅
            var newW = h;
            var newH = w;

            _widthProp.intValue = newW;
            _heightProp.intValue = newH;
            _cellsProp.arraySize = newW * newH;

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                // (x, y) → (h-1-y, x) を新配列に格納
                var newX = h - 1 - y;
                var newY = x;
                var newIndex = newY * newW + newX;
                _cellsProp.GetArrayElementAtIndex(newIndex).boolValue = tempCells[y * w + x];
            }
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 現在のセル配列をbool[]として読み出す。
        /// </summary>
        private bool[] ReadCells(int w, int h)
        {
            var cells = new bool[w * h];
            for (var i = 0; i < _cellsProp.arraySize && i < cells.Length; i++)
                cells[i] = _cellsProp.GetArrayElementAtIndex(i).boolValue;
            return cells;
        }

        // =====================================================================
        //  プリセット適用
        // =====================================================================
        private void ApplyPreset(int index)
        {
            Undo.RecordObject(target, "Apply Preset");

            int w, h;
            bool[] cells;

            switch (index)
            {
                case 0: // 横棒 1×2
                    w = 2; h = 1;
                    cells = new[] { true, true };
                    break;
                case 1: // 縦棒 2×1
                    w = 1; h = 2;
                    cells = new[] { true, true };
                    break;
                case 2: // 横棒 1×3
                    w = 3; h = 1;
                    cells = new[] { true, true, true };
                    break;
                case 3: // 縦棒 3×1
                    w = 1; h = 3;
                    cells = new[] { true, true, true };
                    break;
                case 4: // 横棒 1×4
                    w = 4; h = 1;
                    cells = new[] { true, true, true, true };
                    break;
                case 5: // 縦棒 4×1
                    w = 1; h = 4;
                    cells = new[] { true, true, true, true };
                    break;
                case 6: // 2×2 正方形
                    w = 2; h = 2;
                    cells = new[] { true, true, true, true };
                    break;
                case 7: // 3×3 正方形
                    w = 3; h = 3;
                    cells = new[]
                    {
                        true, true, true,
                        true, true, true,
                        true, true, true
                    };
                    break;
                case 8: // L字
                    w = 2; h = 3;
                    cells = new[]
                    {
                        true, false,
                        true, false,
                        true, true,
                    };
                    break;
                case 9: // 逆L字
                    w = 2; h = 3;
                    cells = new[]
                    {
                        false, true,
                        false, true,
                        true, true,
                    };
                    break;
                case 10: // T字
                    w = 3; h = 2;
                    cells = new[]
                    {
                        true, true, true,
                        false, true, false,
                    };
                    break;
                case 11: // S字
                    w = 3; h = 2;
                    cells = new[]
                    {
                        false, true, true,
                        true, true, false,
                    };
                    break;
                case 12: // Z字
                    w = 3; h = 2;
                    cells = new[]
                    {
                        true, true, false,
                        false, true, true,
                    };
                    break;
                case 13: // 十字
                    w = 3; h = 3;
                    cells = new[]
                    {
                        false, true, false,
                        true, true, true,
                        false, true, false,
                    };
                    break;
                case 14: // コの字
                    w = 3; h = 2;
                    cells = new[]
                    {
                        true, true, true,
                        true, false, true,
                    };
                    break;
                default:
                    return;
            }

            _widthProp.intValue = w;
            _heightProp.intValue = h;
            _cellsProp.arraySize = w * h;
            for (var i = 0; i < cells.Length; i++)
                _cellsProp.GetArrayElementAtIndex(i).boolValue = cells[i];

            serializedObject.ApplyModifiedProperties();
        }
    }
}
