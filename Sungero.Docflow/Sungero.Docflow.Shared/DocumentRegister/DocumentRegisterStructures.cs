using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.DocumentRegister
{
    /// <summary>
    /// Индекс регистрационного номера.
    /// </summary>
    partial class RegistrationNumberIndex
    {
     public int Index { get; set; }
     
     public string Postfix { get; set; }
     
     public string CorrectingPostfix { get; set; }
    }

    /// <summary>
    /// Префикс и постфикс регистрационного номера документа.
    /// </summary>
    partial class RegistrationNumberParts
    {
     public string Prefix { get; set; }
     
     public string Postfix { get; set; }
    }

}