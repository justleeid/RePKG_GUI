using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace RePKG.WpfGui.Helpers;

/// <summary>
/// 支持虚拟化的 WrapPanel，用于大量数据的网格展示
/// </summary>
public class VirtualizingWrapPanel : VirtualizingPanel, IScrollInfo
{
    private const double ScrollLineDelta = 16.0;
    private const double MouseWheelDelta = 48.0;

    private Size _extentSize;
    private Size _viewportSize;
    private Point _offset;
    private TranslateTransform? _transform;

    public VirtualizingWrapPanel()
    {
        CanVerticallyScroll = true;
        CanHorizontallyScroll = false;
    }

    #region 依赖属性

    public static readonly DependencyProperty ItemWidthProperty =
        DependencyProperty.Register(nameof(ItemWidth), typeof(double), typeof(VirtualizingWrapPanel),
            new FrameworkPropertyMetadata(200.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double ItemWidth
    {
        get => (double)GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    public static readonly DependencyProperty ItemHeightProperty =
        DependencyProperty.Register(nameof(ItemHeight), typeof(double), typeof(VirtualizingWrapPanel),
            new FrameworkPropertyMetadata(200.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    #endregion

    #region IScrollInfo

    public bool CanVerticallyScroll { get; set; }
    public bool CanHorizontallyScroll { get; set; }

    public double ExtentWidth => _extentSize.Width;
    public double ExtentHeight => _extentSize.Height;
    public double ViewportWidth => _viewportSize.Width;
    public double ViewportHeight => _viewportSize.Height;
    public double HorizontalOffset => _offset.X;
    public double VerticalOffset => _offset.Y;

    private ScrollViewer? _scrollOwner;
    public ScrollViewer? ScrollOwner
    {
        get => _scrollOwner;
        set
        {
            _scrollOwner = value;
            if (value != null)
            {
                _transform = new TranslateTransform();
                RenderTransform = _transform;
            }
        }
    }

    public void LineUp() => SetVerticalOffset(VerticalOffset - ScrollLineDelta);
    public void LineDown() => SetVerticalOffset(VerticalOffset + ScrollLineDelta);
    public void LineLeft() { }
    public void LineRight() { }

    public void PageUp() => SetVerticalOffset(VerticalOffset - ViewportHeight);
    public void PageDown() => SetVerticalOffset(VerticalOffset + ViewportHeight);
    public void PageLeft() { }
    public void PageRight() { }

    public void MouseWheelUp() => SetVerticalOffset(VerticalOffset - MouseWheelDelta);
    public void MouseWheelDown() => SetVerticalOffset(VerticalOffset + MouseWheelDelta);
    public void MouseWheelLeft() { }
    public void MouseWheelRight() { }

    public void SetHorizontalOffset(double offset) { }

    public void SetVerticalOffset(double offset)
    {
        offset = Math.Max(0, Math.Min(offset, ExtentHeight - ViewportHeight));
        if (Math.Abs(_offset.Y - offset) > 0.1)
        {
            _offset.Y = offset;
            if (_transform != null)
                _transform.Y = -offset;
            InvalidateMeasure();
        }
    }

    public Rect MakeVisible(Visual visual, Rect rectangle)
    {
        return rectangle;
    }

    #endregion

    #region 布局重写

    protected override Size MeasureOverride(Size availableSize)
    {
        var itemsControl = ItemsControl.GetItemsOwner(this);
        if (itemsControl == null)
            return availableSize;

        var itemCount = itemsControl.Items.Count;
        var itemsPerRow = Math.Max(1, (int)(availableSize.Width / ItemWidth));
        var rowCount = (int)Math.Ceiling((double)itemCount / itemsPerRow);

        // 更新滚动信息
        var extentWidth = itemsPerRow * ItemWidth;
        var extentHeight = rowCount * ItemHeight;
        var newExtent = new Size(extentWidth, extentHeight);
        var newViewport = availableSize;

        if (_extentSize != newExtent)
        {
            _extentSize = newExtent;
            _scrollOwner?.InvalidateScrollInfo();
        }

        if (_viewportSize != newViewport)
        {
            _viewportSize = newViewport;
            _scrollOwner?.InvalidateScrollInfo();
        }

        // 限制偏移量
        var maxOffset = Math.Max(0, extentHeight - availableSize.Height);
        if (_offset.Y > maxOffset)
        {
            SetVerticalOffset(maxOffset);
        }

        // 计算可见范围
        var firstRow = (int)(_offset.Y / ItemHeight);
        var lastRow = (int)Math.Ceiling((_offset.Y + availableSize.Height) / ItemHeight);
        var firstIndex = firstRow * itemsPerRow;
        var lastIndex = Math.Min(lastRow * itemsPerRow - 1, itemCount - 1);

        // 虚拟化：回收不可见项
        var generator = ItemContainerGenerator;
        for (var i = InternalChildren.Count - 1; i >= 0; i--)
        {
            var pos = new GeneratorPosition(i, 0);
            var index = generator.IndexFromGeneratorPosition(pos);
            if (index < firstIndex || index > lastIndex)
            {
                generator.Remove(pos, 1);
                RemoveInternalChildRange(i, 1);
            }
        }

        // 生成可见项
        for (var index = firstIndex; index <= lastIndex; index++)
        {
            var pos = generator.GeneratorPositionFromIndex(index);
            if (pos.Offset == 0 && pos.Index >= 0 && pos.Index < InternalChildren.Count)
            {
                // 已存在，只 Measure
                InternalChildren[pos.Index].Measure(new Size(ItemWidth, ItemHeight));
                continue;
            }

            using (generator.StartAt(pos, GeneratorDirection.Forward, true))
            {
                var child = (UIElement)generator.GenerateNext();
                generator.PrepareItemContainer(child);
                AddInternalChild(child);
                child.Measure(new Size(ItemWidth, ItemHeight));
            }
        }

        return availableSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var itemsPerRow = Math.Max(1, (int)(finalSize.Width / ItemWidth));
        var generator = ItemContainerGenerator;

        for (var i = 0; i < InternalChildren.Count; i++)
        {
            var child = InternalChildren[i];
            var pos = new GeneratorPosition(i, 0);
            var index = generator.IndexFromGeneratorPosition(pos);

            var row = index / itemsPerRow;
            var col = index % itemsPerRow;

            var x = col * ItemWidth;
            var y = row * ItemHeight - _offset.Y;

            child.Arrange(new Rect(x, y, ItemWidth, ItemHeight));
        }

        return finalSize;
    }

    #endregion
}
