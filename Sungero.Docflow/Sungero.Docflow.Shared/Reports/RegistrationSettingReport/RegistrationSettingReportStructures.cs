using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.RegistrationSettingReport
{

  /// <summary>
  /// Строчка отчета.
  /// </summary>
  partial class TableLine
  {
    public string ReportSessionId { get; set; }
    
    public int Id { get; set; }
    
    public string BusinessUnit { get; set; }
    
    public string DocumentFlow { get; set; }

    public int? DocumentFlowIndex { get; set; }
    
    public string DocumentKind { get; set; }
    
    public string RegistrationSetting { get; set; }

    public string RegistrationSettingUri { get; set; }
    
    public int Priority { get; set; }
    
    public string Departments { get; set; }
    
    public string SettingType { get; set; }
    
    public string DocumentRegister { get; set; }
    
    public string DocumentRegisterUri { get; set; }

    public string NumberExample { get; set; }
  }
}