using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.InternalDocumentBase;

namespace Sungero.Docflow.Shared
{
  partial class InternalDocumentBaseFunctions
  {
    /// <summary>
    /// Получить ответственного за документ.
    /// </summary>
    /// <returns>Ответственный за документ.</returns>
    public override Sungero.Company.IEmployee GetDocumentResponsibleEmployee()
    {
      if (_obj.PreparedBy != null)
        return _obj.PreparedBy;
      
      return base.GetDocumentResponsibleEmployee();
    }

    /// <summary>
    /// Отключение родительской функции, т.к. здесь не нужна доступность рег.номера и даты.
    /// </summary>
    public override void EnableRegistrationNumberAndDate()
    {
      
    }
    
  }
}