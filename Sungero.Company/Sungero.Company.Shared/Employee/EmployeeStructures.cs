using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company.Structures.Employee
{ 
  /// <summary>
  /// Полное имя человека.
  /// </summary>
  partial class PersonFullName
  {
    /// <summary>
    /// Фамилия.
    /// </summary>
    public string LastName { get; set; }
    
    /// <summary>
    /// Имя.
    /// </summary>
    public string FirstName { get; set; }
    
    /// <summary>
    /// Отчество.
    /// </summary>
    public string MiddleName { get; set; }
  }
}