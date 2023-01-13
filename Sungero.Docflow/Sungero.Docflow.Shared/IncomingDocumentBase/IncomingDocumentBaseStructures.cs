using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.IncomingDocumentBase
{
  /// <summary>
  /// Отступы для расположения отметки о поступлении.
  /// </summary>
  partial class RegistrationStampPosition
  {
    public double RightIndent { get; set; }
    
    public double BottomIndent { get; set; }
  }
}