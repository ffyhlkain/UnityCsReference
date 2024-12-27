// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Profiling;

namespace UnityEngine.UIElements.UIR
{
    partial class RenderChain
    {
        class VisualChangesProcessor : IDisposable
        {
            enum VisualsProcessingType
            {
                Head,
                Tail
            }

            struct EntryProcessingInfo
            {
                public VisualElement visualElement;
                public VisualsProcessingType type;
                public Entry rootEntry;
            }

            static readonly ProfilerMarker k_GenerateEntriesMarker = new("UIR.GenerateEntries");
            static readonly ProfilerMarker k_ConvertEntriesToCommandsMarker = new("UIR.ConvertEntriesToCommands");
            static readonly ProfilerMarker k_UpdateOpacityIdMarker = new ("UIR.UpdateOpacityId");

            RenderChain m_RenderChain;
            MeshGenerationContext m_MeshGenerationContext;
            BaseElementBuilder m_ElementBuilder;
            List<EntryProcessingInfo> m_EntryProcessingList;
            List<EntryProcessor> m_Processors;

            public BaseElementBuilder elementBuilder => m_ElementBuilder;
            public MeshGenerationContext meshGenerationContext => m_MeshGenerationContext;

            public VisualChangesProcessor(RenderChain renderChain)
            {
                m_RenderChain = renderChain;
                m_MeshGenerationContext = new MeshGenerationContext(
                    m_RenderChain.meshWriteDataPool,
                    m_RenderChain.entryRecorder,
                    m_RenderChain.tempMeshAllocator,
                    m_RenderChain.meshGenerationDeferrer,
                    m_RenderChain.meshGenerationNodeManager);
                m_ElementBuilder = new DefaultElementBuilder(m_RenderChain);
                m_EntryProcessingList = new List<EntryProcessingInfo>();
                m_Processors = new List<EntryProcessor>(4);
            }

            public void ScheduleMeshGenerationJobs()
            {
                m_ElementBuilder.ScheduleMeshGenerationJobs(m_MeshGenerationContext);
            }

            public void ProcessOnVisualsChanged(VisualElement ve, uint dirtyID, ref ChainBuilderStats stats)
            {
                bool hierarchical = ve.renderChainData.pendingHierarchicalRepaint || (ve.renderChainData.dirtiedValues & RenderDataDirtyTypes.VisualsHierarchy) != 0;
                if (hierarchical)
                    stats.recursiveVisualUpdates++;
                else stats.nonRecursiveVisualUpdates++;
                DepthFirstOnVisualsChanged(ve, dirtyID, hierarchical, ref stats);
            }

            void DepthFirstOnVisualsChanged(VisualElement ve, uint dirtyID, bool hierarchical, ref ChainBuilderStats stats)
            {
                if (dirtyID == ve.renderChainData.dirtyID)
                    return;
                ve.renderChainData.dirtyID = dirtyID; // Prevent reprocessing of the same element in the same pass

                if (hierarchical)
                    stats.recursiveVisualUpdatesExpanded++;

                if (!ve.areAncestorsAndSelfDisplayed)
                {
                    if (hierarchical)
                        ve.renderChainData.pendingHierarchicalRepaint = true;
                    else
                        ve.renderChainData.pendingRepaint = true;
                    return;
                }

                ve.renderChainData.pendingHierarchicalRepaint = false;
                ve.renderChainData.pendingRepaint = false;

                if (!hierarchical && (ve.renderChainData.dirtiedValues & RenderDataDirtyTypes.AllVisuals) == RenderDataDirtyTypes.VisualsOpacityId)
                {
                    stats.opacityIdUpdates++;
                    UpdateOpacityId(ve, m_RenderChain);
                    return;
                }

                UpdateWorldFlipsWinding(ve);

                Debug.Assert(ve.renderChainData.clipMethod != ClipMethod.Undetermined);
                Debug.Assert(RenderChainVEData.AllocatesID(ve.renderChainData.transformID) || ve.hierarchy.parent == null || ve.renderChainData.transformID.Equals(ve.hierarchy.parent.renderChainData.transformID) || ve.renderChainData.isGroupTransform);

                if (ve is TextElement)
                    RenderEvents.UpdateTextCoreSettings(m_RenderChain, ve);

                if ((ve.renderHints & RenderHints.DynamicColor) == RenderHints.DynamicColor)
                    RenderEvents.SetColorValues(m_RenderChain, ve);

                var rootEntry = m_RenderChain.entryPool.Get();
                rootEntry.type = EntryType.DedicatedPlaceholder;

                m_EntryProcessingList.Add(new EntryProcessingInfo
                {
                    type = VisualsProcessingType.Head,
                    visualElement = ve,
                    rootEntry = rootEntry
                });

                k_GenerateEntriesMarker.Begin();
                m_MeshGenerationContext.Begin(rootEntry, ve);
                m_ElementBuilder.Build(m_MeshGenerationContext);
                m_MeshGenerationContext.End();
                k_GenerateEntriesMarker.End();

                if (hierarchical)
                {
                    // Recurse on children
                    int childrenCount = ve.hierarchy.childCount;
                    for (int i = 0; i < childrenCount; i++)
                        DepthFirstOnVisualsChanged(ve.hierarchy[i], dirtyID, true, ref stats);
                }

                m_EntryProcessingList.Add(new EntryProcessingInfo
                {
                    type = VisualsProcessingType.Tail,
                    visualElement = ve,
                    rootEntry = rootEntry
                });
            }

            // This can only be called when the element local and the parent world states are clean.
            static void UpdateWorldFlipsWinding(VisualElement ve)
            {
                bool flipsWinding = ve.renderChainData.localFlipsWinding;
                bool parentFlipsWinding = false;
                VisualElement parent = ve.hierarchy.parent;
                if (parent != null)
                    parentFlipsWinding = parent.renderChainData.worldFlipsWinding;

                ve.renderChainData.worldFlipsWinding = parentFlipsWinding ^ flipsWinding;
            }

            public void ConvertEntriesToCommands(ref ChainBuilderStats stats)
            {
                k_ConvertEntriesToCommandsMarker.Begin();

                // The depth from the VE that triggered a recursive visuals update. Not necessarily equal
                // to the depth of the VE in the hierarchy.
                int depth = 0;
                for (int i = 0; i < m_EntryProcessingList.Count; ++i)
                {
                    var processingInfo = m_EntryProcessingList[i];
                    if (processingInfo.type == VisualsProcessingType.Head)
                    {
                        EntryProcessor processor;
                        if (depth < m_Processors.Count)
                            processor = m_Processors[depth];
                        else
                        {
                            processor = new EntryProcessor();
                            m_Processors.Add(processor);
                        }

                        ++depth;
                        processor.Init(processingInfo.rootEntry, m_RenderChain, processingInfo.visualElement);
                        processor.ProcessHead();
                    }
                    else
                    {
                        --depth;
                        EntryProcessor processor = m_Processors[depth];
                        processor.ProcessTail();

                        bool hasCommands = processor.firstHeadCommand != null || processor.firstTailCommand != null;
                        if (hasCommands) { }

                        CommandManipulator.ReplaceCommands(m_RenderChain, processingInfo.visualElement, processor);
                    }
                }

                m_EntryProcessingList.Clear();

                for (int i = 0; i < m_Processors.Count; ++i)
                    m_Processors[i].ClearReferences();

                k_ConvertEntriesToCommandsMarker.End();
            }


            public static void UpdateOpacityId(VisualElement ve, RenderChain renderChain)
            {
                k_UpdateOpacityIdMarker.Begin();

                if (ve.renderChainData.headMesh != null)
                    DoUpdateOpacityId(ve, renderChain, ve.renderChainData.headMesh);

                if (ve.renderChainData.tailMesh != null)
                    DoUpdateOpacityId(ve, renderChain, ve.renderChainData.tailMesh);

                if (ve.renderChainData.hasExtraMeshes)
                {
                    ExtraRenderChainVEData extraData = renderChain.GetOrAddExtraData(ve);
                    BasicNode<MeshHandle> extraMesh = extraData.extraMesh;
                    while (extraMesh != null)
                    {
                        DoUpdateOpacityId(ve, renderChain, extraMesh.data);
                        extraMesh = extraMesh.next;
                    }
                }

                k_UpdateOpacityIdMarker.End();
            }

            static void DoUpdateOpacityId(VisualElement ve, RenderChain renderChain, MeshHandle mesh)
            {
                int vertCount = (int)mesh.allocVerts.size;
                NativeSlice<Vertex> oldVerts = mesh.allocPage.vertices.cpuData.Slice((int)mesh.allocVerts.start, vertCount);
                renderChain.device.Update(mesh, (uint)vertCount, out NativeSlice<Vertex> newVerts);
                Color32 opacityData = renderChain.shaderInfoAllocator.OpacityAllocToVertexData(ve.renderChainData.opacityID);
                renderChain.opacityIdAccelerator.CreateJob(oldVerts, newVerts, opacityData, vertCount);
            }

            #region Dispose Pattern

            protected bool disposed { get; private set; }


            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected void Dispose(bool disposing)
            {
                if (disposed)
                    return;

                if (disposing)
                {
                    m_MeshGenerationContext.Dispose();
                    m_MeshGenerationContext = null;
                }
                else DisposeHelper.NotifyMissingDispose(this);

                disposed = true;
            }

            #endregion // Dispose Pattern
        }
    }
}
