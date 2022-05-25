using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.CounterpartyDocument;

namespace Sungero.Docflow
{
  partial class CounterpartyDocumentClientHandlers
  {

    public virtual void CounterpartyValueInput(Sungero.Docflow.Client.CounterpartyDocumentCounterpartyValueInputEventArgs e)
    {
      Functions.CounterpartyDocument.FillName(_obj);
    }

    public override void DocumentKindValueInput(Sungero.Docflow.Client.OfficialDocumentDocumentKindValueInputEventArgs e)
    {
      base.DocumentKindValueInput(e);
      
      // Определить НОР до вызова метода предка для корректной подборки настройки регистрации.
      // При отсутствии НОР настройка регистрации может не подобраться и отобразится предупреждающий хинт, хотя настройки (при заполненной НОР) подбираются корректно.
      if (e.NewValue != null && e.NewValue.NumberingType != Docflow.DocumentKind.NumberingType.NotNumerable && _obj.BusinessUnit == null)
        _obj.BusinessUnit = Docflow.PublicFunctions.Module.GetDefaultBusinessUnit(Company.Employees.Current);
    }

  }
}