using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public delegate void OnViewChangedDelegate(List<Vec2i> nowVisibleChunks, List<Vec2i> nowHiddenChunks);
