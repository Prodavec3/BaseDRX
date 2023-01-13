using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.FormalizedPowerOfAttorney
{
  /// <summary>
  /// Информация о том, кому выдана доверенность.
  /// </summary>
  [Public]
  partial class IssuedToInfo
  {
    public string FullName { get; set; }
    
    public string TIN { get; set; }
    
    public string INILA { get; set; }
  }
}