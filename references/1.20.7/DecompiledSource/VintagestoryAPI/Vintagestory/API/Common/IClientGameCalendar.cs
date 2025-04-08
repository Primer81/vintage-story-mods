using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface IClientGameCalendar : IGameCalendar
{
	/// <summary>
	/// Returns a normalized vector of the sun position at the players current location
	/// </summary>
	Vec3f SunPositionNormalized { get; }

	/// <summary>
	/// Returns a vector of the sun position at the players current location
	/// </summary>
	Vec3f SunPosition { get; }

	/// <summary>
	/// Returns a vector of the moon position at the players current location
	/// </summary>
	Vec3f MoonPosition { get; }

	/// <summary>
	/// Returns a normalized color of the sun at the players current location
	/// </summary>
	Vec3f SunColor { get; }

	Vec3f ReflectColor { get; }

	/// <summary>
	/// A horizontal offset that is applied when reading the sky glow color at the players current location. Creates a greater variety of sunsets. Changes to a different value once per day (during midnight)
	/// </summary>
	float SunsetMod { get; }

	/// <summary>
	/// Returns a value between 0 (no sunlight) and 1 (full sunlight) at the players current location
	/// </summary>
	/// <returns></returns>
	float DayLightStrength { get; }

	/// <summary>
	/// Returns a value between 0 (no sunlight) and 1 (full sunlight) at the players current location
	/// </summary>
	/// <returns></returns>
	float MoonLightStrength { get; }

	float SunLightStrength { get; }

	/// <summary>
	/// If true, its currently dusk at the players current location
	/// </summary>
	bool Dusk { get; }
}
