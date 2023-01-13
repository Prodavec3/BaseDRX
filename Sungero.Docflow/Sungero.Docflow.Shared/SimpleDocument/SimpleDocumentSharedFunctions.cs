using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.SimpleDocument;

namespace Sungero.Docflow.Shared
{
  partial class SimpleDocumentFunctions
  {
    public override void ChangeRegistrationPaneVisibility(bool needShow, bool repeatRegister)
    {
      base.ChangeRegistrationPaneVisibility(needShow, repeatRegister);
      
      var notNumerable = _obj.DocumentKind != null && _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.NotNumerable;
      var needShowRegistrationProperties = !notNumerable && needShow;
      var canRegister = _obj.AccessRights.CanRegister();
      var caseIsEnabled = notNumerable || !notNumerable && canRegister;
      // Может быть уже закрыто от редактирования, если документ зарегистрирован и в формате номера журнала
      // присутствует индекс файла.
      caseIsEnabled = caseIsEnabled && _obj.State.Properties.CaseFile.IsEnabled;
      
      _obj.State.Properties.RegistrationNumber.IsVisible = needShowRegistrationProperties;
      _obj.State.Properties.RegistrationDate.IsVisible = needShowRegistrationProperties;
      _obj.State.Properties.DocumentRegister.IsVisible = needShowRegistrationProperties;
      _obj.State.Properties.CaseFile.IsEnabled = caseIsEnabled;
      _obj.State.Properties.PlacedToCaseFileDate.IsEnabled = caseIsEnabled;
    }
    
    public override void SetRequiredProperties()
    {
      base.SetRequiredProperties();
      
      var isNotNumerableType = _obj.DocumentKind == null || _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.NotNumerable;
      _obj.State.Properties.PreparedBy.IsRequired = !isNotNumerableType;
      
      // Изменить обязательность полей в зависимости от того, программная или визуальная работа.
      var isVisualMode = ((Domain.Shared.IExtendedEntity)_obj).Params.ContainsKey(Sungero.Docflow.PublicConstants.OfficialDocument.IsVisualModeParamName);
      
      // При программной работе содержание делаем необязательным.
      // Чтобы сбросить обязательность, если она изменилась в вызове текущего метода в базовой сущности.
      // При визуальной работе - обязательность содержания определится в вызове текущего метода в базовой сущности.
      if (!isVisualMode)
        _obj.State.Properties.Subject.IsRequired = false;
    }
    
    public override void RefreshDocumentForm()
    {
      base.RefreshDocumentForm();
      
      var isNotNumerable = _obj.DocumentKind == null || _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.NotNumerable;
      _obj.State.Properties.BusinessUnit.IsVisible = !isNotNumerable;
      _obj.State.Properties.Department.IsVisible = !isNotNumerable;
      _obj.State.Properties.OurSignatory.IsVisible = !isNotNumerable || this.GetShowOurSigningReasonParam();
      _obj.State.Properties.PreparedBy.IsVisible = !isNotNumerable;
      _obj.State.Properties.Assignee.IsVisible = !isNotNumerable;
    }
    
    [Public]
    public override bool IsVerificationModeSupported()
    {
      return true;
    }
    
    [Public]
    public override bool HasEmptyRequiredProperties()
    {
      return string.IsNullOrEmpty(_obj.Subject) && (_obj.Info.Properties.Subject.IsRequired ||
                                                    (_obj.DocumentKind != null &&
                                                     (_obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.Registrable ||
                                                      _obj.DocumentKind.GenerateDocumentName == true)));
    }
  }
}