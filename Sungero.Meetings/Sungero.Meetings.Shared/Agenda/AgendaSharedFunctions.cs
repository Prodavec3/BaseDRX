using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Meetings.Agenda;

namespace Sungero.Meetings.Shared
{
  partial class AgendaFunctions
  {
    /// <summary>
    /// Установить обязательность свойств в зависимости от заполненных данных.
    /// </summary>
    public override void SetRequiredProperties()
    {
      base.SetRequiredProperties();
      _obj.State.Properties.Subject.IsRequired = false;

      _obj.State.Properties.BusinessUnit.IsRequired = true;
      _obj.State.Properties.Department.IsRequired = true;
      _obj.State.Properties.PreparedBy.IsRequired = true;
    }
    
    public override void ChangeDocumentPropertiesAccess(bool isEnabled, bool repeatRegister)
    {
      base.ChangeDocumentPropertiesAccess(isEnabled, repeatRegister);
      _obj.State.Properties.Assignee.IsVisible = false;
    }
    
    /// <summary>
    /// Заполнить имя.
    /// </summary>
    public override void FillName()
    {
      var documentKind = _obj.DocumentKind;
      
      if (documentKind != null && !documentKind.GenerateDocumentName.Value && _obj.Name == Docflow.Resources.DocumentNameAutotext)
        _obj.Name = string.Empty;
      
      if (documentKind == null || !documentKind.GenerateDocumentName.Value)
        return;
      
      var name = string.Empty;
      if (_obj.Meeting != null)
      {
        var meeting = _obj.Meeting;
        
        /* Имя в формате:
        <Вид документа> от <дата совещания> по <тема совещания>.
         */
        using (TenantInfo.Culture.SwitchTo())
        {
          name += Functions.Meeting.GetMeetingNameWithDate(meeting);
        }
      }
      
      if (string.IsNullOrWhiteSpace(name))
        name = Docflow.Resources.DocumentNameAutotext;
      else if (documentKind != null)
        name = documentKind.ShortName + name;
      
      name = Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = Docflow.PublicFunctions.OfficialDocument.AddClosingQuote(name, _obj);
      
    }
  }
}