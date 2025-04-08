using System;
using System.Collections.Generic;
using Vintagestory.API.Client;

namespace Vintagestory.API.Common;

/// <summary>
/// A tag parser
/// </summary>
/// <param name="capi"></param>
/// <param name="token">The currently parsed token, its attributes, and child elements</param>
/// <param name="fontStack">The current font, you and push a new font if this tag modifies the current font or call .Peek() to get the current one</param>
/// <param name="didClickLink">Handler passed on by the displaying dialog that should be called if a user pressed a piece of text, if it is clickable at all</param>
/// <returns></returns>
public delegate RichTextComponentBase Tag2RichTextDelegate(ICoreClientAPI capi, VtmlTagToken token, Stack<CairoFont> fontStack, Action<LinkTextComponent> didClickLink);
