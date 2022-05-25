using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Meetings.Minutes;

namespace Sungero.Meetings.Shared
{
  partial class MinutesFunctions
  {
    /// <summary>
    /// Установить обязательность свойств в зависимости от заполненных данных.
    /// </summary>
    public override void SetRequiredProperties()
    {
      base.SetRequiredProperties();
      if (_obj.Meeting != null)
        _obj.State.Properties.Subject.IsRequired = false;
    }
    
    /// <summary>
    /// Заполнить имя.
    /// </summary>
    public override void FillName()
    {
      if (_obj.Meeting == null)
      {
        base.FillName();
        return;
      }
      var documentKind = _obj.DocumentKind;
      
      if (documentKind != null && !documentKind.GenerateDocumentName.Value && _obj.Name == Docflow.Resources.DocumentNameAutotext)
        _obj.Name = string.Empty;
      
      if (documentKind == null || !documentKind.GenerateDocumentName.Value)
        return;
      
      var name = string.Empty;
      
      var meeting = _obj.Meeting;
      
      /* Имя в формате:
        <Вид документа> №<номер> от <дата> по <тема совещания>.
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += Docflow.OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
        
        if (_obj.RegistrationDate != null)
          name += Docflow.OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
        
        name += Functions.Meeting.GetMeetingName(meeting);
      }
      
      if (string.IsNullOrWhiteSpace(name))
        name = Docflow.Resources.DocumentNameAutotext;
      else if (documentKind != null)
        name = documentKind.ShortName + name;
      
      name = Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = Docflow.PublicFunctions.OfficialDocument.AddClosingQuote(name, _obj);
      
    }
    
    public override void ChangeDocumentPropertiesAccess(bool isEnabled, bool isRepeatRegister)
    {
      base.ChangeDocumentPropertiesAccess(isEnabled, isRepeatRegister);
      var hasMeeting = _obj.Meeting != null;
      
      _obj.State.Properties.Subject.IsEnabled = hasMeeting || isEnabled || isRepeatRegister;
      _obj.State.Properties.Meeting.IsEnabled = !hasMeeting || (isEnabled && !CallContext.CalledFrom(Meetings.Info));
    }
  }
}