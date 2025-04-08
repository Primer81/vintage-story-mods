namespace Vintagestory.API.Common;

/// <summary>
/// Should return sin(solar altitude angle). i.e. -1 for 90 degrees far below horizon, 0 for horizon and 1 for vertical
/// </summary>
/// <param name="posX">World x coordinate</param>
/// <param name="posZ">World z coordinate</param>
/// <param name="yearRel">Current year progress, from 0..1</param>
/// <param name="dayRel">Current day progress, from 0..1</param>
/// <returns></returns>
public delegate SolarSphericalCoords SolarSphericalCoordsDelegate(double posX, double posZ, float yearRel, float dayRel);
