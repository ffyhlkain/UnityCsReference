// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Experimental.Rendering;

using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.AnimatedValues;

namespace UnityEditorInternal.FrameDebuggerInternal
{
    internal class FrameDebuggerEventDetailsView
    {
        // Render target view options
        [NonSerialized] private int m_RTIndex;
        [NonSerialized] private int m_MeshIndex = 0;
        [NonSerialized] private int m_RTIndexLastSet = 0;
        [NonSerialized] private int m_RTSelectedChannel;
        [NonSerialized] private float m_RTBlackLevel;
        [NonSerialized] private float m_RTWhiteLevel = 1.0f;
        [NonSerialized] private float m_RTBlackMinLevel;
        [NonSerialized] private float m_RTWhiteMaxLevel = 1.0f;

        // Private
        private int m_SelectedColorChannel = 0;
        private Vector2 m_ScrollViewVector = Vector2.zero;
        private Vector4 m_SelectedMask = Vector4.one;
        private AnimBool[] m_FoldoutAnimators = null;
        private MeshPreview m_Preview;
        private CachedEventDisplayData m_CachedEventData = null;
        private FrameDebuggerWindow m_FrameDebugger = null;
        private Lazy<FrameDebuggerEventData> m_CurEventData = new Lazy<FrameDebuggerEventData>(() => new FrameDebuggerEventData());
        private RenderTexture m_RenderTargetRenderTextureCopy = null;

        // Constants / Readonly
        private const int k_NumberGUISections = 11;
        private readonly string[] k_foldoutKeys = new string[]{
            "FrameDebuggerFoldout0",
            "FrameDebuggerFoldout1",
            "FrameDebuggerFoldout2",
            "FrameDebuggerFoldout3",
            "FrameDebuggerFoldout4",
            "FrameDebuggerFoldout5",
            "FrameDebuggerFoldout6",
            "FrameDebuggerFoldout7",
            "FrameDebuggerFoldout8",
            "FrameDebuggerFoldout9",
            "FrameDebuggerFoldout10",
        };

        // Properties
        private FrameDebuggerEvent curEvent { get; set; }
        private FrameDebuggerEventData curEventData => m_CurEventData.Value;
        private int curEventIndex => FrameDebuggerUtility.limit - 1;

        // Internal functions
        internal FrameDebuggerEventDetailsView(FrameDebuggerWindow frameDebugger)
        {
            m_FrameDebugger = frameDebugger;
        }

        internal void Reset()
        {
            m_RTSelectedChannel = 0;
            m_SelectedColorChannel = 0;
            m_RTIndex = 0;
            m_RTBlackLevel = 0.0f;
            m_RTBlackMinLevel = 0.0f;
            m_RTWhiteLevel = 1.0f;
            m_RTWhiteMaxLevel = 1.0f;
        }

        internal void OnNewFrameEventSelected()
        {
            m_MeshIndex = 0;
            m_Preview?.Dispose();
            m_Preview = null;
        }

        internal void OnDisable()
        {
            if (m_CachedEventData != null)
                m_CachedEventData.OnDisable();

            // Release the texture...
            FrameDebuggerHelper.ReleaseTemporaryTexture(ref m_RenderTargetRenderTextureCopy);

            m_Preview?.Dispose();
            m_Preview = null;
            Reset();
        }

        internal void DrawEventDetails(Rect rect, FrameDebuggerEvent[] descs, bool isDebuggingEditor)
        {
            // Early out if the frame is not valid
            if (!FrameDebuggerHelper.IsAValidFrame(curEventIndex, descs.Length))
                return;

            // Making sure we only initialize once. We do that in the Layout event, which is called before repaint
            if (Event.current.type == EventType.Layout)
                Initialize(curEventIndex, descs);

            if (m_CachedEventData == null)
                return;

            // Make sure the window is scrollable...
            GUILayout.BeginArea(rect);
            m_ScrollViewVector = EditorGUILayout.BeginScrollView(m_ScrollViewVector);

            // Toolbar
            Profiler.BeginSample("DrawToolbar");
            DrawRenderTargetToolbar();
            Profiler.EndSample();

            // Title
            Profiler.BeginSample("DrawTitle");
            GUILayout.BeginHorizontal(FrameDebuggerStyles.EventDetails.s_TitleHorizontalStyle);
            EditorGUILayout.LabelField(m_CachedEventData.m_Title, FrameDebuggerStyles.EventDetails.s_TitleStyle);
            GUILayout.EndHorizontal();
            if (Event.current.type == EventType.ContextClick)
                ShaderPropertyCopyValueMenu(GUILayoutUtility.GetLastRect(), FrameDebuggerStyles.EventDetails.s_CopyEventText, () => m_CachedEventData.copyString);
            Profiler.EndSample();

            // Output & Mesh
            // We disable Output and Mesh for Compute and Ray Tracing events
            bool shouldDrawOutput = !m_CachedEventData.m_IsComputeEvent && !m_CachedEventData.m_IsRayTracingEvent;
            Profiler.BeginSample("DrawOutputAndMesh");
            DrawOutputFoldout(rect, shouldDrawOutput, isDebuggingEditor);
            Profiler.EndSample();

            // Event Details
            Profiler.BeginSample("DrawDetails");
            DrawDetails(rect);
            Profiler.EndSample();

            Profiler.BeginSample("DrawMeshFoldout");
            bool shouldDrawMesh = shouldDrawOutput && !FrameDebugger.IsRemoteEnabled();
            DrawMeshFoldout(rect, shouldDrawMesh, isDebuggingEditor);
            Profiler.EndSample();

            ShaderPropertyCollection[] shaderProperties = m_CachedEventData.m_ShaderProperties;
            if (shaderProperties != null && shaderProperties.Length > 0)
            {
                // Shader keywords & properties...
                Profiler.BeginSample("DrawKeywords");
                DrawShaderData(ShaderPropertyType.Keyword, 2, FrameDebuggerStyles.EventDetails.s_FoldoutKeywordsText, m_CachedEventData.m_Keywords);
                Profiler.EndSample();

                Profiler.BeginSample("DrawTextureProperties");
                DrawShaderData(ShaderPropertyType.Texture, 3, FrameDebuggerStyles.EventDetails.s_FoldoutTexturesText, m_CachedEventData.m_Textures);
                Profiler.EndSample();

                Profiler.BeginSample("DrawIntProperties");
                DrawShaderData(ShaderPropertyType.Int, 4, FrameDebuggerStyles.EventDetails.s_FoldoutIntsText, m_CachedEventData.m_Ints);
                Profiler.EndSample();

                Profiler.BeginSample("DrawFloatProperties");
                DrawShaderData(ShaderPropertyType.Float, 5, FrameDebuggerStyles.EventDetails.s_FoldoutFloatsText, m_CachedEventData.m_Floats);
                Profiler.EndSample();

                Profiler.BeginSample("DrawVectorProperties");
                DrawShaderData(ShaderPropertyType.Vector, 6, FrameDebuggerStyles.EventDetails.s_FoldoutVectorsText, m_CachedEventData.m_Vectors);
                Profiler.EndSample();

                Profiler.BeginSample("DrawMatrixProperties");
                DrawShaderData(ShaderPropertyType.Matrix, 7, FrameDebuggerStyles.EventDetails.s_FoldoutMatricesText, m_CachedEventData.m_Matrices);
                Profiler.EndSample();

                Profiler.BeginSample("DrawBufferProperties");
                DrawShaderData(ShaderPropertyType.Buffer, 8, FrameDebuggerStyles.EventDetails.s_FoldoutBuffersText, m_CachedEventData.m_Buffers);
                Profiler.EndSample();

                Profiler.BeginSample("DrawConstantBufferProperties");
                DrawShaderData(ShaderPropertyType.CBuffer, 9, FrameDebuggerStyles.EventDetails.s_FoldoutCBufferText, m_CachedEventData.m_CBuffers);
                Profiler.EndSample();
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        ///////////////////////////////////////////////
        // PRIVATE
        ///////////////////////////////////////////////

        private void Initialize(int curEventIndex, FrameDebuggerEvent[] descs)
        {
            Profiler.BeginSample("Initialize");

            uint eventDataHash = FrameDebuggerUtility.eventDataHash;
            bool isReceivingFrameEventData = FrameDebugger.IsRemoteEnabled() && FrameDebuggerUtility.receivingRemoteFrameEventData;
            bool isFrameEventDataValid = curEventIndex == curEventData.m_FrameEventIndex;

            if (!isFrameEventDataValid
                 || m_CachedEventData == null
                 || !m_CachedEventData.m_IsValid
                 || m_CachedEventData.m_Index != curEventIndex
                 || (eventDataHash != 0 && (eventDataHash != m_CachedEventData.m_Hash))
                )
            {
                if (m_CachedEventData == null)
                    m_CachedEventData = new CachedEventDisplayData();

                // Release the texture...
                FrameDebuggerHelper.ReleaseTemporaryTexture(ref m_RenderTargetRenderTextureCopy);

                isFrameEventDataValid = FrameDebuggerUtility.GetFrameEventData(curEventIndex, curEventData);
                m_CachedEventData.m_IsValid = false;
            }

            // event type and draw call info
            curEvent = descs[curEventIndex];
            FrameEventType eventType = curEvent.m_Type;

            // Rebuild strings...
            if (isFrameEventDataValid)
            {
                if (!m_CachedEventData.m_IsValid || m_CachedEventData.m_Index != curEventIndex || m_CachedEventData.m_Type != eventType)
                {
                    m_CachedEventData.m_Hash = eventDataHash;
                    m_CachedEventData.Initialize(curEvent, curEventData);
                }
            }

            if (m_FoldoutAnimators == null || m_FoldoutAnimators.Length == 0)
            {
                m_FoldoutAnimators = new AnimBool[k_NumberGUISections];
                for (int i = 0; i < m_FoldoutAnimators.Length; i++)
                {
                    bool val = EditorPrefs.HasKey(k_foldoutKeys[i]) ? EditorPrefs.GetBool(k_foldoutKeys[i]) : false;
                    m_FoldoutAnimators[i] = new AnimBool(val);
                }
            }
            Profiler.EndSample();
        }

        private void DrawRenderTargetToolbar()
        {
            if (m_CachedEventData.m_IsRayTracingEvent)
                return;

            bool isDepthOnlyRT = GraphicsFormatUtility.IsDepthFormat((GraphicsFormat)curEventData.m_RenderTargetFormat);
            bool isClearAction = (int)curEvent.m_Type <= 7;
            bool hasShowableDepth = (curEventData.m_RenderTargetHasDepthTexture != 0);
            bool hasStencil = (curEventData.m_RenderTargetHasStencilBits != 0);
            int showableRTCount = curEventData.m_RenderTargetCount;

            if (hasShowableDepth)
                showableRTCount++;

            GUILayout.BeginHorizontal(FrameDebuggerStyles.EventToolbar.s_HorizontalStyle);

            // MRT to show
            EditorGUI.BeginChangeCheck();
            GUI.enabled = showableRTCount > 1;

            var rtNames = new GUIContent[showableRTCount + (hasStencil ? 1 : 0)];
            for (var i = 0; i < showableRTCount; ++i)
                rtNames[i] = FrameDebuggerStyles.EventToolbar.s_MRTLabels[i];

            if (hasShowableDepth)
                rtNames[curEventData.m_RenderTargetCount] = FrameDebuggerStyles.EventToolbar.s_DepthLabel;

            if (hasStencil)
                rtNames[rtNames.Length - 1] = FrameDebuggerStyles.EventToolbar.s_StencilLabel;

            // Render Target Selection
            // --------------------------------
            // The UI Dropdown and FrameDebugger use different
            // indices so we need to convert between the two:
            //              Dropdown           Frame Debugger
            // Depth   | Penultimate item   |       -1
            // Stencil | Last item          |       -2

            // If we showed depth/stencil before then try to keep showing depth/stencil
            // otherwise try to keep showing color
            if (m_RTIndexLastSet == -1)
                m_RTIndex = hasShowableDepth ? showableRTCount - 1 : 0;
            else if (m_RTIndexLastSet == -2)
                m_RTIndex = hasStencil ? showableRTCount : 0;
            else if (m_RTIndex > curEventData.m_RenderTargetCount)
                m_RTIndex = 0;

            m_RTIndex = EditorGUILayout.Popup(m_RTIndex, rtNames, FrameDebuggerStyles.EventToolbar.s_PopupLeftStyle, GUILayout.Width(70));

            int rtIndexToSet = m_RTIndex;
            if (hasShowableDepth && rtIndexToSet == (showableRTCount - 1))
                rtIndexToSet = -1;

            if (hasStencil && rtIndexToSet == showableRTCount)
                rtIndexToSet = showableRTCount > 1 ? -2 : -1;

            // --------------------------------

            GUI.enabled = !isDepthOnlyRT;

            // color channels
            EditorGUILayout.Space(5f);
            GUILayout.Label(FrameDebuggerStyles.EventToolbar.s_ChannelHeader, FrameDebuggerStyles.EventToolbar.s_ChannelHeaderStyle);
            EditorGUILayout.Space(5f);

            int channelToDisplay = 0;
            bool forceUpdate = false;
            
            // Negative RT index: display depth or stencil buffer
            bool isDepthOrStencilSelected = m_RTIndexLastSet < 0;

            bool shouldDisableChannelButtons = isDepthOnlyRT || isClearAction || isDepthOrStencilSelected;
            UInt32 componentCount = GraphicsFormatUtility.GetComponentCount((GraphicsFormat)curEventData.m_RenderTargetFormat);
            GUILayout.BeginHorizontal();
            {
                GUI.enabled = !shouldDisableChannelButtons && m_SelectedColorChannel != 0;
                if (GUILayout.Button(FrameDebuggerStyles.EventToolbar.s_ChannelAll, FrameDebuggerStyles.EventToolbar.s_ChannelAllStyle)) { m_RTSelectedChannel = 0; }

                GUI.enabled = !shouldDisableChannelButtons && componentCount > 0 && m_SelectedColorChannel != 1;
                if (GUILayout.Button(FrameDebuggerStyles.EventToolbar.s_ChannelR, FrameDebuggerStyles.EventToolbar.s_ChannelStyle)) { m_RTSelectedChannel = 1; }

                GUI.enabled = !shouldDisableChannelButtons && componentCount > 1 && m_SelectedColorChannel != 2;
                if (GUILayout.Button(FrameDebuggerStyles.EventToolbar.s_ChannelG, FrameDebuggerStyles.EventToolbar.s_ChannelStyle)) { m_RTSelectedChannel = 2; }

                GUI.enabled = !shouldDisableChannelButtons && componentCount > 2 && m_SelectedColorChannel != 3;
                if (GUILayout.Button(FrameDebuggerStyles.EventToolbar.s_ChannelB, FrameDebuggerStyles.EventToolbar.s_ChannelStyle)) { m_RTSelectedChannel = 3; }

                GUI.enabled = !shouldDisableChannelButtons && componentCount > 3 && m_SelectedColorChannel != 4;
                if (GUILayout.Button(FrameDebuggerStyles.EventToolbar.s_ChannelA, FrameDebuggerStyles.EventToolbar.s_ChannelAStyle)) { m_RTSelectedChannel = 4; }

                // Force the channel to be "All" when:
                // * Showing Shadows/Depth/Clear
                // * Channel index is higher then the number available channels
                bool shouldForceAll = (m_RTSelectedChannel != 0 && (shouldDisableChannelButtons || m_RTSelectedChannel < 4 && componentCount < m_RTSelectedChannel));
                channelToDisplay = shouldForceAll ? 0 : m_RTSelectedChannel;

                if (channelToDisplay != m_SelectedColorChannel)
                {
                    forceUpdate = true;
                    m_SelectedColorChannel = channelToDisplay;
                }

                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();

            GUI.enabled = true;

            // levels
            GUILayout.BeginHorizontal(FrameDebuggerStyles.EventToolbar.s_LevelsHorizontalStyle);
            GUILayout.Label(FrameDebuggerStyles.EventToolbar.s_LevelsHeader);

            float blackMinLevel = EditorGUILayout.DelayedFloatField(m_RTBlackMinLevel, GUILayout.MaxWidth(40.0f));
            float blackLevel = m_RTBlackLevel;
            float whiteLevel = m_RTWhiteLevel;
            EditorGUILayout.MinMaxSlider(ref blackLevel, ref whiteLevel, m_RTBlackMinLevel, m_RTWhiteMaxLevel, GUILayout.MaxWidth(200.0f));
            float whiteMaxLevel = EditorGUILayout.DelayedFloatField(m_RTWhiteMaxLevel, GUILayout.MaxWidth(40.0f));

            if (blackMinLevel < whiteMaxLevel)
            {
                m_RTBlackMinLevel = blackMinLevel;
                m_RTWhiteMaxLevel = whiteMaxLevel;

                m_RTBlackLevel = Mathf.Clamp(blackLevel, m_RTBlackMinLevel, whiteLevel);
                m_RTWhiteLevel = Mathf.Clamp(whiteLevel, blackLevel, m_RTWhiteMaxLevel);
            }

            if (EditorGUI.EndChangeCheck()
                || (!isDepthOnlyRT && (rtIndexToSet != m_RTIndexLastSet))
                || forceUpdate)
            {
                m_SelectedMask = Vector4.zero;
                switch (channelToDisplay)
                {
                    case 1: m_SelectedMask.x = 1f; break;
                    case 2: m_SelectedMask.y = 1f; break;
                    case 3: m_SelectedMask.z = 1f; break;
                    case 4: m_SelectedMask.w = 1f; break;
                    case 5: m_SelectedMask = Vector4.zero; break;
                    default: m_SelectedMask = Vector4.one; break;
                }

                FrameDebuggerUtility.SetRenderTargetDisplayOptions(rtIndexToSet, m_SelectedMask, m_RTBlackLevel, m_RTWhiteLevel);
                m_FrameDebugger.RepaintAllNeededThings();
                m_RTIndexLastSet = rtIndexToSet;
                FrameDebuggerHelper.ReleaseTemporaryTexture(ref m_RenderTargetRenderTextureCopy);
            }
            else if (m_RTIndexLastSet == -2 && showableRTCount <= 1)
            {
                m_RTIndexLastSet = -1;
                FrameDebuggerUtility.SetRenderTargetDisplayOptions(-1, m_SelectedMask, m_RTBlackLevel, m_RTWhiteLevel);
            }

            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.enabled = true;
        }

        private void DrawOutputFoldout(Rect rect, bool shouldDrawOutput, bool isDebuggingEditor)
        {
            if (BeginFoldoutBox(0, shouldDrawOutput, FrameDebuggerStyles.EventDetails.s_FoldoutOutputText, out float fadePercent, null))
            {
                if (shouldDrawOutput)
                {
                    EditorGUILayout.BeginVertical();
                    {
                        float viewportWidth = rect.width - 30f;
                        float viewportHeightFaded = FrameDebuggerStyles.EventDetails.k_MaxViewportHeight * fadePercent;
                        float renderTargetWidth = m_CachedEventData.m_RenderTargetWidth;
                        float renderTargetHeight = m_CachedEventData.m_RenderTargetHeight;

                        float scaledRenderTargetWidth = renderTargetWidth;
                        float scaledRenderTargetHeight = renderTargetHeight;

                        if (scaledRenderTargetWidth > viewportWidth)
                        {
                            float scale = viewportWidth / scaledRenderTargetWidth;
                            scaledRenderTargetWidth *= scale;
                            scaledRenderTargetHeight *= scale;
                        }

                        if (scaledRenderTargetHeight > FrameDebuggerStyles.EventDetails.k_MaxViewportHeight)
                        {
                            float scale = FrameDebuggerStyles.EventDetails.k_MaxViewportHeight / scaledRenderTargetHeight;
                            scaledRenderTargetWidth *= scale;
                            scaledRenderTargetHeight = FrameDebuggerStyles.EventDetails.k_MaxViewportHeight;
                        }

                        DrawTargetTexture(rect, viewportWidth, viewportHeightFaded, renderTargetWidth, renderTargetHeight, scaledRenderTargetWidth, scaledRenderTargetHeight, fadePercent, isDebuggingEditor);
                    }
                    GUILayout.EndVertical();
                }
            }
            EndFoldoutBox();
        }

        private void DrawTargetTexture(Rect rect, float viewportWidth, float viewportHeight, float renderTargetWidth, float renderTargetHeight, float scaledRenderTargetWidth, float scaledRenderTargetHeight, float fadePercent, bool isDebuggingEditor)
        {
            EditorGUILayout.BeginHorizontal(FrameDebuggerStyles.EventDetails.s_RenderTargetMeshBackgroundStyle);
            Rect previewRect = GUILayoutUtility.GetRect(viewportWidth, viewportHeight);

            // Early out if the texture is so small it can not be drawn...
            int scaledTexWidthInt = (int) scaledRenderTargetWidth;
            int scaledTexHeightInt = (int) scaledRenderTargetHeight;
            if (scaledTexWidthInt <= 0 || scaledTexHeightInt <= 0)
            {
                EditorGUILayout.EndHorizontal();
                return;
            }

            // We insert a dummy Draw Texture call when not repainting.
            if (Event.current.type != EventType.Repaint)
            {
                GUI.DrawTexture(Rect.zero, null, ScaleMode.ScaleAndCrop, false, (scaledRenderTargetWidth / scaledRenderTargetHeight));
                EditorGUILayout.EndHorizontal();
                return;
            }

            float yPos = previewRect.y;
            if (viewportHeight > scaledRenderTargetHeight)
                yPos += (viewportHeight - scaledRenderTargetHeight) * 0.5f;

            float xPos = 10f + Mathf.Max(viewportWidth * 0.5f - scaledRenderTargetWidth * 0.5f, 0f);

            // This is a weird one. When opening/closing the foldout, the image
            // shifts a tiny bit to the right. This magic code prevents that.
            if (fadePercent < 1f)
                xPos -= 7f;

            Rect textureRect = new Rect(xPos, yPos, scaledRenderTargetWidth, scaledRenderTargetHeight);

            if (m_RenderTargetRenderTextureCopy)
            {
                GUI.DrawTexture(textureRect, m_RenderTargetRenderTextureCopy, ScaleMode.ScaleAndCrop, false, (scaledRenderTargetWidth / scaledRenderTargetHeight));
            }
            else if (m_CachedEventData.m_RenderTargetRenderTexture && previewRect.height > 1.0f)
            {
                GraphicsFormat targetTextureFormat = m_CachedEventData.m_RenderTargetFormat;
                uint componentCount = GraphicsFormatUtility.GetComponentCount(targetTextureFormat);

                // On some devices the backbuffer gives the None Graphics Format. To prevent us
                // displaying a black & white texture we force the channels to the selected mask.
                bool shouldForceSelectedMask = targetTextureFormat == GraphicsFormat.None && m_CachedEventData.m_RenderTargetIsBackBuffer;
                Vector4 channels = (!m_CachedEventData.m_RenderTargetIsDepthOnlyRT && (componentCount > 1 || shouldForceSelectedMask)) ? m_SelectedMask : new Vector4(1, 0, 0, 0);

                bool linearColorSpace = QualitySettings.activeColorSpace == ColorSpace.Linear;
                bool textureSRGB = GraphicsFormatUtility.IsSRGBFormat(targetTextureFormat);
                bool undoOutputSRGB = (isDebuggingEditor && (!linearColorSpace || textureSRGB)) ? false : true;
                bool shouldYFlip = m_CachedEventData.m_RenderTargetIsBackBuffer && isDebuggingEditor && SystemInfo.graphicsUVStartsAtTop;
                Vector4 levels = new Vector4(m_RTBlackLevel, m_RTWhiteLevel, 0f, 0f);

                // Get a temporary texture...
                int renderTargetWidthInt = (int)renderTargetWidth;
                int renderTargetHeightInt = (int)renderTargetHeight;
                m_RenderTargetRenderTextureCopy = RenderTexture.GetTemporary(renderTargetWidthInt, renderTargetHeightInt);

                // Blit with the settings from the toolbar...
                FrameDebuggerHelper.BlitToRenderTexture(
                    ref m_CachedEventData.m_RenderTargetRenderTexture,
                    ref m_RenderTargetRenderTextureCopy,
                    renderTargetWidthInt,
                    renderTargetHeightInt,
                    channels,
                    levels,
                    shouldYFlip,
                    undoOutputSRGB
                );

                // Draw the texture to the screen...
                GUI.DrawTexture(textureRect, m_RenderTargetRenderTextureCopy, ScaleMode.ScaleAndCrop, false, (scaledRenderTargetWidth / scaledRenderTargetHeight));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawMeshFoldout(Rect rect, bool shouldDrawMesh, bool isDebuggingEditor)
        {
            // Not supported on remote players so we change the foldout header to show that.
            GUIContent header;
            bool shouldDraw;
            if (FrameDebugger.IsRemoteEnabled())
            {
                shouldDraw = false;
                header = FrameDebuggerStyles.EventDetails.s_FoldoutMeshNotSupportedText;
            }
            else
            {
                shouldDraw = shouldDrawMesh && m_CachedEventData.m_Meshes != null && m_CachedEventData.m_Meshes.Length > 0;
                header = FrameDebuggerStyles.EventDetails.s_FoldoutMeshText;
            }


            if (BeginFoldoutBox(10, shouldDraw, header, out float fadePercent, null) && shouldDraw)
            {
                // Safety checks as things can get go wrong when switching between editor and remote...
                m_MeshIndex = Mathf.Min(m_MeshIndex, m_CachedEventData.m_Meshes.Length - 1);
                for (int i = 0; i < m_CachedEventData.m_Meshes.Length; i++)
                {
                    if (m_CachedEventData.m_Meshes[i] == null)
                    {
                        EndFoldoutBox();
                        return;
                    }
                }

                // Draw the Mesh Preview
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                {
                    float viewportWidth = rect.width - 30f;
                    float viewportHeightFaded = FrameDebuggerStyles.EventDetails.k_MaxViewportHeight * fadePercent;
                    float renderTargetWidth = m_CachedEventData.m_RenderTargetWidth;
                    float renderTargetHeight = m_CachedEventData.m_RenderTargetHeight;

                    float scaledRenderTargetWidth = renderTargetWidth;
                    float scaledRenderTargetHeight = renderTargetHeight;

                    if (scaledRenderTargetWidth > viewportWidth)
                    {
                        float scale = viewportWidth / scaledRenderTargetWidth;
                        scaledRenderTargetWidth *= scale;
                        scaledRenderTargetHeight *= scale;
                    }

                    if (scaledRenderTargetHeight > FrameDebuggerStyles.EventDetails.k_MaxViewportHeight)
                    {
                        float scale = FrameDebuggerStyles.EventDetails.k_MaxViewportHeight / scaledRenderTargetHeight;
                        scaledRenderTargetWidth *= scale;
                        scaledRenderTargetHeight = FrameDebuggerStyles.EventDetails.k_MaxViewportHeight;
                    }

                    DrawEventMesh(viewportWidth, viewportHeightFaded, scaledRenderTargetWidth);
                }
                GUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                // Draw the list of meshes to select from...
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(FrameDebuggerStyles.EventDetails.s_FoldoutMeshText, FrameDebuggerStyles.EventDetails.s_MonoLabelStyle);
                EditorGUILayout.EndHorizontal();

                bool clicked = false;
                GUIStyle style;
                for (int i = 0; i < m_CachedEventData.m_Meshes.Length; i++)
                {
                    if (m_CachedEventData.m_Meshes[i] == null)
                        continue;

                    style = (i == m_MeshIndex) ? FrameDebuggerStyles.EventDetails.s_MonoLabelBoldPaddingStyle : FrameDebuggerStyles.EventDetails.s_MonoLabelStylePadding;

                    // Draw...
                    EditorGUILayout.BeginHorizontal();
                        // Mesh Name to select for the preview...
                        clicked = GUILayout.Button(m_CachedEventData.m_Meshes[i].name, style);

                        // Object Field to ping the object in the Project View
                        GUI.enabled = false;
                        EditorGUILayout.ObjectField(m_CachedEventData.m_Meshes[i], typeof(Mesh), true, GUILayout.Width(FrameDebuggerStyles.EventDetails.k_ShaderObjectFieldWidth));
                        GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();

                    // Update the mesh index if the label was clicked...
                    if (clicked)
                        m_MeshIndex = Mathf.Min(i, m_CachedEventData.m_Meshes.Length - 1);
                }

                if (m_Preview != null && m_CachedEventData.m_Meshes[m_MeshIndex] != null)
                    m_Preview.mesh = m_CachedEventData.m_Meshes[m_MeshIndex];
            }
            EndFoldoutBox();
        }

        private void DrawEventMesh(float viewportWidth, float viewportHeight, float texWidth)
        {
            if (viewportHeight - FrameDebuggerStyles.EventDetails.k_MeshBottomToolbarHeight < 1.0f)
                return;

            if (m_CachedEventData.m_Meshes == null || m_CachedEventData.m_Meshes.Length == 0 || m_CachedEventData.m_Meshes[m_MeshIndex] == null)
            {
                DrawEventMeshBackground(viewportWidth, viewportHeight);
                return;
            }

            if (m_Preview == null)
                m_Preview = new MeshPreview(m_CachedEventData.m_Meshes[m_MeshIndex]);
            else if (m_Preview.mesh == null)
                m_Preview.mesh = m_CachedEventData.m_Meshes[m_MeshIndex];

            // We need this rect called here to push the control buttons below the Mesh...
            Rect previewRect = GUILayoutUtility.GetRect(viewportWidth - 100, viewportHeight - FrameDebuggerStyles.EventDetails.k_MeshBottomToolbarHeight, GUILayout.ExpandHeight(false));

            // Rectangle for the buttons...
            Rect rect = EditorGUILayout.BeginHorizontal(GUIContent.none, EditorStyles.toolbar, GUILayout.Height(FrameDebuggerStyles.EventDetails.k_MeshBottomToolbarHeight));
            {
                GUILayout.FlexibleSpace();
                m_Preview.OnPreviewSettings();
            }
            EditorGUILayout.EndHorizontal();

            var evt = Event.current;
            if (FrameDebuggerHelper.IsHoveringRect(previewRect) || evt.type != EventType.ScrollWheel)
            {
                m_Preview?.OnPreviewGUI(previewRect, EditorStyles.helpBox);
            }
        }

        private void DrawEventMeshBackground(float viewportWidth, float viewportHeight)
        {
            EditorGUILayout.BeginHorizontal(FrameDebuggerStyles.EventDetails.s_RenderTargetMeshBackgroundStyle, GUILayout.Width(viewportWidth));
            GUILayoutUtility.GetRect(viewportWidth, viewportHeight);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDetails(Rect rect)
        {
            bool isFoldoutOpen = BeginFoldoutBox(1, true, FrameDebuggerStyles.EventDetails.s_FoldoutEventDetailsText, out float fadePercent, () => m_CachedEventData.detailsCopyString);
            if (!isFoldoutOpen)
            {
                EndFoldoutBox();
                return;
            }

            GUIStyle style = FrameDebuggerStyles.EventDetails.s_MonoLabelStyle;

            // Size, Color Actions, Blending, Z, Stencil...
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(m_CachedEventData.details, style, GUILayout.MinWidth(m_CachedEventData.m_DetailsGUIWidth), GUILayout.MinHeight(m_CachedEventData.m_DetailsGUIHeight));
            EditorGUILayout.EndHorizontal();

            // Shader
            if (m_CachedEventData.m_ShouldDisplayRealAndOriginalShaders)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(FrameDebuggerStyles.EventDetails.s_RealShaderText, style, GUILayout.Width(FrameDebuggerStyles.EventDetails.k_ShaderLabelWidth));
                if (m_CachedEventData.m_RealShader == null)
                    EditorGUILayout.LabelField(m_CachedEventData.m_RealShaderName, style);
                else
                {
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField(m_CachedEventData.m_RealShader, typeof(Shader), true, GUILayout.Width(FrameDebuggerStyles.EventDetails.k_ShaderObjectFieldWidth));
                    GUI.enabled = true;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(FrameDebuggerStyles.EventDetails.s_OriginalShaderText, GUILayout.Width(FrameDebuggerStyles.EventDetails.k_ShaderLabelWidth));
                if (m_CachedEventData.m_OriginalShader == null)
                    EditorGUILayout.LabelField(m_CachedEventData.m_OriginalShaderName, style);
                else
                {
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField(m_CachedEventData.m_OriginalShader, typeof(Shader), true, GUILayout.Width(FrameDebuggerStyles.EventDetails.k_ShaderObjectFieldWidth));
                    GUI.enabled = true;
                }
                EditorGUILayout.EndHorizontal();
            }

            EndFoldoutBox();
        }

        private void DrawShaderData(ShaderPropertyType propType, int foldoutIndex, GUIContent foldoutText, ShaderPropertyCollection shaderProperties)
        {
            ShaderPropertyDisplayInfo[] propertyDisplayInfo = shaderProperties.m_Data;

            // We disable and hide keywords and shader properties for clear and resolve events or when we don't have any data.
            bool shouldDisplayProperties = !m_CachedEventData.m_IsClearEvent && !m_CachedEventData.m_IsResolveEvent;
            bool shouldDraw = shouldDisplayProperties && propertyDisplayInfo != null && propertyDisplayInfo.Length > 0;
            bool isFoldoutOpen = BeginFoldoutBox(foldoutIndex, shouldDraw, foldoutText, out float fadePercent, () => shaderProperties.copyString);

            if (!shouldDraw || !isFoldoutOpen)
            {
                EndFoldoutBox();
                return;
            }

            GUILayout.BeginVertical(FrameDebuggerStyles.EventDetails.s_PropertiesBottomMarginStyle);

            GUILayout.BeginHorizontal(FrameDebuggerStyles.EventDetails.s_PropertiesLeftMarginStyle);
            GUILayout.Label(shaderProperties.m_Header, FrameDebuggerStyles.EventDetails.s_MonoLabelBoldStyle);
            GUILayout.EndHorizontal();

            for (int i = 0; i < propertyDisplayInfo.Length; i++)
            {
                ShaderPropertyDisplayInfo data = propertyDisplayInfo[i];
                if (!data.m_IsArray)
                {
                    Texture textureToDisplay = data.m_TextureCopy != null ? data.m_TextureCopy as Texture : data.m_Texture;
                    if (textureToDisplay == null)
                    {
                        GUILayout.BeginHorizontal(FrameDebuggerStyles.EventDetails.s_PropertiesLeftMarginStyle);
                        GUILayout.Label(data.m_PropertyString, FrameDebuggerStyles.EventDetails.s_MonoLabelNoWrapStyle);
                    }
                    else
                    {
                        GUILayout.BeginHorizontal();

                        // Texture Preview..
                        // for 2D textures, we want to display them directly as a preview (this will make render textures display their contents) but
                        // for cube maps and other non-2D types DrawPreview does not do anything useful right now, so get their asset type icon at least
                        bool isTex2D = textureToDisplay.dimension == TextureDimension.Tex2D;
                        Texture previewTexture = isTex2D ? textureToDisplay : AssetPreview.GetMiniThumbnail(textureToDisplay);
                        Rect previewRect = GUILayoutUtility.GetRect(10, 10, FrameDebuggerStyles.EventDetails.s_TextureButtonStyle);
                        previewRect.width = 10;
                        previewRect.height = 10;
                        previewRect.x += 4f;
                        previewRect.y += 6f;

                        GUI.DrawTexture(previewRect, previewTexture, ScaleMode.StretchToFill, false);
                        GUILayout.Label(data.m_PropertyString, FrameDebuggerStyles.EventDetails.s_MonoLabelNoWrapStyle);

                        if (FrameDebuggerHelper.IsCurrentEventMouseDown() && FrameDebuggerHelper.IsClickingRect(previewRect))
                        {
                            PopupWindowWithoutFocus.Show(
                                previewRect,
                                new ObjectPreviewPopup(textureToDisplay),
                                new[] { PopupLocation.Left, PopupLocation.Below, PopupLocation.Right }
                            );
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginVertical(FrameDebuggerStyles.EventDetails.s_PropertiesLeftMarginStyle);

                    data.m_IsFoldoutOpen = EditorGUILayout.Foldout(data.m_IsFoldoutOpen, data.m_FoldoutString, FrameDebuggerStyles.EventDetails.s_ArrayFoldoutStyle);
                    if (data.m_IsFoldoutOpen)
                        GUILayout.Label(data.m_PropertyString, data.m_ArrayGUIStyle);

                    GUILayout.EndVertical();
                }
                propertyDisplayInfo[i] = data;

                if (Event.current.type == EventType.ContextClick)
                    ShaderPropertyCopyValueMenu(GUILayoutUtility.GetLastRect(), FrameDebuggerStyles.EventDetails.s_CopyPropertyText, () => data.copyString);
            }
            GUILayout.EndVertical();
            EndFoldoutBox();
        }

        private bool BeginFoldoutBox(int foldoutIndex, bool hasData, GUIContent header, out float fadePercent, Func<string> copyStringAction = null)
        {
            GUI.enabled = hasData;

            EditorGUILayout.BeginVertical(FrameDebuggerStyles.EventDetails.s_FoldoutCategoryBoxStyle);
            Rect r = GUILayoutUtility.GetRect(2, 21);

            EditorGUI.BeginChangeCheck();
            bool expanded = EditorGUI.FoldoutTitlebar(r, header, m_FoldoutAnimators[foldoutIndex].target, true, EditorStyles.inspectorTitlebarFlat, EditorStyles.inspectorTitlebarText);
            if (EditorGUI.EndChangeCheck())
            {
                bool newState = !m_FoldoutAnimators[foldoutIndex].target;
                EditorPrefs.SetBool(k_foldoutKeys[foldoutIndex], newState);

                // If Shift is being held down, we change the state for all of them...
                if (Event.current.shift || Event.current.alt)
                    for (int i = m_FoldoutAnimators.Length - 1; i >= 0; i--)
                        m_FoldoutAnimators[i].target = newState;
                else
                    m_FoldoutAnimators[foldoutIndex].target = newState;
            }

            if (Event.current.type == EventType.ContextClick)
                if (copyStringAction != null && FrameDebuggerStyles.EventDetails.s_FoldoutCopyText[foldoutIndex] != null && copyStringAction != null)
                    ShaderPropertyCopyValueMenu(r, FrameDebuggerStyles.EventDetails.s_FoldoutCopyText[foldoutIndex], copyStringAction);

            GUI.enabled = true;
            EditorGUI.indentLevel++;
            fadePercent = m_FoldoutAnimators[foldoutIndex].faded;

            return EditorGUILayout.BeginFadeGroup(m_FoldoutAnimators[foldoutIndex].faded);
        }

        private void EndFoldoutBox()
        {
            EditorGUILayout.EndFadeGroup();
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private void ShaderPropertyCopyValueMenu(Rect valueRect, GUIContent menuText, Func<string> textToCopy)
        {
            Profiler.BeginSample("ShaderPropertyCopyValueMenu");
            var e = Event.current;

            // Copy function
            if (valueRect.Contains(e.mousePosition))
            {
                e.Use();

                GenericMenu menu = new GenericMenu();
                menu.AddItem(menuText, false, delegate {
                    if (textToCopy != null)
                        EditorGUIUtility.systemCopyBuffer = textToCopy();
                });
                menu.ShowAsContext();
            }

            Profiler.EndSample();
        }
    }
}
