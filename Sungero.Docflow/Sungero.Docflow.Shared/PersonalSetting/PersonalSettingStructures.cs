using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.PersonalSetting
{
  /// <summary>
  /// Форматная строка для адреса сайта проверки контрагента.
  /// </summary>
  partial class CompanySiteUrl
  {
    // Форматная строка с url сайта.
    public string Url { get; set; }
    
    // Признак обязательности ОГРН.
    public bool PsrnNeeded { get; set; }
    
    // Признак обязательности ИНН.
    public bool TinNeeded { get; set; }
  }

}