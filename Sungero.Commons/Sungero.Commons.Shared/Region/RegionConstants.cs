using System;

namespace Sungero.Commons.Constants
{
  public static class Region
  {
    /// <summary>
    /// Regexp маска парсинга типов регионов.
    /// </summary>
    public const string AddressTypesMask = @"(р[еспублика]*\.?|к[рай]*\.?|о[бласть]*\.?|а[втономный]*\.?\s?о[круг]*\.?)";
  }
}