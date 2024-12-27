// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace UnityEngine.UIElements.Layout;

[StructLayout(LayoutKind.Sequential)]
partial struct LayoutNode : IEquatable<LayoutNode>
{
    public static LayoutNode Undefined => new(default, LayoutHandle.Undefined);

    readonly LayoutDataAccess m_Access;
    readonly LayoutHandle m_Handle;

    internal LayoutNode(LayoutDataAccess access, LayoutHandle handle)
    {
        m_Access = access;
        m_Handle = handle;
    }

    /// <summary>
    /// Returns <see langword="true"/> if this is an invalid/undefined node.
    /// </summary>
    public bool IsUndefined => m_Handle.Equals(LayoutHandle.Undefined);

    /// <summary>
    /// Returns the handle for this node.
    /// </summary>
    public LayoutHandle Handle => m_Handle;

    /// <summary>
    /// Gets the computed layout struct for this node.
    /// </summary>
    public ref LayoutComputedData Layout => ref m_Access.GetComputedData(m_Handle);

    /// <summary>
    /// Gets the style input struct for this node.
    /// </summary>
    public ref LayoutStyleData Style => ref m_Access.GetStyleData(m_Handle);

    /// <summary>
    /// Gets or sets the dirty flag for this node. Used when calculating layout.
    /// </summary>
    public bool IsDirty
    {
        get => m_Access.GetNodeData(m_Handle).IsDirty;
        set => m_Access.GetNodeData(m_Handle).IsDirty = value;
    }

    /// <summary>
    /// Gets or sets the new layout flag for this node. Used when calculating layout.
    /// </summary>
    public bool HasNewLayout
    {
        get => m_Access.GetNodeData(m_Handle).HasNewLayout;
        set => m_Access.GetNodeData(m_Handle).HasNewLayout = value;
    }

    /// <summary>
    /// Returns <see langword="true"/> if a custom measurement method is defined.
    /// </summary>
    public bool IsMeasureDefined => m_Access.GetNodeData(m_Handle).ManagedMeasureFunctionIndex != 0;

    /// <summary>
    /// Gets or sets the custom measure function for this node.
    /// </summary>
    public LayoutMeasureFunction Measure
    {
        get => m_Access.GetMeasureFunction(m_Handle);
        set => m_Access.SetMeasureFunction(m_Handle, value);
    }

    /// <summary>
    /// Sets the owner of this node.
    /// </summary>
    public void SetOwner(VisualElement func)
    {
        m_Access.SetOwner(m_Handle, func);
    }

    public VisualElement GetOwner()
    {
       return m_Access.GetOwner(m_Handle);
    }

    /// <summary>
    /// Returns <see langword="true"/> if a custom baseline method is defined.
    /// </summary>
    public bool IsBaselineDefined => m_Access.GetNodeData(m_Handle).ManagedBaselineFunctionIndex != 0;

    /// <summary>
    /// Gets or sets the custom baseline function for this node.
    /// </summary>
    public LayoutBaselineFunction Baseline
    {
        get => m_Access.GetBaselineFunction(m_Handle);
        set => m_Access.SetBaselineFunction(m_Handle, value);
    }

    /// <summary>
    /// Gets or sets the line index for this node. Used when calculating layout.
    /// </summary>
    public ref int LineIndex => ref m_Access.GetNodeData(m_Handle).LineIndex;

    /// <summary>
    /// Gets or sets the shared configuration object for this node.
    /// </summary>
    public LayoutConfig Config
    {
        get => new(m_Access, m_Access.GetNodeData(m_Handle).Config);
        set => m_Access.GetNodeData(m_Handle).Config = value.Handle;
    }

    /// <summary>
    /// Marks this node and all ancestors as dirty.
    /// </summary>
    public void MarkDirty()
    {
        if (IsDirty)
            return;

        IsDirty = true;

        Layout.ComputedFlexBasis = float.NaN;

        if (!Parent.IsUndefined)
            Parent.MarkDirty();
    }

    /// <summary>
    /// Marks this node layout as seen (not new).
    /// </summary>
    public void MarkLayoutSeen()
    {
        HasNewLayout = false;
    }

    public void CopyFromComputedStyle(ComputedStyle style)
    {
        FlexGrow = style.flexGrow;
        FlexShrink = style.flexShrink;
        FlexBasis = style.flexBasis.ToLayoutValue();
        Left = style.left.ToLayoutValue();
        Top = style.top.ToLayoutValue();
        Right = style.right.ToLayoutValue();
        Bottom = style.bottom.ToLayoutValue();
        MarginLeft = style.marginLeft.ToLayoutValue();
        MarginTop = style.marginTop.ToLayoutValue();
        MarginRight = style.marginRight.ToLayoutValue();
        MarginBottom = style.marginBottom.ToLayoutValue();
        PaddingLeft = style.paddingLeft.ToLayoutValue();
        PaddingTop = style.paddingTop.ToLayoutValue();
        PaddingRight = style.paddingRight.ToLayoutValue();
        PaddingBottom = style.paddingBottom.ToLayoutValue();
        BorderLeftWidth = style.borderLeftWidth;
        BorderTopWidth = style.borderTopWidth;
        BorderRightWidth = style.borderRightWidth;
        BorderBottomWidth = style.borderBottomWidth;
        Width = style.width.ToLayoutValue();
        Height = style.height.ToLayoutValue();
        PositionType = (LayoutPositionType)style.position;
        Overflow = (LayoutOverflow)style.overflow;
        AlignSelf = (LayoutAlign)style.alignSelf;
        MaxWidth = style.maxWidth.ToLayoutValue();
        MaxHeight = style.maxHeight.ToLayoutValue();
        MinWidth = style.minWidth.ToLayoutValue();
        MinHeight = style.minHeight.ToLayoutValue();
        FlexDirection = (LayoutFlexDirection)style.flexDirection;
        AlignContent = (LayoutAlign)style.alignContent;
        AlignItems = (LayoutAlign)style.alignItems;
        JustifyContent = (LayoutJustify)style.justifyContent;
        Wrap = (LayoutWrap)style.flexWrap;
        Display = (LayoutDisplay)style.display;
    }

    /// <summary>
    /// Copies the style from the given <see cref="LayoutNode"/>.
    /// </summary>
    /// <param name="node">The node to copy the style from.</param>
    public void CopyStyle(LayoutNode node)
    {
        var markDirty = false;
        unsafe
        {
            fixed (LayoutStyleData* dstStyle = &Style)
            fixed (LayoutStyleData* srcStyle = &node.Style)
            {
                if (UnsafeUtility.MemCmp(dstStyle, srcStyle, UnsafeUtility.SizeOf<LayoutStyleData>()) != 0)
                {
                    Style = node.Style;
                    markDirty = true;
                }
            }
        }

        if (markDirty)
            MarkDirty();
    }

    /// <summary>
    /// Resets the node for immediate re-use on the same element.
    /// </summary>
    public void SoftReset()
    {
        ref var data = ref m_Access.GetNodeData(m_Handle);
        data.HasNewLayout = true;
    }

    /// <summary>
    /// Resets the node for re-use.
    /// </summary>
    public void Reset()
    {
        ref var data = ref m_Access.GetNodeData(m_Handle);

        Assert.IsTrue(!data.Children.IsCreated || data.Children.Count == 0, "Cannot reset a node which still has children attached");

        data.Parent = default;
        data.HasNewLayout = true;
        data.ResolvedDimensions = new FixedBuffer2<LayoutValue>
        {
            [0] = LayoutValue.Undefined(),
            [1] = LayoutValue.Undefined()
        };

        Measure = null;
        Baseline = null;
        SetOwner(null);

        Layout = LayoutComputedData.Default;
        Style = LayoutStyleData.Default;
    }

    public bool Equals(LayoutNode other)
    {
        return m_Handle.Equals(other.m_Handle);
    }

    public override bool Equals(object obj)
    {
        return obj is LayoutNode other && Equals(other);
    }

    public override int GetHashCode()
    {
        return m_Handle.GetHashCode();
    }

    public static bool operator ==(LayoutNode lhs, LayoutNode rhs)
    {
        if (lhs.IsUndefined)
        {
            if (rhs.IsUndefined)
                return true;

            return false;
        }

        return lhs.Equals(rhs);
    }

    public static bool operator !=(LayoutNode lhs, LayoutNode rhs) => !(lhs == rhs);

    /// <summary>
    /// Performs the flexbox layout calculation.
    /// </summary>
    /// <param name="width">The desired width.</param>
    /// <param name="height">The desired height.</param>
    public void CalculateLayout(float width = float.NaN, float height = float.NaN)
    {
        LayoutProcessor.CalculateLayout(this, width, height, Style.Direction);
    }
}
