using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company.Structures.Module
{

  /// <summary>
  /// Замещение.
  /// </summary>
  partial class Substitution
  {
    /// <summary>
    /// Замещающий (кто).
    /// </summary>
    public IUser User { get; set; }
    
    /// <summary>
    /// Замещаемый (кого).
    /// </summary>
    public IUser SubstitutedUser { get; set; }
  }
  
}