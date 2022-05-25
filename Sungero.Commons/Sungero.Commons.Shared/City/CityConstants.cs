using System;
using Sungero.Core;

namespace Sungero.Commons.Constants
{
  public static class City
  {
    /// <summary>
    /// Regexp маска парсинга типов населенных пунктов.
    /// </summary>
    public const string AddressTypesMask = @"(г[ород]*\.?|п[оселок]*\.?\s*г[ородского]*\.?\s*т[ипа]*\.?)";
    
    public const string CityMask = "г.";
    public const string LocalityMask = "пгт.";
  }
}