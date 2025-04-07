#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

//
// Summary:
//     Box sizing model comparable to the box sizing model of cascading style sheets
//     using "position:relative;" Each element has a position, size, padding and margin
//     Padding is counted towards the size of the box, whereas margin is not
public class ElementBounds
{
    public ElementBounds ParentBounds;

    public ElementBounds LeftOfBounds;

    public List<ElementBounds> ChildBounds = new List<ElementBounds>();

    protected bool IsWindowBounds;

    //
    // Summary:
    //     For debugging purposes only
    public string Code;

    public EnumDialogArea Alignment;

    public ElementSizing verticalSizing;

    public ElementSizing horizontalSizing;

    public double percentPaddingX;

    public double percentPaddingY;

    public double percentX;

    public double percentY;

    public double percentWidth;

    public double percentHeight;

    public double fixedMarginX;

    public double fixedMarginY;

    public double fixedPaddingX;

    public double fixedPaddingY;

    public double fixedX;

    public double fixedY;

    public double fixedWidth;

    public double fixedHeight;

    public double fixedOffsetX;

    public double fixedOffsetY;

    public double absPaddingX;

    public double absPaddingY;

    public double absMarginX;

    public double absMarginY;

    public double absOffsetX;

    public double absOffsetY;

    public double absFixedX;

    public double absFixedY;

    public double absInnerWidth;

    public double absInnerHeight;

    public string Name;

    public bool AllowNoChildren;

    public bool Initialized;

    //
    // Summary:
    //     If set, bgDrawX/Y will be relative, instead of absolute
    public bool IsDrawingSurface;

    private bool requiresrelculation = true;

    public double renderOffsetX;

    public double renderOffsetY;

    //
    // Summary:
    //     Set the vertical and horizontal sizing, see also Vintagestory.API.Client.ElementSizing.
    //     Setting this is equal to calling Vintagestory.API.Client.ElementBounds.WithSizing(Vintagestory.API.Client.ElementSizing)
    public ElementSizing BothSizing
    {
        set
        {
            verticalSizing = value;
            horizontalSizing = value;
        }
    }

    public virtual bool RequiresRecalculation => requiresrelculation;

    //
    // Summary:
    //     Position relative to it's parent element plus margin
    public virtual double relX => absFixedX + absMarginX + absOffsetX;

    public virtual double relY => absFixedY + absMarginY + absOffsetY;

    //
    // Summary:
    //     Absolute position of the element plus margin. Same as renderX but without padding
    public virtual double absX => absFixedX + absMarginX + absOffsetX + ParentBounds.absPaddingX + ParentBounds.absX;

    public virtual double absY => absFixedY + absMarginY + absOffsetY + ParentBounds.absPaddingY + ParentBounds.absY;

    //
    // Summary:
    //     Width including padding
    public virtual double OuterWidth => absInnerWidth + 2.0 * absPaddingX;

    //
    // Summary:
    //     Height including padding
    public virtual double OuterHeight => absInnerHeight + 2.0 * absPaddingY;

    public virtual int OuterWidthInt => (int)OuterWidth;

    public virtual int OuterHeightInt => (int)OuterHeight;

    public virtual double InnerWidth => absInnerWidth;

    public virtual double InnerHeight => absInnerHeight;

    //
    // Summary:
    //     Position where the element has to be drawn. This is a position relative to it's
    //     parent element plus margin plus padding.
    public virtual double drawX => bgDrawX + absPaddingX;

    public virtual double drawY => bgDrawY + absPaddingY;

    //
    // Summary:
    //     Position where the background has to be drawn, this encompasses the elements
    //     padding
    public virtual double bgDrawX => absFixedX + absMarginX + absOffsetX + (ParentBounds.IsDrawingSurface ? ParentBounds.absPaddingX : ParentBounds.drawX);

    public virtual double bgDrawY => absFixedY + absMarginY + absOffsetY + (ParentBounds.IsDrawingSurface ? ParentBounds.absPaddingY : ParentBounds.drawY);

    public virtual double renderX => absFixedX + absMarginX + absOffsetX + ParentBounds.absPaddingX + ParentBounds.renderX + renderOffsetX;

    public virtual double renderY => absFixedY + absMarginY + absOffsetY + ParentBounds.absPaddingY + ParentBounds.renderY + renderOffsetY;

    //
    // Summary:
    //     Quick Method to create a new ElementBounds instance that fills 100% of its parent
    //     bounds. Useful for backgrounds.
    public static ElementBounds Fill => new ElementBounds
    {
        Alignment = EnumDialogArea.None,
        BothSizing = ElementSizing.Percentual,
        percentWidth = 1.0,
        percentHeight = 1.0
    };

    //
    // Summary:
    //     Create a special instance of type Vintagestory.API.Client.ElementEmptyBounds
    //     whose position is 0 and size 1. It's often used for other bounds that need a
    //     static, unchanging parent bounds
    public static ElementBounds Empty => new ElementEmptyBounds();

    public void MarkDirtyRecursive()
    {
        Initialized = false;
        foreach (ElementBounds childBound in ChildBounds)
        {
            if (ParentBounds != childBound)
            {
                if (this == childBound)
                {
                    throw new Exception($"Fatal: Element bounds {this} self reference itself in child bounds, this would cause a stack overflow.");
                }

                childBound.MarkDirtyRecursive();
            }
        }
    }

    public virtual void CalcWorldBounds()
    {
        requiresrelculation = false;
        absOffsetX = scaled(fixedOffsetX);
        absOffsetY = scaled(fixedOffsetY);
        if (horizontalSizing == ElementSizing.FitToChildren && verticalSizing == ElementSizing.FitToChildren)
        {
            absFixedX = scaled(fixedX);
            absFixedY = scaled(fixedY);
            absPaddingX = scaled(fixedPaddingX);
            absPaddingY = scaled(fixedPaddingY);
            buildBoundsFromChildren();
        }
        else
        {
            switch (horizontalSizing)
            {
                case ElementSizing.Fixed:
                    absFixedX = scaled(fixedX);
                    if (LeftOfBounds != null)
                    {
                        absFixedX += LeftOfBounds.absFixedX + LeftOfBounds.OuterWidth;
                    }

                    absInnerWidth = scaled(fixedWidth);
                    absPaddingX = scaled(fixedPaddingX);
                    break;
                case ElementSizing.Percentual:
                case ElementSizing.PercentualSubstractFixed:
                    absFixedX = percentX * ParentBounds.OuterWidth;
                    absInnerWidth = percentWidth * ParentBounds.OuterWidth;
                    absPaddingX = scaled(fixedPaddingX) + percentPaddingX * ParentBounds.OuterWidth;
                    if (horizontalSizing == ElementSizing.PercentualSubstractFixed)
                    {
                        absInnerWidth -= scaled(fixedWidth);
                    }

                    break;
                case ElementSizing.FitToChildren:
                    absFixedX = scaled(fixedX);
                    absPaddingX = scaled(fixedPaddingX);
                    buildBoundsFromChildren();
                    break;
            }

            switch (verticalSizing)
            {
                case ElementSizing.Fixed:
                    absFixedY = scaled(fixedY);
                    absInnerHeight = scaled(fixedHeight);
                    absPaddingY = scaled(fixedPaddingY);
                    break;
                case ElementSizing.Percentual:
                case ElementSizing.PercentualSubstractFixed:
                    absFixedY = percentY * ParentBounds.OuterHeight;
                    absInnerHeight = percentHeight * ParentBounds.OuterHeight;
                    absPaddingY = scaled(fixedPaddingY) + percentPaddingY * ParentBounds.OuterHeight;
                    if (horizontalSizing == ElementSizing.PercentualSubstractFixed)
                    {
                        absInnerHeight -= scaled(fixedHeight);
                    }

                    break;
                case ElementSizing.FitToChildren:
                    absFixedY = scaled(fixedY);
                    absPaddingY = scaled(fixedPaddingY);
                    buildBoundsFromChildren();
                    break;
            }
        }

        if (ParentBounds.Initialized)
        {
            calcMarginFromAlignment(ParentBounds.InnerWidth, ParentBounds.InnerHeight);
        }

        Initialized = true;
        foreach (ElementBounds childBound in ChildBounds)
        {
            if (!childBound.Initialized)
            {
                childBound.CalcWorldBounds();
            }
        }
    }

    private void calcMarginFromAlignment(double dialogWidth, double dialogHeight)
    {
        int num = 0;
        int num2 = 0;
        ElementBounds parentBounds = ParentBounds;
        if (parentBounds != null && parentBounds.IsWindowBounds)
        {
            num = GuiStyle.LeftDialogMargin;
            num2 = GuiStyle.RightDialogMargin;
        }

        switch (Alignment)
        {
            case EnumDialogArea.FixedMiddle:
                absMarginY = dialogHeight / 2.0 - OuterHeight / 2.0;
                break;
            case EnumDialogArea.FixedBottom:
                absMarginY = dialogHeight - OuterHeight;
                break;
            case EnumDialogArea.CenterFixed:
                absMarginX = dialogWidth / 2.0 - OuterWidth / 2.0;
                break;
            case EnumDialogArea.CenterBottom:
                absMarginX = dialogWidth / 2.0 - OuterWidth / 2.0;
                absMarginY = dialogHeight - OuterHeight;
                break;
            case EnumDialogArea.CenterMiddle:
                absMarginX = dialogWidth / 2.0 - OuterWidth / 2.0;
                absMarginY = dialogHeight / 2.0 - OuterHeight / 2.0;
                break;
            case EnumDialogArea.CenterTop:
                absMarginX = dialogWidth / 2.0 - OuterWidth / 2.0;
                break;
            case EnumDialogArea.LeftBottom:
                absMarginX = num;
                absMarginY = dialogHeight - OuterHeight;
                break;
            case EnumDialogArea.LeftMiddle:
                absMarginX = num;
                absMarginY = dialogHeight / 2.0 - absInnerHeight / 2.0;
                break;
            case EnumDialogArea.LeftTop:
                absMarginX = num;
                absMarginY = 0.0;
                break;
            case EnumDialogArea.LeftFixed:
                absMarginX = num;
                break;
            case EnumDialogArea.RightBottom:
                absMarginX = dialogWidth - OuterWidth - (double)num2;
                absMarginY = dialogHeight - OuterHeight;
                break;
            case EnumDialogArea.RightMiddle:
                absMarginX = dialogWidth - OuterWidth - (double)num2;
                absMarginY = dialogHeight / 2.0 - OuterHeight / 2.0;
                break;
            case EnumDialogArea.RightTop:
                absMarginX = dialogWidth - OuterWidth - (double)num2;
                absMarginY = 0.0;
                break;
            case EnumDialogArea.RightFixed:
                absMarginX = dialogWidth - OuterWidth - (double)num2;
                break;
            case EnumDialogArea.FixedTop:
                break;
        }
    }

    private void buildBoundsFromChildren()
    {
        if (ChildBounds == null || ChildBounds.Count == 0)
        {
            if (!AllowNoChildren)
            {
                throw new Exception("Cant build bounds from children elements, there are no children!");
            }

            return;
        }

        double num = 0.0;
        double num2 = 0.0;
        foreach (ElementBounds childBound in ChildBounds)
        {
            if (childBound == this)
            {
                throw new Exception("Endless loop detected. Bounds instance is contained itself in its ChildBounds List. Fix your code please :P");
            }

            EnumDialogArea alignment = childBound.Alignment;
            childBound.Alignment = EnumDialogArea.None;
            childBound.CalcWorldBounds();
            if (childBound.horizontalSizing != ElementSizing.Percentual)
            {
                num = Math.Max(num, childBound.OuterWidth + childBound.relX);
            }

            if (childBound.verticalSizing != ElementSizing.Percentual)
            {
                num2 = Math.Max(num2, childBound.OuterHeight + childBound.relY);
            }

            childBound.Alignment = alignment;
        }

        if (num == 0.0 || num2 == 0.0)
        {
            throw new Exception("Couldn't build bounds from children, there were probably no child elements using fixed sizing! (or they were size 0)");
        }

        if (horizontalSizing != 0)
        {
            absInnerWidth = num;
        }

        if (verticalSizing != 0)
        {
            absInnerHeight = num2;
        }
    }

    public static double scaled(double value)
    {
        return value * (double)RuntimeEnv.GUIScale;
    }

    public ElementBounds WithScale(double factor)
    {
        fixedX *= factor;
        fixedY *= factor;
        fixedWidth *= factor;
        fixedHeight *= factor;
        absPaddingX *= factor;
        absPaddingY *= factor;
        absMarginX *= factor;
        absMarginY *= factor;
        percentPaddingX *= factor;
        percentPaddingY *= factor;
        percentX *= factor;
        percentY *= factor;
        percentWidth *= factor;
        percentHeight *= factor;
        return this;
    }

    public ElementBounds WithChildren(params ElementBounds[] bounds)
    {
        foreach (ElementBounds bounds2 in bounds)
        {
            WithChild(bounds2);
        }

        return this;
    }

    public ElementBounds WithChild(ElementBounds bounds)
    {
        if (!ChildBounds.Contains(bounds))
        {
            ChildBounds.Add(bounds);
        }

        if (bounds.ParentBounds == null)
        {
            bounds.ParentBounds = this;
        }

        return this;
    }

    public ElementBounds RightOf(ElementBounds leftBounds, double leftMargin = 0.0)
    {
        LeftOfBounds = leftBounds;
        fixedX = leftMargin;
        return this;
    }

    //
    // Summary:
    //     Returns the relative coordinate if supplied coordinate is inside the bounds,
    //     otherwise null
    //
    // Parameters:
    //   absPointX:
    //
    //   absPointY:
    public Vec2d PositionInside(int absPointX, int absPointY)
    {
        if (PointInside(absPointX, absPointY))
        {
            return new Vec2d((double)absPointX - absX, (double)absPointY - absY);
        }

        return null;
    }

    //
    // Summary:
    //     Returns true if supplied coordinate is inside the bounds
    //
    // Parameters:
    //   absPointX:
    //
    //   absPointY:
    public bool PointInside(int absPointX, int absPointY)
    {
        if ((double)absPointX >= absX && (double)absPointX <= absX + OuterWidth && (double)absPointY >= absY)
        {
            return (double)absPointY <= absY + OuterHeight;
        }

        return false;
    }

    //
    // Summary:
    //     Returns true if supplied coordinate is inside the bounds
    //
    // Parameters:
    //   absPointX:
    //
    //   absPointY:
    public bool PointInside(double absPointX, double absPointY)
    {
        if (absPointX >= absX && absPointX <= absX + OuterWidth && absPointY >= absY)
        {
            return absPointY <= absY + OuterHeight;
        }

        return false;
    }

    //
    // Summary:
    //     Checks if the bounds is at least partially inside it's parent bounds by checking
    //     if any of the 4 corner points is inside
    public bool PartiallyInside(ElementBounds boundingBounds)
    {
        if (!boundingBounds.PointInside(absX, absY) && !boundingBounds.PointInside(absX + OuterWidth, absY) && !boundingBounds.PointInside(absX, absY + OuterHeight))
        {
            return boundingBounds.PointInside(absX + OuterWidth, absY + OuterHeight);
        }

        return true;
    }

    //
    // Summary:
    //     Makes a copy of the current bounds but leaves the position and 0. Sets the parent
    //     to the calling bounds
    public ElementBounds CopyOnlySize()
    {
        return new ElementBounds
        {
            verticalSizing = verticalSizing,
            horizontalSizing = horizontalSizing,
            percentHeight = percentHeight,
            percentWidth = percentHeight,
            fixedHeight = fixedHeight,
            fixedWidth = fixedWidth,
            fixedPaddingX = fixedPaddingX,
            fixedPaddingY = fixedPaddingY,
            ParentBounds = Empty.WithSizing(ElementSizing.FitToChildren)
        };
    }

    //
    // Summary:
    //     Makes a copy of the current bounds but leaves the position and padding at 0.
    //     Sets the same parent as the current one.
    public ElementBounds CopyOffsetedSibling(double fixedDeltaX = 0.0, double fixedDeltaY = 0.0, double fixedDeltaWidth = 0.0, double fixedDeltaHeight = 0.0)
    {
        return new ElementBounds
        {
            Alignment = Alignment,
            verticalSizing = verticalSizing,
            horizontalSizing = horizontalSizing,
            percentHeight = percentHeight,
            percentWidth = percentHeight,
            fixedOffsetX = fixedOffsetX,
            fixedOffsetY = fixedOffsetY,
            fixedX = fixedX + fixedDeltaX,
            fixedY = fixedY + fixedDeltaY,
            fixedWidth = fixedWidth + fixedDeltaWidth,
            fixedHeight = fixedHeight + fixedDeltaHeight,
            fixedPaddingX = fixedPaddingX,
            fixedPaddingY = fixedPaddingY,
            fixedMarginX = fixedMarginX,
            fixedMarginY = fixedMarginY,
            percentPaddingX = percentPaddingX,
            percentPaddingY = percentPaddingY,
            ParentBounds = ParentBounds
        };
    }

    //
    // Summary:
    //     Makes a copy of the current bounds but leaves the position and padding at 0.
    //     Sets the same parent as the current one.
    public ElementBounds BelowCopy(double fixedDeltaX = 0.0, double fixedDeltaY = 0.0, double fixedDeltaWidth = 0.0, double fixedDeltaHeight = 0.0)
    {
        return new ElementBounds
        {
            Alignment = Alignment,
            verticalSizing = verticalSizing,
            horizontalSizing = horizontalSizing,
            percentHeight = percentHeight,
            percentWidth = percentHeight,
            percentX = percentX,
            percentY = (percentY = percentHeight),
            fixedOffsetX = fixedOffsetX,
            fixedOffsetY = fixedOffsetY,
            fixedX = fixedX + fixedDeltaX,
            fixedY = fixedY + fixedDeltaY + fixedHeight + fixedPaddingY * 2.0,
            fixedWidth = fixedWidth + fixedDeltaWidth,
            fixedHeight = fixedHeight + fixedDeltaHeight,
            fixedPaddingX = fixedPaddingX,
            fixedPaddingY = fixedPaddingY,
            fixedMarginX = fixedMarginX,
            fixedMarginY = fixedMarginY,
            percentPaddingX = percentPaddingX,
            percentPaddingY = percentPaddingY,
            ParentBounds = ParentBounds
        };
    }

    //
    // Summary:
    //     Create a flat copy of the element with a fixed position offset that causes it
    //     to be right of the original element
    //
    // Parameters:
    //   fixedDeltaX:
    //
    //   fixedDeltaY:
    //
    //   fixedDeltaWidth:
    //
    //   fixedDeltaHeight:
    public ElementBounds RightCopy(double fixedDeltaX = 0.0, double fixedDeltaY = 0.0, double fixedDeltaWidth = 0.0, double fixedDeltaHeight = 0.0)
    {
        return new ElementBounds
        {
            Alignment = Alignment,
            verticalSizing = verticalSizing,
            horizontalSizing = horizontalSizing,
            percentHeight = percentHeight,
            percentWidth = percentHeight,
            percentX = percentX,
            percentY = (percentY = percentHeight),
            fixedOffsetX = fixedOffsetX,
            fixedOffsetY = fixedOffsetY,
            fixedX = fixedX + fixedDeltaX + fixedWidth + fixedPaddingX * 2.0,
            fixedY = fixedY + fixedDeltaY,
            fixedWidth = fixedWidth + fixedDeltaWidth,
            fixedHeight = fixedHeight + fixedDeltaHeight,
            fixedPaddingX = fixedPaddingX,
            fixedPaddingY = fixedPaddingY,
            fixedMarginX = fixedMarginX,
            fixedMarginY = fixedMarginY,
            percentPaddingX = percentPaddingX,
            percentPaddingY = percentPaddingY,
            ParentBounds = ParentBounds
        };
    }

    //
    // Summary:
    //     Creates a clone of the bounds but without child elements
    public ElementBounds FlatCopy()
    {
        return new ElementBounds
        {
            Alignment = Alignment,
            verticalSizing = verticalSizing,
            horizontalSizing = horizontalSizing,
            percentHeight = percentHeight,
            percentWidth = percentHeight,
            fixedOffsetX = fixedOffsetX,
            fixedOffsetY = fixedOffsetY,
            fixedX = fixedX,
            fixedY = fixedY,
            fixedWidth = fixedWidth,
            fixedHeight = fixedHeight,
            fixedPaddingX = fixedPaddingX,
            fixedPaddingY = fixedPaddingY,
            fixedMarginX = fixedMarginX,
            fixedMarginY = fixedMarginY,
            percentPaddingX = percentPaddingX,
            percentPaddingY = percentPaddingY,
            ParentBounds = ParentBounds
        };
    }

    public ElementBounds ForkChild()
    {
        return ForkChildOffseted();
    }

    public ElementBounds ForkChildOffseted(double fixedDeltaX = 0.0, double fixedDeltaY = 0.0, double fixedDeltaWidth = 0.0, double fixedDeltaHeight = 0.0)
    {
        return new ElementBounds
        {
            Alignment = Alignment,
            verticalSizing = verticalSizing,
            horizontalSizing = horizontalSizing,
            percentHeight = percentHeight,
            percentWidth = percentHeight,
            fixedOffsetX = fixedOffsetX,
            fixedOffsetY = fixedOffsetY,
            fixedX = fixedX + fixedDeltaX,
            fixedY = fixedY + fixedDeltaY,
            fixedWidth = fixedWidth + fixedDeltaWidth,
            fixedHeight = fixedHeight + fixedDeltaHeight,
            fixedPaddingX = fixedPaddingX,
            fixedPaddingY = fixedPaddingY,
            percentPaddingX = percentPaddingX,
            percentPaddingY = percentPaddingY,
            ParentBounds = this
        };
    }

    //
    // Summary:
    //     Creates a new elements bounds which acts as the parent bounds of the current
    //     bounds. It will also arrange the fixedX/Y and Width/Height coords of both bounds
    //     so that the parent bounds surrounds the child bounds with given spacings. Uses
    //     fixed coords only!
    //
    // Parameters:
    //   leftSpacing:
    //
    //   topSpacing:
    //
    //   rightSpacing:
    //
    //   bottomSpacing:
    public ElementBounds ForkBoundingParent(double leftSpacing = 0.0, double topSpacing = 0.0, double rightSpacing = 0.0, double bottomSpacing = 0.0)
    {
        ElementBounds elementBounds = new ElementBounds
        {
            Alignment = Alignment,
            verticalSizing = verticalSizing,
            horizontalSizing = horizontalSizing,
            fixedOffsetX = fixedOffsetX,
            fixedOffsetY = fixedOffsetY,
            fixedWidth = fixedWidth + 2.0 * fixedPaddingX + leftSpacing + rightSpacing,
            fixedHeight = fixedHeight + 2.0 * fixedPaddingY + topSpacing + bottomSpacing,
            fixedX = fixedX,
            fixedY = fixedY,
            percentHeight = percentHeight,
            percentWidth = percentWidth
        };
        fixedX = leftSpacing;
        fixedY = topSpacing;
        percentWidth = 1.0;
        percentHeight = 1.0;
        ParentBounds = elementBounds;
        return elementBounds;
    }

    //
    // Summary:
    //     Creates a new elements bounds which acts as the child bounds of the current bounds.
    //     It will also arrange the fixedX/Y and Width/Height coords of both bounds so that
    //     the parent bounds surrounds the child bounds with given spacings. Uses fixed
    //     coords only!
    //
    // Parameters:
    //   leftSpacing:
    //
    //   topSpacing:
    //
    //   rightSpacing:
    //
    //   bottomSpacing:
    public ElementBounds ForkContainingChild(double leftSpacing = 0.0, double topSpacing = 0.0, double rightSpacing = 0.0, double bottomSpacing = 0.0)
    {
        ElementBounds elementBounds = new ElementBounds
        {
            Alignment = Alignment,
            verticalSizing = verticalSizing,
            horizontalSizing = horizontalSizing,
            fixedOffsetX = fixedOffsetX,
            fixedOffsetY = fixedOffsetY,
            fixedWidth = fixedWidth - 2.0 * fixedPaddingX - leftSpacing - rightSpacing,
            fixedHeight = fixedHeight - 2.0 * fixedPaddingY - topSpacing - bottomSpacing,
            fixedX = fixedX,
            fixedY = fixedY,
            percentHeight = percentHeight,
            percentWidth = percentWidth
        };
        elementBounds.fixedX = leftSpacing;
        elementBounds.fixedY = topSpacing;
        percentWidth = 1.0;
        percentHeight = 1.0;
        ChildBounds.Add(elementBounds);
        elementBounds.ParentBounds = this;
        return elementBounds;
    }

    public override string ToString()
    {
        return absX + "/" + absY + " -> " + (absX + OuterWidth) + " / " + (absY + OuterHeight);
    }

    //
    // Summary:
    //     Set the fixed y-position to "refBounds.fixedY + refBounds.fixedHeight + spacing"
    //     so that the bounds will be under the reference bounds
    //
    // Parameters:
    //   refBounds:
    //
    //   spacing:
    public ElementBounds FixedUnder(ElementBounds refBounds, double spacing = 0.0)
    {
        fixedY += refBounds.fixedY + refBounds.fixedHeight + spacing;
        return this;
    }

    //
    // Summary:
    //     Set the fixed x-position to "refBounds.fixedX + refBounds.fixedWidth + leftSpacing"
    //     so that the bounds will be right of reference bounds
    //
    // Parameters:
    //   refBounds:
    //
    //   leftSpacing:
    public ElementBounds FixedRightOf(ElementBounds refBounds, double leftSpacing = 0.0)
    {
        fixedX = refBounds.fixedX + refBounds.fixedWidth + leftSpacing;
        return this;
    }

    //
    // Summary:
    //     Set the fixed x-position to "refBounds.fixedX - fixedWith - rightSpacing" so
    //     that the element will be left of reference bounds
    //
    // Parameters:
    //   refBounds:
    //
    //   rightSpacing:
    public ElementBounds FixedLeftOf(ElementBounds refBounds, double rightSpacing = 0.0)
    {
        fixedX = refBounds.fixedX - fixedWidth - rightSpacing;
        return this;
    }

    //
    // Summary:
    //     Set the fixed width and fixed height values
    //
    // Parameters:
    //   width:
    //
    //   height:
    public ElementBounds WithFixedSize(double width, double height)
    {
        fixedWidth = width;
        fixedHeight = height;
        return this;
    }

    //
    // Summary:
    //     Set the width property
    //
    // Parameters:
    //   width:
    public ElementBounds WithFixedWidth(double width)
    {
        fixedWidth = width;
        return this;
    }

    //
    // Summary:
    //     Set the height property
    //
    // Parameters:
    //   height:
    public ElementBounds WithFixedHeight(double height)
    {
        fixedHeight = height;
        return this;
    }

    //
    // Summary:
    //     Set the alignment property
    //
    // Parameters:
    //   alignment:
    public ElementBounds WithAlignment(EnumDialogArea alignment)
    {
        Alignment = alignment;
        return this;
    }

    //
    // Summary:
    //     Set the vertical and horizontal sizing property to the same value. See also Vintagestory.API.Client.ElementSizing.
    //
    //
    // Parameters:
    //   sizing:
    public ElementBounds WithSizing(ElementSizing sizing)
    {
        verticalSizing = sizing;
        horizontalSizing = sizing;
        return this;
    }

    //
    // Summary:
    //     Set the vertical and horizontal sizing properties individually. See also Vintagestory.API.Client.ElementSizing.
    //
    //
    // Parameters:
    //   horizontalSizing:
    //
    //   verticalSizing:
    public ElementBounds WithSizing(ElementSizing horizontalSizing, ElementSizing verticalSizing)
    {
        this.verticalSizing = verticalSizing;
        this.horizontalSizing = horizontalSizing;
        return this;
    }

    //
    // Summary:
    //     Sets a new fixed margin (pad = top/right/down/left margin)
    //
    // Parameters:
    //   pad:
    public ElementBounds WithFixedMargin(double pad)
    {
        fixedMarginX = pad;
        fixedMarginY = pad;
        return this;
    }

    //
    // Summary:
    //     Sets a new fixed margin (pad = top/right/down/left margin)
    //
    // Parameters:
    //   padH:
    //
    //   padV:
    public ElementBounds WithFixedMargin(double padH, double padV)
    {
        fixedMarginX = padH;
        fixedMarginY = padV;
        return this;
    }

    //
    // Summary:
    //     Sets a new fixed padding (pad = top/right/down/left padding)
    //
    // Parameters:
    //   pad:
    public ElementBounds WithFixedPadding(double pad)
    {
        fixedPaddingX = pad;
        fixedPaddingY = pad;
        return this;
    }

    //
    // Summary:
    //     Sets a new fixed padding (x = left/right, y = top/down padding)
    //
    // Parameters:
    //   leftRight:
    //
    //   upDown:
    public ElementBounds WithFixedPadding(double leftRight, double upDown)
    {
        fixedPaddingX = leftRight;
        fixedPaddingY = upDown;
        return this;
    }

    //
    // Summary:
    //     Sets a new fixed offset that is applied after element alignment. So you could
    //     i.e. horizontally center an element and then offset in x direction from there
    //     using this method.
    //
    // Parameters:
    //   x:
    //
    //   y:
    public ElementBounds WithFixedAlignmentOffset(double x, double y)
    {
        fixedOffsetX = x;
        fixedOffsetY = y;
        return this;
    }

    //
    // Summary:
    //     Sets a new fixed offset that is used during element alignment.
    //
    // Parameters:
    //   x:
    //
    //   y:
    public ElementBounds WithFixedPosition(double x, double y)
    {
        fixedX = x;
        fixedY = y;
        return this;
    }

    //
    // Summary:
    //     Sets a new fixed offset that is used during element alignment.
    //
    // Parameters:
    //   offx:
    //
    //   offy:
    public ElementBounds WithFixedOffset(double offx, double offy)
    {
        fixedX += offx;
        fixedY += offy;
        return this;
    }

    //
    // Summary:
    //     Shrinks the current width/height by a fixed value
    //
    // Parameters:
    //   amount:
    public ElementBounds FixedShrink(double amount)
    {
        fixedWidth -= amount;
        fixedHeight -= amount;
        return this;
    }

    //
    // Summary:
    //     Grows the current width/height by a fixed value
    //
    // Parameters:
    //   amount:
    public ElementBounds FixedGrow(double amount)
    {
        fixedWidth += amount;
        fixedHeight += amount;
        return this;
    }

    //
    // Summary:
    //     Grows the current width/height by a fixed value
    //
    // Parameters:
    //   width:
    //
    //   height:
    public ElementBounds FixedGrow(double width, double height)
    {
        fixedWidth += width;
        fixedHeight += height;
        return this;
    }

    //
    // Summary:
    //     Sets the parent of the bounds
    //
    // Parameters:
    //   bounds:
    public ElementBounds WithParent(ElementBounds bounds)
    {
        ParentBounds = bounds;
        return this;
    }

    //
    // Summary:
    //     Creates a new bounds using FitToChildren and sets that as bound parent. This
    //     is usefull if you want to draw elements that are not part of the dialog
    public ElementBounds WithEmptyParent()
    {
        ParentBounds = Empty;
        return this;
    }

    //
    // Summary:
    //     Create a new ElementBounds instance with given fixed x/y position and width/height
    //     0
    //
    // Parameters:
    //   fixedX:
    //
    //   fixedY:
    public static ElementBounds Fixed(int fixedX, int fixedY)
    {
        return Fixed(fixedX, fixedY, 0.0, 0.0);
    }

    public static ElementBounds FixedPos(EnumDialogArea alignment, double fixedX, double fixedY)
    {
        return new ElementBounds
        {
            Alignment = alignment,
            BothSizing = ElementSizing.Fixed,
            fixedX = fixedX,
            fixedY = fixedY
        };
    }

    //
    // Summary:
    //     Quick method to create a new ElementBounds instance that uses fixed element sizing.
    //     The X/Y Coordinates are left at 0.
    //
    // Parameters:
    //   fixedWidth:
    //
    //   fixedHeight:
    public static ElementBounds FixedSize(double fixedWidth, double fixedHeight)
    {
        return new ElementBounds
        {
            Alignment = EnumDialogArea.None,
            fixedWidth = fixedWidth,
            fixedHeight = fixedHeight,
            BothSizing = ElementSizing.Fixed
        };
    }

    //
    // Summary:
    //     Quick method to create a new ElementBounds instance that uses fixed element sizing.
    //     The X/Y Coordinates are left at 0.
    //
    // Parameters:
    //   alignment:
    //
    //   fixedWidth:
    //
    //   fixedHeight:
    public static ElementBounds FixedSize(EnumDialogArea alignment, double fixedWidth, double fixedHeight)
    {
        return new ElementBounds
        {
            Alignment = alignment,
            fixedWidth = fixedWidth,
            fixedHeight = fixedHeight,
            BothSizing = ElementSizing.Fixed
        };
    }

    //
    // Summary:
    //     Quick method to create new ElementsBounds instance that uses fixed element sizing.
    //
    //
    // Parameters:
    //   fixedX:
    //
    //   fixedY:
    //
    //   fixedWidth:
    //
    //   fixedHeight:
    public static ElementBounds Fixed(double fixedX, double fixedY, double fixedWidth, double fixedHeight)
    {
        return new ElementBounds
        {
            fixedX = fixedX,
            fixedY = fixedY,
            fixedWidth = fixedWidth,
            fixedHeight = fixedHeight,
            BothSizing = ElementSizing.Fixed
        };
    }

    //
    // Summary:
    //     Quick method to create new ElementsBounds instance that uses fixed element sizing.
    //
    //
    // Parameters:
    //   alignment:
    //
    //   fixedOffsetX:
    //
    //   fixedOffsetY:
    //
    //   fixedWidth:
    //
    //   fixedHeight:
    public static ElementBounds FixedOffseted(EnumDialogArea alignment, double fixedOffsetX, double fixedOffsetY, double fixedWidth, double fixedHeight)
    {
        return new ElementBounds
        {
            Alignment = alignment,
            fixedOffsetX = fixedOffsetX,
            fixedOffsetY = fixedOffsetY,
            fixedWidth = fixedWidth,
            fixedHeight = fixedHeight,
            BothSizing = ElementSizing.Fixed
        };
    }

    //
    // Summary:
    //     Quick method to create new ElementsBounds instance that uses fixed element sizing.
    //
    //
    // Parameters:
    //   alignment:
    //
    //   fixedX:
    //
    //   fixedY:
    //
    //   fixedWidth:
    //
    //   fixedHeight:
    public static ElementBounds Fixed(EnumDialogArea alignment, double fixedX, double fixedY, double fixedWidth, double fixedHeight)
    {
        return new ElementBounds
        {
            Alignment = alignment,
            fixedX = fixedX,
            fixedY = fixedY,
            fixedWidth = fixedWidth,
            fixedHeight = fixedHeight,
            BothSizing = ElementSizing.Fixed
        };
    }

    //
    // Summary:
    //     Quick method to create new ElementsBounds instance that uses percentual element
    //     sizing, e.g. setting percentWidth to 0.5 will set the width of the bounds to
    //     50% of its parent width
    //
    // Parameters:
    //   alignment:
    //
    //   percentWidth:
    //
    //   percentHeight:
    public static ElementBounds Percentual(EnumDialogArea alignment, double percentWidth, double percentHeight)
    {
        return new ElementBounds
        {
            Alignment = alignment,
            percentWidth = percentWidth,
            percentHeight = percentHeight,
            BothSizing = ElementSizing.Percentual
        };
    }

    //
    // Summary:
    //     Quick method to create new ElementsBounds instance that uses percentual element
    //     sizing, e.g. setting percentWidth to 0.5 will set the width of the bounds to
    //     50% of its parent width
    //
    // Parameters:
    //   percentX:
    //
    //   percentY:
    //
    //   percentWidth:
    //
    //   percentHeight:
    public static ElementBounds Percentual(double percentX, double percentY, double percentWidth, double percentHeight)
    {
        return new ElementBounds
        {
            percentX = percentX,
            percentY = percentY,
            percentWidth = percentWidth,
            percentHeight = percentHeight,
            BothSizing = ElementSizing.Percentual
        };
    }
}
#if false // Decompilation log
'180' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Could not find by name: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Could not find by name: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Found single assembly: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\protobuf-net.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Specialized.dll'
------------------
Resolve: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NetworkInformation.dll'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\cairo-sharp.dll'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ObjectModel.dll'
------------------
Resolve: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.TypeConverter.dll'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.GraphicsLibraryFramework.dll'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Found single assembly: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Microsoft.Data.Sqlite.dll'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.Desktop.dll'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NameResolution.dll'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.ThreadPool.dll'
------------------
Resolve: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.NonGeneric.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
