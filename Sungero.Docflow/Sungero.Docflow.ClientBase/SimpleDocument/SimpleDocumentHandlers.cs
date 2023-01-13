using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.SimpleDocument;

namespace Sungero.Docflow
{
  partial class SimpleDocumentClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      base.Showing(e);
      
      // Показать хинт о том, что при удалении связи с ведущим документов, приложение было преобразовано в простой документ.
      if (e.Params.Contains(Constants.Addendum.UnbindAddendumParamName))
        e.AddInformation(Sungero.Docflow.SimpleDocuments.Resources.UnbindAddendumHint);
    }
    
    public override void Closing(Sungero.Presentation.FormClosingEventArgs e)
    {
      base.Closing(e);
      _obj.State.Properties.Subject.IsRequired = false;
      
      // При отмене восстанавливаем связь, которая была удалена действием "Удалить связь".
      if (e.Params.Contains(Constants.Addendum.UnbindAddendumParamName))
        Functions.Addendum.Remote.RestoreAddendumRelationToLeadingDocument(_obj.Id);
    }

    public override void DocumentKindValueInput(Sungero.Docflow.Client.OfficialDocumentDocumentKindValueInputEventArgs e)
    {
      // Определить НОР до вызова метода предка для корректной подборки настройки регистрации.
      // При отсутствии НОР настройка регистрации может не подобраться и отобразится предупреждающий хинт, хотя настройки (при заполненной НОР) подбираются корректно.
      if (e.NewValue != null && e.NewValue.NumberingType != Docflow.DocumentKind.NumberingType.NotNumerable && _obj.BusinessUnit == null)
        _obj.BusinessUnit = Docflow.PublicFunctions.Module.GetDefaultBusinessUnit(Company.Employees.Current);
      
      base.DocumentKindValueInput(e);
    }

  }
}