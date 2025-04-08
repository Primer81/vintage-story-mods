using Cairo;

namespace Vintagestory.API.Client;

public delegate void DrawIconDelegate(Context cr, string type, int x, int y, float width, float height, double[] rgba);
