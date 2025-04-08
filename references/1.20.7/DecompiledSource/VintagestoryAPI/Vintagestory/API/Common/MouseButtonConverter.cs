using System;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Vintagestory.API.Common;

public class MouseButtonConverter
{
	public static EnumMouseButton ToEnumMouseButton(MouseButton button)
	{
		return button switch
		{
			MouseButton.Button1 => EnumMouseButton.Left, 
			MouseButton.Button2 => EnumMouseButton.Right, 
			MouseButton.Button3 => EnumMouseButton.Middle, 
			MouseButton.Button4 => EnumMouseButton.Button4, 
			MouseButton.Button5 => EnumMouseButton.Button5, 
			MouseButton.Button6 => EnumMouseButton.Button6, 
			MouseButton.Button7 => EnumMouseButton.Button7, 
			MouseButton.Button8 => EnumMouseButton.Button8, 
			_ => throw new ArgumentOutOfRangeException("button", button, null), 
		};
	}
}
